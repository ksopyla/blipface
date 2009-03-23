using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlipFace.Model
{
    public class StatusViewModel
    {
        public int StatusId { get; set; }

        public string UserId { get; set; }

        public string UserLogin { get; set; }

        public string UserAvatar30 { get; set; }

        public string Content { get; set; }


        public string CreationDate { get; set; }
    }
}
