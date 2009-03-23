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
using System.Windows.Navigation;
using System.Windows.Shapes;
using BlipFace.Service.Communication;
using BlipFace.Service.Entities;
using BlipFace.View;
using BlipFace.Presenter;
using BlipFace.Model;

namespace BlipFace.View
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MainView : Window, IMainView
    {

        const int BlipSize = 160;
        int charLeft=BlipSize;

        private MainPresenter preseneter;

        public MainView()
        {
            InitializeComponent();

            preseneter = new MainPresenter(this);
        }



        private void tbMessage_TextChanged(object sender, TextChangedEventArgs e)
        {
            charLeft = BlipSize - tbMessage.Text.Length;
            if(charLeft<0)
            {
                tblCharLeft.Foreground = new SolidColorBrush(Color.FromRgb(200,100,100));
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

       
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
           
            // preseneter.LoadStatuses();

            preseneter.LoadUserDashboard("blipface");
            
        }

       



        #region IMainView Members

        public IEnumerable<StatusViewModel> Statuses
        {
            get
            {
                return lstbStatusList.ItemsSource as IEnumerable<StatusViewModel>;
            }
            set
            {

                //statusy będą ustawiane asynchronicznie przez prezetnera
                //więc potrzeba obiektu Dispatcher
                Dispatcher.Invoke(new Action<IEnumerable<StatusViewModel>>(delegate(IEnumerable<StatusViewModel> statusesCollection)
                {
                    lstbStatusList.ItemsSource = statusesCollection;

                }), value);
            }
        }

        //todo: to może powinno być jako StatusViewModel
        public BlipStatus MainStatus
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        private void btnSendBlip_Click(object sender, RoutedEventArgs e)
        {
            preseneter.AddStatus(tbMessage.Text);
        }
    }
}
