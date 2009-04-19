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
        public string Error
        {
            get
            {
                return (string)lblError.Content;
            }
            set
            {
                lblError.Content = value;
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
            ValidateCredential();
        }

        private void UserControl_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                //gdy naciśnięto enter to wysyłamy tekst
                ValidateCredential();
             
            }
        }

       
    }
}