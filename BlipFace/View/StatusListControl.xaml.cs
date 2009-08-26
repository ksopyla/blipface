using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using BlipFace.Model;
using BlipFace.Presenter;
using KeyEventArgs=System.Windows.Input.KeyEventArgs;
using Label=System.Windows.Controls.Label;
using MessageBox=System.Windows.MessageBox;

namespace BlipFace.View
{
    public partial class StatusListControl : IStatusesView
    {
       
        private readonly StatusesPresenter presenter;
        

        private string pictureFilePath = string.Empty;
        private object statusListLock = new object();

        public StatusListControl()
        {
            InitializeComponent();
        }

        public StatusListControl(StatusesPresenter _presenter)
        {
            InitializeComponent();

            // Insert code required on object creation below this point.

            presenter = _presenter;

            SetTextBoxFocus();
        }

        #region EventHandlers

        /// <summary>
        /// Służy tylko do wyliczania ilości znaków pozostałych do wpisania
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tbMessage_TextChanged(object sender, TextChangedEventArgs e)
        {
            int charLeft = presenter.CountChars(tbMessage.Text);


            if (charLeft < 0)
            {
                tblCharLeft.Foreground = new SolidColorBrush(Color.FromRgb(200, 100, 100));
                btnSendBlip.IsEnabled = false;
            }
            else if (charLeft == 0)
            {
                tblCharLeft.Foreground = new SolidColorBrush(Color.FromRgb(120, 180, 120));
                btnSendBlip.IsEnabled = true;
            }
            else
            {
                tblCharLeft.Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 160));
                btnSendBlip.IsEnabled = true;
            }

