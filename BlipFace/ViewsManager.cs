using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using BlipFace.Model;
using BlipFace.Presenter;
using BlipFace.View;

namespace BlipFace
{
    public class ViewsManager
    {
        readonly IHostView _hostWindow;



        public ViewsManager(IHostView host)
        {
            _hostWindow = host;
        }



        public void ShowView(UserControl view)
        {

            _hostWindow.AttachView(view);

        }



        public void Run()
        {
            //pokazuje domyślny widok
            //tworzymy nowego prezentera
            var loginPresenter = new LoginPresenter(this);
            //dołączamy do niego widok, jednocześnie przkazując mu referencję 
            var loginView = new LoginViewControl(loginPresenter);
            loginPresenter.SetView(loginView);
            loginPresenter.WorkDone += new EventHandler<ActionsEventArgs>(PresenterWorkDone);
            _hostWindow.AttachView(loginView);
        }

        void PresenterWorkDone(object sender, ActionsEventArgs e)
        {
            switch (e.NextAction)
            {
                case Actions.Login:
                    CreateLoginPresenter();
                    break;
                case Actions.Statuses:

                    UserViewModel usr = e.Data as UserViewModel;

                    if (usr != null)
                        CreateStatusesPresenter(usr);
                    else
                    {
                        throw new ArgumentNullException();
                    }
                    break;
                case Actions.Configuration:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

        }



        private void CreateStatusesPresenter(UserViewModel user)
        {
            var statusPresenter = new StatusesPresenter(user);
            //dołączamy do niego widok, jednocześnie przkazując mu referencję 
            var statusView = new StatusListControl(statusPresenter);
            statusPresenter.SetView(statusView);
            statusPresenter.WorkDone += new EventHandler<ActionsEventArgs>(PresenterWorkDone);

            statusPresenter.Init();

            _hostWindow.SwitchView(statusView);
        }

        private void CreateLoginPresenter()
        {
            var loginPresenter = new LoginPresenter(this);
            //dołączamy do niego widok, jednocześnie przkazując mu referencję 
            var loginView = new LoginViewControl(loginPresenter);
            loginPresenter.SetView(loginView);
            loginPresenter.WorkDone += new EventHandler<ActionsEventArgs>(PresenterWorkDone);
            _hostWindow.SwitchView(loginView);
        }
    }
}
