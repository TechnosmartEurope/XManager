
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using System.Globalization;
using System.IO.Ports;
using System.ComponentModel;
using Windows.Security.Cryptography.Core;
#if X64
using FT_HANDLE = System.UInt64;
#else
using FT_HANDLE = System.UInt32;
#endif

namespace X_Manager.Units.Gipsy6
{
	class Gipsy6N : Gipsy6
	{

		#region DEFCONF

		public static readonly byte[] defConf = new byte[600] {    0xCF, 0x00, 0x00, 0x02,
		// 4	Nome unità: 27 caratteri + terminatore 0
		0x4e, 0x6f, 0x20, 0x4e, 0x61, 0x6d, 0x65, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		// 32	ACQ ON (240s) - ACQ OFF (6s)
		0xf0, 0x00, 0x58, 0x02,
		// 36	Alt on e SAT condition
		0xb4, 0x00,
		0x81,   //flag sat condition + n satelliti
		0x10,   //gsv minima
		// 40	Start delay (minuti, 32 bit)
		0x00, 0x00, 0x00, 0x80,
		// 44	Start delay (date)
		0x02, 0x05, 0xe5, 0x87,
		// 48	ADC soglia + magmin, flag trigger, flag log
		0x02, 0x00, 0x00, 0x00,
		// 52	P1: Schedule A (10 minuti)
		0x0a, 0x01,
		// 54	P1: Schedule B (1 ora)
		0x01, 0x02,
		// 56	Charging current (1 byte) + 57	Debug Events On + 58 Secondi Enhanced Accuracy + 59 Potenza Prossimità (-20dBm default)
		0x64, 0x00, 0x0f, 0xec,
		// 60 - P1: Orari (0 = off, 1 = sch.A, 2 = sch.B)
		0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
		// 84	P2: Schedule C (3 minuti)
		0x03, 0x01,
		// 86	P2: Schedule D (12 minuti)
		0x0c, 0x01,
		// 88	P2: Disponibile
		0x00, 0x0c, 0x00, 0x00,
		// 92 - P2: Orari (0 = off, 1 = sch.C, 2 = sch.D)
		0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
		// 116 - P2: Mesi validità
		0x80, 0x00,
		//118 Soglie batteria: batteryRefuseDownload
		0x9a, 0x09,
		//120 Disponibili
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		// 128 - G1: Vertici
		0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		//288 - G1: Schedule E (15 minuto)
		0x0f, 0x01,
		//290 - G1: Schedule F (20 secondi)
		0x14, 0x00,
		// 292	G1: Disponibile
		0x00, 0x0c, 0x00, 0x00,
		// 296	G1: Orari
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		// 320 - G2: Vertici
		0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		//480 - G2: Schedule G (1 ora)
		0x01, 0x02,
		//482 - G2: Schedule H (5 minuti)
		0x05, 0x01,
		// 484	G2: Disponibile
		0x00, 0x00, 0x00, 0x00,
		// 488	G2: Orari
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		// 512	G1/G2 Enable + 2 byte padding
		0x00, 0x00, 0x00, 0x00,
		//516 Flag. orari bitwise (0xFF), 517-519 orari remoto, 520-522 orari proximity, 523 minuti intervallo proximity
		0xff, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x0f,
		//524-526 Primo indirizzo gruppo prossimità, 527-529 Ultimo indirizzo gruppo prossimità
		0x00, 0x00, 0x02, 0x00, 0x00, 0x03,
		//530-539 Disponibili
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		//540	Unità remota/!locale, Indirizzo remoto
		0x01, 0xFF, 0xFF, 0xFF,
		//544	Soglie batteria
		0x00, 0x0a,     //3.75V		batteryStartLogging
		0x00, 0x0a,     //3.75V		batteryPauseLogging
		0x66, 0x0a,     //3.90V		batteryRestartLogging
		0x33, 0x09,     //3.45V		batteryLowRfStart
		0x77, 0x09,	 	//3.55V		batteryLowRfLog
		//554
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
		//600
		};

		#endregion

		struct TimeStamp
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
			public void resetPos(int initVal)
			{
				_pos = initVal;
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
				tout.resetPos(this.pos);

				return tout;
			}
		}

		const int RETRY_MAX = 4;
		int rfAddress = -1;
		string lastKnownRfAddressString = "N.A.";

		FileType fileType;
		enum FileType : byte
		{
			FILE_GP6 = 0,
			FILE_BS6
		}
		enum eventType : byte
		{
			E_POWER_ON = 0,
			E_SD_START,
			E_SD_STOP,
			E_ACQ_ON,
			E_ACQ_OFF,
			E_SCHEDULE,
			E_ALTON_START,
			E_ALTON_TIMEOUT,
			E_BATTERY_LOW,
			E_MEM_FULL,
			E_POWER_OFF,
			E_RESET,
			E_REMOTE_CONNECTION
		}

		static readonly int[] accuracySteps = { 1, 2, 5, 10, 15, 50, 100 };

		static readonly string[] events = {
			"Power ON.",
			"STARTDELAY - Beginning",
			"STARTDELAY - End",
			"Start searching for satellites...",
			"No visible satellite. Going to sleep...",
			"GPS Schedule: {0} {1}",
			"Fix Acquisition Start",
			"Fix Timeout (Checkpoint #{0})",
			"Low Battery.",
			"Memory Full.",
			"Power OFF",
			"Reset",
			"Remote Connection."
		};

		static readonly string[] scheduleEventTimings = {
			"second(s)",
			"minute(s)",
			"hour(s)",
		};

		//bool repeatEmptyValues = false;
		public bool remoteConnection = false;

		NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;

		BackgroundWorker txtBGW;
		BackgroundWorker kmlBGW;
		private static Semaphore txtSem;
		private static Semaphore kmlSem;
		private static Semaphore txtSemBack;
		private static Semaphore kmlSemBack;
		//private static long lastTimestamp = 0;
		//private static long conversionDone = 0;

		new byte[] firmwareArray = new byte[3];

		public Gipsy6N(object p)
			: base(p)
		{
			modelCode = model_Gipsy6N;
			configureMovementButtonEnabled = true;
			configurePositionButtonEnabled = false;
			defaultArdExtension = "gp6";
		}

		private bool ask(string command)
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

		public override string askName()
		{
			name = "";
			int retryMax = 1;
			byte[] nameAr = new byte[28];
			if (remoteConnection) retryMax = RETRY_MAX;
			for (int retry = 0; retry < retryMax; retry++)
			{
				if (!ask("N"))
				{
					throw new Exception(unitNotReady);
				}
				try
				{
					//byte nIn = 255;
					for (int i = 0; i < 28; i++)
					{
						nameAr[i] = (byte)ft.ReadByte();
					}
					for (int i = 0; i < 28; i++)
					{
						if (nameAr[i] != 0)
						{
							name += Convert.ToChar(nameAr[i]).ToString();
						}
						else
						{
							if (i == 0)
							{
								name = "[name empty]";
							}
							break;
						}
						//nIn = ft.ReadByte();
						//if (nIn != 0)
						//{
						//	name += Convert.ToChar(nIn).ToString();
						//}
						//else
						//{
						//	Thread.Sleep(100);
						//	int p = ft.ReadExisting();
						//	break;
						//}
					}
					break;
				}
				catch
				{
					if (retry == retryMax - 1) throw new Exception(unitNotReady);
				}
			}
			if (name == "") name = "[No Name]";
			unitName = name;
			return name;
		}

		public override string askBattery()
		{
			string battery = "";
			double battLevel = 0;
			int retryMax = 1;
			if (remoteConnection) retryMax = RETRY_MAX;
			for (int retry = 0; retry < retryMax; retry++)
			{
				if (!ask("B"))
				{
					throw new Exception(unitNotReady);
				}
				try
				{
					if (!remoteConnection) ft.ReadTimeout = 500;
					battLevel = ft.ReadByte(); battLevel *= 256;
					battLevel += ft.ReadByte();
					battLevel *= 6;
					battLevel /= 4096;
					break;
				}
				catch
				{
					if (retry == retryMax - 1) throw new Exception(unitNotReady);
				}
				//battLevel = battLevel + (battLevel - 3) * .05 + .14;


			}
			battery = Math.Round(battLevel, 2).ToString("0.00") + "V";
			return battery;
		}

