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
    public class StatusBindableTextBlock : TextBlock
    {
        public static readonly DependencyProperty BoundStatusProperty =
            DependencyProperty.Register("BoundStatus",
                                        typeof(StatusViewModel),
                                        typeof(StatusBindableTextBlock),
                                        new PropertyMetadata(new PropertyChangedCallback(StatusBindableTextBlock.OnBoundStatusChanged)));
        
        
        /* Implementacja tworzenie statusu wersja 2
         * 
        private static Regex linkRegex = new Regex(@"(?<Link>((http|https|ftp)\://[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,3}(:[a-zA-Z0-9]*)?/?([a-zA-Z0-9\-\._\?\,\'/\\\+&amp;%\$#\=~])*))");


        //poprzednie regex \^(.)*?(?=(\s | $))
        private static Regex userRegex = new Regex(@"\^(\w)*?(?=\W)", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        //poprzednie \#(.)*?(?=(\s | $))
        private static Regex tagRegex = new Regex(@"\#(\w)*?(?=\W)", RegexOptions.IgnoreCase | RegexOptions.Singleline); //dopasowuje z spacją lub bez na końcu
        */
        

        #region Regex

        private static Regex textReg = new Regex(@"^[^\#\^]*");

        private static Regex userReg = new Regex(@"^\^\w*");

        private static Regex tagReg = new Regex(@"^\#\w*");

        private static Regex linkRegex = new Regex(@"(http|https|ftp)\://[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,3}\S*");
        #endregion



        #region StringFormats
        private static string userLinkFormat = "http://blip.pl/users/{0}/dashboard";
        private static string tagLinkFormat = "http://blip.pl/tags/{0}";
        private static string userProfileFormat = "Profil {0}";
        #endregion

        #region Brushes
        private static SolidColorBrush hyperlinkColor = new SolidColorBrush(Colors.Yellow);
        private static SolidColorBrush userColor = new SolidColorBrush(Colors.Orange);
        private static SolidColorBrush tagsColor = new SolidColorBrush(Colors.YellowGreen);
        #endregion
        static StatusBindableTextBlock()
        {

            //optymalizacja dzięki temu SolidColorBrush zajmuje mniej pamięci
            //Frozen SolidColorBrush 212 Bytes
            //Non-frozen SolidColorBrush 972 Bytes


            hyperlinkColor.Freeze();
            userColor.Freeze();
            tagsColor.Freeze();
        }

        private static void OnBoundStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //Run run = (Run) d;
            //InlineUIContainer run = (InlineUIContainer) d;
            //ContentControl con = (ContentControl) d;
            //Paragraph paragraph = (Paragraph) run.Parent;
            if(d ==null)
                return;
            TextBlock mainTextBlock = (TextBlock)d;
            StatusViewModel s = (StatusViewModel)e.NewValue;
            if (s == null || s.Content ==null)
                return;
            

            //czyścimy główny text box z pozostałości
            mainTextBlock.Inlines.Clear();

            //jeśli UserLogin == null to nie pokazujemy loginu
            if (!string.IsNullOrEmpty(s.UserLogin))
            {
                CreateUsersBegining(mainTextBlock, s);
            }

            //jeśli nie ma linka,  to od razu dodajmy całą treść
            string blipStatus = s.Content;
            var linkMatches = linkRegex.Matches(blipStatus);

            

            if (linkMatches.Count < 1)
            {
                //jeśli nie ma dopasowań linków
                FormatStatusFragment(s.Content,mainTextBlock);

            }
            else
            {

                int start = 0;

                //dla wszystkich dopasowań
                for (int k = 0; k < linkMatches.Count; k++)
                {
                    int startLink = linkMatches[k].Index;
                    if (startLink > 0)
                    {
                        string temp = blipStatus.Substring(start, startLink - start);
                        FormatStatusFragment(temp, mainTextBlock);
                    }
                    //zrób coś z samym linkiem

                    string rlink,tooltip;
                    Hyperlink h;

                    if (linkMatches[k].Value.Contains("blip.pl"))
                    {
                        rlink = "[blip]";
                        tooltip = s.Cites == null ? linkMatches[k].Value : s.Cites[linkMatches[k].Value];
                        h = CreateLinkHyperLink(rlink, linkMatches[k].Value, tooltip);
                    }
                    else
                    {
                        rlink = "[link]";
                        tooltip = s.Links == null ? linkMatches[k].Value : s.Links[linkMatches[k].Value];
                        h = CreateLinkHyperLink(rlink, linkMatches[k].Value, tooltip);
                    }

                   // Hyperlink h = CreateLinkHyperLink(rlink, linkMatches[k].Value, linkMatches[k].Value);

                    //dołączam linka do wyświetlenia
                    mainTextBlock.Inlines.Add(h);



                    //ustaw się za linkiem w łańcuchu 
                    start = startLink + linkMatches[k].Length;

                }


                //jeśli został jakiś tekst na końcu 
                if (start < s.Content.Length)
                {
                    FormatStatusFragment(s.Content.Substring(start), mainTextBlock);
                }
            }


            #region Second Implementatnio of making Status
            /*
            //jeśli nie ma linka, tagu lub użytownka to od razu dodajmy całą treść
            if (!linkRegex.IsMatch(s.Content) && !tagRegex.IsMatch(s.Content) && !userRegex.IsMatch(s.Content))
            {
                mainTextBlock.Inlines.Add(s.Content);
            }
            else
            {

                string[] words = s.Content.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < words.Length; i++)
                {
                    if (linkRegex.IsMatch(words[i]))
                    {
                        var match = linkRegex.Match(words[i]);

                        string linkAddress = match.Groups["Link"].ToString();

                        //create hyperlink
                        string rlink;
                        if (words[i].Contains("blip.pl"))
                        {
                            rlink = "[blip]";
                        }
                        else
                        {
                            rlink = "[link]";
                        }

                        Hyperlink h = CreateLinkHyperLink(rlink, words[i], words[i]);

                        //dołączam linka do wyświetlenia
                        mainTextBlock.Inlines.Add(h);
                        mainTextBlock.Inlines.Add(" ");

                    }
                    //else if (userRegex.IsMatch(words[i]))
                    else if (words[i].StartsWith("^") )
                    {
                        //create user hyperlink
                        Hyperlink h = CreateUserHyperLink(words[i], string.Format(userLinkFormat, words[i].Substring(1)), words[i]);

                        //dołączam linka do wyświetlenia
                        mainTextBlock.Inlines.Add(h);
                        mainTextBlock.Inlines.Add(" ");
                    }
                    //else if (tagRegex.IsMatch(words[i]))
                    else if (words[i].StartsWith("#"))
                    {
                        //create tag hyperlink

                        Hyperlink h = CreateTagHyperLink(words[i], string.Format(tagLinkFormat, words[i].Substring(1)), words[i]);

                        //dołączam linka do wyświetlenia
                        mainTextBlock.Inlines.Add(h);
                        mainTextBlock.Inlines.Add(" ");
                    }
                    else
                    {
                        //zwykły tekst to dodaj z spacją na końcu
                        mainTextBlock.Inlines.Add(words[i] + " ");
                    }

                }
            } 
            */
            #endregion

            #region FirstImplementation of making status
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
            //trochę nie wydajne, bo zbyt skomplikowane wyrażenie 
            //regularne, można byłoby uprościć gdyż wiemy(narazie)
            //że linki zaczynają się od http://rdir.pl/ a blipy od http://blip.pl/

            /*
            if(linkRegex.IsMatch(s.Content))
            {

                
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
                    start = end + linkUrl.Length;
                    
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
             * */
            #endregion

        }

       
        private static void FormatStatusFragment(string statusFragment,TextBlock mainTextBlock)
        {
            string matchText;

            Match match;
            int position = 0;

            while (!string.IsNullOrEmpty(statusFragment))
            {

                //dopasuj sam tekst, do rozpoczęcia tagu lub nazyw użytkonika
                match = textReg.Match(statusFragment);
                matchText = match.Value;
                if (!string.IsNullOrEmpty(matchText))
                {
                    position = matchText.Length;

                    //dodaj zwykły tekst do wyświetlenia
                    mainTextBlock.Inlines.Add(matchText);

                    //skróć łańcuch 
                    statusFragment = statusFragment.Substring(position);
                }

                //dopasuj nazwę użytkownika
                match = userReg.Match(statusFragment);
                matchText = match.Value;
                if (!string.IsNullOrEmpty(matchText))
                {
                    position = matchText.Length;
                    //stwórzy linka użytkownika
                    Hyperlink h = CreateUserHyperLink(matchText, string.Format(userLinkFormat, matchText.Substring(1)), matchText);

                    //dołączam linka do wyświetlenia
                    mainTextBlock.Inlines.Add(h);

                    statusFragment = statusFragment.Substring(position);
                }


                match = tagReg.Match(statusFragment);
                matchText = match.Value;
                if (!string.IsNullOrEmpty(matchText))
                {
                    position = matchText.Length;
                    //stwórzy linka użytkownika
                    Hyperlink h = CreateTagHyperLink(matchText, string.Format(tagLinkFormat, matchText.Substring(1)), matchText);

                    //dołączam linka do wyświetlenia
                    mainTextBlock.Inlines.Add(h);

                    statusFragment = statusFragment.Substring(position);
                }


            }
        }

        //tworzy początek wiadomości, który zaczyna się od loginu(loginów) użytkownika
        private static void CreateUsersBegining(TextBlock mainTextBlock, StatusViewModel s)
        {


            Hyperlink hypUserLogin = CreateUserHyperLink(s.UserLogin, string.Format(userLinkFormat, s.UserLogin), string.Format(userProfileFormat, s.UserLogin));

            //dodajemy link użytkownka
            mainTextBlock.Inlines.Add(hypUserLogin);

            //tworzymy link odbiorcy wiadomośći
            if ((s.Type == "DirectedMessage") || (s.Type == "PrivateMessage"))
            {
                string mark = (s.Type == "DirectedMessage") ? " > " : " >> ";
                //Run r = CreateRun(mark);

                mainTextBlock.Inlines.Add(mark);

                //tworzymy link użytkownika odbiorcy wiadomości
                Hyperlink hypRecipientLogin = CreateUserHyperLink(s.RecipientLogin, string.Format(userLinkFormat, s.RecipientLogin), string.Format(userProfileFormat, s.RecipientLogin));
                //dodajemy link użytkownka
                mainTextBlock.Inlines.Add(hypRecipientLogin);
            }



            //Run rr = CreateRun(": ");
            mainTextBlock.Inlines.Add(": ");
        }

        private static Hyperlink CreateTagHyperLink(string linkText, string address, string toolTip)
        {
            return CreateHyperLink(linkText, address, toolTip, FontWeights.Normal, tagsColor);
        }

        private static Hyperlink CreateLinkHyperLink(string linkText, string address, string toolTip)
        {
            return CreateHyperLink(linkText, address, toolTip, FontWeights.Normal, hyperlinkColor);
        }

        private static Hyperlink CreateUserHyperLink(string linkText, string address, string toolTip)
        {
            return CreateHyperLink(linkText, address, toolTip, FontWeights.SemiBold, userColor);
        }


        /// <summary>
        /// Tworzy linka i ustawia jego właściwości
        /// </summary>
        /// <param name="linkText"></param>
        /// <param name="linkAdress"></param>
        /// <param name="toolTip"></param>
        /// <returns></returns>
        private static Hyperlink CreateHyperLink(string linkText, string linkAdress, string toolTip, FontWeight weight, Brush linkColor)
        {
            Hyperlink hypUserLogin = new Hyperlink(new Run(linkText))
                                         {
                                             NavigateUri = new Uri(linkAdress),
                                             TextDecorations = null,
                                             FontWeight = weight, //FontWeights.SemiBold,
                                             Foreground = linkColor,
                                             ToolTip = toolTip
                                             
                                         };


            hypUserLogin.AddHandler(Hyperlink.RequestNavigateEvent, new RequestNavigateEventHandler(HyperLinkRequestNavigate));
            return hypUserLogin;
        }


        public static void HyperLinkRequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Hyperlink hl = (Hyperlink)sender;
            string navigateUri = hl.NavigateUri.ToString();
            Process.Start(new ProcessStartInfo(navigateUri));
            e.Handled = true;
        }



        public StatusViewModel BoundStatus
        {
            get { return (StatusViewModel)GetValue(BoundStatusProperty); }
            set
            {
                if(value ==null)
                    return;
                SetValue(BoundStatusProperty, value);
            }
        }
    }
}