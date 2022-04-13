using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using X_Manager.ConfigurationWindows;

namespace X_Manager
{
    public partial class AgmConfigurationWindow : ConfigurationWindow
    {
        byte[] conf;
        byte rate;
        byte accFullScale;
        byte gyroMode;
        byte gyroFullscale;
        byte compassMode;
        byte pressureMode;
        byte triggerMode;
        byte mDebug;
        byte unitType;
        public AgmConfigurationWindow(byte[] axyConf, byte uType)
        {
            InitializeComponent();
            mustWrite = false;
            conf = axyConf;
            unitType = uType;
            base.ContentRendered += contentRendered;
        }

        private void contentRendered(object sender, EventArgs e)
        {
            controlsInitialize(conf);
        }

        private void ctrlManager(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.D))
            {
                if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
                {
                    if ((mDebug == 0))
                    {
                        mDebug = 1;
                        // MessageBox.Show("mDebug enabled.")
                        sendButton.Content = "Send configuration (d)";
                    }
                    else
                    {
                        mDebug = 0;
                        // MessageBox.Show("mDebug disabled.")
                        sendButton.Content = "Send configuration";
                    }

                }

            }
        }

        private void controlsInitialize(byte[] axyConf)
        {
            // ACCELEROMETER - Rate
            ((RadioButton)(accRateGrid.Children[axyConf[15]])).IsChecked = true;
            sediciBitCB.IsChecked = false;
            if ((axyConf[16] > 7))
            {
                axyConf[16] -= 8;
                sediciBitCB.IsChecked = true;
            }

            ((RadioButton)(accFsGrid.Children[(axyConf[16] + 1)])).IsChecked = true;

            // GYROSCOPE
            ((RadioButton)(((Grid)(gyroGrid.Children[0])).Children[(axyConf[17] + 1)])).IsChecked = true;
            ((RadioButton)(((Grid)(gyroGrid.Children[1])).Children[(axyConf[18] + 1)])).IsChecked = true;

            //COMPASS
            ((RadioButton)(((Grid)(compGrid.Children[0])).Children[(axyConf[19] + 1)])).IsChecked = true;

            // PRESSURE
            ((RadioButton)(((Grid)(pressGrid.Children[0])).Children[(axyConf[20] + 1)])).IsChecked = true;
            if ((unitType == Units.Unit.model_AGM1))
            {
                // In caso di AGM se trovata pressione a 2 viene messa a 1
                if ((axyConf[20] == 2))
                {
                    axyConf[20] = 1;
                }

                ((RadioButton)(((Grid)(pressGrid.Children[0])).Children[3])).IsEnabled = false;
            }

            // TRIGGER
            ((RadioButton)(triggerGrid.Children[axyConf[21]])).IsChecked = true;
            mDebug = axyConf[25];
            if ((mDebug == 1))
            {
                sendButton.Content += " (d)";
            }
        }

        private void rateChanged(object sender, RoutedEventArgs e)
        {
            for (byte contaRate = 1; (contaRate <= 5); contaRate++)
            {
                if ((bool)((RadioButton)(accRateGrid.Children[contaRate])).IsChecked)
                {
                    rate = contaRate;
                    break;
                }

            }

        }

        private void accFullScaleChanged(object sender, RoutedEventArgs e)
        {
            for (byte contaAccFullscale = 1; (contaAccFullscale <= 4); contaAccFullscale++)
            {
                if ((bool)((RadioButton)(accFsGrid.Children[contaAccFullscale])).IsChecked)
                {
                    accFullScale = (byte)(contaAccFullscale - 1);
                    break;
                }

            }

        }

        private void gyroModeChanged(object sender, RoutedEventArgs e)
        {
            if ((bool)((RadioButton)(((Grid)(gyroGrid.Children[0])).Children[1])).IsChecked)
            {
                gyroMode = 0;
            }
            else if ((bool)((RadioButton)(((Grid)(gyroGrid.Children[0])).Children[2])).IsChecked)
            {
                gyroMode = 1;
            }
            else
            {
                gyroMode = 2;
            }

        }

        private void gyroFullScaleChanged(object sender, RoutedEventArgs e)
        {
            for (int contaGyroFullscale = 1; (contaGyroFullscale <= 4); contaGyroFullscale++)
            {
                if ((bool)((RadioButton)(((Grid)(gyroGrid.Children[1])).Children[contaGyroFullscale])).IsChecked)
                {
                    gyroFullscale = (byte)(contaGyroFullscale - 1);
                    break;
                }

            }

        }

        private void compassModeChanged(object sender, RoutedEventArgs e)
        {
            if ((bool)((RadioButton)(((Grid)(((Grid)(compGrid)).Children[0])).Children[1])).IsChecked)
            {
                compassMode = 0;
            }
            else if ((bool)((RadioButton)(((Grid)(((Grid)(compGrid)).Children[0])).Children[2])).IsChecked)
            {
                compassMode = 1;
            }
            else
            {
                compassMode = 2;
            }

        }

        private void pressureModeChanged(object sender, RoutedEventArgs e)
        {
            if ((bool)((RadioButton)(((Grid)(((Grid)(pressGrid)).Children[0])).Children[1])).IsChecked)
            {
                pressureMode = 0;
            }
            else if ((bool)((RadioButton)(((Grid)(((Grid)(pressGrid)).Children[0])).Children[2])).IsChecked)
            {
                pressureMode = 1;
            }
            else
            {
                pressureMode = 2;
            }

        }

        private void triggerModeChanged(object sender, RoutedEventArgs e)
        {
            if (((bool)((RadioButton)(triggerGrid.Children[0])).IsChecked)) triggerMode = 0;
            else if (((bool)((RadioButton)(triggerGrid.Children[1])).IsChecked)) triggerMode = 1;
            else if (((bool)((RadioButton)(triggerGrid.Children[2])).IsChecked)) triggerMode = 2;
        }

        private void sendConf(object sender, RoutedEventArgs e)
        {
            //ACCELEROMETER - Rate
            conf[15] = rate;

            //ACCELEROMETER - Fullscale
            conf[16] = accFullScale;
            if ((bool)sediciBitCB.IsChecked) conf[16] += 8;

            //GYROSCOPE Function/Fullscale
            conf[17] = gyroMode;
            conf[18] = gyroFullscale;

            //COMPASS
            conf[19] = compassMode;

            //PRESSURE
            conf[20] = pressureMode;

            //TRIGGER
            conf[21] = triggerMode;
            conf[23] = 0;
            conf[24] = 5;

            //MDEBUG
            conf[25] = mDebug;

            axyConfOut = conf;
            mustWrite = true;
            this.Close();
        }
    }
}
