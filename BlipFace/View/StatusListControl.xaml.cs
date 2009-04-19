﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using BlipFace.Model;
using BlipFace.Presenter;

namespace BlipFace.View
{
    public partial class StatusListControl : IStatusesView
    {
        private const int BlipSize = 160;
        int charLeft = BlipSize;

        private StatusesPresenter presenter;
        
        public StatusListControl()
        {
            this.InitializeComponent();
        }

        public StatusListControl(StatusesPresenter _presenter)
        {
            this.InitializeComponent();

            // Insert code required on object creation below this point.

            presenter = _presenter;
        }
/// <summary>
        /// Służy tylko do wyliczania ilości znaków pozostałych do wpisania
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tbMessage_TextChanged(object sender, TextChangedEventArgs e)
        {
            charLeft = BlipSize - tbMessage.Text.Length;
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
            if (e.Key == Key.Return)
            {
                //gdy naciśnięto enter to wysyłamy tekst

                EnableContrlsForSendMessage(false);
                
                presenter.AddStatus(tbMessage.Text);
            }
        }

        /// <summary>
        /// Handler dla kliknięca przycisku wysyłania tekstu dla blipa
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSendBlip_Click(object sender, RoutedEventArgs e)
        {
            EnableContrlsForSendMessage(false);

            presenter.AddStatus(tbMessage.Text);
        }


       
        public IList<StatusViewModel> Statuses
        {
            get
            {
                return lstbStatusList.ItemsSource as IList<StatusViewModel>;
            }
            set
            {

                //statusy będą ustawiane asynchronicznie przez prezetnera
                //więc potrzeba obiektu Dispatcher
                Dispatcher.Invoke(new Action<IList<StatusViewModel>>(delegate(IList<StatusViewModel> statusesCollection)
                {
                    lstbStatusList.ItemsSource = statusesCollection;

                }), value);
            }
        }

        //todo: to może powinno być jako StatusViewModel
        public StatusViewModel MainStatus
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

        public string TextMessage
        {
            get
            {
                return tbMessage.Text;
            }
            set
            {

                //tekst wiadomości ustawiany asynchronicznie przez prezetnera
                //więc potrzeba obiektu Dispatcher
                Dispatcher.Invoke(new Action<string>(delegate(string textMessage)
                {
                    tbMessage.Text = textMessage;
                    tbMessage.IsEnabled = true;
                    lbShowSave.Visibility = Visibility.Hidden;
                }), value);
            }
        }





        public Exception Error
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                //Application.Current.Dispatcher.Invoke(DispatcherPriority.Send,
                //    (DispatcherOperationCallback)delegate(object arg)
                //    {

                //        Exception ex = (Exception)arg;
                //        //throw new Exception(ex.Message,ex);
                //        MessageBox.Show(ex.Message);
                //    }
                //    , value);

                Dispatcher.Invoke(
                    new Action<Exception>(delegate(Exception _err)
                    {
                        //throw new Exception(_err.Message, _err);
                        MessageBox.Show(_err.Message);

                        EnableContrlsForSendMessage(true);

                    }), System.Windows.Threading.DispatcherPriority.Normal, value);
            }
        }


        /// <summary>
        /// Pomocnicza metoda zawierająca w sobie logikę widoku
        /// przy dodawaniu, wiadomości
        /// true - oznacza że można pokazać i aktywować poszczególene części widoku
        /// zaangażowane w wizualizację wysyłania widomości
        /// </summary>
        /// <param name="show"></param>
        private void EnableContrlsForSendMessage(bool enable)
        {

            lbShowSave.Visibility = enable ? Visibility.Hidden : Visibility.Visible;
            tbMessage.IsEnabled = enable;
            btnSendBlip.IsEnabled = enable;
        }

        
    }
}