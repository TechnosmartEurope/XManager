using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
//using System.Windows.Forms;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace X_Manager.ConfigurationWindows
{
	/// <summary>
	/// Interaction logic for ConfigurationWindow.xaml
	/// </summary>
	public partial class Axy5ConfigurationWindow : ConfigurationWindow
	{

		//public bool mustWrite = false;
		public UInt32[] soglie = new UInt32[18];
		byte mDebug = 0;
		ushort[] c = new ushort[7];
		uint firmTotA;
		//bool evitaSoglieDepth = false;
		//bool remoteHourEditing = false;
		ConfigurationWindows.AccDayIntervals scheduleC;
		volatile bool stopRender = false;

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
				pressureOnOff.IsChecked = true;
				tempDepthLogginUD.IsEnabled = true;
			}

			//ADC logging
			adcOnOff.IsChecked = false;
			if (axyconf[15] == 1)
			{
				adcOnOff.IsChecked = true;
			}
			if (firmTotA < 1005000)
			{
				adcOnOff.IsChecked = false;
				axyconf[15] = 0;
				adcOnOff.IsEnabled = false;
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
			magOnOff.SelectedIndex = axyconf[21];

			movThreshUd.Value = axyconf[23];
			latencyThreshUd.Value = axyconf[24];

			sendTB.Text = "Send configuration";
			mDebug = 0;
			if (axyconf[25] == 1)
			{
				mDebug = 1;
				sendTB.Text += " (d)";
			}

			//Schedule
			scheduleC = new ConfigurationWindows.AccDayIntervals();
			if (firmTotA >= 1004000)
			{
				scheduleC.enable12();
			}
			scheduleC.importSchedule(schedule);
			scheduleGB.Content = scheduleC;

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
			//int a = 0;
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

			//Riempie il textbox
			renderSummary();

			var rend = new Thread(renderSummaryThread);
			rend.Start();
			
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

		//private void tempChecked(object sender, RoutedEventArgs e)
		//{
		//	if ((bool)temperatureOnOff.IsChecked)
		//	{
		//		temperatureOnOff.Content = "Enabled";
		//	}
		//	else
		//	{
		//		temperatureOnOff.Content = "Disabled";
		//	}
		//}

		//private void depthChecked(object sender, RoutedEventArgs e)
		//{
		//	if ((bool)pressureOnOff.IsChecked)
		//	{
		//		pressureOnOff.Content = "Enabled";
		//	}
		//	else
		//	{
		//		pressureOnOff.Content = "Disabled";
		//	}
		//}

		//private void adchChecked(object sender, RoutedEventArgs e)
		//{
		//	if ((bool)adcOnOff.IsChecked)
		//	{
		//		adcOnOff.Content = "Enabled";
		//	}
		//	else
		//	{
		//		adcOnOff.Content = "Disabled";
		//	}
		//}

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

		private void tempDepthAdcCBChecked(object sender, RoutedEventArgs e)
		{
			if (((bool)temperatureOnOff.IsChecked) | ((bool)pressureOnOff.IsChecked) | ((bool)adcOnOff.IsChecked))
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
						sendTB.Text = "Send configuration (d)";
					}
					else
					{
						mDebug = 0;
						// MessageBox.Show("mDebug disabled.")
						sendTB.Text = "Send configuration";
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
			stopRender = true;
			sendConfiguration();
		}

		private void sendConfiguration()
		{

			axyConfOut[15] = 0;
			axyConfOut[17] = 0;
			axyConfOut[18] = 0;
			if (firmTotA >= 1005000)
			{
				if (adcOnOff.IsChecked == true)
				{
					axyConfOut[15] = 1;
				}
			}
			if (temperatureOnOff.IsChecked == true)
			{
				axyConfOut[17] = 1;
			}
			if (pressureOnOff.IsChecked == true)
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

			axyConfOut[21] = (byte)magOnOff.SelectedIndex;
			if (firmTotA < 1004000)
			{
				if (axyConfOut[21] == 2) axyConfOut[21] = 1;
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

		}

		private void Rectangle_MouseEnter(object sender, MouseEventArgs e)
		{

		}

		private void Rectangle_MouseLeave(object sender, MouseEventArgs e)
		{
			if (System.Windows.Input.Mouse.LeftButton == MouseButtonState.Pressed)
			{
				remoteHourEdit(sender);
			}

		}

		private void remoteHourClickedDown(object sender, MouseButtonEventArgs e)
		{

		}

		private void remoteHourClickedUp(object sender, MouseButtonEventArgs e)
		{
			//remoteHourEditing = true;
			remoteHourEdit(sender);
		}

		private void remoteHourEdit(object sender)
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
			//remoteHourEditing = false;
		}

		private void renderSummaryThread()
		{
			while (true)
			{
				if (stopRender) return;
				Application.Current.Dispatcher.Invoke(() => renderSummary());
				//renderSummary();
				Thread.Sleep(500);
			}
		}

		private void renderSummary()
		{
			byte[] schedule;
			try
			{
				schedule = scheduleC.exportSchedule();
			}
			catch
			{
				return;
			}


			int nint = 1;
			string[] card = new string[] { "the first one", "the second one", "the third one", "the fourth one", "the fifth one" };
			string[] accB = new String[5];
			int[] frequenze = new int[6] { 0, 1, 10, 25, 50, 100 };
			int[] fondo = new int[4] { 2, 4, 8, 16 };
			int[] bit = new int[3] { 8, 10, 12 };
			int[] orari = new int[7];
			orari[0] = 0;
			orari[6] = 0x24;


			//Intervalli
			for (int i = 0; i < 5; i++)
			{
				if (schedule[i * 6] < 36) nint++;
				orari[i + 1] = schedule[i * 6];

				if (schedule[(i * 6) + 1] > 0)
				{
					accB[i] = String.Format("runs at {0} Hz, {1}g fullscale, {2}-bit resolution.", frequenze[schedule[(i * 6) + 1]], fondo[schedule[(i * 6) + 2]], bit[schedule[(i * 6) + 3]]);
				}
				else
				{
					accB[i] = "is idle.";
				}

			}

			string summary = "";

			//Accelerometro
			if (nint > 1)
			{
				string pl = "";
				if (nint > 1)
				{
					pl = "s";
				}
				summary = String.Format("Day is divided into {0} interval{1}:\r\n", nint, pl);
				for (int i = 0; i < nint; i++)
				{
					summary += String.Format("   - {0} starts at {1:x} and stops at {2:x}\r\n", card[i], orari[i], orari[i + 1]);
					summary += String.Format("     The accelerometer {0}\r\n", accB[i]);
				}
			}
			else
			{
				summary = "Day is not divided into any interval.\r\n\r\n";
				summary += String.Format("The accelerometer {0}\r\n", accB[0]);
			}

			//Remoto
			var oraP = new List<int>();
			var oraQ = new List<int>();
			if ((bool)remoteOnOff.IsChecked)
			{
				int status, oldstatus = 0;
				for (int i = 0; i < 24; i++)
				{
					var r = (Rectangle)remoteScheduleSP.Children[i];
					status = 1;
					if (((SolidColorBrush)r.Fill).Color.R == 0x18)
					{
						status = 0;
					}

					if (status != oldstatus)
					{
						oldstatus = status;
						if (status == 1)
						{
							oraP.Add(i);
						}
						else
						{
							oraQ.Add(i);
						}
					}
				}
				if (oraP.Count > 0)
				{
					summary += "\r\nRemote download is scheduled:\r\n";
					if (oraQ.Count < oraP.Count)
					{
						oraQ.Add(24);
					}

					for (int i = 0; i < oraP.Count; i++)
					{
						summary += String.Format("   from {0}:00 to {1}:59\r\n", oraP[i], (oraQ[i] - 1));
					}
					//summary += String.Format("   at {0} for {1} hour(s).\r\n", oraP[oraP.Count - 1], (oraQ[oraQ.Count - 1] - oraP[oraP.Count - 1]));
				}
				else
				{
					summary += "\r\nRemote download is enabled but not scheduled.\r\n";
					summary += "Please set a schedule to activate it.\r\n";
				}
			}
			else
			{
				summary += "\r\nRemote is disabled.\r\n";
			}

			//Magnetometro
			string pl0 = "dis";
			string pl1 = "";
			if (magOnOff.SelectedIndex > 0)
			{
				pl0 = "en";
				pl1 = String.Format(" ({0} Hz)", magOnOff.SelectedIndex);
			}
			summary += String.Format("\r\n Magnetometer is {0}abled{1}.\r\n", pl0, pl1);

			//TD
			summary += "\r\n Temperature/Depth logging is ";
			if ((bool)temperatureOnOff.IsChecked | (bool)pressureOnOff.IsChecked)
			{
				summary += "enabled ";
				if (!(bool)pressureOnOff.IsChecked) summary += "\r\n  for temperature only";
				if (!(bool)temperatureOnOff.IsChecked) summary += "\r\n  for pressure only";
				summary += String.Format(" and set at\r\n  1 sample every {0} seconds.\r\n", tempDepthLogginUD.Text);
			}
			else
			{
				summary += "disabled\r\n";
			}
			summary += "\r\n ";

			//ADC
			if (firmTotA >= 1005000)
			{
				summary += "ADC logging is ";
				if ((bool)adcOnOff.IsChecked)
				{
					summary += String.Format("enabled and set at\r\n  1 sample every {0} seconds.\r\n", tempDepthLogginUD.Text);
				}
				else
				{
					summary += "disabled\r\n";
				}
				summary += "\r\n ";
			}

			summaryTB.Text = summary;

		}

		private void ConfigurationWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			stopRender = true;
		}

		private void SaveB_Click(object sender, RoutedEventArgs e)
		{
			var s = new System.Windows.Forms.SaveFileDialog();
			try
			{
				s.InitialDirectory = (System.IO.File.ReadAllLines(MainWindow.iniFile))[10];
			}
			catch { }
			s.DefaultExt = ".sch";

			s.ShowDialog();

			if (String.IsNullOrEmpty(s.FileName)) return;

			try
			{
				System.IO.File.WriteAllText(s.FileName, "");
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
				return;
			}

			byte[] schedule = scheduleC.exportSchedule();
			foreach (byte b in schedule)
			{
				System.IO.File.AppendAllText(s.FileName, (b.ToString() + "\r\n"));
			}
			for (int i = 0; i < 24; i++)
			{
				var r = (Rectangle)remoteScheduleSP.Children[i];
				if (((SolidColorBrush)r.Fill).Color.R == 0x18)
				{
					System.IO.File.AppendAllText(s.FileName, ("0\r\n"));
				}
				else
				{
					System.IO.File.AppendAllText(s.FileName, ("1\r\n"));
				}
			}

			System.IO.File.AppendAllText(s.FileName, string.Format("{0}\r\n", magOnOff.SelectedIndex));
			System.IO.File.AppendAllText(s.FileName, string.Format("{0}\r\n", remoteOnOff.IsChecked));
			System.IO.File.AppendAllText(s.FileName, string.Format("{0}\r\n", temperatureOnOff.IsChecked));
			System.IO.File.AppendAllText(s.FileName, string.Format("{0}\r\n", pressureOnOff.IsChecked));
			if (firmTotA >= 1005000)
			{
				System.IO.File.AppendAllText(s.FileName, string.Format("{0}\r\n", adcOnOff.IsChecked));
			}
			System.IO.File.AppendAllText(s.FileName, string.Format("{0}\r\n", tempDepthLogginUD.Text));
			System.IO.File.AppendAllText(s.FileName, string.Format("{0}\r\n", firmTotA));

			string[] prefs = System.IO.File.ReadAllLines(MainWindow.iniFile);
			prefs[10] = System.IO.Path.GetDirectoryName(s.FileName);
			System.IO.File.WriteAllLines(MainWindow.iniFile, prefs);

		}

		private void LoadB_Click(object sender, RoutedEventArgs e)
		{

			uint newFirmTotA;
			var l = new System.Windows.Forms.OpenFileDialog();
			try
			{
				l.InitialDirectory = (System.IO.File.ReadAllLines(MainWindow.iniFile))[10];
			}
			catch { }

			l.ShowDialog();

			if (string.IsNullOrEmpty(l.FileName)) return;

			string[] conf = System.IO.File.ReadAllLines(l.FileName);

			newFirmTotA = uint.Parse(conf[conf.Length - 1]);
			if (newFirmTotA >= 1004000)
			{
				scheduleC.enable12();
			}
			byte[] schedule = new byte[30];
			for (int i = 0; i < 30; i++)
			{
				schedule[i] = byte.Parse(conf[i]);
			}
			scheduleC.importSchedule(schedule);

			for (int i = 30; i < 54; i++)
			{
				var r = (Rectangle)remoteScheduleSP.Children[i - 30];
				if (conf[i] == "1")
				{
					r.Fill = new SolidColorBrush(Color.FromArgb(0xff, 0x00, 0xaa, 0xde));
				}
				else
				{
					r.Fill = new SolidColorBrush(Color.FromArgb(0xff, 0x18, 0x18, 0x18));
				}
			}
			bool magOld;
			if (bool.TryParse(conf[54], out magOld))
			{
				if (bool.Parse(conf[54]))
				{
					magOnOff.SelectedIndex = 1;
				}
				else
				{
					magOnOff.SelectedIndex = 0;
				}
			}
			else
			{
				magOnOff.SelectedIndex = int.Parse(conf[54]);
			}

			remoteOnOff.IsChecked = bool.Parse(conf[55]);
			temperatureOnOff.IsChecked = bool.Parse(conf[56]);
			pressureOnOff.IsChecked = bool.Parse(conf[57]);
			if (newFirmTotA >= 1005000)
			{
				adcOnOff.IsEnabled = true;
				adcOnOff.IsChecked = bool.Parse(conf[58]);
				tempDepthLogginUD.Text = conf[59];
				if (firmTotA < 1005000)
				{
					adcOnOff.IsEnabled = false;
					adcOnOff.IsChecked = false;
				}
			}
			else
			{
				tempDepthLogginUD.Text = conf[58];
			}

		}

	}

}