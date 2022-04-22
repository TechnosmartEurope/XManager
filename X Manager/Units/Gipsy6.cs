
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using System.Globalization;
using System.IO.Ports;
using System.ComponentModel;
#if X64
using FT_HANDLE = System.UInt64;
#else
using FT_HANDLE = System.UInt32;
#endif

namespace X_Manager.Units
{
	class Gipsy6 : Units.Unit
	{

		FTDI_Device ft;
		byte[] buffIn;
		byte[] buffOut;
		struct TimeStamp
		{
			private int _pos;

			public int tsType;
			public int tsTypeExt1;
			public int tsTypeExt2;
			public int ore;
			//public int secondi;
			//public int minuti;
			//public int anno;
			//public int mese;
			//public int giorno;
			public double batteryLevel;
			public double temperature;
			public double press;
			public double pressOffset;
			public double altitude;
			//public int altSegno;
			//public int eo;
			//public int ns;
			//public int latGradi;
			//public int latMinuti;
			//public int latMinDecH;
			//public int latMinDecL;
			//public int latMinDecLL;
			//public int lonGradi;
			//public int lonMinuti;
			//public int lonMinDecH;
			//public int lonMinDecL;
			//public int lonMinDecLL;
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
				tout.pressOffset = this.pressOffset;
				tout.altitude = this.altitude;
				//tout.altSegno = this.altSegno;
				//tout.eo = this.eo;
				//tout.ns = this.ns;
				tout.lat = this.lat;
				tout.lon = this.lon;
				tout.speed = this.speed;
				tout.hAcc = this.hAcc;
				tout.vAcc = this.vAcc;
				tout.sat = this.sat;
				tout.gsvSum = this.gsvSum;
				tout.timeStampLength = this.timeStampLength;
				tout.dateTime = this.dateTime;
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
				tout.ADC = this.ADC;
				tout.GPS_second = this.GPS_second;
				tout.resetPos(this.pos);

				return tout;
			}
		}

		enum eventType : byte
		{
			E_POWER_ON = 0,
			E_SD_START,
			E_SD_STOP,
			E_ACQ_ON,
			E_ACQ_OFF,
			E_SCHEDULE,
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

		bool repeatEmptyValues = false;

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

		public Gipsy6(object p)
			: base(p)
		{
			modelCode = model_Gipsy6;
			useFtdi = true;
			configureMovementButtonEnabled = true;
			configurePositionButtonEnabled = false;
			buffIn = new byte[8192];
			buffOut = new byte[8192];
			if (sp.IsOpen) sp.Close();
			ft = new FTDI_Device(sp.PortName);
			ft.Open();
		}

