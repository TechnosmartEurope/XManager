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
		bool? solar = null;
		bool? remote = null;
		bool batterySelected = false;
		Button[] battAr;
		SolidColorBrush ciano;
		//SolidColorBrush nero;
		//SolidColorBrush grigio;
		SolidColorBrush bianco;


		static readonly Double[][] thRP = {  new Double[] { 3.75, 3.7, 3.85, 3.4, 3.55, 3.8 },	//10mAh remoti / pann
											new Double[] { 3.7, 3.7, 3.85, 3.4, 3.5, 3.75 },		//20-35mAh
											new Double[] { 3.7, 3.65, 3.85, 3.4, 3.5, 3.7 },		//40/50mAH
											new Double[] { 3.65, 3.60, 3.85, 3.4, 3.5, 3.7 },	//70-90mAH
											new Double[] { 3.65, 3.60, 3.85, 3.35, 3.5, 3.7 },	//110-300mAH
											new Double[] { 3.60, 3.55, 3.85, 3.35, 3.5, 3.60 },	//400-650mAH
											new Double[] { 3.55, 3.5, 3.85, 3.4, 3.5, 3.55 } };  //1200+mAH

		static readonly Double[][] thRN = {   new Double[] { 3.75, 3.5, 3.85, 3.4, 3.55, 3.8 },	//10mAh remoti / no pann
											new Double[] { 3.7, 3.5, 3.85, 3.4, 3.5, 3.75 },		//20-35mAh
											new Double[] { 3.7, 3.45, 3.85, 3.4, 3.5, 3.7 },		//40/50mAH
											new Double[] { 3.65, 3.45, 3.85, 3.4, 3.5, 3.65 },	//70-90mAH
											new Double[] { 3.65, 3.40, 3.85, 3.35, 3.5, 3.65 },	//110-300mAH
											new Double[] { 3.60, 3.35, 3.85, 3.35, 3.5, 3.60 },	//400-650mAH
											new Double[] { 3.55, 3.30, 3.85, 3.35, 3.5, 3.55 } };  //1200+mAH

		static readonly Double[][] thLP = {  new Double[]  { 3.75, 3.7, 3.85, 3.4, 3.3, 3.3 },	//10mAh locali / pann
											new Double[] { 3.7, 3.7, 3.85, 3.4, 3.3, 3.3 },		//20-35mAh
											new Double[] { 3.7, 3.65, 3.85, 3.4, 3.3, 3.3 },		//40/50mAH
											new Double[] { 3.65, 3.60, 3.85, 3.4, 3.3, 3.3 },	//70-90mAH
											new Double[] { 3.65, 3.60, 3.85, 3.35, 3.3, 3.3 },	//110-300mAH
											new Double[] { 3.60, 3.55, 3.85, 3.35, 3.3, 3.3 },	//400-650mAH
											new Double[] { 3.55, 3.5, 3.85, 3.35, 3.3, 3.3 } };  //1200+mAH

		static readonly Double[][] thLN = {   new Double[] { 3.75, 3.5, 3.85, 3.4, 3.3, 3.3 },	//10mAh locali / no pann
											new Double[] { 3.7, 3.5, 3.85, 3.4, 3.3, 3.3 },		//20-35mAh
											new Double[] { 3.7, 3.45, 3.85, 3.4, 3.3, 3.3 },		//40/50mAH
											new Double[] { 3.65, 3.45, 3.85, 3.4, 3.3, 3.3 },	//70-90mAH
											new Double[] { 3.65, 3.40, 3.85, 3.35, 3.3, 3.3 },	//110-300mAH
											new Double[] { 3.60, 3.35, 3.85, 3.35, 3.3, 3.3 },	//400-650mAH
											new Double[] { 3.55, 3.30, 3.85, 3.35, 3.3, 3.3 } };  //1200+mAH

		public BatteryConfiguration(byte[] conf)
		{
			InitializeComponent();

			ciano = new SolidColorBrush(Color.FromArgb(0xff, 0x00, 0xaa, 0xde));
			bianco = new SolidColorBrush(Color.FromArgb(0xff, 0xba, 0xba, 0xba));

			this.conf = conf;
			Closing += closing;
			vAr = new double[6];
			tbAr = new TextBox[6] { startLogging, pauseLogging, resumeLogging, lowRfStart, lowRfLogging, lowRfRefuse };
			battAr = new Button[] { b1, b2, b3, b4, b5, b6, b7 };
			handled = false;
			update = false;
			for (int i = 0; i < tbAr.Length; i++)
			{
				if (i == tbAr.Length - 1)
				{
					vAr[i] = conf[118] + conf[119] * 256;
				}
				else
				{
					vAr[i] = conf[544 + i * 2] + conf[545 + i * 2] * 256;
				}
				vAr[i] = Math.Round(vAr[i] * 6.0 / 4096, 2);
				tbAr[i].Text = vAr[i].ToString("0.00", CultureInfo.InvariantCulture);
				tbAr[i].LostFocus += BatteryConfiguration_LostFocus;
			}

			clearButtons();

			chargingCurrent = (conf[56] * 80000 / 128) + 70;
			chargingCurrent = 1000000 / chargingCurrent;
			currentTB.Text = Math.Round(chargingCurrent, 2).ToString();
			currentTB.LostFocus += currentTB_LostFocus;
		}

		private void clearButtons()
		{
			presetsGB.Foreground = bianco;
			remoteB.Foreground = bianco;
			localB.Foreground = bianco;
			panelNoB.Foreground = bianco;
			panelYesB.Foreground = bianco;


			Button[] buttons = new Button[] { b1, b2, b3, b4, b5, b6, b7 };
			for (int i = 0; i < buttons.Length; i++)
			{
				buttons[i].Foreground = bianco;
			}

			solar = null;
			remote = null;
			batterySelected = false;
		}

		private void bxClick(object sender, RoutedEventArgs e)
		{
			foreach (Button bt in battAr)
			{
				bt.Foreground = bianco;
			}
			var b = sender as Button;
			batterySelected = true;
			b.Foreground = ciano;
			if (remote != null && solar != null)
			{
				presetsGB.Foreground = ciano;
				fillVolts();
			}
		}

		private void localClick(object sender, RoutedEventArgs e)
		{
			remoteB.Foreground = bianco;
			localB.Foreground = ciano;
			remote = false;
			if (batterySelected && solar != null)
			{
				presetsGB.Foreground = ciano;
				fillVolts();
			}
		}
		private void remoteClick(object sender, RoutedEventArgs e)
		{
			remoteB.Foreground = ciano;
			localB.Foreground = bianco;
			remote = true;
			if (batterySelected && solar != null)
			{
				presetsGB.Foreground = ciano;
				fillVolts();
			}
		}
		private void panelNoClick(object sender, RoutedEventArgs e)
		{
			panelNoB.Foreground = ciano;
			panelYesB.Foreground = bianco;
			solar = false;
			if (batterySelected && remote != null)
			{
				presetsGB.Foreground = ciano;
				fillVolts();
			}
		}

		private void panelYesClick(object sender, RoutedEventArgs e)
		{
			panelNoB.Foreground = bianco;
			panelYesB.Foreground = ciano;
			solar = true;
			if (batterySelected && remote != null)
			{
				presetsGB.Foreground = ciano;
				fillVolts();
			}
		}
		private void fillVolts()
		{
			var arrss = new List<Double[][]>() { thRP, thRN, thLP, thLN };
			int index = 0;
			if (remote == false)
			{
				index += 2;
			}
			if (solar == false)
			{
				index += 1;
			}
			var arrs = arrss[index];
			Button[] buttons = new Button[] { b1, b2, b3, b4, b5, b6, b7 };
			int jindex = 0;
			for (int i = 0; i < buttons.Length; i++)
			{
				if (buttons[i].Foreground == ciano)
				{
					jindex = i;
					break;
				}
			}
			var arr = arrs[jindex];
			for (int i = 0; i < tbAr.Length; i++)
			{
				vAr[i] = arr[i];
				tbAr[i].Text = vAr[i].ToString("0.00", CultureInfo.InvariantCulture);
			}

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
					clearButtons();
					vAr[index] = newVal;
				}
			}
			tbAr[index].Text = vAr[index].ToString("0.00", CultureInfo.InvariantCulture);
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
			for (int i = 0; i < vAr.Length ; i++)
			{
				vAr[i] *= 4096;
				vAr[i] /= 6;
				vAr[i] = Math.Round(vAr[i], 0);
				val = (UInt16)vAr[i];
				if (i == tbAr.Length - 1)
				{
					conf[118] = (byte)(val & 0xff);
					conf[119] = (byte)(val >> 8);
				}
				else
				{
					conf[544 + i * 2] = (byte)(val & 0xff);
					conf[545 + i * 2] = (byte)(val >> 8);
				}
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
