using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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

namespace BlipFace
{
    /// <summary>
    /// Interaction logic for HostWindow.xaml
    /// </summary>
    public partial class HostWindow : Window, IHostView
    {
        private ViewsManager mgr;
        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Drawing.Icon normalNotifyIcon;
        private System.Drawing.Icon statusAddedNotifyIcon;
        private BlipFaceWindowsState currentState;

        private bool showBallon = true;

        public HostWindow()
        {
            InitializeComponent();

            //położenie okna 
            this.Left = System.Windows.SystemParameters.PrimaryScreenWidth - this.Width - 20;


            //ikona dla aplikacji, pozakuje się na pasku
            //blipFace_logo_round.png"
            Uri iconUri = new Uri("pack://application:,,,/Resource/Img/blipFace.ico",
                                  UriKind.RelativeOrAbsolute);
            this.Icon = BitmapFrame.Create(iconUri);


            //ikona w sys tray'u po zminimalizowaniu
            notifyIcon = new System.Windows.Forms.NotifyIcon
                             {
                                 BalloonTipText =
                                     "BlipFace został zminimzlizowany, jeżeli chcesz go zobaczyć jeszcze raz kliknij na ikonę.",
                                 BalloonTipTitle = "BlipFace",
                                 Text = "Kliknij aby pokazał się BlipFace"
                             };

            normalNotifyIcon = IconFromResource(iconUri.ToString());

            statusAddedNotifyIcon = IconFromResource("pack://application:,,,/Resource/Img/blipFaceAddStatus.ico");

            notifyIcon.Click += new EventHandler(NotifyIconClick);

            //ustawienie ikony w tray'u kiedy jest ustawiona opcja aby była ona tam ciągle
            if (Properties.Settings.Default.AlwaysInTray)
                ChangeIconInTray(IconInTrayState.Normal);

            mgr = new ViewsManager(this);

            //gdy zmienią się ustawienia aplikacji trzeba ustawić odpowiednie elementy okna
            //todo:trzeba pomyśleć jak to zrobić inaczej
            Properties.Settings.Default.PropertyChanged += new PropertyChangedEventHandler(Default_PropertyChanged);

            currentState = BlipFaceWindowsState.Normal;
        }

        void Default_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "AlwaysInTray")
            {
                if (Properties.Settings.Default.AlwaysInTray)
                    ChangeIconInTray(IconInTrayState.Normal);
                else
                    ChangeIconInTray(IconInTrayState.None);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            mgr.Run();
        }

        private void btnCloseApp_Click(object sender, RoutedEventArgs e)
        {

            mgr.Close();
            this.Close();

        }

