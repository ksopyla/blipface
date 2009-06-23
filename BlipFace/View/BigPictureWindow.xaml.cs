using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace BlipFace.View
{
    /// <summary>
    /// Interaction logic for BigPictureWindow.xaml
    /// </summary>
    public partial class BigPictureWindow : Window
    {
        public ImageSource PictureSource
        {
            get { return imgBigPicture.Source; }
            set
            {
                
                imgBigPicture.Source = value;
                int picWidth = (int) value.Width;
                int picHeight = (int) value.Height;
                tbPictureSize.Text = string.Format("({0}x{1})", picWidth,picHeight);
            }
        }

        public string PictureUrl
        {
            get
            {
                return hypPictureUrl.NavigateUri.ToString();
            }
            set { 
                hypPictureUrl.NavigateUri = new Uri(value);
                tbUrlText.Text = value;
            }
        }

        public BigPictureWindow()
        {
            InitializeComponent();

            Width = SystemParameters.WorkArea.Width*0.9;
            Height = SystemParameters.WorkArea.Height*0.9;

            //imgBigPicture.MaxHeight = SystemParameters.WorkArea.Height*0.9;
            //imgBigPicture.MaxWidth = SystemParameters.WorkArea.Width * 0.88;

            //imgBigPicture.Width = 
        }

        private void imgBigPicture_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                Close();
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void hypPictureUrl_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Hyperlink hl = (Hyperlink)sender;
            string navigateUri = hl.NavigateUri.ToString();
            Process.Start(new ProcessStartInfo(navigateUri));
            e.Handled = true;
        }

        private void btnCloseApp_Click(object sender, RoutedEventArgs e)
        {
            
            Close();
        }
    }
}