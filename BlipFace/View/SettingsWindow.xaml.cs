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
using BlipFace.Helpers;
using BlipFace.Properties;

namespace BlipFace.View
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();

            chbAutoLogon.IsChecked = Settings.Default.AutoLogon;
            chbAutoStart.IsChecked = Settings.Default.AutoStart;
            chbAlwaysInTray.IsChecked = Settings.Default.AlwaysInTray;
            chbMinimalizeToTray.IsChecked = Settings.Default.MinimalizeToTray;
        }

        private void btnCloseApp_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (chbAutoLogon.IsChecked.HasValue)
            {
                Settings.Default.AutoLogon = chbAutoLogon.IsChecked.Value;                
            }

            if (chbAutoStart.IsChecked.HasValue)
            {
                Settings.Default.AutoStart = chbAutoStart.IsChecked.Value;
                AutoStart.Current.EnabledThroughStartupMenu = chbAutoStart.IsChecked.Value;
            }

            if (chbAlwaysInTray.IsChecked.HasValue)
            {
                Settings.Default.AlwaysInTray = chbAlwaysInTray.IsChecked.Value;
            }

            if (chbMinimalizeToTray.IsChecked.HasValue)
            {
                Settings.Default.MinimalizeToTray = chbMinimalizeToTray.IsChecked.Value;
            }

            Settings.Default.Save();
            Close();
        }
    }
}
