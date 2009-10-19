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
        private string[] availableKeys = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "I", "J", "K", "L", "M", "N", "O", "P", "R", "S", "T", "U", "W", "Y", "Z" };

        public SettingsWindow()
        {
            InitializeComponent();

            chbAutoLogon.IsChecked = Settings.Default.AutoLogon;
            chbAutoStart.IsChecked = Settings.Default.AutoStart;
            chbAlwaysInTray.IsChecked = Settings.Default.AlwaysInTray;
            chbMinimalizeToTray.IsChecked = Settings.Default.MinimalizeToTray;
            chbPlaySoundWhenNewStatus.IsChecked = Settings.Default.PlaySoundWhenNewStatus;
            chbHotKeyEnabled.IsChecked = Settings.Default.HotKeyEnabled;

            foreach (var key in availableKeys)
            {
                HotKeyComboBox.Items.Add(key);
            }
            HotKeyComboBox.SelectedItem = Settings.Default.HotKey.ToString();
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

            if (chbPlaySoundWhenNewStatus.IsChecked.HasValue)
            {
                Settings.Default.PlaySoundWhenNewStatus = chbPlaySoundWhenNewStatus.IsChecked.Value;
            }

            if (chbHotKeyEnabled.IsChecked.HasValue)
            {
                Settings.Default.HotKeyEnabled = chbHotKeyEnabled.IsChecked.Value;
            }

            System.Windows.Forms.KeysConverter keysConverter = new System.Windows.Forms.KeysConverter();
            Settings.Default.HotKey = (System.Windows.Forms.Keys)keysConverter.ConvertFromString(HotKeyComboBox.SelectedItem.ToString());

            Settings.Default.Save();
            Close();
        }

        private void chbHotKeyEnabled_Click(object sender, RoutedEventArgs e)
        {
            if (chbHotKeyEnabled.IsChecked.HasValue)
            {
                if (chbHotKeyEnabled.IsChecked.Value)
                {
                    HotKeyComboBox.IsEnabled = true;
                }
                else
                {
                    HotKeyComboBox.IsEnabled = false;
                }
            }
        }
    }
}
