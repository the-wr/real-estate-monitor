using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using Shared;

namespace Monitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow: Window
    {
        public enum SortBy { Price, PricePerSqM, Distance, Region };
        public enum ShowMode { New, Favorites, All, Hidden };

        private static string[] BlackList = { "vermietet", "investment", "investition", "anlage", "kapitalanleger" };

        private List<Apartment> db;
        private List<Apartment> filteredDb = new List<Apartment>();

        private int currentImage = 0;

        private SortBy sortBy = SortBy.Price;
        private ShowMode showMode = ShowMode.All;

        private bool muted;

        public MainWindow()
        {
            InitializeComponent();

            LoadDB();
            ApplyFilters();
            PopulateList();

            lvApartments.SelectionChanged += OnSelectionChanged;

            btnImgPrev.Click += delegate { ChangeImage( -1 ); };
            btnImgNext.Click += delegate { ChangeImage( 1 ); };
            imgLarge.MouseDown += delegate
            {
                gridFullScreen.Visibility = Visibility.Visible;
                imgFullScreen.Source = imgLarge.Source;
            };
            imgFullScreen.MouseDown += delegate
            {
                gridFullScreen.Visibility = Visibility.Collapsed;
            };

            btnOpen.Click += delegate
            {
                var li = lvApartments.SelectedItem as ListItem;
                if ( li == null )
                    return;

                Process.Start( $"https://www.immobilienscout24.de/expose/{li.App.Id}" );
            };

            btnOpenMap.Click += delegate
            {
                var li = lvApartments.SelectedItem as ListItem;
                if ( li == null )
                    return;

                var q = li.App.Street.Replace( " ", "+" );
                if ( !string.IsNullOrWhiteSpace( q ) )
                    q += "+";

                q += li.App.Region.Replace( " ", "+" );

                Process.Start( $"https://www.google.de/maps/search/{q}" );
            };
            // https://www.google.de/maps/search/maronenring+1b+borkheide/

            btnHide.Click += delegate
            {
                var li = lvApartments.SelectedItem as ListItem;
                if ( li == null )
                    return;

                var index = lvApartments.SelectedIndex;
                li.App.IsHidden = true;

                SaveDB();
                ApplyFilters();
                PopulateList();

                // Select next item
                if ( index < lvApartments.Items.Count )
                    lvApartments.SelectedIndex = index;
            };

            btnFavorite.Checked += OnBtnFavoriteCheckChanged;
            btnFavorite.Unchecked += OnBtnFavoriteCheckChanged;

            btnShowNew.Click += delegate
            {
                showMode = ShowMode.New;
                ApplyFilters();
                PopulateList();
            };
            btnShowFavorite.Click += delegate
            {
                showMode = ShowMode.Favorites;
                ApplyFilters();
                PopulateList();
            };
            btnShowAll.Click += delegate
            {
                showMode = ShowMode.All;
                ApplyFilters();
                PopulateList();
            };
            btnShowHidden.Click += delegate
            {
                showMode = ShowMode.Hidden;
                ApplyFilters();
                PopulateList();
            };
            btnSortByPrice.Click += delegate
            {
                sortBy = SortBy.Price;
                ApplyFilters();
                PopulateList();
            };
            btnSortByPricePerSqM.Click += delegate
            {
                sortBy = SortBy.PricePerSqM;
                ApplyFilters();
                PopulateList();
            };
            btnSortByDistance.Click += delegate
            {
                sortBy = SortBy.Distance;
                ApplyFilters();
                PopulateList();
            };
            btnSortByRegion.Click += delegate
            {
                sortBy = SortBy.Region;
                ApplyFilters();
                PopulateList();
            };

            btnUpdate.Click += OnBtnUpdateClicked;
            btnResetNew.Click += delegate
            {
                foreach ( var apartment in db )
                    apartment.IsNew = false;

                SaveDB();
                ApplyFilters();
                PopulateList();
            };
        }

        private void LoadDB()
        {
            using ( var sw = new StreamReader( "db.xml" ) )
                db = new XmlSerializer( typeof( List<Apartment> ) ).Deserialize( sw ) as List<Apartment>;
        }

        private void ApplyFilters()
        {
            filteredDb.Clear();

            foreach ( var apartment in db )
            {
                var name = apartment.Name.ToLowerInvariant();
                if ( BlackList.Any( bl => name.Contains( bl ) ) )
                    continue;

                if ( apartment.Images.Count <= 1 )
                    continue;

                if ( ( showMode == ShowMode.New && apartment.IsNew ) ||
                     ( showMode == ShowMode.Favorites && apartment.IsFavorite ) ||
                     ( showMode == ShowMode.Hidden && apartment.IsHidden ) ||
                     ( showMode == ShowMode.All && !apartment.IsHidden ) )
                {
                    filteredDb.Add( apartment );
                }
            }

            if(sortBy == SortBy.Price)
                filteredDb.Sort( ( a1, a2 ) => a1.Price.CompareTo( a2.Price ) );
            else if (sortBy == SortBy.Region)
                filteredDb.Sort( ( a1, a2 ) => a1.Region.CompareTo( a2.Region ) );
            else if ( sortBy == SortBy.Distance )
                filteredDb.Sort( ( a1, a2 ) => a1.Distance.CompareTo( a2.Distance ) );
            else if (sortBy == SortBy.PricePerSqM)
                filteredDb.Sort( ( a1, a2 ) => (a1.Price / a1.SqM).CompareTo( a2.Price / a2.SqM ) );
        }

        private void SaveDB()
        {
            using ( var sw = new StreamWriter( "db.xml" ) )
                new XmlSerializer( db.GetType() ).Serialize( sw, db );
        }

        private void PopulateList()
        {
            lvApartments.Items.Clear();

            foreach ( var apartment in filteredDb )
                lvApartments.Items.Add( new ListItem( apartment ) );

            tbCount.Text = filteredDb.Count.ToString();
        }

        private void OnSelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            var li = lvApartments.SelectedItem as ListItem;
            if ( li == null )
                return;

            muted = true;

            tbName.Text = li.App.Name;
            tbPrice.Text = li.App.Price.ToString();
            tbRooms.Text = li.App.Rooms.ToString();
            tbSqm.Text = li.App.SqM.ToString();
            tbRegion.Text = li.App.Region;
            tbStreet.Text = li.App.Street;
            btnFavorite.IsChecked = li.App.IsFavorite;

            spSmallImages.Children.Clear();
            int index = 0;
            currentImage = 0;

            tbCurrentImage.Text = $"1/{li.App.Images.Count}";

            foreach ( var image in li.App.Images )
            {
                int i = index;
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CreateOptions |= BitmapCreateOptions.IgnoreColorProfile;
                bitmap.UriSource = new Uri( image, UriKind.Absolute );
                bitmap.EndInit();

                var img = new Image { Source = bitmap, Margin = new Thickness( 0, 0, 5, 0 ) };
                spSmallImages.Children.Add( img );

                if ( i == 0 )
                    imgLarge.Source = bitmap;

                img.MouseDown += delegate
                {
                    currentImage = i;
                    imgLarge.Source = bitmap;
                    tbCurrentImage.Text = $"{i + 1}/{li.App.Images.Count}";
                };
                index++;
            }

            muted = false;
        }

        private void OnBtnFavoriteCheckChanged( object sender, RoutedEventArgs e )
        {
            var li = lvApartments.SelectedItem as ListItem;
            if ( li == null )
                return;

            li.App.IsFavorite = btnFavorite.IsChecked.Value;
            li.Update();
            SaveDB();
        }

        private void OnBtnUpdateClicked( object sender, RoutedEventArgs e )
        {
            var psi = new ProcessStartInfo()
            {
                WorkingDirectory = @"..\..\..\Downloader\bin\Debug",
                FileName = @"..\..\..\Downloader\bin\Debug\Downloader.exe",
                Arguments = "web requests.txt new.xml"
            };
            var process = Process.Start( psi );
            process.WaitForExit();

            using ( var sw = new StreamReader( @"..\..\..\Downloader\bin\Debug\new.xml" ) )
            {
                var newList = new XmlSerializer( typeof (List<Apartment>) ).Deserialize( sw ) as List<Apartment>;

                // Duplicate IDs possible, so no ToDictionary()
                var newDb = newList.GroupBy( x => x.Id ).Where( g => g.Count() == 1 ).Select( g => g.First() ).ToDictionary( a => a.Id );
                var oldDb = db.GroupBy( x => x.Id ).Where( g => g.Count() == 1 ).Select( g => g.First() ).ToDictionary( a => a.Id );
                var mergedList = new List<Apartment>();

                int newCount = 0;
                foreach ( var kvp in newDb )
                {
                    if ( oldDb.ContainsKey( kvp.Key ) )
                    {
                        // Old
                        mergedList.Add( oldDb[kvp.Key] ); // keep modifications
                        oldDb.Remove( kvp.Key );
                    }
                    else
                    {
                        // New
                        mergedList.Add( kvp.Value );
                        kvp.Value.IsNew = true;
                        newCount++;
                    }
                }

                // What's absent in new db
                int removedCount = 0;
                foreach ( var kvp in oldDb )
                {
                    mergedList.Add( kvp.Value );

                    if ( !kvp.Value.IsRemoved )
                        removedCount++;

                    kvp.Value.IsRemoved = true;
                }

                MessageBox.Show( $"Merge complete. {newCount} added." );

                db = mergedList;

                SaveDB();
                ApplyFilters();
                PopulateList();
            }
        }

        private void ChangeImage( int step )
        {
            var li = lvApartments.SelectedItem as ListItem;
            if ( li == null )
                return;

            currentImage += step;
            if ( currentImage < 0 )
                currentImage = li.App.Images.Count - 1;
            if ( currentImage >= li.App.Images.Count )
                currentImage = 0;

            if ( currentImage <= spSmallImages.Children.Count )
            {
                var smallImg = spSmallImages.Children[currentImage] as Image;
                if ( smallImg != null )
                    imgLarge.Source = smallImg.Source;

                tbCurrentImage.Text = $"{ currentImage + 1}/{li.App.Images.Count}";
            }
        }
    }
}
