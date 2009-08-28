using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using BlipFace.Properties;

namespace BlipFace.View
{
    /// <summary>
    /// Interaction logic for AboutBlipFace.xaml
    /// </summary>
    public partial class AboutBlipFace : Window
    {
        public AboutBlipFace()
        {
            InitializeComponent();

            string releaseInfo = string.Format("Wersja: {0} Data wydania: {1}", Settings.Default.Version,
                                               Settings.Default.ReleaseDate);
            tblRelease.Text = releaseInfo;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Otwiera przeglądarkę gdy klikniemy na linka
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Hyperlink hl = (Hyperlink)sender;
            string navigateUri = hl.NavigateUri.ToString();
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(navigateUri));
            e.Handled = true;
        }
    }
}
