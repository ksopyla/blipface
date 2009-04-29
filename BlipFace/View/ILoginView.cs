using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlipFace.View
{
    public interface ILoginView : IView
    {
        string UserName { get; set; }
        string Password { get; set; }
        string Error { get; set; }
       bool Authorize { get; set; }
        
    }
}
