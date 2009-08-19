using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlipFace.Model
{
    /// <summary>
    /// Klasa grupująca informację o nowej wersji BlipFace przekazywana do widoku
    /// </summary>
    public class LatestVersionViewModel
    {
        public Version Version { get; set; }
        public Uri DownloadLink { get; set; }
    }
}