		public override void setPcTime()
		{
			byte[] dateAr = new byte[6];

			if (remoteConnection)
			{
				ft.ReadTimeout = 2200;
				byte[] command = new byte[] { 84, 84, 84, 84, 84, 84, 84, 71, 71, 65, 0x74, 0, 0, 0, 0, 0, 0 };
				bool ok = false;
				for (int retry = 0; retry < RETRY_MAX; retry++)
				{
					var dateToSend = DateTime.UtcNow;
					dateAr[0] = (byte)dateToSend.Second;
					dateAr[1] = (byte)dateToSend.Minute;
					dateAr[2] = (byte)dateToSend.Hour;
					dateAr[3] = (byte)dateToSend.Day;
					dateAr[4] = (byte)dateToSend.Month;
					dateAr[5] = (byte)(dateToSend.Year - 2000);
					Array.Copy(dateAr, 0, command, 11, dateAr.Length);
					ft.Write(command, 17);
					try
					{
						ft.ReadByte();
						ok = true;
						break;
					}
					catch { }
				}
				if (!ok)
				{
					throw new Exception(unitNotReady);
				}
			}
			else
			{
				if (!ask("t"))
				{
					throw new Exception(unitNotReady);
				}
				try
				{
					var dateToSend = DateTime.UtcNow;
					dateAr[0] = (byte)dateToSend.Second;
					dateAr[1] = (byte)dateToSend.Minute;
					dateAr[2] = (byte)dateToSend.Hour;
					dateAr[3] = (byte)dateToSend.Day;
					dateAr[4] = (byte)dateToSend.Month;
					dateAr[5] = (byte)(dateToSend.Year - 2000);
					Thread.Sleep(10);
					ft.Write(dateAr, 6);
					ft.ReadByte();
				}
				catch
				{
					throw new Exception(unitNotReady);
				}
			}

		}

		public override uint[] askMaxMemory()
		{
			UInt32 m;
			int retryMax = 1;
			if (remoteConnection) retryMax = RETRY_MAX;
			for (int retry = 0; retry < retryMax; retry++)
			{
				if (!ask("m"))
				{
					throw new Exception(unitNotReady);
				}
				try
				{
					m = ft.ReadByte(); m *= 256;
					m += ft.ReadByte(); m *= 256;
					m += ft.ReadByte(); m *= 256;
					m += ft.ReadByte();
					mem_min_physical_address = m;
					m = ft.ReadByte(); m *= 256;
					m += ft.ReadByte(); m *= 256;
					m += ft.ReadByte(); m *= 256;
					m += ft.ReadByte();
					mem_max_physical_address = m;
					break;
				}
				catch
				{
					if (retry == retryMax - 1) throw new Exception(unitNotReady);
				}
			}
			return new uint[] { mem_min_physical_address, mem_max_physical_address };
		}

		public override uint[] askMemory()
		{
			int retryMax = 1;
			if (remoteConnection) retryMax = RETRY_MAX;
			for (int retry = 0; retry < retryMax; retry++)
			{
				if (!ask("M"))
				{
					throw new Exception(unitNotReady);
				}
				try
				{
					mem_address = ft.ReadByte(); mem_address <<= 8;
					mem_address += ft.ReadByte(); mem_address <<= 8;
					mem_address += ft.ReadByte(); mem_address <<= 8;
					mem_address += ft.ReadByte();
					mem_max_logical_address = ft.ReadByte(); mem_max_logical_address <<= 8;
					mem_max_logical_address += ft.ReadByte(); mem_max_logical_address <<= 8;
					mem_max_logical_address += ft.ReadByte(); mem_max_logical_address <<= 8;
					mem_max_logical_address += ft.ReadByte();
					break;
				}
				catch
				{
					if (retry == retryMax - 1) throw new Exception(unitNotReady);
				}
			}
			return new uint[] { mem_max_logical_address, mem_address };
		}

		public override void eraseMemory()
		{
			int retryMax = 1;
			if (remoteConnection) retryMax = RETRY_MAX;
			for (int retry = 0; retry < retryMax; retry++)
			{
				if (!ask("E"))
				{
					throw new Exception(unitNotReady);
				}
				try
				{
					if (!remoteConnection) ft.ReadTimeout = 500;
					ft.ReadByte();
					break;
				}
				catch
				{
					if (retry == retryMax - 1) throw new Exception(unitNotReady);
				}
			}
		}

		public override void setName(string newName)
		{

			byte[] nameShort = Encoding.ASCII.GetBytes(newName);

			if (remoteConnection)
			{
				byte[] name = new byte[28 + 11];
				Array.Copy(nameShort, 0, name, 11, nameShort.Length);
				name[0] = (byte)'T';
				name[1] = (byte)'T';
				name[2] = (byte)'T';
				name[3] = (byte)'T';
				name[4] = (byte)'T';
				name[5] = (byte)'T';
				name[6] = (byte)'T';
				name[7] = (byte)'G';
				name[8] = (byte)'G';
				name[9] = (byte)'A';
				name[10] = (byte)'n';

				ft.ReadTimeout = 2200;
				bool ok = false;
				for (int retry = 0; retry < RETRY_MAX; retry++)
				{

					ft.Write(name, 0, 28 + 11);
					uint res = 0;
					try
					{
						res = ft.ReadByte();
						ok = true;
						break;
					}
					catch { }
				}
				if (!ok) throw new Exception(unitNotReady);
			}
			else
			{
				byte[] name = new byte[28];
				Array.Copy(nameShort, 0, name, 0, nameShort.Length);

				for (int k = 0; k < 4; k++)
				{
					if (!ask("n"))
					{
						throw new Exception(unitNotReady);
					}
					Thread.Sleep(10);
					ft.ReadTimeout = 200;
					ft.Write(name, 28);
					uint res = 0;
					try
					{
						res = ft.ReadByte();
					}
					catch
					{
						throw new Exception(unitNotReady);
					}
					if (res == "I".ToArray()[0])
					{
						break;
					}
					Thread.Sleep(100);
				}
			}
		}

		public override bool getRemote()
		{
			remote = false;
			int retryMax = 1;
			if (remoteConnection) retryMax = RETRY_MAX;
			for (int retry = 0; retry < retryMax; retry++)
			{
				if (!ask("l"))
				{
					throw new Exception(unitNotReady);
				}

				try
				{
					if (ft.ReadByte() == 1) remote = true;
					break;
				}
				catch
				{
					if (retry == retryMax - 1) throw new Exception(unitNotReady);
				}
			}
			return remote;
		}

		public override byte[] getConf()
		{
			if (remoteConnection)
			{
				return getConfRemote();
			}
			else
			{
				return getConfCable();
			}
		}

		private byte[] getConfRemote()
		{
			ft.ReadTimeout = 2200;
			byte[] conf = new byte[0x1000];
			byte[] command = new byte[] { 84, 84, 84, 84, 84, 84, 84, 71, 71, 65, 67, 255 };
			int size = 0;

			for (int retry = 0; retry < RETRY_MAX; retry++)
			{
				Debug.WriteLine("get-size-" + retry.ToString());
				ft.ReadExisting();
				ft.Write(command, 0, 12);
				try
				{
					size = ft.ReadByte();
					size <<= 8;
					size += ft.ReadByte();
					if (size != 522)        //Questo poi andrà sistemato perché il software non sa a priori la dimensione del bufffer 
					{                       //di configurazione
						Debug.WriteLine("get-size=" + size.ToString() + " WRONG SIZE!");
						Thread.Sleep(500);
						retry = 0;
						continue;
					}
					break;
				}
				catch
				{
					if (retry == 3) throw new Exception(unitNotReady);
				}
			}

			Debug.WriteLine("get-size=" + size.ToString());

			byte nPack = (byte)(size / 64);
			byte rPack = (byte)(size % 64);

			for (byte i = 0; i < nPack; i++)
			{
				command[11] = i;
				for (int retry = 0; retry < RETRY_MAX; retry++)
				{
					Debug.WriteLine("get-packet" + i.ToString() + "-" + retry.ToString());
					try
					{
						ft.Write(command, 0, 12);
						ft.Read(conf, ((uint)i * 64) + 32, 64);
						break;
					}
					catch
					{
						if (retry == 3) throw new Exception(unitNotReady);
					}
				}
			}

			command[11] = nPack;
			for (int retry = 0; retry < RETRY_MAX; retry++)
			{
				Debug.WriteLine("get-packet" + nPack.ToString() + "-" + retry.ToString());
				try
				{
					ft.Write(command, 0, 12);
					int read = ft.Read(conf, ((uint)nPack * 64) + 32, rPack);
					break;
				}
				catch
				{
					if (retry == 3) throw new Exception(unitNotReady);
				}
			}
			ft.ReadExisting();

			return conf;
		}

