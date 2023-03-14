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
		int acqOn, acqOff, altOn, nSat, gsv, remoteAddress, pxInterval, pxFirst, pxLast;
		uint sdTime, sddDate;
		List<TextBox> ctrAr;
		static readonly int[] maxs = new int[7] { 65000, 65000, 65000, 8, 200, 0x7ffffff, 60 };
		static readonly int[] mins = new int[7] { 10, 2, 2, 0, 5, 0, 1 };

		//CheckBox[] remSchAr = new CheckBox[24];
		byte[] conf;
		DateTime sdDate;
		string[] oldAdd;
		bool isRemote = false;
		bool _lockRfAddress = false;
		Units.Unit unit;
		FrameworkElement[] remoteControls;
		FrameworkElement[] proximityControls;
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

		public BasicsConfiguration(byte[] conf, Units.Unit unit)
					: base()
		{
			InitializeComponent();

			remoteHB.oneAtLeast = true;
			proximityHB.oneAtLeast = false;
			oldAdd = new string[3];

			this.unit = unit;

			ctrAr = new List<TextBox> { acqOnTB, acqOffTB, altOnTB, nsatTB, gsvTB, startDelayTimeTB, pxIntervalTB };

			remoteControls = new FrameworkElement[] { remoteScheduleTitleTB, remoteScheduleTB, proximityTB, remoteHB, proximityHB, rfAddressTB, pxIntervalLabel, remoteAddressTB,
														pxIntervalTB, rfAddressesSP};
			proximityControls = new FrameworkElement[] { proximityTB, proximityHB, pxIntervalLabel, pxIntervalTB, rfAddressesSP };
			this.conf = conf;

			//Secondi enhanced accuracy (indirizzo 57 gipsy6xs, 58 gipsy6n)
			byte enAccPointer = 57;
			if (unit is Units.Gipsy6.Gipsy6N)
			{
				enAccPointer++;
			}
			//Controllo di sicurezza per valore non impostato
			if (conf[enAccPointer] == 0)
			{
				conf[enAccPointer] = 15;
			}
			//Riempimento combobox enhanced accuracy con valori possibili
			for (int i = 15; i <= 60; i += 5)
			{
				enAccSelCB.Items.Add(i.ToString());
			}
			//Selezione valore in memoria unità connessa
			enAccSelCB.SelectedItem = conf[enAccPointer].ToString();
			if (unit.firmTotA < 1004007)        //Per firmware precedenti la funzionalità viene disabilitata
			{
				enAccSelCB.IsEnabled = false;
				enAccSelCB.Visibility = Visibility.Hidden;
			}
			//Parametri di navigazione gps
			acqOn = BitConverter.ToInt16(conf, 32);     //32-33
			acqOff = BitConverter.ToInt16(conf, 34);    //34-35
			altOn = BitConverter.ToInt16(conf, 36);     //36-37
			nSat = conf[38] & 0x3f;                     //38
			gsv = conf[39];                             //39
			sdTime = BitConverter.ToUInt32(conf, 40);    //40-43
			sddDate = BitConverter.ToUInt32(conf, 44);    //44-47
			earlyStopCB.IsChecked = (conf[38] & 0x80) == 0x80;
			enhancedAccuracyCB.IsChecked = (conf[38] & 0x40) == 0x40;
			acqOnTB.Text = acqOn.ToString();
			acqOffTB.Text = acqOff.ToString();
			altOnTB.Text = altOn.ToString();
			nsatTB.Text = nSat.ToString();
			gsvTB.Text = gsv.ToString();

			//Per unità gipsy6n carica indirizzo remoto e parametri di prossimità
			if (unit is Units.Gipsy6.Gipsy6N)
			{
				remoteAddress = (conf[541] << 16) + (conf[542] << 8) + (conf[543]);
				pxInterval = conf[523];
				pxFirst = (conf[524] << 16) + (conf[525] << 8) + conf[526];
				pxLast = (conf[527] << 16) + (conf[528] << 8) + conf[529];
				pxIntervalTB.Text = pxInterval.ToString();
				remoteAddressTB.Text = remoteAddress.ToString();
				remoteAddressTB.PreviewKeyDown += rfAddressTB_PreviewKeyDown;
				remoteAddressTB.TextChanged += rfAddressTB_TextChanged;
				pxFirstTB.Text = pxFirst.ToString();
				pxFirstTB.PreviewKeyDown += rfAddressTB_PreviewKeyDown;
				pxFirstTB.TextChanged += rfAddressTB_TextChanged;
				pxLastTB.Text = pxLast.ToString();
				pxLastTB.PreviewKeyDown += rfAddressTB_PreviewKeyDown;
				pxLastTB.TextChanged += rfAddressTB_TextChanged;
				oldAdd[0] = remoteAddressTB.Text;
				oldAdd[1] = pxFirstTB.Text;
				oldAdd[2] = pxLastTB.Text;
			}

			KeyDown += ctrlManager;

			//Imposta le caselle di sart delay
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

			if (unit is Units.Gipsy6.Gipsy6N && conf[540] == 1)
			{
				isRemote = true;
				if (unit.firmTotA < 1005000)
				{
					foreach (FrameworkElement fe in proximityControls)
					{
						fe.Visibility = Visibility.Hidden;
					}
				}
			}
			else
			{
				isRemote = false;
				foreach (FrameworkElement fe in remoteControls)
				{
					fe.Visibility = Visibility.Hidden;
				}
			}

			if (unit is Units.Gipsy6.Gipsy6N)
			{
				if (isRemote)
				{
					if (conf[516] == 0xff)
					{
						byte[] rSch = conf.Skip(517).Take(3).ToArray();
						byte[] pSch = conf.Skip(520).Take(3).ToArray();
						remoteHB.setStatus(rSch);
						proximityHB.setStatus(pSch);
					}
					else
					{
						remoteHB.setStatus(conf.Skip(516).Take(24).ToArray());
						proximityHB.setStatus(new byte[24]);
					}
					//proximityPowerCB.SelectedIndex = conf[59];
					var sb = (sbyte)conf[59];
					string sbs = sb.ToString() + "dBm";
					int ind = 0;
					foreach (ComboBoxItem it in proximityPowerCB.Items)
					{
						if (it.Content.Equals(sbs))
						{
							break;
						}
						ind++;
					}
					proximityPowerCB.SelectedIndex = ind;

				}
				else
				{

					byte schMode = conf[516];
					for (int i = 516; i < 540; i++)
					{
						conf[i] = 0;
					}
					if (schMode == 0xff)
					{
						conf[516] = 0xff;
					}

				}
			}
			//Spostamento puntatore al flug degli eventi di debug
			enAccPointer--;
			if (conf[enAccPointer] == 0)
			{
				debugEventsL.Text = "";
			}

			Loaded += loaded;
		}

		private void loaded(object sender, RoutedEventArgs e)
		{
			acqOnTB.Focus();
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
							foreach (FrameworkElement fe in remoteControls)
							{
								fe.Visibility = Visibility.Hidden;
							}
							if (unit is Units.Gipsy6.Gipsy6N)
							{
								byte schMode = conf[516];
								for (int i = 516; i < 540; i++)
								{
									conf[i] = 0;
								}
								if (schMode == 0xff)
								{
									conf[516] = 0xff;
								}
								conf[544] = 0x11; conf[545] = 0x09;
								conf[546] = 0xcd; conf[547] = 0x08;
								conf[548] = 0xab; conf[549] = 0x0a;
								conf[550] = 0x00; conf[551] = 0x08;
								conf[552] = 0x00; conf[553] = 0x08;
								MessageBox.Show("The unit will be set as local.");
							}
							else
							{
								MessageBox.Show("Gipsy6 XS can be only local.");
							}

						}
						else if (e.Key == Key.R)
						{
							if (unit is Units.Gipsy6.Gipsy6XS)
							{
								MessageBox.Show("Gipsy6 XS can be only local.");
								return;
							}
							isRemote = true;
							foreach (FrameworkElement fe in remoteControls)
							{
								fe.Visibility = Visibility.Visible;
							}
							//remoteScheduleTitleTB.Visibility = Visibility.Visible;
							//remoteAddressTitleTB.Visibility = Visibility.Visible;
							//remoteAddressTB.Visibility = Visibility.Visible;
							//remoteScheduleTB.Visibility = Visibility.Visible;
							//remoteHB.Visibility = Visibility.Visible;
							if (unit.firmTotA < 1005000)
							{
								foreach (FrameworkElement fe in proximityControls)
								{
									fe.Visibility = Visibility.Hidden;
								}
								//proximityTB.Visibility = Visibility.Visible;
								//proximityHB.Visibility = Visibility.Visible;
							}
							remoteHB.allOn();
							proximityHB.allOff();

							conf[544] = 0xbc; conf[545] = 0x09;
							conf[546] = 0xbc; conf[547] = 0x09;
							conf[548] = 0x44; conf[549] = 0x0a;
							conf[550] = 0x11; conf[551] = 0x09;
							conf[552] = 0x77; conf[553] = 0x09;
							MessageBox.Show("The unit will be set as remote.");
						}
						else if (e.Key == Key.B)
						{
							var bc = new GiPSy6.BatteryConfiguration(conf, unit);
							bc.ShowDialog();
						}
						e.Handled = true;
					}
					e.Handled = true;
				}
				else if (e.Key == Key.D)
				{
					byte debugEventsPointer = 56;
					if (unit is Units.Gipsy6.Gipsy6N)
					{
						debugEventsPointer++;
					}
					if (conf[debugEventsPointer] == 0)
					{
						conf[debugEventsPointer] = 1;
						debugEventsL.Text = "(Debug Events ON)";
					}
					else
					{
						conf[debugEventsPointer] = 0;
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

		private void rfAddressTB_PreviewKeyDown(object sender, KeyEventArgs e)
		{

			if (e.Key == Key.Enter)
			{
				if ((sender as TextBox).Text == "")
				{
					(sender as TextBox).Text = "000000";

				}
			}
			//oldAdd = remoteAddressTB.Text;
		}

		private void rfAddressTB_TextChanged(object sender, TextChangedEventArgs e)
		{
			int newRfAddress = 0;
			int index = 100;
			if ((sender as TextBox).Name == "remoteAddressTB") index = 0;
			if ((sender as TextBox).Name == "pxFirstTB") index = 1;
			if ((sender as TextBox).Name == "pxLastTB") index = 2;
			if ((sender as TextBox).Text == "") return;
			if ((sender as TextBox).Text == "0") return;
			bool conv = false;
			char rs = 's';
			try
			{
				rs = (sender as TextBox).Text.Skip(1).Take(1).ToArray()[0];
			}
			catch { }
			if (rs == 'x' || rs == 'X')
			{
				string hexNum = (sender as TextBox).Text.Substring(2);
				if (hexNum == "")
				{
					oldAdd[index] = (sender as TextBox).Text;
					return;
				}
				conv = int.TryParse(hexNum, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out newRfAddress);
			}
			else
			{
				conv = int.TryParse((sender as TextBox).Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out newRfAddress);
			}
			if (!conv || (newRfAddress == 0) || (newRfAddress >= 0xffff00))
			{
				(sender as TextBox).TextChanged -= rfAddressTB_TextChanged;
				(sender as TextBox).Text = oldAdd[index];
				(sender as TextBox).TextChanged += rfAddressTB_TextChanged;
				newRfAddress = Int32.Parse(oldAdd[index]);
			}
			else
			{
				//remoteAddressTB.Text = remoteAddress.ToString();
				oldAdd[index] = (sender as TextBox).Text;
			}
			switch (index)
			{
				case 0:
					remoteAddress = newRfAddress;
					break;
				case 1:
					pxFirst = newRfAddress;
					break;
				case 2:
					pxLast = newRfAddress;
					break;
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
			for (int i = 0; i < 7; i++)
			{
				if (s == ctrAr[i])
				{
					index = i;
					break;
				}
			}
			if (index == 100) return;
			int[] oldVal = new int[7] { acqOn, acqOff, altOn, nSat, gsv, (int)sdTime, pxInterval };
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
				case 6:
					pxInterval = newVal;
					if (pxInterval >= 60)
					{
						pxInterval = 60;
					}
					else if (pxInterval > 30)
					{
						pxInterval = 30;
					}
					else if (pxInterval == 0)
					{
						pxInterval = 1;
					}
					else
					{
						bool valueValid = false;
						int units = pxLast - pxFirst + 1;
						int tempPxInterval = pxInterval;
						while (!valueValid)
						{
							while (60 % tempPxInterval != 0)
							{
								tempPxInterval--;
							}
							if (units * 2 >= tempPxInterval * 60)
							{
								pxInterval++;
								tempPxInterval = pxInterval;
							}
							else
							{
								valueValid = true;
							}
						}
					}
					pxIntervalTB.Text = pxInterval.ToString();
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

			if (unit is Units.Gipsy6.Gipsy6N)
			{
				copyValues_N();
			}
			else
			{
				copyValues_XS();
			}
		}

		private void copyValues_N()
		{
			conf[58] = byte.Parse(enAccSelCB.SelectedItem as string);
			conf[59] = (byte)sbyte.Parse(proximityPowerCB.Text.Substring(0, proximityPowerCB.Text.Length - 3));

			if (unit.firmTotA >= 1005000)
			{
				conf[516] = 0xff;
				Array.Copy(remoteHB.getStatus(GiPSy6.HourBar.MODE.MODE_NEW), 0, conf, 517, 3);
				Array.Copy(proximityHB.getStatus(GiPSy6.HourBar.MODE.MODE_NEW), 0, conf, 520, 3);
				conf[523] = byte.Parse(pxIntervalTB.Text);

				conf[524] = (byte)(pxFirst >> 16);
				conf[525] = (byte)(pxFirst >> 8);
				conf[526] = (byte)(pxFirst & 0xff);

				conf[527] = (byte)(pxLast >> 16);
				conf[528] = (byte)(pxLast >> 8);
				conf[529] = (byte)(pxLast & 0xff);
			}
			else
			{
				Array.Copy(remoteHB.getStatus(GiPSy6.HourBar.MODE.MODE_OLD), 0, conf, 516, 24);
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
				if (remoteAddressTB.Text.Length > 0 && remoteAddressTB.Text != "0")
				{
					int.TryParse(remoteAddressTB.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out remoteAddress);
				}
			}
			conf[541] = (byte)(remoteAddress >> 16);
			conf[542] = (byte)(remoteAddress >> 8);
			conf[543] = (byte)(remoteAddress & 0xff);
		}

		private void copyValues_XS()
		{
			conf[57] = byte.Parse(enAccSelCB.SelectedItem as string);
		}

	}
}
