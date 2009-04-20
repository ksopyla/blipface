using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Text;

namespace BlipFace
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Dispatcher.UnhandledException += new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(Dispatcher_UnhandledException);
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
