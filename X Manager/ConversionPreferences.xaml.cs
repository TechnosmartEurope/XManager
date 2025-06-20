using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;


namespace X_Manager
{
	/// <summary>
	/// Interaction logic for ConversionPreferences.xaml
	/// </summary>

	public partial class ConversionPreferences : Window
	{

		//string prefFile = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + MainWindow.companyFolder + MainWindow.appFolder + "\\convPrefs.ini";
		// "\\TecnoSmArt Europe\\X Manager\\convPresf.ini";
		//string[] lastPrefs;
		public bool OldUnitDebug = false;
		public bool goOn;
		public bool debugEvents = false;
		public byte debugLevel = 0;
		public bool addGpsTime = false;
		public bool overrideSystemDate = false;
		public bool isRem = false;
		public bool overrideTime = false;
		bool am;
		public bool events = false;
		public bool proximity = false;
		//bool removeNonGps = false;

		//const int pref_pressMetri = 0;
		//const int pref_millibars = 1;
		//const int pref_dateFormat = 2;
		//const int pref_timeFormat = 3;
		//const int pref_fillEmpty = 4;
		//const int pref_sameColumn = 5;
		//const int pref_battery = 6;
		//const int pref_txt = 7;
		//const int pref_kml = 8;
		//const int pref_h = 9;
		//const int pref_m = 10;
		//const int pref_s = 11;
		//const int pref_date_year = 12;
		//const int pref_date_month = 13;
		//const int pref_date_day = 14;
		//const int pref_time_override = 15;
		//const int pref_metadata = 16;
		//const int pref_leapSeconds = 17;
		//const int pref_nonGps = 18;
		//const int pref_proximity = 19;

		string fileName;
		public ConversionPreferences(string fileName)
		{
			DataContext = this;
			InitializeComponent();

			groundLevelAirPressure.minValue = 100;
			groundLevelAirPressure.maxValue = 5000;
			goOn = false;

			Loaded += loaded;
			Closing += closing;
			this.fileName = fileName;

			proximityCB.Visibility = System.IO.Path.GetExtension(fileName).Contains("6") ? Visibility.Visible : Visibility.Hidden;

		}

		private void loaded(object sender, RoutedEventArgs e)
		{

			if (Properties.Settings.Default.CONV_PRESSURE_UNIT == "millibars")
			{
				Millibars.IsChecked = true;
			}
			else
			{
				Meters.IsChecked = true;
			}
			groundLevelAirPressure.Value = Properties.Settings.Default.CONV_PRESSURE_OFFSET;
			switch (Properties.Settings.Default.CONV_DATE_FORMAT)
			{
				case 1:
					date1.IsChecked = true;
					//dateTimePicker.FormatString = "dd/MM/yyyy";
					break;
				case 2:
					date2.IsChecked = true;
					//dateTimePicker.FormatString = "MM/dd/yyyy";
					break;
				case 3:
					date3.IsChecked = true;
					//dateTimePicker.FormatString = "yyyy/MM/dd";
					break;
				case 4:
					date4.IsChecked = true;
					//dateTimePicker.FormatString = "yyyy/dd/MM";
					break;
			}
			switch (Properties.Settings.Default.CONV_TIME_FORMAT)
			{
				case 1:
					time1.IsChecked = true;
					break;
				case 2:
					time2.IsChecked = true;
					break;
			}
			fill.IsChecked = Properties.Settings.Default.CONV_FILL_EMPTY;
			same.IsChecked = Properties.Settings.Default.CONV_SAME_COLUMN;

			byte p = (byte)Properties.Settings.Default.CONV_TIME.Hour;
			if ((bool)time1.IsChecked)
			{
				hUd.footerContent = "";
				hUd.maxValue = 23;
				hUd.minValue = 0;
				hUd.Value = p;
				amLabel.Visibility = Visibility.Hidden;
				pmLabel.Visibility = Visibility.Hidden;
			}
			else
			{
				amLabel.Visibility = Visibility.Visible;
				pmLabel.Visibility = Visibility.Visible;
				hUd.maxValue = 12;
				hUd.minValue = 1;
				if (p < 12)
				{
					switchToAm();
					if (p == 0) p = 12;
				}
				else
				{
					switchToPm();
					if (p != 12) p -= 12;
				}
				hUd.Value = p;
			}

			hUd.footer.Width = 0;
			mUd.Value = Properties.Settings.Default.CONV_TIME.Minute;
			sUd.Value = Properties.Settings.Default.CONV_TIME.Second;
			var dd = Properties.Settings.Default.CONV_TIME;
			dateTimePicker.SelectedDate = dd;

			OverrideTime.IsChecked = Properties.Settings.Default.CONV_TIME_OVERRIDE;
			metadataCB.IsChecked = Properties.Settings.Default.CONV_METADATA;
			leapSecondsUD.Value = (double)Properties.Settings.Default.CONV_LEAP_SECONDS;
			removeNonGps.IsChecked = Properties.Settings.Default.CONV_NON_GPS;
			proximityCB.IsChecked = Properties.Settings.Default.CONV_PROXIMITY;
			splitKmlCB.IsChecked = Properties.Settings.Default.CONV_SPLIT_KML;
			switch (Properties.Settings.Default.CONV_SPLIT_KML_EVERY)
			{
				case 100:
					splitKmlCBB.SelectedIndex = 0;
					break;
				case 1000:
					splitKmlCBB.SelectedIndex = 1;
					break;
				case 10000:
					splitKmlCBB.SelectedIndex = 2;
					break;
			}
			splitKmlCBB.SelectionChanged += splitKmlCBB_SelectionChanged;
		}

