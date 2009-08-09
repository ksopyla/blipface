using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BlipFace.View.Controls
{
    /// <summary>
    /// Interaction logic for Pager.xaml
    /// </summary>
    public partial class Pager : UserControl
    {
        public Pager()
        {
            InitializeComponent();
            CurrentPageIndex = 1;
        }

        public  bool IsDecIndexEnable
        {
            get
            {
               return  (CurrentPageIndex > 1);
            }
        }

        

        public int CurrentPageIndex
        {
            get { return (int) GetValue(CurrentPageIndexProperty); }
            set { 
                SetValue(CurrentPageIndexProperty, value);

                tblCurrentPage.Text = string.Format("Strona: {0}", CurrentPageIndex);
            }
        }

        public static readonly DependencyProperty CurrentPageIndexProperty =
            DependencyProperty.Register("CurrentPageIndex", typeof (int), typeof (Pager),
                                        GetCurrentPageIndexPropetyMetadata(),
                                        new ValidateValueCallback(ValidateCurrentPageIndex));

        private void OnCurrentPageIndexChanged(EventArgs e)
        {
            EventHandler handler = CurrentPageIndexChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public event EventHandler CurrentPageIndexChanged;

        private static bool ValidateCurrentPageIndex(object value)
        {
            int val = (int) value;
            if (val < 0)
            {
                return false;
            }
            
            return true;
        }

        private static void CurrentPageIndexPropertyChanged(DependencyObject element,
                                                            DependencyPropertyChangedEventArgs args)
        {
            ((Pager) element).CurrentPageIndexPropertyChanged((int) args.NewValue);
        }

        private void CurrentPageIndexPropertyChanged(int newCurrentPageIndex)
        {
            OnCurrentPageIndexChanged(EventArgs.Empty);
        }


        private static PropertyMetadata GetCurrentPageIndexPropetyMetadata()
        {
            PropertyMetadata pm = new PropertyMetadata();
            pm.DefaultValue = 1;
            pm.PropertyChangedCallback = new PropertyChangedCallback(CurrentPageIndexPropertyChanged);


            return pm;
        }

        private void DecreasePageIndex(object sender, MouseButtonEventArgs e)
        {
            if(CurrentPageIndex>1)
            {
                CurrentPageIndex--;
            }

            
            //lblPrevious.IsEnabled = (CurrentPageIndex > 1);
        }

        private void IncPageIndex(object sender, MouseButtonEventArgs e)
        {
            CurrentPageIndex++;

           // tblCurrentPage.Text = string.Format("Strona: {0}", CurrentPageIndex);
            
            //lblPrevious.IsEnabled = (CurrentPageIndex > 1);
        }
    }
}