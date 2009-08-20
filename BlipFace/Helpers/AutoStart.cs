using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;

namespace BlipFace.Helpers
{
    public class AutoStart
    {
        public readonly string ApplicationName;
        readonly string regkeyname;
        /// <summary>
        /// Creates an AutoStart object for the current application
        /// </summary>
        public AutoStart()
            : this(Application.ProductName, Application.ExecutablePath)
        {
        }
        public AutoStart(string ApplicationName)
            : this(null, ApplicationName)
        {
        }
        private AutoStart(string KeyName, string ApplicationName)
        {
            this.ApplicationName = ApplicationName;
            FileInfo fi = new FileInfo(ApplicationName);
            if (!fi.Exists)
                throw new Exception(ApplicationName + " Does not exist!");
            regkeyname = KeyName == null ? fi.Name : KeyName;
            startupfolderfile =
            Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\"
            + fi.Name.Replace(fi.Extension, ".lnk");
        }
        public AutoStart(string ApplicationName, params string[] CommandLineArguments)
            : this(ApplicationName)
        {
            this.CommandlineParameters = string.Join(" ", CommandLineArguments);
        }
        readonly RegistryKey startkey = GetStartupRegistryDir();
        private string commandlineparams;
        /// <summary>
        /// gets or sets the command line arguments for the application
        /// </summary>
        public string CommandlineParameters
        {
            get { return commandlineparams; }
            set
            {
                if (value.Trim().Length == 0) value = null;
                commandlineparams = value;
                if (EnabledThroughRegistry)
                    SetRegKey();
            }
        }
        /// <summary>
        /// Sets the <see cref="CommandlineParameters"/> property to the commandlines with which
        /// the application was started
        /// </summary>
        public void SetCurrentCommandLine()
        {
            CommandlineParameters = string.Join(" ", Environment.GetCommandLineArgs());
        }
        private void SetRegKey()
        {
            startkey.SetValue(regkeyname, CompleteName);
        }
        /// <summary>
        /// The complete command line name to start the file including arguments
        /// </summary>
        public string CompleteName
        {
            get
            {
                return ApplicationName +
                (commandlineparams == null ? null : " " + commandlineparams);
            }
        }
        /// <summary>
        /// If true a registry item exists that starts the for which this class was created.
        /// </summary>
        public bool EnabledThroughRegistry
        {
            get { return startkey.GetValue(regkeyname) != null; }
            set
            {
                if (value == EnabledThroughRegistry) return;
                if (value)
                {
                    SetRegKey();
                }
                else
                {
                    startkey.DeleteValue(regkeyname);
                }
            }
        }
        readonly string startupfolderfile;
        public bool EnabledThroughStartupMenu
        {
            get { return File.Exists(startupfolderfile); }
            set
            {
                if (EnabledThroughStartupMenu == value) return;
                if (value)
                    createshortcut();
                else
                    File.Delete(startupfolderfile);
            }
        }
        void createshortcut()
        {
            //Chosen for creating a shortcut with the help of vbscript
            //It's an extra liablility, but better than forcing the
            //use of a WSH wrapper.
            //An alternative can be found here: http://www.msjogren.net/dotnet/eng/samples/dotnet_shelllink.asp
            //Looks very interesting, but didn't try it, since it would inflate the
            //use of this simple class too much.
            //But still it might be a very good thing to use, especially if
            //this doesn't work ;-)
            string file = Application.UserAppDataPath + "\\createshortcut.vbs";
            try
            {
                StreamWriter sw = new StreamWriter(file);
                sw.Write(string.Format(
                @"Set Shell = CreateObject(""WScript.Shell"")
Set link = Shell.CreateShortcut(""{0}"")
link.TargetPath = ""{1}""
link.Description = ""{2}""
link.Arguments = ""{3}""
link.WorkingDirectory = ""{4}""
link.Save"
                , startupfolderfile, ApplicationName, regkeyname, commandlineparams, new FileInfo(ApplicationName).DirectoryName));
                sw.Close();
                Process.Start(file).WaitForExit();
            }
            catch
            {
                throw;
            }
            finally
            {
                try { File.Delete(file); }
                catch { }
            }
        }
        static AutoStart current;
        /// <summary>
        /// Returns the AutoStart information for the current application.
        /// The object is created upon the first call and kept in memory for
        /// faster access. The object can be destroyed if wanted with <see cref="ResetCurrent"/>
        /// </summary>
        public static AutoStart Current
        {
            get
            {
                if (current == null) current = new AutoStart();
                return current;
            }
        }
        /// <summary>
        /// Destroys the object created when <see cref="Current"/> was called.
        /// <see cref="Current"/> can still be used, but the object will be recreated
        /// </summary>
        public static void ResetCurrent()
        {
            current = null;
        }
        public static RegistryKey GetStartupRegistryDir()
        {
            return Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
        }
        public static string[] GetStartupRegistryApplications()
        {
            RegistryKey r = GetStartupRegistryDir();
            //if (r.SubKeyCount == 0) return new string[0];
            return r.GetValueNames();
        }
    }
}