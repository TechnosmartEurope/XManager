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
using System.Windows.Shapes;

namespace X_Manager
{
	/// <summary>
	/// Interaction logic for ConfigurationWindow.xaml
	/// </summary>
	public partial class Axy5ConfigurationWindow : ConfigurationWindow
	{

		//public bool mustWrite = false;
		public UInt32[] soglie = new UInt32[18];
		byte mDebug = 0;
		UInt16[] c = new UInt16[7];
		//byte[] unitFirmware;
		UInt32 firmTotA;
		//bool evitaSoglieDepth = false;

		public Axy5ConfigurationWindow(byte[] axyconf, byte[] schedule, UInt32 unitFirm)
			: base()
		{
			InitializeComponent();
			this.Loaded += loaded;
			mustWrite = false;
			axyConfOut = new byte[30];
			axyScheduleOut = new byte[30];
			firmTotA = unitFirm;


			//sendButton.Margin = new Thickness(25);

			//Temperature logging
			temperatureOnOff.IsChecked = false;
			tempDepthLogginUD.IsEnabled = false;
			if (axyconf[17] == 1)
			{
				temperatureOnOff.IsChecked = true;
				tempDepthLogginUD.IsEnabled = true;
			}

			//Pressure logging
			pressureOnOff.IsChecked = false;
			if (axyconf[18] == 1)
			{
				temperatureOnOff.IsChecked = true;
				tempDepthLogginUD.IsEnabled = true;
			}

			//T/D period
			tempDepthLogginUD.Text = axyconf[19].ToString();

			//Waterswitch
			waterOnOff.IsChecked = false;
			waterOnOff.IsEnabled = false;

			//Remote
			remoteOnOff.IsChecked = false;
			remoteScheduleGB.IsEnabled = false;
			if (axyconf[20] == 1)
			{
				remoteOnOff.IsChecked = true;
				if (firmTotA > 1001000)
				{
					remoteScheduleGB.IsEnabled = true;
					//remoteInterval1.IsEnabled = true;
					//remoteInterval2.IsEnabled = true;
				}
			}

			//Magnetometro
			magOnOff.IsChecked = false;
			if (axyconf[21] == 1)
			{
				magOnOff.IsChecked = true;
			}

			movThreshUd.Value = axyconf[23];
			latencyThreshUd.Value = axyconf[24];

			sendButton.Content = "Send configuration";
			mDebug = 0;
			if (axyconf[25] == 1)
			{
				mDebug = 1;
				sendButton.Content += " (d)";
			}

			//Schedule
			scheduleC.importSchedule(schedule);

			//Remote Schedule
			uint bitMask = 0;
			if (firmTotA > 1001000)
			{
				bitMask = ((uint)axyconf[26] << 16) + ((uint)axyconf[27] << 8) + (uint)axyconf[28];
			}

			for (int i = 0; i < 24; i++)
			{
				var r = (Rectangle)remoteScheduleSP.Children[i];
				if (((bitMask >> i) & 1) == 1)
				{
					r.Fill = new SolidColorBrush(Color.FromArgb(0xff, 0x00, 0xaa, 0xde));
				}
				else
				{
					r.Fill = new SolidColorBrush(Color.FromArgb(0xff, 0x18, 0x18, 0x18));
				}
			}

		}

		private void cbChecked(object sender, RoutedEventArgs e)
		{
			int a = 0;
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

			movThreshUd.IsEnabled = false;
			movThreshUd.Value = 0;
			latencyThreshUd.IsEnabled = false;
			latencyThreshUd.Value = 0;

			//TDgroupBox.Header = "TEMPERATURE LOGGING";
			//logPeriodStackPanel.IsEnabled = false;

			// setThresholdUds()

		}

		private void setThresholdUds()
		{
			movValueChanged();
			latValueChanged();
		}

		private void movValueChanged()
		{
			//byte range = 0;
			//if ((bool)range2RB.IsChecked)
			//{
			//	range = 4;
			//}
			//else if ((bool)range4RB.IsChecked)
			//{
			//	range = 8;
			//}
			//else if ((bool)range8RB.IsChecked)
			//{
			//	range = 16;
			//}
			//else if ((bool)range16RB.IsChecked)
			//{
			//	range = 32;
			//}

			//movThresholdLabel.Content = (Math.Round((range / (256 * movThreshUd.Value)), 4).ToString() + " g");
		}

