using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Globalization;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.VisualBasic;

namespace X_Manager.ConfigurationWindows
{
	/// <summary>
	/// Logica di interazione per BasicsConfiguration.xaml
	/// </summary>
	partial class BasicsConfiguration : PageCopy
	{
		int acqOn, acqOff, altOn, nSat, gsv, remoteAddress;
		uint sdTime, sddDate;
		List<TextBox> ctrAr;
		int[] maxs = new int[6] { 65000, 65000, 65000, 8, 200, 0x7ffffff };
		int[] mins = new int[6] { 10, 2, 2, 0, 5, 0 };
		CheckBox[] remSchAr = new CheckBox[24];
		byte[] conf;
		DateTime sdDate;
		string oldAdd;
		bool isRemote = false;
		bool _lockRfAddress = false;
		uint firmware;

		public bool lockRfAddress
		{
			get
			{
				return _lockRfAddress;
			}
			set
			{
				_lockRfAddress = value;
				remoteAddressTB.IsEnabled = !value;
			}
		}

		public BasicsConfiguration(byte[] conf, uint fw)
					: base()
		{
			InitializeComponent();

			firmware = fw;

			ctrAr = new List<TextBox> { acqOnTB, acqOffTB, altOnTB, nsatTB, gsvTB, startDelayTimeTB };
			this.conf = conf;

			if (conf[58] == 0)
			{
				conf[58] = 15;
			}
			for (int i = 15; i <= 60; i += 5)
			{
				enAccSelCB.Items.Add(i.ToString());
			}
			enAccSelCB.SelectedItem = conf[58].ToString();
			if (firmware < 1004007)
			{
				enAccSelCB.IsEnabled = false;
				enAccSelCB.Visibility = Visibility.Hidden;
			}

			acqOn = BitConverter.ToInt16(conf, 32);     //32-33
			acqOff = BitConverter.ToInt16(conf, 34);    //34-35
			altOn = BitConverter.ToInt16(conf, 36);     //36-37
			nSat = conf[38] & 0x3f;                     //38
			gsv = conf[39];                             //39
			sdTime = BitConverter.ToUInt32(conf, 40);    //40-43
			sddDate = BitConverter.ToUInt32(conf, 44);    //44-47
			remoteAddress = (conf[541] << 16) + (conf[542] << 8) + (conf[543]);

			earlyStopCB.IsChecked = (conf[38] & 0x80) == 0x80;
			enhancedAccuracyCB.IsChecked = (conf[38] & 0x40) == 0x40;

			acqOnTB.Text = acqOn.ToString();
			acqOffTB.Text = acqOff.ToString();
			altOnTB.Text = altOn.ToString();
			nsatTB.Text = nSat.ToString();
			gsvTB.Text = gsv.ToString();
			remoteAddressTB.Text = remoteAddress.ToString();
			remoteAddressTB.PreviewKeyDown += remoteAddressTB_PreviewKeyDown;
			remoteAddressTB.TextChanged += remoteAddressTB_TextChanged;

			KeyDown += ctrlManager;

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
				todayB.Visibility = Visibility.Hidden; ;
			}
			else
			{
				sddCB.IsChecked = true;
				startDelayDateDP.Visibility = Visibility.Visible;
				todayB.Visibility = Visibility.Visible;
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
				v.Margin = new Thickness(20 + (i * 65), 0, 0, 0);
				v.Unchecked += remoteHourUnchecked;
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
				v.Margin = new Thickness(20 + (i * 65), 0, 0, 0);
				v.Unchecked += remoteHourUnchecked;
				maingGrid.Children.Add(v);
				remSchAr[i + 12] = v;
			}
			if (conf[540] == 1)
			{
				isRemote = true;
			}
			else
			{
				isRemote = false;
				remoteAddressTitleTB.Visibility = Visibility.Hidden;
				remoteAddressTB.Visibility = Visibility.Hidden;
				remoteScheduleTitleTB.Visibility = Visibility.Hidden;
				allOnB.Visibility = Visibility.Hidden;
				allOffB.Visibility = Visibility.Hidden;
			}
			for (int i = 0; i < 24; i++)
			{
				if (!isRemote)
				{
					conf[i + 516] = 0;
					remSchAr[i].Visibility = Visibility.Hidden;
				}
				if (conf[i + 516] == 1)
				{
					var cb = remSchAr[i];
					cb.IsChecked = true;
				}
			}
			if (conf[57] == 0)
			{
				debugEventsL.Text = "";
			}

