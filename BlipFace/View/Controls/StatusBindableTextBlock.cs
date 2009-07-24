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
using System.Collections.Generic;
using System.Windows.Input;
using BlipFace.Helpers;

namespace BlipFace.View.Controls
{
    //todo: brzydka ta kontrolka, ma wszystko w sobie, do rekatoryzacji
    public class StatusBindableTextBlock : TextBlock
    {
        public static readonly DependencyProperty BoundStatusProperty =
            DependencyProperty.Register("BoundStatus",
                                        typeof(StatusViewModel),
                                        typeof(StatusBindableTextBlock),
                                        new PropertyMetadata(
                                            new PropertyChangedCallback(StatusBindableTextBlock.OnBoundStatusChanged)));

        public StatusViewModel BoundStatus
        {
            get { return (StatusViewModel)GetValue(BoundStatusProperty); }
            set
            {
                if (value == null)
                    return;
                SetValue(BoundStatusProperty, value);
            }
        }

        
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
            if (d == null)
                return;
            //TextBlock mainTextBlock = (TextBlock)d;

            StatusBindableTextBlock mainTextBlock = (StatusBindableTextBlock) d;


            StatusViewModel s = (StatusViewModel) e.NewValue;
            if (s == null || s.Content == null)
                return;

            //czyścimy główny text box z pozostałości
            mainTextBlock.Inlines.Clear();

            //jeśli UserLogin == null to nie pokazujemy loginu
            if (!string.IsNullOrEmpty(s.UserLogin))
            {
                CreateUsersBegining(mainTextBlock, s);
            }


            string blipStatus = s.Content;
            var linkMatches = linkRegex.Matches(blipStatus);