		private bool ask(string command)
		{
			int tent = 0;
			bool res = false;
			ft.Open();
			ft.ReadTimeout = 30;
			ft.ReadExisting();
			int test = 0;
			bool goon = false;
			if (MainWindow.keepAliveTimer != null)
			{
				MainWindow.keepAliveTimer.Stop();
				MainWindow.keepAliveTimer.Start();
			}
			for (int y = 0; y < 4; y++) //(rimettere y < 4 dopo sviluppo
			{
				goon = false;
				ft.Write("TTTTTTGGA" + command);

				try
				{
					//test = sp.ReadByte();   //Byte di risposta per verifica correttezza comando
					test = ft.ReadByte();
				}
				catch
				{
					Thread.Sleep(500);
					//	sp.Write(new byte[] { 0 }, 0, 1);
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
			ft.ReadTimeout = 400;
			ft.Write("TTTTTGGAP");
			try
			{
				ft.ReadByte();
				Thread.Sleep(10);
				ft.ReadExisting();
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
			ft.ReadTimeout = oldTimeout;
		}

		public override void msBaudrate()
		{
			ft.BaudRate = 2000000;
		}
		public override void changeBaudrate(ref SerialPort sp, int maxMin)
		{

			uint oldBaudRate = ft.BaudRate;
			uint newBaudRate = 0;
			uint b;
			try
			{

				//sp.Write(new byte[] { 0x54, 0x54, 0x47, 0x47, 0x41, 0x62, 0x4b, 0x01 }, 0, 8);

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
				//sp.Close();
				ft.BaudRate = newBaudRate;
				//sp.Open();
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

		public override string askFirmware()
		{
			byte[] f = new byte[3];
			string firmware = "";

			if (!ask("F"))
			{
				throw new Exception(unitNotReady);
				return firmware;
			}
			ft.ReadTimeout = 400;
			int i = 0;
			try
			{
				for (i = 0; i < 3; i++)
				{
					f[i] = (byte)ft.ReadByte();
				}
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
			firmTotA = 0;
			for (i = 0; i < 3; i++)
			{
				firmTotA *= 1000;
				firmTotA += f[i];
			}
			for (i = 0; i <= (f.Length - 2); i++)
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

			if (!ask("N"))
			{
				throw new Exception(unitNotReady);
				return "[No Name]";
			}
			try
			{
				byte nIn = 255;
				for (int i = 0; i < 28; i++)
				{
					nIn = (byte)ft.ReadByte();
					if (nIn != 0)
					{
						name += Convert.ToChar(nIn).ToString();
					}
				}
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
			if (name == "") name = "[No Name]";
			return name;
		}

		public override string askBattery()
		{
			string battery = "";
			double battLevel;
			if (!ask("B"))
			{
				throw new Exception(unitNotReady);
			}
			try
			{
				ft.ReadTimeout = 500;
				battLevel = ft.ReadByte(); battLevel *= 256;
				battLevel += ft.ReadByte();
				battLevel *= 6;
				battLevel /= 4096;
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
			//battLevel = battLevel + (battLevel - 3) * .05 + .14;

			battery = Math.Round(battLevel, 2).ToString("0.00") + "V";
			return battery;
		}

		public override void setPcTime()
		{
			if (!ask("t"))
			{
				throw new Exception(unitNotReady);
			}
			try
			{
				byte[] dateAr = new byte[6];
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

		public override uint[] askMaxMemory()
		{
			UInt32 m;
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
			}
			catch
			{
				throw new Exception(unitNotReady);
			}

			return new uint[] { mem_min_physical_address, mem_max_physical_address };
		}

		public override uint[] askMemory()
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
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
			return new uint[] { mem_max_logical_address, mem_address };
		}

		public override void eraseMemory()
		{
			//sp.Write("TTTTTTTTGGAE");
			if (!ask("E"))
			{
				throw new Exception(unitNotReady);
			}
			try
			{
				ft.ReadTimeout = 500;
				ft.ReadByte();
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
		}

		public override void setName(string newName)
		{
			byte[] nameShort = Encoding.ASCII.GetBytes(newName);
			byte[] name = new byte[28];
			Array.Copy(nameShort, 0, name, 0, nameShort.Length);

			for (int k = 0; k < 4; k++)
			{
				if (!ask("n"))
				{
					throw new Exception(unitNotReady);
					return;
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
					return;
				}
				if (res == "I".ToArray()[0])
				{
					break;
				}
				Thread.Sleep(100);
			}
		}

		public override bool getRemote()
		{
			remote = false;
			if (!ask("l"))
			{
				throw new Exception(unitNotReady);
			}
			try
			{
				if (ft.ReadByte() == 1) remote = true;
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
			return remote;
		}

		public override byte[] getConf()
		{

			byte[] conf = new byte[0x1000];
			for (int j = 0; j < 2; j++)
			{
				if (!ask("C"))
				{
					throw new Exception(unitNotReady);
					return new byte[] { 0 };
				}
				try
				{
					ft.ReadTimeout = 400;
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

					break;
				}
				catch
				{
					Thread.Sleep(100);
					throw new Exception(unitNotReady);
				}
			}
			return conf;
		}

		public override void setConf(byte[] conf)
		{

			for (int j = 0; j < 3; j++)
			{
				if (!ask("c"))
				{
					throw new Exception(unitNotReady);
				}
				ft.ReadTimeout = 400;
				try
				{
					ft.ReadByte();
					for (uint i = 0; i < 17; i++)
					{
						ft.Write(conf, (i * 28) + 32, 28);
						ft.ReadByte();
					}

					ft.Write(conf, 480, 20);
					ft.ReadByte();

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

		public override void disconnect()
		{
			if (remote)
			{
				ask("O");
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
				return;
			}

			ft.ReadTimeout = 1600;

			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.progressBarStopButton.IsEnabled = true));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.progressBarStopButtonColumn.Width = new GridLength(80)));

			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.IsIndeterminate = false));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Minimum = 0));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Maximum = buffSize));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Value = buffPointer));

			address = BitConverter.GetBytes(mem_max_logical_address);
			Array.Reverse(address);
			Array.Copy(address, 0, outBuffer, 1, 3);
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
						var foC = new BinaryWriter(File.Open(fileNameMdp, System.IO.FileMode.Create));
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

			inBuffer[inBuffer.Length - 2] = 0x0a;
			inBuffer[inBuffer.Length - 1] = 0x00;

			var fo = new BinaryWriter(File.Open(fileNameMdp, System.IO.FileMode.Create));

			fo.Write(inBuffer, 0, inBuffer.Length);
			fo.Close();

			if (ok)
			{
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFinished()));
			}
		}

