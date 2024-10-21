using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using Windows.Storage.Streams;
using Windows.System.Profile;

namespace X_Manager.Units.Gipsy6
{
	public abstract class Gipsy6 : Unit
	{
		protected struct TimeStamp
		{
			private int _pos;

			public int tsType;
			public int tsTypeExt1;
			public int tsTypeExt2;
			public int ore;
			public double batteryLevel;
			public double temperature;
			public double press;
			public double pressOffset;
			public double altitude;
			public double lat;
			public double lon;
			public double speed;
			public int hAcc;
			public int vAcc;
			public int cog;
			public int sat;
			public int gsvSum;
			public int timeStampLength;
			public DateTime dateTime;
			public byte[] infoAr;
			public byte[] eventAr;
			public byte[] raw;
			public bool isEvent;
			public int stopEvent;
			public int inWater;
			public int inAdc;
			public int ADC;
			public int GPS_second;
			public int proximityAddress;
			public sbyte proximityPower;
			public string unitNameTxt;
			public string rfAddressString;
			public int pos
			{
				get => _pos;
				set
				{
					_pos = value + ((value / 0x1fe) + 1) * 2;
				}
			}
			public int txtAllowed;
			public bool rawPreset;
			public void resetPos(int initVal)
			{
				_pos = initVal;
			}

			public byte[] cloneRaw()
			{
				var rawOut = new byte[raw.Length];
				Array.Copy(raw, rawOut, raw.Length);
				return rawOut;
			}
			public TimeStamp clone()
			{
				var tout = new TimeStamp();
				tout.tsType = this.tsType;
				tout.tsTypeExt1 = this.tsTypeExt1;
				tout.tsTypeExt2 = this.tsTypeExt2;
				tout.ore = this.ore;
				tout.batteryLevel = this.batteryLevel;
				tout.temperature = this.temperature;
				tout.press = this.press;
				tout.pressOffset = pressOffset;
				tout.altitude = altitude;
				//tout.altSegno = this.altSegno;
				//tout.eo = this.eo;
				//tout.ns = this.ns;
				tout.lat = lat;
				tout.lon = lon;
				tout.speed = speed;
				tout.hAcc = hAcc;
				tout.vAcc = vAcc;
				tout.cog = cog;
				tout.sat = this.sat;
				tout.gsvSum = this.gsvSum;
				tout.timeStampLength = this.timeStampLength;
				tout.dateTime = dateTime;
				if (this.infoAr != null)
				{
					tout.infoAr = new byte[this.infoAr.Length];
					Array.Copy(this.infoAr, tout.infoAr, infoAr.Length);
				}
				if (this.eventAr != null)
				{
					tout.eventAr = new byte[this.eventAr.Length];
					Array.Copy(this.eventAr, tout.eventAr, eventAr.Length);
				}
				tout.isEvent = this.isEvent;
				tout.stopEvent = this.stopEvent;
				tout.inWater = this.inWater;
				tout.inAdc = this.inAdc;
				tout.ADC = ADC;
				tout.GPS_second = GPS_second;
				tout.proximityAddress = proximityAddress;
				tout.proximityPower = proximityPower;
				tout.rfAddressString = rfAddressString;
				tout.unitNameTxt = unitNameTxt;
				tout.rawPreset = rawPreset;
				tout.resetPos(this.pos);

				return tout;
			}
		}

		public static int contoFix = 0;

		protected const int RETRY_MAX = 4;

		protected string unitName = "";
		protected string lastKnownUnitName = "";

		protected int p_fileCsv_name = 0;
		protected int p_fileCsv_rfAddress = 1;
		protected int p_fileCsv_date = 2;
		//protected int p_fileCsv_time = 3;
		protected int p_fileCsv_latitude = 3;
		protected int p_fileCsv_longitude = 4;
		protected int p_fileCsv_horizontalAccuracy = 5;
		protected int p_fileCsv_altitude = 6;
		protected int p_fileCsv_verticalAccuracy = 7;
		protected int p_fileCsv_speed = 8;
		protected int p_fileCsv_course = 9;
		protected int p_fileCsv_battery = 10;
		protected int p_fileCsv_proximity = 11;
		protected int p_fileCsv_proximityPower = 12;
		protected int p_fileCsv_event = 13;
		protected int p_fileCsv_position = 14;
		protected int p_fileCsv_length = 15;

