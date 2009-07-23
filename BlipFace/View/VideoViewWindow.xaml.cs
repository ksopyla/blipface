using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BlipFace.View
{
    /// <summary>
    /// Interaction logic for VideoViewWindow.xaml
    /// </summary>
    public partial class VideoViewWindow : Window
    {
        private static string embededFormat =
            @"<object><param name=""allowFullScreen"" value=""true""></param>
<param name=""allowscriptaccess"" value=""always""></param>
<embed src=""http://www.youtube.com/v/{0}"" type=""application/x-shockwave-flash"" allowscriptaccess=""always"" allowfullscreen=""true"" width=""99%"" height=""99%"">
</embed></object>";


        private static Regex youtubeWatchKey = new Regex(@"v=([\w|-]*)[&\s]?");

        private string videoUrl;

        public VideoViewWindow(string url)
        {
            InitializeComponent();

            videoUrl = url;


            var m = youtubeWatchKey.Match(url);
            string videoKey = m.Groups[1].Value;

            string content = string.Format(embededFormat, videoKey);

           // wbVideoView.NavigateToString(content);

            tbUrlText.Text = videoUrl;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void btnCloseApp_Click(object sender, RoutedEventArgs e)
        {
           // wbVideoView.NavigateToString("auto:blank");
            Close();
        }


        private void hypVideoUrl_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Hyperlink hl = (Hyperlink)sender;
            string navigateUri = hl.NavigateUri.ToString();


            Process.Start(new ProcessStartInfo(navigateUri));
            e.Handled = true;
        }
    }
}