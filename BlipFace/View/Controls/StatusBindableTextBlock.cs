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

namespace BlipFace.View.Controls
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

        private static SolidColorBrush linkColor = new SolidColorBrush(Colors.Orange);

        static StatusBindableTextBlock()
        {

            //optymalizacja dzięki temu SolidColorBrush zajmuje mniej pamięci
            //Frozen SolidColorBrush 212 Bytes
            //Non-frozen SolidColorBrush 972 Bytes
 

           linkColor.Freeze(); 
        }

        private static void OnBoundStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //Run run = (Run) d;
            //InlineUIContainer run = (InlineUIContainer) d;
            //ContentControl con = (ContentControl) d;
            //Paragraph paragraph = (Paragraph) run.Parent;

            TextBlock mainTextBlock = (TextBlock) d;
            StatusViewModel s = (StatusViewModel) e.NewValue;



            //tworzymy link użytkownika
            Hyperlink hypUserLogin = CreateHyperLink(s.UserLogin, string.Format(userLinkFormat, s.UserLogin),string.Format(userProfileFormat, s.UserLogin));

            //dodajemy link użytkownka
            mainTextBlock.Inlines.Add(hypUserLogin);

            //tworzymy link odbiorcy wiadomośći
            if ((s.Type == "DirectedMessage") || (s.Type == "PrivateMessage"))
            {
                string mark = (s.Type == "DirectedMessage") ? " > " : " >> ";
                Run r = CreateRun(mark);

                mainTextBlock.Inlines.Add(r);

                //tworzymy link użytkownika odbiorcy wiadomości
                Hyperlink hypRecipientLogin = CreateHyperLink(s.RecipientLogin, string.Format(userLinkFormat, s.RecipientLogin), string.Format(userProfileFormat, s.RecipientLogin));
                //dodajemy link użytkownka
                mainTextBlock.Inlines.Add(hypRecipientLogin);
            }



            Run rr = CreateRun(": ");
            mainTextBlock.Inlines.Add(rr);


            
            //trochę nie wydajne, bo zbyt skomplikowane wyrażenie 
            //regularne, można byłoby uprościć gdyż wiemy(narazie)
            //że linki zaczynają się od http://rdir.pl/ a blipy od http://blip.pl/
            if(linkRegex.IsMatch(s.Content))
            {

                /*
                 * Jak to działa
                 * 1. Sprawdzamy czy w naszym blipnięciu znajdują się dopasowane linki
                 * 2. Jeżeli tak to wchodzimy do tego if'a w któym jesteśmy
                 * 3. Szukamy pierwszego dopasowanego linka (może to być zwykły link lub blipnięcie)
                 * 4. Dołączamy tekst przed linkiem do wyświetlenia
                 * 5. Dołączamy linka
                 * 6. Przesuwamy się w łańcuchu na pozycję za linkiem zmienna start
                 * 7. No i zaczynamy nową pętlę z sprawdzeniem czy istnieją jakieś jeszcze linki
                 * 8. Gdy nie ma więcej linków, dodajemy na koniec tekst za ostatnim linkiem
                 */ 
                var match = linkRegex.Match(s.Content);
                string content = s.Content;
                int start = 0;
                int end = 0;
                string linkUrl = string.Empty;
                
                while (match.Success)
                {
                    var g = match.Groups["Link"];
                     linkUrl= g.ToString();

                    end = content.IndexOf(linkUrl,start);
                    
                    //dodajemy tekst przed linkiem
                    //paragraph.Inlines.Add(content.Substring(start, end-start));
                    if (end > start)
                    {
                        string startContent = content.Substring(start, end - start);
                        mainTextBlock.Inlines.Add(startContent);
                    }

                    //dodajemy linka
                    

                    string rlink;
                    if(linkUrl.Contains("blip.pl"))
                    {
                        rlink ="[blip]";
                    }
                    else
                    {
                        rlink = "[link]";
                    }

                    Hyperlink h = CreateHyperLink(rlink, linkUrl, linkUrl);
                    
                    //dołączam linka do wyświetlenia
                    mainTextBlock.Inlines.Add(h);

                    //przesuwamy się w łańcuchu za obenice dopasowanego linka
                    start += end + linkUrl.Length;
                    
                    match = match.NextMatch();
                }
                
                //dołączamy to co jest za ostatnim linkiem
                if(start<content.Length)
                {
                    string contentend = content.Substring(start);
                    mainTextBlock.Inlines.Add(contentend);
                }
            
            }
            else
            {
                //gdy nie ma linków to możemy dodać od razu wszystko
               
                // paragraph.Inlines.Add(s.Content);
                mainTextBlock.Inlines.Add(s.Content);
            }

          
        }


        private static Run CreateRun(string mark)
        {
            Run r = new Run(mark)
                        {
                            FontSize = 8,
                            FontWeight = FontWeights.SemiBold,
                            Foreground = linkColor
                        };
            return r;
        }


        /// <summary>
        /// Tworzy linka i ustawia jego właściwości
        /// </summary>
        /// <param name="linkText"></param>
        /// <param name="linkAdress"></param>
        /// <param name="toolTip"></param>
        /// <returns></returns>
        private static Hyperlink CreateHyperLink(string linkText,string linkAdress,string toolTip)
        {
            Hyperlink hypUserLogin = new Hyperlink(new Run(linkText))
                                         {
                                             NavigateUri = new Uri(linkAdress),
                                             TextDecorations = null,
                                             FontWeight = FontWeights.SemiBold,
                                             Foreground = linkColor,
                                             ToolTip = toolTip
                                         };


            hypUserLogin.AddHandler(Hyperlink.RequestNavigateEvent, new RequestNavigateEventHandler(HyperLink_RequestNavigate));
            return hypUserLogin;
        }

        
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