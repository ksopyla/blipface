using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace BlipFace
{
   public interface IHostView
    {
        UserControl CurrentView { get; }

        void AttachView(UserControl _control);
        void SwitchView(UserControl view);
    }
}
