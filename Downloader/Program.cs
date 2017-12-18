using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Serialization;
using Shared;

namespace Downloader
{
    class Program
    {
        static void Main( string[] args )
        {
            //            Apartment app = new Apartment() { Id = "91435413" };
            //            GetDetailedInfo( app );

            if ( args.Length != 3 )
                return;

            var mode = args[0];
            var requestFileName = args[1];
            var outFileName = args[2];

            var contents = new List<string>();

            if ( mode == "web" )
            {
                var oldFiles = Directory.GetFiles( "Pages", "*.txt" );
                foreach ( var oldFile in oldFiles )
                    File.Delete( oldFile );

                var requests = File.ReadAllLines( requestFileName );
                int urlIndex = 0;
                foreach ( var url in requests )
                {
                    Console.Out.WriteLine( $"Processing request [{url}]..." );
                    Console.Out.WriteLine( "Downloading first page..." );

                    var page0 = Helpers.GetPage( url );
                    page0 = page0.Replace( "><", ">\r\n<" );
                    var urlParts = url.Split( '/' );
                    var hostName = urlParts[0] + "/" + urlParts[1] + "/" + urlParts[2];

                    contents.Add( page0 );
                    File.WriteAllText( $"Pages\\{urlIndex}-000.txt", page0, Encoding.UTF8 );

                    Console.Out.WriteLine( "Parsing meta..." );

                    List<string> pages;
                    int totalCount;
                    ParsePageForMeta( page0, out pages, out totalCount );

                    Console.Out.WriteLine( $"Found {totalCount} items on {pages.Count} pages." );
                    Console.Out.WriteLine( "Starting to download data..." );

                    for ( int i = 1; i < pages.Count; ++i ) // 1st page already downloaded
                    {
                        Console.Out.WriteLine( $"Downloading page {i}..." );

                        var pageUrl = hostName + pages[i];
                        var page = Helpers.GetPage( pageUrl );

                        contents.Add( page );
                        File.WriteAllText( $"Pages\\{urlIndex}-{i:000}.txt", page );
                    }

                    Console.Out.WriteLine( "Downloading done!" );
                    urlIndex++;
                }
            }

            if ( mode == "file" )
            {
                var files = Directory.GetFiles( "Pages" );
                foreach ( var file in files )
                {
                    contents.Add( File.ReadAllText( file, Encoding.UTF8 ) );
                }
            }

            Console.Out.WriteLine( "Parsing pages..." );
            var db = new List<Apartment>();

            foreach ( var content in contents )
            {
                ParsePage( db, content );
            }

            Console.Out.WriteLine( "Getting detailed info..." );
            int count = 0;
            foreach ( var apartment in db )
            {
                GetDetailedInfo( apartment );
                count++;
                Console.Out.WriteLine( $"{count} / {db.Count}..." );
            }

            using ( var sw = new StreamWriter( outFileName ) )
                new XmlSerializer( db.GetType() ).Serialize( sw, db );
        }

        static void ParsePageForMeta( string page, out List<string> resultPages, out int apartmentTotalCount )
        {
            resultPages = new List<string>();
            apartmentTotalCount = 0;
            var pages = 0;

            bool collectingPages = false;

            var lines = page.Split( '\n' );
            foreach ( var line in lines )
            {
                if ( apartmentTotalCount == 0 &&
                     line.Contains( "<span class=\"font-normal\" data-is24-qa=\"resultlist-resultCount\">" ) )
                {
                    var countString = Helpers.ExtractTag( line, 2 ).Replace( ".", "" );
                    apartmentTotalCount = int.Parse( countString );
                }

                if ( line.Contains( "<div id=\"pageSelection\"" ) )
                    collectingPages = true;

                if ( collectingPages && line.Contains( "</div>" ) )
                    collectingPages = false;

                if ( collectingPages && line.Contains( "<option value=" ) )
                {
                    pages = int.Parse( Helpers.ExtractValue( line, 1 ) );
                }

                if ( line.Contains( "<a href=\"/Suche/S-T/P-" ) )
                {
                    var url = Helpers.ExtractValue( line, 1 );
                    for ( int i = 1; i <= pages; ++i )
                    {
                        resultPages.Add( url.Replace( "/P-2/", "/P-" + ( i + 1 ).ToString() + "/" ) );
                    }
                }
            }
        }