		private byte[] getConfCable()
		{
			byte[] conf = new byte[0x1000];
			if (!ask("C"))
			{
				throw new Exception(unitNotReady);
			}
			try
			{
				//ACQ, Start delay e GSV			
				for (int i = 32; i < 32 + 20; i++)
				{
					conf[i] = ft.ReadByte();
				}
				//Schedule A e B
				for (int i = 52; i < 52 + 32; i++)
				{
					conf[i] = ft.ReadByte();
				}
				ft.Write(new byte[] { 1 }, 1);//***********************************************SYNC

				//Schedule C e D + mesi + primo quadrato geofencing 1
				for (int i = 84; i < 84 + 60; i++)
				{
					conf[i] = ft.ReadByte();
				}
				ft.Write(new byte[] { 2 }, 1);//***********************************************SYNC

				//Quadrati 2-5 geofencing-1
				for (int i = 144; i < 144 + 64; i++)
				{
					conf[i] = ft.ReadByte();
				}
				ft.Write(new byte[] { 3 }, 1);//***********************************************SYNC

				//Quadrati 6-9 geofencing-1
				for (int i = 208; i < 208 + 64; i++)
				{
					conf[i] = ft.ReadByte();
				}
				ft.Write(new byte[] { 4 }, 1);//***********************************************SYNC

				//Quadrato 10 geofencing-1 + Schedule E/F + Orari Geofencing 1 + Primo quadrato geofencing-2
				for (int i = 272; i < 272 + 64; i++)
				{
					conf[i] = ft.ReadByte();
				}
				ft.Write(new byte[] { 5 }, 1);//***********************************************SYNC

				//Quadrati 2-5 geofencing-2
				for (int i = 336; i < 336 + 64; i++)
				{
					conf[i] = ft.ReadByte();
				}
				ft.Write(new byte[] { 6 }, 1);//***********************************************SYNC

				//Quadrati 6-9 geofencing-2
				for (int i = 400; i < 400 + 64; i++)
				{
					conf[i] = ft.ReadByte();
				}
				ft.Write(new byte[] { 7 }, 1);//***********************************************SYNC

				//Ultimo quadrato geofencing 2 + Schedule G/H + orari Geofencing 2 + Enable Geofencing 1 e 2 + schedule remoto + unità locale/remota + indirizzo remoto
				for (int i = 464; i < 464 + 50; i++)
				{
					conf[i] = ft.ReadByte();
				}
				if (firmTotA > 1)
				{
					for (int i = 514; i < 514 + 14; i++)
					{
						conf[i] = ft.ReadByte();
					}
					ft.Write(new byte[] { 8 }, 1);//***********************************************SYNC
					int end = 12;
					if (firmTotA >= 1000000)
					{
						end = 26;
					}
					for (int i = 528; i < 528 + end; i++)
					{
						conf[i] = ft.ReadByte();
					}
				}
				ft.Write(new byte[] { 9 }, 1);//***********************************************SYNC
			}
			catch
			{
				Thread.Sleep(100);
				throw new Exception(unitNotReady);
			}

			return conf;
		}

		public override void setConf(byte[] conf)
		{
			if (remoteConnection)
			{
				setConfRemote(conf);
			}
			else
			{
				setConfCable(conf);
			}

		}

		private void setConfCable(byte[] conf)
		{
			for (int j = 0; j < 3; j++)
			{
				if (!ask("c"))
				{
					throw new Exception(unitNotReady);
				}
				ft.ReadTimeout = 400;
				uint packetLength = 28;
				uint packetNumber = 17;
				if (firmTotA < 1003001)
				{
					packetLength = 64;
					packetNumber = 7;
				}
				try
				{
					ft.ReadByte();
					for (uint i = 0; i < packetNumber; i++)
					{
						ft.Write(conf, (i * packetLength) + 32, packetLength);
						ft.ReadByte();
					}

					ft.Write(conf, 480, 20);
					if (firmTotA > 1003000)
					{
						ft.ReadByte();
					}

					ft.Write(conf, 500, 16);
					ft.ReadByte();

					if (firmTotA > 1)
					{
						ft.Write(conf, 516, 24);
					}
					if (firmTotA >= 1000000)
					{
						Thread.Sleep(5);
						ft.Write(conf, 540, 14);
					}
					ft.ReadByte();

					break;
				}
				catch
				{
					if (j == 2)
					{
						throw new Exception(unitNotReady);
					}
					else
					{
						Thread.Sleep(100);
					}
				}
			}
		}

		private void setConfRemote(byte[] conf)
		{
			int size = 0;
			ft.ReadTimeout = 2200;
			for (int retry = 0; retry < RETRY_MAX; retry++)
			{
				Debug.WriteLine("set-getSize-" + retry.ToString());
				try
				{
					ft.ReadExisting();
					ft.Write(new byte[] { 84, 84, 84, 84, 84, 84, 84, 71, 71, 65, 67, 255 }, 0, 12);
					size = ft.ReadByte();
					size <<= 8;
					size += ft.ReadByte();
					if (size != 522)    //Questo poi andrà sistemato perché il software non sa a priori la dimensione del bufffer 
					{                   //di configurazione
						Debug.WriteLine("set-getSize=" + size.ToString() + " WRONG SIZE!");
						Thread.Sleep(500);
						retry = 0;
						continue;
					}
					break;
				}
				catch
				{
					if (retry == 3) throw new Exception(unitNotReady);
				}
			}

			Debug.WriteLine("set-getSize=" + size.ToString());

			Thread.Sleep(1);
			byte nPack = (byte)(size / 64);
			byte rPack = (byte)(size % 64);

			byte[] command = new byte[76];
			for (int i = 0; i < 7; i++)
			{
				command[i] = 84;
			}
			command[7] = 71;
			command[8] = 71;
			command[9] = 65;
			command[10] = 99;

			for (byte i = 0; i < nPack; i++)
			{
				command[11] = i;
				Array.Copy(conf, ((uint)i * 64) + 32, command, 12, 64);
				for (int retry = 0; retry < RETRY_MAX; retry++)
				{
					Debug.WriteLine("set-packet" + i.ToString() + "-" + retry.ToString());
					try
					{
						ft.Write(command, 0, 76);
						ft.ReadByte();
						Thread.Sleep(1);
						break;
					}
					catch
					{
						if (retry == 3) throw new Exception(unitNotReady);
					}
				}
			}
			command[11] = nPack;
			Array.Copy(conf, ((uint)nPack * 64) + 32, command, 12, rPack);
			for (int retry = 0; retry < RETRY_MAX; retry++)
			{
				Debug.WriteLine("set-packet" + nPack.ToString() + "-" + retry.ToString());
				try
				{
					ft.Write(command, 0, (uint)12 + rPack);
					ft.ReadByte();
					break;
				}
				catch
				{
					if (retry == 3) throw new Exception(unitNotReady);
				}
			}
		}

		public override void disconnect()
		{
			if (remote)
			{
				ask("O");
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
				ask("O");
			}

			connected = false;
		}