		private void ctrlManager(object sender, KeyEventArgs e)
		{
			if (Keyboard.IsKeyDown(Key.LeftCtrl) | Keyboard.IsKeyDown(Key.RightCtrl))
			{
				switch (e.Key)
				{
					case Key.H:
						string testo = "H: questo messagio\r\n";
						testo += "D: livello di debug\r\n";
						testo += "A: switch MdebugPicB con MemFull\r\n";
						testo += "G: Aggiunge orario GPS alla colonna timestamp";
						MessageBox.Show(testo);
						break;
					case Key.D:
						string testo1 = "CONVERSION SETTINGS";

						switch (debugLevel)
						{
							case 0:
								//if (System.IO.Path.GetExtension(fileName).Contains("6"))
								//{
								convSettingsGB.Header = testo1 + " (D)";
								//}
								//else
								//{
								//	txt.Content = testo1 + " (d1)";
								//}
								debugLevel = 1;
								break;
							case 1:
								//if (System.IO.Path.GetExtension(fileName).Contains("6"))
								//{
								convSettingsGB.Header = testo1;
								debugLevel = 0;
								//}
								//else
								//{
								//	txt.Content = testo1 + " (d2)";
								//	debugLevel = 2;
								//}
								break;
								//case 2:
								//	txt.Content = testo1 + " (d3)";
								//	debugLevel = 3;
								//	break;
								//case 3:
								//	txt.Content = testo1;
								//	debugLevel = 0;
								//	break;
						}
						break;
					case Key.A:
						if (!OldUnitDebug)
						{
							MessageBox.Show("Old unit debug.");
							OldUnitDebug = true;
						}
						else
						{
							MessageBox.Show("New unit debug.");
							OldUnitDebug = false;
						}
						break;
					case Key.G:
						if (same.IsChecked == true)
						{
							if (addGpsTime)
							{
								addGpsTime = false;
								same.Content = "Date and Time on the same column";
							}
							else
							{
								addGpsTime = true;
								same.Content = "Date and Time on the same column + GPS time";
							}
						}
						break;
					case Key.F:
						if (overrideSystemDate)
						{
							overrideSystemDate = false;
							MessageBox.Show("System time independent.");
						}
						else
						{
							overrideSystemDate = true;
							MessageBox.Show("System time adjusted by GNSS time.");
						}
						break;

				}
			}

			if (e.Key == Key.Return)
			{
				goOn = true;
				Close();
			}
		}

		private void closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (!goOn) return;

			if ((bool)Millibars.IsChecked)
			{
				Properties.Settings.Default.CONV_PRESSURE_UNIT = "millibars";
			}
			else
			{
				Properties.Settings.Default.CONV_PRESSURE_UNIT = "meters";
			}
			Properties.Settings.Default.CONV_PRESSURE_OFFSET = groundLevelAirPressure.Value;
			if ((bool)date1.IsChecked)
			{
				Properties.Settings.Default.CONV_DATE_FORMAT = 1;
			}
			else if ((bool)date2.IsChecked)
			{
				Properties.Settings.Default.CONV_DATE_FORMAT = 2;
			}
			else if ((bool)date3.IsChecked)
			{
				Properties.Settings.Default.CONV_DATE_FORMAT = 3;
			}
			else if ((bool)date4.IsChecked)
			{
				Properties.Settings.Default.CONV_DATE_FORMAT = 4;
			}
			Properties.Settings.Default.CONV_TIME_FORMAT = 2;
			if ((bool)time1.IsChecked) Properties.Settings.Default.CONV_TIME_FORMAT = 1;
			Properties.Settings.Default.CONV_FILL_EMPTY = (bool)fill.IsChecked;
			Properties.Settings.Default.CONV_SAME_COLUMN = (bool)same.IsChecked;
			Properties.Settings.Default.CONV_TIME_OVERRIDE = (bool)OverrideTime.IsChecked;

