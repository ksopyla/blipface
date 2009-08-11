using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlipFace.View
{
    public interface IView
    {

        void WorkDone();
        event EventHandler<ActionsEventArgs> ChangeView;
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
