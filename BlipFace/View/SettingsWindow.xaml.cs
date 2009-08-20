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

            chbAutoLogon.IsChecked = Properties.Settings.Default.AutoLogon;
            chbAutoStart.IsChecked = Properties.Settings.Default.AutoStart;
        }

        private void btnCloseApp_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (chbAutoLogon.IsChecked.HasValue)
            {
                Properties.Settings.Default.AutoLogon = chbAutoLogon.IsChecked.Value;                
            }

            if (chbAutoStart.IsChecked.HasValue)
            {
                Properties.Settings.Default.AutoStart = chbAutoStart.IsChecked.Value;
                AutoStart.Current.EnabledThroughStartupMenu = chbAutoStart.IsChecked.Value;
            }

            Properties.Settings.Default.Save();
            Close();
        }
    }
}
