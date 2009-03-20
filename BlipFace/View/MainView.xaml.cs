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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BlipFace
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MainView : Window
    {

        const int BlipSize = 120;
        int charLeft=BlipSize;

        public MainView()
        {
            InitializeComponent();
        }

        private void tbMessage_TextChanged(object sender, TextChangedEventArgs e)
        {
            charLeft = BlipSize - tbMessage.Text.Length;
            if(charLeft<0)
            {
                tblCharLeft.Foreground = new SolidColorBrush(Color.FromRgb(200,100,100));
                btnSendBlip.IsEnabled = false;
            }else {
                tblCharLeft.Foreground = new SolidColorBrush(Color.FromRgb(160,160,160));
                btnSendBlip.IsEnabled = true;
            }

            tblCharLeft.Text = charLeft.ToString();
        }
    }
}
