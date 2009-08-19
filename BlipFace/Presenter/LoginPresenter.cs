using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Security;
using System.Text;
using BlipFace.Model;
using BlipFace.View;
using BlipFace.Service.Communication;
using System.IO;
using BlipFace.Helpers;
using System.Threading;

namespace BlipFace.Presenter
{
    public class LoginPresenter : IPresenter
    {
        private ILoginView _view;

        private const string FileWithUserPassword = "BlipPass.bup";


        readonly BlipCommunication com = new BlipCommunication();

        public LoginPresenter()
        {

            com.AuthorizationComplete += new BlipCommunication.BoolDelegate(ComAuthorizationComplete);
            com.CommunicationError += new EventHandler<CommunicationErrorEventArgs>(ComCantCommunicate);
            com.ExceptionOccure += new EventHandler<ExceptionEventArgs>(ComExceptionOccure);


        }

        public void ValidateCredential(string user, string password)
        {
            //tak na dobra sprawe to powinno tu byc odwolanie do logiki

            com.SetAuthorizationCredential(user, password);

            com.ValideteAsync();

           
        }

        void ComCantCommunicate(object sender, CommunicationErrorEventArgs e)
        {
            _view.Authorize = false;

            _view.Error = "Nie mogę się połączyć z blipem, HttpCode="+e.Message;
        }


        /// <summary>
        /// Callback gdy wystąpi błąd podczas logowania
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ComExceptionOccure(object sender, ExceptionEventArgs e)
        {
            _view.Authorize = false;

            //przekazujemy wyjątek dalej

            string msg = e.Error.Message;

            if(string.IsNullOrEmpty(msg))
            {
                msg = "Bład połączenia";
            }
            _view.Error = msg;
            //throw e.Error;

        }

        /// <summary>
        /// Callback po wykonaniu autoryzacji
        /// </summary>
        /// <param name="value"></param>
        private void ComAuthorizationComplete(bool value)
        {
            if (value)
            {
                _view.Error = "";
            }
            else
            {
                _view.Error = "Logowanie nieudane";
            }
            _view.Authorize = value;

            //jeśli true to znaczy że dało się zalogować zapisz 
            //hasło
            if (value)
            {
                SaveCredenctial(_view.RememberCredencial);

                //wysłanie informacji o użyciu BlipFace
                BlipFaceNotifyUse notifyUse = new BlipFaceNotifyUse();
                notifyUse.Notyfi();

                _view.WorkDone();
               
            }
        }

        #region Implementation of IPresenter

        public void SetView(IView view)
        {
            if (view is ILoginView)
            {
                _view = (ILoginView)view;
            }
            else
            {
                string message =
                    string.Format(@"Przekazano nieodpowiedni widok, oczekiwano widoku typu {0} a podano {1} ",
                                  typeof(ILoginView), view.GetType().ToString());
                throw new ArgumentException(message);
            }
        }

        public void Init()
        {
            using (IsolatedStorageAccess isoAccess = new IsolatedStorageAccess(FileWithUserPassword))
            {

                string[] credenctial = isoAccess.ReadAll();

                if (credenctial != null && credenctial.Length>1)
                {
                    //odczytujemy w pierwszej lini login
                    var user = credenctial[0].Decrypt().ToInsecureString();
                    //odczytujemy w durgiej lini hasło
                    var pass = credenctial[1].Decrypt().ToInsecureString();

                    _view.UserName = user;
                    _view.Password = pass;

                    _view.RememberCredencial = true;
                }
            }

            #region stara implementacja
            //IsolatedStorageFile isoStore =
            //        IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);

            ////pobieramy nazwy plików do tablicy, powinien być maksymalnie 1
            //string[] fileNames = isoStore.GetFileNames(FileWithUserPassword);
            //foreach (string file in fileNames)
            //{
            //    //metoda GetFileNames zwraca listę plików pasującą do wzorca, 
            //    //powinien być tylko 1, lecz na wszelki wypadek stosujemy zabezpieczenie aby
            //    //nic nam niepotrzbnym wyjątkiem nie rzuciło
            //    if (file == FileWithUserPassword)
            //    {
            //        using (
            //            StreamReader sr =
            //                new StreamReader(new IsolatedStorageFileStream(FileWithUserPassword, FileMode.Open, isoStore))
            //            )
            //        {
            //            //odczytujemy w pierwszej lini login
            //            var user = sr.ReadLine().Decrypt().ToInsecureString();
            //            //odczytujemy w durgiej lini hasło
            //            var pass = sr.ReadLine().Decrypt().ToInsecureString();

            //            _view.UserName = user;
            //            _view.Password = pass;
            //        }
            //    }

            //}//end foreach
            #endregion

            //w celu aby wyglądało że się szybciej loguje,
            com.ConnectAsync();



        }

        public void Close()
        {
            
            //nic nie robi
        }

        #endregion


        /// <summary>
        /// Metoda wywoływana gdy autorycajca zakończy się sukcesem
        /// </summary>
        /// <param name="remember"></param>
        public void SaveCredenctial(bool remember)
        {
            SecureString usr = _view.UserName.ToSecureString();
            SecureString pas = _view.Password.ToSecureString();

            using (IsolatedStorageAccess isoAccess = new IsolatedStorageAccess(FileWithUserPassword))
            {

                if (remember)
                {
                    string[] credenctial = new[] {usr.Encrypt(), pas.Encrypt()};

                    isoAccess.WriteStrings(credenctial);

                    #region stara implementacja

                    ////gdzie są przychowywane foldery można przeczytać
                    ////http://msdn.microsoft.com/en-us/library/3ak841sy(VS.80).aspx
                    ////u mnie na viście jest to folder
                    ////C:\Users\ksirg\AppData\Local\VirtualStore\Program Files\BlipFace
                    ////oraz C:\Users\ksirg\AppData\Local\IsolatedStorage\ plus dziwne nazwy folderów
                    //IsolatedStorageFile isoStore =
                    //    IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);

                    ////towrzymy główny folder do przechowywania
                    ////isoStore.CreateDirectory("blipFace");

                    ////tworzymy plik w którym bedzie przechowywane  zaszyfrowane hasło i login
                    //IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream(FileWithUserPassword,
                    //                                                                    FileMode.Create, isoStore);

                    ////zawsze tworzymy i nadpisujemy plik
                    ////w pierwszej lini login a w drugiej hasło
                    //using (StreamWriter sw = new StreamWriter(isoStream))
                    //{
                    //    //zapisujem login
                    //    sw.Write(usr.Encrypt());

                    //    //nowa linia
                    //    sw.Write(Environment.NewLine);

                    //    //zapisujemy hasło
                    //    sw.Write(pas.Encrypt());

                    //    sw.Close();
                    //}

                    #endregion
                }
                else
                {
                    isoAccess.DeleteFile();
                }


            }

        }
    }
}