		public unsafe override void downloadRemote(string fileName, uint fromMemory, uint toMemory, int baudrate)
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
				return;
			}

			ft.ReadTimeout = 3200;

			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.progressBarStopButton.IsEnabled = true));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.progressBarStopButtonColumn.Width = new GridLength(80)));

			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.IsIndeterminate = false));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Minimum = 0));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Maximum = buffSize));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Value = buffPointer));

			address = BitConverter.GetBytes(mem_max_logical_address >> 8);
			Array.Copy(address, 0, outBuffer, 1, 3);
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

				Thread.Sleep(1);
				ft.Write(new byte[] { 66 }, 1);
				if (ft.Read(inBuffer, buffPointer, 0x200) < 0x200)
				{
					ok = false;
					if (buffPointer != 0)
					{
						var foC = new BinaryWriter(File.Open(fileNameMdp, System.IO.FileMode.Create));
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

			if (ok)
			{
				ft.Write("x");
				ft.ReadByte();
				Thread.Sleep(100);
			}
			inBuffer[inBuffer.Length - 2] = 0x0a;
			inBuffer[inBuffer.Length - 1] = 0x00;

			var fo = new BinaryWriter(File.Open(fileNameMdp, System.IO.FileMode.Create));

			fo.Write(inBuffer, 0, inBuffer.Length);
			fo.Close();

			if (ok)
			{
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFinished()));
			}
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
				if (X_Manager.Parent.getParameter("keepMdp").Equals("false"))
				{
					System.IO.File.Delete(fileNameMdp);
				}
				else
				{
					if (!Path.GetExtension(fileNameMdp).Contains("Dump"))
					{
						string newFileNameMdp = Path.GetDirectoryName(fileNameMdp) + "\\" + Path.GetFileNameWithoutExtension(fileNameMdp) + ".memDump";
						if (System.IO.File.Exists(newFileNameMdp)) System.IO.File.Delete(newFileNameMdp);
						//string newFileNameMdp = Path.GetFileNameWithoutExtension(fileNameMdp) + ".memDump";
						System.IO.File.Move(fileNameMdp, newFileNameMdp);
					}
				}
			}
			catch { }
			if (!fromDownload) Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.nextFile()));
		}

		public override void convert(string fileName, string[] prefs)
		{
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

			//Crea e avvia il thread per la scrittura del file txt
			string txtName = System.IO.Path.GetDirectoryName(fileName) + "\\" + System.IO.Path.GetFileNameWithoutExtension(fileName) + ".txt";
			List<TimeStamp> txtList = new List<TimeStamp>();
			txtSem = new Semaphore(0, 1);
			txtSemBack = new Semaphore(1, 1);
			txtBGW = new BackgroundWorker();
			txtBGW.DoWork += (s, args) =>
			{
				txtBGW_doWork(ref txtList, txtName);
			};
			txtBGW.RunWorkerAsync();

			//Crea e avvia il thread per la scrittura del file kml
			string kmlName = System.IO.Path.GetDirectoryName(fileName) + "\\" + System.IO.Path.GetFileNameWithoutExtension(fileName);
			List<TimeStamp> kmlList = new List<TimeStamp>();
			kmlSem = new Semaphore(0, 1);
			kmlSemBack = new Semaphore(1, 1);
			kmlBGW = new BackgroundWorker();
			kmlBGW.DoWork += (s, args) =>
			{
				kmlBGW_doWork(ref kmlList, kmlName);
			};
			kmlBGW.RunWorkerAsync();

			//Carica il file gp6 in memoria e lo chiude
			BinaryReader gp6File = new System.IO.BinaryReader(new FileStream(fileName, FileMode.Open));
			int filePointer = 0;
			byte[] gp6 = new byte[gp6File.BaseStream.Length - ((gp6File.BaseStream.Length / 512) + 1) * 2];
			//byte[] gp6 = new byte[(gp6File.BaseStream.Length / 512) * 510];
			for (int i = 0; i < (gp6File.BaseStream.Length / 0x200); i++)
			{
				gp6File.ReadBytes(2);
				gp6File.BaseStream.Read(gp6, filePointer, 510);
				filePointer += 510;
			}


			gp6File.Close();

			//Inizializza le variabili
			int pos = 0;
			int end = gp6.Length;
			TimeStamp timeStamp = new TimeStamp();
			List<byte> noStampBuffer = new List<byte>();

			int relTxt = 0;
			int relkml = 0;

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
			debugLevel = parent.stDebugLevel;

			int progressBarCounter = 0;
			//Cicla nel buffer decodificando i timestamp e aggiungendoli alla pila
			while (pos < end)
			{
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

				kmlSemBack.WaitOne();
				kmlList.Add(timeStamp.clone()); //aggiunge il timestamp alla pila kml
				kmlSem.Release();

				////Acquisisce l'acccesso alla pila txt
				//lock (txtList)
				//{
				//	txtList.Add(timeStamp.clone()); //aggiunge il timestamp alla pila txt
				//}
				//relTxt = txtSem.Release();   //sblocca il thread txt

				//Acquisisce l'acccesso alla pila kml
				//lock (kmlList)
				//{
				//	kmlList.Add(timeStamp.clone()); //aggiunge il timestamp alla pila kml
				//}
				//relkml = kmlSem.Release();   //sblocca il thread kml

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

			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.kmlProgressBar.Maximum = kmlList.Count));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.txtProgressBar.Maximum = txtList.Count));
			//Interlocked.Increment(ref lastTimestamp);   //Segnala ai thread che non saranno più aggiunti timestamp alle pile
			txtSemBack.WaitOne();
			kmlSemBack.WaitOne();
			txtSem.Release();   //rilascia il thread txt per l'ultima volta
			kmlSem.Release();   //rilascia il thread kml per l'ultima volta

			//while (Interlocked.Read(ref conversionDone) < 2)
			//{
			//	Thread.Sleep(200);
			//}
			//aspetta che i thread abbiano finito di scrivere i rispettivi file
			txtSemBack.WaitOne();
			kmlSemBack.WaitOne();

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
			//								data   ora    lon   lat   hAcc	alt	vAcc	speed	cog	eve   batt
			string[] tabs = new string[10];
			int contoStatus = 0;
			int pbmax = 0;
			var t = new TimeStamp();
			while (true)
			{
				//if (Interlocked.Read(ref lastTimestamp) == 0)   //Se il thread principale sta ancora aggiungendo timestamp alla pila
				//{                                               //aspetta che il thread principale abbia aggiunto un nuovo timestamp alla lista
				//	txtSem.WaitOne();
				//}
				txtSem.WaitOne();
				if (tL.Count == 0)  //Se non ci sono più timestamp nella pila, si esce dal loop
				{
					break;
				}
				t = tL[0];
				tL.RemoveAt(0);

				//lock (tL)
				//{
				//	if (tL.Count == 0)  //Se non ci sono più timestamp nella pila, si esce dal loop
				//	{
				//		break;
				//	}
				//	if (Interlocked.Read(ref lastTimestamp) > 0)
				//	{
				//		contoStatus++;
				//		if (contoStatus == 100)
				//		{
				//			contoStatus = 0;
				//			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.txtProgressBar.Value += 100));
				//		}
				//	}
				//	t = tL[0];
				//	tL.RemoveAt(0);

				//}

				//Si scrive il timestmap nel txt
				if (!repeatEmptyValues)
				{
					tabs = new string[12];
				}
				tabs[0] = t.dateTime.Day.ToString("00") + "/" + t.dateTime.Month.ToString("00") + "/" + t.dateTime.Year.ToString("0000");
				tabs[1] = t.dateTime.Hour.ToString("00") + ":" + t.dateTime.Minute.ToString("00") + ":" + t.dateTime.Second.ToString("00");

				if ((t.tsType & ts_battery) == ts_battery)
				{
					tabs[9] = t.batteryLevel.ToString("0.00") + "V";
				}
				if ((t.tsType & ts_coordinate) == ts_coordinate)
				{
					tabs[2] = t.lat.ToString("00.0000000", nfi);
					tabs[3] = t.lon.ToString("000.0000000", nfi);
					if (t.hAcc == 7)
					{
						tabs[4] = "> 100m";
					}
					else
					{
						tabs[4] = String.Format("< {0}m", accuracySteps[t.hAcc]);
					}

					tabs[5] = t.altitude.ToString();
					if (t.vAcc == 7)
					{
						tabs[6] = "> 100m";
					}
					else
					{
						tabs[6] = String.Format("< {0}m", accuracySteps[t.vAcc]);
					}
					tabs[7] = t.speed.ToString("0.0");
					tabs[8] = t.cog.ToString("0.0");
				}
				if ((t.tsType & ts_event) == ts_event)
				{
					tabs[10] = decodeEvent(ref t);
				}

				if (debugLevel == 3)
				{
					tabs[10] += " - " + t.pos.ToString("X8");
				}


				for (int i = 0; i < 11; i++)
				{
					txtBW.Write(tabs[i] + "\t");
				}
				txtBW.Write(tabs[11] + "\r\n");
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
			int contoStatus = 0;
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
				//lock (tL)
				//{
				//	if (tL.Count == 0)  //Se non ci sono più timestamp nella pila, esce dal loop
				//	{
				//		break;
				//	}
				//	if (Interlocked.Read(ref lastTimestamp) > 0)
				//	{
				//		contoStatus++;
				//		if (contoStatus == 100)
				//		{
				//			contoStatus = 0;
				//			//int cc = tL.Count;
				//			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.kmlProgressBar.Value += 100)); ;
				//		}
				//	}
				//	t = tL[0];
				//	tL.RemoveAt(0);
				//}

				if (t.sat > 0) //Si scrive il timestmap nel kml
				{
					if (contoCoord == 10000)
					{
						//kmlS += Properties.Resources.Path_Bot + Properties.Resources.Path_Top;
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

		private double[] extractGroup(ref MemoryStream ard, ref TimeStamp tsc)
		{
			byte[] group = new byte[2000];
			bool badGroup = false;
			long position = 0;
			int dummy, dummyExt;
			int badPosition = 600;

			if (ard.Position == ard.Length) return lastGroup;

			do
			{
				dummy = ard.ReadByte();
				if (dummy == 0xab)
				{
					if (ard.Position < ard.Length) dummyExt = ard.ReadByte();
					else return lastGroup;

					if (dummyExt == 0xab)
					{
						group[position] = (byte)0xab;
						position += 1;
						dummy = 0;
					}
					else
					{
						ard.Position -= 1;
						if (badGroup)
						{
							//System.IO.File.AppendAllText(((FileStream)ard.BaseStream).Name + "errorList.txt", "-> " + ard.BaseStream.Position.ToString("X8") + "\r\n");
						}
					}
				}
				else
				{
					if (position < badPosition)
					{
						group[position] = (byte)dummy;
						position++;
					}
					else if ((position == badPosition) && (!badGroup))
					{
						badGroup = true;
						//System.IO.File.AppendAllText(((FileStream)ard.BaseStream).Name + "errorList.txt", "-> " + ard.BaseStream.Position.ToString("X8") + "\r\n");
					}
				}
			} while ((dummy != (byte)0xab) && (ard.Position < ard.Length));

			//Implementare dati accelerometrici quando disponibili dall'unità
			//tsc.timeStampLength = (byte)(position);

			//int resultCode = 0;
			//double[] doubleResultArray = new double[nOutputs * 3];
			//if (bits)
			//{
			//	resultCode = resample4(group, (int)tsc.timeStampLength, doubleResultArray, nOutputs);
			//}
			//else
			//{
			//	resultCode = resample3(group, (int)tsc.timeStampLength, doubleResultArray, nOutputs);
			//}

			//return doubleResultArray;
			return new double[] { };
		}

		private List<byte> decodeTimeStamp(ref byte[] gp6, ref TimeStamp t, ref int pos)
		{

			//Cerca un timestamp o un nostamp
			List<byte> bufferNoStamp = new List<byte>();
			byte test = gp6[pos];
			int max = gp6.Length - 1;
			t.txtAllowed = 0;
			if (debugLevel > 0) t.txtAllowed++;
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
				if ((gp6[pos] & 0x40) == 0x40)
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

				t.cog = (gp6[pos + 9 & 3]) << 2;
				if ((gp6[pos + 10] & 0x80) == 0x80)
				{
					t.cog += 2;
				}
				if ((gp6[pos + 3] & 0x80) == 0x80)
				{
					t.cog += 1;
				}

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
			}

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
				pos += infoLength;
			}

			//Data e Ora
			if ((t.tsTypeExt1 & ts_time) == ts_time)
			{
				DateTime oldDate = t.dateTime;
				byte second = gp6[pos];
				if (second > 0x7f)
				{
					second -= 0x80;
					int year = gp6[pos + 6] * 256 + gp6[pos + 5];
					int month = gp6[pos + 4];
					int day = gp6[pos + 3];
					int hour = gp6[pos + 2];
					int minute = gp6[pos + 1];
					t.dateTime = new DateTime(year, month, day, hour, minute, second);
					pos += 7;
				}
				else
				{
					int hour = gp6[pos + 2];
					int minute = gp6[pos + 1];
					t.dateTime = new DateTime(t.dateTime.Year, t.dateTime.Month, t.dateTime.Day, hour, minute, second);
					pos += 3;
				}
				if (t.dateTime < oldDate)
				{
					t.dateTime = t.dateTime.AddDays(1);
				}
			}
			else
			{
				t.dateTime.AddSeconds(1);
			}

			return bufferNoStamp;

		}

		private string decodeEvent(ref TimeStamp ts)
		{
			string outs;
			switch ((eventType)ts.eventAr[1])
			{
				case eventType.E_SCHEDULE:

					outs = String.Format(events[ts.eventAr[1]], ts.eventAr[3], scheduleEventTimings[ts.eventAr[2]]);
					if (ts.eventAr[0] == 3)
					{
						outs += " / Geofencing " + ts.eventAr[4].ToString();
					}
					break;
				case eventType.E_SD_START:
					outs = events[ts.eventAr[1]];
					break;
				default:
					outs = events[ts.eventAr[1]];
					break;
			}
			return outs;
		}

		private bool detectEof(ref MemoryStream ard)
		{
			if (ard.Position >= ard.Length) return true;
			else return false;
		}

		public override void Dispose()
		{
			if (ft.IsOpen)
			{
				ft.Close();
			}
			base.Dispose();
		}


	}

}
