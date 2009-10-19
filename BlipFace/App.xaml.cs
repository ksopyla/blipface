using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Text;
using ManagedWinapi;

namespace BlipFace
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Hotkey hotkey;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Dispatcher.UnhandledException += new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(Dispatcher_UnhandledException);

            hotkey = new Hotkey();
            hotkey.Ctrl = true;
            hotkey.KeyCode = BlipFace.Properties.Settings.Default.HotKey;
            hotkey.HotkeyPressed += new EventHandler(hotkey_HotkeyPressed);
            BlipFace.Properties.Settings.Default.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(Default_PropertyChanged);
            try
            {
                hotkey.Enabled = true;
            }
            catch (HotkeyAlreadyInUseException)
            {
                MessageBox.Show("HotKey jest już zajęty. Zmień w ustawieniach HotKey na inny.");
            }
        }

        void Default_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            hotkey.KeyCode = BlipFace.Properties.Settings.Default.HotKey;
            try
            {
                hotkey.Enabled = false;
                hotkey.Enabled = true;
            }
            catch (HotkeyAlreadyInUseException)
            {
                MessageBox.Show("HotKey jest już zajęty. Zmień w ustawieniach HotKey na inny.");
            }
        }

        void hotkey_HotkeyPressed(object sender, EventArgs e)
        {
            HostWindow windows = (HostWindow)MainWindow;
            windows.ToNormalBlipFaceWindows();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            hotkey.Dispose();
            base.OnExit(e);
        }

        void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {

            StringBuilder message = new StringBuilder(200);
            message.Append("Nieoczekiwany błąd aplikacji\n ");
            message.Append(e.Exception.Message);
            message.Append("\n");
            if (e.Exception.InnerException != null)
            {
                message.Append(e.Exception.InnerException.Message);
                message.Append("\n");

            }

            MessageBox.Show(message.ToString());
            e.Handled = true;
        }
    }
}
