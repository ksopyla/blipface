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
using BlipFace.Model;
using BlipFace.Presenter;

namespace BlipFace.View
{
    public partial class LoginViewControl :ILoginView
    {

        readonly LoginPresenter _presenter;
        

        #region ILoginView propoerties
        public string UserName
        {
            get
            {
                string usr = string.Empty;

                Dispatcher.Invoke(new Action(delegate()
                {
                    usr = tbUserName.Text;
                }), System.Windows.Threading.DispatcherPriority.Normal);

                return usr;

                
            }
            set { tbUserName.Text = value; }
        }

        public string Password
        {
            get
            {
                string pass = string.Empty;

                Dispatcher.Invoke(new Action(delegate()
                {
                    pass = pswPassword.Password;
                }), System.Windows.Threading.DispatcherPriority.Normal);

                return pass;
            }
            set { pswPassword.Password = value; }
        }

        private bool authorize;

        public bool RememberCredencial
        {
            get
            {
                //return chbRememberPassword.IsChecked.HasValue
                //                               ? chbRememberPassword.IsChecked.Value
                //                               : false;
                bool rem=false;

                Dispatcher.Invoke(new Action(delegate()
                {
                  rem=    chbRememberPassword.IsChecked.HasValue
                                               ? chbRememberPassword.IsChecked.Value
                                               : false;
                }), System.Windows.Threading.DispatcherPriority.Normal);

                return rem;
            }
            set
            {
                Dispatcher.Invoke(new Action<bool>(delegate(bool remember)
                                                       {
                                                           chbRememberPassword.IsChecked = remember;
                                                       })
                                  , System.Windows.Threading.DispatcherPriority.Normal, value);
            }
        }

            /// <summary>
        /// Czy poprwanie się zautoryzowano
        /// </summary>
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

                        //jeśli poprawnie się zautoryzowno w blipie
                        //if(auth)
                        //{
                        //    //czy należy zapamiętać 
                        //    bool remember = chbRememberPassword.IsChecked.HasValue
                        //                        ? chbRememberPassword.IsChecked.Value
                        //                        : false;
                        //    //powiadamiamy prezentera że nastąpiła poprawna autoryzacja
                        //    //widok wyświetlił wszystko co miał
                        //    //i więc teraz należy zachować albo nie dane do logowania
                        //    //w zależności co użytkownik zaznaczył w checkBoxie
                        //    _presenter.AuthorizationDone(remember);
                        //}
                        
                    }), System.Windows.Threading.DispatcherPriority.Normal,value);
            }
        }

        public string Error
        {
            get
            {
                return (string) tblError.Text;
            }
            set
            {
                

                Dispatcher.Invoke(
                    new Action<string>(delegate(string _err)
                    {
                       tblError.Visibility = System.Windows.Visibility.Visible;
                        tblError.Text = _err;
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

        #region Implementation of IView

        public void WorkDone()
        {
            Dispatcher.Invoke(new Action(delegate()
            {
                UserViewModel usr = new UserViewModel() {UserName = this.UserName, Password = this.Password};
                ChangeView(this,new ActionsEventArgs(Actions.Statuses,usr));
            }),System.Windows.Threading.DispatcherPriority.Normal);
        }

        public event EventHandler<ActionsEventArgs> ChangeView;

        #endregion
    }
}