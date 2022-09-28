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

namespace X_Manager.ConfigurationWindows
{
	/// <summary>
	/// Interaction logic for ConfigurationWindow.xaml
	/// </summary>
	public partial class AxyConfigurationWindow : ConfigurationWindow
	{

		public UInt32[] soglie = new UInt32[18];
		byte unitType;
		UInt16[] c = new UInt16[7];
		UInt32 firmTotA;
		byte _mDebug = 0;
		byte mDebug
		{
			get
			{
				return _mDebug;
			}
			set
			{
				_mDebug = value;
				if (value == 0)
				{
					sendButton.Content = "Send configuration";
				}
				else
				{
					sendButton.Content = "Send configuration (d)";
				}
			}
		}

		public AxyConfigurationWindow(byte[] axyconf, UInt32 unitFirm)
			: base()
		{
			InitializeComponent();
			this.Loaded += loaded;
			mustWrite = false;
			axyConfOut = new byte[25];
			unitType = axyconf[25];
			firmTotA = unitFirm;

			if ((unitType == Units.Unit.model_Co2Logger))
			{
				mainGrid.RowDefinitions[2].Height = new GridLength(0);
				mainGrid.RowDefinitions[3].Height = new GridLength(0);
				mainGrid.RowDefinitions[4].Height = new GridLength(0);
				mainGrid.RowDefinitions[5].Height = new GridLength(0);
				accGrid.ColumnDefinitions[1].Width = new GridLength(0);
				Height = 450;
				sampleRateGB.Header = "SAMPLING PERIOD (s)";
				((RadioButton)(rates.Children[0])).Content = "1";
				((RadioButton)(rates.Children[1])).Content = "10";
				((RadioButton)(rates.Children[2])).Content = "60";
				((RadioButton)(rates.Children[3])).Visibility = Visibility.Hidden;
				((RadioButton)(rates.Children[4])).Visibility = Visibility.Hidden;
			}
			else
			{
				mainGrid.RowDefinitions[5].Height = new System.Windows.GridLength(0);
				sendButton.Margin = new Thickness(25);
				if (firmTotA < 3000000)
				{
					magCB.Visibility = Visibility.Hidden;
					magL.Visibility = Visibility.Hidden;
					magGB.Visibility = Visibility.Hidden;
					magCol.Width = new GridLength(0, GridUnitType.Pixel);
				}
				if (firmTotA < 3002000)
				{
					adcCB.IsChecked = false;
					adcCB.IsEnabled = false;
				}
				else
				{
					adcCB.IsChecked = axyconf[22] == 1 ? true : false;
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

			//if (unitType == Units.Unit.model_Co2Logger)
			//{
			//	switch (axyconf[24])
			//	{
			//		case 1:
			//			rate1RB.IsChecked = true;
			//			break;
			//		case 2:
			//			rate10RB.IsChecked = true;
			//			break;
			//		case 3:
			//			rate25RB.IsChecked = true;
			//			break;
			//	}
			//	sendButton.Content = "Send configuration";
			//	mDebug = axyconf[22];
			//	if ((axyconf[22] == 1))
			//	{
			//		sendButton.Content += " (d)";
			//	}

			//	return;
			//}

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

			if (unitType == Units.Unit.model_axy4 || unitType == Units.Unit.model_axyDepth || unitType == Units.Unit.model_axyDepthFast)
			{
				//sendButton.Content = "Send configuration";
				if (axyconf[15] < 8)
				{
					mDebug = 0;
				}
				else
				{
					sendButton.Content += " (d)";
					mDebug = 1;
				}

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

			tempDepthCB.IsChecked = false;
			tempDepthLogginUD.IsEnabled = false;
			if (axyconf[17] == 1)
			{
				tempDepthCB.IsChecked = true;
				tempDepthLogginUD.IsEnabled = true;
			}

			tempDepthLogginUD.Text = "";
			if (unitType != Units.Unit.model_axy3)
			{
				if (unitType != Units.Unit.model_axy4 | firmTotA >= 3001000)
				{
					tempDepthLogginUD.Text = axyconf[19].ToString();
				}
			}

			if (firmTotA >= 3000000)
			{
				magCB.SelectedIndex = axyconf[21];
			}

			//	if (unitType == Units.Unit.model_axyTrek)
			//	{
			//		switch (axyconf[22])
			//		{
			//			case 0:
			//				sendButton.Content = "Send configuration";
			//				mDebug = 0;
			//				break;
			//			case 1:
			//				sendButton.Content = "Send configuration (d)";
			//				mDebug = 1;
			//				break;
			//		}
			//		if ((firmTotA > 2000000))
			//		{
			//			switch (axyconf[23])
			//			{
			//				case 0:
			//					WsDisabledRB.IsChecked = true;
			//					break;
			//				case 1:
			//					WsEnabledRB.IsChecked = true;
			//					break;
			//				case 2:
			//					WsHardwareRB.IsChecked = true;
			//					break;
			//				default:
			//					WsDisabledRB.IsChecked = true;
			//					break;
			//			}
			//		}
			//	}
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
			if ((unitType == Units.Unit.model_axyDepth) || (unitType == Units.Unit.model_axy3) || (unitType == Units.Unit.model_axy4))
			{
				movThreshUd.IsEnabled = false;
				movThreshUd.Value = 0;
				latencyThreshUd.IsEnabled = false;
				latencyThreshUd.Value = 0;
			}

			if (unitType == Units.Unit.model_axy3)
			{
				TDgroupBox.Header = "TEMPERATURE LOGGING";
				logPeriodStackPanel.IsEnabled = false;
			}

			if ((unitType == Units.Unit.model_axy4) & (firmTotA < 3001000))
			{
				TDgroupBox.Header = "TEMPERATURE LOGGING";
				logPeriodStackPanel.IsEnabled = false;
			}

			// setThresholdUds()
		}

		private void setThresholdUds()
		{
			if ((bool)rate1RB.IsChecked)
			{
				if (magCB.SelectedIndex == 2)
				{
					magCB.SelectedIndex = 1;
				}
				if (magCB.Items.Count == 3)
				{
					magCB.Items.RemoveAt(2);
				}
			}
			else
			{
				if (magCB.Items.Count == 2)
				{
					magCB.Items.Add("2");
				}
				movValueChanged();
				latValueChanged();
			}
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
					if (unitType != Units.Unit.model_axy3)
					{
						if (mDebug == 0)
						{
							mDebug = 1;
							// MessageBox.Show("mDebug enabled.")
							//sendButton.Content = "Send configuration (d)";
						}
						else
						{
							mDebug = 0;
							// MessageBox.Show("mDebug disabled.")
							//sendButton.Content = "Send configuration";
						}

					}

				}

			}

		}

		private void sendButton_click(object sender, RoutedEventArgs e)
		{
			sendConfiguration();
		}

		private void sendConfiguration()
		{
			//if (unitType == Units.Unit.model_Co2Logger)
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

			if (unitType == Units.Unit.model_axy4 || unitType == Units.Unit.model_axyDepth || unitType == Units.Unit.model_axyDepthFast)
			{
				if (mDebug == 1)
				{
					axyConfOut[15] += 8;
				}

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
			if ((tempDepthCB.IsChecked == true))
			{
				axyConfOut[17] = 1;
				axyConfOut[18] = 1;
			}

			//axyConfOut[19] = Convert.ToByte(double.Parse(tempDepthLogginUD.Text));
			axyConfOut[19] = axyConfOut[20] = axyConfOut[21] = 0;
			try
			{
				axyConfOut[19] = Convert.ToByte(tempDepthLogginUD.Text);
			}
			catch { }
			if (firmTotA >= 3000000)
			{
				axyConfOut[21] = (byte)magCB.SelectedIndex;
			}
			if (firmTotA >= 3002000)
			{
				axyConfOut[22] = (bool)(adcCB.IsChecked) ? (byte)1 : (byte)0;
			}
			mustWrite = true;
			Close();
		}

	}

}
