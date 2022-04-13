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
		int on, off, sdt, nSat, gsv;
		List<TextBox> ctrAr;
		int[] maxs = new int[5] { 65000, 65000, 2147483647, 6, 90 };
		int[] mins = new int[5] { 10, 2, 0, 0, 0 };
		byte[] conf;
		DateTime date;

		public BasicsConfiguration(byte[] conf)
			: base()
		{
			InitializeComponent();

			ctrAr = new List<TextBox> { onTB, offTB, startDelayTimeTB, satTB, gsvTB };
			this.conf = conf;

			on = conf[33] * 256 + conf[32];
			off = conf[35] * 256 + conf[34];
			sdt = (conf[39] << 24) + (conf[38] << 16) + (conf[37] << 8) + conf[36];
			nSat = conf[44];
			gsv = conf[45];
			int anno = ((conf[41] * 256) + conf[40]);
			int mese = conf[42];
			int giorno = conf[43];
			date = new DateTime(anno, mese, giorno);

			onTB.Text = on.ToString();
			offTB.Text = off.ToString();
			startDelayTimeTB.Text = sdt.ToString();
			if (sdt > 0)
			{
				sdtCB.IsChecked = true;
			}
			else
			{
				startDelayTimeTB.Visibility = Visibility.Hidden;
				minLabel.Visibility = Visibility.Hidden;
			}
			startDelayDateDP.SelectedDate = date;
			if (anno > 2019)
			{
				sddCB.IsChecked = true;
			}
			else
			{
				startDelayDateDP.Visibility = Visibility.Hidden;
			}
			satTB.Text = nSat.ToString();
			gsvTB.Text = gsv.ToString();

		}

		private void sdtCB_Checked(object sender, RoutedEventArgs e)
		{
			if (sdtCB.IsChecked == false)
			{
				sdt = 0;
				startDelayTimeTB.Visibility = Visibility.Hidden;
				minLabel.Visibility = Visibility.Hidden;
			}
			else
			{
				startDelayTimeTB.Visibility = Visibility.Visible;
				minLabel.Visibility = Visibility.Visible;
				sdt = (conf[39] << 24) + (conf[38] << 16) + (conf[37] << 8) + conf[36];
				if (sdt == 0)
				{
					sdt = 360;
				}
				startDelayTimeTB.Text = sdt.ToString();
			}
		}

		private void sddCB_Checked(object sender, RoutedEventArgs e)
		{
			if (sddCB.IsChecked == false)
			{
				date = new DateTime(2019, 1, 1);
				startDelayDateDP.Visibility = Visibility.Hidden;
			}
			else
			{
				startDelayDateDP.Visibility = Visibility.Visible;
				int anno = (conf[41] * 256 + conf[40]);
				int mese = conf[42];
				int giorno = conf[43];
				date = new DateTime(anno, mese, giorno);
				startDelayDateDP.SelectedDate = date;
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
				date = (DateTime)startDelayDateDP.SelectedDate;
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
			MessageBox.Show(s.SelectedDate.ToString());
		}

		private void validate(TextBox s)
		{
			//TextBox s = (TextBox)sender;
			int index = 100;
			for (int i = 0; i < 5; i++)
			{
				if (s == ctrAr[i])
				{
					index = i;
					break;
				}
			}
			if (index == 100) return;
			int[] oldVal = new int[5] { on, off, sdt, nSat, gsv };
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
					on = newVal;
					break;
				case 1:
					off = newVal;
					break;
				case 2:
					sdt = newVal;
					break;
				case 3:
					nSat = newVal;
					break;
				case 4:
					gsv = newVal;
					break;
			}

		}
		
		public override void copyValues()
		{
			conf[33] = (byte)(on >> 8);
			conf[32] = (byte)(on & 0xff);
			conf[35] = (byte)(off >> 8);
			conf[34] = (byte)(off & 0xff);

			conf[39] = (byte)(sdt >> 24);
			conf[38] = (byte)((sdt >> 16) & 0xff);
			conf[37] = (byte)((sdt >> 8) & 0xff);
			conf[36] = (byte)(sdt & 0xff);
			conf[44] = (byte)nSat;
			conf[45] = (byte)gsv;

			conf[41] = (byte)(date.Year >> 8);
			conf[40] = (byte)(date.Year & 0xff);
			conf[42] = (byte)(date.Month);
			conf[43] = (byte)(date.Day);
		}

	}
}
