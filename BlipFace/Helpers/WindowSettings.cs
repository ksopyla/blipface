using System;
using System.Diagnostics;
using System.Globalization;
using System.ComponentModel;
using System.Configuration;
using System.Windows;
using System.Windows.Markup;

namespace BlipFace.Helpers
{
    /// <summary>
    /// Persists a Window's Size, Location to UserScopeSettings 
    /// </summary>
    public class WindowSettings
    {
        #region Constructor
        private Window window = null;

        public WindowSettings(Window window)
        {
            this.window = window;
        }

        #endregion

        #region Attached "Save" Property Implementation
        /// <summary>
        /// Register the "Save" attached property and the "OnSaveInvalidated" callback 
        /// </summary>
        public static readonly DependencyProperty SaveProperty
           = DependencyProperty.RegisterAttached("Save", typeof(bool), typeof(WindowSettings),
                new FrameworkPropertyMetadata(new PropertyChangedCallback(OnSaveInvalidated)));

        public static void SetSave(DependencyObject dependencyObject, bool enabled)
        {
            dependencyObject.SetValue(SaveProperty, enabled);
        }

        /// <summary>
        /// Called when Save is changed on an object.
        /// </summary>
        private static void OnSaveInvalidated(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            Window window = dependencyObject as Window;
            if (window != null)
            {
                if ((bool)e.NewValue)
                {
                    WindowSettings settings = new WindowSettings(window);
                    settings.Attach();
                }
            }
        }

        #endregion

        #region Protected Methods
        /// <summary>
        /// Load the Window Size Location and State from the settings object
        /// </summary>
        protected virtual void LoadWindowState()
        {
            Properties.Settings.Default.Reload();
            Rect rect = Rect.Parse(Properties.Settings.Default[window.GetType().Name].ToString().Replace(';', ','));

            if (rect != Rect.Empty)
            {
                this.window.Left = rect.Left;
                this.window.Top = rect.Top;
                this.window.Width = rect.Width;
                this.window.Height = rect.Height;
            }
        }

        /// <summary>
        /// Save the Window Size, Location and State to the settings object
        /// </summary>
        protected virtual void SaveWindowState()
        {
            Properties.Settings.Default[window.GetType().Name] = this.window.RestoreBounds.ToString();
            Properties.Settings.Default.Save();
        }
        #endregion

        #region Private Methods

        private void Attach()
        {
            if (this.window != null)
            {
                this.window.Closing += new CancelEventHandler(window_Closing);
                this.window.Initialized += new EventHandler(window_Initialized);
            }
        }

        private void window_Initialized(object sender, EventArgs e)
        {
            LoadWindowState();
        }

        private void window_Closing(object sender, CancelEventArgs e)
        {
            SaveWindowState();
        }
        #endregion
    }
}