		public bool kmlClose;

		public bool remoteConnection = false;

		protected NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;

		protected BackgroundWorker txtBGW;
		protected BackgroundWorker kmlBGW;
		protected BackgroundWorker rawBGW;
		protected static Semaphore txtSem;
		protected static Semaphore kmlSem;
		protected static Semaphore rawSem;
		protected static Semaphore txtSemBack;
		protected static Semaphore kmlSemBack;
		protected static Semaphore rawSemBack;
		protected Gipsy6(object p)
					: base(p)
		{
		}

		protected virtual bool ask(string command)
		{
			ft.Open();
			ft.ReadExisting();
			int test = 0;
			bool goon = false;
			if (MainWindow.keepAliveTimer != null)
			{
				MainWindow.keepAliveTimer.Stop();
				MainWindow.keepAliveTimer.Start();
			}
			if (remoteConnection)
			{
				ft.ReadTimeout = 2200;
				ft.Write("TTTTTTTGGA" + command);
				return true;
			}
			ft.ReadTimeout = 30;
			for (int y = 0; y < 4; y++) //(rimettere y < 4 dopo sviluppo
			{
				goon = false;
				ft.Write("TTTTTTTGGA" + command);

				try
				{
					test = ft.ReadByte();   //Byte di risposta per verifica correttezza comando
				}
				catch
				{
					Thread.Sleep(500);
					ft.Write(new byte[] { 0 }, 1);
					continue;               //Dopo il timeout di 3 ms non è arrivata la risposta: il comando viene reinviato
				}

				if (test == command.ToArray()[0])   //Il comando è arrivato giusto, si manda conferma e si continua
				{
					ft.Write("K");
					goon = true;
					break;
				}
				else
				{
					//Il comando è arrivato sbagliato, si aspetta il timeout del micro e si reinvia il comando
					Thread.Sleep(2);
					continue;
				}
			}

			return goon;
		}

		public override void keepAlive()
		{
			uint oldTimeout = ft.ReadTimeout;
			if (remoteConnection)
			{
				ft.ReadTimeout = 100;
			}
			else
			{
				ft.ReadTimeout = 400;
			}
			ft.Write("TTTTTTTGGAP");
			try
			{
				ft.ReadByte();
				Thread.Sleep(10);
				ft.ReadExisting();
			}
			catch
			{
				if (!remoteConnection)
				{
					throw new Exception(unitNotReady);
				}
			}
			ft.ReadTimeout = oldTimeout;
		}

		public override void msBaudrate()
		{
			ft.BaudRate = 2000000;
		}

		public override void changeBaudrate(int maxMin)
		{
			if (!remoteConnection)
			{
				uint oldBaudRate = ft.BaudRate;
				uint newBaudRate = 0;
				uint b;
				try
				{

					if (!ask("b"))
					{
						return;
					}
					ft.Write(new byte[] { (byte)maxMin }, 1);
					ft.ReadTimeout = 1200;
					newBaudRate = (uint)ft.ReadByte();
					newBaudRate = newBaudRate + ((uint)ft.ReadByte() << 8);
					newBaudRate = newBaudRate + ((uint)ft.ReadByte() << 16);
					newBaudRate = newBaudRate + ((uint)ft.ReadByte() << 24);
					b = ft.ReadByte();
					ft.BaudRate = newBaudRate;
					ft.Write(new byte[] { 0x55 }, 1);
					Thread.Sleep(5);
					b = ft.ReadByte();
					Thread.Sleep(5);
				}
				catch
				{
					ft.BaudRate = oldBaudRate;
				}
			}
		}

