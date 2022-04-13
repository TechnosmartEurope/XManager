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
    /// Interaction logic for DayTab.xaml
    /// </summary>
    public partial class DayTab : UserControl
    {
        bool comboEnable = true;
        bool nintEnable = true;
        string[] orari = new string[29];
        string[] nint = new string[5];
        string[] modi = new string[7];
        string[] modiShortPeriods = new string[5];
        string[] modiLongPeriods = new string[9];
        string[] modiVeryLongPeriods = new string[7];
        string[] modiMultiFix = new string[18];
        //string headName;
        public event EventHandler<string> copyToNextDayEvent;
        public DayTab(string header)
        {
            InitializeComponent();
            //this.copyToNextDayEvent(this, "");
            dayLabel.Content = header;
            // costruzione numero di intervalli
            for (byte i = 0; (i <= 4); i++)
            {
                nint[i] = (i + 1).ToString();
            }
            numeroIntervalliCB.ItemsSource = nint;

            // costruzione orari
            for (byte i = 0; (i <= 28); i++)
            {
                orari[i] = i.ToString("00");
            }

            // costruzione collezione modi 
            modi[0] = "Off";
            modi[1] = "Continuous";
            modi[2] = "Short periods";
            modi[3] = "Long periods";
            modi[4] = "Very Long periods";
            modi[5] = "MultiFix";
            modi[6] = "Subsecond";

            //costruzione collezione modi shortperiods
            modiShortPeriods[0] = " 1 fix / 1 sec (Power Save)";
            modiShortPeriods[1] = "1 fix / 2 secs";
            modiShortPeriods[2] = "1 fix / 3 secs";
            modiShortPeriods[3] = "1 fix / 4 secs";
            modiShortPeriods[4] = "1 fix / 5 secs";

            //costruzione modi GA
            modiLongPeriods[0] = "1 fix / 10 secs";
            modiLongPeriods[1] = "1 fix / 20 secs";
            modiLongPeriods[2] = "1 fix / 30 secs";
            modiLongPeriods[3] = "1 fix / 1 minute";
            modiLongPeriods[4] = "1 fix / 3 minutes";
            modiLongPeriods[5] = "1 fix / 5 minutes";
            modiLongPeriods[6] = "1 fix / 10 minutes";
            modiLongPeriods[7] = "1 fix / 15 minutes";
            modiLongPeriods[8] = "1 fix / 20 minutes";

            //costruzione modi GALong
            modiVeryLongPeriods[0] = "1 fix / 30 minutes";
            modiVeryLongPeriods[1] = "1 fix / 1 hour";
            modiVeryLongPeriods[2] = "1 fix / 1:30 hours";
            modiVeryLongPeriods[3] = "1 fix / 2 hours";
            modiVeryLongPeriods[4] = "1 fix / 4 hours";
            modiVeryLongPeriods[5] = "1 fix / 6 hours";
            modiVeryLongPeriods[6] = "1 fix / 12 hours";

            //costruzione modi MultiFix
            modiMultiFix[0] = "5 fix / 5 minutes";
            modiMultiFix[1] = "10 fix / 5 minutes";
            modiMultiFix[2] = "15 fix / 5 minutes";

            modiMultiFix[3] = "5 fix / 10 minutes";
            modiMultiFix[4] = "10 fix / 10 minutes";
            modiMultiFix[5] = "15 fix / 10 minutes";

            modiMultiFix[6] = "5 fix / 15 minutes";
            modiMultiFix[7] = "10 fix / 15 minutes";
            modiMultiFix[8] = "15 fix / 15 minutes";

            modiMultiFix[9] = "5 fix / 30 minutes";
            modiMultiFix[10] = "10 fix / 30 minutes";
            modiMultiFix[11] = "15 fix / 30 minutes";

            modiMultiFix[12] = "5 fix / 1 hour";
            modiMultiFix[13] = "10 fix / 1 hour";
            modiMultiFix[14] = "15 fix / 1 hour";

            modiMultiFix[15] = "5 fix / 6 hours";
            modiMultiFix[16] = "10 fix / 6 hours";
            modiMultiFix[17] = "15 fix / 6 hours";

            foreach (ComboBox c in orari2SP.Children)
            {
                c.ItemsSource = orari;
            }

            foreach (ComboBox c in modiSP.Children)
            {
                c.ItemsSource = modi;
                c.SelectedIndex = 1;
            }

            I1H1.Text = "00";
            numeroIntervalliCB.SelectedItem = "5";
        }

        private void modiSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox modiCb = (ComboBox)(modiSP.FindName(((ComboBox)sender).Name));
            ComboBox parametriCb = (ComboBox)(parametriSP.FindName(((ComboBox)sender).Name.Substring(0, 2) + "P"));
            parametriCb.IsEnabled = false;
            parametriCb.IsEnabled = true;
            switch (modiCb.SelectedIndex)
            {
                case 0:
                    Array[] nullAr = new Array[0];
                    parametriCb.ItemsSource = nullAr;
                    parametriCb.IsEnabled = false;
                    break;
                case 1:
                    Array[] nullAr2 = new Array[0];
                    parametriCb.ItemsSource = nullAr2;
                    parametriCb.IsEnabled = false;
                    break;
                case 2:
                    parametriCb.ItemsSource = modiShortPeriods;
                    break;
                case 3:
                    parametriCb.ItemsSource = modiLongPeriods;
                    break;
                case 4:
                    parametriCb.ItemsSource = modiVeryLongPeriods;
                    break;
                case 5:
                    parametriCb.ItemsSource = modiMultiFix;
                    break;
            }
            if ((bool)parametriCb.IsEnabled) parametriCb.SelectedIndex = 0;
        }

        private void riassegnaOrariSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!comboEnable) return;
            else comboEnable = false;

            byte[] orari = new byte[5];
            orari[0] = byte.Parse((string)(I1H2.SelectedItem));
            orari[1] = byte.Parse((string)(I2H2.SelectedItem));
            orari[2] = byte.Parse((string)(I3H2.SelectedItem));
            orari[3] = byte.Parse((string)(I4H2.SelectedItem));
            orari[4] = byte.Parse((string)(I5H2.SelectedItem));
            cambiaOrari(orari);
        }

        private void numeroIntervalliSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((nintEnable == false)) return;
            byte b = (byte)(24 - numeroIntervalliCB.SelectedIndex);

            cambiaOrari(new byte[] { b, (byte)(b + 1), (byte)(b + 2), (byte)(b + 3), (byte)(b + 4) });
        }

        private void copyToNextDayButtonClick(object sender, RoutedEventArgs e)
        {
            this.copyToNextDayEvent(this, (string)dayLabel.Content);
        }

        public void import(sbyte[] inn)
        {
            I1M.SelectedIndex = inn[6];
            I2M.SelectedIndex = inn[7];
            I3M.SelectedIndex = inn[8];
            I4M.SelectedIndex = inn[9];
            I5M.SelectedIndex = inn[10];

            I1P.SelectedIndex = inn[11];
            I2P.SelectedIndex = inn[12];
            I3P.SelectedIndex = inn[13];
            I4P.SelectedIndex = inn[14];
            I5P.SelectedIndex = inn[15];

            byte[] orari = new byte[] { (byte)(inn[1]), (byte)(inn[2]), (byte)(inn[3]), (byte)(inn[4]), (byte)(inn[5]) };
            cambiaOrari(orari);
        }

        public double[] export()
        {
            double[] outt = new double[16];
            outt[0] = numeroIntervalliCB.SelectedIndex;

            outt[1] = double.Parse((string)I1H2.SelectedItem);
            outt[2] = double.Parse((string)I2H2.SelectedItem);
            outt[3] = double.Parse((string)I3H2.SelectedItem);
            outt[4] = double.Parse((string)I4H2.SelectedItem);
            outt[5] = double.Parse((string)I5H2.SelectedItem);

            outt[6] = (byte)I1M.SelectedIndex;
            outt[7] = (byte)I2M.SelectedIndex;
            outt[8] = (byte)I3M.SelectedIndex;
            outt[9] = (byte)I4M.SelectedIndex;
            outt[10] = (byte)I5M.SelectedIndex;

            byte count = 1;
            foreach (ComboBox cb in parametriSP.Children)
            {
                outt[count + 10] = (byte)(cb.SelectedIndex);
                count++;
            }

            return outt;

        }

        public void resetta()
        {
            numeroIntervalliCB.SelectedIndex = 0;
            ComboBox[] modi = new ComboBox[] { I1M, I2M, I3M, I4M, I5M };
            for (int interv = 0; interv <= 4; interv++)
            {
                modi[interv].SelectedItem = 1;
            }
        }

        private void cambiaOrari(byte[] nuoviOrari)
        {
            byte[] orariAr = nuoviOrari;
            ComboBox[] orariCb = new ComboBox[] { I1H2, I2H2, I3H2, I4H2, I5H2 };
            byte[] orariFissiAr = new byte[]{0, 0, 0, 0, 0};
            TextBox[] orariFissiTb = new TextBox[] { I1H1, I2H1, I3H1, I4H1, I5H1 };
            ComboBox[] modi = new ComboBox[] { I1M, I2M, I3M, I4M, I5M };
            ComboBox[] parametri = new ComboBox[] { I1P, I2P, I3P, I4P, I5P };
            sbyte inizio = 0;
            sbyte fine = 0;
            sbyte noInt = 0;
            comboEnable = false;

            I1H2.ItemsSource = null;
            I2H2.ItemsSource = null;
            I3H2.ItemsSource = null;
            I4H2.ItemsSource = null;
            I5H2.ItemsSource = null;
            I1H2.Items.Clear();
            I2H2.Items.Clear();
            I3H2.Items.Clear();
            I4H2.Items.Clear();
            I5H2.Items.Clear();

            nintEnable = false;

            for (byte intervallo = 0; intervallo <= 4; intervallo++)
            {
                switch (intervallo)
                {
                    case 0:
                        inizio = 1;
                        fine = (sbyte)(orariAr[1] - 1);
                        break;
                    case 4:
                        inizio = (sbyte)(orariAr[3] + 1);
                        fine = (sbyte)orariAr[4];
                        break;
                    default:
                        inizio = (sbyte)(orariAr[intervallo - 1] + 1);
                        fine = (sbyte)(orariAr[intervallo + 1] - 1);
                        break;
                }

                for (sbyte orario = inizio; (orario <= fine); orario++)
                {
                    orariCb[intervallo].Items.Add(orario.ToString("00"));
                    //string f = String.Format("{00}",orario); output = "1"
                    //f = String.Format("{##}", orario); ERRORE
                    //f = orario.ToString("00"); output = "01"
                    //f = orario.ToString("##"); output = "1"
                }
                orariCb[intervallo].SelectedItem = orariAr[intervallo].ToString("00");

                //sviluppo
                //MessageBox.Show(dayLabel.Content.ToString());
                ////sviluppo
                if (intervallo != 4) orariFissiAr[intervallo + 1] = byte.Parse((string)orariCb[intervallo].SelectedItem);

                orariFissiTb[intervallo].Text =orariFissiAr[intervallo].ToString("00");

                if (int.Parse((string)orariCb[intervallo].SelectedItem) == 24) orariCb[intervallo].IsEnabled = false;
                else orariCb[intervallo].IsEnabled = true;

                Visibility v = new Visibility();
                if (int.Parse((string)orariCb[intervallo].SelectedItem) > 24) v = Visibility.Hidden;
                else
                {
                    noInt += 1;
                    v = Visibility.Visible;
                }

                orariCb[intervallo].Visibility = v;
                orariFissiTb[intervallo].Visibility = v;
                modi[intervallo].Visibility = v;
                parametri[intervallo].Visibility = v;

                numeroIntervalliCB.SelectedIndex = noInt - 1;
            }
            comboEnable = true;
            nintEnable = true;
        }

    }


}
