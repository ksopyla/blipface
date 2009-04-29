using System;
using System.Collections.Generic;
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


        public HostWindow()
        {
            InitializeComponent();
            this.Left = System.Windows.SystemParameters.PrimaryScreenWidth - this.Width - 20;
            mgr = new ViewsManager(this);
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Uri iconUri = new Uri("pack://application:,,,/Resource/Img/blipFace_logo_round.png",
                                  UriKind.RelativeOrAbsolute);
            this.Icon = BitmapFrame.Create(iconUri);
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

        #region IHost Members

        #endregion
    }
}