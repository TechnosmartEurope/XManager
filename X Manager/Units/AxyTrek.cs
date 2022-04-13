using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
//using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.IO;
//using FTD2XX_NET;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Runtime.Versioning;
using System.Windows.Annotations;
//using System.Diagnostics;
#if X64
using FT_HANDLE = System.UInt64;
#else
using FT_HANDLE = System.UInt32;
#endif

namespace X_Manager
{
	class AxyTrek : Units.Unit
	{
		ushort[] coeffs = new ushort[7];
		double[] convCoeffs = new double[7];
		bool evitaSoglie = false;
		bool disposed = false;
		new byte[] firmwareArray = new byte[6];
		struct timeStamp
		{
			public int tsType;
			public int tsTypeExt1;
			public int tsTypeExt2;
			public int ore;
			public int secondi;
			public int minuti;
			public int anno;
			public int mese;
			public int giorno;
			public double batteryLevel;
			public double temperature;
			public double press;
			public double pressOffset;
			public int altL;
			public int altH;
			public int altSegno;
			public int eo;
			public int ns;
			public int latGradi;
			public int latMinuti;
			public int latMinDecH;
			public int latMinDecL;
			public int latMinDecLL;
			public int lonGradi;
			public int lonMinuti;
			public int lonMinDecH;
			public int lonMinDecL;
			public int lonMinDecLL;
			public int DOP;
			public int DOPdec;
			public int nSat;
			public int vel;
			public int timeStampLength;
			public DateTime orario;
			public int gsvSum;
			public byte[] eventAr;
			//public bool isEvent;
			public int stopEvent;
			public int inWater;
			public int inAdc;
			public int ADC;
			public int slowData;
			public long ardPosition;
		}

		public struct coordKml
		{
			public string cSstring;
			public string cPlacemark;
			public string cName;
			public string cClass;
		}

		bool adcLog = false;
		bool adcStop = false;
		ushort adcThreshold = 0;
		//bool adcMagmin = false;
		uint contoCoord;

		bool addGpsTime;
		//uint sogliaNeg;
		//uint rendiNeg;
		double gCoeff;
		bool angloTime = false;
		bool bits;
		byte bitsDiv;
		string dateFormatParameter;
		byte dateFormat;
		//byte timeFormat;
		bool inMeters = false;
		bool prefBattery = false;
		bool repeatEmptyValues = true;
		int isDepth = 1;
		byte range;
		ushort rate;
		bool primaCoordinata;
		bool sameColumn = false;
		int temperatureEnabled;
		int pressureEnabled;
		ushort addMilli;
		int debugStampId = 13;
		int debugStampLenght = 15;
		//byte cifreDec;
		string cifreDecString;
		//ushort groupCountOriginal = 0;
		//uint fixSequenceNumber = 0;
		//uint positionInFile = 0;
		bool metadata;
		bool oldUnitDebug = false;
		int leapSeconds;
		long infRemPosition;

		DateTime nullDate = new DateTime(1970, 1, 1, 0, 0, 0);
		DateTime recoveryDate = new DateTime(1970, 1, 1, 0, 0, 0);

		public AxyTrek(object p)
			: base(p)
		{
			positionCanSend = true;
			configurePositionButtonEnabled = true;
			modelCode = model_axyTrek;
			modelName = "Axy-Trek";
		}

		public override string askFirmware()
		{
			byte[] f = new byte[6];
			bool firmValid = false;
			int tentativi = 0;
			string firmware = "";
			int oldTimeOut = sp.ReadTimeout;

			sp.ReadTimeout = 50;
			while ((!firmValid) && (tentativi < 5))
			{
				sp.ReadExisting();
				sp.Write("TTTTTTTGGAF");
				sp.ReadTimeout = 400;
				Thread.Sleep(600);
				f[0] = f[1] = f[2] = f[3] = f[4] = f[5] = 0xff;
				try
				{
					for (int i = 0; i < 6; i++)
					{
						f[i] = (byte)sp.ReadByte();
					}
				}
				catch { Thread.Sleep(1200); }
				tentativi += 1;
				firmValid = true;
				foreach (byte b in f)
				{
					if (b == 0xff)
					{
						firmValid = false;
						Thread.Sleep(400);
						break;
					}
				}
			}

			if (firmValid)
			{
				firmTotA = f[0] * (uint)1000000 + f[1] * (uint)1000 + f[2];
				firmTotB = f[3] * (uint)1000000 + f[4] * (uint)1000 + f[5];
				firmware = "a" + f[0].ToString() + "." + f[1].ToString() + "." + f[2].ToString();
				firmware += "b" + f[3].ToString() + "." + f[4].ToString() + "." + f[5].ToString();
				firmwareArray = f;
			}
			else
			{
				throw new Exception(unitNotReady);
			}
			sp.ReadTimeout = oldTimeOut;
			return firmware;
		}

