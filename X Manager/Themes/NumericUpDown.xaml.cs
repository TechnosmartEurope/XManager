using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace X_Manager.Themes
{
    /// <summary>
    /// Interaction logic for NumericUpDown.xaml
    /// </summary>
    public partial class NumericUpDown : UserControl
    {
        double inc = 1;
        double maxValueCheck;
        double minValueCheck;
        byte rDigits;
        double valore;
        static readonly DependencyProperty headerContentProperty = DependencyProperty.Register("headerContent", typeof(String), typeof(NumericUpDown));
        static readonly DependencyProperty headerHeightProperty = DependencyProperty.Register("headerHeight", typeof(string), typeof(NumericUpDown));
        static readonly DependencyProperty footerContentProperty = DependencyProperty.Register("footerContent", typeof(string), typeof(NumericUpDown));
        static readonly DependencyProperty footerWidthProperty = DependencyProperty.Register("footerWidth", typeof(string), typeof(NumericUpDown));
        static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(double), typeof(NumericUpDown));
        static readonly DependencyProperty maxValueProperty = DependencyProperty.Register("maxValue", typeof(double), typeof(NumericUpDown));
        static readonly DependencyProperty minValueProperty = DependencyProperty.Register("minValue", typeof(double), typeof(NumericUpDown), new PropertyMetadata(null));
        static readonly DependencyProperty roundDigitsProperty = DependencyProperty.Register("roundDigits", typeof(byte), typeof(NumericUpDown));
        static readonly DependencyProperty increaseProperty = DependencyProperty.Register("Increase", typeof(double), typeof(NumericUpDown));

        public NumericUpDown()
        {
            InitializeComponent();

            this.Loaded += loaded;
            headerContent = null;
            headerHeight = "Auto";
            footerContent = null;
            footerWidth = "Auto";
            Value = 0;
            maxValue = 100;
            minValue = 0;
            roundDigits = 0;
            Increase = 1;
            validate();
        }

        private void loaded(object sender, RoutedEventArgs e)
        {
            header.Content = headerContent;
            setHeaderHeight(headerHeight);
            setFooterWidth(footerWidth);
            footer.Content = footerContent;
            valore = Value;
            maxValueCheck = maxValue;
            minValueCheck = minValue;
            rDigits = roundDigits;
            inc = Increase;
            validate();
            Value = valore;
        }

        #region Proprietà
        
        public string headerContent
        {
            get
            {
                return (string)(base.GetValue(headerContentProperty));
            }
            set
            {
                SetValue(headerContentProperty, value);
                header.Content = value;
            }

        }
        
        public string headerHeight
        {
            get
            {
                return (string)(base.GetValue(headerHeightProperty));
            }
            set
            {
                SetValue(headerHeightProperty, value);
                setHeaderHeight(value);
            }
        }
        
        public string footerContent
        {
            get
            {
                return (string)(base.GetValue(footerContentProperty));
            }
            set
            {
                base.SetValue(footerContentProperty, value);
                footer.Content = value;
            }
        }
                
        public string footerWidth
        {
            get
            {
                return (string)(base.GetValue(footerWidthProperty));
            }
            set
            {
                SetValue(footerWidthProperty, value);
                setFooterWidth(value);
            }
        }

        public double Value
        {
            get
            {
                return (double)(base.GetValue(ValueProperty));
            }
            set
            {
                SetValue(ValueProperty, value);
                valore = value;
                if ((valore % 1) != 0)
                {
                    if (roundDigits == 0)
                    {
                        roundDigits = 1;
                    }
                }
                validate();
            }
        }

        public double maxValue
        {
            get
            {
                return (double)(base.GetValue(maxValueProperty));
            }
            set
            {
                SetValue(maxValueProperty, value);
                maxValueCheck = value;
            }
        }

        public double minValue
        {
            get
            {
                return (double)(base.GetValue(minValueProperty));
            }
            set
            {
                SetValue(minValueProperty, value);
                minValueCheck = value;
            }
        }

        public byte roundDigits
        {
            get
            {
                return (byte)(base.GetValue(roundDigitsProperty));
            }
            set
            {
                SetValue(roundDigitsProperty, value);
                rDigits = value;
            }
        }

        public double Increase
        {
            get
            {
                return (double)(base.GetValue(increaseProperty));
            }
            set
            {
                if (value == 0) value = 1;
                SetValue(increaseProperty, value);
                inc = value;
            }
        }

        #endregion

        private void upButtonClick(object sender, RoutedEventArgs e)
        {
            if ((valore + inc) <= maxValueCheck)
            {
                valore += inc;
                validate();
                Value = Convert.ToDouble(valueTB.Text);
            }
        }

        private void downButtonClick(object sender, RoutedEventArgs e)
        {
            if ((valore - inc) >= minValueCheck)
            {
                valore -= inc;
                validate();
                Value = Convert.ToDouble(valueTB.Text);
            }
        }

        private void kdValidate(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return | e.Key == Key.Tab)
            {

                try
                {
                    valore = Convert.ToDouble(valueTB.Text);
                }
                catch
                {
                    valore = this.Value;
                }

                validate();
                Value = valore;
                if (e.Key == Key.Return)
                {
                    FocusNavigationDirection fnd = FocusNavigationDirection.Next;
                    System.Windows.Input.TraversalRequest tr = new System.Windows.Input.TraversalRequest(fnd);
                    UIElement el = Keyboard.FocusedElement as UIElement;
                    if (el != null)
                    {
                        el.MoveFocus(tr);
                    }
                }
                
                //e.Handled = true;
                //if (e.Key == Key.Return) this.select
            }
        }

        private void lsValidate(object sender, RoutedEventArgs e)
        {
            try
            {
                valore = Convert.ToDouble(valueTB.Text);
            }
            catch
            {
                valore = this.Value;
            }
            validate();
            Value = valore;
        }

        private void validate()
        {
            if (valore > maxValueCheck) valore = maxValueCheck;

            if (valore < minValueCheck) valore = minValueCheck;

            valueTB.Text = Math.Round(valore, rDigits).ToString();

            onValidate(new RoutedEventArgs());
            
        }


        public event RoutedEventHandler valueChanged;

        protected virtual void onValidate(RoutedEventArgs e)
        {
            RoutedEventHandler handler = valueChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void setHeaderHeight(string v)
        {
            if (v == "Auto")
            {
                header.Height = Double.NaN;
            }
            else
            {
                try
                {
                    header.Height = Convert.ToInt16(v);
                }
                catch
                {
                    header.Height = 0;
                }
            }
        }

        private void setFooterWidth(string v)
        {
            if (v == "Auto")
            {
                header.Height = Double.NaN;
            }
            else
            {
                try
                {
                    footer.Width = Convert.ToInt16(v);
                }
                catch
                {
                    footer.Width = 0;
                }
            }
        }
    }
}