		public override string askFirmware()
		{
			byte[] f = new byte[3];
			string firmware = "";
			int retryMax = 1;
			if (remoteConnection) retryMax = RETRY_MAX;
			for (int retry = 0; retry < retryMax; retry++)
			{

				if (!ask("F"))
				{
					throw new Exception(unitNotReady);
				}
				if (!remoteConnection) ft.ReadTimeout = 400;
				int i = 0;
				try
				{
					for (i = 0; i < 3; i++)
					{
						f[i] = ft.ReadByte();
					}
					break;
				}
				catch
				{
					if (retry == retryMax - 1)
					{
						throw new Exception(unitNotReady);
					}
				}
			}
			firmTotA = 0;
			for (int i = 0; i < 3; i++)
			{
				firmTotA *= 1000;
				firmTotA += f[i];
			}

			for (int i = 0; i <= (f.Length - 2); i++)
			{
				firmware += f[i].ToString() + ".";
			}

			firmware += f[f.Length - 1].ToString();
			firmwareArray = f;
			return firmware;
		}

		public override void powerOff()
		{
			if (remote)
			{
				ask("o");
				try
				{
					if (remoteConnection)
					{
						ft.ReadTimeout = 2200;
						ft.ReadByte();
					}
				}
				catch { }
			}
			else
			{
				ask("o");
			}

			connected = false;
		}

		public override void convert(string fileName)
		{
			base.convert("");

			pref_debugLevel = parent.stDebugLevel;
			pref_addGpsTime = parent.addGpsTime;

			if (pref_addGpsTime)
			{
				pref_repeatEmptyValues = false;
				pref_sameColumn = true;
			}

		}