		public unsafe override void download(string fileName, uint fromMemory, uint toMemory, int baudrate)
		{

			byte[] outBuffer = new byte[50];
			byte[] inBuffer;
			byte[] tempBuffer = new byte[2048];
			byte[] address = new byte[8];

			ft.ReadTimeout = 1000;

			uint buffSize;
			if (mem_address > mem_max_logical_address)
			{
				buffSize = mem_address - mem_max_logical_address;
			}
			else
			{
				buffSize = (mem_max_physical_address - mem_max_logical_address) + (mem_address - mem_min_physical_address);
			}

			if ((buffSize & 0x1ff) != 0)
			{
				buffSize &= 0xfffffe00;
				buffSize += 0x200;
			}

			convertStop = false;
			inBuffer = new byte[buffSize + 2];
			uint buffPointer = 0;

			string fileNameMdp = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + ".gp6";

			if (!ask("D"))
			{
				throw new Exception(unitNotReady);
			}

			ft.ReadTimeout = 1600;

			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.progressBarStopButton.IsEnabled = true));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.progressBarStopButtonColumn.Width = new GridLength(80)));

			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.IsIndeterminate = false));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Minimum = 0));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Maximum = buffSize));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Value = buffPointer));

			address = BitConverter.GetBytes(mem_max_logical_address);
			//Array.Reverse(address);
			Array.Copy(address, 1, outBuffer, 1, 3);
			outBuffer[0] = 0x65;        //load address
			ft.Write(outBuffer, 4);
			try
			{
				ft.ReadByte();
			}
			catch
			{
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFailed()));
				return;
			}

			bool ok = true;

			while (buffPointer < buffSize)
			{

				if (convertStop)
				{
					break;
				}

				ft.Write(new byte[] { 66 }, 1);
				if (ft.Read(inBuffer, buffPointer, 0x200) < 0x200)
				{
					ok = false;
					if (buffPointer != 0)
					{
						var foC = new BinaryWriter(File.Open(fileNameMdp, FileMode.Create));
						foC.Write(inBuffer, 0, inBuffer.Length);
						foC.Close();
					}
					Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFailed()));
					break;
				}
				buffPointer += 0x200;
				if (MainWindow.keepAliveTimer != null)
				{
					MainWindow.keepAliveTimer.Stop();
					MainWindow.keepAliveTimer.Start();
				}
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Value = buffPointer));

			}

			inBuffer[inBuffer.Length - 2] = modelCode;
			inBuffer[inBuffer.Length - 1] = 0x00;

			var fo = new BinaryWriter(File.Open(fileNameMdp, FileMode.Create));

			fo.Write(inBuffer, 0, inBuffer.Length);
			fo.Close();

			if (ok)
			{
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFinished()));
			}
		}

		public unsafe override void downloadRemote(string fileName, uint fromMemory, uint toMemory, int baudrate)
		{

			//Globale mem_address è fino a dove bisogna scaricare
			//Globale mem_max_logical_address è l'indirizzo da cui iniziare a scaricare

			byte[] inBuffer;

			bool resume = false;
			if (fileName.Contains("incomplete")) resume = true;

			ft.ReadTimeout = 2200;
			MainWindow.keepAliveTimer.Stop();
			FileStream fs = null;

			uint maxLogical = mem_max_logical_address;
			if (resume)
			{
				fs = File.OpenRead(fileName);
				byte[] addArr = new byte[4];
				try
				{
					fs.Position = fs.Length - 10;
					fs.Read(addArr, 0, 4);
				}
				catch { }

				maxLogical = BitConverter.ToUInt32(addArr, 0);
			}

			uint buffSize;
			if (mem_address > mem_max_logical_address)
			{
				buffSize = mem_address - mem_max_logical_address;
			}
			else
			{
				buffSize = (mem_max_physical_address - mem_max_logical_address) + (mem_address - mem_min_physical_address);
			}

			if ((buffSize & 0x1ff) != 0)
			{
				buffSize &= 0xfffffe00;
				buffSize += 0x200;
			}
			uint buffPointer = 0;
			inBuffer = new byte[buffSize];
			if (resume)
			{
				fs.Position = 0;
				fs.Read(inBuffer, 0, (int)(fs.Length - 10));
				buffPointer = (uint)(fs.Length - 10);
				fs.Close();
			}

			convertStop = false;
			uint address = maxLogical;

			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.progressBarStopButton.IsEnabled = true));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.progressBarStopButtonColumn.Width = new GridLength(80)));

			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.IsIndeterminate = false));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Minimum = 0));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Maximum = buffSize));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Value = buffPointer));

			bool sendCommand = true;

			byte[] command = new byte[] {   (byte)'T', (byte)'T', (byte)'T', (byte)'T', (byte)'T', (byte)'T', (byte)'T', (byte)'G',
											(byte)'G', (byte)'A', (byte)'D' };

			byte[] outBuffer = new byte[5];
			int res = 0;

			byte[] ack = new byte[1];
			//byte seqNumber = 0;
			outBuffer[0] = (byte)'T';
			while (buffPointer < buffSize)
			{
				if (convertStop)
				{
					break;
				}
				if (sendCommand)
				{
					Debug.WriteLine("command-D address:" + address.ToString("X8"));
					ft.ReadExisting();
					ft.Write(command, 0, 11);
					res = ft.Read(ack, 0, 1);
					if (res != 1)
					{
						continue;
					}
					sendCommand = false;
					//address &= 0xfffffe00;
					//buffPointer &= 0xfffffe00;
				}
				if ((address % 0x200) == 0)
				{
					Debug.WriteLine("dl-A-" + address.ToString("X8"));
					outBuffer[1] = (byte)'A';
					outBuffer[2] = (byte)(address >> 24);
					outBuffer[3] = (byte)(address >> 16);
					outBuffer[4] = (byte)(address >> 8);

					ft.Write(outBuffer, 0, 5);
					res = ft.Read(ack, 0, 1);
					if (res != 1)
					{
						sendCommand = true;
						continue;
					}
					outBuffer[1] = (byte)'P';
					Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Value = buffPointer));
				}
				outBuffer[2] = (byte)(address % 0x200 / 0x40);
				Debug.WriteLine("dl-P-" + outBuffer[2].ToString());
				ft.Write(outBuffer, 0, 3);
				res = ft.Read(inBuffer, buffPointer, 0x40);
				if (res < 0x40)
				{
					ft.ReadExisting();
					sendCommand = true;
					continue;
				}
				else
				{
					//seqNumber++;
					//outBuffer[2] = seqNumber;
					buffPointer += 0x40;
					address += 0x40;
					if (address == mem_max_physical_address)
					{
						address = mem_min_physical_address;
					}
				}
			}
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Value = buffPointer));
			command[1] = (byte)'x';
			ft.Write(command, 0, 3);
			ft.Read(command, 0, 1);
			Thread.Sleep(100);

			if (buffPointer > 0)
			{
				try
				{
					string fileNameMdp = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName);
					if (File.Exists(fileNameMdp + ".gp6")) File.Delete(fileNameMdp + ".gp6");
					if (resume)
					{
						fileNameMdp = fileNameMdp.Remove(fileNameMdp.IndexOf("_incomplete"), 11);
					}
					if (buffPointer < buffSize)
					{
						Array.Resize(ref inBuffer, ((int)buffPointer / 0x200) * 0x200);
						address = (address & 0xfffffe00);
						fileNameMdp += "_incomplete";
					}
					fileNameMdp += ".gp6";
					Array.Resize(ref inBuffer, inBuffer.Length + 10);
					Array.Copy(BitConverter.GetBytes(address), 0, inBuffer, inBuffer.Length - 10, 4);
					Array.Copy(BitConverter.GetBytes(mem_address), 0, inBuffer, inBuffer.Length - 6, 4);
					inBuffer[inBuffer.Length - 2] = modelCode;
					inBuffer[inBuffer.Length - 1] = 0x00;
					var fo = new BinaryWriter(File.Open(fileNameMdp, FileMode.Create));

					fo.Write(inBuffer, 0, inBuffer.Length);
					fo.Close();
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message);
					Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFailed()));
					return;
				}

			}


			if (buffPointer > 0)
			{
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFinished()));
			}
			try
			{
				MainWindow.keepAliveTimer.Start();
			}
			catch { }

		}

		public override void abortConf() { }

		public override void extractArds(string fileNameMdp, string fileName, bool fromDownload)
		{
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusLabel.Content = "Creating Ard file(s)..."));
			var mdp = new BinaryReader(System.IO.File.Open(fileNameMdp, FileMode.Open));

			BinaryWriter ard = BinaryWriter.Null;
			//ushort packLength = 255;
			//ushort firstPackLength = 254;
			string fileNameArd = "";
			byte testByte, testByte2;
			const int yes = 1;
			const int no = 2;
			const int yes_alaways = 11;
			int resp = no;
			ushort counter = 0;

			while (mdp.BaseStream.Position < mdp.BaseStream.Length)
			{

				testByte = mdp.ReadByte();

				if (testByte == 0xcf)
				{
					testByte2 = mdp.ReadByte();
					if (ard != BinaryWriter.Null)
					{
						ard.Close();
					}
					counter++;
					fileNameArd = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + "_S" + counter.ToString() + ".ard";

					if (System.IO.File.Exists(fileNameArd))
					{
						if (resp < 11)
						{
							var yn = new YesNo(fileNameArd + " already exists. Do you want to overwrite it?", "FILE EXISTING", "Remeber my choice");
							resp = yn.ShowDialog();
						}
						if ((resp == yes) | (resp == yes_alaways))
						{
							System.IO.File.Delete(fileNameArd);
						}
						else
						{
							do
							{
								fileNameArd = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileNameArd) + " (1)" + ".ard";
							} while (System.IO.File.Exists(fileNameArd));
						}
					}   //Richiesta overwrite se file già esistente

					ard = new System.IO.BinaryWriter(System.IO.File.Open(fileNameArd, FileMode.Create));
					ard.Write(new byte[] { modelCode }, 0, 1);
					if (!connected)
					{
						var oldPosition = mdp.BaseStream.Position;
						mdp.BaseStream.Position = mdp.BaseStream.Length - 1;
						if (mdp.ReadByte() == 254)
						{
							mdp.BaseStream.Position -= 5;
							firmwareArray = mdp.ReadBytes(3);
						}
						mdp.BaseStream.Position = oldPosition;
					}
					ard.Write(firmwareArray, 0, 3);
					ard.Write(mdp.ReadBytes(254));
				}

				else if (testByte == 0x55)
				{
					mdp.ReadBytes(2);
					ard.Write(mdp.ReadBytes(253));
				}

				else if (testByte == 0xff)
				{
					try
					{
						mdp.ReadBytes(255);
						if (mdp.ReadByte() == 0xcf)
						{
							mdp.BaseStream.Position--;
						}
						else
						{
							break;
						}

					}
					catch
					{
						break;
					}
				}
				else
				{


				}
			}
			try
			{
				mdp.Close();
				ard.Close();
			}
			catch { }

			try
			{
				if (Parent.getParameter("keepMdp").Equals("false"))
				{
					File.Delete(fileNameMdp);
				}
				else
				{
					if (!Path.GetExtension(fileNameMdp).Contains("Dump"))
					{
						string newFileNameMdp = Path.GetDirectoryName(fileNameMdp) + "\\" + Path.GetFileNameWithoutExtension(fileNameMdp) + ".memDump";
						if (File.Exists(newFileNameMdp)) System.IO.File.Delete(newFileNameMdp);
						//string newFileNameMdp = Path.GetFileNameWithoutExtension(fileNameMdp) + ".memDump";
						File.Move(fileNameMdp, newFileNameMdp);
					}
				}
			}
			catch { }
			if (!fromDownload) Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.nextFile()));
		}

		public override void convert(string fileName, string[] prefs)
		{
			base.convert(fileName, prefs);

			//Stabilisce se è un download diretto o da basestation
			if (Path.GetExtension(fileName).IndexOf("bs6", StringComparison.InvariantCultureIgnoreCase) != -1)
			{
				fileType = FileType.FILE_BS6;       //Basestation
			}
			else
			{
				fileType = FileType.FILE_GP6;       //Diretto
			}

			//Attribuisce un nome all'unità
			if (unitName == "")
			{
				string newName = Path.GetFileNameWithoutExtension(fileName);
				if (newName.Contains("_p"))
				{
					newName = newName.Remove(newName.IndexOf("_p"), 2);
				}
				unitName = newName;
			}
			lastKnownUnitName = unitName;

			//Triplica la progress bar
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
			{
				parent.statusProgressBar.Height = 6;
				parent.txtProgressBar.Height = 6;
				parent.kmlProgressBar.Height = 6;
				parent.statusProgressBar.Margin = new Thickness(10, 5, 10, 0);
				parent.txtProgressBar.Margin = new Thickness(10, 0, 10, 0);
				parent.kmlProgressBar.Margin = new Thickness(10, 0, 10, 0);
			}));

			//Reinizializza le variabili statiche (in caso di più file da convertire)
			//conversionDone = 0;
			//lastTimestamp = 0;

			//Carica il file gp6 in memoria
			BinaryReader gp6File = new BinaryReader(new FileStream(fileName, FileMode.Open));
			int filePointer = 0;
			byte[] gp6 = null;
			int headerLength = 0;
			if (fileType == FileType.FILE_BS6)
			{
				headerLength = 0x600;
			}

			long buffLen = gp6File.BaseStream.Length;
			buffLen -= 2;               //Toglie gli ultimi due byte col tipo di unità dal calcolo
			buffLen -= headerLength;    //Toglie l'eventuale header
			long partH = buffLen / 0x200;
			if (buffLen % 0x200 > 0)
			{
				partH += 1;
			}
			buffLen -= (partH * 2);
			gp6 = new byte[buffLen];

			if (fileType == FileType.FILE_BS6)  //Recupera il nome dell'unità e l'eventuale schedule
			{
				gp6File.BaseStream.Position = 0x16;
				string add = Encoding.ASCII.GetString(gp6File.ReadBytes(28));
				add = add.Trim(Path.GetInvalidFileNameChars());
				int stringPos = add.Length - 1;
				while (add[add.Length - 1] == ' ')
				{
					add = add.Substring(0, add.Length - 1);
				}
				fileName = Path.GetDirectoryName(fileName) + "\\" + add + Path.GetFileName(fileName).Substring(8, 16);
				gp6File.BaseStream.Position = 0x15;
				byte newConfP = gp6File.ReadByte();
				if (newConfP != 0)
				{
					byte[] config = new byte[616];
					Array.Copy(Encoding.ASCII.GetBytes("--gipsy6Config--"), config, 16);
					gp6File.BaseStream.Position = 0x32;
					gp6File.Read(config, 0x10, 600);
					int year = config[88 + 0x10] + 2000;
					int month = config[89 + 0x10];
					int day = config[90 + 0x10];
					int hour = config[91 + 0x10];
					DateTime confDate = DateTime.Now;
					try
					{
						confDate = new DateTime(year, month, day, hour, 0, 0);
						string fn = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName)
													+ "_configuration (" + confDate.ToString("dd-MM-yyyy HH.mm.ss") + ").cfg";
						File.WriteAllBytes(fn, config);
					}
					catch
					{
						File.WriteAllBytes(Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + ".cfg", config);
					}

				}
				gp6File.BaseStream.Position = 0x600;
			}



			for (int i = 0; i < ((gp6File.BaseStream.Length - headerLength) / 0x200); i++)
			{
				gp6File.ReadBytes(2);
				gp6File.BaseStream.Read(gp6, filePointer, 510);
				filePointer += 510;
			}
			if (gp6File.BaseStream.Length - gp6File.BaseStream.Position > 2)
			{
				gp6File.BaseStream.Position += 2;
				gp6File.BaseStream.Read(gp6, filePointer, (int)(gp6File.BaseStream.Length - gp6File.BaseStream.Position - 2));
			}
			gp6File.Close();

			//Crea e avvia il thread per la scrittura del file txt
			string txtName = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + ".txt";
			List<TimeStamp> txtList = new List<TimeStamp>();
			txtSem = new Semaphore(0, 1);
			txtSemBack = new Semaphore(1, 1);
			txtBGW = new BackgroundWorker();
			txtBGW.DoWork += (s, args) =>
			{
				txtBGW_doWork(ref txtList, txtName);
			};
			txtBGW.RunWorkerAsync();

			List<TimeStamp> kmlList = null;
			if (pref_makeKml)
			{
				//Crea e avvia il thread per la scrittura del file kml
				string kmlName = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName);
				kmlList = new List<TimeStamp>();
				kmlSem = new Semaphore(0, 1);
				kmlSemBack = new Semaphore(1, 1);
				kmlBGW = new BackgroundWorker();
				kmlBGW.DoWork += (s, args) =>
				{
					kmlBGW_doWork(ref kmlList, kmlName);
				};
				kmlBGW.RunWorkerAsync();
			}

			//Inizializza le variabili
			int pos = 0;
			int end = gp6.Length;
			TimeStamp timeStamp = new TimeStamp();
			timeStamp.dateTime = new DateTime(2, 1, 1, 1, 0, 0);
			List<byte> noStampBuffer = new List<byte>();

			//Inizializza la progress bar
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
			{
				parent.statusProgressBar.IsIndeterminate = false;
				parent.statusProgressBar.Minimum = 0;
				parent.statusProgressBar.Maximum = end;
				parent.statusProgressBar.Value = 0;
				parent.txtProgressBar.Minimum = 0;
				parent.txtProgressBar.Maximum = end;
				parent.txtProgressBar.Value = 0;
				parent.kmlProgressBar.Minimum = 0;
				parent.kmlProgressBar.Maximum = end;
				parent.kmlProgressBar.Value = 0;
			}));

			//Importa il livello di debug per la conversione
			pref_debugLevel = parent.stDebugLevel;
			if (pref_debugLevel > 0)
			{
				pref_battery = true;
				pref_metadata = true;
				pref_proximity = true;
			}

			int progressBarCounter = 0;
			//Cicla nel buffer decodificando i timestamp e aggiungendoli alla pila
			int actPos = 0;
			while (pos < end)
			{
				actPos = (((pos / 512) + 1) * 2) + pos;

				try
				{
					noStampBuffer = decodeTimeStamp(ref gp6, ref timeStamp, ref pos);   //decodifica il timestamp
				}
				catch
				{
					continue;
				}


				if (noStampBuffer.Count == 1)   //Segnale di fine file
				{
					break;
				}

				//Aggiorna la progress bar del producer
				progressBarCounter++;
				if (progressBarCounter >= 100)
				{
					Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
					{
						parent.statusProgressBar.Value = pos;
					}));
					progressBarCounter = 0;
				}

				if (timeStamp.txtAllowed > 0)
				{
					txtSemBack.WaitOne();
					txtList.Add(timeStamp.clone()); //aggiunge il timestamp alla pila txt
					txtSem.Release();
				}

				if (kmlList != null)
				{
					kmlSemBack.WaitOne();
					kmlList.Add(timeStamp.clone()); //aggiunge il timestamp alla pila kml
					kmlSem.Release();
				}

				if (noStampBuffer.Count == 1)
				{
					break;
				}
			}
			//Aggiorna per l'ultima volta la progress bar del producer
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
			{
				parent.statusProgressBar.Value = pos;
			}));

			if (pref_makeKml)
			{
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.kmlProgressBar.Maximum = kmlList.Count));
			}
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.txtProgressBar.Maximum = txtList.Count));
			//Interlocked.Increment(ref lastTimestamp);   //Segnala ai thread che non saranno più aggiunti timestamp alle pile
			txtSemBack.WaitOne();
			if (pref_makeKml)
			{
				kmlSemBack.WaitOne();
				kmlSem.Release();   //rilascia il thread kml per l'ultima volta
			}
			txtSem.Release();   //rilascia il thread txt per l'ultima volta


			//while (Interlocked.Read(ref conversionDone) < 2)
			//{
			//	Thread.Sleep(200);
			//}
			//aspetta che i thread abbiano finito di scrivere i rispettivi file
			txtSemBack.WaitOne();
			if (pref_makeKml) kmlSemBack.WaitOne();

			Application.Current.Dispatcher.Invoke(new Action(() =>
			{
				parent.statusProgressBar.Height = 20;
				parent.txtProgressBar.Height = 0;
				parent.kmlProgressBar.Height = 0;
				parent.statusProgressBar.Margin = new Thickness(10, 5, 10, 10);
			}));

			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
				new Action(() => parent.nextFile()));
		}

		private void txtBGW_doWork(ref List<TimeStamp> tL, string txtName)
		{
			StreamWriter txtBW = new StreamWriter(new FileStream(txtName, FileMode.Create));

			placeHeader(txtBW);//, ref columnPlace);

			string[] tabs = new string[p_fileCsv_length];
			tabs[p_fileCsv_name] = lastKnownUnitName;
			tabs[p_fileCsv_rfAddress] = lastKnownRfAddressString;
			//if (fileType == FileType.FILE_BS6)
			//{
			//	unitName = Path.GetFileNameWithoutExtension(txtName);
			//}
			var t = new TimeStamp();
			while (true)
			{
				txtSem.WaitOne();
				if (tL.Count == 0)  //Se non ci sono più timestamp nella pila, si esce dal loop
				{
					break;
				}
				t = tL[0];
				tL.RemoveAt(0);

				//Si scrive il timestmap nel txt
				if (!pref_repeatEmptyValues)
				{
					tabs = new string[p_fileCsv_length];
					tabs[p_fileCsv_name] = t.unitNameTxt;
					tabs[p_fileCsv_rfAddress] = t.rfAddressString;
				}
				tabs[p_fileCsv_date] = t.dateTime.Day.ToString("00") + "/" + t.dateTime.Month.ToString("00") + "/" + t.dateTime.Year.ToString("0000");
				if (pref_sameColumn)
				{
					tabs[p_fileCsv_date] += " " + t.dateTime.Hour.ToString("00") + ":" + t.dateTime.Minute.ToString("00") + ":" + t.dateTime.Second.ToString("00");
				}
				else
				{
					tabs[p_fileCsv_time] = t.dateTime.Hour.ToString("00") + ":" + t.dateTime.Minute.ToString("00") + ":" + t.dateTime.Second.ToString("00");
				}

				if (((t.tsType & ts_battery) == ts_battery) && pref_battery)
				{
					tabs[p_fileCsv_battery] = t.batteryLevel.ToString("0.00") + "V";
				}
				if ((t.tsType & ts_coordinate) == ts_coordinate)
				{
					tabs[p_fileCsv_latitude] = t.lat.ToString("00.0000000", nfi);
					tabs[p_fileCsv_longitude] = t.lon.ToString("000.0000000", nfi);
					if (t.hAcc == 7)
					{
						tabs[p_fileCsv_horizontalAccuracy] = "200";
					}
					else
					{
						tabs[p_fileCsv_horizontalAccuracy] = String.Format("{0}", accuracySteps[t.hAcc]);
					}

					tabs[p_fileCsv_altitude] = t.altitude.ToString();
					if (t.vAcc == 7)
					{
						tabs[p_fileCsv_verticalAccuracy] = "200";
					}
					else
					{
						tabs[p_fileCsv_verticalAccuracy] = String.Format("{0}", accuracySteps[t.vAcc]);
					}
					tabs[p_fileCsv_speed] = t.speed.ToString("0.0");
					tabs[p_fileCsv_course] = t.cog.ToString("0.0");
				}

				if (((t.tsTypeExt1 & ts_proximity) == ts_proximity) && pref_proximity)
				{
					tabs[p_fileCsv_proximity] = t.proximityAddress.ToString();
					tabs[p_fileCsv_proximityPower] = t.proximityPower.ToString();
				}

				if (((t.tsType & ts_event) == ts_event) && pref_metadata)
				{
					tabs[p_fileCsv_event] = decodeEvent(ref t);
				}

				if (pref_debugLevel > 0)
				{
					tabs[p_fileCsv_position] = t.pos.ToString("X8");
				}

				for (int i = 0; i < p_fileCsv_length - 1; i++)
				{
					txtBW.Write(tabs[i] + "\t");
				}
				txtBW.Write(tabs[p_fileCsv_length - 1] + "\r\n");
				txtSemBack.Release();

			}

			txtBW.Close();
			txtSemBack.Release();

			//Interlocked.Increment(ref conversionDone);
		}

		private void kmlBGW_doWork(ref List<TimeStamp> tL, string kmlName)
		{
			//								data   ora    lon   lat   alt   eve   batt

			//BinaryWriter placeMark;
			//placeMark = new BinaryWriter(new FileStream(kmlName + ".kml", FileMode.Create));
			//BinaryWriter kml;
			//kml = new BinaryWriter(new FileStream(kmlName + "_temp.kml", FileMode.Create));
			StreamWriter placeMark;
			placeMark = new StreamWriter(kmlName + ".kml");
			StreamWriter kml;
			kml = new StreamWriter(kmlName + "_temp.kml");

			//string kmlS = Properties.Resources.Folder_Path_Top + Properties.Resources.Path_Top;
			//string placeS = Properties.Resources.Final_Top_1 + Path.GetFileNameWithoutExtension(kmlName) + Properties.Resources.Final_Top_2;

			kml.Write(Properties.Resources.Folder_Path_Top + Properties.Resources.Path_Top);
			placeMark.Write(Properties.Resources.Final_Top_1 + Path.GetFileNameWithoutExtension(kmlName) + Properties.Resources.Final_Top_2);

			int contoCoord = 0;
			int pbmax = 0;
			bool primaCoordinata = true;
			string lonS = "", latS = "", altS = "";

			var t = new TimeStamp();
			while (true)
			{
				//if (Interlocked.Read(ref lastTimestamp) == 0)   //Se il thread principale sta ancora aggiungendo timestamp alla pila
				//{                                               //aspetta che il thread principale abbia aggiunto un nuovo timestamp alla lista
				//	kmlSem.WaitOne();
				//}
				kmlSem.WaitOne();
				if (tL.Count == 0)  //Se non ci sono più timestamp nella pila, si esce dal loop
				{
					break;
				}
				t = tL[0];
				tL.RemoveAt(0);

				if (t.sat > 0) //Si scrive il timestmap nel kml
				{
					if (contoCoord == 10000)
					{
						kml.Write(Properties.Resources.Path_Bot + Properties.Resources.Path_Top);
						contoCoord = 0;
					}

					kml.Write("\t\t\t\t\t");
					lonS = t.lon.ToString("00.0000000", nfi) + ",";
					kml.Write(lonS);
					latS = t.lat.ToString("00.0000000", nfi) + ",";
					kml.Write(latS);
					altS = t.altitude.ToString("0000.0", nfi);
					kml.Write(altS);

					if (primaCoordinata)
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
					placeMark.Write(X_Manager.Properties.Resources.Placemarks_Generic_Bot);

					contoCoord++;
					//kml.Write(kmlS);
					//kmlS = "";
					//placeMark.Write(placeS);
					//placeS = "";
				}
				//kmlSem.Release();
				kmlSemBack.Release();
			}
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.kmlProgressBar.Value = pbmax));
			//kml.Write(kmlS);
			//placeMark.Write(placeS);
			//Scrive il segnaposto di stop nel fime kml dei placemarks
			//placeMark.Write(System.Text.Encoding.ASCII.GetBytes(X_Manager.Properties.Resources.Folder_Bot));
			//placeMark.Write(System.Text.Encoding.ASCII.GetBytes(X_Manager.Properties.Resources.Placemarks_Stop_Top +
			//	"\r\n\t\t\t\t\t+<coordinates>" + temp + "</coordinates>\r\n" + X_Manager.Properties.Resources.Placemarks_Stop_Bot));
			placeMark.Write(X_Manager.Properties.Resources.Folder_Bot);
			placeMark.Write(X_Manager.Properties.Resources.Placemarks_Stop_Top +
				"\r\n\t\t\t\t\t+<coordinates>" + lonS + latS + altS + "</coordinates>\r\n" + X_Manager.Properties.Resources.Placemarks_Stop_Bot);

			kml.Close();
			placeMark.Close();

			//Scrive l'header finale nel file kml string
			File.AppendAllText(kmlName + "_temp.kml", X_Manager.Properties.Resources.Path_Bot);
			File.AppendAllText(kmlName + "_temp.kml", X_Manager.Properties.Resources.Folder_Bot);

			//Accorpa kml placemark e string
			File.AppendAllText(kmlName + ".kml", System.IO.File.ReadAllText(kmlName + "_temp.kml"));

			//Chiude il kml placemark
			File.AppendAllText(kmlName + ".kml", X_Manager.Properties.Resources.Final_Bot);
			//Elimina il kml string temporaneo
			fDel(kmlName + "_temp.kml");

			kmlSemBack.Release();
			//Interlocked.Increment(ref conversionDone);
		}

		private List<byte> decodeTimeStamp(ref byte[] gp6, ref TimeStamp t, ref int pos)
		{

			//Cerca un timestamp o un nostamp
			List<byte> bufferNoStamp = new List<byte>();
			byte test = gp6[pos];
			int max = gp6.Length - 1;
			t.txtAllowed = 0;
			//if (pref_debugLevel > 0) t.txtAllowed++;
			while (true)
			{
				while ((test != 0xab) & (pos < max) && (test != 0xac) && (test != 0x0a))
				{
					pos++;
					test = gp6[pos];
				}
				//if (test == 0xff | test == 0x0a)
				if (test == 0x0a | pos == max)
				{
					List<byte> lout = new List<byte>();
					lout.Add(0xff);
					return lout;
				}
				if (test == 0xac)
				{
					//no stamp: 0xAC seguito da due byte di buffersize b e b byte di buffer
					int noSize = gp6[pos + 1] * 256 + gp6[pos + 2];
					pos += 3;
					bufferNoStamp.AddRange(gp6.Skip(pos).Take(noSize));
					pos += noSize;
					test = gp6[pos];
					continue;
				}
				else
				{
					break;
				}
			}
			t.pos = pos;
			pos++;
			t.tsType = gp6[pos];
			pos++;
			t.tsTypeExt1 = t.tsTypeExt2 = 0;
			if ((t.tsType & ts_ext1) == ts_ext1)
			{
				t.tsTypeExt1 = gp6[pos];
				pos++;
			}
			if ((t.tsTypeExt1 & ts_ext2) == ts_ext2)
			{
				t.tsTypeExt2 = gp6[pos];
				pos++;
			}

			//Inserire Pressione
			//Inserire Temperatura

			//Batteria
			if ((t.tsType & ts_battery) == ts_battery)
			{
				t.batteryLevel = gp6[pos] * 256;
				t.batteryLevel += gp6[pos + 1];
				t.batteryLevel = (t.batteryLevel * 6) / 4096; //Rimettere *6 dopo sviluppo
				pos += 2;
				if (pref_battery)
				{
					t.txtAllowed++;
				}
			}

			//Coordinata
			if ((t.tsType & ts_coordinate) == ts_coordinate)
			{
				int llon;
				llon = (gp6[pos] & 0x7f) << 24;
				llon += gp6[pos + 1] << 16;
				llon += gp6[pos + 2] << 8;
				llon += gp6[pos + 6] & 0xc0;
				if ((gp6[pos] & 0x80) == 0x80)
				{
					llon = -llon;
				}
				t.lon = llon / 10000000.0;

				int llat;
				llat = (gp6[pos + 3] & 0x3f) << 24;
				llat += gp6[pos + 4] << 16;
				llat += gp6[pos + 5] << 8;
				llat += (gp6[pos + 6] & 0x30) << 2;
				if ((gp6[pos + 3] & 0x40) == 0x40)
				{
					llat = -llat;
				}
				t.lat = llat / 10000000.0;

				t.altitude = (gp6[pos + 6] & 0x0f) << 8;
				t.altitude = t.altitude + gp6[pos + 7] - 191;

				t.hAcc = (gp6[pos + 8] & 0xe0) >> 5;
				t.vAcc = (gp6[pos + 8] & 0x1c) >> 2;

				t.speed = (gp6[pos + 8] & 3) << 6;
				t.speed += (gp6[pos + 9] & 0xfc) >> 2;
				t.speed *= 0.9;

				t.cog = (gp6[pos + 9] & 3) << 2;
				if ((gp6[pos + 10] & 0x80) == 0x80)
				{
					t.cog += 2;
				}
				if ((gp6[pos + 3] & 0x80) == 0x80)
				{
					t.cog += 1;
				}
				t.cog *= 23;
				t.cog += 11;

				t.GPS_second = gp6[pos + 10] & 0x3f;
				pos += 11;
				t.sat = 8;  //forzata per il kml
				t.txtAllowed++;
			}

			//Evento
			if ((t.tsType & ts_event) == ts_event)
			{
				t.eventAr = new byte[10];
				int evLength = gp6[pos] + 2;
				Array.Copy(gp6, pos, t.eventAr, 0, evLength);
				pos += evLength;
				if (pref_metadata)
				{
					t.txtAllowed++;
				}
			}

			//Inserire Flag Attività
			//Inserire Accelerometro

			if ((t.tsTypeExt1 == 0) & (t.tsTypeExt2 == 0))
			{
				return bufferNoStamp;
			}

			//Info
			if ((t.tsTypeExt1 & ts_info) == ts_info)
			{
				byte infoLength = gp6[pos];
				pos++;
				t.infoAr = new byte[infoLength];
				Array.Copy(gp6, pos, t.infoAr, 0, infoLength);
				if (infoLength > 3)
				{
					rfAddress = t.infoAr[3] * 65536 + t.infoAr[4] * 256 + t.infoAr[5];
					if (rfAddress != 0 && rfAddress != 0xffffff)
					{
						lastKnownRfAddressString = rfAddress.ToString();
					}
				}
				if (infoLength > 6)
				{
					byte[] nomeArr = new byte[28];
					Array.Copy(t.infoAr, 6, nomeArr, 0, 28);
					lastKnownUnitName = Encoding.ASCII.GetString(nomeArr);//
					lastKnownUnitName = lastKnownUnitName.Split('\0')[0];
				}
				t.unitNameTxt = lastKnownUnitName;
				t.rfAddressString = lastKnownRfAddressString;
				pos += infoLength;
			}

			//Inserire Magnetometro

			//Data e Ora
			if ((t.tsTypeExt1 & ts_time) == ts_time)
			{
				DateTime oldDate = t.dateTime;
				byte second = gp6[pos];
				int year, month, day, hour, minute;
				if (second > 0x7f)
				{
					second -= 0x80;
					year = gp6[pos + 6] * 256 + gp6[pos + 5];
					month = gp6[pos + 4];
					day = gp6[pos + 3];
					hour = gp6[pos + 2];
					minute = gp6[pos + 1];
					pos += 7;
				}
				else
				{
					year = t.dateTime.Year;
					month = t.dateTime.Month;
					day = t.dateTime.Day;
					hour = gp6[pos + 2];
					minute = gp6[pos + 1];
					//t.dateTime = new DateTime(t.dateTime.Year, t.dateTime.Month, t.dateTime.Day, hour, minute, second);
					pos += 3;
				}
				try
				{
					t.dateTime = new DateTime(year, month, day, hour, minute, second);
				}
				catch
				{
					t.dateTime = new DateTime(1000, 1, 1, 0, 0, 0);
				}

				if (t.dateTime < oldDate.AddHours(-1))
				{
					t.dateTime = t.dateTime.AddDays(1);
				}
			}
			else
			{
				t.dateTime.AddSeconds(1);
			}

			//Prossimità
			t.proximityAddress = 0;
			if ((t.tsTypeExt1 & ts_proximity) == ts_proximity)
			{
				int proxLength = gp6[pos];
				t.proximityAddress = gp6[pos + 1] * 65536 + gp6[pos + 2] * 256 + gp6[pos + 3];
				if (proxLength > 3)
				{
					t.proximityPower = (sbyte)gp6[pos + 4];
					pos += 1;
				}
				pos += 4;
				if (pref_proximity)
				{
					t.txtAllowed++;
				}
			}

			return bufferNoStamp;

		}

		private string decodeEvent(ref TimeStamp ts)
		{
			string outs;
			switch ((eventType)ts.eventAr[1])
			{
				case eventType.E_SCHEDULE:

					if (ts.eventAr[2] == 3)
					{
						outs = "GPS Schedule: OFF";
					}
					else
					{
						try
						{
							outs = String.Format(events[ts.eventAr[1]], ts.eventAr[3], scheduleEventTimings[ts.eventAr[2]]);
						}
						catch
						{
							outs = "B_EVENT";
							break;
						}
					}
					if ((ts.eventAr[2] != 3) && (ts.eventAr[0] == 3))
					{
						outs += " / Geofencing " + ts.eventAr[4].ToString();
					}
					break;
				case eventType.E_SD_START:
					outs = events[ts.eventAr[1]];
					break;
				case eventType.E_ALTON_TIMEOUT:
					outs = String.Format(events[ts.eventAr[1]], ts.eventAr[2]);
					break;
				default:
					outs = events[ts.eventAr[1]];
					break;
			}
			return outs;
		}

		//private bool detectEof(ref MemoryStream ard)
		//{
		//	if (ard.Position >= ard.Length) return true;
		//	else return false;
		//}

		//private double[] extractGroup(ref MemoryStream ard, ref TimeStamp tsc)
		//{
		//	byte[] group = new byte[2000];
		//	bool badGroup = false;
		//	long position = 0;
		//	int dummy, dummyExt;
		//	int badPosition = 600;

		//	if (ard.Position == ard.Length) return lastGroup;

		//	do
		//	{
		//		dummy = ard.ReadByte();
		//		if (dummy == 0xab)
		//		{
		//			if (ard.Position < ard.Length) dummyExt = ard.ReadByte();
		//			else return lastGroup;

		//			if (dummyExt == 0xab)
		//			{
		//				group[position] = (byte)0xab;
		//				position += 1;
		//				dummy = 0;
		//			}
		//			else
		//			{
		//				ard.Position -= 1;
		//				if (badGroup)
		//				{
		//					//System.IO.File.AppendAllText(((FileStream)ard.BaseStream).Name + "errorList.txt", "-> " + ard.BaseStream.Position.ToString("X8") + "\r\n");
		//				}
		//			}
		//		}
		//		else
		//		{
		//			if (position < badPosition)
		//			{
		//				group[position] = (byte)dummy;
		//				position++;
		//			}
		//			else if ((position == badPosition) && (!badGroup))
		//			{
		//				badGroup = true;
		//				//System.IO.File.AppendAllText(((FileStream)ard.BaseStream).Name + "errorList.txt", "-> " + ard.BaseStream.Position.ToString("X8") + "\r\n");
		//			}
		//		}
		//	} while ((dummy != (byte)0xab) && (ard.Position < ard.Length));

		//	//Implementare dati accelerometrici quando disponibili dall'unità
		//	//tsc.timeStampLength = (byte)(position);

		//	//int resultCode = 0;
		//	//double[] doubleResultArray = new double[nOutputs * 3];
		//	//if (bits)
		//	//{
		//	//	resultCode = resample4(group, (int)tsc.timeStampLength, doubleResultArray, nOutputs);
		//	//}
		//	//else
		//	//{
		//	//	resultCode = resample3(group, (int)tsc.timeStampLength, doubleResultArray, nOutputs);
		//	//}

		//	//return doubleResultArray;
		//	return new double[] { };
		//}

		public override void Dispose()
		{
			base.Dispose();
		}


	}

}
