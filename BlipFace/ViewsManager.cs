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
            loginView.ChangeView += new EventHandler<ActionsEventArgs>(ViewWorkDone);
            loginPresenter.SetView(loginView);

            //loginPresenter.WorkDone += new EventHandler<ActionsEventArgs>(ViewWorkDone);

            loginPresenter.Init();
            _hostWindow.AttachView(loginView);
        }

        private void ViewWorkDone(object sender, ActionsEventArgs e)
        {
            switch (e.NextAction)
            {
                case Actions.Login:
                    currentPresenter = CreateLoginView();
                    break;
                case Actions.Statuses:

                    UserViewModel usr = e.Data as UserViewModel;

                    if (usr != null)
                    {
                        currentPresenter = CreateStatusesView(usr);
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


        private IPresenter CreateStatusesView(UserViewModel user)
        {
            var statusPresenter = new StatusesPresenter(user);
            //dołączamy do niego widok, jednocześnie przkazując mu referencję 
            var statusView = new StatusListControl(statusPresenter);
            statusView.ChangeView += new EventHandler<ActionsEventArgs>(ViewWorkDone);
            
            statusPresenter.SetView(statusView);
            //statusPresenter.WorkDone += new EventHandler<ActionsEventArgs>(PresenterWorkDone);

            statusPresenter.Init();

            _hostWindow.SwitchView(statusView);

            return statusPresenter;
        }

        private IPresenter CreateLoginView()
        {
            var loginPresenter = new LoginPresenter();
            //dołączamy do niego widok, jednocześnie przkazując mu referencję 
            var loginView = new LoginViewControl(loginPresenter);
            loginView.ChangeView +=new EventHandler<ActionsEventArgs>(ViewWorkDone);
            loginPresenter.SetView(loginView);
           
           

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