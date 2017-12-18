using System.IO;
using System.Net;
using System.Text;

namespace Shared
{
    public class Helpers
    {
        public static string GetPage( string url )
        {
            try
            {
                var request = WebRequest.Create( url ) as HttpWebRequest;
                request.Method = WebRequestMethods.Http.Get;
                request.Headers.Add( "Accept-Language", "en-US" );
                request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Win32)";

                Stream responseStream = request.GetResponse().GetResponseStream();

                if ( responseStream == null )
                    return string.Empty;
                StreamReader reader = new StreamReader( responseStream, Encoding.UTF8 );

                return reader.ReadToEnd();
            }
            catch
            {
                return null;
            }
        }

        public static string ExtractTag( string str, int index )
        {
            return Extract( str, "<>", index );
        }

        public static string ExtractValue( string str, int index )
        {
            return Extract( str, "\"", index );
        }

        public static string Extract( string str, string delimiters, int index )
        {
            var parts = str.Split( delimiters.ToCharArray() );
            if ( parts.Length <= index )
                return string.Empty;

            return parts[index];
        }

        public static string ExtractAfterTag( string str, string tag )
        {
            var start = str.IndexOf( tag );
            if ( start < 0 )
                return string.Empty;

            var closeTag = str.IndexOf( '>', start );
            if ( closeTag < 0 )
                return string.Empty;

            var nextTag = str.IndexOf( '<', closeTag );
            if ( nextTag < 0 )
                return string.Empty;

            return str.Substring( closeTag + 1, nextTag - closeTag - 1 );
        }

        public static float? EurToFloat( string str )
        {
            float v;
            if ( float.TryParse( str.Trim( " €".ToCharArray() ), out v ) )
                return v;

            return null;
        }

        public static int SortAsc( float? f1, float? f2 )
        {
            if ( f1 == null && f2 == null )
                return 0;
            if ( f1 != null && f2 == null )
                return -1;
            if ( f2 != null && f1 == null )
                return 1;

            return f1.Value.CompareTo( f2.Value );
        }

        public static int SortDesc( float? f1, float? f2 )
        {
            if ( f1 == null && f2 == null )
                return 0;
            if ( f1 != null && f2 == null )
                return -1;
            if ( f2 != null && f1 == null )
                return 1;

            return -f1.Value.CompareTo( f2.Value );
        }
    }
}
