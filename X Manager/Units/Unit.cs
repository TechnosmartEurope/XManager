using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.IO.Ports;
using Microsoft.VisualBasic.FileIO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using System.Globalization;
using Windows.Security.Cryptography.Core;


#if X64
using FT_HANDLE = System.UInt64;
#else
using FT_HANDLE = System.UInt32;
#endif

namespace X_Manager.Units
{
	public abstract class Unit : IDisposable
	{
		//Tipi unità
		bool disposed = false;
		public const int model_axyDepth_legacy = 255;       //0xff
		public const int model_axy3_connecting = 253;       //0xfd
		public const int model_axy4_legacy = 252;           //0xfc
		public const int model_axyDepthFast = 128;          //0x80
		public const int model_axyDepth = 127;              //0x7f
		public const int model_axy2 = 126;                  //0x7e
		public const int model_axy3 = 125;                  //0x7d
		public const int model_axy4 = 124;                  //0x7c
		public const int model_Co2Logger = 123;             //0x7b
		public const int model_axy5 = 122;                  //0x7a
		public const int model_Gipsy6IR = 13;
		public const int model_Gipsy6XS = 12;
		public const int model_drop_off = 11;
		public const int model_AGM1_calib = 10;
		public const int model_Gipsy6N = 10;
		public const int model_AGM1 = 9;
		public const int model_axyTrekR = 7;
		public const int model_axyTrekN = 6;
		public const int model_axyTrekHD = 4;
		public const int model_axyTrekFT = 3;
		public const int model_axyTrekCO2Small = 2;
		public const int model_axyTrekCO2 = 8;

		//Maschere timestamp0
		public const byte ts_ext1 = 0b_0000_0001;
		public const byte ts_temperature = 0b_0000_0010;
		public const byte ts_pressure = 0b_0000_0100;
		public const byte ts_battery = 0b_0000_1000;
		public const byte ts_coordinate = 0b0001_0000;
		public const byte ts_event = 0b_0010_0000;
		public const byte ts_activity = 0b_0100_0000;
		public const byte ts_water = 0b_1000_0000;

		//Maschere timestamp1
		public const byte ts_ext2 = 0b_0000_0001;
		public const byte ts_adcValue = 0b0000_0010;
		public const byte ts_raw = 0b0000_0010;
		public const byte ts_adcThreshold = 0b0000_0100;
		public const byte ts_info = 0b0000_0100;
		public const byte ts_mag = 0b_0000_1000;
		public const byte ts_sched = 0b_0001_0000;
		public const byte ts_time = 0b0010_0000;
		public const byte ts_multi = 0b0100_0000;
		public const byte ts_proximity = 0b1000_0000;
		public const byte ts_escape = 0b1000_0000;

		//Maschere timestamp di fine blocco
		public const byte ts_be_powerOff = 0b_0000_1000;
		public const byte ts_be_battery = 0b0001_0000;
		public const byte ts_be_memFull = 0b0010_0000;
		public const byte ts_blockEnd = 0b0100_0000;


		//Preferenze conversione
		protected bool pref_angloTime = false;
		protected string pref_dateFormatParameter;
		protected byte pref_dateFormat;
		protected byte pref_timeFormat;
		protected bool pref_overrideTime = false;
		protected bool pref_inMeters = false;
		protected bool pref_repeatEmptyValues = true;
		protected bool pref_sameColumn = false;
		protected double pref_pressOffset;
		protected bool pref_addGpsTime;
		protected bool pref_isDepth = true;
		protected bool pref_metadata = false;
		protected int pref_leapSeconds;
		protected bool pref_removeNonGps = false;
		protected bool pref_proximity = false;
		protected bool pref_splitKml = false;
		protected int pref_splitKmlEvery = 1000;
		protected byte pref_debugLevel;

		protected string _modelName = "";
		//protected CultureInfo dateCi;
		public virtual string modelName
		{
			get { return _modelName; }
			set { _modelName = value; }
		}