		public override string askBattery()
		{
			string battery = "";
			double battLevel;
			sp.Write("TTTTGGAB");
			try
			{
				battLevel = sp.ReadByte(); battLevel *= 256;
				battLevel += sp.ReadByte();
				battLevel *= 6;
				battLevel /= 4096;
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
			battery = battLevel.ToString("0.00") + "V";
			return battery;
		}

		public override string askName()
		{
			string unitNameBack;
			try
			{
				sp.Write("TTTTTTTGGAN");
				unitNameBack = sp.ReadLine();
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
			name = formatName(unitNameBack);
			return name;
		}

		public override uint[] askMaxMemory()
		{
			UInt32 m;
			sp.Write("TTTTTTTGGAm");
			try
			{
				m = (UInt32)sp.ReadByte(); m *= 256;
				m += (UInt32)sp.ReadByte(); m *= 256;
				m += (UInt32)sp.ReadByte(); m *= 256;
				m += (UInt32)sp.ReadByte();
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
			mem_max_physical_address = m;
			mem_min_physical_address = 0;
			return new uint[] { mem_min_physical_address, mem_max_physical_address };
		}

		public override uint[] askMemory()
		{
			UInt32 m;
			sp.Write("TTTTTTTGGAM");
			try
			{
				m = (UInt32)sp.ReadByte(); m *= 256;
				m += (UInt32)sp.ReadByte(); m *= 256;
				m += (UInt32)sp.ReadByte(); m *= 256;
				m += (UInt32)sp.ReadByte();
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
			mem_address = m;
			mem_max_logical_address = 0;
			return new uint[] { mem_max_logical_address, mem_address };
		}

		public override void eraseMemory()
		{
			sp.Write("TTTTTTTTGGAE");
			try
			{
				sp.ReadByte();
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
		}

		public override void getCoeffs()
		{
			if (firmTotA <= 1000009)
			{
				evitaSoglie = true;
				return;
			}

			coeffs[0] = 0;
			sp.Write("TTTTTTTTTTTTTGGAg");
			try
			{
				coeffs[1] = (ushort)(sp.ReadByte() * 256 + sp.ReadByte());
				coeffs[2] = (ushort)(sp.ReadByte() * 256 + sp.ReadByte());
				coeffs[3] = (ushort)(sp.ReadByte() * 256 + sp.ReadByte());
				coeffs[4] = (ushort)(sp.ReadByte() * 256 + sp.ReadByte());
				coeffs[5] = (ushort)(sp.ReadByte() * 256 + sp.ReadByte());
				coeffs[6] = (ushort)(sp.ReadByte() * 256 + sp.ReadByte());
			}
			catch
			{
				throw new Exception(unitNotReady);
			}

			int sommaSoglie = coeffs[1] + coeffs[2] + coeffs[3];
			if ((sommaSoglie == 0) | (sommaSoglie == 0x2fd))
			{
				evitaSoglie = true;
			}
		}

		public override byte[] getConf()
		{
			byte[] conf = new byte[29];
			conf[25] = modelCode;
			sp.Write("TTTTTTTTGGAC");
			try
			{
				for (int i = 2; i <= 4; i++)
				{
					conf[i] = (byte)sp.ReadByte();
				}
				for (int i = 15; i <= 21; i++)
				{
					conf[i] = (byte)sp.ReadByte();
				}
				conf[22] = (byte)sp.ReadByte();
				if (firmTotA > 2000000)
				{
					conf[23] = (byte)sp.ReadByte();
				}
				if (firmTotA >= 3008000)
				{
					for (int i = 25; i < 29; i++)
					{
						conf[i] = (byte)sp.ReadByte();
					}

				}
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
			return conf;
		}

		public override void setConf(byte[] conf)
		{
			sp.Write("TTTTTTTTTGGAc");
			try
			{
				sp.ReadByte();
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
			sp.Write(conf, 2, 3);
			sp.Write(conf, 15, 7);
			sp.Write(conf, 22, 1);
			if (firmTotA > 2000000)
			{
				sp.Write(conf, 23, 1);
			}
			if (firmTotA >= 3008000)
			{
				sp.Write(conf, 25, 4);
			}
			try
			{
				sp.ReadByte();
				sp.ReadExisting();
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
			if (!evitaSoglie)
			{
				uint[] soglie = calcolaSoglieDepth();
				sp.Write("TTTTTTTTTTGGAG");
				try
				{
					sp.ReadByte();
				}
				catch
				{
					throw new Exception(unitNotReady);
				}
				for (int s = 0; s <= 16; s++)
				{
					sp.Write(new byte[] { (BitConverter.GetBytes(soglie[s])[2]) }, 0, 1);
					sp.Write(new byte[] { (BitConverter.GetBytes(soglie[s])[1]) }, 0, 1);
					sp.Write(new byte[] { (BitConverter.GetBytes(soglie[s])[0]) }, 0, 1);
				}
			}
		}

		public override bool getRemote()
		{
			if (firmTotA > 3001000)
			{
				sp.Write("TTTTTTTTTTGGAl");
				try
				{
					if (sp.ReadByte() == 1) remote = true;
				}
				catch { throw new Exception(unitNotReady); }
			}
			return remote;
		}

		public override bool isSolar()
		{
			if (firmTotA >= 3008001)
			{
				sp.Write("TTTTTTTTTTGGAi");
				try
				{
					if (sp.ReadByte() == 1) solar = true;
				}
				catch { throw new Exception(unitNotReady); }
			}
			return remote;
		}

		public override void abortConf()
		{

		}

		public override byte[] getGpsSchedule()
		{
			byte[] schedule = new byte[200];
			sp.Write("TTTTTTTTTTTTTGGAS");
			Thread.Sleep(200);
			try
			{
				for (int i = 0; i <= 63; i++) { schedule[i] = (byte)sp.ReadByte(); }
				if (remote) sp.Write(new byte[] { 2 }, 0, 1);

				for (int i = 64; i <= 127; i++) { schedule[i] = (byte)sp.ReadByte(); }
				if (remote) sp.Write(new byte[] { 2 }, 0, 1);

				for (int i = 128; i <= 171; i++) { schedule[i] = (byte)sp.ReadByte(); }
				if (firmTotB > 3003999)
				{
					for (int i = 172; i <= 178; i++) { schedule[i] = (byte)sp.ReadByte(); }
				}
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
			return schedule;

		}

		public override void setGpsSchedule(byte[] schedule)
		{
			sp.Write("TTTTTTTTTTTTTGGAs");
			Thread.Sleep(200);
			try
			{
				sp.ReadByte();
				sp.Write(schedule, 0, 64);
				if (remote) sp.ReadByte();
				sp.Write(schedule, 64, 64);
				if (remote) sp.ReadByte();
				if (firmTotB < 3004000)
				{
					sp.Write(schedule, 128, 44);
				}
				else
				{
					sp.Write(schedule, 128, 51);
				}

				Thread.Sleep(200);
				sp.ReadByte();
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
		}

		public override void setName(string newName)
		{
			if (newName.Length < 10)
			{
				for (int i = newName.Length; i < 10; i++) newName += " ";
				sp.Write("TTTTTTTGGAn");
				try
				{
					sp.ReadByte();
				}
				catch
				{
					throw new Exception(unitNotReady);
				}
				sp.WriteLine(newName);
			}
		}

		public override void disconnect()
		{
			base.disconnect();
			if (!(bool)sp.IsOpen)
			{
				sp.Open();
			}
			sp.Write("TTTTTTTGGAO");
		}

		private uint[] calcolaSoglieDepth()
		{
			uint[] soglie = new uint[18];
			double[] temps = new double[] { -15, -10, 0, 10, 20, 30, 40, 50 };
			double[] tempsInt = new double[] { -18, -12, -5, 5, 15, 25, 35, 45, 55 };

			for (int i = 0; i <= 7; i++) soglie[i] = (uint)guessD2(temps[i]);

			for (int i = 8; i <= 16; i++) soglie[i] = guessD1(guessD2(tempsInt[i - 8]), 1520);

			soglie[17] = 1;
			return soglie;
		}

		private double guessD2(double t)
		{
			double max = 16777215;
			double min = 0;
			double d2 = (max - min) / 2;
			double temp;

			while (Math.Abs(max - min) > 4)
			{
				temp = pTemp(d2);
				if (temp > t) max = d2;
				else min = d2;
				d2 = ((max - min) / 2) + min;
			}
			return d2;
		}

		private uint guessD1(double d2, double p)
		{
			double max = 16777215;
			double min = 0;
			double d1 = (max - min) / 2;
			double press;

			while (Math.Abs(max - min) > 4)
			{
				press = pDepth(d1, d2);
				if (press > p) max = d1;
				else min = d1;
				d1 = ((max - min) / 2) + min;
			}
			return (uint)d1;
		}

		private double pDepth(double d1, double d2)
		{
			double dT;
			double off;
			double sens;
			double temp;
			double[] c = new double[7];

			for (int count = 1; count <= 6; count++)
			{
				c[count - 1] = (double)coeffs[count];
			}
			dT = d2 - c[4] * 256;
			temp = 2000 + (dT * c[5]) / 8388608;
			off = c[1] * 65536 + (c[3] * dT) / 128;
			sens = c[0] * 32768 + (c[2] * dT) / 256;
			if (temp > 2000)
			{
				temp -= ((2 * Math.Pow(dT, 2)) / 137438953472);
				off -= ((Math.Pow((temp - 2000), 2)) / 16);
			}
			else
			{
				off -= 3 * ((Math.Pow((temp - 2000), 2)) / 2);
				sens -= 5 * ((Math.Pow((temp - 2000), 2)) / 8);
				if (temp < -1500)
				{
					off -= 7 * Math.Pow((temp + 1500), 2);
					sens -= 4 * Math.Pow((temp + 1500), 2);
				}
				temp -= (3 * (Math.Pow(dT, 2))) / 8589934592;
			}
			sens = d1 * sens / 2097152;
			sens -= off;
			sens /= 81920;
			return sens;
		}

		private double pTemp(double d2)
		{
			double ti;
			double dt;
			double temp;
			double[] c = new double[7];

			for (int count = 1; count <= 6; count++)
			{
				c[count] = (double)coeffs[count];
			}
			dt = d2 - c[5] * 256;
			temp = 2000 + ((dt * c[6]) / 8388608);
			if ((temp / 100) < 20) ti = 3 * (Math.Pow(dt, 2)) / 8388608;
			else ti = 2 * (Math.Pow(dt, 2)) / 137438953472;
			temp = (temp - ti) / 100;
			return temp;
		}

		public unsafe override void download(string fileName, uint fromMemory, uint toMemory, int baudrate)
		{
			convertStop = false;
			uint actMemory = fromMemory;
			System.IO.FileMode fm = FileMode.Create;
			if (fromMemory != 0) fm = FileMode.Append;
			string fileNameMdp = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + ".mdp";
			var fo = new BinaryWriter(File.Open(fileNameMdp, fm));

			string command = "e";
			if (firmTotA < 1000007) command = "D";

			sp.Write("TTTTTTTTTTTTGGA" + command);
			try
			{
				sp.ReadByte();
			}
			catch
			{
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFailed()));
				try
				{
					fo.Close();
				}
				catch { }
				return;
			}

			Thread.Sleep(50);
			if (command == "D") sp.BaudRate = MainWindow.Baudrate_3M;
			else
			{
				byte b = (byte)(baudrate / 1000000);
				sp.Write(new byte[] { b }, 0, 1);
				Thread.Sleep(100);
				sp.BaudRate = baudrate;
				Thread.Sleep(100);
			}

			Thread.Sleep(200);
			sp.Write("S");
			Thread.Sleep(1000);
			int dieCount = sp.ReadByte();
			if (dieCount == 0x53) dieCount = 2;     //S
			if (dieCount == 0x73) dieCount = 1;     //s
			if ((dieCount != 1) & (dieCount != 2))
			{
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFailed()));
				try
				{
					fo.Close();
				}
				catch { }
				return;
			}


			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.progressBarStopButton.IsEnabled = true));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.progressBarStopButtonColumn.Width = new GridLength(80)));

			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.IsIndeterminate = false));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Minimum = 0));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Maximum = toMemory));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Value = fromMemory));

			//Passa alla gestione FTDI D2XX
			sp.Close();

			MainWindow.FT_STATUS FT_Status;
			FT_HANDLE FT_Handle = 0;
			byte[] outBuffer = new byte[50];
			byte[] inBuffer = new byte[4096];
			byte[] tempBuffer = new byte[2048];
			byte[] address = new byte[8];

			uint bytesToWrite = 0, bytesWritten = 0, bytesReturned = 0;

			FT_Status = MainWindow.FT_OpenEx(parent.ftdiSerialNumber, (UInt32)1, ref FT_Handle);
			if (FT_Status != MainWindow.FT_STATUS.FT_OK)
			{
				MainWindow.FT_Close(FT_Handle);
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFailed()));
				try
				{
					fo.Close();
				}
				catch { }
				return;
			}

			MainWindow.FT_SetLatencyTimer(FT_Handle, (byte)1);
			MainWindow.FT_SetTimeouts(FT_Handle, (uint)1000, (uint)1000);

			bool firstLoop = true;
			bool mem4 = false;
			if (firmTotA > 2999999) mem4 = true;

			while (actMemory < toMemory)
			{
				if (((actMemory % 0x2000000) == 0) | (firstLoop))
				{
					address = BitConverter.GetBytes(actMemory);
					Array.Reverse(address);
					Array.Copy(address, 0, outBuffer, 1, 3);
					outBuffer[0] = 65;  //A
					bytesToWrite = 4;
					firstLoop = false;
				}
				else
				{
					outBuffer[0] = 79;
					bytesToWrite = 1;
				}
				fixed (byte* outP = outBuffer, inP = inBuffer)
				{
					FT_Status = MainWindow.FT_Write(FT_Handle, outP, bytesToWrite, ref bytesWritten);
					FT_Status = MainWindow.FT_Read(FT_Handle, inP, (uint)4096, ref bytesReturned);
				}

				if (FT_Status != MainWindow.FT_STATUS.FT_OK)
				{
					outBuffer[0] = 88;
					fixed (byte* outP = outBuffer) { FT_Status = MainWindow.FT_Write(FT_Handle, outP, bytesToWrite, ref bytesWritten); }
					MainWindow.FT_Close(FT_Handle);
					Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFailed()));
					fo.Write(inBuffer);
					fo.Close();
					return;
				}
				else if (bytesReturned != 4096)
				{
					firstLoop = true;
				}
				else
				{
					actMemory += 4096;
					if (mem4 && ((actMemory % 0x20000) == 0))
					{

						if (dieCount == 2)
						{
							actMemory -= 4096;
							for (int i = 0; i < 2; i++)
							{
								address = BitConverter.GetBytes(actMemory);
								Array.Reverse(address);
								Array.Copy(address, 0, outBuffer, 1, 3);
								outBuffer[0] = 97;
								bytesToWrite = 4;
								fixed (byte* outP = outBuffer, inP = inBuffer)
								{
									FT_Status = MainWindow.FT_Write(FT_Handle, outP, bytesToWrite, ref bytesWritten);
									FT_Status = MainWindow.FT_Read(FT_Handle, inP, (uint)2048, ref bytesReturned);
								}
								fo.Write(inBuffer, 0, 2048);
								actMemory += 2048;
							}
							firstLoop = true;
						}
						else
						{
							fo.Write(inBuffer, 0, 4096);
							if ((actMemory % 0x40000) == 0)
							{
								firstLoop = true;
							}
							//	address = BitConverter.GetBytes(actMemory);
							//	Array.Reverse(address);
							//	Array.Copy(address, 0, outBuffer, 1, 3);
							//	outBuffer[0] = 97;
							//	bytesToWrite = 4;
							//	fixed (byte* outP = outBuffer, inP = inBuffer)
							//	{
							//		FT_Status = MainWindow.FT_Write(FT_Handle, outP, bytesToWrite, ref bytesWritten);
							//		FT_Status = MainWindow.FT_Read(FT_Handle, inP, (uint)4096, ref bytesReturned);
							//	}
							//	fo.Write(inBuffer, 0, 4096);
							//	actMemory += 4096;
						}

					}
					else
					{
						fo.Write(inBuffer, 0, 4096);
					}
				}

				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Value = actMemory));

				if (convertStop) actMemory = toMemory;
			}

			fo.Write(firmwareArray, 0, 6);
			fo.Write(new byte[] { model_axyTrek, (byte)254 }, 0, 2);

			fo.Close();
			outBuffer[0] = 88;
			bytesToWrite = 1;
			fixed (byte* outP = outBuffer)
			{
				FT_Status = MainWindow.FT_Write(FT_Handle, outP, bytesToWrite, ref bytesWritten);
			}
			MainWindow.FT_Close(FT_Handle);
			sp.BaudRate = 115200;
			sp.Open();
			if (!convertStop) extractArds(fileNameMdp, fileName, true);
			else
			{
				if (Parent.getParameter("keepMdp").Equals("false"))
				{
					try
					{
						fDel(fileNameMdp);
					}
					catch { }
				}
			}
			Thread.Sleep(300);
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFinished()));
		}

		public unsafe override void downloadRemote(string fileName, uint fromMemory, uint toMemory, int baudrate)
		{
			convertStop = false;
			uint actMemory = fromMemory;
			System.IO.FileMode fm = System.IO.FileMode.Create;
			if (fromMemory != 0) fm = System.IO.FileMode.Append;
			string fileNameMdp = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + ".mdp";
			var fo = new BinaryWriter(File.Open(fileNameMdp, fm));

			byte mdrSpeed = 9;
			mdrSpeed = 8;
			string br = "D";
			if (mdrSpeed == 9) br = "H";
			sp.Write("TTTTTTTTTTTTTTGGA" + br);
			int dieCount = 0;
			try
			{
				sp.ReadByte();
				Thread.Sleep(100);
				sp.Write("+++");
				Thread.Sleep(200);
				sp.Write(new byte[] { 0x41, 0x54, 0x42, 0x52, mdrSpeed }, 0, 5);
				Thread.Sleep(200);
				if (mdrSpeed == 9) sp.BaudRate = 2000000;
				else sp.BaudRate = 1500000;
				Thread.Sleep(300);
				sp.Write("ATX");
				Thread.Sleep(900);
				sp.Write("R");
				//Thread.Sleep(100);
				dieCount = sp.ReadByte();
				sp.ReadExisting();
				if (dieCount == 0x52) dieCount = 2;
				if (dieCount == 0x72) dieCount = 1;
				if ((dieCount != 1) & (dieCount != 2))
				{
					throw new Exception(unitNotReady);
				}
			}
			catch
			{
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFailed()));
				try
				{
					sp.ReadByte();
					fo.Close();
				}
				catch { }
				return;
			}


			Thread.Sleep(50);

			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.progressBarStopButton.IsEnabled = true));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.progressBarStopButtonColumn.Width = new GridLength(80)));

			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.IsIndeterminate = false));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Minimum = 0));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Maximum = toMemory));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Value = fromMemory));

			//Passa alla gestione FTDI D2XX
			sp.Close();

			MainWindow.FT_STATUS FT_Status;
			FT_HANDLE FT_Handle = 0;
			byte[] outBuffer = new byte[50];
			byte[] inBuffer = new byte[4096];
			byte[] tempBuffer = new byte[2048];
			byte[] address = new byte[8];

			uint bytesToWrite = 0, bytesWritten = 0, bytesReturned = 0;

			FT_Status = MainWindow.FT_OpenEx(parent.ftdiSerialNumber, (UInt32)1, ref FT_Handle);
			if (FT_Status != MainWindow.FT_STATUS.FT_OK)
			{
				MainWindow.FT_Close(FT_Handle);
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFailed()));
				try
				{
					fo.Close();
				}
				catch { }
				return;
			}

			MainWindow.FT_SetLatencyTimer(FT_Handle, (byte)1);
			MainWindow.FT_SetTimeouts(FT_Handle, (uint)1000, (uint)1000);

			bool firstLoop = true;
			bool mem4 = false;
			if (firmTotA > 2999999) mem4 = true;

			while (actMemory < toMemory)
			{
				if (((actMemory % 0x2000000) == 0) | (firstLoop))
				{
					address = BitConverter.GetBytes(actMemory);
					Array.Reverse(address);
					Array.Copy(address, 0, outBuffer, 1, 3);
					outBuffer[0] = 65;
					bytesToWrite = 4;
					firstLoop = false;
				}
				else
				{
					outBuffer[0] = 79;
					bytesToWrite = 1;
				}
				fixed (byte* outP = outBuffer, inP = inBuffer)
				{
					FT_Status = MainWindow.FT_Write(FT_Handle, outP, bytesToWrite, ref bytesWritten);
					FT_Status = MainWindow.FT_Read(FT_Handle, inP, 4096, ref bytesReturned);
				}

				if (FT_Status != MainWindow.FT_STATUS.FT_OK)
				{
					outBuffer[0] = 88;
					fixed (byte* outP = outBuffer) { FT_Status = MainWindow.FT_Write(FT_Handle, outP, bytesToWrite, ref bytesWritten); }
					MainWindow.FT_Close(FT_Handle);
					Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFailed()));
					fo.Write(inBuffer);
					fo.Close();
					return;
				}
				else if (bytesReturned != 4096)
				{
					firstLoop = true;
				}
				else
				{
					actMemory += 4096;
					if (mem4 && ((actMemory % 0x20000) == 0))
					{
						if (dieCount == 2)
						{
							actMemory -= 4096;
							for (int i = 0; i < 2; i++)
							{
								address = BitConverter.GetBytes(actMemory);
								Array.Reverse(address);
								Array.Copy(address, 0, outBuffer, 1, 3);
								outBuffer[0] = 97;
								bytesToWrite = 4;
								fixed (byte* outP = outBuffer, inP = inBuffer)
								{
									FT_Status = MainWindow.FT_Write(FT_Handle, outP, bytesToWrite, ref bytesWritten);
									FT_Status = MainWindow.FT_Read(FT_Handle, inP, (uint)2048, ref bytesReturned);
								}
								fo.Write(inBuffer, 0, 2048);
								actMemory += 2048;
							}
							firstLoop = true;
						}
						else
						{
							fo.Write(inBuffer, 0, 4096);
							if ((actMemory % 0x40000) == 0)
							{
								firstLoop = true;
							}
						}
					}
					else
					{
						fo.Write(inBuffer, 0, 4096);
					}
				}

				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Value = actMemory));

				if (convertStop) actMemory = toMemory;
			}

			fo.Write(firmwareArray, 0, 6);
			fo.Write(new byte[] { model_axyTrek, (byte)254 }, 0, 2);

			fo.Close();
			outBuffer[0] = 88;
			bytesToWrite = 1;
			fixed (byte* outP = outBuffer)
			{
				FT_Status = MainWindow.FT_Write(FT_Handle, outP, bytesToWrite, ref bytesWritten);
			}
			MainWindow.FT_Close(FT_Handle);

			sp.Open();
			Thread.Sleep(50);
			sp.Write("+++");
			Thread.Sleep(200);
			sp.Write(new byte[] { 0x41, 0x54, 0x42, 0x52, 3 }, 0, 5);
			sp.BaudRate = 115200;
			Thread.Sleep(100);
			sp.Write("ATX");

			sp.BaudRate = 115200;

			if (!convertStop)
			{
				extractArds(fileNameMdp, fileName, true);
			}
			else
			{
				if (Parent.getParameter("keepMdp").Equals("false"))
				{
					try
					{
						fDel(fileNameMdp);
					}
					catch { }
				}
			}

			Thread.Sleep(600);
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFinished()));
		}

		public override void extractArds(string fileNameMdp, string fileName, bool fromDownload)
		{
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusLabel.Content = "Creating Ard file(s)..."));
			var mdp = new BinaryReader(File.Open(fileNameMdp, FileMode.Open));

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
					if (File.Exists(fileNameArd))
					{
						if (resp < 11)
						{
							var yn = new YesNo(fileNameArd + " already exists. Do you want to overwrite it?", "FILE EXISTING", "Remeber my choice");
							resp = yn.ShowDialog();
						}
						if ((resp == yes) | (resp == yes_alaways))
						{
							fDel(fileNameArd);
						}
						else
						{
							do
							{
								fileNameArd = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileNameArd) + " (1)" + ".ard";
							} while (System.IO.File.Exists(fileNameArd));
						}
					}
					ard = new System.IO.BinaryWriter(System.IO.File.Open(fileNameArd, FileMode.Create));
					ard.Write(new byte[] { modelCode }, 0, 1);
					if (!connected)
					{
						var oldPosition = mdp.BaseStream.Position;
						mdp.BaseStream.Position = mdp.BaseStream.Length - 1;
						if (mdp.ReadByte() == 254)
						{
							mdp.BaseStream.Position -= 8;
							firmwareArray = mdp.ReadBytes(6);
						}
						mdp.BaseStream.Position = oldPosition;
					}
					ard.Write(firmwareArray, 0, 6);
					ard.Write(mdp.ReadBytes(254));

				}

				else if (testByte == 0x55)
				{
					ard.Write(mdp.ReadBytes(255));
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
#if DEBUG
					//MessageBox.Show(testByte.ToString("X2") + "  " + mdp.BaseStream.Position.ToString("X"));
					ard.Write(mdp.ReadBytes(255));
#endif


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
					fDel(fileNameMdp);
				}
				else
				{
					if (!Path.GetExtension(fileNameMdp).Contains("Dump"))
					{
						string newFileNameMdp = Path.GetDirectoryName(fileNameMdp) + "\\" + Path.GetFileNameWithoutExtension(fileNameMdp) + ".memDump";
						if (System.IO.File.Exists(newFileNameMdp)) fDel(newFileNameMdp);
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
			bool makeTxt = false;
			bool makeKml = false;
			timeStamp timeStampO = new timeStamp();
			byte[] ev = new byte[5];
			string barStatus = "";
			debugLevel = parent.stDebugLevel;
			oldUnitDebug = parent.stOldUnitDebug;
			addGpsTime = parent.addGpsTime;
			bool removeNonGps = false;

			//Imposta le preferenze di conversione
			timeStampO.eventAr = ev;
			if ((Parent.getParameter("pressureRange") == "air")) isDepth = 0;

			if ((prefs[pref_fillEmpty] == "False")) repeatEmptyValues = false;
			if (addGpsTime) repeatEmptyValues = false;

			dateSeparator = csvSeparator;
			if ((prefs[pref_sameColumn] == "True"))
			{
				sameColumn = true;
				dateSeparator = " ";
			}
			if (addGpsTime) sameColumn = true;

			if (prefs[pref_txt] == "True") makeTxt = true;
			if (prefs[pref_kml] == "True") makeKml = true;
			if (prefs[pref_battery] == "True") prefBattery = true;
			if (prefs[pref_pressMetri] == "meters") inMeters = true;
			if (prefs[pref_timeFormat] == "2") angloTime = true;

			timeStampO.pressOffset = double.Parse(prefs[pref_millibars]);
			dateFormat = byte.Parse(prefs[pref_dateFormat]);
			//timeFormat = byte.Parse(prefs[pref_timeFormat]);
			switch (dateFormat)
			{
				case 1:
					dateFormatParameter = "dd/MM/yyyy";
					break;
				case 2:
					dateFormatParameter = "MM/dd/yyyy";
					break;
				case 3:
					dateFormatParameter = "yyyy/MM/dd";
					break;
				case 4:
					dateFormatParameter = "yyyy/dd/MM";
					break;
			}
			metadata = false;
			if (prefs[pref_metadata] == "True") metadata = true;
			leapSeconds = int.Parse(prefs[pref_leapSeconds]);
			removeNonGps = bool.Parse(prefs[pref_removeNonGps]);

			timeStampO.inAdc = 0;
			timeStampO.inWater = 0;

			string shortFileName;

			string addOn = "";
			string exten = Path.GetExtension(fileName);
			if ((exten.Length > 4)) addOn = ("_S" + exten.Remove(0, 4));

			string fileNameCsv = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + addOn + ".csv";
			string FileNametxt = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + addOn + ".txt";
			string fileNameKml = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + addOn + "_temp" + ".kml";
			string fileNamePlaceMark = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + addOn + ".kml";

			BinaryReader ardFile = null;

			for (int i = 0; i < 3; i++)
			{
				try
				{
					ardFile = new BinaryReader(File.Open(fileName, FileMode.Open));
					break;
				}
				catch (Exception fileError)
				{
					if (i == 2)
					{
						MessageBox.Show(fileError.Message);
						Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
										new Action(() => parent.nextFile()));
						return;
					}
					Thread.Sleep(1000);
				}
			}

			var sesAdd = new List<long>();

			if (exten.Contains("rem"))
			{
				ardFile.BaseStream.Position = 0x10;
				long sesAddPointer = ardFile.ReadByte() + ardFile.ReadByte() * 0x100 + ardFile.ReadByte() * 0x10000 + ardFile.ReadByte() * 0x1000000;
				ardFile.BaseStream.Position = sesAddPointer + 0x10;
				long newAdd = 0;
				do
				{
					newAdd = BitConverter.ToInt64(ardFile.ReadBytes(8), 0);
					ardFile.ReadBytes(8);
					sesAdd.Add(newAdd);
				} while ((newAdd != 0) & (ardFile.BaseStream.Position < ardFile.BaseStream.Length));
				sesAdd.RemoveAt(sesAdd.Count - 1);
			}
			else
			{
				removeNonGps = false;
				sesAdd.Add(0);
			}

			BinaryWriter csv = new System.IO.BinaryWriter(System.IO.File.OpenWrite(fileNameCsv));
			BinaryWriter txt = BinaryWriter.Null;
			BinaryWriter kml = BinaryWriter.Null;
			BinaryWriter placeMark = BinaryWriter.Null;

			if (makeTxt)
			{
				if ((File.Exists(FileNametxt)) & (exten.Contains("ard"))) fDel(FileNametxt);
				txt = new BinaryWriter(File.OpenWrite(FileNametxt));
			}
			if (makeKml)
			{
				if ((File.Exists(fileNameKml)) & (exten.Contains("ard"))) fDel(fileNameKml);
				if ((File.Exists(fileNamePlaceMark)) & (exten.Contains("ard"))) fDel(fileNamePlaceMark);
				kml = new BinaryWriter(File.OpenWrite(fileNameKml));
				placeMark = new BinaryWriter(File.OpenWrite(fileNamePlaceMark));
				primaCoordinata = true;
				contoCoord = 0;
				//string
				kml.Write(Encoding.ASCII.GetBytes(Properties.Resources.Folder_Path_Top));
				kml.Write(Encoding.ASCII.GetBytes(Properties.Resources.Path_Top));
				//placemark
				placeMark.Write(System.Text.Encoding.ASCII.GetBytes(Properties.Resources.Final_Top_1 +
					Path.GetFileNameWithoutExtension(fileName) + Properties.Resources.Final_Top_2));
			}

			byte[] ardBuffer;// = new byte[ardFile.BaseStream.Length];
			bool headerMissing = true;


			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => barStatus = (string)parent.statusLabel.Content));

			int sesMax = sesAdd.Count;
			int sesCounter = 1;
			ardFile.Close();

			string logFile = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileName(fileName) + ".log";
			try
			{
				fDel(logFile);
			}
			catch { }

			while (sesAdd.Count > 0)
			{
				ardFile = new BinaryReader(File.Open(fileName, FileMode.Open));
				long bufLen;
				if (sesAdd.Count > 1)
				{
					bufLen = sesAdd[1] - sesAdd[0];
				}
				else
				{
					bufLen = ardFile.BaseStream.Length - sesAdd[0];
				}

				ardFile.BaseStream.Position = sesAdd[0];
				infRemPosition = sesAdd[0];
				sesAdd.RemoveAt(0);
				ardBuffer = new byte[bufLen];
				ardFile.Read(ardBuffer, 0, (int)bufLen);

				ardFile.Close();

				MemoryStream ard = new MemoryStream(ardBuffer);

				if (debugLevel > 0)
				{
					txt.Write(Encoding.ASCII.GetBytes("\r\n********************************* SESSION #" + sesCounter.ToString() + " (0x" +
					(ard.Position + infRemPosition).ToString("X4") + ")\r\n"));
				}
				//Encoding.ASCII.GetBytes(coord)

				byte[] uf = new byte[16];
				ard.Position = 1;
				firmTotA = (uint)ard.ReadByte() * (uint)1000000 + (uint)ard.ReadByte() * (uint)1000 + (uint)ard.ReadByte();
				firmTotB = (uint)ard.ReadByte() * (uint)1000000 + (uint)ard.ReadByte() * (uint)1000 + (uint)ard.ReadByte();

				byte[] coeffCheck = new byte[12];// = ard.ReadBytes(12);
				ard.Read(coeffCheck, 0, 12);


				byte adcFlag = coeffCheck[0];
				int coeffSum12 = 0;
				foreach (byte b in coeffCheck) { coeffSum12 += b; }
				//coeffChek = ard.ReadBytes(4);
				ard.Read(coeffCheck, 0, 4);
				int coeffSum16 = coeffSum12;
				foreach (byte b in coeffCheck) { coeffSum16 += b; }
				//long pos = 0;
				bool adcPresence = false;
				if (adcFlag != 0)                   //Se il primo byte non è zero, sicuramente non c'è l'info adc
				{
					adcPresence = false;                        //no adc
				}
				else
				{                                   //Altrimenti potrebbero essere a zero i coeff adc. In questo caso:
					if (coeffSum16 == 0)                        //16 byte consecutivi a zero vuol dire PRESENZA di info adc e coefficienti a zero (4 + 12)
					{
						adcPresence = true;
					}
					else
					{                                           //I 16 byte non sono tutti a zero: in questo caso:
						if (coeffSum12 == 0)                        //Sono a zero tutti e solo i primi 12, vuol dire NO info adc e coefficienti a zero
						{
							adcPresence = false;                //no adc
						}
						else
						{                                           //I primi 12 byte non sono tutti a zero: i primi 4 sono le info adc e successivamente ci sono i coefficienti
							adcPresence = true;                 //si adc
						}
					}
				}
				ard.Position = 7;

				//Controlla se l'ard contiene le informazioni sul sensore Analogico
				if (adcPresence)
				{
					ard.ReadByte();
					adcThreshold = (ushort)(ard.ReadByte() * 256 + ard.ReadByte());
					byte adcTemp = (byte)ard.ReadByte();
					if ((adcTemp & 8) == 8) adcStop = true;
					//if ((adcTemp & 4) == 4) adcMagmin = true;
					if ((adcTemp & 2) == 2) adcLog = true;
				}
				//Legge i coefficienti di profondità
				//ard.BaseStream.Position = pos;
				convCoeffs = new double[] { 0, 0, 0, 0, 0, 0 };
				for (int u = 0; u <= 5; u++) convCoeffs[u] = (ard.ReadByte() * 256) + ard.ReadByte();

				//Legge i parametri di logging
				pressureEnabled = ard.ReadByte();
				temperatureEnabled = pressureEnabled;
				pressureEnabled /= 16;
				temperatureEnabled &= 15;

				byte rrb = (byte)ard.ReadByte();
				rate = findSamplingRate(rrb);
				range = findRange(rrb);
				bits = findBits(rrb);
				bitsDiv = findBytesPerSample();
				nOutputs = rate;
				findDebugStampPar();
				Array.Resize(ref lastGroup, ((rate * 3)));

				//cifreDec = 3;
				cifreDecString = "0.000";
				if (bits)
				{
					//cifreDec = 4;
					cifreDecString = "0.0000";
				}

				string sesInfo = "";
				if (exten.Contains("rem"))
				{
					sesInfo = " (Session " + sesCounter.ToString() + "/" + sesMax.ToString() + ")";
				}

				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
					new Action(() => parent.statusLabel.Content = barStatus + sesInfo + " - Searching for a GPS fix..."));

				while (true)
				{
					byte abCheck = (byte)ard.ReadByte();
					if (abCheck != 0xab)
					{
						continue;
					}
					else
					{
						abCheck = (byte)ard.ReadByte();
						if (abCheck == 0xab)
						{
							continue;
						}
						else
						{
							ard.Position -= 1;
							break;
						}
					}
				}


				//while (abCheck != 0xab) abCheck = (byte)ard.ReadByte();


				long pos = ard.Position;
				//ard.Close();
				//timeStampO.orario = findStartTime(ref ard, ref prefs, pos);
				//in caso di rem, se la sessione non contiene il fix gps viene tolta dal csv

				DateTime startTime = findStartTime(ref ard, ref prefs, pos, exten.Contains("rem"));

				if ((startTime.Year == 1980) | (startTime.Year == 2080))    //Se la data è 1980 l'ora è giusta
				{
					if (DateTime.Compare(recoveryDate, nullDate) != 0)  //Quindi se la sessione precedente ha l'ora la sfrutta
					{
						//Imposta la data della recovery date e l'ora dall'orario gps
						startTime = new DateTime(recoveryDate.Year, recoveryDate.Month, recoveryDate.Day, startTime.Hour,
							startTime.Minute, startTime.Second);
						if (DateTime.Compare(recoveryDate, startTime) > 0)
						{
							startTime = startTime.AddDays(1);
						}

					}
				}
				recoveryDate = new DateTime(1970, 1, 1, 0, 0, 0);

				ard.Position = pos;

				if (exten.Contains("rem") & !convertStop)
				{
					File.AppendAllText(logFile, "Session no. " + sesCounter.ToString() + ":\tCSV Position: " + csv.BaseStream.Position.ToString("X4")
						+ "\tREM Position: " + infRemPosition.ToString("X4") + "\r\n"
					+ "START:  " + startTime.AddSeconds(1).ToString("dd/MM/yyyy HH:mm:ss"));

					if ((startTime == new DateTime(1, 1, 1, 1, 1, 1)) & removeNonGps)
					{
						File.AppendAllText(logFile, "\tGPS FIX MISSING: This session contains no gps fix and won't be included in the csv file.\r\n");

						continue;
					}
					//File.AppendAllText(logFile, "\r\n");
				}

				timeStampO.orario = startTime;

				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.IsIndeterminate = false));
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
					new Action(() => parent.statusLabel.Content = barStatus + sesInfo + " - Converting"));
				//ard = new System.IO.BinaryReader(File.Open(fileName, FileMode.Open));
				//ard.BaseStream.Position = pos;
				shortFileName = Path.GetFileNameWithoutExtension(fileName);

				//Stopwatch sw = new Stopwatch();
				//sw.Start();



				if (headerMissing)
				{
					csvPlaceHeader(ref csv);
					headerMissing = false;
				}

				progMax = ard.Length - 1;
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
					new Action(() => parent.statusProgressBar.Maximum = ard.Length - 1));
				while (progressWorker.IsBusy) { };
				progressWorker.RunWorkerAsync();

				string sBuffer = "";

				//var oldPos = ard.Position;
				//Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
				//	new Action(() => progressUpdate(0, (int)(ard.Length - 1))));
				while (!convertStop)
				{

					while (Interlocked.Exchange(ref progLock, 2) > 0) { }

					progVal = ard.Position;
					Interlocked.Exchange(ref progLock, 0);

					if (detectEof(ref ard)) break;

					try
					{
						decodeTimeStamp(ref ard, ref timeStampO, firmTotA);
					}
					catch
					{

					}

					if (timeStampO.stopEvent > 0)
					{
						if ((timeStampO.stopEvent == 4) & (timeStampO.orario.Year > 1980) & (timeStampO.orario.Year < 2080))
						{

							recoveryDate = new DateTime(timeStampO.orario.Year, timeStampO.orario.Month, timeStampO.orario.Day,
								timeStampO.orario.Hour, timeStampO.orario.Minute, timeStampO.orario.Second);
						}
						groupConverter(ref timeStampO, lastGroup, shortFileName, ref sBuffer, ref infRemPosition);
						csv.Write(System.Text.Encoding.ASCII.GetBytes(sBuffer));
						break;
					}

					try
					{
						groupConverter(ref timeStampO, extractGroup(ref ard, ref timeStampO), shortFileName, ref sBuffer, ref infRemPosition);

						if (sBuffer.Length > 0x400)
						{
							csv.Write(Encoding.ASCII.GetBytes(sBuffer));
							sBuffer = "";
						}

						//csv.Write(System.Text.Encoding.ASCII.GetBytes(
						//    groupConverter(ref timeStampO, extractGroup(ref ard, ref timeStampO), shortFileName)));
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message);
					}

					if (((timeStampO.tsType & 32) == 32) | ((timeStampO.tsType & 16) == 16))
					{
						if (makeTxt) txtWrite(ref timeStampO, ref txt);
					}
					if ((timeStampO.tsType & 16) == 16)
					{
						if (makeKml) kmlWrite(ref timeStampO, ref kml, ref placeMark);
					}
				}

				while (Interlocked.Exchange(ref progLock, 2) > 0) { }
				progVal = (int)ard.Position;
				Thread.Sleep(300);
				progVal = -1;
				Interlocked.Exchange(ref progLock, 0);

				if (exten.Contains("rem"))
				{
					File.AppendAllText(logFile, "\t\tSTOP: " + timeStampO.orario.AddSeconds(1).ToString("dd/MM/yyyy HH:mm:ss") + "\r\n\r\n");
				}

				if (makeTxt) txtWrite(ref timeStampO, ref txt);
				sesCounter++;
				//Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
				//	new Action(() => parent.statusProgressBar.Value = ard.Position));

				//	csv.Write(Encoding.ASCII.GetBytes(sBuffer));
				if (convertStop & exten.Contains("rem"))
				{

					ard.Close();
					csv.Close();
					File.Delete(fileNameCsv);
					try
					{
						txt.Close();
						File.Delete(FileNametxt);
					}
					catch { }
					try
					{
						placeMark.Close();
						kml.Close();
						File.Delete(fileNamePlaceMark);
						File.Delete(fileNameKml);
					}
					catch { }
					sesAdd.Clear();
				}

				ard.Close();

			}

			if (makeKml)
			{
				try
				{
					//Scrive il segnaposto di stop nel fime kml dei placemarks
					placeMark.Write(System.Text.Encoding.ASCII.GetBytes(X_Manager.Properties.Resources.Folder_Bot));
					placeMark.Write(System.Text.Encoding.ASCII.GetBytes(X_Manager.Properties.Resources.Placemarks_Stop_Top +
						(decoderKml(ref timeStampO)).cPlacemark + X_Manager.Properties.Resources.Placemarks_Stop_Bot));
					kml.Close();
					placeMark.Close();

					//Scrive l'header finale nel file kml string
					File.AppendAllText(fileNameKml, X_Manager.Properties.Resources.Path_Bot);
					File.AppendAllText(fileNameKml, X_Manager.Properties.Resources.Folder_Bot);

					//Accorpa kml placemark e string
					File.AppendAllText(fileNamePlaceMark, System.IO.File.ReadAllText(fileNameKml));

					//Chiude il kml placemark
					File.AppendAllText(fileNamePlaceMark, X_Manager.Properties.Resources.Final_Bot);
					//Elimina il kml string temporaneo
					fDel(fileNameKml);
				}
				catch { }

			}

			if (makeTxt) txt.Close();

			csv.Close();
			try
			{
				ardFile.Close();
			}
			catch { }

			//sw.Stop();
			//MessageBox.Show(sw.Elapsed.TotalSeconds.ToString());
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
				new Action(() => parent.nextFile()));
		}

		private void decodeTimeStamp(ref MemoryStream ard, ref timeStamp tsc, uint fTotA)
		{
			tsc.ardPosition = ard.Position;
			tsc.stopEvent = 0;
			ushort secondAmount = 1;
			tsc.slowData = 0;

			tsc.tsType = ard.ReadByte();

			//Flag timestamp esteso
			if ((tsc.tsType & 1) == 1)
			{
				tsc.tsTypeExt1 = ard.ReadByte();
				if ((tsc.tsTypeExt1 & 1) == 1)
				{
					tsc.tsTypeExt2 = ard.ReadByte();
				}
				else
				{
					tsc.tsTypeExt2 = 0;
				}
			}
			else
			{
				tsc.tsTypeExt1 = 0;
			}

			//Temperatura (eventualmente anche pressione)
			if ((tsc.tsType & 2) == 2)
			{
				tsc.slowData++;
				if (temperatureEnabled == 2)
				{
					tsc.temperature = ard.ReadByte() + ard.ReadByte() * 256;
					tsc.temperature = (uint)(tsc.temperature) >> 6;
					if (tsc.temperature > 511)
					{
						tsc.temperature -= 1024;
					}
					tsc.temperature = (tsc.temperature * 0.1221) + 22.5;
				}
				else
				{
					if (isDepth == 1)
					{
						if (fTotA > 2000000)
						{
							if (pressureDepth5837(ref ard, ref tsc)) return;
						}
						else
						{
							if (pressureDepth5803(ref ard, ref tsc)) return;
						}
					}
					else
					{
						if (pressureAir(ref ard, ref tsc)) return;
					}
				}
			}

			//Batteria
			if ((tsc.tsType & 8) == 8)
			{
				tsc.slowData++;
				tsc.batteryLevel = ((ard.ReadByte() * 256 + ard.ReadByte()) * 6.0 / 4096);
			}

			//Coordinata
			if ((tsc.tsType & 16) == 16)
			{
				tsc.slowData++;
				ushort diffMask = (ushort)(ard.ReadByte() * 256 + ard.ReadByte());  //Legge la maschera
				byte[] fissi = new byte[6];//= ard.ReadBytes(6);                                    //Legge i dati fissi
				ard.Read(fissi, 0, 6);

				tsc.secondi = unchecked(fissi[0] >> 2);

				tsc.latMinDecL = unchecked((fissi[0] & 3) << 5);
				tsc.latMinDecL += unchecked(fissi[1] >> 3);
				tsc.lonMinDecL = unchecked((fissi[1] & 7) << 4);
				tsc.lonMinDecL += unchecked(fissi[2] >> 4);

				tsc.DOPdec = unchecked((fissi[2] & 15) >> 1);

				tsc.vel = unchecked((fissi[2] & 1) << 5);
				tsc.vel += unchecked(fissi[3] >> 3);
				//tsc.vel *= 2;

				tsc.nSat = (fissi[3] & 7);
				tsc.altL = fissi[4];

				tsc.latMinDecLL = unchecked(fissi[5] >> 4);
				tsc.lonMinDecLL = (fissi[5] & 15);

				tsc.altSegno = 0; tsc.ns = 0; tsc.eo = 0;
				if ((diffMask & 1) == 1) tsc.eo = 1;
				if ((diffMask & 2) == 2) tsc.ns = 1;
				if ((diffMask & 4) == 4) tsc.altSegno = 1;
				if ((diffMask & 8) == 8) tsc.anno = ard.ReadByte();
				if ((diffMask & 0x10) == 0x10) tsc.giorno = ard.ReadByte();
				if ((diffMask & 0x20) == 0x20) tsc.DOP = ard.ReadByte();
				if ((diffMask & 0x40) == 0x40) tsc.lonMinDecH = (byte)ard.ReadByte();
				if ((diffMask & 0x80) == 0x80) tsc.lonMinuti = (byte)ard.ReadByte();
				if ((diffMask & 0x100) == 0x100) tsc.lonGradi = (byte)ard.ReadByte();
				if ((diffMask & 0x200) == 0x200) tsc.latMinDecH = ard.ReadByte();
				if ((diffMask & 0x400) == 0x400) tsc.latMinuti = ard.ReadByte();
				if ((diffMask & 0x800) == 0x800) tsc.latGradi = ard.ReadByte();
				if ((diffMask & 0x1000) == 0x1000) tsc.minuti = ard.ReadByte();
				if ((diffMask & 0x2000) == 0x2000) tsc.ore = ard.ReadByte();
				if ((diffMask & 0x4000) == 0x4000)
				{
					int b = ard.ReadByte();
					tsc.mese = unchecked(b >> 4);
					tsc.altH = b & 15;
				}
				tsc.gsvSum = (ard.ReadByte() * 256 + ard.ReadByte());

			}


			//evento
			if ((tsc.tsType & 32) == 32)
			{
				tsc.slowData++;
				int b = ard.ReadByte();
				int debugCheck = ard.ReadByte();
				ard.Position -= 2;
				if ((b == debugStampId) && (debugCheck > 2))
				{
					tsc.eventAr = new byte[debugStampLenght];
					ard.Read(tsc.eventAr, 0, debugStampLenght);
					tsc.eventAr[0] = 80;
				}
				else
				{
					ard.Read(tsc.eventAr, 0, 5);
				}

				if (tsc.eventAr[0] == 11) tsc.stopEvent = 1;
				else if (tsc.eventAr[0] == 12) tsc.stopEvent = 2;
				else if (tsc.eventAr[0] == 13) tsc.stopEvent = 3;
				else if (tsc.eventAr[0] == 14)
				{
					tsc.stopEvent = 4;
				}

			}

			//Attività/acqua
			tsc.inWater = 0;
			if ((tsc.tsType & 128) == 128) tsc.inWater = 1;

			//Parametri estesi
			if ((tsc.tsType & 1) == 1)
			{
				//ADC log
				if ((tsc.tsTypeExt1 & 2) == 2)
				{
					tsc.slowData++;
					tsc.ADC = (ard.ReadByte() * 256 + ard.ReadByte());
				}

				//ADC Threshold
				tsc.inAdc = 0;
				if ((tsc.tsTypeExt1 & 0x4) == 0x4)
				{
					tsc.inAdc = 1;
				}

				//Timestamp multiplo
				if ((tsc.tsTypeExt1 & 0x40) == 0x40)
				{
					secondAmount = (byte)ard.ReadByte();
				}
			}

			tsc.orario = tsc.orario.AddSeconds(secondAmount);
		}

		private double[] extractGroup(ref MemoryStream ard, ref timeStamp tsc)
		{
			List<byte> group = new List<byte>();
			bool badGroup = false;
			//long position = 0;
			byte dummy, dummyExt;
			ushort badPosition = 1000;

			if (ard.Position == ard.Length) return lastGroup;

			do
			{
				dummy = (byte)ard.ReadByte();
				if (dummy == 0xab)
				{
					if (ard.Position < ard.Length) dummyExt = (byte)ard.ReadByte();
					else return lastGroup;

					if (dummyExt == 0xab)
					{
						//group[position] = (byte)0xab;
						group.Add(0xab);
						//position += 1;
						dummy = 0;
					}
					else
					{
						ard.Position -= 1;
						if (badGroup)
						{
							//System.IO.File.AppendAllText(((FileStream)ard.BaseStream).Name + "errorList.txt", "-> " + ard.Position.ToString("X8") + "\r\n");
						}
					}
				}
				else
				{
					//if (position < badPosition)
					if (group.Count < badPosition)
					{
						//group[position] = dummy;
						group.Add(dummy);
						//position++;
					}
					//else if ((position == badPosition) && (!badGroup))
					else if ((group.Count == badPosition) && (!badGroup))
					{
						badGroup = true;
						//System.IO.File.AppendAllText(((FileStream)ard.BaseStream).Name + "errorList.txt", "-> " + ard.BaseStream.Position.ToString("X8") + "\r\n");
					}
				}


			} while ((dummy != 0xab) && (ard.Position < ard.Length));

			tsc.timeStampLength = (int)(group.Count / bitsDiv);

			int resultCode = 0;
			if (group.Count == 0)
			{
				return new double[] { };
			}

			double[] doubleResult = new double[3 * nOutputs];
			if (bits)
			{
				resultCode = resample4(group.ToArray(), tsc.timeStampLength, doubleResult, nOutputs);
			}
			else
			{
				resultCode = resample3(group.ToArray(), tsc.timeStampLength, doubleResult, nOutputs);
			}
			return doubleResult;
		}

		private void groupConverter(ref timeStamp tsLoc, double[] group, string unitName, ref string textOut, ref long offset)
		{
			if (group == null) return;
			short iend;
			if (group.Length == 0)
			{
				if (tsLoc.slowData > 0)
				{
					group = new double[] { 0, 0, 0, };
					iend = 0;
				}
				else
				{
					return;
				}
			}
			else
			{
				iend = (short)(rate * 3);
			}

			double x, y, z;
			string dateTimeS, additionalInfo;
			string ampm = "";
			string dateS = "";
			ushort milli;
			NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;
			string activityWater = "";

			ushort contoTab = 0;

			dateS = tsLoc.orario.ToString(dateFormatParameter);

			var dateCi = new CultureInfo("it-IT");
			if (angloTime)
			{
				dateCi = new CultureInfo("en-US");
			}
			dateTimeS = dateS + dateSeparator + tsLoc.orario.ToString("T", dateCi);
			if (angloTime)
			{
				ampm = dateTimeS.Split(' ')[dateTimeS.Split(' ').Length - 1];
				dateTimeS = dateTimeS.Remove(dateTimeS.Length - 3, 3);
			}
			milli = 0;
			//textOut = "";


			textOut += unitName + csvSeparator + dateTimeS + ".000";
			if (angloTime)
			{
				textOut += " " + ampm;
			}
			if (addGpsTime)
			{
				if ((tsLoc.tsType & 16) == 16)
				{
					textOut += " (GPS: " + tsLoc.ore.ToString("00") + ":" + tsLoc.minuti.ToString("00") + ":" + tsLoc.secondi.ToString("00") + ") ";
					DateTime dtDiff;
					try
					{
						dtDiff = new DateTime(tsLoc.orario.Year, tsLoc.orario.Month, tsLoc.orario.Day,
						tsLoc.ore, tsLoc.minuti, tsLoc.secondi);
						double ts = (dtDiff - tsLoc.orario).TotalSeconds;
						textOut += ts.ToString();
						if (Math.Abs(ts) > 5)
						{
							textOut += " W";
						}
					}
					catch
					{
						//int a = 0;
					}

				}
			}
			x = group[0] * gCoeff;
			y = group[1] * gCoeff;
			z = group[2] * gCoeff;

			textOut += csvSeparator + x.ToString(cifreDecString, nfi) + csvSeparator + y.ToString(cifreDecString, nfi) + csvSeparator + z.ToString(cifreDecString, nfi);

			additionalInfo = "";
			if (debugLevel > 2)
			{
				additionalInfo += csvSeparator + (tsLoc.ardPosition + offset).ToString("X");
			}
			contoTab += 1;
			if ((tsLoc.tsType & 64) == 64) activityWater = "Active";
			else activityWater = "Inactive";
			if ((tsLoc.tsType & 128) == 128) activityWater += "/Wet";
			else activityWater += "/Dry";

			additionalInfo += csvSeparator + activityWater;

			if (pressureEnabled > 0)
			{
				contoTab += 1;
				additionalInfo += csvSeparator;
				if (((tsLoc.tsType & 4) == 4) | repeatEmptyValues) additionalInfo += tsLoc.press.ToString("0.00", nfi);
			}
			if (temperatureEnabled > 0)
			{
				contoTab += 1;
				additionalInfo += csvSeparator;
				if (((tsLoc.tsType & 2) == 2) | repeatEmptyValues) additionalInfo += tsLoc.temperature.ToString("0.0", nfi);
			}

			//Inserire la coordinata.
			contoTab += 7;
			if (((tsLoc.tsType & 16) == 16) | repeatEmptyValues)
			{
				string altSegno, eo, ns;
				altSegno = eo = ns = "-";
				if (tsLoc.altSegno == 0) altSegno = "";
				if (tsLoc.eo == 1) eo = "";
				if (tsLoc.ns == 1) ns = "";
				double speed = tsLoc.vel * 3.704;
				double lon, lat = 0;

				lon = ((tsLoc.lonMinuti + (tsLoc.lonMinDecH / 100.0) + (tsLoc.lonMinDecL / 10000.0) + (tsLoc.lonMinDecLL / 100000.0)) / 60) + tsLoc.lonGradi;
				lat = ((tsLoc.latMinuti + (tsLoc.latMinDecH / 100.0) + (tsLoc.latMinDecL / 10000.0) + (tsLoc.latMinDecLL / 100000.0)) / 60) + tsLoc.latGradi;

				additionalInfo += csvSeparator + ns + lat.ToString("#00.00000", nfi);
				additionalInfo += csvSeparator + eo + lon.ToString("#00.00000", nfi);
				additionalInfo += csvSeparator + altSegno + ((tsLoc.altH * 256 + tsLoc.altL) * 2).ToString();
				additionalInfo += csvSeparator + speed.ToString("0.0", nfi);
				additionalInfo += csvSeparator + tsLoc.nSat.ToString();
				additionalInfo += csvSeparator + tsLoc.DOP.ToString() + "." + tsLoc.DOPdec.ToString();
				additionalInfo += csvSeparator + tsLoc.gsvSum.ToString();
			}
			else
			{
				additionalInfo += csvSeparator + csvSeparator + csvSeparator + csvSeparator + csvSeparator + csvSeparator + csvSeparator;
			}

			//Inserisce il sensore analogico
			if (adcLog)
			{
				contoTab += 1;
				additionalInfo += csvSeparator;
				if (((tsLoc.tsTypeExt1 & 2) == 2) | repeatEmptyValues) additionalInfo += tsLoc.ADC.ToString("0000");
			}

			if (adcStop)
			{
				contoTab += 1;
				additionalInfo += csvSeparator;
				if (((tsLoc.tsTypeExt1 & 4) == 4) | repeatEmptyValues) additionalInfo += "Threshold crossed";
			}

			//Inserisce la batteria
			if (prefBattery)
			{
				contoTab += 1;
				additionalInfo += csvSeparator;
				if (((tsLoc.tsType & 8) == 8) | repeatEmptyValues) additionalInfo += tsLoc.batteryLevel.ToString("0.00", nfi);
			}

			//Inserisce i metadati
			if (metadata)
			{
				contoTab += 1;
				additionalInfo += csvSeparator;
				if (tsLoc.stopEvent > 0)
				{
					switch (tsLoc.stopEvent)
					{
						case 1:
							additionalInfo += "Low battery.";
							break;
						case 2:
							additionalInfo += "Power off command received.";
							break;
						case 3:
							additionalInfo += "Memory full.";
							break;
						case 4:
							additionalInfo += "Remote Connection.";
							break;
					}
					textOut += additionalInfo + "\r\n";
					return;// textOut;
				}
			}

			textOut += additionalInfo + "\r\n";

			if (tsLoc.stopEvent > 0) return;// textOut;

			if (!repeatEmptyValues)
			{
				additionalInfo = "";
				for (ushort ui = 0; ui < contoTab; ui++) additionalInfo += csvSeparator;
			}

			milli += addMilli;
			dateTimeS += ".";
			if (tsLoc.stopEvent > 0) bitsDiv = 1;

			for (short i = 3; i < iend; i += 3)
			{
				x = group[i] * gCoeff;
				y = group[i + 1] * gCoeff;
				z = group[i + 2] * gCoeff;

				if (rate == 1)
				{
					tsLoc.orario = tsLoc.orario.AddSeconds(1);
					dateTimeS = dateS + csvSeparator + tsLoc.orario.ToString("T", dateCi) + ".";
				}
				textOut += unitName + csvSeparator + dateTimeS + milli.ToString("D3");

				if (angloTime) textOut += " " + ampm;
				textOut += csvSeparator + x.ToString(cifreDecString, nfi) + csvSeparator + y.ToString(cifreDecString, nfi) + csvSeparator + z.ToString(cifreDecString, nfi);

				textOut += additionalInfo + "\r\n";
				milli += addMilli;
			}

			//return textOut;
		}

		private DateTime findStartTime(ref MemoryStream br, ref string[] prefs, long pos, bool isRem)
		{
			//BinaryReader br = new System.IO.BinaryReader(System.IO.File.Open(fileName, FileMode.Open));
			const int pref_h = 9;
			const int pref_m = 10;
			const int pref_s = 11;
			const int pref_date_year = 12;
			const int pref_date_month = 13;
			const int pref_date_day = 14;

			timeStamp tsc = new timeStamp();
			pos -= 1;

			//long fileLength = br.Length;

			DateTime dt = new DateTime(int.Parse(prefs[pref_date_year]), int.Parse(prefs[pref_date_month]), int.Parse(prefs[pref_date_day]),
				int.Parse(prefs[pref_h]), int.Parse(prefs[pref_m]), int.Parse(prefs[pref_s]));
			if (isRem)
			{
				dt = new DateTime(1, 1, 1, 1, 1, 1);
			}

			byte timeStamp0 = 0;
			byte timeStamp1 = 0;
			uint secondiAdd = 0;
			byte[] coordinate = new byte[22];
			int noByteTemper = 3;
			if (temperatureEnabled == 2)
			{
				noByteTemper = 2;
			}


			//br.BaseStream.Position = 7;
			//if (br.ReadByte() == 0) br.BaseStream.Position = 25;
			//else br.BaseStream.Position = 21;
			//var br = new MemoryStream(buf);

			br.Position = pos;
			ushort secondAmount = 1;
			double brMax = br.Length;
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Minimum = 0));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Maximum = brMax));

			while (br.Position < br.Length)
			{

				if (br.ReadByte() == 0xab)
				{
					timeStamp0 = (byte)br.ReadByte();
					secondAmount = 1;
					if (timeStamp0 != 0xab)
					{
						double ppos = br.Position;
						Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Value = ppos));
						if ((timeStamp0 & 1) == 1) timeStamp1 = (byte)br.ReadByte();
						if ((timeStamp0 & 2) == 2) br.Position += noByteTemper;
						if ((timeStamp0 & 4) == 4) br.Position += 3;
						if ((timeStamp0 & 8) == 8) br.Position += 2;
						if ((timeStamp0 & 16) == 16)
						{
							br.Read(coordinate, 0, 22);
							//break;
							if (coordinate[8] != 80) break;
						}
						if ((timeStamp0 & 32) == 32)
						{
							byte ev = (byte)br.ReadByte();       //ex b
							int debugCheck = (byte)br.ReadByte();

							br.Position -= 2;
							if ((ev == debugStampId) && (debugCheck > 2))
							{
								//br.ReadBytes(debugStampLenght);
								br.Position += debugStampLenght;
							}
							else
							{
								//br.ReadBytes(5);
								br.Position += 5;
							}

						}
						if ((timeStamp0 & 1) == 1)
						{
							if ((timeStamp1 & 2) == 2) br.Position += 2;
							if ((timeStamp1 & 0x40) == 0x40) secondAmount = (byte)br.ReadByte();
						}
						secondiAdd += secondAmount;
					}
				}
			}
			if (br.Position >= br.Length)
			{
				//br.Close();
				return dt;
			}

			// Valori di sicurezza
			tsc.anno = 14;
			tsc.giorno = 19;
			tsc.mese = 11;
			tsc.ore = 12;
			tsc.minuti = 51;
			ushort diffMask = (ushort)(coordinate[0] * 256 + coordinate[1]);

			tsc.secondi = unchecked(coordinate[2] >> 2);
			tsc.latMinDecL = unchecked((coordinate[2] & 3) << 5);
			tsc.latMinDecL += unchecked(coordinate[3] >> 3);
			tsc.lonMinDecL = unchecked((coordinate[3] & 7) << 4);
			tsc.lonMinDecL += unchecked((coordinate[4] >> 4));
			tsc.DOPdec = unchecked((coordinate[4] & 15) >> 1);
			tsc.vel = unchecked((coordinate[4] & 1) << 5);
			tsc.vel += unchecked((coordinate[5] >> 3));
			tsc.vel *= 2;
			tsc.nSat = (coordinate[5] & 7);
			tsc.altL = coordinate[6];
			tsc.latMinDecLL += unchecked(coordinate[7] >> 4);
			tsc.lonMinDecLL = (coordinate[7] & 15);
			byte cCounter = 8;

			//tsc.altSegno = true; tsc.ns = false; tsc.eo = false;
			//if ((diffMask & 1) == 1) tsc.eo = true;
			//if ((diffMask & 2) == 2) tsc.ns = true;
			//if ((diffMask & 4) == 4) tsc.altSegno = true;
			if ((diffMask & 8) == 8) { tsc.anno = coordinate[cCounter]; cCounter += 1; }
			if ((diffMask & 0x10) == 0x10) { tsc.giorno = coordinate[cCounter]; cCounter += 1; }
			if ((diffMask & 0x20) == 0x20) { tsc.DOP = coordinate[cCounter]; cCounter += 1; }
			if ((diffMask & 0x40) == 0x40) { tsc.lonMinDecH = coordinate[cCounter]; cCounter += 1; }
			if ((diffMask & 0x80) == 0x80) { tsc.lonMinuti = coordinate[cCounter]; cCounter += 1; }
			if ((diffMask & 0x100) == 0x100) { tsc.lonGradi = coordinate[cCounter]; cCounter += 1; }
			if ((diffMask & 0x200) == 0x200) { tsc.latMinDecH = coordinate[cCounter]; cCounter += 1; }
			if ((diffMask & 0x400) == 0x400) { tsc.latMinuti = coordinate[cCounter]; cCounter += 1; }
			if ((diffMask & 0x800) == 0x800) { tsc.latGradi = coordinate[cCounter]; cCounter += 1; }
			if ((diffMask & 0x1000) == 0x1000) { tsc.minuti = coordinate[cCounter]; cCounter += 1; }
			if ((diffMask & 0x2000) == 0x2000) { tsc.ore = coordinate[cCounter]; cCounter += 1; }
			if ((diffMask & 0x4000) == 0x4000)
			{
				tsc.mese = unchecked(coordinate[cCounter] >> 4);
				tsc.altH = (coordinate[cCounter] & 15);
			}
			//br.Close();
			secondiAdd += 1;    //Questo secondo sottratto in più viene reinserito al decoding del primo timestamp

			try
			{
				dt = new DateTime(2000 + tsc.anno, tsc.mese, tsc.giorno, tsc.ore, tsc.minuti, tsc.secondi);
				dt = dt.AddSeconds(-secondiAdd);
				dt = dt.AddSeconds(leapSeconds * -1);
			}
			catch { }

			return dt;
		}

		private ushort findSamplingRate(byte rateIn)
		{
			byte rateOut;
			rateIn = unchecked((byte)(rateIn >> 4));
			switch (rateIn)
			{
				case 0:
					rateOut = 50;
					break;
				case 1:
					rateOut = 25;
					break;
				case 2:
					rateOut = 100;
					break;
				case 3:
					rateOut = 10;
					break;
				case 4:
					rateOut = 1;
					break;
				default:
					rateOut = 50;
					break;
			}
			if ((rateOut == 1)) addMilli = 0;
			else addMilli = (ushort)((1.0 / rateOut) * 1000);

			return rateOut;

		}

		private byte findRange(byte rangeIn)
		{
			rangeIn = unchecked((byte)(rangeIn & 15));
			if ((rangeIn > 7))
			{
				rangeIn -= 8;
			}

			switch (rangeIn)
			{
				case 0:
					return 2;
				case 1:
					return 4;
				case 2:
					return 8;
				case 3:
					return 16;
				default:
					return 2;
			}
		}

		private bool findBits(byte bitsIn)
		{
			bitsIn = unchecked((byte)(bitsIn & 15));
			//sogliaNeg = 127;
			//rendiNeg = 256;
			if (bitsIn < 8)
			{
				switch (range)
				{
					case 2:
						gCoeff = 15.63;
						break;
					case 4:
						gCoeff = 31.26;
						break;
					case 8:
						gCoeff = 62.52;
						break;
					case 16:
						gCoeff = 187.58;
						break;
				}
				gCoeff /= 1000;
				return false;
			}
			else
			{
				switch (range)
				{
					case 2:
						gCoeff = 3.9;
						break;
					case 4:
						gCoeff = 7.82;
						break;
					case 8:
						gCoeff = 15.63;
						break;
					case 16:
						gCoeff = 46.9;
						break;
				}
				gCoeff /= 1000;
				return true;
			}
		}

		private byte findBytesPerSample()
		{
			byte bitsDiv = 3;
			if (bits) bitsDiv = 4;
			return bitsDiv;
		}

		private void findDebugStampPar()
		{
			debugStampId = 80;
			debugStampLenght = 15;
			if (firmTotB < 003002000) debugStampId = 13;
			if (firmTotB < 003001000) debugStampId = 200;
			if (firmTotB < 003000000) debugStampId = 13;
			if (firmTotB < 001000004) debugStampLenght = 7;
		}

		private void txtWrite(ref timeStamp tsc, ref BinaryWriter txtW)
		{
			string altSegno, eo, ns, coord;
			var nfi = new CultureInfo("en-US", false).NumberFormat;
			string dateS = tsc.orario.ToString(dateFormatParameter);
			string dateTimeS = dateS + csvSeparator + tsc.orario.ToString("HH:mm:ss");

			if ((((tsc.tsType & 32) == 32) && (debugLevel > 0)))
			{
				coord = (dateTimeS + '\t');
				string coord2 = "";
				decodeEvent(ref coord2, ref tsc);
				if ((coord2 != ""))
				{
					coord += coord2 + "\r\n";
					txtW.Write(Encoding.ASCII.GetBytes(coord));
				}
			}

			if ((tsc.tsType & 16) == 16)
			{
				altSegno = "-"; if (tsc.altSegno == 0) altSegno = "";
				eo = "-"; if (tsc.eo == 1) eo = "";
				ns = "-"; if (tsc.ns == 1) ns = "";
				double speed = tsc.vel * 3.704;
				double lat, lon; lat = lon = 0;
				lon = ((tsc.lonMinuti + (tsc.lonMinDecH / (double)100) + (tsc.lonMinDecL / (double)10000) + (tsc.lonMinDecLL / (double)100000)) / 60) + tsc.lonGradi;
				lat = ((tsc.latMinuti + (tsc.latMinDecH / (double)100) + (tsc.latMinDecL / (double)10000) + (tsc.latMinDecLL / (double)100000)) / 60) + tsc.latGradi;
				coord = dateTimeS;
				coord += "\t" + ns + lat.ToString("#00.00000", nfi);
				coord += "\t" + eo + lon.ToString("#00.00000", nfi);
				coord += "\t" + altSegno + ((tsc.altH * 256 + tsc.altL) * 2).ToString() + "\t" + speed.ToString("0.0") + "\t";
				coord += tsc.nSat.ToString() + "\t" + tsc.DOP.ToString() + "." + tsc.DOPdec.ToString();
				coord += "\t" + tsc.gsvSum.ToString() + "\r\n";
				txtW.Write(Encoding.ASCII.GetBytes(coord));
			}


		}

		private void kmlWrite(ref timeStamp tsc, ref BinaryWriter kmlW, ref BinaryWriter placeMarkW)
		{
			coordKml coordinataKml = new coordKml();
			byte[] buffer;
			if (tsc.nSat > 0)
			{
				coordinataKml = decoderKml(ref tsc);
				if ((contoCoord == 10000))
				{
					//  se arrivato a 10000 coordinate apre un nuovo <coordinates>
					kmlW.Write(Encoding.ASCII.GetBytes(Properties.Resources.Path_Bot));
					kmlW.Write(Encoding.ASCII.GetBytes(Properties.Resources.Path_Top));
					contoCoord = 0;
				}
				kmlW.Write(Encoding.ASCII.GetBytes("\t\t\t\t\t" + coordinataKml.cSstring + "\r\n"));
				contoCoord++;

				if (primaCoordinata)
				{
					primaCoordinata = false;
					string[] lookAtValues = new string[3];
					lookAtValues = coordinataKml.cPlacemark.Split(',');
					lookAtValues[0] = lookAtValues[0].Remove(0, (lookAtValues[0].IndexOf(">") + 1));
					lookAtValues[2] = lookAtValues[2].Remove(lookAtValues[2].IndexOf("<"), (lookAtValues[2].Length - lookAtValues[2].IndexOf("<")));
					buffer = Encoding.ASCII.GetBytes(
						Properties.Resources.lookat1 + lookAtValues[0] + X_Manager.Properties.Resources.lookat2 + lookAtValues[1]
						+ X_Manager.Properties.Resources.lookat3 + lookAtValues[2] + X_Manager.Properties.Resources.lookat4);
					placeMarkW.Write(buffer, 0, buffer.Length - 1);
					buffer = System.Text.Encoding.ASCII.GetBytes(X_Manager.Properties.Resources.Placemarks_Start_Top + coordinataKml.cPlacemark
						+ X_Manager.Properties.Resources.Placemarks_Start_Bot);
					placeMarkW.Write(buffer, 0, buffer.Length);
					placeMarkW.Write(System.Text.Encoding.ASCII.GetBytes(X_Manager.Properties.Resources.Folder_Generics_Top));
				}
				buffer = System.Text.Encoding.ASCII.GetBytes(
					X_Manager.Properties.Resources.Placemarks_Generic_Top_1 + coordinataKml.cName + X_Manager.Properties.Resources.Placemarks_Generic_Top_2 + coordinataKml.cClass
					+ X_Manager.Properties.Resources.Placemarks_Generic_Top_3 + ((tsc.altH * 256 + tsc.altL) * 2).ToString()
					+ X_Manager.Properties.Resources.Placemarks_Generic_Top_4 + (tsc.vel * 3.704).ToString()
					+ X_Manager.Properties.Resources.Placemarks_Generic_Top_5 + coordinataKml.cPlacemark + X_Manager.Properties.Resources.Placemarks_Generic_Bot);
				placeMarkW.Write(buffer, 0, buffer.Length);
			}
		}

		private coordKml decoderKml(ref timeStamp tsc)
		{
			double LATgradi, LATminuti, LONgradi, LONminuti;
			char[] Kmlarr;
			coordKml strOut = new coordKml();

			LATminuti = tsc.latMinDecLL + (tsc.latMinDecL * 10) + (tsc.latMinDecH * 1000);
			LONminuti = tsc.lonMinDecLL + (tsc.lonMinDecL * 10) + (tsc.lonMinDecH * 1000);
			LATminuti = (((LATminuti / 100000) + tsc.latMinuti) / 60);
			LATgradi = (tsc.latGradi + LATminuti);
			LONminuti = (((LONminuti / 100000) + tsc.lonMinuti) / 60);
			LONgradi = (tsc.lonGradi + LONminuti);

			strOut.cSstring = "";

			if (tsc.eo == 0) strOut.cSstring += "-";
			strOut.cSstring += LONgradi.ToString("00.000000") + ",";

			if (tsc.ns == 0) strOut.cSstring += "-";
			strOut.cSstring += LATgradi.ToString("00.000000") + ",";

			if (tsc.altSegno == 1) strOut.cSstring += "-";
			strOut.cSstring += ((tsc.altH * 256) + tsc.altL).ToString("0000.0");
			strOut.cSstring = strOut.cSstring.Replace('.', ',');
			Kmlarr = strOut.cSstring.ToCharArray();
			byte contoVirgole = 0;
			for (byte i = 0; i < Kmlarr.Length; i++)
			{
				if (Kmlarr[i] == ',')
				{
					contoVirgole++;
					if ((contoVirgole % 2) != 0) Kmlarr[i] = ('.');
				}
			}
			strOut.cSstring = new string(Kmlarr);
			strOut.cPlacemark = "\r\n\t\t\t\t\t+<coordinates>" + strOut.cSstring + "</coordinates>\r\n";
			strOut.cName = tsc.orario.ToString("dd/MM/yyyy") + " " + tsc.orario.ToString("HH:mm:ss");
			double nVel = tsc.vel * 3.6;
			if (nVel < 10) strOut.cClass = "1";
			else if (nVel < 20) strOut.cClass = "2";
			else if (nVel < 30) strOut.cClass = "3";
			else if (nVel < 40) strOut.cClass = "4";
			else if (nVel < 50) strOut.cClass = "5";
			else if (nVel < 60) strOut.cClass = "6";
			else if (nVel < 70) strOut.cClass = "7";
			else if (nVel < 80) strOut.cClass = "8";
			else strOut.cClass = "9";

			return strOut;
		}

		private void decodeEvent(ref string s, ref timeStamp t)
		{
			switch (t.eventAr[0])
			{
				case 0:
					s += "Start searching for satellites...";
					break;
				case 1:
					s += "Signal obtained. Starting schedule...";
					break;
				case 2:
					s += "No visible satellite. Going to sleep...";
					break;
				case 3:
					s += "Start Pre-fix Start Delay for ";
					s = s + ((t.eventAr[1] * 256) + t.eventAr[2]).ToString() + " minutes.";
					break;
				case 4:
					s += "End of start delay. Start searching for satellites...";
					break;
				case 5:
					s += "Signal lost -> ACQ ON";
					s = s + "   GSV Sum: " + ((t.eventAr[1] * 256) + t.eventAr[2]).ToString();
					break;
				case 6:
					s += "Signal lost -> ACQ OFF";
					s = s + "   GSV Sum: " + ((t.eventAr[1] * 256) + t.eventAr[2]).ToString();
					break;
				case 7:
					s = s + "Schedule: " + t.eventAr[1].ToString() + " " + t.eventAr[2].ToString() + " " + t.eventAr[3].ToString();
					break;
				case 8:
					if ((debugLevel > 1)) s += "ACTIVITY = ACT_RUN";
					else s = "";
					break;
				case 9:
					if ((debugLevel > 1)) s += "ACTIVITY = ACT_LASTONE";
					else s = "";
					break;
				case 10:
					if ((debugLevel > 1)) s += "ACTIVITY = ACT_STOP";
					else s = "";
					break;
				case 11:
					s += "Low battery: turning off.";
					break;
				case 12:
					s += "Power off command received: turning off.";
					break;
				case 13:
					s += "Memory full, turning off.";
					break;
				case 14:
					s += "Remote connection.";
					break;
				case 15:
					s += "Maintenance reset. New data on next session...";
					break;
				case 16:
					if ((debugLevel > 1)) s += "MAX7 found OFF during CONT or ALT_ON. Restarted.";
					else s = "";
					break;
				case 17:
					s += "Post-fix Start Delay";
					break;
				case 18:
					s += "Got Time and Position.";
					break;
				case 80:
					if (debugLevel > 2)
					{
						s += "Debug. Fase: " + t.eventAr[1].ToString();
						s += " OnOff: " + (t.eventAr[2] & 1).ToString();
						s += " InWater: " + ((t.eventAr[2] & 2) >> 1).ToString();
						s += " InAdc: " + ((t.eventAr[2] & 4) >> 2).ToString();
						s += " MinutiRtcc: " + t.eventAr[3].ToString();
						s += " SecRtcc: " + t.eventAr[7].ToString();
						s += " N_Ora: " + t.eventAr[9].ToString();
						s += " SecReg: " + t.eventAr[8].ToString();
						s += " ContoNoSignal: " + t.eventAr[10].ToString();
						s += " ContoFixAcq: " + (t.eventAr[11] * 256 + t.eventAr[12]).ToString();
						s += " ContoDop: " + t.eventAr[14].ToString();
						s += " Activity: " + t.eventAr[13].ToString();
					}
					else s = "";
					break;
			}
		}

		private bool pressureDepth5803(ref MemoryStream ard, ref timeStamp tsc)
		{
			double dT;
			double off;
			double sens;
			double d1, d2;

			try
			{
				d2 = ard.ReadByte() * 65536 + ard.ReadByte() * 256 + ard.ReadByte();
			}
			catch
			{
				return true;
			}

			dT = d2 - convCoeffs[4] * 256;
			tsc.temperature = (2000 + (dT * convCoeffs[5]) / 8_388_608);
			off = convCoeffs[1] * 65_536 + (convCoeffs[3] * dT) / 128;
			sens = convCoeffs[0] * 32_768 + (convCoeffs[2] * dT) / 256;
			if (tsc.temperature > 2000)
			{
				tsc.temperature -= (7 * Math.Pow(dT, 2)) / 137_438_953_472;
				off -= ((Math.Pow((tsc.temperature - 2000), 2)) / 16);
			}
			else
			{
				tsc.temperature -= 3 * (Math.Pow(dT, 2)) / 8_589_934_592;
				off -= 3 * ((Math.Pow((tsc.temperature - 2000), 2)) / 2);
				sens -= 5 * ((Math.Pow((tsc.temperature - 2000), 2)) / 8);
				if (tsc.temperature < -1500)
				{
					off -= 7 * Math.Pow((tsc.temperature + 1500), 2);
					sens -= 4 * Math.Pow((tsc.temperature + 1500), 2);
				}
			}
			tsc.temperature = tsc.temperature / 100;
			if ((tsc.tsType & 4) == 4)
			{
				try
				{
					d1 = ard.ReadByte() * 65536 + ard.ReadByte() * 256 + ard.ReadByte();
				}
				catch
				{
					return true;
				}
				tsc.press = (((d1 * sens / 2_097_152) - off) / 81_920);
				if (inMeters)
				{
					tsc.press -= tsc.pressOffset;
					if (tsc.press < 0) tsc.press = 0;
					else
					{
						tsc.press = tsc.press / 98.1;
						//tsc.press = Math.Round(tsc.press, 2);
					}
				}
			}
			return false;
		}

		private bool pressureAir(ref MemoryStream ard, ref timeStamp tsc)
		{
			double dT, off, sens, t2, off2, sens2;
			double d1, d2;

			try
			{
				d2 = ard.ReadByte() * 65536 + ard.ReadByte() * 256 + ard.ReadByte();
			}
			catch
			{
				return true;
			}

			dT = d2 - convCoeffs[4] * 256;
			tsc.temperature = 2000 + (dT * convCoeffs[5]) / 8388608;
			off = convCoeffs[1] * 131072 + (convCoeffs[3] * dT) / 64;
			sens = convCoeffs[0] * 65536 + (convCoeffs[2] * dT) / 128;
			if (tsc.temperature > 2000)
			{
				t2 = 0;
				off2 = 0;
				sens2 = 0;
			}
			else
			{
				t2 = Math.Pow(dT, 2) / 2147483648;
				off2 = 61 * Math.Pow((tsc.temperature - 2000), 2) / 16;
				sens2 = 2 * Math.Pow((tsc.temperature - 2000), 2);
				if (tsc.temperature < -1500)
				{
					off2 += 20 * Math.Pow((tsc.temperature + 1500), 2);
					sens2 += 12 * Math.Pow((tsc.temperature + 1500), 2);
				}
			}
			tsc.temperature -= t2;
			off -= off2;
			sens -= sens2;
			tsc.temperature /= 100;
			//tsc.temp = Math.Round(tsc.temp, 1);

			if ((tsc.tsType & 4) == 4)
			{
				try
				{
					d1 = ard.ReadByte() * 65536 + ard.ReadByte() * 256 + ard.ReadByte();
				}
				catch
				{
					return true;
				}
				tsc.press = ((d1 * sens / 2097152) - off) / 32768;
				tsc.press /= 100;
				//tsc.press = Math.Round(tsc.press, 2);
			}
			return false;
		}

		private bool pressureDepth5837(ref MemoryStream ard, ref timeStamp tsc)
		{
			double dT;
			double off;
			double sens;
			double d1, d2;

			try
			{
				d2 = ard.ReadByte() * 65536 + ard.ReadByte() * 256 + ard.ReadByte();
			}
			catch
			{
				return true;
			}

			dT = d2 - convCoeffs[4] * 256;
			tsc.temperature = 2000 + (dT * convCoeffs[5]) / 8_388_608;
			off = convCoeffs[1] * 65_536 + (convCoeffs[3] * dT) / 128;
			sens = convCoeffs[0] * 32_768 + (convCoeffs[2] * dT) / 256;
			if (tsc.temperature > 2000)
			{
				tsc.temperature -= (2 * Math.Pow(dT, 2)) / 137_438_953_472;
				off -= ((Math.Pow((tsc.temperature - 2000), 2)) / 16);
			}
			else
			{
				tsc.temperature -= 3 * (Math.Pow(dT, 2)) / 8_589_934_592;
				off -= 3 * ((Math.Pow((tsc.temperature - 2000), 2)) / 2);
				sens -= 5 * ((Math.Pow((tsc.temperature - 2000), 2)) / 8);
				if (tsc.temperature < -1500)
				{
					off -= 7 * Math.Pow((tsc.temperature + 1500), 2);
					sens -= 4 * Math.Pow((tsc.temperature + 1500), 2);
				}
			}
			tsc.temperature /= 100;
			//tsc.temp = Math.Round((tsc.temp / 100), 1);
			if ((tsc.tsType & 4) == 4)
			{
				try
				{
					d1 = ard.ReadByte() * 65536 + ard.ReadByte() * 256 + ard.ReadByte();
				}
				catch
				{
					return true;
				}
				tsc.press = (((d1 * sens / 2_097_152) - off) / 81_920);
				if (inMeters)
				{
					tsc.press -= tsc.pressOffset;
					if (tsc.press <= 0) tsc.press = 0;
					else
					{
						tsc.press = tsc.press / 98.1;
						//tsc.press = Math.Round(tsc.press, 2);
					}
				}
			}
			return false;
		}

		private void csvPlaceHeader(ref BinaryWriter csv)
		{
			string csvHeader = "TagID";
			if (sameColumn)
			{
				csvHeader = csvHeader + csvSeparator + "Timestamp";
			}
			else
			{
				csvHeader = csvHeader + csvSeparator + "Date" + csvSeparator + "Time";
			}

			csvHeader = csvHeader + csvSeparator + "X" + csvSeparator + "Y" + csvSeparator + "Z";
			csvHeader = csvHeader + csvSeparator + "Activity";
			if (pressureEnabled > 0)
			{
				if (inMeters)
				{
					if (isDepth == 1)
					{
						csvHeader = csvHeader + csvSeparator + "Depth";
					}
					else
					{
						csvHeader = csvHeader + csvSeparator + "Altitude";
					}

				}
				else
				{
					csvHeader = csvHeader + csvSeparator + "Pressure";
				}
			}
			if (temperatureEnabled > 0)
			{
				csvHeader += csvSeparator + "Temp. (°C)";
			}

			csvHeader += csvSeparator + "location-lat" + csvSeparator + "location-lon" + csvSeparator + "height-msl"
				+ csvSeparator + "ground-speed" + csvSeparator + "satellites" + csvSeparator + "hdop" + csvSeparator + "signal-strength";
			if (adcLog) csvHeader += csvSeparator + "Sensor Raw";
			if (adcStop) csvHeader += csvSeparator + "Sensor State";
			if (prefBattery) csvHeader += csvSeparator + "Battery (V)";
			if (metadata) csvHeader += csvSeparator + "Metadata";

			csvHeader += "\r\n";

			csv.Write(System.Text.Encoding.ASCII.GetBytes(csvHeader));
		}

		private bool detectEof(ref MemoryStream ard)
		{
			if (ard.Position >= ard.Length) return true;
			else return false;
		}

		protected override void Dispose(bool disposing)
		{
			if (!disposed)
			{
				if (disposing)
				{
					//Release managed resources.
				}
				// Release unmanaged resources.
				// Set large fields to null.
				disposed = true;
			}
			base.Dispose(disposing);
		}

	}
}
