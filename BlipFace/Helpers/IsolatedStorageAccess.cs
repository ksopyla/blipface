using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;

namespace BlipFace.Helpers
{
    public class IsolatedStorageAccess : IDisposable
    {
        private IsolatedStorageFile isoStore;
       // private IsolatedStorageFileStream isoStream;

        private string isoFileName;

        public IsolatedStorageAccess(string fileName)
        {
            isoFileName = fileName;


            //gdzie są przychowywane foldery można przeczytać
            //http://msdn.microsoft.com/en-us/library/3ak841sy(VS.80).aspx
            //u mnie na viście jest to folder
            //C:\Users\ksirg\AppData\Local\VirtualStore\Program Files\BlipFace
            //oraz C:\Users\ksirg\AppData\Local\IsolatedStorage\ plus dziwne nazwy folderów
            isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null,
                                                    null);

            //towrzymy główny folder do przechowywania
            //isoStore.CreateDirectory("blipFace");

            //tworzymy plik w którym bedzie przechowywane  zaszyfrowane hasło i login
           // isoStream = new IsolatedStorageFileStream(isoFileName, FileMode.Create, isoStore);
        }

        public void WriteStrings(string[] texts)
        {
            using (StreamWriter sw = new StreamWriter(new IsolatedStorageFileStream(isoFileName, FileMode.Create, isoStore)))
            {
                for (int i = 0; i < texts.Length; i++)
                {
                    sw.WriteLine(texts[i]);
                }
                ////zapisujem login
                //sw.Write(usr.Encrypt());

                ////nowa linia
                //sw.Write(Environment.NewLine);

                ////zapisujemy hasło
                //sw.Write(pas.Encrypt());

                sw.Close();
            }
        }

        public string[] ReadAll()
        {
            string[] fileNames = isoStore.GetFileNames(isoFileName);
            foreach (string file in fileNames)
            {
                //metoda GetFileNames zwraca listę plików pasującą do wzorca, 
                //powinien być tylko 1, lecz na wszelki wypadek stosujemy zabezpieczenie aby
                //nic nam niepotrzbnym wyjątkiem nie rzuciło
                if (file == isoFileName)
                {
                    using (
                        var sr = new StreamReader(new IsolatedStorageFileStream(isoFileName, FileMode.Open, isoStore)))
                    {
                        List<string> fileContent = new List<string>(2);

                        while (sr.Peek() >= 0)
                        {
                            fileContent.Add(sr.ReadLine());
                        }

                        return fileContent.ToArray();
                    }
                }
            } //end foreach

            //jeśli nic nie odczyta to zwróć null
            return null;
        }

        public void DeleteFile()
        {
            string[] fileNames = isoStore.GetFileNames(isoFileName);

            if (fileNames.Length > 0)
            {
                isoStore.DeleteFile(isoFileName);
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
           
            if(isoStore!=null)
                isoStore.Dispose();
        }

        #endregion
    }
}