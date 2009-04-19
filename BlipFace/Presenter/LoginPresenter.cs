using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlipFace.Model;
using BlipFace.View;
using BlipFace.Service.Communication;

namespace BlipFace.Presenter
{
    public class LoginPresenter : IPresenter
    {
        private ViewsManager _viewsManager;
        ILoginView _view;
        public LoginPresenter(ViewsManager manager)
        {
            _viewsManager = manager;
            
        }

        public void ValidateCredential(string user, string password)
        {
            //tak na dobra sprawe to powinno tu byc odwolanie do logiki

            BlipCommunication com = new BlipCommunication(user,password);
            if (com.Validate())
            {
                
                _view.Error = "";

                WorkDone(this, new ActionsEventArgs(Actions.Statuses,
                    new UserViewModel() { UserName = this._view.UserName, Password = this._view.Password }));

            }
            else
            {



                _view.Error = "Logowanie nieudane";
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
                    string.Format(@"Przekazano nieodpowiedni widok, oczekiwano widoku typu {0} a podano {1} ", typeof(ILoginView), view.GetType().ToString());
                throw new ArgumentException(message);
            }
        }

        public void Init()
        {
            throw new NotImplementedException();
        }

        public event EventHandler<ActionsEventArgs> WorkDone;

        #endregion
    }
}
