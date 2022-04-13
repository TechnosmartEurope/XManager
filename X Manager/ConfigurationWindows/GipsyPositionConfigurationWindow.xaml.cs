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
using System.Windows.Shapes;
using X_Manager.Units;
using X_Manager.ConfigurationWindows;
using X_Manager;

namespace X_Manager.ConfigurationWindows
{
    /// <summary>
    /// Interaction logic for GipsyConfigurationWindow.xaml
    /// </summary>
    public partial class GipsyPositionConfigurationWindow : ConfigurationWindow
    {
        //bool res = true;
        //public bool failure = false;
        //bool setIdle = false;
        public bool unitConnected = false;
        public UInt32 firmTotB = 0;
        Unit unit;
        public GipsyPositionConfigurationWindow(ref Unit unitIn)
        {
            InitializeComponent();
            this.Loaded += loaded;
            this.Unloaded += unloaded;
            unit = unitIn;

            if (!(unit == null))
            {
                readButton.IsEnabled = true;
                sendButton.IsEnabled = true;
                //setIdle = true;
                unitConnected = true;
                firmTotB = unit.firmTotB;
                res = true;
            }

            for (UInt16 i = 1; (i <= 9); i++)
            {
                TabItem t = new TabItem();
                // With...
                t.FontSize = 12;
                t.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0xaa, 0xde));
                t.Name = "day" + i.ToString() + "TabItem";
                t.Header = "Day " + i.ToString();
                t.Width = 65;
                t.Height = 30;
                t.Background = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30));
                t.Margin = new Thickness(2);

                ScheduleTab.Items.Add(t);
            }

            ((TabItem)ScheduleTab.Items.GetItemAt(7)).Header = "Settings";
            ((TabItem)ScheduleTab.Items.GetItemAt(7)).Name = "Settings";

            ((TabItem)ScheduleTab.Items.GetItemAt(8)).Header = "Remote";
            ((TabItem)ScheduleTab.Items.GetItemAt(8)).Name = "Remote";

            defaultSettings();
        }

        private void loaded(object sender, RoutedEventArgs e)
        {
            bool result = false;
            while (!result)         //Sistemare, forse result non viene messo a true
            {
                if (!System.IO.File.Exists(gipsySchedFile))
                {
                    exportSchedule(gipsySchedFile);
                    result = true;
                }
                else
                {
                    result = importSchedule(gipsySchedFile);
                    if (!result) System.IO.File.Delete(gipsySchedFile);
                }
            }
        }

        private void unloaded(object sender, EventArgs e)
        {
            exportSchedule(gipsySchedFile);
        }

        private void dailyCheckChecked(object sender, RoutedEventArgs e)
        {
            byte ccount = 1;
            foreach (TabItem t in ScheduleTab.Items)
            {
                if ((ccount == 1))
                {
                    DayTab dt = (DayTab)t.Content;
                    dt.dayLabel.Content = "Every day";
                    dt.copyToNextDayButton.Visibility = Visibility.Hidden;
                    dt.copyToNextDayButton.IsEnabled = false;
                }
                if (((ccount != 1) && ((ccount != 8) && (ccount != 9)))) t.Visibility = Visibility.Hidden;
                ccount++;
            }

            ScheduleTab.SelectedIndex = 0;
            ((TabItem)ScheduleTab.Items.GetItemAt(0)).Header = "Every day";
        }

        private void weeklyCheckChecked(object sender, RoutedEventArgs e)
        {
            foreach (TabItem t in ScheduleTab.Items)
            {
                t.Visibility = Visibility.Visible;
                try
                {
                    DayTab dt = (DayTab)t.Content;
                    if (dt.Name == "dayTab1")
                    {
                        dt.dayLabel.Content = "Day 1";
                        dt.copyToNextDayButton.IsEnabled = true;
                        dt.copyToNextDayButton.Visibility = Visibility.Visible;
                    }
                }
                catch { }
            }
            ((TabItem)ScheduleTab.Items.GetItemAt(0)).Header = "Day 1";
        }

        private void readButtonClick(object sender, RoutedEventArgs e)  //Controllare format string
        {
            defaultSettings();
            byte[] conf = new byte[256];
            try
            {
                conf = unit.getGpsSchedule();
            }
            catch (Exception ex)
            {
                res = false;
                MessageBox.Show(ex.Message);
                this.Close();
                return;
            }

            GipsySettingsTab st = new GipsySettingsTab(this);
            st.switchToAdc();

            //Acquisition On e Off time
            st.onTimeNud.Value = conf[0x10] * 256 + conf[0x11];
            st.offTimeNud.Value = conf[0x12] * 256 + conf[0x13];

            //Debug Mode
            st.dDisabled.IsChecked = true;
            if (conf[0x14] > 15)
            {
                st.dEnabled.IsChecked = true;
            }

            //Start delay
            switch ((conf[0x14] & 15))
            {
                case 0:
                    st.OffRB.IsChecked = true;
                    break;
                case 1:
                    st.ByTimeRB.IsChecked = true;
                    break;
                case 2:
                    st.ByDateRB.IsChecked = true;
                    break;
            }
            st.SdDateTimePicker.Value = new DateTime(conf[0x17] + 2000, conf[0x16], conf[0x15]);
            st.startDelayNud.Value = conf[0x18] * 256 + conf[0x19];

            //ADC
            byte adcMode = conf[0x1a];
            uint adcRawSatMode = (conf[0x1b] * (uint)256 + conf[0x1c]);
            double adcRaw = adcRawSatMode & 0xfff;

            if (((byte)(adcMode >> 6)) > 0)
            {
                st.adcRecording.IsChecked = true;
            }
            else
            {
                st.adcRecording.IsChecked = false;
            }
            if ((((byte)(adcMode >> 4)) & 3) > 0)
            {
                st.adcTrigger.IsChecked = true;
            }
            else
            {
                st.adcTrigger.IsChecked = false;
            }

            st.magMinB.Content = "<";
            if ((adcMode & 15) > 0)
            {
                st.magMinB.Content = ">";
            }
            st.ADCValueUD.Value = adcRaw;

            //Schedule Mode
            byte schMode = (byte)(adcRawSatMode >> 15);
            if (schMode == 1)
            {
                DailyCheck.IsChecked = true;
            }
            else
            {
                WeeklyCheck.IsChecked = true;
            }

            //Parametri low signal
            st.nSatNud.Value = (adcRawSatMode >> 12) & 7;
            st.acqSumNud.Value = conf[0x1d];
            st.acq1Nud.Value = conf[0x1e];

            //Soglia batteria
            st.batteryNud.Value = (((conf[0x20] * 256) + conf[0x21]) * 6.0) / 4096;

            //Passaggi magnete power off
            st.powerOffNud.Value = conf[0x22];

            //Remote tab
            RemoteTab rt = new RemoteTab();

            //Schedule e numero di intervalli per giorno
            DayTab dt;
            for (int i = 0x30; i < 0x91; i += 0x10)
            {
                //giorno target
                dt = ((DayTab)((TabItem)ScheduleTab.Items.GetItemAt((i / 0x10) - 3)).Content);
                // numero di intervalli
                dt.numeroIntervalliCB.SelectedIndex = conf[i] - 1;
                //orari
                dt.I1H2.SelectedItem = conf[(1 + i)].ToString("D2");
                dt.I2H2.SelectedItem = conf[(4 + i)].ToString("D2");
                dt.I3H2.SelectedItem = conf[(7 + i)].ToString("D2");
                dt.I4H2.SelectedItem = conf[(10 + i)].ToString("D2");
                dt.I5H2.SelectedItem = conf[(13 + i)].ToString("D2");
                //modi
                dt.I1M.SelectedIndex = conf[(2 + i)];
                dt.I2M.SelectedIndex = conf[(5 + i)];
                dt.I3M.SelectedIndex = conf[(8 + i)];
                dt.I4M.SelectedIndex = conf[(11 + i)];
                dt.I5M.SelectedIndex = conf[(14 + i)];
                //parametri
                dt.I1P.SelectedIndex = conf[(3 + i)];
                dt.I2P.SelectedIndex = conf[(6 + i)];
                dt.I3P.SelectedIndex = conf[(9 + i)];
                dt.I4P.SelectedIndex = conf[(12 + i)];
                dt.I5P.SelectedIndex = conf[(15 + i)];
            }

            ScheduleTab.Items.RemoveAt(7);
            ScheduleTab.Items.RemoveAt(7);
            var sti = new TabItem();
            sti.FontSize = 12;
            sti.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0xaa, 0xde));
            sti.Name = "Settings";
            sti.Header = "Settings";
            sti.Width = 65;
            sti.Height = 30;
            sti.Background = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30));
            sti.Margin = new Thickness(2);

            sti.Content = st;
            ScheduleTab.Items.Add(sti);

            var rti = new TabItem();
            rti.FontSize = 12;
            rti.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0xaa, 0xde));
            rti.Name = "Remote";
            rti.Header = "Remote";
            rti.Width = 65;
            rti.Height = 30;
            rti.Background = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30));
            rti.Margin = new Thickness(2);

            rti.Content = rt;
            ScheduleTab.Items.Add(rti);

            //MessageBox.Show("Configuration read.");
            var w = new Warning("Configuration succesfully read");
            w.ShowDialog();

        }

        private void sendButtonClick(object sender, RoutedEventArgs e)
        {
            byte[] conf = new byte[256];

            try
            {
                conf = unit.getGpsSchedule();
            }
            catch (Exception ex)
            {
                res = false;
                MessageBox.Show(ex.Message);
                this.Close();
                return;
            }

            GipsySettingsTab st = (GipsySettingsTab)(((TabItem)(ScheduleTab.Items.GetItemAt(7))).Content);

            //Acquisition On e Off time
            conf[0x10] = (byte)(((UInt16)st.onTimeNud.Value & 0xff00) >> 8);
            conf[0x11] = (byte)((UInt16)st.onTimeNud.Value & 0xff);
            conf[0x12] = (byte)(((UInt16)st.offTimeNud.Value & 0xff00) >> 8);
            conf[0x13] = (byte)((UInt16)st.offTimeNud.Value & 255);

            //Debug Mode
            conf[0x14] = 0;
            if (st.dDisabled.IsChecked == true)
            {
                conf[0x14] &= 0xf;
            }
            if (st.dEnabled.IsChecked == true)
            {
                conf[0x14] |= 0x10;
            }

            //Start delay
            if (st.ByTimeRB.IsChecked == true)
            {
                conf[0x14] += 1;
            }
            else if (st.ByDateRB.IsChecked == true)
            {
                conf[0x14] += 2;
            }
            conf[0x15] = (byte)(((DateTime)(st.SdDateTimePicker.Value)).Day);
            conf[0x16] = (byte)(((DateTime)(st.SdDateTimePicker.Value)).Month);
            conf[0x17] = (byte)(((DateTime)(st.SdDateTimePicker.Value)).Year - 2000);

            conf[0x18] = (byte)((uint)(st.startDelayNud.Value) >> 8);
            conf[0x19] = (byte)((uint)(st.startDelayNud.Value) & 0xff);

            //ADC
            byte adcMode = 0; //0x1a
            if (st.adcRecording.IsChecked == true)
            {
                adcMode |= 0x40;
            }
            if (st.adcTrigger.IsChecked == true)
            {
                adcMode |= 0x10;
            }
            if (st.magMinB.Content.Equals(">"))
            {
                adcMode += 1;
            }
            conf[0x1a] = adcMode;

            ushort adcRaw = (ushort)((uint)(st.nSatNud.Value) << 12);   //NSAT
            if (DailyCheck.IsChecked == true)                           //Weekly/Daily
            {
                adcRaw |= 0x8000;
            }
            adcRaw += (ushort)(st.ADCValueUD.Value);                    //ADC Raw Value
            conf[0x1b] = (byte)(adcRaw >> 8);
            conf[0x1c] = (byte)(adcRaw & 0xff);

            //Parametri low signal
            conf[0x1d] = (byte)st.acqSumNud.Value;
            conf[0x1e] = (byte)st.acq1Nud.Value;

            //Low Battery
            var rawBattery = (ushort)(st.batteryNud.Value * 4096 / 6);
            conf[0x20] = (byte)(rawBattery >> 8);
            conf[0x21] = (byte)(rawBattery & 0xff);

            //Passaggi magnete power off
            conf[0x22] = (byte)(st.powerOffNud.Value);

            DayTab dt;

            for (int i = 0x30; i < 0x91; i += 0x10)
            {
                //giorno target
                dt = ((DayTab)((TabItem)ScheduleTab.Items.GetItemAt((i / 0x10) - 3)).Content);

                //numero di intervalli
                conf[i] = (byte)(dt.numeroIntervalliCB.SelectedIndex + 1);

                //orari
                conf[i + 1] = byte.Parse((string)dt.I1H2.SelectedItem);
                conf[i + 4] = byte.Parse((string)dt.I2H2.SelectedItem);
                conf[i + 7] = byte.Parse((string)dt.I3H2.SelectedItem);
                conf[i + 10] = byte.Parse((string)dt.I4H2.SelectedItem);
                conf[i + 13] = byte.Parse((string)dt.I5H2.SelectedItem);

                //modi
                conf[i + 2] = (byte)dt.I1M.SelectedIndex;
                conf[i + 5] = (byte)dt.I2M.SelectedIndex;
                conf[i + 8] = (byte)dt.I3M.SelectedIndex;
                conf[i + 11] = (byte)dt.I4M.SelectedIndex;
                conf[i + 14] = (byte)dt.I5M.SelectedIndex;

                //parametri
                try { conf[i + 3] = (byte)dt.I1P.SelectedIndex; }
                catch { }
                try { conf[i + 6] = (byte)dt.I2P.SelectedIndex; }
                catch { }
                try { conf[i + 9] = (byte)dt.I3P.SelectedIndex; }
                catch { }
                try { conf[i + 12] = (byte)dt.I4P.SelectedIndex; }
                catch { }
                try { conf[i + 15] = (byte)dt.I5P.SelectedIndex; }
                catch { }
            }


            unit.setGpsSchedule(conf);


            var w = new Warning("Configuration succesfully updated.");
            w.ShowDialog();

            return;

        }

        private void resetClick(object sender, RoutedEventArgs e)
        {
            defaultSettings();
        }

        private void defaultSettings()
        {
            WeeklyCheck.IsChecked = true;
            UInt16 ccount = 1;
            //bool cHyde = true;
            foreach (TabItem t in ScheduleTab.Items)
            {
                //cHyde = false;
                //if (ccount == 7) cHyde = true;
                DayTab cc = new DayTab("Day " + ccount.ToString());
                cc.copyToNextDayEvent += gestioneCopiaDayTab;
                cc.Name = "dayTab" + ccount.ToString();
                cc.resetta();
                cc.copyToNextDayTB.Text = "Copy to day " + (ccount + 1).ToString() + " ->";
                if (ccount == 7)
                {
                    cc.copyToNextDayButton.IsEnabled = false;
                    cc.copyToNextDayButton.Visibility = Visibility.Hidden;
                }
                t.Content = cc;
                ccount++;
            }
            GipsySettingsTab tt = new GipsySettingsTab(this);
            tt.switchToAdc();

            ((TabItem)(ScheduleTab.Items.GetItemAt(7))).Content = tt;
            WeeklyCheck.IsChecked = true;

            RemoteTab rt = new RemoteTab();
            ((TabItem)(ScheduleTab.Items.GetItemAt(8))).Content = rt;
        }

        private void gestioneCopiaDayTab(object sender, string name)
        {
            string[] compStrAr = name.Split(' ');
            byte oldIndex = (byte)(byte.Parse(compStrAr[1]) - 1);
            var newIndex = oldIndex + 1;

            var oldDayTab = ((DayTab)((TabItem)(ScheduleTab.Items.GetItemAt(oldIndex))).Content);
            double[] orariD = oldDayTab.export();
            sbyte[] orariS = new sbyte[16];

            for (int i = 0; i <= (orariD.Length - 1); i++)
            {
                orariS[i] = (sbyte)(orariD[i]);
            }
            DayTab newDayTab = new DayTab("Day " + (newIndex + 1).ToString());
            newDayTab.Name = "dayTab" + (newIndex + 1).ToString();
            if (newIndex != 6)
            {
                newDayTab.copyToNextDayTB.Text = "Copy to day " + (newIndex + 2).ToString() + " ->";
                newDayTab.copyToNextDayEvent += gestioneCopiaDayTab;
            }
            else newDayTab.copyToNextDayButton.Visibility = Visibility.Hidden;
            newDayTab.import(orariS);

            ((TabItem)(ScheduleTab.Items.GetItemAt(newIndex))).Content = newDayTab;
            ScheduleTab.SelectedIndex = newIndex;
        }

        private void saveScheduleAs(object sender, RoutedEventArgs e)
        {
            MainWindow.lastSettings = System.IO.File.ReadAllLines(iniFile);
            var saveSchedule = new Microsoft.Win32.SaveFileDialog();

            if (MainWindow.lastSettings[1] != null) saveSchedule.InitialDirectory = MainWindow.lastSettings[1];
            else saveSchedule.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            saveSchedule.DefaultExt = ("Schedule Files|*.sch");
            saveSchedule.Filter = ("Schedule Files|*.sch");
            saveSchedule.OverwritePrompt = true;
            if ((bool)saveSchedule.ShowDialog())
            {
                exportSchedule(saveSchedule.FileName);
                MainWindow.lastSettings[1] = System.IO.Path.GetDirectoryName(saveSchedule.FileName);
                System.IO.File.WriteAllLines(iniFile, MainWindow.lastSettings);
            }

        }

        private void openSchedule(object sender, RoutedEventArgs e)
        {
            MainWindow.lastSettings = System.IO.File.ReadAllLines(iniFile);
            var openSchedule = new Microsoft.Win32.OpenFileDialog();
            if (MainWindow.lastSettings[2] != "null")
            {
                openSchedule.InitialDirectory = MainWindow.lastSettings[2];
            }
            else
            {
                openSchedule.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            openSchedule.DefaultExt = ("Schedule Files|*.sch");
            openSchedule.Filter = ("Schedule Files|*.sch");
            if ((bool)openSchedule.ShowDialog())
            {
                importSchedule(openSchedule.FileName);
                MainWindow.lastSettings[2] = System.IO.Path.GetDirectoryName(openSchedule.FileName);
                System.IO.File.WriteAllLines(iniFile, MainWindow.lastSettings);
            }
        }

        private bool importSchedule(string fileName)
        {
            System.IO.BinaryReader fi = new System.IO.BinaryReader(System.IO.File.Open(fileName, System.IO.FileMode.Open));
            string header = "";
            try
            {
                header = fi.ReadString();
            }
            catch
            {
                fi.Close();
                return false;
            }

            if ((header != "TSM-GIPS"))
            {
                fi.Close();
                return false;
            }

            double high = fi.ReadDouble();
            double low = fi.ReadDouble();
            if ((high < 3))
            {
                if ((low < 9))
                {
                    fi.Close();
                    return false;
                }
            }
            defaultSettings();

            double dw = fi.ReadDouble();
            sbyte[] schS = new sbyte[16];
            for (byte dayCount = 0; (dayCount <= 6); dayCount++)
            {
                for (int valCount = 0; (valCount <= 15); valCount++)
                {
                    schS[valCount] = (sbyte)(fi.ReadDouble());
                }
                ((DayTab)((TabItem)ScheduleTab.Items.GetItemAt(dayCount)).Content).import(schS);
            }

            
            double[] setD = new double[17];
            for (int valcount = 0; (valcount < 17); valcount++)
            {
                setD[valcount] = fi.ReadDouble();
            }


            if ((dw == 1)) DailyCheck.IsChecked = true;
            else WeeklyCheck.IsChecked = true;

            var settingTab = ((GipsySettingsTab)((TabItem)(ScheduleTab.Items.GetItemAt(7))).Content);
            settingTab.import(setD);

            fi.Close();

            return true;
        }

        private void exportSchedule(string fileName)
        {
            var fo = new System.IO.BinaryWriter(System.IO.File.Open(fileName, System.IO.FileMode.Create));

            string header = "TSM-GIPS";
            fo.Write(header);
            double dw = 2;
            fo.Write(dw);
            dw = 9;
            fo.Write(dw);
            dw = 0;
            if ((bool)DailyCheck.IsChecked) dw = 1;
            fo.Write(dw);

            double[] schedD = new double[16];
            for (int ccount = 0; ccount <= 6; ccount++)
            {
                var dt = ((DayTab)((TabItem)(ScheduleTab.Items.GetItemAt(ccount))).Content);
                schedD = dt.export();
                for (int dCount = 0; dCount <= 15; dCount++)
                {
                    fo.Write(schedD[dCount]);
                }
            }

            var dt2 = ((GipsySettingsTab)((TabItem)(ScheduleTab.Items.GetItemAt(7))).Content);
            double[] setD = dt2.export();
            foreach (double value in setD)
            {
                fo.Write(value);
            }
            fo.Close();
        }

        public new bool? ShowDialog()
        {
            base.ShowDialog();
            return res;
        }
    }
}
