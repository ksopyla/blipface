using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;

namespace BlipFace.Helpers
{

    /// <summary>
    /// Klasa pomaga szyfrować i rozszyforwywać plik z hasłem, zaiwiera kilka extensions methods
    /// Użycie:
    /// 
    /// Password = Password.ToSecureString().Encrypt();
    /// SecureString password = EncryptedPassword.Decrypt();
    /// 
    /// Kod zaczerpnięty z artykułu od Jon Galloway
    /// http://weblogs.asp.net/jgalloway/archive/2008/04/13/encrypting-passwords-in-a-net-app-config-file.aspx
    /// </summary>
    public static class ProtectionHelper
    {
        static byte[] entropy = Encoding.Unicode.GetBytes("%$Dadf8fpo0na4igA_5jfKDik");


        /// <summary>
        /// Pozwal zaszyfrować łańcuch znaków
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string Encrypt(this SecureString input)
        {
            byte[] encryptedData = System.Security.Cryptography.ProtectedData.Protect(
                Encoding.Unicode.GetBytes(ToInsecureString(input)),
                entropy,
                System.Security.Cryptography.DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedData);
        }


        /// <summary>
        /// Rozszyforwywuje łańcuch znaków
        /// </summary>
        /// <param name="encryptedData"></param>
        /// <returns></returns>
        public static SecureString Decrypt(this string encryptedData)
        {
            try
            {
                byte[] decryptedData = System.Security.Cryptography.ProtectedData.Unprotect(
                    Convert.FromBase64String(encryptedData),
                    entropy,
                    System.Security.Cryptography.DataProtectionScope.CurrentUser);
                return ToSecureString(Encoding.Unicode.GetString(decryptedData));
            }
            catch
            {
                return new SecureString();
            }
        }


        /// <summary>
        /// Przekształca zwykłego sttringa do SecureString
        /// </summary>
        /// <param name="input">łańcuch znaków do przekształecenia</param>
        /// <returns>Bezpieczny string</returns>
        public static SecureString ToSecureString(this string input)
        {
            SecureString secure = new SecureString();
            foreach (char c in input)
            {
                secure.AppendChar(c);
            }
            secure.MakeReadOnly();
            return secure;
        }

        /// <summary>
        /// Przekształca SecureString do zwykłego stringa
        /// </summary>
        /// <param name="input">bezpieczny łańcuch znaków</param>
        /// <returns>zwykły string</returns>
        public static string ToInsecureString(this SecureString input)
        {
            string returnValue = string.Empty;
            IntPtr ptr = System.Runtime.InteropServices.Marshal.SecureStringToBSTR(input);
            try
            {
                returnValue = System.Runtime.InteropServices.Marshal.PtrToStringBSTR(ptr);
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ZeroFreeBSTR(ptr);
            }
            return returnValue;
        }
    }
}
