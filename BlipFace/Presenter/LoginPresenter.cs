using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlipFace.Model;
using BlipFace.View;
using BlipFace.Service.Communication;
using System.IO;

namespace BlipFace.Presenter
{
    public class LoginPresenter : IPresenter
    {
        private ILoginView _view;

        private string fileWithUserPassword= "BlipPass.bup";

        public void ValidateCredential(string user, string password)
        {
            //tak na dobra sprawe to powinno tu byc odwolanie do logiki

            BlipCommunication com = new BlipCommunication(user, password);

            com.AuthorizationComplete += new BlipCommunication.BoolDelegate(com_AuthorizationComplete);

            com.ValideteAsync();

            //if (com.Validate())
            //{

            //    _view.Error = "";

            //    WorkDone(this, new ActionsEventArgs(Actions.Statuses,
            //        new UserViewModel() { UserName = this._view.UserName, Password = this._view.Password }));

            //}
            //else
            //{


            //    _view.Error = "Logowanie nieudane";
            //}
        }


        private void com_AuthorizationComplete(bool value)
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
        }

        #region Implementation of IPresenter

        public void SetView(IView view)
        {
            if (view is ILoginView)
            {
                _view = (ILoginView) view;
            }
            else
            {
                string message =
                    string.Format(@"Przekazano nieodpowiedni widok, oczekiwano widoku typu {0} a podano {1} ",
                                  typeof (ILoginView), view.GetType().ToString());
                throw new ArgumentException(message);
            }
        }

        public void Init()
        {

            string line;
            if (File.Exists(fileWithUserPassword))
            {
                using(StreamReader sr = new StreamReader(fileWithUserPassword))
                {
                    line = sr.ReadLine();
                }
                string[] usrAndPass=line.Split(new[]{':'});


                _view.UserName = usrAndPass[0];
                _view.Password = usrAndPass[1];
            }
        }

        public event EventHandler<ActionsEventArgs> WorkDone;

        #endregion

        public void AuthorizationOK(bool remember)
        {
            string usr = _view.UserName;
            string pas = _view.Password;


            if (remember)
            {

                //zawsze tworzymy i nadpisujemy plik
                using (FileStream fs = new FileStream(fileWithUserPassword, FileMode.Create))
                {
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        sw.Write(usr);
                        sw.Write(":");
                        sw.Write(pas);
                        
                        
                        sw.Close();
                    }
                }

            }


            WorkDone(this, new ActionsEventArgs(Actions.Statuses,
                                                new UserViewModel()
                                                    {UserName = usr, Password = pas}));
        }
    }
}