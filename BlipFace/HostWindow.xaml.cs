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

            Stream iconStream = Application.GetResourceStream(iconUri).Stream;
            notifyIcon.Icon = new System.Drawing.Icon(iconStream);
            
            
            notifyIcon.Click += new EventHandler(NotifyIconClick);


            mgr = new ViewsManager(this);
        }

        


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
            mgr.Run();
        }

        private void btnCloseApp_Click(object sender, RoutedEventArgs e)
        {
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
           // WindowState = System.Windows.WindowState.Minimized;

            this.Hide();
            if (notifyIcon != null)
                notifyIcon.ShowBalloonTip(2000);
        }

        #region IHost Members

        #endregion


        //implementacja howania okna zaczerpnięta z 
        //http://possemeeg.wordpress.com/2007/09/06/minimize-to-tray-icon-in-wpf/
        #region chowanie okna 

        void NotifyIconClick(object sender, EventArgs e)
        {
            
            Show();
        //if(WindowState== System.Windows.WindowState.Maximized)
        //{
        //    this.WindowState = System.Windows.WindowState.Normal;
        //}
        //else
             this.WindowState = storedWindowState;
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

        private WindowState storedWindowState = WindowState.Normal;

        /// <summary>
        /// Gdy zmieni się stan okna np. z normalnego do minmalizowanego
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HostWindow_OnStateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                this.Hide();
                if (notifyIcon != null)
                    notifyIcon.ShowBalloonTip(2000);
            }
            else
                storedWindowState = WindowState.Normal;
                    //WindowState;
        }

        /// <summary>
        /// Kiedy zmieni się widoczność okna
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HostWindow_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            CheckTrayIcon();

        }

        private void CheckTrayIcon()
        {

            ShowTrayIcon(!IsVisible);
        }

        private void ShowTrayIcon(bool show)
        {

            if (notifyIcon != null)
                notifyIcon.Visible = show;
        }

        #endregion
    }
}