using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlipFace.View;

namespace BlipFace.Presenter
{
    internal interface IPresenter
    {
        void SetView(IView view);
        void Init();
       
        event EventHandler<ActionsEventArgs> WorkDone;

        void Close();
    }

    public class ActionsEventArgs : EventArgs
    {
        public Actions NextAction { get; private set; }

        public object Data { get; private set; }

        public ActionsEventArgs(Actions action)
        {
            NextAction = action;
        }

        public ActionsEventArgs(Actions action, object data)
        {
            NextAction = action;
            Data = data;
        }
    }

    public enum Actions
    {
        Login,
        Statuses,
        Configuration,
        Close
    } ;
}
