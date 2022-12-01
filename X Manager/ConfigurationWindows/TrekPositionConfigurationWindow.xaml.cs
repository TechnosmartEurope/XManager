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
using X_Manager.Units;
using X_Manager.ConfigurationWindows;
using X_Manager;
using X_Manager.Units.AxyTreks;

namespace X_Manager.ConfigurationWindows
{

	public partial class TrekPositionConfigurationWindow : ConfigurationWindow
	{
		//public bool failure = false;
		//bool setIdle = false;
		public bool unitConnected = false;
		public UInt32 firmTotB_loc = 0;
		public Unit unit;
		public bool connected = false;
		MainWindow mw;
		public TrekPositionConfigurationWindow(MainWindow mw, Unit unitIn)
		{
			InitializeComponent();
			Loaded += loaded;
			Unloaded += unloaded;
			if (unitIn != null)
			{
				unit = unitIn;
				connected = true;
			}
			else
			{
				unit = new AxyTrekN(mw);
			}
			this.mw = mw;
			if (!(unit == null))
			{
				readButton.IsEnabled = true;
				sendButton.IsEnabled = true;
				//setIdle = true;
				unitConnected = true;
				firmTotB_loc = unit.firmTotB;
				res = true;
			}

			for (UInt16 i = 1; i <= 9; i++)
			{
				TabItem t = new TabItem();
				// With...
				t.FontSize = 12;
				t.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0xaa, 0xde));
				t.Name = "day" + i.ToString() + "TabItem";
				t.Header = "Day " + i.ToString();
				t.Width = 65;
				t.Height = 30;
				t.Background = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30));
				t.Margin = new Thickness(2);

				ScheduleTab.Items.Add(t);
			}

			((TabItem)ScheduleTab.Items.GetItemAt(7)).Header = "Settings";
			((TabItem)ScheduleTab.Items.GetItemAt(7)).Name = "Settings";
			((TabItem)ScheduleTab.Items.GetItemAt(8)).Header = "Remote";
			((TabItem)ScheduleTab.Items.GetItemAt(8)).Name = "Remote";

			defaultSettings();
		}

		private void loaded(object sender, RoutedEventArgs e)
		{
			bool result = false;
			while (!result)         //Sistemare, forse result non viene messo a true
			{
				if (!System.IO.File.Exists(trekSchedFile))
				{
					exportSchedule(trekSchedFile);
					result = true;
				}
				else
				{
					result = importSchedule(trekSchedFile, false);
					if (!result)
					{
						try
						{
							System.IO.File.Delete(trekSchedFile);
						}
						catch { }
					}
				}
			}
			if (unitConnected && (firmTotB_loc < 3004000))
			{
				((TabItem)ScheduleTab.Items.GetItemAt(8)).Visibility = Visibility.Hidden;
			}
		}

		private void unloaded(object sender, EventArgs e)
		{
			exportSchedule(trekSchedFile);
		}

		private void dailyCheckChecked(object sender, RoutedEventArgs e)
		{
			byte ccount = 1;
			foreach (TabItem t in ScheduleTab.Items)
			{
				if ((ccount == 1))
				{
					DayTab dt = (DayTab)t.Content;
					dt.dayLabel.Content = "Every day";
					dt.copyToNextDayButton.Visibility = Visibility.Hidden;
					dt.copyToNextDayButton.IsEnabled = false;
				}
				if (((ccount != 1) && ((ccount != 8) && (ccount != 9)))) t.Visibility = Visibility.Hidden;
				ccount++;
			}

			ScheduleTab.SelectedIndex = 0;
			((TabItem)ScheduleTab.Items.GetItemAt(0)).Header = "Every day";
			if (unitConnected)
			{
				if (firmTotB_loc < 3004000)
				{
					((TabItem)ScheduleTab.Items.GetItemAt(8)).Visibility = Visibility.Hidden;
				}
			}
		}

		private void weeklyCheckChecked(object sender, RoutedEventArgs e)
		{
			foreach (TabItem t in ScheduleTab.Items)
			{
				t.Visibility = Visibility.Visible;
				try
				{
					DayTab dt = (DayTab)t.Content;
					if (dt.Name == "dayTab1")
					{
						dt.dayLabel.Content = "Day 1";
						dt.copyToNextDayButton.IsEnabled = true;
						dt.copyToNextDayButton.Visibility = Visibility.Visible;
					}
				}
				catch { }
			}
			((TabItem)ScheduleTab.Items.GetItemAt(0)).Header = "Day 1";
			if (unitConnected)
			{
				if (firmTotB_loc < 3004000)
				{
					((TabItem)ScheduleTab.Items.GetItemAt(8)).Visibility = Visibility.Hidden;
				}
			}
		}

		private void readButtonClick(object sender, RoutedEventArgs e)  //Controllare format string
		{
			defaultSettings();
			byte[] conf;// = new byte[200];
			try
			{
				conf = unit.getGpsSchedule();
			}
			catch (Exception ex)
			{
				res = false;
				MessageBox.Show(ex.Message);
				Close();
				return;
			}

			//Acquisition On e Off time
			TrekSettingsTab st = new TrekSettingsTab(this);
			st.onTimeNud.Value = conf[1] * 256 + conf[2];
			st.offTimeNud.Value = conf[3] * 256 + conf[4];

			//Schedule e numero di intervalli per giorno
			DayTab dt;
			for (int i = 0; i <= 6; i++)
			{
				// numero di intervalli
				dt = ((DayTab)((TabItem)ScheduleTab.Items.GetItemAt(i)).Content);
				dt.numeroIntervalliCB.SelectedIndex = conf[153 + i] - 1;
				//giorni
				dt.I1H2.SelectedItem = conf[(5 + (i * 19))].ToString("D2");
				dt.I2H2.SelectedItem = conf[(8 + (i * 19))].ToString("D2");
				dt.I3H2.SelectedItem = conf[(11 + (i * 19))].ToString("D2");
				dt.I4H2.SelectedItem = conf[(14 + (i * 19))].ToString("D2");
				dt.I5H2.SelectedItem = conf[(17 + (i * 19))].ToString("D2");
				//modi
				dt.I1M.SelectedIndex = conf[(6 + (i * 19))];
				dt.I2M.SelectedIndex = conf[(9 + (i * 19))];
				dt.I3M.SelectedIndex = conf[(12 + (i * 19))];
				dt.I4M.SelectedIndex = conf[(15 + (i * 19))];
				dt.I5M.SelectedIndex = conf[(18 + (i * 19))];
				//parametri
				dt.I1P.SelectedIndex = conf[(7 + (i * 19))];
				dt.I2P.SelectedIndex = conf[(10 + (i * 19))];
				dt.I3P.SelectedIndex = conf[(13 + (i * 19))];
				dt.I4P.SelectedIndex = conf[(16 + (i * 19))];
				dt.I5P.SelectedIndex = conf[(19 + (i * 19))];
			}

			st.dEnabled.IsChecked = false;
			st.dDisabled.IsChecked = true;
			if (conf[138] == 1)
			{
				st.dDisabled.IsChecked = false;
				st.dEnabled.IsChecked = true;
			}

			if (unit is AxyTrekR)
			{

				byte[] rOn = new byte[] { 0, 3, 5, 10, 15, 20, 25, 30 };
				st.radarOnTimeCB.SelectedIndex = Array.IndexOf(rOn, conf[139]);

				int[] rPer = new int[] { 10, 20, 30, 60, 300, 600, 1800, 3600 };
				st.radarPeriodCB.SelectedIndex = Array.IndexOf(rPer, conf[160] * 256 + conf[161]);
			}
			else
			{
				st.adcRecording.IsChecked = false;
				if (conf[139] == 1) st.adcRecording.IsChecked = true;

				st.magMinB.Content = "<";
				if ((conf[160] & 4) == 4) st.magMinB.Content = ">";

				st.adcTrigger.IsChecked = false;
				if ((conf[160] & 8) == 8) st.adcTrigger.IsChecked = true;

				st.ADCValueUD.Value = ((conf[160] & 3) * 256) + conf[161];
			}

			st.startDelayNud.Value = (conf[140] * 256) + conf[141];

			if (firmTotB_loc < 3002000)
			{
				if (st.startDelayNud.Value == 0) st.setSdRb(0);
				else st.setSdRb(1);
			}
			else
			{
				st.setSdRb(conf[142]);
				st.setSdDate(new double[] { (double)conf[143] + 2000, conf[144], conf[145] });
			}

			WeeklyCheck.IsChecked = true;
			if (conf[152] == 1) DailyCheck.IsChecked = true;

			double a = (conf[162] * 256) + conf[163];
			a *= 6; a /= 1023; a = Math.Round(a, 2);

			st.dEnabled.IsChecked = false;
			if (conf[138] == 1) st.dEnabled.IsChecked = true;
			st.dDisabled.IsChecked = !st.dEnabled.IsChecked;

			st.nSatNud.Value = conf[168];
			st.acqSumNud.Value = conf[169];
			st.acq1Nud.Value = conf[170];
			st.acq2Nud.Value = conf[171];

			manageSensorAppearance(ref st);
			manageStartDelayAppearance(ref st);

			TrekRemoteIntervals rtClone = (TrekRemoteIntervals)((TrekRemoteIntervals)((TabItem)ScheduleTab.Items.GetItemAt(8)).Content).Clone();

			ScheduleTab.Items.RemoveAt(7);
			try
			{
				ScheduleTab.Items.RemoveAt(7);
			}
			catch { }

			var t = new TabItem();
			t.FontSize = 12;
			t.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0xaa, 0xde));
			t.Name = "Settings";
			t.Header = "Settings";
			t.Width = 65;
			t.Height = 30;
			t.Background = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30));
			t.Margin = new Thickness(2);

			t.Content = st;
			ScheduleTab.Items.Add(t);


			TrekRemoteIntervals rt = null;
			if (firmTotB_loc > 3003999)
			{
				rt = new TrekRemoteIntervals(this);
				double[] rem = new double[23];
				Array.Copy(conf, 172, rem, 16, 7);
				rt.import(rem);

				//rt.impo

			}
			else
			{
				rt = rtClone;
			}
			var r = new TabItem();
			r.FontSize = 12;
			r.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0xaa, 0xde));
			r.Name = "Remote";
			r.Header = "Remote";
			r.Width = 65;
			r.Height = 30;
			r.Background = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30));
			r.Margin = new Thickness(2);

			r.Content = rt;
			ScheduleTab.Items.Add(r);
			if (firmTotB_loc < 3004000)
			{
				((TabItem)ScheduleTab.Items.GetItemAt(8)).Visibility = Visibility.Hidden;
			}

			//MessageBox.Show("Configuration read.");
			var w = new Warning("Configuration succesfully read");
			w.ShowDialog();

		}

		private void sendButtonClick(object sender, RoutedEventArgs e)
		{
			byte[] conf = new byte[200];

			//Acquisition On e Off time
			TrekSettingsTab st = (TrekSettingsTab)(((TabItem)(ScheduleTab.Items.GetItemAt(7))).Content);
			conf[1] = (byte)(((UInt16)st.onTimeNud.Value & 0xff00) >> 8);
			conf[2] = (byte)((UInt16)st.onTimeNud.Value & 0xff);
			conf[3] = (byte)(((UInt16)st.offTimeNud.Value & 0xff00) >> 8);
			conf[4] = (byte)((UInt16)st.offTimeNud.Value & 255);

			DayTab dt;

			for (int i = 0; i < 7; i++)
			{
				dt = ((DayTab)((TabItem)ScheduleTab.Items.GetItemAt(i)).Content);
				conf[153 + i] = (byte)(dt.numeroIntervalliCB.SelectedIndex + 1);

				//giorni
				conf[5 + i * 19] = byte.Parse((string)dt.I1H2.SelectedItem);
				conf[8 + i * 19] = byte.Parse((string)dt.I2H2.SelectedItem);
				conf[11 + i * 19] = byte.Parse((string)dt.I3H2.SelectedItem);
				conf[14 + i * 19] = byte.Parse((string)dt.I4H2.SelectedItem);
				conf[17 + i * 19] = byte.Parse((string)dt.I5H2.SelectedItem);

				//modi
				conf[6 + i * 19] = (byte)dt.I1M.SelectedIndex;
				conf[9 + i * 19] = (byte)dt.I2M.SelectedIndex;
				conf[12 + i * 19] = (byte)dt.I3M.SelectedIndex;
				conf[15 + i * 19] = (byte)dt.I4M.SelectedIndex;
				conf[18 + i * 19] = (byte)dt.I5M.SelectedIndex;

				//parametri
				try { conf[7 + i * 19] = (byte)dt.I1P.SelectedIndex; }
				catch { }
				try { conf[10 + i * 19] = (byte)dt.I2P.SelectedIndex; }
				catch { }
				try { conf[13 + i * 19] = (byte)dt.I3P.SelectedIndex; }
				catch { }
				try { conf[16 + i * 19] = (byte)dt.I4P.SelectedIndex; }
				catch { }
				try { conf[19 + i * 19] = (byte)dt.I5P.SelectedIndex; }
				catch { }
			}
			conf[138] = 0; if ((bool)st.dEnabled.IsChecked) conf[138] = 1;

			if (unit is AxyTrekR)
			{
				byte[] rOn = new byte[] { 0, 3, 5, 10, 15, 20, 25, 30 };
				conf[139] = rOn[st.radarOnTimeCB.SelectedIndex];

				int[] rPer = new int[] { 10, 20, 30, 60, 300, 600, 1800, 3600 };
				int val = rPer[st.radarPeriodCB.SelectedIndex];
				conf[160] = (byte)(val >> 8);
				conf[161] = (byte)(val & 0xff);
			}
			else
			{
				conf[139] = 0; if ((bool)st.adcRecording.IsChecked) conf[139] = 1;

				conf[160] = (byte)(((UInt16)st.ADCValueUD.Value) >> 8);
				conf[161] = (byte)(((UInt16)st.ADCValueUD.Value) & 0xff);
				if ((string)st.magMinB.Content == ">") conf[160] += 4;
				if ((bool)st.adcTrigger.IsChecked) conf[160] += 8;
			}

			conf[140] = (byte)(((UInt16)st.startDelayNud.Value) >> 8);
			conf[141] = (byte)(((UInt16)st.startDelayNud.Value) & 0xff);
			conf[142] = 0;
			if ((bool)st.ByTimeRB.IsChecked) conf[142] = 1;
			else if ((bool)st.ByDateRB.IsChecked) conf[142] = 2;

			if (firmTotB_loc < 3002000)
			{
				if ((bool)st.OffRB.IsChecked)
				{
					conf[140] = 0;
					conf[141] = 0;
				}
			}

			conf[143] = (byte)(st.SdDateTimePicker.Value.Value.Year - 2000);
			conf[144] = (byte)(st.SdDateTimePicker.Value.Value.Month);
			conf[145] = (byte)(st.SdDateTimePicker.Value.Value.Day);

			conf[152] = 0;
			if ((bool)DailyCheck.IsChecked)
			{
				conf[152] = 1;
				for (int i = 1; i <= 6; i++)
				{
					for (int j = 5; j <= 23; j++)
					{
						conf[j + 19 * i] = conf[j];
					}
				}
			}

			for (int i = 162; i <= 167; i++)
			{
				conf[i] = 0;
			}

			conf[168] = (byte)st.nSatNud.Value;
			conf[169] = (byte)st.acqSumNud.Value;
			conf[170] = (byte)st.acq1Nud.Value;
			conf[171] = (byte)st.acq2Nud.Value;

			//Finestre remoto
			if (firmTotB_loc > 3003999)
			{
				double[] rem = ((TrekRemoteIntervals)((TabItem)(ScheduleTab.Items.GetItemAt(8))).Content).export();
				conf[172] = (byte)rem[0];
				conf[173] = (byte)rem[1];
				conf[174] = (byte)rem[2];
				conf[175] = (byte)rem[3];
				conf[176] = (byte)rem[4];
				conf[177] = (byte)rem[5];
				conf[178] = (byte)rem[6];
			}

			try
			{
				unit.setGpsSchedule(conf);
			}
			catch (Exception ex)
			{
				res = false;
				MessageBox.Show(ex.Message);
				Close();
				return;
			}

			//MessageBox.Show("Configuration succesfully updated.");
			var w = new Warning("Configuration succesfully updated.");
			w.ShowDialog();

		}

		private void resetClick(object sender, RoutedEventArgs e)
		{
			defaultSettings();
		}

		private void defaultSettings()
		{
			WeeklyCheck.IsChecked = true;
			UInt16 ccount = 1;
			//bool cHyde = true;
			foreach (TabItem t in ScheduleTab.Items)
			{
				//cHyde = false;
				//if (ccount == 7) cHyde = true;
				DayTab cc = new DayTab("Day " + ccount.ToString());
				cc.copyToNextDayEvent += gestioneCopiaDayTab;
				cc.Name = "dayTab" + ccount.ToString();
				cc.resetta();
				cc.copyToNextDayTB.Text = "Copy to day " + (ccount + 1).ToString() + " ->";
				if (ccount == 7)
				{
					cc.copyToNextDayButton.IsEnabled = false;
					cc.copyToNextDayButton.Visibility = Visibility.Hidden;
				}
				t.Content = cc;
				ccount++;
			}
			TrekSettingsTab tt = new TrekSettingsTab(this);
			manageSensorAppearance(ref tt);
			manageStartDelayAppearance(ref tt);
			((TabItem)ScheduleTab.Items.GetItemAt(7)).Content = tt;

			TrekRemoteIntervals tr = new TrekRemoteIntervals(this);
			tr.import(new double[23]);
			((TabItem)(ScheduleTab.Items.GetItemAt(8))).Content = tr;

			WeeklyCheck.IsChecked = true;
		}

		private void gestioneCopiaDayTab(object sender, string name)
		{
			string[] compStrAr = name.Split(' ');
			byte oldIndex = (byte)(byte.Parse(compStrAr[1]) - 1);
			var newIndex = oldIndex + 1;

			var oldDayTab = ((DayTab)((TabItem)(ScheduleTab.Items.GetItemAt(oldIndex))).Content);
			double[] orariD = oldDayTab.export();
			sbyte[] orariS = new sbyte[16];

			for (int i = 0; i <= (orariD.Length - 1); i++)
			{
				orariS[i] = (sbyte)(orariD[i]);
			}
			DayTab newDayTab = new DayTab("Day " + (newIndex + 1).ToString());
			newDayTab.Name = "dayTab" + (newIndex + 1).ToString();
			if (newIndex != 6)
			{
				newDayTab.copyToNextDayTB.Text = "Copy to day " + (newIndex + 2).ToString() + " ->";
				newDayTab.copyToNextDayEvent += gestioneCopiaDayTab;
			}
			else newDayTab.copyToNextDayButton.Visibility = Visibility.Hidden;
			newDayTab.import(orariS);

			((TabItem)(ScheduleTab.Items.GetItemAt(newIndex))).Content = newDayTab;
			ScheduleTab.SelectedIndex = newIndex;
		}

		private void saveScheduleAs(object sender, RoutedEventArgs e)
		{
			//MainWindow.lastSettings = System.IO.File.ReadAllLines(iniFile);
			var saveSchedule = new Microsoft.Win32.SaveFileDialog();

			if (X_Manager.Parent.getParameter("trekScheduleSavePath") != null) saveSchedule.InitialDirectory = X_Manager.Parent.getParameter("trekScheduleSavePath");
			else saveSchedule.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

			saveSchedule.DefaultExt = ("Schedule Files|*.sch");
			saveSchedule.Filter = ("Schedule Files|*.sch");
			saveSchedule.OverwritePrompt = true;
			if ((bool)saveSchedule.ShowDialog())
			{
				exportSchedule(saveSchedule.FileName);
				X_Manager.Parent.updateParameter("trekScheduleSavePath", System.IO.Path.GetDirectoryName(saveSchedule.FileName));
				//MainWindow.lastSettings[1] = System.IO.Path.GetDirectoryName(saveSchedule.FileName);
				//System.IO.File.WriteAllLines(iniFile, MainWindow.lastSettings);
			}

		}

		private void openSchedule(object sender, RoutedEventArgs e)
		{
			//MainWindow.lastSettings = System.IO.File.ReadAllLines(iniFile);
			var openSchedule = new Microsoft.Win32.OpenFileDialog();
			if (X_Manager.Parent.getParameter("trekScheduleOpenPath") != "null")
			{
				openSchedule.InitialDirectory = X_Manager.Parent.getParameter("trekScheduleOpenPath");
			}
			else
			{
				openSchedule.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			}

			openSchedule.DefaultExt = ("Schedule Files|*.sch");
			openSchedule.Filter = ("Schedule Files|*.sch");
			if ((bool)openSchedule.ShowDialog())
			{
				importSchedule(openSchedule.FileName, true);
				X_Manager.Parent.updateParameter("trekScheduleOpenPath", System.IO.Path.GetDirectoryName(openSchedule.FileName));
				//MainWindow.lastSettings[2] = System.IO.Path.GetDirectoryName(openSchedule.FileName);
				//System.IO.File.WriteAllLines(iniFile, MainWindow.lastSettings);
			}
		}

		private bool importSchedule(string fileName, bool checkCompatibility)
		{
			System.IO.BinaryReader fi = new System.IO.BinaryReader(System.IO.File.Open(fileName, System.IO.FileMode.Open));
			string header = "";
			try
			{
				header = fi.ReadString();
			}
			catch
			{
				fi.Close();
				return false;
			}

			if (!header.Contains("TSM-TREK"))
			{
				fi.Close();
				return false;
			}

			Unit importUnit = null;
			switch (header.Substring(8))
			{
				case "":
				case "N":
					importUnit = new AxyTrekN(mw);
					break;
				case "CO2":
					importUnit = new AxyTrekCO2(mw);
					break;
				case "R":
					importUnit = new AxyTrekR(mw);
					break;
				case "FT":
					importUnit = new AxyTrekFT(mw);
					break;
				case "HD":
					importUnit = new AxyTrekHD(mw);
					break;
			}

			if (connected)
			{
				if (importUnit.modelCode != unit.modelCode)
				{
					if (checkCompatibility)
					{
						MessageBox.Show("WARNING: the selected configuration belongs to a different AxyTrek type.");
					}
					fi.Close();
					return false;
				}
			}

			double high = fi.ReadDouble();
			double low = fi.ReadDouble();
			if (high < 3)
			{
				if (low < 9)
				{
					fi.Close();
					return false;
				}
			}
			defaultSettings();

			double dw = fi.ReadDouble();
			sbyte[] schS = new sbyte[16];
			for (byte dayCount = 0; (dayCount <= 6); dayCount++)
			{
				for (int valCount = 0; (valCount <= 15); valCount++)
				{
					schS[valCount] = (sbyte)fi.ReadDouble();
				}
				((DayTab)((TabItem)ScheduleTab.Items.GetItemAt(dayCount)).Content).import(schS);
			}

			double[] setD = new double[23];
			for (int valcount = 0; valcount <= 7; valcount++)
			{
				setD[valcount] = fi.ReadDouble();
			}

			try
			{
				setD[8] = fi.ReadDouble();
			}
			catch { }

			try
			{
				setD[9] = fi.ReadDouble();
				setD[10] = fi.ReadDouble();
				setD[11] = fi.ReadDouble();
			}
			catch { }

			try
			{
				setD[12] = fi.ReadDouble();
				setD[13] = fi.ReadDouble();
				setD[14] = fi.ReadDouble();
				setD[15] = fi.ReadDouble();
			}
			catch
			{
				setD[12] = 0;
				setD[13] = 0;
				setD[14] = 0;
				setD[15] = 0;
			}

			try
			{
				setD[16] = fi.ReadDouble();
				setD[17] = fi.ReadDouble();
				setD[18] = fi.ReadDouble();
				setD[19] = fi.ReadDouble();
				setD[20] = fi.ReadDouble();
				setD[21] = fi.ReadDouble();
				setD[22] = fi.ReadDouble();
			}
			catch
			{
				setD[16] = 0;
				setD[17] = 0;
				setD[18] = 0;
				setD[19] = 0;
				setD[20] = 0;
				setD[21] = 0;
				setD[22] = 0;
			}


			if (dw == 1) DailyCheck.IsChecked = true;
			else WeeklyCheck.IsChecked = true;

			((TabItem)ScheduleTab.Items.GetItemAt(7)).Content = null;
			var settingTab = new TrekSettingsTab(this);
			settingTab.import(setD);
			//var settingTab = (TrekSettingsTab)((TabItem)ScheduleTab.Items.GetItemAt(7)).Content;
			//settingTab.import(setD);
			manageSensorAppearance(ref settingTab);
			manageStartDelayAppearance(ref settingTab);
			((TabItem)ScheduleTab.Items.GetItemAt(7)).Content = settingTab;
			((TabItem)ScheduleTab.Items.GetItemAt(7)).Header = "Settings";
			((TabItem)ScheduleTab.Items.GetItemAt(7)).Name = "Settings";

			var remoteTab = (TrekRemoteIntervals)((TabItem)ScheduleTab.Items.GetItemAt(8)).Content;
			remoteTab.import(setD);
			fi.Close();

			//unit = null;
			importUnit = null;

			return true;
		}

		private void exportSchedule(string fileName)
		{
			var fo = new System.IO.BinaryWriter(System.IO.File.Open(fileName, System.IO.FileMode.Create));

			string header = "TSM-TREK";
			if (unit is AxyTrekN)
			{
				header += "N";
			}
			else if (unit is AxyTrekCO2)
			{
				header += "CO2";
			}
			else if (unit is AxyTrekFT)
			{
				header += "FT";
			}
			else if (unit is AxyTrekHD)
			{
				header += "HD";
			}
			else if (unit is AxyTrekR)
			{
				header += "R";
			}
			fo.Write(header);
			double dw = 2;
			fo.Write(dw);
			dw = 9;
			fo.Write(dw);
			dw = 0;
			if ((bool)DailyCheck.IsChecked) dw = 1;
			fo.Write(dw);

			double[] schedD = new double[16];
			for (int ccount = 0; ccount <= 6; ccount++)
			{
				var dt = (DayTab)((TabItem)ScheduleTab.Items.GetItemAt(ccount)).Content;
				schedD = dt.export();
				for (int dCount = 0; dCount <= 15; dCount++)
				{
					fo.Write(schedD[dCount]);
				}
			}

			var dt2 = (TrekSettingsTab)((TabItem)ScheduleTab.Items.GetItemAt(7)).Content;
			double[] setD = dt2.export();
			foreach (double value in setD)
			{
				fo.Write(value);
			}

			var dt3 = (TrekRemoteIntervals)((TabItem)ScheduleTab.Items.GetItemAt(8)).Content;
			double[] setR = dt3.export();
			foreach (double value in setR)
			{
				fo.Write(value);
			}

			fo.Close();
		}

		private void manageSensorAppearance(ref TrekSettingsTab tt)
		{
			if (unitConnected)
			{
				if (firmTotB_loc > 3000000) tt.switchToAdc();
				else
				{
					if (firmTotB_loc > 2000000) tt.disableWaterControl();
				}
			}
			else tt.switchToAdc();
		}

		private void manageStartDelayAppearance(ref TrekSettingsTab tt)
		{
			if (unitConnected)
			{
				if (firmTotB_loc > 3002000) tt.SdByDateSP.Visibility = Visibility.Visible;
				else
				{
					tt.SdByDateSP.Visibility = Visibility.Hidden;
					if (tt.getSdRb() == 2) tt.setSdRb(0);
				}
			}
			else tt.SdByDateSP.Visibility = Visibility.Visible;
		}

		public new bool? ShowDialog()
		{
			base.ShowDialog();
			return res;
		}

	}
}
