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
        private readonly IHostView _hostWindow;


        public ViewsManager(IHostView host)
        {
            _hostWindow = host;
        }

        private IPresenter currentPresenter;

        public void ShowView(UserControl view)
        {
            _hostWindow.AttachView(view);
        }


        public void Run()
        {
            //pokazuje domyślny widok
            //tworzymy nowego prezentera
            var loginPresenter = new LoginPresenter();

            //obecny prezenter
            currentPresenter = loginPresenter;

            //dołączamy do niego widok, jednocześnie przkazując mu referencję 
            var loginView = new LoginViewControl(loginPresenter);
            loginPresenter.SetView(loginView);

            loginPresenter.WorkDone += new EventHandler<ActionsEventArgs>(PresenterWorkDone);

            loginPresenter.Init();
            _hostWindow.AttachView(loginView);
        }

        private void PresenterWorkDone(object sender, ActionsEventArgs e)
        {
            switch (e.NextAction)
            {
                case Actions.Login:
                    currentPresenter = CreateLoginPresenter();
                    break;
                case Actions.Statuses:

                    UserViewModel usr = e.Data as UserViewModel;

                    if (usr != null)
                    {
                        currentPresenter = CreateStatusesPresenter(usr);
                    }
                    else
                    {
                        throw new ArgumentNullException();
                    }
                    break;
                case Actions.Configuration:
                    break;
                case  Actions.Close:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        private IPresenter CreateStatusesPresenter(UserViewModel user)
        {
            var statusPresenter = new StatusesPresenter(user);
            //dołączamy do niego widok, jednocześnie przkazując mu referencję 
            var statusView = new StatusListControl(statusPresenter);
            statusPresenter.SetView(statusView);
            statusPresenter.WorkDone += new EventHandler<ActionsEventArgs>(PresenterWorkDone);

            statusPresenter.Init();

            _hostWindow.SwitchView(statusView);

            return statusPresenter;
        }

        private IPresenter CreateLoginPresenter()
        {
            var loginPresenter = new LoginPresenter();
            //dołączamy do niego widok, jednocześnie przkazując mu referencję 
            var loginView = new LoginViewControl(loginPresenter);
            loginPresenter.SetView(loginView);
            loginPresenter.WorkDone += new EventHandler<ActionsEventArgs>(PresenterWorkDone);

            loginPresenter.Init();
            _hostWindow.SwitchView(loginView);

            return loginPresenter;
        }

        public void Close()
        {
            if (currentPresenter != null)
                currentPresenter.Close();
        }
    }
}