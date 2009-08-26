using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;
using BlipFace.Helpers;

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
        

        private string videoUrl;

        public VideoViewWindow(string url)
        {
            InitializeComponent();

            videoUrl = url;


            Match m = BlipRegExp.YoutubeWatchKey.Match(url);
            string videoKey = m.Groups[1].Value;

            string content = ConstructHtmlVideoString2(videoKey);

            wbVideoView.NavigateToString(content);

            hypVideoUrl.NavigateUri = new Uri(videoUrl);
            tbUrlText.Text = videoUrl;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void btnCloseApp_Click(object sender, RoutedEventArgs e)
        {
            wbVideoView.NavigateToString("auto:blank");
            Close();
        }


        private void hypVideoUrl_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            var hl = (Hyperlink) sender;
            string navigateUri = hl.NavigateUri.ToString();


            Process.Start(new ProcessStartInfo(navigateUri));
            e.Handled = true;
        }


        private string ConstructHtmlVideoString2(string urlsToSwf)
        {
            var videoHtmlSb = new StringBuilder(1523);
            videoHtmlSb.Append(@"<!DOCTYPE HTML PUBLIC ""-//W3C//DTD HTML 4.01 Transitional//EN"" ");
            videoHtmlSb.Append(
                @"""http://www.w3.org/TR/html4/loose.dtd"">
<html>
<head>
<title>Video z Blip.pl</title>
</head>");
            videoHtmlSb.Append(
                @"<style>
body {
font: 12px Arial, sans-serif; background-color: #000000; height: 100%; width: 100%; margin: 0; overflow: hidden;
}
</style>");

            videoHtmlSb.Append(
@"<body>
<div id=""watch-player-div"" class=""flash-player"" style=""position: absolute; width:100%; height:100%;""> ");

            videoHtmlSb.Append(@"
<object width=""100%"" height=""100%"">");
            videoHtmlSb.AppendFormat(@"
<param name=""movie"" value=""http://www.youtube.com/v/{0}&hl=pl&fs=1&color1=0x3a3a3a&color2=0x999999""></param>",
                urlsToSwf);
            videoHtmlSb.Append(@"
<param name=""allowFullScreen"" value=""true""></param>
<param name=""allowscriptaccess"" value=""always""></param>");
            videoHtmlSb.AppendFormat(@"
<embed width=""100%"" height=""100%""
src=""http://www.youtube.com/v/{0}&hl=pl&fs=1&color1=0x3a3a3a&color2=0x999999"" type=""application/x-shockwave-flash"" 
allowscriptaccess=""always"" allowfullscreen=""true"" ></embed>
</object>",
                urlsToSwf);

            videoHtmlSb.Append(@"
</div>");
            videoHtmlSb.Append(@"
</body></html>");

            return videoHtmlSb.ToString();
        }

        //obecnie nie używana lecz niech jeszcze zostanie

        /*
        private string ConstructHtmlVideoString(string urlsToSwf)
        {
            var videoHtmlSb = new StringBuilder(1523);
            videoHtmlSb.Append(@"<!DOCTYPE HTML PUBLIC ""-//W3C//DTD HTML 4.01 Transitional//EN"" ");
            videoHtmlSb.Append(
                @"""http://www.w3.org/TR/html4/loose.dtd"">
<html>
<head>
<title>Video z Blip.pl</title>
</head>
<script type=""text/javascript"" ");
            videoHtmlSb.Append(
                @"src=""http://s.ytimg.com/yt/jsbin/www-core-vfl116470.js""></script>
<style>
body {
font: 12px Arial, sans-serif; background-color: #000000; height: 100%; width: 100%; margin: 0; overflow: hidden;
}
</style>");

            videoHtmlSb.Append(
                @"<body onload=""performOnLoadFunctions();"">
<div id=""watch-player-div"" class=""flash-player"" style=""position: absolute; width:100%; ");
            videoHtmlSb.Append(@"height:100%;""></div>
<script type=""text/javascript"">");

            videoHtmlSb.AppendFormat(
                @"var video_url =""http://www.youtube.com/v/{0}&amp;showinfo=0&amp;enablejsapi=1&amp;et=OEgsToPDskI43XQstbTAf-cernWK",
                urlsToSwf);
            videoHtmlSb.Append(
                @"3ymJ&amp;hl=pl&amp;autoplay=1&amp;fs=1"";
var pop_ads = """";
var fo = new SWFObject(video_url,""movie_player"", ""100%"", ""100%"", ""7"", ""#000000"");
var startTime = processLocationHashSeekTime();
fo.addParam(""allowFullscreen"",""true"");
fo.addParam(""AllowScriptAccess"", ""always"");
fo.addVariable(""el"", ""detailpage"");
fo.addVariable(""ps"", ""popup"");
");
            videoHtmlSb.Append(
                @"if (startTime){
fo.addVariable('start',startTime);
}
fo.write(""watch-player-div"");
</script>");

            videoHtmlSb.Append(@"</body>

</html>");

            return videoHtmlSb.ToString();
        }
      */
    }
}