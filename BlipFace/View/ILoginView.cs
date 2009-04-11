using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlipFace.View
{
    public interface ILoginView
    {
        public string UserName { get; set; }

        public string Password { get; set; }

        public string ErrorMessage { get; set; }
    }
}