		public bool positionCanSend = false;
		public bool configurePositionButtonEnabled = false;
		public bool configureMovementButtonEnabled = true;
		protected bool realTimeSPVisibility = false;
		public UInt32 mem_min_physical_address;
		public UInt32 mem_max_physical_address;
		public UInt32 mem_address;
		public UInt32 mem_max_logical_address;
		protected string name;
		public byte modelCode = 0;
		public UInt32 firmTotA;
		public UInt32 firmTotB;
		protected byte[] firmwareArray;
		public bool connected;
		public bool remote = false;
		public bool solar = false;
		public volatile bool convertStop;
		protected int nInputs;
		protected int nOutputs;
		protected double[] lastGroup = new double[1];

		protected Parent parent;
		protected FTDI_Device ft;

		protected string csvSeparator;
		//protected string dateSeparator;
		protected DateTime orDateTime;
		protected const string unitNotReady = "Unit not ready";

		protected long progVal = 0;
		protected int progLock = 0;
		protected double progMax = 1;

		public string defaultArdExtension = "ard";

		public static readonly double[,] batteryLevels = new double[,]
		{
			{100, 95, 90, 85, 80, 75, 70, 65, 60, 55, 50, 45, 40, 35, 30, 25, 20, 15, 10, 5, 0},
			{4.20, 4.15, 4.11, 4.08, 4.02, 3.98, 3.95, 3.91, 3.87, 3.85, 3.84, 3.82, 3.80, 3.79, 3.77, 3.75, 3.73, 3.71, 3.69, 3.61, 3.27}
		};

