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

namespace X_Manager.ConfigurationWindows
{
    /// <summary>
    /// Interaction logic for SettingsTab.xaml
    /// </summary>
    public partial class TrekSettingsTab : UserControl
    {
        object parent;
        public NumericUpDown batteryNud;
        public TrekSettingsTab(object p)
        {
            InitializeComponent();

            parent = p;

            adcRecording.IsChecked = true;

            dEnabled.IsChecked = false;
            dDisabled.IsChecked = true;

            RoutedEventArgs ev = new RoutedEventArgs();
            ev.RoutedEvent = CheckBox.CheckedEvent;

            ((Grid)((GroupBox)((Grid)((Grid)this.Content).Children[2]).Children[0]).Content).RowDefinitions[1].Height = new GridLength(0);

            ADCValueUD.Value = 512;
            magMinB.Content = ">";

            SdDateTimePicker.Value = DateTime.Now;
            OffRB.IsChecked = true;
            ByTimeRB.IsChecked = false;
            ByDateRB.IsChecked = false;

        }

        public double[] export()
        {
            double[] schSettings = new double[16];
            schSettings[0] = startDelayNud.Value;
            schSettings[1] = onTimeNud.Value;
            schSettings[2] = offTimeNud.Value;
            schSettings[3] = nSatNud.Value;
            schSettings[4] = acqSumNud.Value;
            schSettings[5] = acq1Nud.Value;
            schSettings[6] = acq2Nud.Value;
            schSettings[7] = 0;

            if ((bool)dEnabled.IsChecked) schSettings[7] = 1;

            schSettings[8] = 0;
            if ((bool)adcRecording.IsChecked) schSettings[8] = 1;

            schSettings[9] = 0;
            if ((bool)adcTrigger.IsChecked) schSettings[9] = 1;

            schSettings[10] = 0;
            if (((string)(magMinB.Content) == ">")) schSettings[10] = 1;

            schSettings[11] = ADCValueUD.Value;

            schSettings[12] = SdDateTimePicker.Value.Value.Day;
            schSettings[13] = SdDateTimePicker.Value.Value.Month;
            schSettings[14] = SdDateTimePicker.Value.Value.Year;

            schSettings[15] = 0;
            if ((bool)ByTimeRB.IsChecked) schSettings[15] = 1;
            if ((bool)ByDateRB.IsChecked) schSettings[15] = 2;

            return schSettings;
        }

        public void import(double[] schSettings)
        {
            startDelayNud.Value = schSettings[0];
            onTimeNud.Value = schSettings[1];
            offTimeNud.Value = schSettings[2];
            nSatNud.Value = schSettings[3];
            acqSumNud.Value = schSettings[4];
            acq1Nud.Value = schSettings[5];
            acq2Nud.Value = schSettings[6];
            dEnabled.IsChecked = false;
            dDisabled.IsChecked = true;

            if (schSettings[7] == 1)
            {
                dDisabled.IsChecked = false;
                dEnabled.IsChecked = true;
            }
            adcRecording.IsChecked = false;
            if (schSettings[8] == 1) adcRecording.IsChecked = true;
            adcTrigger.IsChecked = false;
            if (schSettings[9] == 1) adcTrigger.IsChecked = true;
            magMinB.Content = "<";
            if (schSettings[10] == 1) magMinB.Content = ">";
            ADCValueUD.Value = (UInt16)schSettings[11];

            if ((schSettings.Length > 12))
            {
                try
                {
                    setSdDate(new double[] { schSettings[14], schSettings[13], schSettings[12] });
                }
                catch { }

                setSdRb(schSettings[15]);
            }
            else SdDateTimePicker.Value = DateTime.Now;
        }

        public void disableWaterControl()
        {
            ((GroupBox)((Grid)((Grid)this.Content).Children[2]).Children[0]).Header = "WATER SWITCH";
            adcRecording.IsChecked = true;
            adcRecording.Content = "Handled by Accelerometer Configuration";
            adcRecording.FontSize = 11;
            adcRecording.IsEnabled = false;
        }

        public void switchToAdc()
        {
            ((Grid)((GroupBox)((Grid)((Grid)this.Content).Children[2]).Children[0]).Content).RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
            ((GroupBox)((Grid)((Grid)this.Content).Children[2]).Children[0]).Header = "ADC RECORDER / TRIGGER";
            adcRecording.Content = "Enable raw data recording";

        }

        private void magMinClick(object sender, RoutedEventArgs e)
        {
            if ((string)magMinB.Content == ">") magMinB.Content = "<";
            else magMinB.Content = ">";
        }

        public void setSdDate(double[] d)
        {
            DateTime dateD = new DateTime((int)d[0], (int)d[1], (int)d[2]);
            SdDateTimePicker.Value = dateD;
        }

        public void setSdRb(double d)
        {
            int n = (int)d;
            OffRB.IsChecked = false;
            ByTimeRB.IsChecked = false;
            ByDateRB.IsChecked = false;
            switch (n)
            {
                case 0:
                    OffRB.IsChecked = true;
                    break;
                case 1:
                    ByTimeRB.IsChecked = true;
                    break;
                case 2:
                    ByDateRB.IsChecked = true;
                    break;
            }
        }

        public double getSdRb()
        {
            if ((bool)ByTimeRB.IsChecked) return 1;
            else if ((bool)ByDateRB.IsChecked) return 2;
            else return 0;
        }

        private void offRbChecked(object sender, RoutedEventArgs e)
        {
            ByTimeRB.IsChecked = false;
            ByDateRB.IsChecked = false;

            if ((SdDateTimePicker.Visibility == Visibility.Hidden)) startDelayNud.Value = 0;
        }

        private void byTimeRbChecked(object sender, RoutedEventArgs e)
        {
            OffRB.IsChecked = false;
            ByDateRB.IsChecked = false;

        }

        private void byDateRbChecked(object sender, RoutedEventArgs e)
        {
            OffRB.IsChecked = false;
            ByTimeRB.IsChecked = false;

        }

	}
}