            //jeśli nie ma linka,  to od razu dodajmy całą treść
            if (linkMatches.Count < 1)
            {
                //jeśli nie ma dopasowań linków
                FormatStatusFragment(s.Content, mainTextBlock);
            }
            else
            {
                int start = 0;

                //dla wszystkich dopasowań
                for (int k = 0; k < linkMatches.Count; k++)
                {
                    int startLink = linkMatches[k].Index;

                    //sprawdzamy odkąd zaczyna się link
                    if (startLink > 0)
                    {
                        //wycinamy częśc zwykłego tekstu, przed linkiem
                        string temp = blipStatus.Substring(start, startLink - start);
                        FormatStatusFragment(temp, mainTextBlock);
                    }
                    //zrób coś z samym linkiem

                    string rlink, tooltip;
                    Hyperlink h;

                    string linkUrl = linkMatches[k].Value;
                    if (linkUrl.Contains("blip.pl"))
                    {
                        //link do cytowania blipa
                        rlink = "[blip]";
                        tooltip = linkMatches[k].Value;

                        if (s.Cites != null && s.Cites.ContainsKey(tooltip))
                        {
                            tooltip = s.Cites[linkMatches[k].Value];
                        }

                        h = CreateLinkHyperLink(rlink, linkMatches[k].Value, tooltip, mainTextBlock);
                    }else if (linkUrl.Contains("youtube."))
                    {
                        rlink = "[video]";
                        tooltip = linkMatches[k].Value;

                        if (s.Links != null && s.Links.ContainsKey(tooltip))
                        {
                            tooltip = s.Links[linkMatches[k].Value];
                        }

                        h = CreateVideoHyperLink(rlink, linkMatches[k].Value, tooltip, mainTextBlock);
                    }
                    else
                    {
                        rlink = "[link]";
                        
                        //zwykły link


                        tooltip = linkMatches[k].Value;

                        if (s.Links != null && s.Links.ContainsKey(tooltip))
                        {
                            tooltip = s.Links[linkMatches[k].Value];
                        }

                        h = CreateLinkHyperLink(rlink, linkMatches[k].Value, tooltip, mainTextBlock);
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


                //dodaj na końcu linki do video

                //foreach (var url in videoUrls)
                //{
                //    var m = youtubeWatchKey.Match(url);
                //    string videoKey = m.Groups[1].Value;

                //    WebBrowser wb = new WebBrowser();
                //    wb.MinWidth = 200;
                //    wb.MinHeight = 200;
                //     string content = string.Format(embededFormat, videoKey);
                //    wb.NavigateToString(content);


                //    mainTextBlock.Inlines.Add(wb);
                //        //Inlines.Add(wb);


                //}
            }
        }


        private static void FormatStatusFragment(string statusFragment, StatusBindableTextBlock mainTextBlock)
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
                    Hyperlink h = CreateUserHyperLink(matchText, string.Format(userLinkFormat, matchText.Substring(1)),
                                                      matchText, mainTextBlock);

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
                    Hyperlink h = CreateTagHyperLink(matchText, string.Format(tagLinkFormat, matchText.Substring(1)),
                                                     matchText, mainTextBlock);

                    //dołączam linka do wyświetlenia
                    mainTextBlock.Inlines.Add(h);

                    statusFragment = statusFragment.Substring(position);
                }
            }
        }

        /// <summary>
        /// tworzy początek wiadomości, który zaczyna się od loginu(loginów) użytkownika
        /// </summary>
        /// <param name="mainTextBlock"></param>
        /// <param name="s"></param>
        private static void CreateUsersBegining(StatusBindableTextBlock mainTextBlock, StatusViewModel s)
        {
            Hyperlink hypUserLogin = CreateUserHyperLink(s.UserLogin, string.Format(userLinkFormat, s.UserLogin),
                                                         string.Format(userProfileFormat, s.UserLogin), mainTextBlock);

            //dodajemy link użytkownka
            mainTextBlock.Inlines.Add(hypUserLogin);

            //tworzymy link odbiorcy wiadomośći
            if ((s.Type == "DirectedMessage") || (s.Type == "PrivateMessage"))
            {
                string mark = (s.Type == "DirectedMessage") ? " > " : " >> ";
                //Run r = CreateRun(mark);

                mainTextBlock.Inlines.Add(mark);

                //tworzymy link użytkownika odbiorcy wiadomości
                Hyperlink hypRecipientLogin = CreateUserHyperLink(s.RecipientLogin,
                                                                  string.Format(userLinkFormat, s.RecipientLogin),
                                                                  string.Format(userProfileFormat, s.RecipientLogin),
                                                                  mainTextBlock);
                //dodajemy link użytkownka
                mainTextBlock.Inlines.Add(hypRecipientLogin);
            }


            //Run rr = CreateRun(": ");
            mainTextBlock.Inlines.Add(": ");
        }

        private static Hyperlink CreateTagHyperLink(string linkText, string address, string toolTip,
                                                    StatusBindableTextBlock block)
        {
            return CreateHyperLink(linkText, address, toolTip, FontWeights.Normal, tagsColor, StatusesCommands.Navigate);
        }

        private static Hyperlink CreateLinkHyperLink(string linkText, string address, string toolTip,
                                                     StatusBindableTextBlock block)
        {
            return CreateHyperLink(linkText, address, toolTip, FontWeights.Normal, hyperlinkColor, StatusesCommands.Navigate);
        }

        private static Hyperlink CreateUserHyperLink(string linkText, string address, string toolTip,
                                                     StatusBindableTextBlock block)
        {
            return CreateHyperLink(linkText, address, toolTip, FontWeights.SemiBold, userColor, StatusesCommands.Navigate);
        }

        private static Hyperlink CreateVideoHyperLink(string linkText, string address, string toolTip,
                                                      StatusBindableTextBlock block)
        {
            return CreateHyperLink(linkText, address,
                toolTip, FontWeights.Normal, hyperlinkColor, StatusesCommands.ShowVideo);
        }


        /// <summary>
        /// Tworzy linka i ustawia jego właściwości
        /// </summary>
        /// <param name="linkText"></param>
        /// <param name="linkAdress"></param>
        /// <param name="toolTip"></param>
        /// <returns></returns>
        private static Hyperlink CreateHyperLink(string linkText, string linkAdress, string toolTip, FontWeight weight,
                                                 Brush linkColor, RoutedCommand command)
        {
            System.Windows.Controls.ToolTip tip = new ToolTip();
            tip.Content = toolTip;

            tip.Style = (Style) Application.Current.FindResource("YellowToolTipStyle");


            Hyperlink hyperlink = new Hyperlink(new Run(linkText))
                                      {
                                          NavigateUri = new Uri(linkAdress),
                                          TextDecorations = null,
                                          FontWeight = weight,
                                          //FontWeights.SemiBold,
                                          Foreground = linkColor,
                                          ToolTip = tip
                                      };

            hyperlink.Command = command;
            hyperlink.CommandParameter = linkAdress;


            //hyperlink.AddHandler(Hyperlink.RequestNavigateEvent,
            //                        new RequestNavigateEventHandler(HyperLinkRequestNavigate));
            return hyperlink;
        }

        /*

        public static void HyperLinkRequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Hyperlink hl = (Hyperlink) sender;
            string navigateUri = hl.NavigateUri.ToString();
            Process.Start(new ProcessStartInfo(navigateUri));
            e.Handled = true;
        }

        */
    }
}