		public double _batteryLevel;
		public double batteryLevel
		{
			get
			{
				return _batteryLevel;
			}
			set
			{
				_batteryLevel = value;
				batteryPercentage = 100;
				int counter = 0;
				while ((counter < 20) && (batteryLevel < batteryLevels[1, counter]))
				{
					counter++;
				}
				if (_batteryLevel < 3.27)
				{
					batteryPercentage = 0;
					return;
				}
				if (counter > 0)
				{
					double coeff = (batteryLevels[0, counter] - batteryLevels[0, counter - 1]) / (batteryLevels[1, counter] - batteryLevels[1, counter - 1]);
					batteryPercentage = batteryLevels[0, counter] + coeff * (value - batteryLevels[1, counter]);
				}
			}
		}
		public double batteryPercentage;


#if X64
		[DllImport(@"resampleLib_x64.dll", CallingConvention = CallingConvention.Cdecl)]
#else
		[DllImport(@"resampleLib.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
		public static extern int resample3(byte[] inputArray, int nInputSamples, double[] outArray, int nOutputSamples);

#if X64
		[DllImport(@"resampleLib_x64.dll", CallingConvention = CallingConvention.Cdecl)]
#else
		[DllImport(@"resampleLib.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
		public static extern int resample4(byte[] inputArray, int nInputSamples, double[] outArray, int nOutputSamples);

#if X64
		[DllImport(@"resampleLib_x64.dll", CallingConvention = CallingConvention.Cdecl)]
#else
		[DllImport(@"resampleLib.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
		public static extern int resample5(byte[] inputArray, int nInputSamples, double[] outArray, int nOutputSamples);

		public Unit(object p)
		{
			parent = (Parent)p;
			ft = MainWindow.FTDI;
			connected = false;
			progressWorker.DoWork += prog_doWork;
			progressWorker.RunWorkerCompleted += prog_endWork;
		}

		public void loadConversionSettings()
		{
			csvSeparator = Properties.Settings.Default.CONV_CSV_SEPARATOR;

			if (Properties.Settings.Default.CONV_PRESSURE_UNIT == "meters") pref_inMeters = true;
			if (Properties.Settings.Default.CONV_PRESSURE_RANGE == "air") pref_isDepth = false;

			pref_repeatEmptyValues = Properties.Settings.Default.CONV_FILL_EMPTY;

			pref_timeFormat = Properties.Settings.Default.CONV_TIME_FORMAT;
			pref_dateFormat = Properties.Settings.Default.CONV_DATE_FORMAT;
			if (pref_timeFormat == 2) pref_angloTime = true;
			switch (pref_dateFormat)
			{
				case 1:
					pref_dateFormatParameter = "dd/MM/yyyy";
					break;
				case 2:
					pref_dateFormatParameter = "MM/dd/yyyy";
					break;
				case 3:
					pref_dateFormatParameter = "yyyy/MM/dd";
					break;
				case 4:
					pref_dateFormatParameter = "yyyy/dd/MM";
					break;
			}
			pref_sameColumn = Properties.Settings.Default.CONV_SAME_COLUMN;
			if (Properties.Settings.Default.CONV_SAME_COLUMN)
			{
				pref_sameColumn = true;
				pref_dateFormatParameter += " ";
			}
			else
			{
				pref_dateFormatParameter += csvSeparator;
			}

			string centesimiDiSecondo = ".fff";
			//Rimuove dal formata data e ora i centesimi di secondo perché per ora non sono utilizzati dal gipsy6. Togliere in caso di accelerometro
			if (this is Gipsy6.Gipsy6) centesimiDiSecondo = "";

			if (Properties.Settings.Default.CONV_TIME_FORMAT == 2)
			{
				pref_dateFormatParameter += "hh:mm:ss" + centesimiDiSecondo + " tt";
			}
			else
			{
				pref_dateFormatParameter += "HH:mm:ss" + centesimiDiSecondo;
			}

			pref_pressOffset = Properties.Settings.Default.CONV_PRESSURE_OFFSET;
			pref_overrideTime = Properties.Settings.Default.CONV_TIME_OVERRIDE;
			pref_metadata = Properties.Settings.Default.CONV_METADATA;

			pref_leapSeconds = Properties.Settings.Default.CONV_LEAP_SECONDS;
			pref_removeNonGps = Properties.Settings.Default.CONV_NON_GPS;
			pref_proximity = Properties.Settings.Default.CONV_PROXIMITY;
			if (Properties.Settings.Default.CONV_SAME_COLUMN)
			{
				pref_sameColumn = true;
			}
			orDateTime = Properties.Settings.Default.CONV_TIME;

			pref_splitKml = Properties.Settings.Default.CONV_SPLIT_KML;
			pref_splitKmlEvery = Properties.Settings.Default.CONV_SPLIT_KML_EVERY;
		}

		public void loadTempConversionSettings(string[] prefs, DateTime dt)
		{

			const int c_pref_pressMetri = 0;
			const int c_pref_millibars = 1;
			const int c_pref_dateFormat = 2;
			const int c_pref_timeFormat = 3;
			const int c_pref_fillEmpty = 4;
			const int c_pref_sameColumn = 5;
			//const int c_pref_dateTime = 7;
			const int c_pref_overrideTime = 8;
			const int c_pref_metadata = 9;
			const int c_pref_leapSeconds = 10;
			const int c_pref_removeNonGps = 11;
			const int c_pref_proximity = 12;

			if (prefs[c_pref_pressMetri] == "meters") pref_inMeters = true;
			if (prefs[c_pref_pressMetri] == "air") pref_isDepth = false;

			pref_repeatEmptyValues = false;
			if (prefs[c_pref_fillEmpty].Equals("True")) pref_repeatEmptyValues = true;


			pref_timeFormat = byte.Parse(prefs[c_pref_timeFormat]);
			pref_dateFormat = byte.Parse(prefs[c_pref_dateFormat]);
			if (pref_timeFormat == 2) pref_angloTime = true;
			switch (pref_dateFormat)
			{
				case 1:
					pref_dateFormatParameter = "dd/MM/yyyy";
					break;
				case 2:
					pref_dateFormatParameter = "MM/dd/yyyy";
					break;
				case 3:
					pref_dateFormatParameter = "yyyy/MM/dd";
					break;
				case 4:
					pref_dateFormatParameter = "yyyy/dd/MM";
					break;
			}
			pref_sameColumn = false;
			if (prefs[c_pref_sameColumn].Equals("True"))
			{
				pref_sameColumn = true;
			}

			if (pref_sameColumn)
			{
				pref_sameColumn = true;
				pref_dateFormatParameter += " ";
			}
			else
			{
				pref_dateFormatParameter += csvSeparator;
			}

			if (int.Parse(prefs[c_pref_timeFormat]) == 2)
			{
				pref_dateFormatParameter += "hh:mm:ss.fff tt";
			}
			else
			{
				pref_dateFormatParameter += "HH:mm:ss.fff";
			}

			pref_pressOffset = int.Parse(prefs[c_pref_millibars]);
			pref_overrideTime = false;
			if (prefs[c_pref_overrideTime].Equals("True")) pref_overrideTime = true;
			pref_metadata = false;
			if (prefs[c_pref_metadata].Equals("True")) pref_metadata = true;

			pref_leapSeconds = int.Parse(prefs[c_pref_leapSeconds]);
			pref_metadata = false;
			if (prefs[c_pref_metadata].Equals("True")) pref_metadata = true;
			pref_removeNonGps = false;
			if (prefs[c_pref_removeNonGps].Equals("True")) pref_removeNonGps = true;
			pref_proximity = false;
			if (prefs[c_pref_proximity].Equals("True")) pref_proximity = true;
			orDateTime = dt;

		}

		public virtual void changeBaudrate(int newBaudrate)
		{ }

		public static string askModel()
		{
			FTDI_Device ft = MainWindow.FTDI;

			ft.Write("TTTTTTTGGAf");
			string unitModelString;
			try
			{
				unitModelString = ft.ReadLine();
			}
			catch
			{
				throw new Exception("unit not ready");
			}
			//byte model=0;
			switch (unitModelString)
			{
				//case "Axy-4":
				//case "Axy-4.5":
				//case "Axy-Depth":
				//case "Axy-Depth.5":
				//break;
				case "Axy-Trek":
					break;
				case "Axy-Track":
					unitModelString = "Axy-Trek";
					break;
				case "Axy-Trek_D":
					unitModelString = "Axy-Trek";
					break;
				case "Axy-Trek_A":
					unitModelString = "Axy-Trek";
					break;
				case "AGM-1":
				case "AMG-1":
					unitModelString = "AGM-1";
					break;
				default:
					//mode 0;
					break;
			}
			return unitModelString;
		}

		public static string askLegacyModel()
		{
			FTDI_Device ft = MainWindow.FTDI;
			string model = ft.ReadLine();
			model = model.Split('.')[1];
			switch (Int16.Parse(model))
			{
				case model_axy4_legacy:
					model = "Axy-4";
					break;
				case model_axy3_connecting:
					model = "Axy-3";
					break;
				case model_axyDepth_legacy:
					model = "Axy-Depth";
					break;
			}
			return model;
		}

		public virtual void keepAlive()
		{

		}

		public virtual void msBaudrate()
		{

		}

		public abstract string askFirmware();

		public abstract void askBattery();

		public abstract string askName();

		protected string formatName(string nameIn)
		{
			int pointer = nameIn.Length;
			do
			{
				pointer--;
			} while ((nameIn[pointer] == 0x20) & pointer > 0);
			return nameIn.Substring(0, pointer + 1);

		}

		public abstract UInt32[] askMaxMemory();

		public abstract UInt32[] askMemory();

		public virtual int askRealTime()
		{
			return 0;
		}

		public virtual void getCoeffs()
		{

		}

		public abstract byte[] getConf();

		public abstract void setConf(byte[] conf);

		public abstract void abortConf();

		public virtual bool getRemote()
		{
			return false;
		}

		public virtual void setPcTime()
		{

		}

		public virtual byte[] getGpsSchedule()
		{
			return new byte[] { 0 };
		}

		public virtual void setGpsSchedule(byte[] schedule)
		{

		}

		public virtual byte[] getAccSchedule()
		{
			return new byte[] { 0 };
		}

		public virtual void setAccSchedule(byte[] schedule)
		{
		}

		public virtual void download(string fileName, UInt32 fromMemory, UInt32 toMemory, int baudrate)
		{
		}

		public virtual void downloadRemote(string fileName, UInt32 fromMemory, UInt32 toMemory, int baudrate)
		{
		}

		public abstract void extractArds(string fileNameMdp, string fileName, bool fromDownload);

		public virtual void convert(string fileName)
		{

		}

		public abstract void eraseMemory();

		public virtual void setName(string newName)
		{

		}

		public virtual void disconnect()
		{
			connected = false;
		}

		public virtual void powerOff()
		{

		}

		public virtual void shutDown()
		{
			connected = false;
		}

		void setStateDisconnected()
		{
			connected = false;
		}

		public virtual bool isSolar()
		{
			return true;
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposed)
			{
				if (disposing)
				{
					disposed = true;
				}
			}
		}

		public virtual void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		//public void progressUpdate(int mode, int value)
		//{
		//	if (mode == 0)
		//	{
		//		parent.statusProgressBar.Maximum = value;
		//		parent.progressMax = value;
		//	}
		//	else
		//	{
		//		parent.statusProgressBar.Value = value;
		//		parent.progress = value;
		//	}
		//}

		internal void fDel(string fileName)
		{
			try
			{
				FileSystem.DeleteFile(fileName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
			}
			catch { }
		}

		internal readonly BackgroundWorker progressWorker = new BackgroundWorker();

		private void prog_doWork(object sender, DoWorkEventArgs e)
		{
			long intProgVal;
			double intProgMax = progMax;
			string endS = "\\";
			DateTime dtStart = DateTime.Now;
			//while (Interlocked.Exchange(ref progLock, 1) > 0)
			//{
			//	Thread.Sleep(10);
			//}
			//intProgVal = progVal;
			//Interlocked.Exchange(ref progLock, 0);

			intProgVal = 1;
			while (intProgVal >= 0)
			{

				while (Interlocked.Exchange(ref progLock, 1) > 0)
				{
					Thread.Sleep(10);
				}
				intProgVal = progVal;
				Interlocked.Exchange(ref progLock, 0);
				if (intProgVal < 0)
				{
					break;
				}
				//Aggiorna la progress bar
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(
									() => parent.statusProgressBar.Value = intProgVal));

				//Aggiorna l'ETA
				TimeSpan secAm = DateTime.Now - dtStart;
				double tot = secAm.TotalMilliseconds;   //Millisecondi trascorsi dall'inizio della conversione
				tot /= intProgVal;  //Millisecondi ipiegati per byte
				tot *= (intProgMax - intProgVal);   //Millisecondi rimanenti
				string eta = "ETA:";
				//Ore
				int printVal = (int)Math.Floor(tot / 3600000);
				if (printVal > 0) eta += " " + printVal.ToString() + "h";
				tot -= (printVal * 3600000);
				//Minuti
				printVal = (int)Math.Floor(tot / 60000);
				if (printVal > 0) eta += " " + printVal.ToString() + "m";
				tot -= (printVal * 60000);
				//Secondi
				printVal = (int)Math.Floor(tot / 1000);
				if (printVal > 0) eta += " " + printVal.ToString() + "s";
				tot -= printVal * 1000;
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(
					() => parent.etaLabel.Content = eta));

				//Aggiorna la console
				double textProg = intProgVal * 20 / intProgMax;
				textProg = Math.Ceiling(textProg);
				string testo = "[";
				for (int i = 0; i < textProg; i++)
				{
					testo += "X";
				}
				for (int i = (int)textProg; i < 20; i++)
				{
					testo += ".";
				}
				testo += "] ";
				if (endS.Equals("/"))
				{
					endS = "\\";
				}
				else
				{
					endS = "/";
				}
				Console.Write("\r" + testo + endS + " " + eta + "             ");
				try
				{
					if (Console.KeyAvailable)
					{
						terminate(true);
					}
				}
				catch { }
				//Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(
				//	() => parent.progress = intProgVal));
				Thread.Sleep(100);
			}
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(
					() => parent.etaLabel.Content = ""));
			//Console.WriteLine("\r\nConversion complete.");

		}

		private void terminate(bool read)
		{
			ConsoleKeyInfo t;
			if (read)
			{
				t = Console.ReadKey();
			}
			{
				Console.Write("\rConversion in progress. Do you want to stop it? (y/n)");
				if (Console.ReadKey().Key == ConsoleKey.Y)
				{
					Console.WriteLine(": Operation Aborted");
					Environment.Exit(-1);
				}
				Console.Write("\r                                                            ");
			}
		}

		//private void ckp(object sender, ConsoleCancelEventArgs e)
		//{
		//	Console.WriteLine("\r\nConversion in progress CCC. Do you want to stop it? (y/n)");
		//	ConsoleKeyInfo k = Console.ReadKey();
		//	if (k.Key == ConsoleKey.Y)
		//	{
		//		Console.WriteLine("Operation Aborted");
		//	//	Environment.Exit(-1);
		//	}
		//	e.Cancel = true;

		//}

		private void prog_endWork(object sender, RunWorkerCompletedEventArgs e)
		{

		}
	}


}
