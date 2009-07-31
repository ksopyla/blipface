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
        /*
        private static string embededFormat =
            @"<object width=""99%"" height=""99%"">
<param name=""allowFullScreen"" value=""true""></param>
<param name=""allowscriptaccess"" value=""always""></param>
<embed src=""http://www.youtube.com/v/{0}"" type=""application/x-shockwave-flash"" allowscriptaccess=""always"" allowfullscreen=""true"" width=""99%"" height=""99%"">
</embed></object>";
        */
        private static Regex youtubeWatchKey = new Regex(@"v=([\w|-]*)[&\s]?");

        private string videoUrl;

        public VideoViewWindow(string url)
        {
            InitializeComponent();

            videoUrl = url;


            var m = youtubeWatchKey.Match(url);
            string videoKey = m.Groups[1].Value;

            string content = ConstructHtmlVideoString(videoKey);

            wbVideoView.NavigateToString(content);

            hypVideoUrl.NavigateUri = new Uri(videoUrl);
            tbUrlText.Text = videoUrl;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void btnCloseApp_Click(object sender, RoutedEventArgs e)
        {
            wbVideoView.NavigateToString("auto:blank");
            Close();
        }


        private void hypVideoUrl_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Hyperlink hl = (Hyperlink) sender;
            string navigateUri = hl.NavigateUri.ToString();


            Process.Start(new ProcessStartInfo(navigateUri));
            e.Handled = true;
        }

        private string ConstructHtmlVideoString(string urlsToSwf)
        {
            StringBuilder videoHtmlSb = new StringBuilder(1523);
            videoHtmlSb.Append(@"<!DOCTYPE HTML PUBLIC ""-//W3C//DTD HTML 4.01 Transitional//EN"" ");
            videoHtmlSb.Append(@"""http://www.w3.org/TR/html4/loose.dtd"">
<html>
<head>
<title>Video z Blip.pl</title>
</head>
<script type=""text/javascript"" ");
            videoHtmlSb.Append(@"src=""http://s.ytimg.com/yt/js/base_all_with_bidi-vfl111852.js""></script>
<style>
body {
font: 12px Arial, sans-serif; background-color: #000000; height: 100%; width: 100%; margin: 0; overflow: hidden;
}
</style>");

            videoHtmlSb.Append(@"<body onload=""performOnLoadFunctions();"">
<div id=""watch-player-div"" class=""flash-player"" style=""position: absolute; width:100%; ");
            videoHtmlSb.Append(@"height:100%;""></div>
<script type=""text/javascript"">");

            videoHtmlSb.AppendFormat(@"var video_url =""http://www.youtube.com/v/{0}&amp;showinfo=0&amp;enablejsapi=1&amp;et=OEgsToPDskI43XQstbTAf-cernWK", urlsToSwf);
            videoHtmlSb.Append(@"3ymJ&amp;hl=pl&amp;autoplay=1&amp;fs=1"";
var pop_ads = """";
var fo = new SWFObject(video_url,""movie_player"", ""100%"", ""100%"", ""7"", ""#000000"");
var startTime = processLocationHashSeekTime();
fo.addParam(""allowFullscreen"",""true"");
fo.addParam(""AllowScriptAccess"", ""always"");
fo.addVariable(""el"", ""detailpage"");
fo.addVariable(""ps"", ""popup"");
");
            videoHtmlSb.Append(@"if (startTime){
fo.addVariable('start',startTime);
}
fo.write(""watch-player-div"");
</script>");

            videoHtmlSb.Append(@"</body>

</html>");

            return videoHtmlSb.ToString();
        }
    }
}