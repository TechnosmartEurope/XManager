using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
//using System.Windows.Forms;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.IO.Ports;
using X_Manager.Units;
using X_Manager.ConfigurationWindows;
using System.Windows.Shapes;

namespace X_Manager.ConfigurationWindows
{
	static class ExtensionsForWPF
	{
		public static System.Windows.Forms.Screen GetScreen(this Window window)
		{
			return System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(window).Handle);
		}
	}

	/// <summary>
	/// Interaction logic for TrekMovementConfigurationWindow.xaml
	/// </summary>
	public partial class TrekMovementConfigurationWindow : ConfigurationWindow
	{

		//public bool mustWrite = false;
		public UInt32[] soglie = new UInt32[18];
		byte mDebug = 0;
		byte unitType;
		UInt16[] c = new UInt16[7];
		//byte[] unitFirmware;
		UInt32 firmTotA;
		//bool evitaSoglieDepth = false;
		int burstLenght;
		int burstBackUp;
		int burstPeriod;
		Timer reshapeTimer = null;
		SerialPort sp;

		public TrekMovementConfigurationWindow(byte[] axyconf, UInt32 unitFirm, ref SerialPort sp)
			: base()
		{
			InitializeComponent();
			Loaded += loaded;
			mustWrite = false;
			axyConfOut = new byte[29];
			unitType = axyconf[0];
			firmTotA = unitFirm;

			this.sp = sp;

			if ((firmTotA < 2000001))
			{
				externalGrid2.RowDefinitions[2].Height = new System.Windows.GridLength(0);
				sendButton.Margin = new Thickness(25);
			}
			else if ((firmTotA < 3000000))
			{
				externalGrid2.RowDefinitions[2].Height = new System.Windows.GridLength(74);
				sendButton.Margin = new Thickness(10);
				WsHardwareRB.Visibility = Visibility.Hidden;
				WsHardwareRB.IsEnabled = false;
				WSGrid.ColumnDefinitions[1].Width = new GridLength(0);
				WsDisabledRB.Margin = new Thickness(30, 0, 0, 0);
				WsEnabledRB.Margin = new Thickness(30, 0, 0, 0);
			}
			else
			{
				externalGrid2.RowDefinitions[2].Height = new System.Windows.GridLength(74);
				sendButton.Margin = new Thickness(10);
				if ((firmTotA < 3001000))
				{
					WsEnabledRB.Content = "Software";
					WsHardwareRB.Content = "Hardware";
				}
				else
				{
					WSGrid.ColumnDefinitions[1].Width = new System.Windows.GridLength(0);
					WsHardwareRB.Visibility = Visibility.Hidden;
					WsHardwareRB.IsEnabled = false;
					WsEnabledRB.Content = "Enabled";
					WsDisabledRB.Margin = new Thickness(30, 0, 0, 0);
					WsEnabledRB.Margin = new Thickness(30, 0, 0, 0);
				}
			}
			if (firmTotA < 3004000)
			{
				pressureCB.IsEnabled = false;
			}

			if (firmTotA < 3008000)
			{
				externalGrid1.Children.RemoveAt(3);
				ghostRow.Height = new GridLength(0);
				Height = 710;
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
					if ((ccount == axyconf[16]))
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
				//tempDepthLogginUD.IsEnabled = true;
			}
			if (firmTotA < 3004000)
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
			if (firmTotA > 2000000)
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

			if (firmTotA >= 3008000)
			{
				contCB.IsChecked = false;
				//burstpTB.Text = axyconf[26].ToString();
				//burstlTB.Text = axyconf[25].ToString();
				//burstLenght = axyconf[25];
				//burstPeriod = axyconf[26];

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

		private void loaded(object sender, System.Windows.RoutedEventArgs e)
		{
			movThreshUd.header.Content = "Acceleration magnitude: ";
			movThreshUd.minValue = 0;
			movThreshUd.maxValue = 10;
			movThreshUd.roundDigits = 0;
			latencyThreshUd.minValue = 0;
			latencyThreshUd.maxValue = 40;
			latencyThreshUd.header.Content = "Latency time: ";
			latencyThreshUd.roundDigits = 0;
			if ((unitType == Unit.model_axyDepth) || (unitType == Unit.model_axy3) || (unitType == Unit.model_axy4))
			{
				movThreshUd.IsEnabled = false;
				movThreshUd.Value = 0;
				latencyThreshUd.IsEnabled = false;
				latencyThreshUd.Value = 0;
			}

			if ((unitType == Unit.model_axy3) || (unitType == Unit.model_axy4))
			{
				TDgroupBox.Header = "TEMPERATURE LOGGING";
				logPeriodStackPanel.IsEnabled = false;
			}

			reshapeTimer = new System.Timers.Timer(100);
			reshapeTimer.Elapsed += reshapeTimerElapsed;
			LocationChanged += locationChanged;
			reshape();
		}

		private void reshapeTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
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

		//private void reshape(Object source, System.Timers.ElapsedEventArgs e)
		//{
		//	System.Windows.Forms.Screen actScreen = ExtensionsForWPF.GetScreen(this);
		//	System.Drawing.Rectangle r = actScreen.Bounds;

		//	if (r.Height < 900)
		//	{
		//		Width = 800;
		//		Height = 560;
		//		generalSB.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
		//		generalSB.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
		//		generalSP.Orientation = Orientation.Horizontal;
		//	}
		//	else
		//	{
		//		generalSP.Orientation = Orientation.Vertical;
		//		Height = 790;
		//		Width = 460;
		//		generalSB.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
		//		generalSB.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
		//	}
		//}

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

			if ((firmTotA < 3002000))
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

		private void cmdDown_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			UInt16 i = UInt16.Parse(tempDepthLogginUD.Text);
			if ((i != 1)) i--;
			tempDepthLogginUD.Text = i.ToString();
		}

		private void cmdUp_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			UInt16 i = UInt16.Parse(tempDepthLogginUD.Text);
			if (i != 5) i++;
			tempDepthLogginUD.Text = i.ToString();
		}

		private void tempDepthCBChecked(object sender, RoutedEventArgs e)
		{
			if (firmTotA < 3004000)
			{
				pressureCB.IsChecked = temperatureCB.IsChecked;
			}

			var c = (CheckBox)sender;
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
					if ((unitType != Units.Unit.model_axy3))
					{
						if ((mDebug == 0))
						{
							mDebug = 1;
							// MessageBox.Show("mDebug enabled.")
							sendButton.Content = "Send configuration (d)";
						}
						else
						{
							mDebug = 0;
							// MessageBox.Show("mDebug disabled.")
							sendButton.Content = "Send configuration";
						}

					}

				}
				else if (e.Key == Key.C)
				{
					if (unitType != Units.Unit.model_axyQuattrok) return;
					sp.Write("TTTTTTTTTTTTTTTGGAg");
					int[] coeffs = new int[12];

					try
					{
						for (int i = 0; i < 12; i++)
						{
							coeffs[i] = sp.ReadByte();
						}
					}
					catch
					{
						return;
					}

					QuattrokPressureCalibration qp = new QuattrokPressureCalibration(coeffs);
					qp.ShowDialog();

					if (qp.mustWrite)
					{
						qp.zero *= 100;
						qp.span = Math.Round(qp.span * 100, 0);

						qp.threshold *= 65;
						qp.threshold /= 1000;
						qp.threshold /= .0000625;
						qp.threshold = Math.Round(qp.threshold, 0, MidpointRounding.ToEven);
						sp.Write("TTTTTTTTTTTTTTTGGAb");
						try
						{
							sp.ReadByte();
							byte b = (byte)((UInt16)qp.zero >> 8);
							sp.Write(new byte[] { b }, 0, 1);
							b = (byte)(qp.zero);
							sp.Write(new byte[] { b }, 0, 1);
							b = (byte)((UInt16)qp.span >> 8);
							sp.Write(new byte[] { b }, 0, 1);
							b = (byte)(qp.span);
							sp.Write(new byte[] { b }, 0, 1);
							b = (byte)((UInt16)qp.threshold >> 8);
							sp.Write(new byte[] { b }, 0, 1);
							b = (byte)(qp.threshold);
							sp.Write(new byte[] { b }, 0, 1);
							sp.ReadByte();
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
			//if (unitType == MainWindow.model_Co2Logger)
			//{
			//	if ((bool)rate1RB.IsChecked)
			//	{
			//		axyConfOut[24] = 1;
			//	}
			//	else if ((bool)rate10RB.IsChecked)
			//	{
			//		axyConfOut[24] = 2;
			//	}
			//	else
			//	{
			//		axyConfOut[24] = 3;
			//	}

			//	axyConfOut[22] = mDebug;
			//	mustWrite = true;
			//	this.Close();
			//	return;
			//}

			if ((rate50RB.IsChecked == true))
			{
				axyConfOut[15] = 0;
			}
			else if ((rate25RB.IsChecked == true))
			{
				axyConfOut[15] = 1;
			}
			else if ((rate100RB.IsChecked == true))
			{
				axyConfOut[15] = 2;
			}
			else if ((rate10RB.IsChecked == true))
			{
				axyConfOut[15] = 3;
			}
			else if ((rate1RB.IsChecked == true))
			{
				axyConfOut[15] = 4;
			}

			//if ((unitType == MainWindow.model_axy4) || (unitType == MainWindow.model_axyDepth))
			//{

			//if ((mDebug == 1))
			//{
			//	axyConfOut[15] += 8;
			//}

			//}

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
			if ((temperatureCB.IsChecked == true))
			{
				axyConfOut[17] = 1;
			}
			if ((pressureCB.IsChecked == true))
			{
				axyConfOut[18] = 1;
			}


			//axyConfOut[19] = Convert.ToByte(double.Parse(tempDepthLogginUD.Text));
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

			if ((firmTotA > 2000000))
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

			if (firmTotA >= 3008000)
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