        private void NonRectangularWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();

        }

        #region IHost Members

        public void AttachView(UserControl view)
        {
            //view.Height = PlaceHolder.ActualHeight;
            Dispatcher.Invoke(
                new Action<UserControl>(delegate(UserControl control) { PlaceHolder.Children.Add(control); })
                , System.Windows.Threading.DispatcherPriority.Normal, view);
        }

        public void SwitchView(UserControl view)
        {
            Dispatcher.Invoke(
                new Action<UserControl>(delegate(UserControl control)
                                            {
                                                if (PlaceHolder.Children.Count > 0)
                                                {
                                                    UIElement element = PlaceHolder.Children[0];

                                                    if (element != null)
                                                    {
                                                        element.Visibility = Visibility.Collapsed;
                                                        PlaceHolder.Children.RemoveAt(0);
                                                    }
                                                }

                                                //    view.Height = PlaceHolder.ActualHeight;

                                                PlaceHolder.Children.Add(view);
                                            })
                , System.Windows.Threading.DispatcherPriority.Normal, view);
        }

        #endregion

        private void btnMinimalizeApp_Click(object sender, RoutedEventArgs e)
        {
            MinimalizeBlipFaceWindows();
        }

        #region IHost Members

        #endregion


        //implementacja howania okna zaczerpnięta z 
        //http://possemeeg.wordpress.com/2007/09/06/minimize-to-tray-icon-in-wpf/
        #region chowanie okna

        void NotifyIconClick(object sender, EventArgs e)
        {
            if (currentState == BlipFaceWindowsState.InTray || currentState == BlipFaceWindowsState.MinimalizeAndInTray)
            {
                ToNormalBlipFaceWindows();
            }
        }

        /// <summary>
        /// wywoływana przed zamknęciem
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HostWindow_OnClosing(object sender, CancelEventArgs e)
        {
            //upewniamy się że ikona zostanie usunięta
            notifyIcon.Dispose();
            notifyIcon = null;
        }

        /// <summary>
        /// Czas pokazywania podpowiedzi po zminimalizowaniu aplikacji
        /// </summary>
        private const int BallonTipTime = 1;

        /// <summary>
        /// Gdy zmieni się stan okna np. z normalnego do minmalizowanego
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HostWindow_OnStateChanged(object sender, EventArgs e)
        {
            if (currentState == BlipFaceWindowsState.Normal)
            {
                MinimalizeBlipFaceWindows();
            }
            else
            {
                ToNormalBlipFaceWindows();
            }
        }

        /// <summary>
        /// Logika minimalizowania BlipFace
        /// </summary>
        private void MinimalizeBlipFaceWindows()
        {
            if (Properties.Settings.Default.MinimalizeToTray)
            {
                this.Hide();
                if (notifyIcon != null)
                {
                    ChangeIconInTray(IconInTrayState.Normal);
                    if (showBallon)
                    {
                        notifyIcon.ShowBalloonTip(BallonTipTime);
                        showBallon = false;
                    }
                }
                currentState = BlipFaceWindowsState.InTray;
            }
            else
            {
                WindowState = WindowState.Minimized;
                currentState = BlipFaceWindowsState.Minimalize;
                if (Properties.Settings.Default.AlwaysInTray)
                    currentState = BlipFaceWindowsState.MinimalizeAndInTray;
            }
        }

        /// <summary>
        /// Logika przywracania BlipFace do normalnego wyglądu
        /// </summary>
        private void ToNormalBlipFaceWindows()
        {
            if (!IsVisible)
                Show();
            WindowState = WindowState.Normal;
            currentState = BlipFaceWindowsState.Normal;

            if (Properties.Settings.Default.AlwaysInTray)
            {
                ChangeIconInTray(IconInTrayState.Normal);
            }
            else
            {
                ChangeIconInTray(IconInTrayState.None);
            }
        }

        #endregion

        public void StatusAdded()
        {
            if (notifyIcon != null && notifyIcon.Visible)
            {
                ChangeIconInTray(IconInTrayState.NewStatus);

                System.Media.SystemSound sound = System.Media.SystemSounds.Beep;
                sound.Play();
            }
        }

        private System.Drawing.Icon IconFromResource(string path)
        {
            Uri iconUri = new Uri(path, UriKind.RelativeOrAbsolute);
            Stream iconStream = Application.GetResourceStream(iconUri).Stream;
            return new System.Drawing.Icon(iconStream);
        }

        private IconInTrayState currentIconState = IconInTrayState.None;

        private void ChangeIconInTray(IconInTrayState state)
        {
            if (currentIconState == IconInTrayState.None && state != IconInTrayState.None)
            {
                notifyIcon.Visible = true;
            }
            else if (state == IconInTrayState.None)
            {
                notifyIcon.Visible = false;
                currentIconState = IconInTrayState.None;
            }

            if (currentIconState != IconInTrayState.Normal && state == IconInTrayState.Normal)
            {
                notifyIcon.Icon = normalNotifyIcon;
                currentIconState = IconInTrayState.Normal;
            }

            if (currentIconState != IconInTrayState.NewStatus && state == IconInTrayState.NewStatus)
            {
                notifyIcon.Icon = statusAddedNotifyIcon;
                currentIconState = IconInTrayState.NewStatus;
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (currentIconState == IconInTrayState.NewStatus)
                ChangeIconInTray(IconInTrayState.Normal);
        }


    }

    enum BlipFaceWindowsState
    {
        Normal,
        Minimalize,
        InTray,
        MinimalizeAndInTray
    }

    enum IconInTrayState
    {
        None,
        Normal,
        NewStatus
    }
}