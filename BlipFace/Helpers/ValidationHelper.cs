using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlipFace.Helpers
{


    /// <summary>
    /// Metody pomocnicze do sprawdzania prametrów
    /// </summary>
    public class ValidationHelper
    {

        /// <summary>
        /// sprawdza zakres
        /// </summary>
        /// <param name="param"></param>
        /// <param name="lower"></param>
        /// <param name="upper"></param>
        /// <returns></returns>
        public static bool ChceckRange(int param, int lower, int upper)
        {
            bool checkLower = param >= lower;
            bool checkUpper = param <= upper;

            return checkLower && checkUpper;
        }
    }
}
