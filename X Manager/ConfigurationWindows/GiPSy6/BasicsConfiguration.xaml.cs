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
		int acqOn, acqOff, altOn, nSat, gsv, sdTime;
		List<TextBox> ctrAr;
		int[] maxs = new int[6] { 65000, 65000, 65000, 8, 200, 0x7ffffff0 };
		int[] mins = new int[6] { 10, 2, 2, 0, 5, 10 };
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
			nSat = conf[38] & 0x7f;                     //38
			gsv = conf[39];                             //39
			sdTime = BitConverter.ToInt32(conf, 40);    //40-43
			int anno = BitConverter.ToInt16(conf, 44);  //44-46
			int mese = conf[46];
			int giorno = conf[47];
			sdDate = new DateTime(anno, mese, giorno);

			gsvCB.IsChecked = (conf[38] & 0x80) == 0x80;
			nsatTB.IsEnabled = (conf[38] & 0x80) == 0x80;
			gsvTB.IsEnabled = (conf[38] & 0x80) == 0x80;

			acqOnTB.Text = acqOn.ToString();
			acqOffTB.Text = acqOff.ToString();
			altOnTB.Text = altOn.ToString();
			nsatTB.Text = nSat.ToString();
			gsvTB.Text = gsv.ToString();

			startDelayTimeTB.Text = sdTime.ToString();
			if (sdTime > 0)
			{
				sdtCB.IsChecked = true;
			}
			else
			{
				startDelayTimeTB.Visibility = Visibility.Hidden;
				minLabel.Visibility = Visibility.Hidden;
			}
			startDelayDateDP.SelectedDate = sdDate;
			if (anno > 2019)
			{
				sddCB.IsChecked = true;
			}
			else
			{
				startDelayDateDP.Visibility = Visibility.Hidden;
			}
		}

		private void sdtCB_Checked(object sender, RoutedEventArgs e)
		{
			if (sdtCB.IsChecked == false)
			{
				sdTime = 0;
				startDelayTimeTB.Visibility = Visibility.Hidden;
				minLabel.Visibility = Visibility.Hidden;
			}
			else
			{
				startDelayTimeTB.Visibility = Visibility.Visible;
				minLabel.Visibility = Visibility.Visible;
				sdTime = BitConverter.ToInt32(conf, 40);
				//sdTime = (conf[40] << 24) + (conf[38] << 16) + (conf[37] << 8) + conf[36];
				if (sdTime == 0)
				{
					sdTime = 360;
				}
				startDelayTimeTB.Text = sdTime.ToString();
			}
		}

		private void sddCB_Checked(object sender, RoutedEventArgs e)
		{
			if (sddCB.IsChecked == false)
			{
				sdDate = new DateTime(2019, 1, 1);
				startDelayDateDP.Visibility = Visibility.Hidden;
			}
			else
			{
				startDelayDateDP.Visibility = Visibility.Visible;
				int anno = (conf[44] * 256 + conf[45]);
				int mese = conf[46];
				int giorno = conf[47];
				sdDate = new DateTime(anno, mese, giorno);
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

		private void gsvCB_UnChecked(object sender, RoutedEventArgs e)
		{
			nsatTB.IsEnabled = (bool)gsvCB.IsChecked;
			gsvTB.IsEnabled = (bool)gsvCB.IsChecked;
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
			MessageBox.Show(s.SelectedDate.ToString());
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
			int[] oldVal = new int[6] { acqOn, acqOff, altOn, nSat, gsv, sdTime };
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
					sdTime = newVal;
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
			if (gsvCB.IsChecked == true) conf[38] += 0x80;

			conf[40] = (byte)(sdTime & 0xff);
			conf[41] = (byte)((sdTime >> 8) & 0xff);
			conf[42] = (byte)((sdTime >> 16) & 0xff);
			conf[43] = (byte)(sdTime >> 24);

			conf[44] = (byte)(sdDate.Year & 0xff);
			conf[45] = (byte)(sdDate.Year >> 8);
			conf[46] = (byte)(sdDate.Month);
			conf[47] = (byte)(sdDate.Day);
		}

	}
}