		protected void kmlBGW_doWork(ref List<TimeStamp> tL, string kmlName)
		{
			//								data   ora    lon   lat   alt   eve   batt

			//BinaryWriter placeMark;
			//placeMark = new BinaryWriter(new FileStream(kmlName + ".kml", FileMode.Create));
			//BinaryWriter kml;
			//kml = new BinaryWriter(new FileStream(kmlName + "_temp.kml", FileMode.Create));
			bool placeExisting = File.Exists(kmlName + ".kml");
			int contoFile = 1;
			string newKmlName = kmlName;
			string baseKmlName = kmlName;
			if (placeExisting && pref_splitKml)
			{
				while (File.Exists(kmlName + "_" + contoFile.ToString() + ".kml"))
				{
					newKmlName = kmlName + "_" + contoFile.ToString();
					contoFile++;
				}
			}
			kmlName = newKmlName;			

			StreamWriter placeMark;
			placeMark = new StreamWriter(kmlName + ".kml", true);
			StreamWriter kml;
			kml = new StreamWriter(kmlName + "_temp.kml", true);

			//string kmlS = Properties.Resources.Folder_Path_Top + Properties.Resources.Path_Top;
			//string placeS = Properties.Resources.Final_Top_1 + Path.GetFileNameWithoutExtension(kmlName) + Properties.Resources.Final_Top_2;

			if (!placeExisting)
			{
				kml.Write(Properties.Resources.Folder_Path_Top + Properties.Resources.Path_Top);
				placeMark.Write(Properties.Resources.Final_Top_1 + Path.GetFileNameWithoutExtension(kmlName) + Properties.Resources.Final_Top_2);
				contoFix = 0;
			}

			int pbmax = 0;
			bool primaCoordinata = true;
			string lonS = "", latS = "", altS = "";

			var t = new TimeStamp();
			while (true)
			{
				kmlSem.WaitOne();   //Se il thread principale sta ancora aggiungendo timestamp alla pila
									//aspetta che il thread principale abbia aggiunto un nuovo timestamp alla lista

				if (tL.Count == 0)  //Se non ci sono più timestamp nella pila, si esce dal loop
				{
					break;
				}
				t = tL[0];
				tL.RemoveAt(0);

				if (t.sat > 0) //Si scrive il timestmap nel kml
				{
					contoFix++;
					if (pref_splitKml && (contoFix == pref_splitKmlEvery))    //Se abilitato lo split, raggiunto il n. prefissato di fix chiude il file e ne apre uno nuovo
					{
						kml.Close();
						placeMark.Close();

						//Scrive l'header finale nel file kml string
						File.AppendAllText(kmlName + "_temp.kml", Properties.Resources.Path_Bot);
						File.AppendAllText(kmlName + "_temp.kml", Properties.Resources.Folder_Bot);

						//Accorpa kml placemark e string
						File.AppendAllText(kmlName + ".kml", File.ReadAllText(kmlName + "_temp.kml"));

						//Chiude il kml placemark
						File.AppendAllText(kmlName + ".kml", Properties.Resources.Final_Bot);

						//Elimina il kml string temporaneo
						fDel(kmlName + "_temp.kml");

						contoFix = 0;
						kmlName = baseKmlName + "_" + contoFile.ToString();
						contoFile++;

						placeMark = new StreamWriter(kmlName + ".kml", true);
						kml = new StreamWriter(kmlName + "_temp.kml", true);
						kml.Write(Properties.Resources.Folder_Path_Top + Properties.Resources.Path_Top);
						placeMark.Write(Properties.Resources.Final_Top_1 + Path.GetFileNameWithoutExtension(kmlName) + Properties.Resources.Final_Top_2);

					}

				}

				kml.Write("\t\t\t\t\t");
				lonS = t.lon.ToString("00.0000000", nfi) + ",";
				kml.Write(lonS);
				latS = t.lat.ToString("00.0000000", nfi) + ",";
				kml.Write(latS);
				altS = t.altitude.ToString("0000.0", nfi);
				kml.Write(altS + "\r\n");

				if (primaCoordinata && !placeExisting)
				{
					primaCoordinata = false;
					//Segnaposto di start
					placeMark.Write(Properties.Resources.lookat1);
					placeMark.Write(t.lon.ToString("00.0000000", nfi));
					placeMark.Write(Properties.Resources.lookat2);
					placeMark.Write(t.lat.ToString("00.0000000", nfi));
					placeMark.Write(Properties.Resources.lookat3);
					placeMark.Write(t.altitude.ToString("0000.0", nfi));
					placeMark.Write(Properties.Resources.lookat4);
					//Coordinata placemark
					placeMark.Write(Properties.Resources.Placemarks_Start_Top + "\r\n\t\t\t\t<coordinates>");
					placeMark.Write(lonS);
					placeMark.Write(latS);
					placeMark.Write(altS);
					placeMark.Write("</coordinates>\r\n");
					placeMark.Write(Properties.Resources.Placemarks_Start_Bot + Properties.Resources.Folder_Generics_Top);
				}

				char cl = (char)(49 + (t.speed / 10));
				if (cl > 55)
				{
					cl = '9';
				}

				placeMark.Write(Properties.Resources.Placemarks_Generic_Top_1);
				placeMark.Write(t.dateTime.ToString("dd/MM/yyyy HH:mm:ss"));
				placeMark.Write(Properties.Resources.Placemarks_Generic_Top_2 + cl.ToString());
				placeMark.Write(Properties.Resources.Placemarks_Generic_Top_3);
				placeMark.Write(t.altitude.ToString());
				placeMark.Write(Properties.Resources.Placemarks_Generic_Top_4);
				placeMark.Write(t.speed.ToString());
				placeMark.Write(Properties.Resources.Placemarks_Generic_Top_5);
				placeMark.Write("\r\n\t\t\t\t\t<coordinates>");
				placeMark.Write(lonS);
				placeMark.Write(latS);
				placeMark.Write(altS);
				placeMark.Write("</coordinates>\r\n");
				placeMark.Write(Properties.Resources.Placemarks_Generic_Bot);

				contoFix++;

				kmlSemBack.Release();

			}

			System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.kmlProgressBar.Value = pbmax));
			//kml.Write(kmlS);
			//placeMark.Write(placeS);
			//Scrive il segnaposto di stop nel fime kml dei placemarks
			//placeMark.Write(System.Text.Encoding.ASCII.GetBytes(X_Manager.Properties.Resources.Folder_Bot));
			//placeMark.Write(System.Text.Encoding.ASCII.GetBytes(X_Manager.Properties.Resources.Placemarks_Stop_Top +
			//	"\r\n\t\t\t\t\t+<coordinates>" + temp + "</coordinates>\r\n" + X_Manager.Properties.Resources.Placemarks_Stop_Bot));
			if ((pref_debugLevel > 0) || kmlClose)
			{
				placeMark.Write(Properties.Resources.Folder_Bot);
				placeMark.Write(Properties.Resources.Placemarks_Stop_Top +
					"\r\n\t\t\t\t\t+<coordinates>" + lonS + latS + altS + "</coordinates>\r\n" + Properties.Resources.Placemarks_Stop_Bot);
			}
			kml.Close();
			placeMark.Close();