		private void latValueChanged()
		{
			byte rate = 0;
			//if ((bool)rate100RB.IsChecked)
			//{
			//	rate = 100;
			//}
			//else if ((bool)rate10RB.IsChecked)
			//{
			//	rate = 10;
			//	// ElseIf rate1RB.IsChecked Then
			//	// rate = 1
			//}
			//else if ((bool)rate25RB.IsChecked)
			//{
			//	rate = 25;
			//}
			//else if ((bool)rate50RB.IsChecked)
			//{
			//	rate = 50;
			//}

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

		private void magCheck(object sender, RoutedEventArgs e)
		{
			if ((bool)magOnOff.IsChecked)
			{
				magOnOff.Content = "Enabled";
			}
			else
			{
				magOnOff.Content = "Disabled";
			}
		}

		private void remCheck(object sender, RoutedEventArgs e)
		{
			if ((bool)remoteOnOff.IsChecked)
			{
				remoteOnOff.Content = "Enabled";
				if (firmTotA > 1001000)
				{
					remoteScheduleGB.IsEnabled = true;
					//remoteInterval1.IsEnabled = true;
					//remoteInterval2.IsEnabled = true;
				}
			}
			else
			{
				remoteOnOff.Content = "Disabled";
				remoteScheduleGB.IsEnabled = false;
				//remoteInterval1.IsEnabled = false;
				//remoteInterval2.IsEnabled = false;
			}
		}

		private void waterCheck(object sender, RoutedEventArgs e)
		{
			if ((bool)waterOnOff.IsChecked)
			{
				waterOnOff.Content = "Enabled";
			}
			else
			{
				waterOnOff.Content = "Disabled";
			}
		}

		private void tempChecked(object sender, RoutedEventArgs e)
		{
			if ((bool)temperatureOnOff.IsChecked)
			{
				temperatureOnOff.Content = "Enabled";
			}
			else
			{
				temperatureOnOff.Content = "Disabled";
			}
		}

		private void depthChecked(object sender, RoutedEventArgs e)
		{
			if ((bool)pressureOnOff.IsChecked)
			{
				pressureOnOff.Content = "Enabled";
			}
			else
			{
				pressureOnOff.Content = "Disabled";
			}
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
			if (((bool)temperatureOnOff.IsChecked) | ((bool)pressureOnOff.IsChecked))
			{
				tempDepthLogginUD.IsEnabled = true;
			}
			else
			{
				tempDepthLogginUD.IsEnabled = false;
			}

		}

		private void ctrlManager(object sender, KeyEventArgs e)
		{
			e.Handled = true;
			if ((e.Key == Key.Return))
			{
				//MessageBox.Show(sender.ToString());
				sendConfiguration();
			}
			else if ((e.Key == Key.D))
			{
				if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
				{
					e.Handled = true;
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
			else
			{
				e.Handled = false;
			}
		}

		private void sendConf(object sender, RoutedEventArgs e)
		{
			sendConfiguration();
		}

		private void sendConfiguration()
		{

			axyConfOut[17] = 0;
			axyConfOut[18] = 0;
			if ((temperatureOnOff.IsChecked == true))
			{
				axyConfOut[17] = 1;
			}
			if ((pressureOnOff.IsChecked == true))
			{
				axyConfOut[18] = 1;
			}

			axyConfOut[19] = axyConfOut[20] = axyConfOut[21] = axyConfOut[23] = axyConfOut[24] = 0;
			try
			{
				axyConfOut[19] = Convert.ToByte(tempDepthLogginUD.Text);
			}
			catch { }

			if (remoteOnOff.IsChecked == true)
			{
				axyConfOut[20] = 1;
			}
			else
			{
				axyConfOut[20] = 0;
			}

			if (magOnOff.IsChecked == true)
			{
				axyConfOut[21] = 1;
			}
			
			try
			{
				axyConfOut[23] = (byte)movThreshUd.Value;
			}
			catch { }
			try
			{
				axyConfOut[24] = (byte)latencyThreshUd.Value;
			}
			catch { }

			axyConfOut[25] = mDebug;

			if (firmTotA > 1001000)
			{
				uint bitMask = 0;
				for (int i = 0; i < 24; i++)
				{
					var r = (Rectangle)remoteScheduleSP.Children[i];
					if (((SolidColorBrush)r.Fill).Color.R == 0)
					{
						bitMask += (uint)Math.Pow(2, i);
					}
					axyConfOut[26] = (byte)((bitMask >> 16) & 0b1111_1111);
					axyConfOut[27] = (byte)((bitMask >> 8) & 0b1111_1111);
					axyConfOut[28] = (byte)(bitMask & 0b1111_1111);
				}
			}

			axyScheduleOut = scheduleC.exportSchedule();

			mustWrite = true;
			this.Close();
		}

		private void remoteHourClicked(object sender, RoutedEventArgs e)
		{
			var r = (Rectangle)sender;
			if (((SolidColorBrush)r.Fill).Color.R == 0)
			{
				r.Fill = new SolidColorBrush(Color.FromArgb(0xff, 0x18, 0x18, 0x18));
			}
			else
			{
				r.Fill = new SolidColorBrush(Color.FromArgb(0xff, 0x00, 0xaa, 0xde));
			}
		}

		//private void rem1Ch(object sender, RoutedEventArgs e)
		//{
		//	if (remInt1.SelectedIndex > remInt2.SelectedIndex)
		//	{
		//		remInt1.SelectedIndex = remInt2.SelectedIndex;
		//	}
		//}

		//private void rem2Ch(object sender, RoutedEventArgs e)
		//{
		//	if (remInt2.SelectedIndex < remInt1.SelectedIndex)
		//	{
		//		remInt2.SelectedIndex = remInt1.SelectedIndex;
		//	}
		//	if (remInt2.SelectedIndex > remInt3.SelectedIndex)
		//	{
		//		remInt2.SelectedIndex = remInt3.SelectedIndex;
		//	}
		//}

		//private void rem3Ch(object sender, RoutedEventArgs e)
		//{
		//	if (remInt3.SelectedIndex < remInt2.SelectedIndex)
		//	{
		//		remInt3.SelectedIndex = remInt2.SelectedIndex;
		//	}
		//	if (remInt3.SelectedIndex > remInt4.SelectedIndex)
		//	{
		//		remInt3.SelectedIndex = remInt4.SelectedIndex;
		//	}
		//}

		//private void rem4Ch(object sender, RoutedEventArgs e)
		//{
		//	if (remInt4.SelectedIndex < remInt3.SelectedIndex)
		//	{
		//		remInt4.SelectedIndex = remInt3.SelectedIndex;
		//	}
		//}

	}

}