        static void ParsePage( List<Apartment> db, string page )
        {
            var lines = page.Split( "\r\n".ToCharArray() );

            Apartment currentApartment = null;

            bool roomCountSoon = false;
            foreach ( var line in lines )
            {
                // Opening div
                if ( line.Contains( "<article data-item=\"result\"" ) )
                //if ( line.Contains( "<div class=\"gallery-container slick-gallery\">" ) )
                {
                    // Store previous
                    if ( currentApartment != null && !string.IsNullOrWhiteSpace( currentApartment.Id ) )
                    {
                        db.Add( currentApartment );
                        currentApartment = null;
                    }

                    currentApartment = new Apartment();
                }

                if ( currentApartment != null )
                {
                    // ID
                    if ( line.Contains( "data-go-to-expose-id=" ) && string.IsNullOrWhiteSpace( currentApartment.Id ) )
                    {
                        var index1 = line.IndexOf( "data-go-to-expose-id" ) +22 ;
                        var index2 = line.IndexOf( "\"", index1 );
                        currentApartment.Id = line.Substring( index1, index2 - index1 );
                        if ( string.IsNullOrWhiteSpace( currentApartment.Id ) )
                            currentApartment.Id = Helpers.ExtractValue( line, 1 );  // Multiple-app block
                    }

                    /*
                    // Images
                    if ( line.Contains( "img class=\"gallery__image" ) )
                    {
                        var path = Helpers.ExtractValue( line, 3 );

                        // Cut resizing trash an end
                        if ( path.Contains( ".jpg/" ) )
                        {
                            int index = path.IndexOf( ".jpg" );
                            path = path.Substring( 0, index ) + ".jpg";
                        }

                        // Cut resizing trash at start
                        if ( path.Contains( "/http://" ) )
                        {
                            int index = path.IndexOf( "/http://" );
                            path = path.Substring( index + 1 );
                        }

                        currentApartment.Images.Add( path );
                    }
                    */

                    // Name
                    if ( line.Contains( "</h5>" ) && string.IsNullOrWhiteSpace( currentApartment.Name ) )
                    {
                        //var name = line.Replace( "</h5>", "" ).Replace( "-->", "" ).Trim( " ".ToCharArray() );
                        var name = Helpers.ExtractTag( line, 2 );
                        currentApartment.Name =
                            name.Replace( "&auml;", "ä" ).Replace( "&uuml;", "ü" ).Replace( "&ouml;", "ö" );
                    }

                    // Distance + area
                    /*
                    if ( line.Contains( "<span class=\"font-ellipsis font-line-s font-s\">" ) )
                    {
                        var str = Helpers.ExtractTag( line, 2 );
                        var parts1 = str.Split( '|' );

                        float dist;
                        if ( float.TryParse( parts1[0].Replace( " km ", "" ).Replace( ".", "," ), out dist ) )
                            currentApartment.Distance = dist;

                        var parts2 = parts1[1].Split( ',' );
                        if ( parts2.Length == 3 )
                        {
                            currentApartment.Street = parts2[0].Trim( ' ' );
                            currentApartment.Region = parts2[1].Trim( ' ' );
                        } else if ( parts2.Length == 2 )
                        {
                            currentApartment.Region = parts2[0].Trim( ' ' );
                        }
                    }*/

                    // Distance
                    if ( line.Contains( "km|" ) )
                    {
                        var str = Helpers.ExtractTag( line, 2 ).Replace( "km|", "" );
                        float dist;
                        if ( float.TryParse( str.Replace( ".", "," ), out dist ) )
                            currentApartment.Distance = dist;
                    }

                    // Area
                    if ( line.Contains( "Auf der Karte anzeigen" ) )
                    {
                        var str = Helpers.ExtractTag( line, 2 );
                        var parts2 = str.Split( ',' );
                        if ( parts2.Length == 3 )
                        {
                            currentApartment.Street = parts2[0].Trim( ' ' );
                            currentApartment.Region = parts2[1].Trim( ' ' );
                        }
                        else if ( parts2.Length == 2 )
                        {
                            currentApartment.Region = parts2[0].Trim( ' ' );
                        }
                    }

                    // Price
                    if ( line.Contains( "€" ) && currentApartment.Price == 0 )
                    {
                        var str = Helpers.ExtractTag( line, 2 ).Replace( "€", "" ).Replace( ".", "" ).Split( '-' )[0].Trim( ' ' );

                        int price;
                        if ( int.TryParse( str.Split( ',' )[0], out price ) )
                            currentApartment.Price = price;
                    }

                    // Sqm
                    if ( line.Contains( "m²" ) && currentApartment.SqM == 0 )
                    {
                        var str = Helpers.ExtractTag( line, 2 ).Replace( "m²", "" ).Split( ',' )[0].Trim( ' ' );

                        int area;
                        if ( int.TryParse( str, out area ) )
                            currentApartment.SqM = area;

                        //roomCountSoon = true;
                    }

                    // Rooms
                    if ( line.Contains( "Zi.</span>" ) )
                    {
                        var str = Helpers.ExtractTag( line, 2 );
                        float rooms;
                        if ( float.TryParse( str, out rooms ) )
                        {
                            currentApartment.Rooms = rooms;
                            roomCountSoon = false;
                        }
                    }
                }
            }

            if ( currentApartment != null && !string.IsNullOrWhiteSpace( currentApartment.Id ) )
                db.Add( currentApartment );
        }

        static void GetDetailedInfo( Apartment app )
        {
            var page = Helpers.GetPage( $"https://www.immobilienscout24.de/expose/{app.Id}" );
            if ( string.IsNullOrWhiteSpace( page ) )
                return;

            var hausgeld = Helpers.ExtractAfterTag( page, "is24qa-hausgeld grid-item three-fifths" );
            app.Hausgeld = Helpers.EurToFloat( hausgeld );

            var income = Helpers.ExtractAfterTag( page, "is24qa-mieteinnahmen-pro-monat grid-item three-fifths" );
            app.RentIncome = Helpers.EurToFloat( income );

            if ( app.RentIncome != null && app.RentIncome > 0 )
                app.PriceToIncome = app.Price / app.RentIncome.Value;

            page = page.Replace( "><", ">\r\n<" );
            page = page.Replace( "> <", ">\r\n<" );
            var lines = page.Split( '\n' );

            foreach ( var line in lines )
            {
                if ( line.Contains( "<img class=\"sp-image" ) )
                {
                    var path = Helpers.ExtractValue( line, 3 );

                    // Cut resizing trash an end
                    if ( path.Contains( ".jpg/" ) )
                    {
                        int index = path.IndexOf( ".jpg" );
                        path = path.Substring( 0, index ) + ".jpg";

                        app.Images.Add( path );
                    }
                }
            }
            //var provision = Helpers.ExtractAfterTag( page, "is24qa-provision-note one-whole font-regular" );


        }
    }
}
