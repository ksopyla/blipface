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
        void Close();
    }

    
}