			if ((pref_debugLevel > 0) || kmlClose)
			{
				//Scrive l'header finale nel file kml string
				File.AppendAllText(kmlName + "_temp.kml", Properties.Resources.Path_Bot);
				File.AppendAllText(kmlName + "_temp.kml", Properties.Resources.Folder_Bot);

				//Accorpa kml placemark e string
				File.AppendAllText(kmlName + ".kml", File.ReadAllText(kmlName + "_temp.kml"));

				//Chiude il kml placemark
				File.AppendAllText(kmlName + ".kml", Properties.Resources.Final_Bot);

				//Elimina il kml string temporaneo
				fDel(kmlName + "_temp.kml");
			}

			kmlSemBack.Release();
			//Interlocked.Increment(ref conversionDone);
		}

		protected void placeHeader(StreamWriter txtBW, bool writeHeader)//, ref byte[] columnPlace)
		{

			var headers = new List<string>() { "Name", "RF address", "Date\tTime", "Latitude", "Longitude", "Hor. Acc.", "Altitude", "Vert. Acc.", "Speed", "Course", "Battery", "Nearby device",
										"Nearby device signal strenght", "Event", "gp6Pos" };

			if (pref_debugLevel == 0) headers.Remove("gp6Pos");
			if (!pref_metadata) headers.Remove("Event");
			if (!pref_proximity)
			{
				headers.Remove("Nearby device");
				headers.Remove("Nearby device signal strenght");
			}
			if (pref_sameColumn)
			{
				headers[headers.IndexOf("Date\tTime")] = "Timestamp";
			}

			if (this is Gipsy6XS) headers.Remove("RF address");

			p_fileCsv_date = Math.Max(headers.IndexOf("Date\tTime"), headers.IndexOf("Timestamp"));
			p_fileCsv_latitude = headers.IndexOf("Latitude");
			p_fileCsv_longitude = headers.IndexOf("Longitude");
			p_fileCsv_horizontalAccuracy = headers.IndexOf("Hor. Acc.");
			p_fileCsv_altitude = headers.IndexOf("Altitude");
			p_fileCsv_verticalAccuracy = headers.IndexOf("Vert. Acc.");
			p_fileCsv_speed = headers.IndexOf("Speed");
			p_fileCsv_course = headers.IndexOf("Course");
			p_fileCsv_battery = headers.IndexOf("Battery");
			p_fileCsv_proximity = headers.IndexOf("Nearby device");
			p_fileCsv_proximityPower = headers.IndexOf("Nearby device signal strenght");
			p_fileCsv_event = headers.IndexOf("Event");
			p_fileCsv_position = headers.IndexOf("gp6Pos");
			p_fileCsv_length = headers.Count;

			//byte place = 0;
			//for (COLUMN i = 0; i < COLUMN.COL_LENGTH; i++)
			//{
			//	columnPlace[(int)i] = place;
			//	switch (i)
			//	{
			//		case COLUMN.COL_DATE: if (!sameColumn) place++; break;
			//		case COLUMN.COL_BATTERY: if (prefBattery) place++; break;
			//		case COLUMN.COL_EVENT: if (metadata) place++; break;
			//		case COLUMN.COL_POSITION_IN_FILE: if (debugLevel >= 3) place++; break;
			//		default: place++; break;
			//	}
			//}
			//columnPlace[(int)COLUMN.COL_LENGTH] = place++;

			//var heads = new List<string>();
			//foreach (string s in headers)
			//{
			//	heads.Add(s);
			//}

			//if (debugLevel < 3) heads.RemoveAt(15);
			//if (!metadata) heads.RemoveAt(14);
			//if (!prefBattery) heads.RemoveAt(11);
			//if (sameColumn)
			//{
			//	heads.RemoveAt(3);
			//	heads[2] = "Timestamp";
			//}
			if (writeHeader)
			{
				for (int i = 0; i < headers.Count - 1; i++)
				{
					txtBW.Write(headers[i] + "\t");
				}
				txtBW.Write(headers[headers.Count - 1] + "\r\n");
			}
		}
	}
}
