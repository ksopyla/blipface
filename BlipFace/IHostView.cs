using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace BlipFace
{
   public interface IHostView
    {
       void AttachView(UserControl view);
        void SwitchView(UserControl view);
    }
}
