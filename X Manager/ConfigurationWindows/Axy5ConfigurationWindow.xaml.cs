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

		public Axy5ConfigurationWindow(byte[] axyconf, UInt32 unitFirm)
			: base()
		{
			InitializeComponent();
			this.Loaded += loaded;
			mustWrite = false;
			axyConfOut = new byte[26];
			firmTotA = unitFirm;

			//sendButton.Margin = new Thickness(25);

			foreach (RadioButton c in rates.Children)
			{
				try
				{
					c.IsChecked = false;
				}
				catch { }
			}

			foreach (RadioButton c in ranges.Children)
			{
				try
				{
					c.IsChecked = false;
				}
				catch { }
			}


			switch (axyconf[15])
			{
				//Inserire caso =: accc in off, appena fatto schedule
				case 1:
					rate1RB.IsChecked = true;
					break;
				case 2:
					rate10RB.IsChecked = true;
					break;
				case 3:
					rate25RB.IsChecked = true;
					break;
				case 4:
					rate50RB.IsChecked = true;
					break;
				case 5:
					rate100RB.IsChecked = true;
					break;
				default:
					rate25RB.IsChecked = true;
					break;
			}

			switch (axyconf[16])
			{
				default:
				case 0:
					range2RB.IsChecked = true;
					break;
				case 1:
					range4RB.IsChecked = true;
					break;
				case 2:
					range8RB.IsChecked = true;
					break;
				case 3:
					range16RB.IsChecked = true;
					break;
			}

			tempDepthCB.IsChecked = false;
			tempDepthLogginUD.IsEnabled = false;
			if (axyconf[17] == 1)
			{
				tempDepthCB.IsChecked = true;
				tempDepthLogginUD.IsEnabled = true;
			}

			tempDepthLogginUD.Text = axyconf[19].ToString();

			remoteOnOff.IsChecked = false;
			if (axyconf[20] == 1)
			{
				remoteOnOff.IsChecked = true;
			}

			magOnOff.IsChecked = false;
			if (axyconf[21] == 1)
			{
				magOnOff.IsChecked = true;
			}

			bits8RB.IsChecked = false;
			bits10RB.IsChecked = false;
			bits12RB.IsChecked = false;
			switch (axyconf[22])
			{
				case 0:
					bits10RB.IsChecked = true;
					break;
				case 1:
					bits12RB.IsChecked = true;
					break;
				case 2:
				default:
					bits8RB.IsChecked = true;
					break;
			}

			movThreshUd.Value = axyconf[23];
			latencyThreshUd.Value = axyconf[24];

			sendButton.Content = "Send configuration";
			if (axyconf[25] == 1)
			{
				sendButton.Content += " (d)";
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

			movThreshUd.IsEnabled = false;
			movThreshUd.Value = 0;
			latencyThreshUd.IsEnabled = false;
			latencyThreshUd.Value = 0;

			TDgroupBox.Header = "TEMPERATURE LOGGING";
			logPeriodStackPanel.IsEnabled = false;

			//Da modificare man mano che si sviluppa il firmware
			remoteOnOff.IsChecked = false;
			remoteOnOff.IsEnabled = false;
			if ((bool)bits12RB.IsChecked)
			{
				bits10RB.IsChecked = true;
			}
			bits12RB.IsEnabled = false;
			WsDisabledRB.IsChecked = false;
			WaterSwitchGB.IsEnabled = false;

			// setThresholdUds()
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

			movThresholdLabel.Content = (Math.Round((range / (256 * movThreshUd.Value)), 4).ToString() + " g");
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
			}
			else
			{
				remoteOnOff.Content = "Disabled";
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
			if ((bool)tempDepthCB.IsChecked)
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
			if ((e.Key == Key.Return))
			{
				sendConfiguration();
			}

			if ((e.Key == Key.D))
			{
				if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
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
			e.Handled = true;

		}

		private void sendConf(object sender, RoutedEventArgs e)
		{
			sendConfiguration();
		}

		private void sendConfiguration()
		{


			if ((rate1RB.IsChecked == true))
			{
				axyConfOut[15] = 1;
			}
			else if ((rate10RB.IsChecked == true))
			{
				axyConfOut[15] = 2;
			}
			else if ((rate25RB.IsChecked == true))
			{
				axyConfOut[15] = 3;
			}
			else if ((rate50RB.IsChecked == true))
			{
				axyConfOut[15] = 4;
			}
			else if ((rate100RB.IsChecked == true))
			{
				axyConfOut[15] = 5;
			}

			if (range2RB.IsChecked == true)
			{
				axyConfOut[16] = 0;
			}
			else if (range4RB.IsChecked == true)
			{
				axyConfOut[16] = 1;
			}
			else if (range8RB.IsChecked == true)
			{
				axyConfOut[16] = 2;
			}
			else if (range16RB.IsChecked == true)
			{
				axyConfOut[16] = 3;
			}


			axyConfOut[17] = 0;
			axyConfOut[18] = 0;
			if ((tempDepthCB.IsChecked == true))
			{
				axyConfOut[17] = 1;
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

			if (magOnOff.IsChecked == true)
			{
				axyConfOut[21] = 1;
			}

			if (bits10RB.IsChecked == true)
			{
				axyConfOut[22] = 0;
			}
			else if (bits12RB.IsChecked == true)
			{
				axyConfOut[22] = 1;
			}
			else if (bits8RB.IsChecked == true)
			{
				axyConfOut[22] = 2;
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

			mustWrite = true;
			this.Close();
		}

	}

}
