using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace X_Manager.ConfigurationWindows.GiPSy6
{
	/// <summary>
	/// Logica di interazione per BatteryConfiguration.xaml
	/// </summary>
	public partial class BatteryConfiguration : Window
	{
		byte[] conf;
		TextBox[] tbAr;
		double[] vAr;
		bool handled;
		bool update;
		double chargingCurrent;
		public BatteryConfiguration(byte[] conf)
		{
			InitializeComponent();
			this.conf = conf;
			Closing += closing;
			vAr = new double[5];
			tbAr = new TextBox[5] { startLogging, pauseLogging, resumeLogging, lowRf, lowPc };
			handled = false;
			update = false;
			for (int i = 0; i < tbAr.Length; i++)
			{
				vAr[i] = conf[544 + i * 2] + conf[545 + i * 2] * 256;
				vAr[i] = (vAr[i] * 6.0) / 4096;
				tbAr[i].Text = vAr[i].ToString("0.00", CultureInfo.InvariantCulture);
				tbAr[i].LostFocus += BatteryConfiguration_LostFocus;
			}
			chargingCurrent = (conf[56] * 80000 / 128) + 70;
			chargingCurrent = 1000000 / chargingCurrent;
			currentTB.Text = Math.Round(chargingCurrent, 2).ToString();
			currentTB.LostFocus += currentTB_LostFocus;
		}

		private void BatteryConfiguration_LostFocus(object sender, RoutedEventArgs e)
		{
			if (handled)
			{
				handled = false;
			}
			else
			{
				validate((TextBox)sender);
			}
		}

		private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter || e.Key == Key.Tab)
			{
				validate((TextBox)sender);
				handled = true;
			}
		}

		private void validate(TextBox tb)
		{
			int index = 0;
			for (int i = 0; i < vAr.Length; i++)
			{
				if (tb == tbAr[i])
				{
					index = i;
					break;
				}
			}
			tbAr[index].Text = tbAr[index].Text.Replace(',', '.');
			double newVal = 0;
			if (double.TryParse(tbAr[index].Text, NumberStyles.Float, CultureInfo.InvariantCulture, out newVal))
			{
				if (newVal > 2.5 && newVal < 4.2)
				{
					vAr[index] = newVal;
				}
			}
			tbAr[index].Text = vAr[index].ToString("0.00", CultureInfo.InvariantCulture);
		}

		private void localClick(object sender, RoutedEventArgs e)
		{
			tbAr[0].Text = "3.40"; vAr[0] = 3.40;
			tbAr[1].Text = "3.30"; vAr[1] = 3.30;
			tbAr[2].Text = "3.85"; vAr[2] = 3.85;
			tbAr[3].Text = "3.40"; vAr[3] = 3.40;
			tbAr[4].Text = "3.55"; vAr[4] = 3.55;
		}
		private void remoteClick(object sender, RoutedEventArgs e)
		{
			tbAr[0].Text = "3.65"; vAr[0] = 3.65;
			tbAr[1].Text = "3.65"; vAr[1] = 3.65;
			tbAr[2].Text = "3.85"; vAr[2] = 3.85;
			tbAr[3].Text = "3.40"; vAr[3] = 3.40;
			tbAr[4].Text = "3.55"; vAr[4] = 3.55;
		}
		private void Button_Click(object sender, RoutedEventArgs e)
		{
			update = true;
			Close();
		}

		private void closing(object sender, EventArgs e)
		{
			if (!update) return;

			UInt16 val;
			for (int i = 0; i < vAr.Length; i++)
			{
				vAr[i] *= 4096;
				vAr[i] /= 6;
				vAr[i] = Math.Round(vAr[i], 0);
				val = (UInt16)vAr[i];
				conf[544 + i * 2] = (byte)(val & 0xff);
				conf[545 + i * 2] = (byte)(val >> 8);
			}
			double curr = double.Parse(currentTB.Text);
			curr = 1000000 / curr;
			curr -= 70;
			curr /= 625;
			curr = Math.Round(curr, 0);
			conf[56] = (byte)curr;
		}

		private void currentTB_LostFocus(object sender, RoutedEventArgs e)
		{
			if (handled)
			{
				handled = false;
			}
			else
			{
				validateCurrent();
			}
		}

		private void currentTB_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter || e.Key == Key.Tab)
			{
				validateCurrent();
				handled = true;
			}
		}

		private void validateCurrent()
		{
			currentTB.Text = currentTB.Text.Replace(',', '.');
			double val = 0;
			if ((!double.TryParse(currentTB.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out val)) | (val < 16) | (val > 320))
			{
				val = chargingCurrent;
			}
			val = 1000000 / val;               //Resistenza in Ohm
			val -= 70;                      //Resistenza meno resistenza fissa di cursore
			val = Math.Round(val / 625, 0); //Valore intero del registro più vicino a quello desiderato
			val = (val * 625) + 70;         //Valore effettivo della resistenza in ohm
			chargingCurrent = 1000000 / val;            //Valore della corrente in milliOhm
			currentTB.Text = Math.Round(chargingCurrent, 2).ToString();
		}
	}
}
