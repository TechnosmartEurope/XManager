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
	/// Interaction logic for ConversionPreferences.xaml
	/// </summary>

	public partial class ConversionPreferences : Window
	{

		string prefFile = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + MainWindow.companyFolder + MainWindow.appFolder + "\\convPrefs.ini";
		// "\\TecnoSmArt Europe\\X Manager\\convPresf.ini";
		string[] lastPrefs;
		public bool OldUnitDebug = false;
		public bool goOn;
		public bool debugEvents = false;
		public byte debugLevel = 0;
		public bool addGpsTime = false;
		public bool isRem = false;
		public bool overrideTime = false;
		bool am;
		public bool events = false;
		public bool proximity = false;
		//bool removeNonGps = false;

		const int pref_pressMetri = 0;
		const int pref_millibars = 1;
		const int pref_dateFormat = 2;
		const int pref_timeFormat = 3;
		const int pref_fillEmpty = 4;
		const int pref_sameColumn = 5;
		const int pref_battery = 6;
		const int pref_txt = 7;
		const int pref_kml = 8;
		const int pref_h = 9;
		const int pref_m = 10;
		const int pref_s = 11;
		const int pref_date_year = 12;
		const int pref_date_month = 13;
		const int pref_date_day = 14;
		const int pref_time_override = 15;
		const int pref_metadata = 16;
		const int pref_leapSeconds = 17;
		const int pref_nonGps = 18;
		const int pref_proximity = 19;

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

			if (System.IO.Path.GetExtension(fileName).Contains("6"))
			{
				//eventsCB.Visibility = Visibility.Visible;
				proximityCB.Visibility = Visibility.Visible;
			}
			else
			{
				//eventsCB.Visibility = Visibility.Hidden;
				proximityCB.Visibility = Visibility.Hidden;
			}
		}

		private void loaded(object sender, RoutedEventArgs e)
		{
			try
			{
				lastPrefs = System.IO.File.ReadAllLines(prefFile);
				string s = lastPrefs[16];

				if (s == "" | string.IsNullOrEmpty(s) | lastPrefs.Length != 20)
				{
					throw new Exception("");
				}

			}
			catch
			{
				var dt = DateTime.Now;
				string newPrefs = "millibars\r\n";              //pressione in millibar o metri
				newPrefs += "1016\r\n";                         //millibar a livello del mare
				newPrefs += "1\r\n";                            //formato data
				newPrefs += "1\r\n";                            //formato ora
				newPrefs += "False\r\n";                        //Fill empty fields
				newPrefs += "False\r\n";                        //Date and Time on the same column
				newPrefs += "False\r\n";                        //Battery
				newPrefs += "True\r\n";                         //TXT
				newPrefs += "True\r\n";                         //KML
				newPrefs += dt.Hour.ToString() + "\r\n";        //Ore
				newPrefs += dt.Minute.ToString() + "\r\n";      //Minuti
				newPrefs += dt.Second.ToString() + "\r\n";      //Secondi
				newPrefs += dt.Date.Year.ToString() + "\r\n";   //Anno
				newPrefs += dt.Date.Month.ToString() + "\r\n";  //Mese
				newPrefs += dt.Date.Day.ToString() + "\r\n";    //Giorno
				newPrefs += "False\r\n";                        //Override date time
				newPrefs += "True\r\n";                         //Metadata
				newPrefs += "2\r\n";                            //Leap seconds
				newPrefs += "False\r\n";                        //Sessioni senza gps
				newPrefs += "True\r\n";                         //Prossimità (gipsy6)

				System.IO.File.WriteAllText(prefFile, newPrefs);
			}

			lastPrefs = System.IO.File.ReadAllLines(prefFile);
			if (lastPrefs[pref_pressMetri] == "millibars")
			{
				Millibars.IsChecked = true;
			}
			else
			{
				Meters.IsChecked = true;
			}
			groundLevelAirPressure.Value = Convert.ToDouble(lastPrefs[pref_millibars]);
			switch (lastPrefs[pref_dateFormat])
			{
				case "1":
					date1.IsChecked = true;
					//dateTimePicker.FormatString = "dd/MM/yyyy";
					break;
				case "2":
					date2.IsChecked = true;
					//dateTimePicker.FormatString = "MM/dd/yyyy";
					break;
				case "3":
					date3.IsChecked = true;
					//dateTimePicker.FormatString = "yyyy/MM/dd";
					break;
				case "4":
					date4.IsChecked = true;
					//dateTimePicker.FormatString = "yyyy/dd/MM";
					break;
			}
			switch (lastPrefs[pref_timeFormat])
			{
				case "1":
					time1.IsChecked = true;
					break;
				case "2":
					time2.IsChecked = true;
					break;
			}
			fill.IsChecked = false;
			if (lastPrefs[pref_fillEmpty] == "True") fill.IsChecked = true;
			same.IsChecked = false;
			if (lastPrefs[pref_sameColumn] == "True") same.IsChecked = true;
			batteryCB.IsChecked = false;
			if (lastPrefs[pref_battery] == "True") batteryCB.IsChecked = true;
			txt.IsChecked = false;
			if (lastPrefs[pref_txt] == "True") txt.IsChecked = true;
			kml.IsChecked = false;
			if (lastPrefs[pref_kml] == "True") kml.IsChecked = true;

			if (System.IO.Path.GetExtension(fileName).Contains("6"))
			{
				txt.IsChecked = true;
				txt.IsEnabled = false;
			}

			byte p = Convert.ToByte(lastPrefs[pref_h]);
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
			mUd.Value = Convert.ToDouble(lastPrefs[pref_m]);
			sUd.Value = Convert.ToDouble(lastPrefs[pref_s]);
			DateTime dd = new DateTime(Convert.ToInt16(lastPrefs[pref_date_year]), Convert.ToInt16(lastPrefs[pref_date_month]), Convert.ToInt16(lastPrefs[pref_date_day]));
			dateTimePicker.SelectedDate = dd;

			OverrideTime.IsChecked = false;
			if (lastPrefs[pref_time_override] == "True") OverrideTime.IsChecked = true;

			metadataCB.IsChecked = false;
			if (lastPrefs[pref_metadata] == "True") metadataCB.IsChecked = true;

			leapSecondsUD.Value = int.Parse(lastPrefs[pref_leapSeconds]);

			removeNonGps.IsChecked = bool.Parse(lastPrefs[pref_nonGps]);

			proximityCB.IsChecked = bool.Parse(lastPrefs[pref_proximity]);

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
						string testo1 = "Generate additional Text file with positions";

						switch (debugLevel)
						{
							case 0:
								//if (System.IO.Path.GetExtension(fileName).Contains("6"))
								//{
								txt.Content = testo1 + " (D)";
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
								txt.Content = testo1;
								debugLevel = 0;
								//}
								//else
								//{
								//	txt.Content = testo1 + " (d2)";
								//	debugLevel = 2;
								//}
								break;
							case 2:
								txt.Content = testo1 + " (d3)";
								debugLevel = 3;
								break;
							case 3:
								txt.Content = testo1;
								debugLevel = 0;
								break;
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
			string[] lastPrefs = new string[20];
			if ((bool)Millibars.IsChecked)
			{
				lastPrefs[pref_pressMetri] = "millibars";
			}
			else
			{
				lastPrefs[pref_pressMetri] = "meters";
			}
			lastPrefs[pref_millibars] = groundLevelAirPressure.Value.ToString();
			if ((bool)date1.IsChecked)
			{
				lastPrefs[pref_dateFormat] = "1";
			}
			else if ((bool)date2.IsChecked)
			{
				lastPrefs[pref_dateFormat] = "2";
			}
			else if ((bool)date3.IsChecked)
			{
				lastPrefs[pref_dateFormat] = "3";
			}
			else if ((bool)date4.IsChecked)
			{
				lastPrefs[pref_dateFormat] = "4";
			}
			lastPrefs[pref_timeFormat] = "2";
			if ((bool)time1.IsChecked) lastPrefs[pref_timeFormat] = "1";
			lastPrefs[pref_fillEmpty] = fill.IsChecked.ToString();
			lastPrefs[pref_sameColumn] = same.IsChecked.ToString();
			lastPrefs[pref_battery] = batteryCB.IsChecked.ToString();
			lastPrefs[pref_txt] = txt.IsChecked.ToString();
			lastPrefs[pref_kml] = kml.IsChecked.ToString();
			overrideTime = (bool)OverrideTime.IsChecked;

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
			lastPrefs[pref_h] = p.ToString();
			lastPrefs[pref_m] = mUd.Value.ToString();
			lastPrefs[pref_s] = sUd.Value.ToString();
			lastPrefs[pref_date_year] = dateTimePicker.SelectedDate.Value.Year.ToString();
			lastPrefs[pref_date_month] = dateTimePicker.SelectedDate.Value.Month.ToString();
			lastPrefs[pref_date_day] = dateTimePicker.SelectedDate.Value.Day.ToString();
			lastPrefs[pref_time_override] = OverrideTime.IsChecked.ToString();
			lastPrefs[pref_metadata] = metadataCB.IsChecked.ToString();
			lastPrefs[pref_leapSeconds] = leapSecondsUD.Value.ToString();
			lastPrefs[pref_nonGps] = removeNonGps.IsChecked.ToString();
			lastPrefs[pref_proximity] = proximityCB.IsChecked.ToString();

			if (System.IO.File.Exists(prefFile)) System.IO.File.Delete(prefFile);
			System.IO.File.WriteAllLines(prefFile, lastPrefs);
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
			batteryCB.IsChecked = false;
			metadataCB.IsChecked = false;
		}

	}


}
