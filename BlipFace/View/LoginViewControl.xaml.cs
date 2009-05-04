using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using BlipFace.Presenter;

namespace BlipFace.View
{
    public partial class LoginViewControl :ILoginView
    {

        readonly LoginPresenter _presenter;
        

        #region ILoginView propoerties
        public string UserName
        {
            get { return tbUserName.Text; }
            set { tbUserName.Text = value; }
        }

        public string Password
        {
            get { return pswPassword.Password; }
            set { pswPassword.Password = value; }
        }

        private bool authorize;
        public  bool Authorize
        {
            get
            {
                return authorize;
            }
            set
            {
                Dispatcher.Invoke(
                    new Action<bool>(delegate(bool auth)
                    {
                        lblLoginAnimation.Visibility = System.Windows.Visibility.Collapsed;
                        Storyboard sbdHideLoading = (Storyboard)FindResource("AnimatedDotLabel");
                        sbdHideLoading.Stop();
                        authorize = auth;

                        if(auth)
                        {
                            bool remember = chbRememberPassword.IsChecked.HasValue
                                                ? chbRememberPassword.IsChecked.Value
                                                : false;
                            _presenter.AuthorizationDone(remember);
                        }
                        
                    }), System.Windows.Threading.DispatcherPriority.Normal,value);
            }
        }

        public string Error
        {
            get
            {
                return (string)lblError.Content;
            }
            set
            {
                

                Dispatcher.Invoke(
                    new Action<string>(delegate(string _err)
                    {
                       lblError.Visibility = System.Windows.Visibility.Visible;
                        lblError.Content = _err;
                    }), System.Windows.Threading.DispatcherPriority.Normal,value);
            }
        }
        #endregion

        public LoginViewControl(LoginPresenter presenter)
        {
            InitializeComponent();

            // Insert code required on object creation below this point.
            _presenter = presenter;


        }

        public void ValidateCredential()
        {
            _presenter.ValidateCredential(UserName, Password);
        }

      

       

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            ShowLoading();
            ValidateCredential();
        }

        private void ShowLoading()
        {

            Dispatcher.Invoke(
                    new Action(delegate
                                   {

                                       lblLoginAnimation.Visibility = Visibility.Visible;
                                       Storyboard sbdHideLoading = (Storyboard) FindResource("AnimatedDotLabel");
                                       sbdHideLoading.Begin();

                                       //lblLoginAnimation.
                                       //lblError.Content = _err;
                                   }), System.Windows.Threading.DispatcherPriority.Normal);

        }

        private void UserControl_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                //gdy naciśnięto enter to wysyłamy tekst
                ValidateCredential();
             
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            tbUserName.Focus();
        }

       
    }
}