            tblCharLeft.Text = charLeft.ToString();
        }

        /// <summary>
        /// Zdarzenie gdy naciśnięty zostanie w kontrolce tbMessage klawisz
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tbMessage_KeyDown(object sender, KeyEventArgs e)
        {
            //if (e.Key == Key.Return)
            //{
            //    //gdy naciśnięto enter to wysyłamy tekst
            //    SendStatus();
            //}

            //e.Handled = true;
        }

        private void TbMessage_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                SendStatus();

                e.Handled = true;
            }
            //gdy naciśnięto enter to wysyłamy tekst
        }

        private void SendStatus()
        {
            int statusLenght = presenter.CountChars(tbMessage.Text);
            if (!string.IsNullOrEmpty(tbMessage.Text) && (statusLenght <= 160))
            {
                EnableContrlsForSendMessage(false);

                if (!string.IsNullOrEmpty(pictureFilePath))
                {
                    presenter.AddStatus(tbMessage.Text, pictureFilePath);
                }
                else
                {
                    presenter.AddStatus(tbMessage.Text);
                }
            }
            else if (statusLenght > 160)
            {
                MessageBox.Show("Status jest za długi");
                EnableContrlsForSendMessage(true);
            }
        }

        /// <summary>
        /// Handler dla kliknięca przycisku wysyłania tekstu dla blipa
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSendBlip_Click(object sender, RoutedEventArgs e)
        {
            SendStatus();
        }


        /// <summary>
        /// Otwiera przeglądarkę gdy klikniemy na linka
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            var hl = (Hyperlink) sender;
            string navigateUri = hl.NavigateUri.ToString();
            Process.Start(new ProcessStartInfo(navigateUri));
            e.Handled = true;
        }

        private void btnAddPicture_Click(object sender, RoutedEventArgs e)
        {
            var opf = new OpenFileDialog();
            opf.Filter = "Obrazy (*.bmp, *.jpg, *.gif, *.png)|*.bmp; *.jpg; *.gif; *.png|All Files|*.*";
            opf.Title = "Wybierz obraz do załączenia do statusu";
            opf.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            opf.AutoUpgradeEnabled = true;


            if (opf.ShowDialog() == DialogResult.OK)
            {
                //todo: ustaw obrazek
                //opf.FileName

                PicturePath = opf.FileName;
            }
        }


        private void btnDeletePic_Click(object sender, RoutedEventArgs e)
        {
            PicturePath = string.Empty;
        }

        #endregion

        //todo: błysk okna testowo
        [DllImport("user32.dll")]
        private static extern bool FlashWindow(IntPtr hwnd, bool bInvert);

        public static void FlashMainWindow(Window window, bool invert)
        {
            IntPtr handle = (new WindowInteropHelper(window)).Handle;
            FlashWindow(handle, invert);
        }

        private void ShowMessage(object sender, MouseButtonEventArgs e)
        {
            var lb = (Label) sender;

            string msg = (string) lb.Content + (string) lb.ToolTip;
            var message = new StringBuilder(msg);

            if (lb.Tag != null)
            {
                var s = (string) lb.Tag;
                message.Append(Environment.NewLine);
                message.Append(s);
            }

            MessageBox.Show(message.ToString(), "Blip Info", MessageBoxButton.OK, MessageBoxImage.Information);

            e.Handled = true;
        }

        /*
        private void ShowSettingsWindows(object sender, MouseButtonEventArgs e)
        {
            SettingsWindow s = new SettingsWindow();
            s.Show();
        }
         */

        private void pagerControl_CurrentPageIndexChanged(object sender, EventArgs e)
        {
            presenter.ShowArchiv(ucPager.CurrentPageIndex - 1);
        }

        private void tgbArchive_Click(object sender, RoutedEventArgs e)
        {
            ucPager.CurrentPageIndex = 1;
            PagerVisibility(Visibility.Visible);

            presenter.SetMode(UpdateMode.Archive);
        }

        private void PagerVisibility(Visibility visibility)
        {
            ucPager.Visibility = visibility;
        }

        private void ToggleButtons_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var tgb = (ToggleButton) e.OriginalSource;

            tgb.IsEnabled = false;

            IList<ToggleButton> tgbList = new List<ToggleButton>
                                              {
                                                  tgbArchive,
                                                  tgbDashboard,
                                                  tgbSecretary
                                              };

            foreach (ToggleButton button in tgbList)
            {
                if (button != tgb && button.IsChecked.Value)
                {
                    button.IsChecked = false;
                    button.IsEnabled = true;
                }
            }
        }

        private void tgbDashboard_Click(object sender, RoutedEventArgs e)
        {
            PagerVisibility(Visibility.Collapsed);

            presenter.SetMode(UpdateMode.Dashboard);
        }


        private void tgbSecretary_Click(object sender, RoutedEventArgs e)
        {
            PagerVisibility(Visibility.Collapsed);
            presenter.SetMode(UpdateMode.Secretary);
        }

        #region Implementation of IView

        public void WorkDone()
        {
            throw new NotImplementedException();
        }

        public event EventHandler<ActionsEventArgs> ChangeView;

        #endregion

        #region IStatusesView

        //todo: to może powinno być jako StatusViewModel

        private StatusViewModel mainStatus;

        public IList<StatusViewModel> Statuses
        {
            get { return lstbStatusList.ItemsSource as IList<StatusViewModel>; }
            set
            {
                //statusy będą ustawiane asynchronicznie przez prezetnera
                //więc potrzeba obiektu Dispatcher
                Dispatcher.Invoke(
                    new Action<IList<StatusViewModel>>(
                        delegate(IList<StatusViewModel> statusesCollection)
                            {
                                lstbStatusList.ItemsSource = statusesCollection;

                                if (statusesCollection != null)
                                {
                                    tbShowLoad.Visibility = Visibility.Collapsed;
                                }
                                else
                                {
                                    tbShowLoad.Visibility = Visibility.Visible;
                                }

                                //todo:to tak testowo
                                FlashMainWindow(Window.GetWindow(Parent), true);
                            }), value);
            }
        }

        public StatusViewModel MainStatus
        {
            get { return mainStatus; }
            set
            {
                //status będzie ustawiany asynchronicznie przez prezetnera
                //więc potrzeba obiektu Dispatcher
                Dispatcher.Invoke(new Action<StatusViewModel>(delegate(StatusViewModel status)
                                                                  {
                                                                      // lbUserLogin.Text = status.UserLogin;
                                                                      // lbContent.Text = status.Content;

                                                                      mainStatus = status;

                                                                      status.UserLogin = string.Empty;
                                                                      statusContent.BoundStatus = status;

                                                                      if (imgUserAvatar.Source == null)
                                                                      {
                                                                          var imgAvatar = new BitmapImage();
                                                                          imgAvatar.BeginInit();
                                                                          imgAvatar.UriSource =
                                                                              new Uri(status.UserAvatar50);
                                                                          imgAvatar.EndInit();
                                                                          imgUserAvatar.Source = imgAvatar;
                                                                      }
                                                                  }), value);
            }
        }

        public string TextMessage
        {
            get { return tbMessage.Text; }
            set
            {
                //tekst wiadomości ustawiany asynchronicznie przez prezetnera
                //więc potrzeba obiektu Dispatcher
                Dispatcher.Invoke(new Action<string>(delegate(string textMessage)
                                                         {
                                                             tbMessage.Text = textMessage;
                                                             tbMessage.IsEnabled = true;
                                                             tbShowSave.Visibility = Visibility.Hidden;

                                                             SetTextBoxFocus();
                                                         }), value);
            }
        }


        public Exception Error
        {
            get { throw new NotImplementedException(); }
            set
            {
                Dispatcher.Invoke(
                    new Action<Exception>(delegate(Exception err)
                                              {
                                                  tbError.Visibility = Visibility.Visible;
                                                  tbError.ToolTip = err.Message;
                                                  tbError.Tag = "Szczegóły błędu: " + Environment.NewLine +
                                                                err.StackTrace;
                                                  EnableContrlsForSendMessage(true);
                                              }), DispatcherPriority.Normal, value);
            }
        }

        public TitleMessageViewModel ConnectivityStatus
        {
            get { throw new NotImplementedException(); }
            set
            {
                Dispatcher.Invoke(
                    new Action<TitleMessageViewModel>(delegate(TitleMessageViewModel status)
                                                          {
                                                              //chowamy błędy 
                                                              tbError.Visibility = Visibility.Hidden;

                                                              tbOffline.Content = status.Title;
                                                              tbOffline.ToolTip = status.Message;

                                                              EnableContrlsForSendMessage(true);
                                                          }), value);
            }
        }

        public LatestVersionViewModel LatestVersion
        {
            get { throw new NotImplementedException(); }
            set
            {
                Dispatcher.Invoke(
                    new Action<LatestVersionViewModel>(delegate(LatestVersionViewModel latestVersion)
                                                           {
                                                               //chowamy błędy 
                                                               newVersion.Visibility = Visibility.Visible;

                                                               newVersionLink.NavigateUri = latestVersion.DownloadLink;
                                                               newVersionTextBox.ToolTip += " (" +
                                                                                            latestVersion.Version.
                                                                                                ToString() + ")";
                                                           }), value);
            }
        }

        public void UpdateStatuses(IList<StatusViewModel> statuses)
        {
            Dispatcher.Invoke(
                new Action<IList<StatusViewModel>>(
                    delegate(IList<StatusViewModel> statusesList)
                        {
                            // var currentList = lstbStatusList.ItemsSource as ObservableCollection<StatusViewModel>;
                            var currentList = lstbStatusList.ItemsSource as IList<StatusViewModel>;
                            for (int i = statusesList.Count - 1; i >= 0; i--)
                            {
                                currentList.Insert(0, statusesList[i]);
                            }

                            //for (int i =0 ; i <statusesList.Count; i++)
                            //{
                            //    currentList.Insert(0, statusesList[i]);
                            //}

                            //todo:to tak testowo
                            FlashMainWindow(Window.GetWindow(Parent), true);
                        }), statuses);
        }


        /// <summary>
        /// Dodaje status do listy w widoku
        /// </summary>
        /// <param name="statusView">status</param>
        /// <param name="insertAtBeginning">czy dodać na początek czy na koniec listy</param>
        public void AddStatus(StatusViewModel statusView, bool insertAtBeginning)
        {
            Dispatcher.Invoke(
                new Action<StatusViewModel>(
                    delegate(StatusViewModel status)
                        {
                            var currentList = lstbStatusList.ItemsSource as IList<StatusViewModel>;
                            //currentList.Insert(0, status);

                            //jeśli zawiera już ten status to 
                            //go nie dodajemy ponownie, czasami pobierze podwójnie 
                            //ten sam wpis
                            //if(currentList.Contains(status))
                            //{
                            //    return;
                            //}

                            if (currentList != null)
                            {
                                if (insertAtBeginning)
                                {
                                    currentList.Insert(0, status);
                                }
                                else
                                {
                                    currentList.Add(status);
                                }
                            }
                            //todo:testowo zmiana ikony w tray'u
                            Window parentWindows = Window.GetWindow(Parent);
                            if (parentWindows is HostWindow)
                            {
                                ((HostWindow) parentWindows).StatusAdded();
                            }

                            //todo:to tak testowo
                            FlashMainWindow(Window.GetWindow(Parent), true);
                        }), statusView);
        }

        public void ShowInfo(string message)
        {
            MessageBox.Show(message);
        }


        public string PicturePath
        {
            get { return pictureFilePath; }
            set
            {
                Dispatcher.Invoke(new Action<string>(delegate(string picturePath)
                                                         {
                                                             if (string.IsNullOrEmpty(picturePath))
                                                             {
                                                                 btnDeletePic.Visibility = Visibility.Collapsed;
                                                                 imgAttachPic.Source = null;
                                                                 pictureFilePath = string.Empty;
                                                             }
                                                             else
                                                             {
                                                                 pictureFilePath = picturePath;

                                                                 var iconUri = new Uri(picturePath,
                                                                                       UriKind.RelativeOrAbsolute);

                                                                 imgAttachPic.Source = BitmapFrame.Create(iconUri);
                                                                 btnDeletePic.Visibility = Visibility.Visible;
                                                                 btnDeletePic.ToolTip = string.Format("Usuń obraz {0}",
                                                                                                      picturePath);
                                                             }
                                                         }), value);
            }
        }

        #endregion

        #region handlery dla kommend

        /// <summary>
        /// Komenda wywoływana gdy naciśniemy cytuj
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CiteUser_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var status = (StatusViewModel) e.Parameter;
            int position = tbMessage.SelectionStart;

            int currentPosition = presenter.MakeCitation(status, tbMessage.Text, position);

            if (tbMessage.Text.Length > 0)
            {
                tbMessage.Select(currentPosition, 0);
            }
            tbMessage.Focus();
        }


        /// <summary>
        /// Komenda wywoływana gdy naciśniemy przycisk Odpowiedz przy statusie
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DirectMessage_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var status = (StatusViewModel) e.Parameter;


            presenter.MakeDirectMessage(status, tbMessage.Text);

            SetTextBoxFocus();
        }


        /// <summary>
        /// Komenda wywoływana gdy naciśniemy przycisk Prywatna przy statusie,
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PrivateMessage_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var status = (StatusViewModel) e.Parameter;


            presenter.MakePrivateMessage(status, tbMessage.Text);

            SetTextBoxFocus();
        }

        private void ShowPicture_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var w = new BigPictureWindow();


            w.PictureSource = ((Image) e.Parameter).Source;
            w.PictureUrl = (string) ((Image) e.Parameter).Tag;
            w.Show();
        }

        private void ShowVideo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var videoUrl = (string) e.Parameter;

            var vw = new VideoViewWindow(videoUrl);

            vw.Show();
        }

        private void Navigate_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //Hyperlink hl = (Hyperlink)sender;
            //string navigateUri = hl.NavigateUri.ToString();

            var navigateUri = (string) e.Parameter;
            Process.Start(new ProcessStartInfo(navigateUri));
            e.Handled = true;
        }

        #endregion

        #region metody prywatne

        /// <summary>
        /// Pomocnicza metoda zawierająca w sobie logikę widoku
        /// przy dodawaniu, wiadomości
        /// true - oznacza że można pokazać i aktywować poszczególene części widoku
        /// zaangażowane w wizualizację wysyłania widomości
        /// </summary>
        /// <param name="show"></param>
        private void EnableContrlsForSendMessage(bool enable)
        {
            tbShowSave.Visibility = enable ? Visibility.Hidden : Visibility.Visible;
            tbMessage.IsEnabled = enable;
            btnSendBlip.IsEnabled = enable;

            //SetTextBoxFocus();
        }

        private void SetTextBoxFocus()
        {
            if (tbMessage.Text.Length > 0)
            {
                tbMessage.Select(tbMessage.Text.Length, 0);
            }
            tbMessage.Focus();
        }

        #endregion
    }
}