using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BlipFace.Helpers
{

    /// <summary>
    /// Klasa skupiająca wszystkie wykorzystywane wyrażenia regularne
    /// </summary>
    public class BlipRegExp
    {
        public static Regex NormalText = new Regex(@"^[^\#\^]*");

        public static Regex User = new Regex(@"^\^\w*");

        /// <summary>
        /// dopasuj tekst zaczynający się od #po którym występuje litera,cyfra lub myślnik
        /// </summary>
        public static Regex Tag = new Regex(@"^\#[\w\-]*"); //  ^\#\w*

        /// <summary>
        /// 
        /// 
        /// </summary>
        public static Regex Link = new Regex(@"(http|https|ftp)\://[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,3}\S*");

        /// <summary>
        /// dopasowuje tekst w którym jest "mówi o tobie" lub "cię cytuje" przydatne przy notice
        /// </summary>
        public static readonly Regex SayOrCite = new Regex(@"^\^\w*\s(mówi\so\stobie|cię\scytuje)", RegexOptions.IgnoreCase);

        
        /// <summary>
        /// regex wyszukujące czy wiadomość nie rozpoczyna się jak prywatana
        /// </summary>
        public static Regex PrivateStart = new Regex(@"^>>.*?:");

        
        /// <summary>
        /// regex wyszukujące czy wiadomośc nie rozpoczyna się jak skierowana
        /// uwaga to wyrażenie łapie dwa typy tekstu z jednym znakiem > oraz z dwoma znakami >>
        /// dlatego dobrze działa i zamienia gdy drugi raz klikniemy wiadomość prywatna
        /// a dotychczasowa wiadomość jest już prywatna
        /// </summary>
        public static Regex DirectStart = new Regex(@"^>.*?:");


        /// <summary>
        /// wynajduje w url klucz do filmu na youtube
        /// </summary>
        public static Regex YoutubeWatchKey = new Regex(@"v=([\w|-]*)[&\s]?");
                            
        
    }
}