			Loaded += loaded;
		}

		private void loaded(object sender, RoutedEventArgs e)
		{
			acqOnTB.Focus();
		}

		private void remoteHourUnchecked(object sender, RoutedEventArgs e)
		{
			CheckBox v = (CheckBox)sender;
			int check = 0;
			for (int i = 0; i < 24; i++)
			{
				if (remSchAr[i].IsChecked == true)
				{
					check++;
				}
			}
			if (check == 0)
			{
				MessageBox.Show("WARNING: at least one hour must be selected.");
				v.IsChecked = true;
			}

		}

		private void allOnClick(object sender, RoutedEventArgs e)
		{
			for (int i = 0; i < 24; i++)
			{
				remSchAr[i].IsChecked = true;
			}
		}

		private void allOffClick(object sender, RoutedEventArgs e)
		{
			for (int i = 0; i < 24; i++)
			{
				remSchAr[i].Unchecked -= remoteHourUnchecked;
				remSchAr[i].IsChecked = false;
				remSchAr[i].Unchecked += remoteHourUnchecked;
			}
			remSchAr[0].IsChecked = true;
		}

		private void ctrlManager(object sender, KeyEventArgs e)
		{
			if (Keyboard.Modifiers == ModifierKeys.Control)
			{
				if (e.Key == Key.L || e.Key == Key.R || e.Key == Key.B)
				{
					bool allowed = true;
					if (!MainWindow.adminUser)
					{
						allowed = false;
						string res = Interaction.InputBox("Insert password: ", "Password");
						if ((res != "cetriolo") && (res != "saji"))
						{
							MessageBox.Show("Wrong password.");
						}
						else
						{
							allowed = true;
						}
					}
					if (allowed)
					{
						MainWindow.adminUser = true;
						if (e.Key == Key.L)
						{
							isRemote = false;
							remoteAddressTitleTB.Visibility = Visibility.Hidden;
							remoteAddressTB.Visibility = Visibility.Hidden;
							remoteScheduleTitleTB.Visibility = Visibility.Hidden;
							allOnB.Visibility = Visibility.Hidden;
							allOffB.Visibility = Visibility.Hidden;
							for (int i = 0; i < 24; i++)
							{
								remSchAr[i].Visibility = Visibility.Hidden;
							}
							conf[544] = 0x11; conf[545] = 0x09;
							conf[546] = 0xcd; conf[547] = 0x08;
							conf[548] = 0x44; conf[549] = 0x0a;
							conf[550] = 0x11; conf[551] = 0x09;
							conf[552] = 0x77; conf[553] = 0x09;
							MessageBox.Show("The unit will be set as local.");
						}
						else if (e.Key == Key.R)
						{
							isRemote = true;
							remoteAddressTitleTB.Visibility = Visibility.Visible;
							remoteAddressTB.Visibility = Visibility.Visible;
							remoteScheduleTitleTB.Visibility = Visibility.Visible;
							allOnB.Visibility = Visibility.Visible;
							allOffB.Visibility = Visibility.Visible;
							int check = 0;
							for (int i = 0; i < 24; i++)
							{
								remSchAr[i].Visibility = Visibility.Visible;
								if (remSchAr[i].IsChecked == true)
								{
									check++;
								}
							}
							if (check == 0)
							{
								remSchAr[0].IsChecked = true;
							}
							conf[544] = 0xbc; conf[545] = 0x09;
							conf[546] = 0xbc; conf[547] = 0x09;
							conf[548] = 0x44; conf[549] = 0x0a;
							conf[550] = 0x11; conf[551] = 0x09;
							conf[552] = 0x77; conf[553] = 0x09;
							MessageBox.Show("The unit will be set as remote.");
						}
						else if (e.Key == Key.B)
						{
							var bc = new GiPSy6.BatteryConfiguration(conf);
							bc.ShowDialog();
						}
						e.Handled = true;
					}
					e.Handled = true;
				}
				else if (e.Key == Key.D)
				{
					if (conf[57] == 0)
					{
						conf[57] = 1;
						debugEventsL.Text = "(Debug Events ON)";
					}
					else
					{
						conf[57] = 0;
						debugEventsL.Text = "";
					}
					e.Handled = true;
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
			}
		}

		private void sddCB_Checked(object sender, RoutedEventArgs e)
		{
			if (sddCB.IsChecked == false)
			{
				startDelayDateDP.Visibility = Visibility.Hidden;
				todayB.Visibility = Visibility.Hidden;
			}
			else
			{
				startDelayDateDP.Visibility = Visibility.Visible;
				todayB.Visibility = Visibility.Visible;
				startDelayDateDP.SelectedDate = sdDate;
			}
		}

		private void todayBClick(object sender, RoutedEventArgs e)
		{
			sdDate = DateTime.Today;
			startDelayDateDP.SelectedDate = sdDate;
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

		private void remoteAddressTB_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				if (remoteAddressTB.Text == "")
				{
					remoteAddressTB.Text = "000000";

				}
			}
			//oldAdd = remoteAddressTB.Text;
		}

