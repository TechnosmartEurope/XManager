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
#if X64
using FT_HANDLE = System.UInt64;
#else
using FT_HANDLE = System.UInt32;
#endif

namespace X_Manager.Units
{
	abstract public class Unit : IDisposable
	{
		//Tipi unità
		bool disposed = false;
		public const int model_axyDepth_legacy = 255;
		public const int model_axy3_connecting = 253;
		public const int model_axy4_legacy = 252;
		public const int model_axyDepth = 127;
		public const int model_axy2 = 126;
		public const int model_axy3 = 125;
		public const int model_axy4 = 124;
		public const int model_Co2Logger = 123;
		public const int model_axy5 = 122;
		public const int model_drop_off = 11;
		public const int model_AGM1_calib = 10;
		public const int model_Gipsy6 = 10;
		public const int model_AGM1 = 9;
		public const int model_axyTrekS = 7;
		public const int model_axyTrek = 6;
		public const int model_axyQuattrok = 4;

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
		public const byte ts_adcThreshold = 0b0000_0100;
		public const byte ts_info = 0b0000_0100;
		public const byte ts_mag = 0b_0000_1000;
		public const byte ts_sched = 0b_0001_0000;
		public const byte ts_time = 0b0010_0000;
		public const byte ts_multi = 0b0100_0000;
		public const byte ts_escape = 0b1000_0000;

		//Maschere timestamp di fine blocco
		public const byte ts_be_powerOff = 0b_0000_1000;
		public const byte ts_be_battery = 0b0001_0000;
		public const byte ts_be_memFull = 0b0010_0000;
		public const byte ts_blockEnd = 0b0100_0000;


		//Array preferenze
		public const int pref_pressMetri = 0;
		public const int pref_millibars = 1;
		public const int pref_dateFormat = 2;
		public const int pref_timeFormat = 3;
		public const int pref_fillEmpty = 4;
		public const int pref_sameColumn = 5;
		public const int pref_battery = 6;
		public const int pref_txt = 7;
		public const int pref_kml = 8;
		public const int pref_override_time = 15;
		public const int pref_metadata = 16;
		public const int pref_leapSeconds = 17;
		public const int pref_removeNonGps = 18;


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
		protected string modelName;
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
		protected byte debugLevel;

		protected Parent parent;
		protected SerialPort sp;

		protected string csvSeparator;
		protected string dateSeparator;

		protected const string unitNotReady = "Unit not ready";

		protected long progVal = 0;
		protected int progLock = 0;
		protected double progMax = 1;

		public string defaultArdExtension = "ard";

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
			sp = parent.sp;
			connected = false;
			csvSeparator = parent.csvSeparator;
			progressWorker.DoWork += prog_doWork;
			progressWorker.RunWorkerCompleted += prog_endWork;
		}

		public virtual void changeBaudrate(ref SerialPort sp, int newBaudrate)
		{ }
		public static string askModel(ref SerialPort sp)
		{
			sp.Write("TTTTTTTGGAf");
			string unitModelString;
			try
			{
				unitModelString = sp.ReadLine();
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
				case "Co2Logger":
					unitModelString = "CO2 Logger";
					break;
				default:
					//mode 0;
					break;
			}
			return unitModelString;
		}

		public static string askLegacyModel(ref SerialPort sp)
		{
			string model = sp.ReadLine();
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

		public abstract string askFirmware();

		public abstract string askBattery();

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
			if (!(bool)sp.IsOpen)
			{
				sp.Open();
			}
			return new byte[] { 0 };
		}

		public virtual void setAccSchedule(byte[] schedule)
		{
			if (!(bool)sp.IsOpen)
			{
				sp.Open();
			}
		}

		public virtual void download(string fileName, UInt32 fromMemory, UInt32 toMemory, int baudrate)
		{
			if (!(bool)sp.IsOpen)
			{
				sp.Open();
			}
		}

		public virtual void downloadRemote(string fileName, UInt32 fromMemory, UInt32 toMemory, int baudrate)
		{
			if (!(bool)sp.IsOpen)
			{
				sp.Open();
			}
		}

		public abstract void extractArds(string fileNameMdp, string fileName, bool fromDownload);

		public virtual void convert(string fileName, string[] prefsIn)
		{

		}

		public abstract void eraseMemory();

		public virtual void setName(string newName)
		{

		}

		public virtual void disconnect()
		{
			if (!(bool)sp.IsOpen)
			{
				sp.Open();
			}
			connected = false;
		}

		void setStateDisconnected()
		{
			connected = false;
		}

		public virtual bool isRemote()
		{
			return false;
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

		public void Dispose()
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
				if (Console.KeyAvailable)
				{
					terminate(true);
				}
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
