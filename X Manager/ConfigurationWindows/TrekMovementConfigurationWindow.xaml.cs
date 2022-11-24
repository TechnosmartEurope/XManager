using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.IO.Ports;
using X_Manager.Units;
using X_Manager.ConfigurationWindows;
using System.Windows.Shapes;
using X_Manager.Units.AxyTreks;

namespace X_Manager.ConfigurationWindows
{
	static class ExtensionsForWPF
	{
		public static System.Windows.Forms.Screen GetScreen(this Window window)
		{
			return System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(window).Handle);
		}
	}

	public partial class TrekMovementConfigurationWindow : ConfigurationWindow
	{

		public UInt32[] soglie = new UInt32[18];
		byte mDebug = 0;
		//byte unitType;
		UInt16[] c = new UInt16[7];
		//byte[] unitFirmware;
		//bool evitaSoglieDepth = false;
		int burstLenght;
		int burstBackUp;
		int burstPeriod;
		Timer reshapeTimer = null;
		FTDI_Device ft;
		Unit unit;
		ComboBox co2PeriodCB;
		public TrekMovementConfigurationWindow(byte[] axyconf, Unit unit)
			: base()
		{
			InitializeComponent();
			Loaded += loaded;
			mustWrite = false;
			axyConfOut = new byte[29];
			this.unit = unit;

			//this.sp = sp;
			ft = MainWindow.FTDI;

			for (int i = 2; i < 5; i++)
			{
				axyConfOut[i] = axyconf[i];
			}

			if (unit is AxyTrekFT)
			{
				externalGrid2.RowDefinitions[2].Height = new GridLength(74);
				sendButton.Margin = new Thickness(10);
				WSGrid.ColumnDefinitions[1].Width = new GridLength(0);
				WsHardwareRB.Visibility = Visibility.Hidden;
				WsHardwareRB.IsEnabled = false;
				WsEnabledRB.Content = "Enabled";
				WsDisabledRB.Margin = new Thickness(30, 0, 0, 0);
				WsEnabledRB.Margin = new Thickness(30, 0, 0, 0);
				TDgroupBox.Header = "DEPTH LOGGING";
				tdLoggingHeaderSP.Visibility = Visibility.Hidden;
				gridNN.Children.Remove(pressureCB);
				gridNN.Children.Remove(tdPeriodSP);
				TDgroupBox.Content = null;
				TDgroupBox.Content = pressureCB;
				TDgroupBox.Width = 198;
				tdGrid.HorizontalAlignment = HorizontalAlignment.Left;
				TDgroupBox.HorizontalAlignment = HorizontalAlignment.Left;
				var tpgroupBox = new GroupBox();
				tpgroupBox.Header = "T/D PERIOD";
				tpgroupBox.Foreground = new SolidColorBrush(Color.FromArgb(255, 0x00, 0xaa, 0xde));
				tpgroupBox.HorizontalAlignment = HorizontalAlignment.Right;
				tpgroupBox.Margin = new Thickness(0, 0, 10, 5);
				tpgroupBox.Width = 190;
				Grid.SetRow(tpgroupBox, 0);
				externalGrid2.Children.Add(tpgroupBox);
				tpgroupBox.Content = tdPeriodSP;
				tdPeriodSP.IsEnabled = true;
				tdGrid.RowDefinitions[0].Height = new GridLength(0);
			}
			else if (unit is AxyTrekCO2)
			{
				externalGrid2.RowDefinitions[2].Height = new GridLength(74);
				sendButton.Margin = new Thickness(10);
				WSGrid.ColumnDefinitions[1].Width = new GridLength(0);
				WsHardwareRB.Visibility = Visibility.Hidden;
				WsHardwareRB.IsEnabled = false;
				WsEnabledRB.Content = "Enabled";
				WsDisabledRB.Margin = new Thickness(30, 0, 0, 0);
				WsEnabledRB.Margin = new Thickness(30, 0, 0, 0);
				TDgroupBox.Header = "CO2 PERIOD";
				tdLoggingHeaderSP.Visibility = Visibility.Hidden;
				gridNN.Children.Remove(pressureCB);
				gridNN.Children.Remove(tdPeriodSP);
				TDgroupBox.Content = null;
				ComboBox co2PeriodCB = new ComboBox();
				string[] pp = new string[] { "5sec", "30sec", "1min", "5min", "10min", "20min", "30min", "40min", "50min", "60min" };
				co2PeriodCB.Height = 40;
				co2PeriodCB.Width = 90;
				co2PeriodCB.HorizontalAlignment = HorizontalAlignment.Left;
				co2PeriodCB.ItemsSource = pp;
				TDgroupBox.Content = co2PeriodCB;
				this.co2PeriodCB = co2PeriodCB;
				TDgroupBox.Width = 120;
				tdGrid.HorizontalAlignment = HorizontalAlignment.Left;
				TDgroupBox.HorizontalAlignment = HorizontalAlignment.Left;
				var tpgroupBox = new GroupBox();
				tpgroupBox.Header = "T/D PERIOD";
				tpgroupBox.Foreground = new SolidColorBrush(Color.FromArgb(255, 0x00, 0xaa, 0xde));
				tpgroupBox.HorizontalAlignment = HorizontalAlignment.Right;
				tpgroupBox.Margin = new Thickness(0, 0, 10, 5);
				tpgroupBox.Width = 260;
				Grid.SetRow(tpgroupBox, 0);
				externalGrid2.Children.Add(tpgroupBox);
				tdPeriodSP.Children.Add(pressureCB);
				tpgroupBox.Content = tdPeriodSP;
				tdPeriodSP.IsEnabled = true;
				tdGrid.RowDefinitions[0].Height = new GridLength(0);
			}
			else
			{

				if (unit.firmTotA < 2000001)
				{
					externalGrid2.RowDefinitions[2].Height = new GridLength(0);
					sendButton.Margin = new Thickness(25);
				}
				else if (unit.firmTotA < 3000000)
				{
					externalGrid2.RowDefinitions[2].Height = new GridLength(74);
					sendButton.Margin = new Thickness(10);
					WsHardwareRB.Visibility = Visibility.Hidden;
					WsHardwareRB.IsEnabled = false;
					WSGrid.ColumnDefinitions[1].Width = new GridLength(0);
					WsDisabledRB.Margin = new Thickness(30, 0, 0, 0);
					WsEnabledRB.Margin = new Thickness(30, 0, 0, 0);
				}
				else
				{
					externalGrid2.RowDefinitions[2].Height = new GridLength(74);
					sendButton.Margin = new Thickness(10);
					if (unit.firmTotA < 3001000)
					{
						WsEnabledRB.Content = "Software";
						WsHardwareRB.Content = "Hardware";
					}
					else
					{
						WSGrid.ColumnDefinitions[1].Width = new GridLength(0);
						WsHardwareRB.Visibility = Visibility.Hidden;
						WsHardwareRB.IsEnabled = false;
						WsEnabledRB.Content = "Enabled";
						WsDisabledRB.Margin = new Thickness(30, 0, 0, 0);
						WsEnabledRB.Margin = new Thickness(30, 0, 0, 0);
					}
				}

				if (unit.firmTotA < 3004000)
				{
					pressureCB.IsEnabled = false;
				}

				if (unit.firmTotA < 3008000)
				{
					externalGrid1.Children.RemoveAt(3);
					ghostRow.Height = new GridLength(0);
					Height = 710;
				}
			}

			foreach (RadioButton c in rates.Children)
			{
				try
				{
					c.IsChecked = false;
				}
				catch { }
			}

			movThreshUd.Value = axyconf[20];
			latencyThreshUd.Value = axyconf[21];

			switch (axyconf[15])
			{
				case 0:
				case 8:
					rate50RB.IsChecked = true;
					break;
				case 1:
				case 9:
					rate25RB.IsChecked = true;
					break;
				case 2:
				case 10:
					rate100RB.IsChecked = true;
					break;
				case 3:
				case 11:
					rate10RB.IsChecked = true;
					break;
				case 4:
				case 12:
					rate1RB.IsChecked = true;
					break;
				default:
					rate25RB.IsChecked = true;
					break;
			}

			bits8RB.IsChecked = true;
			bits10RB.IsChecked = false;
			if (axyconf[16] > 7)
			{
				bits8RB.IsChecked = false;
				bits10RB.IsChecked = true;
				axyconf[16] -= 8;
			}

			byte ccount = 0;
			foreach (RadioButton c in ranges.Children)
			{
				try
				{
					c.IsChecked = false;
					if (ccount == axyconf[16])
					{
						c.IsChecked = true;
					}

					ccount++;
				}
				catch
				{
				}

			}

			temperatureCB.IsChecked = false;
			pressureCB.IsChecked = false;
			tempDepthLogginUD.IsEnabled = false;
			if (axyconf[17] == 1)
			{
				temperatureCB.IsChecked = true;
			}

			if (unit.firmTotA < 3004000)
			{
				pressureCB.IsChecked = temperatureCB.IsChecked;
			}
			else
			{
				if (axyconf[18] == 1)
				{
					pressureCB.IsChecked = true;
				}
			}
			if ((bool)temperatureCB.IsChecked | (bool)pressureCB.IsChecked)
			{
				tempDepthLogginUD.IsEnabled = true;
				LogEnup.IsEnabled = true;
				LogEndown.IsEnabled = true;
			}
			else
			{
				tempDepthLogginUD.IsEnabled = false;
				LogEnup.IsEnabled = false;
				LogEndown.IsEnabled = false;
			}

			tempDepthLogginUD.Text = "";
			tempDepthLogginUD.Text = axyconf[19].ToString();

			if (unit is AxyTrekFT)
			{
				tempDepthLogginUD.IsEnabled = true;
				LogEndown.IsEnabled = true;
				LogEnup.IsEnabled = true;
			}
			else if (unit is AxyTrekCO2)
			{
				int co2Period = axyconf[3] * 256 + axyconf[4];
				int index = 1;
				switch (co2Period)
				{
					case 5:
						index = 0;
						break;
					case 30:
						index = 1;
						break;
					case 60:
						index = 2;
						break;
					case 300:
						index = 3;
						break;
					case 600:
						index = 4;
						break;
					case 1200:
						index = 5;
						break;
					case 1800:
						index = 6;
						break;
					case 2400:
						index = 7;
						break;
					case 3000:
						index = 8;
						break;
					case 3600:
						index = 9;
						break;
				}
				co2PeriodCB.SelectedIndex = index;
				if (axyconf[18] == 1)
				{
					pressureCB.IsChecked = true;
				}
				else
				{
					pressureCB.IsChecked = false;
				}
			}

			switch (axyconf[22])
			{
				case 0:
					sendButton.Content = "Send configuration";
					mDebug = 0;
					break;
				case 1:
					sendButton.Content = "Send configuration (d)";
					mDebug = 1;
					break;
			}
			if (unit.firmTotA > 2000000)
			{
				switch (axyconf[23])
				{
					case 0:
						WsDisabledRB.IsChecked = true;
						break;
					case 1:
						WsEnabledRB.IsChecked = true;
						break;
					case 2:
						WsHardwareRB.IsChecked = true;
						break;
					default:
						WsDisabledRB.IsChecked = true;
						break;
				}
			}

			if (unit.firmTotA >= 3008000)
			{
				contCB.IsChecked = false;

				burstBackUp = burstLenght = axyconf[25] * 256 + axyconf[26];
				burstPeriod = axyconf[27] * 256 + axyconf[28];
				burstlTB.Text = burstLenght.ToString();
				burstpTB.Text = burstPeriod.ToString();

				if (burstLenght == 0)
				{
					burstLenght = 2;
					burstlTB.Text = "2";
					contCB.IsChecked = true;
				}
			}

		}

		private void loaded(object sender, RoutedEventArgs e)
		{
			movThreshUd.header.Content = "Acceleration magnitude: ";
			movThreshUd.minValue = 0;
			movThreshUd.maxValue = 10;
			movThreshUd.roundDigits = 0;
			latencyThreshUd.minValue = 0;
			latencyThreshUd.maxValue = 40;
			latencyThreshUd.header.Content = "Latency time: ";
			latencyThreshUd.roundDigits = 0;

			reshapeTimer = new Timer(100);
			reshapeTimer.Elapsed += reshapeTimerElapsed;
			LocationChanged += locationChanged;
			reshape();
		}

		private void reshapeTimerElapsed(object sender, ElapsedEventArgs e)
		{
			Application.Current.Dispatcher.Invoke(() => reshape());
		}

		private void reshape()
		{
			System.Windows.Forms.Screen actScreen = ExtensionsForWPF.GetScreen(this);
			System.Drawing.Rectangle r = actScreen.Bounds;

			if (r.Height < 900)
			{
				Width = 800;
				Height = 560;
				generalSB.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
				generalSB.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
				generalSP.Orientation = Orientation.Horizontal;
			}
			else
			{
				generalSP.Orientation = Orientation.Vertical;
				Height = 790;
				Width = 460;
				generalSB.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
				generalSB.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
			}
		}

		private void locationChanged(object sender, EventArgs e)
		{
			reshapeTimer.Stop();
			reshapeTimer.Start();
		}

		private void setThresholdUds()
		{
			movValueChanged();
			latValueChanged();
		}

		private void movValueChanged()
		{
			byte range = 0;
			if ((bool)range2RB.IsChecked)
			{
				range = 4;
			}
			else if ((bool)range4RB.IsChecked)
			{
				range = 8;
			}
			else if ((bool)range8RB.IsChecked)
			{
				range = 16;
			}
			else if ((bool)range16RB.IsChecked)
			{
				range = 32;
			}

			movThresholdLabel.Content = (Math.Round((range / 256.0 * movThreshUd.Value), 4).ToString() + " g");
		}

		private void latValueChanged()
		{
			byte rate = 0;
			if ((bool)rate100RB.IsChecked)
			{
				rate = 100;
			}
			else if ((bool)rate10RB.IsChecked)
			{
				rate = 10;
				// ElseIf rate1RB.IsChecked Then
				// rate = 1
			}
			else if ((bool)rate25RB.IsChecked)
			{
				rate = 25;
			}
			else if ((bool)rate50RB.IsChecked)
			{
				rate = 50;
			}

			if ((unit.firmTotA < 3002000))
			{
				latThresholdLabel.Content = (Math.Round((1000
								/ (rate * (25 * latencyThreshUd.Value)))).ToString() + " ms");
			}
			else
			{
				latThresholdLabel.Content = "sec.";
			}
		}

		private void setThresholdUdsEvent(object sender, RoutedEventArgs e)
		{
			setThresholdUds();
		}

		private void movChangedEvent(object sender, RoutedEventArgs e)
		{
			movValueChanged();
		}

		private void latChangedEvent(object sender, RoutedEventArgs e)
		{
			latValueChanged();
		}

		private void cmdDown_Click(object sender, RoutedEventArgs e)
		{
			UInt16 i = UInt16.Parse(tempDepthLogginUD.Text);
			if ((i != 1)) i--;
			tempDepthLogginUD.Text = i.ToString();
		}

		private void cmdUp_Click(object sender, RoutedEventArgs e)
		{
			UInt16 i = UInt16.Parse(tempDepthLogginUD.Text);
			if (i != 5) i++;
			tempDepthLogginUD.Text = i.ToString();
		}

		private void tempDepthCBChecked(object sender, RoutedEventArgs e)
		{
			if (unit is AxyTrekFT) return;

			var c = (CheckBox)sender;

			if (unit.firmTotA < 3004000)
			{
				pressureCB.IsChecked = temperatureCB.IsChecked;
			}

			if (unit is AxyTrekCO2 && c.Name == "pressureCB")
			{
				tempDepthLogginUD.IsEnabled = (bool)pressureCB.IsChecked;
				LogEnup.IsEnabled = (bool)pressureCB.IsChecked;
				LogEndown.IsEnabled = (bool)pressureCB.IsChecked;
				return;
			}


			if (c.Name == "temperatureCB")
			{
				if (!(bool)temperatureCB.IsChecked)
				{
					pressureCB.IsChecked = false;
				}
			}
			else
			{
				if ((bool)pressureCB.IsChecked)
				{
					temperatureCB.IsChecked = true;
				}
			}

			if ((bool)temperatureCB.IsChecked | (bool)pressureCB.IsChecked)
			{
				tempDepthLogginUD.IsEnabled = true;
				LogEnup.IsEnabled = true;
				LogEndown.IsEnabled = true;
			}
			else
			{
				tempDepthLogginUD.IsEnabled = false;
				LogEnup.IsEnabled = false;
				LogEndown.IsEnabled = false;
			}

		}

		private void ctrlManager(object sender, KeyEventArgs e)
		{
			if ((e.Key == Key.Return))
			{
				sendConfiguration();
			}

			if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
			{
				if (e.Key == Key.D)
				{
					if (!(unit is Axy3))
					{
						if ((mDebug == 0))
						{
							mDebug = 1;
							sendButton.Content = "Send configuration (d)";
						}
						else
						{
							mDebug = 0;
							sendButton.Content = "Send configuration";
						}

					}

				}
				else if (e.Key == Key.C)
				{
					int[] coeffs = new int[14];
					ft.ReadExisting();
					if (unit is AxyTrekHD)
					{
						ft.Write("TTTTTTTTTTTTTTTGGAg");        //Importa i 14 byte da

						try
						{
							for (int i = 0; i < 12; i++)
							{
								coeffs[i] = ft.ReadByte();
							}
							if (unit.firmTotA > 3009000)
							{
								coeffs[12] = ft.ReadByte();
								coeffs[13] = ft.ReadByte();
							}
						}
						catch
						{
							return;
						}
					}
					else if (unit is AxyTrekFT)
					{
						ft.Write("TTTTTTTTTTTTTTTGGAg");

						try
						{
							for (int i = 0; i < 12; i++)
							{
								ft.ReadByte();
							}
							coeffs[0] = ft.ReadByte();
							coeffs[1] = ft.ReadByte();
							coeffs[2] = ft.ReadByte();
							coeffs[3] = ft.ReadByte();
						}
						catch
						{
							return;
						}
					}
					else
					{
						return;
					}

					DepthFastTrekHDFTCalibration qp = new DepthFastTrekHDFTCalibration(coeffs, unit);
					qp.ShowDialog();

					if (qp.mustWrite)
					{
						ft.Write("TTTTTTTTTTTTTTTGGAb");
						try
						{
							ft.ReadByte();

							if (unit is AxyTrekFT)
							{
								//Span-temperatura
								qp.tempSpan *= 1000;
								ft.Write(new byte[2] { (byte)((UInt16)qp.tempSpan >> 8), (byte)qp.tempSpan }, 0, 2);

								//Zero-temperatura
								qp.tempZero *= 1000;
								qp.tempZero += 32500;
								ft.Write(new byte[2] { (byte)((UInt16)qp.tempZero >> 8), (byte)qp.tempZero }, 0, 2);
							}
							else if (unit.firmTotA <= 3009000)
							{

								qp.pressThreshold = (1500 - qp.pressZero) * qp.pressSpan / 100000;// - qp.zero;
								qp.pressThreshold *= 55;
								qp.pressThreshold += 200;
								qp.pressThreshold /= 1000;
								qp.pressThreshold *= 32768;
								qp.pressThreshold /= 2.048;

								qp.pressSpan *= 1000;
								qp.pressZero += 32500;

								//Zero-pressione
								ft.Write(new byte[2] { (byte)((UInt16)qp.pressZero >> 8), (byte)qp.pressZero }, 0, 2);

								//Span-pressione
								ft.Write(new byte[2] { (byte)((UInt16)qp.pressSpan >> 8), (byte)qp.pressSpan }, 0, 2);

								//Threshold-pressione
								ft.Write(new byte[2] { (byte)((UInt16)qp.pressThreshold >> 8), (byte)qp.pressThreshold }, 0, 2);

								if (unit.firmTotA == 3009000)
								{
									//Zero-temperatura
									qp.tempZero *= 1000;
									qp.tempZero += 32500;
									ft.Write(new byte[2] { (byte)((UInt16)qp.tempZero >> 8), (byte)qp.tempZero }, 0, 2);

									//Span-temperatura
									qp.tempSpan *= 1000;
									ft.Write(new byte[2] { (byte)((UInt16)qp.tempSpan >> 8), (byte)qp.tempSpan }, 0, 2);
								}
							}
							else
							{
								//Span-temperatura
								qp.tempSpan *= 1000;
								ft.Write(new byte[2] { (byte)((UInt16)qp.tempSpan >> 8), (byte)qp.tempSpan }, 0, 2);

								//Zero-temperatura
								qp.tempZero *= 1000;
								qp.tempZero += 32500;
								ft.Write(new byte[2] { (byte)((UInt16)qp.tempZero >> 8), (byte)qp.tempZero }, 0, 2);

								//Span-pressione
								qp.pressSpan *= 100;
								ft.Write(new byte[2] { (byte)((UInt16)qp.pressSpan >> 8), (byte)qp.pressSpan }, 0, 2);

								//Zero-pressione
								qp.pressZero += 32500;
								ft.Write(new byte[2] { (byte)((UInt16)qp.pressZero >> 8), (byte)qp.pressZero }, 0, 2);

								//Tcoeff-pressione
								qp.pressTcoeff *= 100;
								qp.pressTcoeff += 32500;
								ft.Write(new byte[2] { (byte)((UInt16)qp.pressTcoeff >> 8), (byte)qp.pressTcoeff }, 0, 2);

								//Soglie temperatura + 2 byte fill
								ft.Write(new byte[2] { (byte)((UInt16)qp.tempOut[0] >> 8), (byte)qp.tempOut[0] }, 0, 2);
								ft.Write(new byte[2] { (byte)((UInt16)qp.tempOut[1] >> 8), (byte)qp.tempOut[1] }, 0, 2);
								ft.Write(new byte[2] { (byte)((UInt16)qp.tempOut[2] >> 8), (byte)qp.tempOut[2] }, 0, 2);
								ft.Write(new byte[2] { (byte)((UInt16)qp.tempOut[3] >> 8), (byte)qp.tempOut[3] }, 0, 2);
								ft.Write(new byte[2] { (byte)((UInt16)qp.tempOut[4] >> 8), (byte)qp.tempOut[4] }, 0, 2);
								ft.Write(new byte[2] { (byte)((UInt16)qp.tempOut[5] >> 8), (byte)qp.tempOut[5] }, 0, 2);
								ft.Write(new byte[2] { (byte)((UInt16)qp.tempOut[6] >> 8), (byte)qp.tempOut[6] }, 0, 2);
								ft.Write(new byte[2] { (byte)((UInt16)qp.tempOut[7] >> 8), (byte)qp.tempOut[7] }, 0, 2);

								//Soglie pressione
								ft.Write(new byte[2] { (byte)((UInt16)qp.pressOut[0] >> 8), (byte)qp.pressOut[0] }, 0, 2);
								ft.Write(new byte[2] { (byte)((UInt16)qp.pressOut[1] >> 8), (byte)qp.pressOut[1] }, 0, 2);
								ft.Write(new byte[2] { (byte)((UInt16)qp.pressOut[2] >> 8), (byte)qp.pressOut[2] }, 0, 2);
								ft.Write(new byte[2] { (byte)((UInt16)qp.pressOut[3] >> 8), (byte)qp.pressOut[3] }, 0, 2);
								ft.Write(new byte[2] { (byte)((UInt16)qp.pressOut[4] >> 8), (byte)qp.pressOut[4] }, 0, 2);
								ft.Write(new byte[2] { (byte)((UInt16)qp.pressOut[5] >> 8), (byte)qp.pressOut[5] }, 0, 2);
								ft.Write(new byte[2] { (byte)((UInt16)qp.pressOut[6] >> 8), (byte)qp.pressOut[6] }, 0, 2);
								ft.Write(new byte[2] { (byte)((UInt16)qp.pressOut[7] >> 8), (byte)qp.pressOut[7] }, 0, 2);
							}

							ft.ReadByte();
						}
						catch
						{
							MessageBox.Show("Unit not ready.");
						}
					}
				}
			}

		}

		private void sendConf(object sender, RoutedEventArgs e)
		{
			sendConfiguration();
		}

		private void sendConfiguration()
		{

			if (rate50RB.IsChecked == true)
			{
				axyConfOut[15] = 0;
			}
			else if (rate25RB.IsChecked == true)
			{
				axyConfOut[15] = 1;
			}
			else if (rate100RB.IsChecked == true)
			{
				axyConfOut[15] = 2;
			}
			else if (rate10RB.IsChecked == true)
			{
				axyConfOut[15] = 3;
			}
			else if (rate1RB.IsChecked == true)
			{
				axyConfOut[15] = 4;
			}

			byte ccount = 0;
			foreach (RadioButton c in ranges.Children)
			{
				try
				{
					if (c.IsChecked == true)
					{
						break;
					}

					ccount++;
				}
				catch { }
			}

			if ((bits10RB.IsChecked == true)) ccount += 8;

			axyConfOut[16] = ccount;
			axyConfOut[17] = 0;
			axyConfOut[18] = 0;
			if (temperatureCB.IsChecked == true)
			{
				if (!(unit is AxyTrekCO2) && !(unit is AxyTrekFT))
				{
					axyConfOut[17] = 1;
				}
			}
			if (pressureCB.IsChecked == true)
			{
				axyConfOut[18] = 1;
			}

			axyConfOut[19] = axyConfOut[20] = axyConfOut[21] = 0;
			try
			{
				axyConfOut[19] = Convert.ToByte(tempDepthLogginUD.Text);
			}
			catch { }
			try
			{
				axyConfOut[20] = (byte)movThreshUd.Value;
			}
			catch { }
			try
			{
				axyConfOut[21] = (byte)latencyThreshUd.Value;
			}
			catch { }

			axyConfOut[22] = mDebug;

			if ((unit.firmTotA > 2000000))
			{
				axyConfOut[23] = 0;
				if ((bool)WsEnabledRB.IsChecked)
				{
					axyConfOut[23] = 1;
				}

				if ((bool)WsHardwareRB.IsChecked)
				{
					axyConfOut[23] = 2;
				}
			}

			if (unit.firmTotA >= 3008000)
			{
				//axyConfOut[26] = (byte)burstPeriod;
				//axyConfOut[25] = (byte)burstLenght;
				//if ((bool)contCB.IsChecked)
				//{
				//	axyConfOut[25] = 0;
				//}

				axyConfOut[25] = (byte)(burstLenght >> 8);
				axyConfOut[26] = (byte)(burstLenght & 0xff);

				axyConfOut[27] = (byte)(burstPeriod >> 8);
				axyConfOut[28] = (byte)(burstPeriod & 0xff);
			}

			if (unit is AxyTrekCO2)
			{
				int perCo2 = 1800;
				switch (co2PeriodCB.SelectedIndex)
				{
					case 0:
						perCo2 = 5;
						break;
					case 1:
						perCo2 = 30;
						break;
					case 2:
						perCo2 = 60;
						break;
					case 3:
						perCo2 = 300;
						break;
					case 4:
						perCo2 = 600;
						break;
					case 5:
						perCo2 = 1200;
						break;
					case 6:
						perCo2 = 1800;
						break;
					case 7:
						perCo2 = 2400;
						break;
					case 8:
						perCo2 = 3000;
						break;
					case 9:
						perCo2 = 3600;
						break;
				}

				axyConfOut[3] = (byte)(perCo2 >> 8);
				axyConfOut[4] = (byte)(perCo2 & 0xff);

			}

			mustWrite = true;
			Close();
		}

		private void contChanged(object sender, RoutedEventArgs e)
		{
			if ((bool)contCB.IsChecked)
			{
				burstBackUp = burstLenght;
				burstLenght = 0;
				burstlTB.Text = "0";
				burstlTB.IsEnabled = false;
				burstpTB.IsEnabled = false;
			}
			else
			{
				burstLenght = burstBackUp;
				burstlTB.Text = burstLenght.ToString();
				burstlTB.IsEnabled = true;
				burstpTB.IsEnabled = true;
			}
		}

		private void burstLenghValidate(object sender, RoutedEventArgs e)
		{
			int l = burstLenght;
			int.TryParse(burstlTB.Text, out l);
			if ((l > 1) & (l < 50000) & (l < burstPeriod - 1))
			{
				burstLenght = l;
			}
			burstlTB.Text = burstLenght.ToString();
		}

		private void burstPeriodValidate(object sender, RoutedEventArgs e)
		{
			int p = burstPeriod;
			int.TryParse(burstpTB.Text, out p);
			if ((p > 3) & (p < 65000) & (p > burstLenght + 1))
			{
				burstPeriod = p;
			}
			burstpTB.Text = burstPeriod.ToString();
		}

		private void burstLenghtKey(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				e.Handled = true;
				burstLenghValidate(null, new RoutedEventArgs());
			}
		}

		private void burstPeriodKey(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				e.Handled = true;
				burstPeriodValidate(null, new RoutedEventArgs());
			}
		}

	}
}