			double p = hUd.Value;
			if ((bool)time2.IsChecked)
			{
				if (am)
				{
					if (p == 12) p = 0;
				}
				else
				{
					if (p != 12) p += 12;
				}
			}
			DateTime pdt = (DateTime)dateTimePicker.SelectedDate;
			Properties.Settings.Default.CONV_TIME = new DateTime(pdt.Year, pdt.Month, pdt.Day, (int)hUd.Value, (int)mUd.Value, (int)sUd.Value);
			Properties.Settings.Default.CONV_METADATA = (bool)metadataCB.IsChecked;
			Properties.Settings.Default.CONV_LEAP_SECONDS = (int)leapSecondsUD.Value;
			Properties.Settings.Default.CONV_NON_GPS = (bool)removeNonGps.IsChecked;
			Properties.Settings.Default.CONV_PROXIMITY = (bool)proximityCB.IsChecked;
			Properties.Settings.Default.CONV_SPLIT_KML = (bool)splitKmlCB.IsChecked;
			Properties.Settings.Default.CONV_SPLIT_KML_EVERY = (int)Math.Pow(10, splitKmlCBB.SelectedIndex + 2);
		}

		private void metersChecked(object sender, RoutedEventArgs e)
		{
			groundLevelAirPressure.IsEnabled = true;
		}

		private void millibarsChecked(object sender, RoutedEventArgs e)
		{
			groundLevelAirPressure.IsEnabled = false;
		}

		private void doneClick(object sender, RoutedEventArgs e)
		{
			goOn = true;
			this.Close();
		}

		private void time1Checked(object sender, RoutedEventArgs e)
		{
			double p = hUd.Value;
			amLabel.Visibility = Visibility.Hidden;
			pmLabel.Visibility = Visibility.Hidden;

			hUd.maxValue = 23;
			hUd.minValue = 0;
			if (am)
			{
				if (p == 12) p = 0;
			}
			else
			{
				if (p != 12) p += 12;
			}
			hUd.footerContent = "";
			hUd.Value = p;
		}

		private void time2Checked(object sender, RoutedEventArgs e)
		{
			amLabel.Visibility = Visibility.Visible;
			pmLabel.Visibility = Visibility.Visible;
			double p = hUd.Value;
			hUd.maxValue = 12;
			hUd.minValue = 1;
			if (p < 12)
			{
				switchToAm();
				if (p == 0) p = 12;
			}
			else
			{
				switchToPm();
				if (p != 12) p -= 12;
			}
			hUd.Value = p;
		}

		private void amClick(object sender, RoutedEventArgs e)
		{
			switchToAm();
		}

		private void pmClick(object sender, RoutedEventArgs e)
		{
			switchToPm();
		}

		private void switchToAm()
		{
			am = true;
			amLabel.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0xaa, 0xde));
			pmLabel.Foreground = new SolidColorBrush(Color.FromArgb(255, 40, 40, 40));
		}

		private void switchToPm()
		{
			am = false;
			amLabel.Foreground = new SolidColorBrush(Color.FromArgb(255, 40, 40, 40));
			pmLabel.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0xaa, 0xde));
		}

		private void dateFormat4(object sender, RoutedEventArgs e)
		{
			//dateTimePicker.FormatString = "yyyy/dd/MM";
		}

		private void dateFormat3(object sender, RoutedEventArgs e)
		{
			//dateTimePicker.FormatString = "yyyy/MM/dd";
		}

		private void dateFormat2(object sender, RoutedEventArgs e)
		{
			//dateTimePicker.FormatString = "MM/dd/yyyy";
		}

		private void dateFormat1(object sender, RoutedEventArgs e)
		{
			//dateTimePicker.FormatString = "dd/MM/yyyy";
		}

		private void setMoveBank(object sender, RoutedEventArgs e)
		{
			Millibars.IsChecked = true;
			date1.IsChecked = true;
			//dateTimePicker.FormatString = "dd/MM/yyyy";
			time1.IsChecked = true;
			fill.IsChecked = false;
			same.IsChecked = true;
			//batteryCB.IsChecked = false;
			metadataCB.IsChecked = false;
		}

		private void splitKmlCB_Checked(object sender, RoutedEventArgs e)
		{
			Properties.Settings.Default.CONV_SPLIT_KML = (bool)splitKmlCB.IsChecked;
		}

		private void splitKmlCBB_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			Properties.Settings.Default.CONV_SPLIT_KML_EVERY = (int)Math.Pow(10, splitKmlCBB.SelectedIndex + 2);
		}
	}


}
