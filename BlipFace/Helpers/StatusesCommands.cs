using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using BlipFace.View;

namespace BlipFace.Helpers
{
    public class StatusesCommands
    {
        public static RoutedCommand CiteUser = new RoutedCommand("CiteUser", typeof(StatusListControl));

        public static RoutedCommand DirectMessage = new RoutedCommand("DirectMessage", typeof(StatusListControl));

        public static RoutedCommand PrivateMessage = new RoutedCommand("PrivateMessage", typeof(StatusListControl));

       // public static RoutedCommand Navigate = new RoutedCommand("Navigate", typeof(StatusListControl));
    }
}
