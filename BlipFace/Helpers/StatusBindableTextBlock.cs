using System;
using System.Collections;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Navigation;
using BlipFace.Model;
using System.Text.RegularExpressions;

namespace BlipFace.Helpers.BindableRun
{
    
    public class StatusBindableTextBlock: TextBlock
    {
        public static readonly DependencyProperty BoundStatusProperty =
            DependencyProperty.Register("BoundStatus", 
            typeof(StatusViewModel),
            typeof(StatusBindableTextBlock), 
            new PropertyMetadata(new PropertyChangedCallback(StatusBindableTextBlock.OnBoundStatusChanged)));


        private static Regex linkRegex = new Regex(@"(?<Link>((http|https|ftp)\://[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,3}(:[a-zA-Z0-9]*)?/?([a-zA-Z0-9\-\._\?\,\'/\\\+&amp;%\$#\=~])*))");
        private static string userLinkFormat = "http://blip.pl/users/{0}/dashboard";
        private static string userProfileFormat = "Profil {0}";

        private static void OnBoundStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //Run run = (Run) d;
            //InlineUIContainer run = (InlineUIContainer) d;
            //ContentControl con = (ContentControl) d;
            //Paragraph paragraph = (Paragraph) run.Parent;

            TextBlock mainTextBlock = (TextBlock) d;
            StatusViewModel s = (StatusViewModel) e.NewValue;


            //tworzymy link użytkownika
            Hyperlink hypUserLogin = new Hyperlink(new Run(s.UserLogin));
            hypUserLogin.NavigateUri = new Uri(string.Format(userLinkFormat,s.UserLogin));
           // hypUserLogin.FontSize = 12;
            hypUserLogin.TextDecorations = null;
            hypUserLogin.FontWeight = FontWeights.SemiBold;
            hypUserLogin.Foreground = new SolidColorBrush(Colors.Orange);
            hypUserLogin.ToolTip = string.Format(userProfileFormat, s.UserLogin);
            hypUserLogin.AddHandler(Hyperlink.RequestNavigateEvent, new RequestNavigateEventHandler(HyperLink_RequestNavigate));
            
            //dodajemy link użytkownka
            mainTextBlock.Inlines.Add(hypUserLogin);

            //tworzymy link odbiorcy wiadomośći
            if ((s.Type == "DirectedMessage") || (s.Type == "PrivateMessage"))
            {

                string mark = (s.Type == "DirectedMessage") ? " > " : " >> ";
                Run r = new Run(mark);
                r.FontSize = 8;
                r.FontWeight = FontWeights.SemiBold;
                r.Foreground = new SolidColorBrush(Colors.Orange);
                
                mainTextBlock.Inlines.Add(r);

                //tworzymy link użytkownika
                Hyperlink hypRecipientLogin = new Hyperlink(new Run(s.RecipientLogin));
                hypRecipientLogin.NavigateUri = new Uri(string.Format(userLinkFormat, s.RecipientLogin));
                //hypRecipientLogin.FontSize = 12;
                hypRecipientLogin.TextDecorations = null;
                hypRecipientLogin.FontWeight = FontWeights.SemiBold;
                hypRecipientLogin.Foreground = new SolidColorBrush(Colors.Orange);
                hypRecipientLogin.ToolTip = string.Format(userProfileFormat, s.RecipientLogin);
                hypRecipientLogin.AddHandler(Hyperlink.RequestNavigateEvent, new RequestNavigateEventHandler(HyperLink_RequestNavigate));

                //dodajemy link użytkownka
                mainTextBlock.Inlines.Add(hypRecipientLogin);
            }


            
            Run rr = new Run(": ");
            rr.FontWeight = FontWeights.SemiBold;
            rr.FontSize = 9;
            rr.Foreground = new SolidColorBrush(Colors.Orange);
            mainTextBlock.Inlines.Add(rr);


            
            if(linkRegex.IsMatch(s.Content))
            {
                var match = linkRegex.Match(s.Content);
                string content = s.Content;
                int start = 0;
                int end = 0;
                while (match.Success)
                {
                    var g = match.Groups["Link"];
                    end = content.IndexOf(g.ToString(),start);


                    
                    //dodajemy tekst przed linkiem
                    //paragraph.Inlines.Add(content.Substring(start, end-start));
                    mainTextBlock.Inlines.Add(new Run(content.Substring(start, end - start)));

                   
                    //dodajemy linka
                    string linkUrl = g.ToString();

                    Run rlink;
                    if(linkUrl.Contains("blip.pl"))
                    {
                        rlink = new Run("[blip]");
                    }
                    else
                    {
                        rlink = new Run("[link]");
                    }

                    Hyperlink h = new Hyperlink(rlink);
                    h.NavigateUri = new Uri(g.ToString());
                    //h.FontSize = 12;
                    h.Foreground = new SolidColorBrush(Colors.Orange);
                    h.ToolTip = g.ToString();

                    //h.RequestNavigate += new System.Windows.Navigation.RequestNavigateEventHandler(HyperLink_RequestNavigate);
                    //h.Click += new RoutedEventHandler(Hyperlink_Click);
                    h.AddHandler(Hyperlink.RequestNavigateEvent, new RequestNavigateEventHandler(HyperLink_RequestNavigate));

                    
                    
                    //paragraph.Inlines.Add(t);
                    mainTextBlock.Inlines.Add(h);
                    start += end + g.ToString().Length;
                    
                    match = match.NextMatch();
                }

                
                //paragraph.Inlines.Add(content.Substring(start));
                mainTextBlock.Inlines.Add(content.Substring(start));
            }
            else
            {
                //gdy nie ma linków to możemy dodać od razu wszystko
               
                // paragraph.Inlines.Add(s.Content);
                mainTextBlock.Inlines.Add(s.Content);
            }

          
        }

        //public static void Hyperlink_Click(object sender, RoutedEventArgs e)
        //{
        //    Hyperlink hl = (Hyperlink)sender;
        //    string navigateUri = hl.NavigateUri.ToString();
        //    Process.Start(new ProcessStartInfo(navigateUri));
        //    e.Handled = true;
        //}

       public static void HyperLink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Hyperlink hl = (Hyperlink)sender;
            string navigateUri = hl.NavigateUri.ToString();
            Process.Start(new ProcessStartInfo(navigateUri));
            e.Handled = true;
        }

        

        public StatusViewModel  BoundStatus
        {
            get { return (StatusViewModel)GetValue(BoundStatusProperty); }
            set { SetValue(BoundStatusProperty, value); }
        }
    }
}
