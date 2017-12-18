using System;
using System.Windows.Controls;
using System.Windows.Media;
using Shared;

namespace Monitor
{
    /// <summary>
    /// Interaction logic for ListItem.xaml
    /// </summary>
    public partial class ListItem: UserControl
    {
        public ListItem( Apartment app )
        {
            InitializeComponent();

            this.App = app;

            Update();
        }

        public void Update()
        {
            tbName.Text = App.Name;
            tbPrice.Text = Math.Round( (float)App.Price / 1000 ) + "k";
            tbPricePerSqM.Text = Math.Round( (float)App.Price / App.SqM ).ToString();
            tbRooms.Text = App.Rooms.ToString();
            tbSqm.Text = App.SqM.ToString();
            tbDist.Text = Math.Round( App.Distance, 1 ).ToString();
            tbRegion.Text = App.Region;

            tbHausgeld.Text = App.Hausgeld != null ? $"{(int)App.Hausgeld}" : string.Empty;
            tbIncome.Text = App.RentIncome != null ? $"{(int)App.RentIncome}" : string.Empty;
            tbPriceToIncome.Text = App.PriceToIncome != null ? $"{(int)App.PriceToIncome}" : string.Empty;

            Background = Brushes.Transparent;

            if ( App.IsHidden )
                Background = new SolidColorBrush( Color.FromArgb( 64, 255, 0, 0 ) );
            else if ( App.IsFavorite )
                Background = new SolidColorBrush( Color.FromArgb( 128, 255, 200, 0 ) );
            else if ( App.IsNew )
                Background = new SolidColorBrush( Color.FromArgb( 128, 0, 200, 0 ) );

            if ( App.Price <= 100000 )
                tbPrice.Background = new SolidColorBrush( Color.FromRgb( 0, 200, 255 ) );
            else if ( App.Price <= 150000 )
                tbPrice.Background = new SolidColorBrush( Color.FromRgb( 128, 255, 0 ) );
            else if ( App.Price <= 200000 )
                tbPrice.Background = new SolidColorBrush( Color.FromRgb( 255, 255, 0 ) );
            else if ( App.Price <= 250000 )
                tbPrice.Background = new SolidColorBrush( Color.FromRgb( 255, 200, 0 ) );
            else if ( App.Price <= 300000 )
                tbPrice.Background = new SolidColorBrush( Color.FromRgb( 255, 150, 150 ) );
        }

        public Apartment App { get; }
    }
}