		private void remoteAddressTB_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (remoteAddressTB.Text == "") return;
			bool conv = false;
			char rs = 's';
			try
			{
				rs = remoteAddressTB.Text.Skip(1).Take(1).ToArray()[0];
			}
			catch { }
			if (rs == 'x' || rs == 'X')
			{
				string hexNum = remoteAddressTB.Text.Substring(2);
				if (hexNum == "")
				{
					oldAdd = remoteAddressTB.Text;
					return;
				}
				conv = Int32.TryParse(hexNum, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out remoteAddress);
			}
			else
			{
				conv = Int32.TryParse(remoteAddressTB.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out remoteAddress);
			}
			if (!conv || (remoteAddress == 0) || (remoteAddress >= 0xffff00))
			{
				remoteAddressTB.TextChanged -= remoteAddressTB_TextChanged;
				remoteAddressTB.Text = oldAdd;
				remoteAddressTB.TextChanged += remoteAddressTB_TextChanged;
			}
			else
			{
				//remoteAddressTB.Text = remoteAddress.ToString();
				oldAdd = remoteAddressTB.Text;
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

			conf[58] = byte.Parse(enAccSelCB.SelectedItem as string);

			for (int i = 0; i < 24; i++)
			{
				conf[i + 516] = (bool)remSchAr[i].IsChecked ? (byte)1 : (byte)0;
			}
			conf[540] = isRemote ? (byte)1 : (byte)0;
			char rs = 't';
			try
			{
				rs = remoteAddressTB.Text.Skip(1).Take(1).ToArray()[0];
			}
			catch { }
			if (rs == 'x' || rs == 'X')
			{
				string s = remoteAddressTB.Text.Substring(2);
				if (s.Length > 0)
				{
					Int32.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out remoteAddress);
				}
			}
			else
			{
				if (remoteAddressTB.Text.Length > 0)
				{
					Int32.TryParse(remoteAddressTB.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out remoteAddress);
				}
			}
			conf[541] = (byte)(remoteAddress >> 16);
			conf[542] = (byte)(remoteAddress >> 8);
			conf[543] = (byte)(remoteAddress & 0xff);
		}

	}
}
