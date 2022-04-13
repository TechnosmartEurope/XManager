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
	/// Logica di interazione per BasicsConfiguration.xaml
	/// </summary>
	partial class BasicsConfiguration : PageCopy
	{
		int acqOn, acqOff, altOn, nSat, gsv;
		uint sdTime, sddDate;
		List<TextBox> ctrAr;
		int[] maxs = new int[6] { 65000, 65000, 65000, 8, 200, 0x7ffffff };
		int[] mins = new int[6] { 10, 2, 2, 0, 5, 0 };
		CheckBox[] remSchAr = new CheckBox[24];
		byte[] conf;
		DateTime sdDate;

		public BasicsConfiguration(byte[] conf)
			: base()
		{
			InitializeComponent();

			ctrAr = new List<TextBox> { acqOnTB, acqOffTB, altOnTB, nsatTB, gsvTB, startDelayTimeTB };
			this.conf = conf;

			acqOn = BitConverter.ToInt16(conf, 32);     //32-33
			acqOff = BitConverter.ToInt16(conf, 34);    //34-35
			altOn = BitConverter.ToInt16(conf, 36);     //36-37
			nSat = conf[38] & 0x3f;                     //38
			gsv = conf[39];                             //39
			sdTime = BitConverter.ToUInt32(conf, 40);    //40-43
			sddDate = BitConverter.ToUInt32(conf, 44);    //44-47

			//bool enableSdDate = true;
			//if (mese > 0x80)
			//{
			//	enableSdDate = false;
			//	mese -= 0x80;
			//}
			//sdDate = new DateTime(anno, mese, giorno);


			earlyStopCB.IsChecked = (conf[38] & 0x80) == 0x80;
			enhancedAccuracyCB.IsChecked = (conf[38] & 0x40) == 0x40;

			acqOnTB.Text = acqOn.ToString();
			acqOffTB.Text = acqOff.ToString();
			altOnTB.Text = altOn.ToString();
			nsatTB.Text = nSat.ToString();
			gsvTB.Text = gsv.ToString();

			if (sdTime > 0x7ffffff)
			{
				sdtCB.IsChecked = false;
				sdTime -= 0x80000000;
				startDelayTimeTB.Visibility = Visibility.Hidden;
				minLabel.Visibility = Visibility.Hidden;
			}
			else
			{
				sdtCB.IsChecked = true;
				startDelayTimeTB.Visibility = Visibility.Visible;
				minLabel.Visibility = Visibility.Visible;
			}
			startDelayTimeTB.Text = sdTime.ToString();

			if (sddDate > 0x7fffffff)
			{
				sddCB.IsChecked = false;
				sddDate -= 0x80000000;
				startDelayDateDP.Visibility = Visibility.Hidden;
			}
			else
			{
				sddCB.IsChecked = true;
				startDelayDateDP.Visibility = Visibility.Visible;
			}
			int anno = (int)(sddDate >> 16);
			int mese = (int)((sddDate >> 8) & 0xff);
			int giorno = (int)(sddDate & 0xff);
			sdDate = new DateTime(anno, mese, giorno);
			startDelayDateDP.SelectedDate = sdDate;

			for (int i = 0; i < 12; i++)
			{
				var v = new CheckBox();
				v.HorizontalContentAlignment = HorizontalAlignment.Left;
				v.VerticalContentAlignment = VerticalAlignment.Center;
				v.Content = (i + 1).ToString("00");
				Grid.SetRow(v, 15);
				v.Margin = new Thickness(20 + (i * 70), 0, 0, 0);
				maingGrid.Children.Add(v);
				remSchAr[i] = v;
			}
			for (int i = 0; i < 12; i++)
			{
				var v = new CheckBox();
				v.HorizontalContentAlignment = HorizontalAlignment.Left;
				v.VerticalContentAlignment = VerticalAlignment.Center;
				v.Content = (i + 13).ToString("00");
				Grid.SetRow(v, 16);
				v.Margin = new Thickness(20 + (i * 70), 0, 0, 0);
				maingGrid.Children.Add(v);
				remSchAr[i + 12] = v;
			}
			for (int i = 0; i < 24; i++)
			{
				if (conf[i + 516] == 1)
				{
					var cb = remSchAr[i];
					cb.IsChecked = true;
					;
				}
			}
		}

		private void sdtCB_Checked(object sender, RoutedEventArgs e)
		{
			if (sdtCB.IsChecked == false)
			{
				//sdTime = 0;
				startDelayTimeTB.Visibility = Visibility.Hidden;
				minLabel.Visibility = Visibility.Hidden;
			}
			else
			{
				startDelayTimeTB.Visibility = Visibility.Visible;
				minLabel.Visibility = Visibility.Visible;
				//sdTime = BitConverter.ToUInt32(conf, 40);
				////sdTime = (conf[40] << 24) + (conf[38] << 16) + (conf[37] << 8) + conf[36];
				//if (sdTime == 0)
				//{
				//	sdTime = 360;
				//}
				//startDelayTimeTB.Text = sdTime.ToString();
			}
		}

		private void sddCB_Checked(object sender, RoutedEventArgs e)
		{
			if (sddCB.IsChecked == false)
			{
				//sdDate = new DateTime(2019, 1, 1);
				startDelayDateDP.Visibility = Visibility.Hidden;
			}
			else
			{
				startDelayDateDP.Visibility = Visibility.Visible;
				//int anno = BitConverter.ToInt16(conf, 44);  //44-46
				//if (conf[46] > 0x80) conf[46] -= 0x80;
				//int mese = conf[46];
				//int giorno = conf[47];
				//sdDate = new DateTime(anno, mese, giorno);
				startDelayDateDP.SelectedDate = sdDate;
			}
		}

		private void validate(object sender, RoutedEventArgs e)
		{
			if (sender is TextBox)
			{
				validate((TextBox)sender);
			}
			else
			{
				validate((DatePicker)sender);
			}
		}

		private void startDelayDateDP_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
		{
			if (startDelayDateDP.SelectedDate != null)
			{
				sdDate = (DateTime)startDelayDateDP.SelectedDate;
			}
		}

		private void onTB_GotFocus(object sender, RoutedEventArgs e)
		{
			if (sender is TextBox)
			{
				((TextBox)sender).SelectAll();
			}
			else
			{
				((DatePicker)sender).IsDropDownOpen = true;
			}
		}

		private void validate(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				if (sender is TextBox)
				{
					validate((TextBox)sender);
				}
				else
				{
					validate((DatePicker)sender);
				}

			}
		}

		private void validate(DatePicker s)
		{
			//MessageBox.Show(s.SelectedDate.ToString());
		}

		private void validate(TextBox s)
		{
			//TextBox s = (TextBox)sender;
			int index = 100;
			for (int i = 0; i < 6; i++)
			{
				if (s == ctrAr[i])
				{
					index = i;
					break;
				}
			}
			if (index == 100) return;
			int[] oldVal = new int[6] { acqOn, acqOff, altOn, nSat, gsv, (int)sdTime };
			int newVal;
			if (!int.TryParse(s.Text, out newVal))
			{
				s.Text = oldVal[index].ToString();
				return;
			}
			if ((newVal > maxs[index]) | newVal < mins[index])
			{
				s.Text = oldVal[index].ToString();
				return;
			}
			switch (index)
			{
				case 0:
					acqOn = newVal;
					break;
				case 1:
					acqOff = newVal;
					break;
				case 2:
					altOn = newVal;
					break;
				case 3:
					nSat = newVal;
					break;
				case 4:
					gsv = newVal;
					break;
				case 5:
					sdTime = (uint)newVal;
					break;
			}

		}

		public override void copyValues()
		{
			conf[32] = (byte)(acqOn & 0xff);
			conf[33] = (byte)(acqOn >> 8);
			conf[34] = (byte)(acqOff & 0xff);
			conf[35] = (byte)(acqOff >> 8);

			conf[36] = (byte)(altOn & 0xff);
			conf[37] = (byte)(altOn >> 8);

			conf[38] = (byte)nSat;
			conf[39] = (byte)gsv;
			conf[38] &= 0x3f;
			if (earlyStopCB.IsChecked == true) conf[38] += 0x80;
			if (enhancedAccuracyCB.IsChecked == true) conf[38] += 0x40;

			if (!(bool)sdtCB.IsChecked)
			{
				sdTime += 0x80000000;
			}
			conf[40] = (byte)(sdTime & 0xff);
			conf[41] = (byte)((sdTime >> 8) & 0xff);
			conf[42] = (byte)((sdTime >> 16) & 0xff);
			conf[43] = (byte)(sdTime >> 24);

			uint ddate = (uint)((sdDate.Year << 16) + (sdDate.Month << 8) + sdDate.Day);
			if (!(bool)sddCB.IsChecked)
			{
				ddate += 0x80000000;
			}

			conf[44] = (byte)(ddate & 0xff);
			conf[45] = (byte)(ddate >> 8);
			conf[46] = (byte)(ddate >> 16);
			conf[47] = (byte)(ddate >> 24);

			for (int i = 0; i < 24; i++)
			{
				conf[i + 516] = (bool)remSchAr[i].IsChecked ? (byte)1 : (byte)0;
			}
		}

	}
}
