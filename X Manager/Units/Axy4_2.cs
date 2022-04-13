using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using System.Threading;
using System.Runtime.InteropServices;
#if X64
using FT_HANDLE = System.UInt64;
#else
using FT_HANDLE = System.UInt32;
#endif

namespace X_Manager.Units
{
	class Axy4_2 : Units.Unit
	{
		//bool evitaSoglie = false;
		bool disposed = false;
		new byte[] firmwareArray = new byte[6];
		struct timeStamp
		{
			public byte tsType, tsTypeExt1, tsTypeExt2;
			public float batteryLevel;
			public double temperature;
			public DateTime orario;
			public byte stopEvent;
			public byte timeStampLength;
			public double adcVal;
			public double magX_A, magY_A, magZ_A, magX_B, magY_B, magZ_B;
			//public long ardPosition;
		}

		byte dateFormat;
		byte timeFormat;
		bool sameColumn = false;
		bool prefBattery = false;
		bool repeatEmptyValues = true;
		bool bits;
		byte bitsDiv;
		bool angloTime = false;
		ushort rate;
		ushort rateComp;
		byte range;
		//uint sogliaNeg;
		//uint rendiNeg;
		double gCoeff;
		string dateFormatParameter;
		ushort addMilli;
		CultureInfo dateCi;
		//byte cifreDec;
		string cifreDecString;
		bool metadata;
		bool overrideTime;
		string ardPos = "";
		int magen;
		int adcEn = 0;
		double[] magData_A = new double[3];
		double[] magData_B = new double[3];

		//double mediaFreq = 0;    //sviluppo
		//double contoFreq = 0;     //sviluppo

		public Axy4_2(object p)
			: base(p)
		{
			base.positionCanSend = false;
			configurePositionButtonEnabled = false;
			modelCode = model_axy4;
			modelName = "Axy-4";
		}

		public override string askFirmware()
		{
			byte[] f = new byte[3];
			string firmware = "";

			sp.ReadExisting();
			sp.Write("TTTTTTTGGAF");
			sp.ReadTimeout = 400;
			int i = 0;
			try
			{
				for (i = 0; i < 3; i++) f[i] = (byte)sp.ReadByte();
			}
			catch
			{
				if (i > 0)
				{
					Array.Resize(ref f, 2);
				}
				else
				{
					throw new Exception(unitNotReady);
				}
			}

			firmTotA = 0;
			for (i = 0; i <= (f.Length - 1); i++)
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

		public override void setPcTime()
		{
			if (firmTotA < 2006000) return;

			sp.Write("TTTTTTTTGGAt");
			try
			{
				sp.ReadByte();
				byte[] dateAr = new byte[6];
				var dateToSend = DateTime.UtcNow;
				dateAr[0] = (byte)dateToSend.Second;
				dateAr[1] = (byte)dateToSend.Minute;
				dateAr[2] = (byte)dateToSend.Hour;
				dateAr[3] = (byte)dateToSend.Day;
				dateAr[4] = (byte)dateToSend.Month;
				dateAr[5] = (byte)(dateToSend.Year - 2000);
				sp.Write(dateAr, 0, 6);
				sp.ReadByte();
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
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
			battery = Math.Round(battLevel, 2).ToString("0.00") + "V";
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
			sp.Write("TTTTTTTTTGGAE");
			try
			{
				sp.ReadByte();
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
		}

		public override bool getRemote()
		{
			if (firmTotA < 2007000)
			{
				return false;
			}
			sp.Write("TTTTTTTTTTGGAl");
			try
			{
				if (sp.ReadByte() == 1) remote = true;
			}
			catch { throw new Exception(unitNotReady); }
			return remote;
		}

		public override byte[] getConf()
		{
			byte[] conf = new byte[30];
			for (byte i = 0; i < 30; i++) conf[i] = 0xff;

			conf[25] = modelCode;

			sp.ReadExisting();
			sp.Write("TTTTTTTTTTTTGGAC");
			try
			{
				for (int i = 15; i <= 17; i++)
				{
					conf[i] = (byte)sp.ReadByte();
				}
				if (firmTotA >= 3000000)
				{
					if (firmTotA >= 3001000)
					{
						conf[19] = (byte)sp.ReadByte();
					}
					conf[21] = (byte)sp.ReadByte();
					if (firmTotA >= 3002000)
					{
						conf[22] = (byte)sp.ReadByte();
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
			sp.ReadExisting();
			sp.Write("TTTTTTTTTTTTGGAc");
			try
			{
				sp.ReadByte();
				sp.Write(conf, 15, 3);
				if (firmTotA >= 3000000)
				{
					if (firmTotA >= 3001000)
					{
						sp.Write(conf, 19, 1);
					}
					sp.Write(conf, 21, 1);
					if (firmTotA >= 3002000)
					{
						sp.Write(conf, 22, 1);
					}
				}
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
				for (int i = newName.Length; i <= 9; i++) newName += " ";
			}
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

		public override void disconnect()
		{
			base.disconnect();
			if (remote)
			{
				sp.Write("TTTTTTTTTGGAW");
			}
			else
			{
				sp.Write("TTTTTTTTTGGAO");
			}

		}

		public unsafe override void download(string fileName, uint fromMemory, uint toMemory, int baudrate)
		{
			convertStop = false;
			uint actMemory = fromMemory;
			System.IO.FileMode fm = System.IO.FileMode.Create;
			if (fromMemory != 0) fm = System.IO.FileMode.Append;
			string fileNameMdp = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + ".mdp";
			var fo = new System.IO.BinaryWriter(File.Open(fileNameMdp, fm));

			sp.Write("TTTTTTTTTTTTGGAe");
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

			Thread.Sleep(70);
			byte b = (byte)(baudrate / 1000000);
			sp.Write(new byte[] { b }, 0, 1);
			sp.BaudRate = baudrate;

			Thread.Sleep(200);
			if (firmTotA < 2006004)
			{
				Thread.Sleep(750);
			}

			sp.Write("S");
			Thread.Sleep(200);

			int dieCount = 0;
			try
			{
				dieCount = sp.ReadByte();
				if (dieCount == 0x53) dieCount = 2;
				if (dieCount == 0x73) dieCount = 1;
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
			byte[] tempBuffer = new byte[2048];
			byte[] address = new byte[8];
			byte[] fileBuffer = new byte[toMemory];
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

				fixed (byte* outP = outBuffer, inP = &fileBuffer[actMemory])
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
					fo.Write(fileBuffer, 0, (int)actMemory);
					fo.Close();
					return;
				}
				else if (bytesReturned != 4096)
				{
					firstLoop = true;
				}
				else
				{
					actMemory += 4096;      //Prima di scrivere incrementa l'indirizzo per capire se sono state scaricate le ultime pagine di un blocco

					if ((actMemory % 0x20000) == 0)
					{
						if ((actMemory % 0x40000) == 0)
						{
							firstLoop = true;
						}
						if (dieCount == 2)      //Nel caso le riscarica singolarmente, per ovviare al bug della memoria dual die
						{
							actMemory -= 4096;
							for (int i = 0; i < 2; i++)
							{
								address = BitConverter.GetBytes(actMemory);
								Array.Reverse(address);
								Array.Copy(address, 0, outBuffer, 1, 3);
								outBuffer[0] = 97;
								bytesToWrite = 4;
								fixed (byte* outP = outBuffer, inP = &fileBuffer[actMemory])
								{
									FT_Status = MainWindow.FT_Write(FT_Handle, outP, bytesToWrite, ref bytesWritten);
									FT_Status = MainWindow.FT_Read(FT_Handle, inP, 2048, ref bytesReturned);
								}
								actMemory += 2048;
							}
							firstLoop = true;
						}
					}
				}

				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Value = actMemory));

				if (convertStop) actMemory = toMemory;
			}

			fo.Write(fileBuffer);
			fo.Write(firmwareArray, 0, firmwareArray.Length);
			for (int i = firmwareArray.Length; i <= 2; i++)
			{
				fo.Write(new byte[] { 0xff }, 0, 1);
			}
			fo.Write(new byte[] { model_axy4, (byte)254 }, 0, 2);

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
		//public unsafe override void download(string fileName, uint fromMemory, uint toMemory, int baudrate)
		//{
		//	convertStop = false;
		//	uint actMemory = fromMemory;
		//	System.IO.FileMode fm = System.IO.FileMode.Create;
		//	if (fromMemory != 0) fm = System.IO.FileMode.Append;
		//	string fileNameMdp = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + ".mdp";
		//	var fo = new System.IO.BinaryWriter(File.Open(fileNameMdp, fm));

		//	sp.Write("TTTTTTTTTTTTGGAe");
		//	try
		//	{
		//		sp.ReadByte();
		//	}
		//	catch
		//	{
		//		Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFailed()));
		//		try
		//		{
		//			fo.Close();
		//		}
		//		catch { }
		//		return;
		//	}

		//	Thread.Sleep(70);
		//	byte b = (byte)(baudrate / 1000000);
		//	sp.Write(new byte[] { b }, 0, 1);
		//	sp.BaudRate = baudrate;

		//	Thread.Sleep(200);
		//	if (firmTotA < 2006004)
		//	{
		//		Thread.Sleep(750);
		//	}

		//	sp.Write("S");
		//	Thread.Sleep(200);

		//	int dieCount = 0;
		//	try
		//	{
		//		dieCount = sp.ReadByte();
		//		if (dieCount == 0x53) dieCount = 2;
		//		if (dieCount == 0x73) dieCount = 1;
		//		if ((dieCount != 1) & (dieCount != 2))
		//		{
		//			throw new Exception(unitNotReady);
		//		}
		//	}
		//	catch
		//	{
		//		Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFailed()));
		//		try
		//		{
		//			fo.Close();
		//		}
		//		catch { }
		//		return;
		//	}


		//	Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.progressBarStopButton.IsEnabled = true));
		//	Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.progressBarStopButtonColumn.Width = new GridLength(80)));

		//	Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.IsIndeterminate = false));
		//	Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Minimum = 0));
		//	Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Maximum = toMemory));
		//	Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Value = fromMemory));

		//	//Passa alla gestione FTDI D2XX
		//	sp.Close();

		//	MainWindow.FT_STATUS FT_Status;
		//	FT_HANDLE FT_Handle = 0;
		//	byte[] outBuffer = new byte[50];
		//	byte[] inBuffer = new byte[4096];
		//	byte[] tempBuffer = new byte[2048];
		//	byte[] address = new byte[8];

		//	uint bytesToWrite = 0, bytesWritten = 0, bytesReturned = 0;

		//	FT_Status = MainWindow.FT_OpenEx(parent.ftdiSerialNumber, (UInt32)1, ref FT_Handle);
		//	if (FT_Status != MainWindow.FT_STATUS.FT_OK)
		//	{
		//		MainWindow.FT_Close(FT_Handle);
		//		Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFailed()));
		//		try
		//		{
		//			fo.Close();
		//		}
		//		catch { }
		//		return;
		//	}

		//	MainWindow.FT_SetLatencyTimer(FT_Handle, (byte)1);
		//	MainWindow.FT_SetTimeouts(FT_Handle, (uint)1000, (uint)1000);

		//	bool firstLoop = true;


		//	while (actMemory < toMemory)
		//	{
		//		if (((actMemory % 0x2000000) == 0) | (firstLoop))
		//		{
		//			address = BitConverter.GetBytes(actMemory);
		//			Array.Reverse(address);
		//			Array.Copy(address, 0, outBuffer, 1, 3);
		//			outBuffer[0] = 65;
		//			bytesToWrite = 4;
		//			firstLoop = false;
		//		}
		//		else
		//		{
		//			outBuffer[0] = 79;
		//			bytesToWrite = 1;
		//		}
		//		fixed (byte* outP = outBuffer, inP = inBuffer)
		//		{
		//			FT_Status = MainWindow.FT_Write(FT_Handle, outP, bytesToWrite, ref bytesWritten);
		//			FT_Status = MainWindow.FT_Read(FT_Handle, inP, 4096, ref bytesReturned);
		//		}

		//		if (FT_Status != MainWindow.FT_STATUS.FT_OK)
		//		{
		//			outBuffer[0] = 88;
		//			fixed (byte* outP = outBuffer) { FT_Status = MainWindow.FT_Write(FT_Handle, outP, bytesToWrite, ref bytesWritten); }
		//			MainWindow.FT_Close(FT_Handle);
		//			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFailed()));
		//			fo.Write(inBuffer);
		//			fo.Close();
		//			return;
		//		}
		//		else if (bytesReturned != 4096)
		//		{
		//			firstLoop = true;
		//		}
		//		else
		//		{
		//			actMemory += 4096;
		//			if ((actMemory % 0x20000) == 0)
		//			{
		//				if (dieCount == 2)
		//				{
		//					actMemory -= 4096;
		//					for (int i = 0; i < 2; i++)
		//					{
		//						address = BitConverter.GetBytes(actMemory);
		//						Array.Reverse(address);
		//						Array.Copy(address, 0, outBuffer, 1, 3);
		//						outBuffer[0] = 97;
		//						bytesToWrite = 4;
		//						fixed (byte* outP = outBuffer, inP = inBuffer)
		//						{
		//							FT_Status = MainWindow.FT_Write(FT_Handle, outP, bytesToWrite, ref bytesWritten);
		//							FT_Status = MainWindow.FT_Read(FT_Handle, inP, (uint)2048, ref bytesReturned);
		//						}
		//						fo.Write(inBuffer, 0, 2048);
		//						actMemory += 2048;
		//					}
		//					firstLoop = true;
		//				}
		//				else
		//				{
		//					fo.Write(inBuffer, 0, 4096);
		//					if ((actMemory % 0x40000) == 0)
		//					{
		//						firstLoop = true;
		//					}
		//				}

		//			}
		//			else
		//			{
		//				fo.Write(inBuffer, 0, 4096);
		//			}
		//		}

		//		Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Value = actMemory));

		//		if (convertStop) actMemory = toMemory;
		//	}

		//	fo.Write(firmwareArray, 0, firmwareArray.Length);
		//	for (int i = firmwareArray.Length; i <= 2; i++)
		//	{
		//		fo.Write(new byte[] { 0xff }, 0, 1);
		//	}
		//	fo.Write(new byte[] { model_axy4, (byte)254 }, 0, 2);

		//	fo.Close();
		//	outBuffer[0] = 88;
		//	bytesToWrite = 1;
		//	fixed (byte* outP = outBuffer)
		//	{
		//		FT_Status = MainWindow.FT_Write(FT_Handle, outP, bytesToWrite, ref bytesWritten);
		//	}
		//	MainWindow.FT_Close(FT_Handle);
		//	sp.BaudRate = 115200;
		//	sp.Open();
		//	if (!convertStop) extractArds(fileNameMdp, fileName, true);
		//	else
		//	{
		//		if (Parent.lastSettings[6].Equals("false"))
		//		{
		//			try
		//			{
		//				fDel(fileNameMdp);
		//			}
		//			catch { }
		//		}
		//	}

		//	Thread.Sleep(300);
		//	Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFinished()));
		//}

		public unsafe override void downloadRemote(string fileName, uint fromMemory, uint toMemory, int baudrate)
		{
			convertStop = false;
			uint actMemory = fromMemory;
			System.IO.FileMode fm = System.IO.FileMode.Create;
			if (fromMemory != 0) fm = System.IO.FileMode.Append;
			string fileNameMdp = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + ".mdp";
			var fo = new BinaryWriter(File.Open(fileNameMdp, fm));

			byte mdrSpeed = 9;
			//mdrSpeed=8;
			string br = "D";
			if (mdrSpeed == 9) br = "H";
			sp.Write("TTTTTTTTTTTTTTGGA" + br);
			int dieCount = 0;
			try
			{
				sp.ReadByte();
				Thread.Sleep(10);
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
				Thread.Sleep(100);
				dieCount = sp.ReadByte();
				if (dieCount == 0x53) dieCount = 2;
				if (dieCount == 0x73) dieCount = 1;
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
					if (((actMemory % 0x20000) == 0))
					{
						if (dieCount == 2)
						{
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

			fo.Write(firmwareArray, 0, firmwareArray.Length);
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

			Thread.Sleep(600);
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFinished()));
		}

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
							mdp.BaseStream.Position -= 5;
							firmwareArray = mdp.ReadBytes(3);
						}
						if (firmwareArray[2] == 0xff) Array.Resize(ref firmwareArray, 2);
						mdp.BaseStream.Position = oldPosition;
					}
					ard.Write(firmwareArray, 0, firmwareArray.Length);
					ard.Write(mdp.ReadBytes(254));
				}
				else if (testByte == 0xff)
				{
					if (counter != 0)
					{
						break;
					}
				}
				else
				{
					ard.Write(mdp.ReadBytes(255));
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
						if (System.IO.File.Exists(newFileNameMdp))
						{
							fDel(newFileNameMdp);
						}
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

			timeStamp timeStampO = new timeStamp();
			string barStatus = "";
			string shortFileName;
			string addOn = "";
			string exten = Path.GetExtension(fileName);
			if ((exten.Length > 4)) addOn = ("_S" + exten.Remove(0, 4));
			string fileNameCsv = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + addOn + ".csv";
			BinaryReader ard = new System.IO.BinaryReader(System.IO.File.Open(fileName, FileMode.Open));
			BinaryWriter csv = new System.IO.BinaryWriter(System.IO.File.OpenWrite(fileNameCsv));

			ard.BaseStream.Position = 1;
			firmTotA = (uint)(ard.ReadByte() * 1000 + ard.ReadByte());
			if (firmTotA > 2004)
			{
				firmTotA *= 1000; firmTotA += ard.ReadByte();
			}

			//Imposta le preferenze di conversione
			debugLevel = parent.stDebugLevel;

			if ((prefs[pref_fillEmpty] == "False")) repeatEmptyValues = false;

			dateSeparator = csvSeparator;
			if ((prefs[pref_sameColumn] == "True"))
			{
				sameColumn = true;
				dateSeparator = " ";
			}

			if (prefs[pref_battery] == "True") prefBattery = true;

			dateCi = new CultureInfo("it-IT");
			angloTime = false;
			if (prefs[pref_timeFormat] == "2")
			{
				angloTime = true;
				dateCi = new CultureInfo("en-US");
			}

			dateFormat = byte.Parse(prefs[pref_dateFormat]);
			timeFormat = byte.Parse(prefs[pref_timeFormat]);
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

			overrideTime = false;
			if (prefs[pref_override_time] == "True") overrideTime = true;

			metadata = false;
			if (prefs[pref_metadata] == "True") metadata = true;
			if (debugLevel == 3) metadata = true;


			//Legge i parametri di logging
			ard.ReadByte();

			byte rrb = ard.ReadByte();
			rate = findSamplingRate(rrb);
			rateComp = rate;
			if (rate == 1 & firmTotA < 3001000) rateComp = 10;
			range = findRange(rrb);
			bits = findBits(rrb);
			bitsDiv = findBytesPerSample();
			if (firmTotA >= 3000000)
			{
				magen = ard.ReadByte();
			}
			if (firmTotA >= 3002000)
			{
				adcEn = ard.ReadByte();
			}

			Array.Resize(ref lastGroup, ((rateComp * 3)));
			nOutputs = rateComp;

			//cifreDec = 3;
			cifreDecString = "0.000";
			if (bits)
			{
				//cifreDec = 4;
				cifreDecString = "0.0000";
			}
			timeStampO.orario = findStartTime(ref prefs);

			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => barStatus = (string)parent.statusLabel.Content));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.IsIndeterminate = false));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
				new Action(() => parent.statusLabel.Content = barStatus + " - Converting"));

			shortFileName = Path.GetFileNameWithoutExtension(fileName);

			convertStop = false;

			ard.ReadByte();

			csvPlaceHeader(ref csv);

			//Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
			//	new Action(() => parent.statusProgressBar.Maximum = ard.BaseStream.Length - 1));
			progMax = ard.BaseStream.Length - 1;
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
				new Action(() => parent.statusProgressBar.Maximum = ard.BaseStream.Length - 1));
			progressWorker.RunWorkerAsync();

			while (!convertStop)
			{
				//Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
				//			new Action(() => parent.statusProgressBar.Value = ard.BaseStream.Position));
				while (Interlocked.Exchange(ref progLock, 2) > 0) { }

				progVal = ard.BaseStream.Position;
				Interlocked.Exchange(ref progLock, 0);
				if (detectEof(ref ard)) break;

				decodeTimeStamp(ref ard, ref timeStampO);

				if (timeStampO.stopEvent > 0)
				{
					csv.Write(Encoding.ASCII.GetBytes(groupConverter(ref timeStampO, lastGroup, shortFileName)));
					break;
				}

				try
				{
					csv.Write(Encoding.ASCII.GetBytes(
						groupConverter(ref timeStampO, extractGroup(ref ard, ref timeStampO), shortFileName)));
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.ToString());
				}

			}

			//sviluppo
			//mediaFreq = mediaFreq / contoFreq;
			//MessageBox.Show(mediaFreq.ToString());
			///sviluppo
			while (Interlocked.Exchange(ref progLock, 2) > 0) { }
			progVal = ard.BaseStream.Position;
			Thread.Sleep(300);
			progVal = -1;
			Interlocked.Exchange(ref progLock, 0);
			//Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Value = ard.BaseStream.Position));

			csv.Close();
			ard.Close();

			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.nextFile()));
		}

		private double[] extractGroup(ref BinaryReader ard, ref timeStamp tsc)
		{
			byte[] group = new byte[2000];
			bool badGroup = false;
			long position = 0;
			byte dummy, dummyExt;
			ushort badPosition = 600;

			if (ard.BaseStream.Position == ard.BaseStream.Length) return lastGroup;

			do
			{
				dummy = ard.ReadByte();
				if (dummy == 0xab)
				{
					if (ard.BaseStream.Position < ard.BaseStream.Length) dummyExt = ard.ReadByte();
					else return lastGroup;

					if (dummyExt == 0xab)
					{
						group[position] = (byte)0xab;
						position += 1;
						dummy = 0;
					}
					else if (dummyExt == 0xac)
					{
						if (ard.BaseStream.Position < ard.BaseStream.Length - 6)
						{
							tsc.magX_B = ard.ReadByte();
							tsc.magX_B += (ard.ReadByte() * 256);
							if (tsc.magX_B > 32767)
							{
								tsc.magX_B -= 65536;
							}
							tsc.magX_B *= 1.5;

							tsc.magY_B = ard.ReadByte();
							tsc.magY_B += (ard.ReadByte() * 256);
							if (tsc.magY_B > 32767)
							{
								tsc.magY_B -= 65536;
							}
							tsc.magY_B *= 1.5;

							tsc.magZ_B = ard.ReadByte();
							tsc.magZ_B += (ard.ReadByte() * 256);
							if (tsc.magZ_B > 32767)
							{
								tsc.magZ_B -= 65536;
							}
							tsc.magZ_B *= 1.5;
						}
						dummy = 0;
					}
					else
					{
						ard.BaseStream.Position -= 1;
						if (badGroup)
						{
							File.AppendAllText(((FileStream)ard.BaseStream).Name + "errorList.txt", "-> " + ard.BaseStream.Position.ToString("X8") + "\r\n");
						}
					}
				}
				else
				{
					if (position < badPosition)
					{
						group[position] = dummy;
						position++;
					}
					else if ((position == badPosition) && (!badGroup))
					{
						badGroup = true;
						System.IO.File.AppendAllText(((FileStream)ard.BaseStream).Name + "errorList.txt", "-> " + ard.BaseStream.Position.ToString("X8") + "\r\n");
					}
				}
			} while ((dummy != (byte)0xab) && (ard.BaseStream.Position < ard.BaseStream.Length));

			//Array.Resize(ref group, (int)position);

			tsc.timeStampLength = (byte)(position / bitsDiv);
			//sviluppo
			//contoFreq++;
			//mediaFreq += tsc.timeStampLength;
			///sviluppo

			//IntPtr doubleResultArray = Marshal.AllocCoTaskMem(sizeof(double) * nOutputs * 3);
			int resultCode = 0;
			double[] doubleResultArray = new double[nOutputs * 3];
			if (bits)
			{
				resultCode = resample4(group, (int)tsc.timeStampLength, doubleResultArray, nOutputs);
			}
			else
			{
				resultCode = resample3(group, (int)tsc.timeStampLength, doubleResultArray, nOutputs);
			}
			//doubleResult = new double[(nOutputs * 3)];
			//Marshal.Copy(doubleResultArray, doubleResult, 0, nOutputs * 3);
			//Marshal.FreeCoTaskMem(doubleResultArray);
			return doubleResultArray;
		}

		private string groupConverter(ref timeStamp tsLoc, double[] group, string unitName)
		{
			if (group.Length == 0) return ("");

			double x, y, z;
			string ampm = "";
			string textOut, dateTimeS, additionalInfo, magAdditionalInfo;
			string dateS = "";
			ushort milli;
			NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;
			ushort contoTab = 0;

			dateS = tsLoc.orario.ToString(dateFormatParameter);

			dateTimeS = dateS + dateSeparator + tsLoc.orario.ToString("T", dateCi);
			if (angloTime)
			{
				ampm = dateTimeS.Split(' ')[dateTimeS.Split(' ').Length - 1];
				dateTimeS = dateTimeS.Remove(dateTimeS.Length - 3, 3);
			}

			milli = 0;

			textOut = "";

			x = group[0]; y = group[1]; z = group[2];

			x *= gCoeff; //x = Math.Round(x, cifreDec);
			y *= gCoeff; //y = Math.Round(y, cifreDec);
			z *= gCoeff; //z = Math.Round(z, cifreDec);

			textOut = unitName + csvSeparator + dateTimeS + ".000";
			if (angloTime) textOut += " " + ampm;
			textOut += csvSeparator + x.ToString(cifreDecString, nfi) + csvSeparator + y.ToString(cifreDecString, nfi) + csvSeparator + z.ToString(cifreDecString, nfi);

			magAdditionalInfo = "";
			additionalInfo = "";

			contoTab = 0;

			//Inserisce il magnetometro
			if (magen > 0)
			{
				//contoTab += 3;
				if (((tsLoc.tsTypeExt1 & 8) == 8) | repeatEmptyValues)
				{
					magAdditionalInfo += csvSeparator + tsLoc.magX_A.ToString("#.0", nfi);
					magAdditionalInfo += csvSeparator + tsLoc.magY_A.ToString("#.0", nfi);
					magAdditionalInfo += csvSeparator + tsLoc.magZ_A.ToString("#.0", nfi);
				}
				else
				{
					magAdditionalInfo += csvSeparator + csvSeparator + csvSeparator;
				}
			}

			contoTab += 1;
			additionalInfo += csvSeparator;
			if (((tsLoc.tsType & 2) == 2) | repeatEmptyValues) additionalInfo += tsLoc.temperature.ToString(nfi);

			//Inserisce l'adc
			if (adcEn > 0)
			{
				contoTab++;
				additionalInfo += csvSeparator;
				if (((tsLoc.tsTypeExt1 & 2) == 2) | repeatEmptyValues) additionalInfo += tsLoc.adcVal.ToString(nfi);
			}

			//Inserisce la batteria
			if (prefBattery)
			{
				contoTab += 1;
				additionalInfo += csvSeparator;
				if (((tsLoc.tsType & 8) == 8) | repeatEmptyValues) additionalInfo += tsLoc.batteryLevel.ToString(nfi);
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
					}
					textOut += magAdditionalInfo + additionalInfo + ardPos + "\r\n";
					return textOut;
				}

			}

			textOut += magAdditionalInfo + additionalInfo + ardPos + "\r\n";

			if (tsLoc.stopEvent > 0) return textOut;
			if (rate == 1)
			{
				if (firmTotA >= 3000000)
				{
					return textOut;
				}
				tsLoc.orario = tsLoc.orario.AddSeconds(1);
				dateTimeS = dateS + csvSeparator + tsLoc.orario.ToString("T", dateCi);
			}

			if (!repeatEmptyValues)
			{
				if (magen > 0)
				{
					magAdditionalInfo = csvSeparator + csvSeparator + csvSeparator;
				}
				additionalInfo = "";
				for (ushort ui = 0; ui < contoTab; ui++) additionalInfo += csvSeparator;
			}

			milli += addMilli;
	
			dateTimeS += ".";
			if (tsLoc.stopEvent > 0) bitsDiv = 1;

			//var iend1 = (short)((rateComp * 3) / 2);
			//var iend2 = (short)(rateComp * 3);
			//var iend1 = (((iend2 / 2) / 3) + 1) * 3;
			var iend2 = (short)(rateComp * 3);
			var iend1 = iend2 / 2;
			iend1 -= (iend1 % 3);

			for (short i = 3; i < iend1; i += 3)
			{
				x = group[i];
				y = group[i + 1];
				z = group[i + 2];

				x *= gCoeff; //x = Math.Round(x, cifreDec);
				y *= gCoeff; //y = Math.Round(y, cifreDec);
				z *= gCoeff; //z = Math.Round(z, cifreDec);


				textOut += unitName + csvSeparator + dateTimeS + milli.ToString("D3");

				if (angloTime) textOut += " " + ampm;
				textOut += csvSeparator + x.ToString(cifreDecString, nfi) + csvSeparator + y.ToString(cifreDecString, nfi) + csvSeparator + z.ToString(cifreDecString, nfi);

				textOut += magAdditionalInfo + additionalInfo + "\r\n";
				milli += addMilli;
				if (rate == 1)
				{
					tsLoc.orario = tsLoc.orario.AddSeconds(1);
					dateTimeS = dateS + csvSeparator + tsLoc.orario.ToString("T", dateCi) + ".";
				}
			}

			if (rateComp == 1) return textOut;

			x = group[iend1]; y = group[iend1 + 1]; z = group[iend1 + 2];

			x *= gCoeff; //x = Math.Round(x, cifreDec);
			y *= gCoeff; //y = Math.Round(y, cifreDec);
			z *= gCoeff; //z = Math.Round(z, cifreDec);

			textOut += unitName + csvSeparator + dateTimeS + milli.ToString("D3");
			if (angloTime) textOut += " " + ampm;
			textOut += csvSeparator + x.ToString(cifreDecString, nfi) + csvSeparator + y.ToString(cifreDecString, nfi) + csvSeparator + z.ToString(cifreDecString, nfi);

			if (magen == 2)
			{
				magAdditionalInfo = csvSeparator + tsLoc.magX_B.ToString("#.0", nfi);
				magAdditionalInfo += csvSeparator + tsLoc.magY_B.ToString("#.0", nfi);
				magAdditionalInfo += csvSeparator + tsLoc.magZ_B.ToString("#.0", nfi);
			}

			textOut += magAdditionalInfo + additionalInfo + ardPos + "\r\n";
			if (!repeatEmptyValues)
			{
				if (magen > 0)
				{
					magAdditionalInfo = csvSeparator + csvSeparator + csvSeparator;
				}
			}
			milli += addMilli;

			for (int i = iend1 + 3; i < iend2; i += 3)
			{
				x = group[i];
				y = group[i + 1];
				z = group[i + 2];

				x *= gCoeff; //x = Math.Round(x, cifreDec);
				y *= gCoeff; //y = Math.Round(y, cifreDec);
				z *= gCoeff; //z = Math.Round(z, cifreDec);

				if (rate == 1)
				{
					tsLoc.orario = tsLoc.orario.AddSeconds(1);
					dateTimeS = dateS + csvSeparator + tsLoc.orario.ToString("T", dateCi) + ".";
				}
				textOut += unitName + csvSeparator + dateTimeS + milli.ToString("D3");

				if (angloTime) textOut += " " + ampm;
				textOut += csvSeparator + x.ToString(cifreDecString, nfi) + csvSeparator + y.ToString(cifreDecString, nfi) + csvSeparator + z.ToString(cifreDecString, nfi);

				textOut += magAdditionalInfo + additionalInfo + "\r\n";
				milli += addMilli;
			}


			return textOut;
		}

		private DateTime findStartTime(ref string[] prefs)
		{
			const int pref_h = 9;
			const int pref_m = 10;
			const int pref_s = 11;
			const int pref_date_year = 12;
			const int pref_date_month = 13;
			const int pref_date_day = 14;

			DateTime dt = new DateTime(int.Parse(prefs[pref_date_year]), int.Parse(prefs[pref_date_month]), int.Parse(prefs[pref_date_day]),
				int.Parse(prefs[pref_h]), int.Parse(prefs[pref_m]), int.Parse(prefs[pref_s]));

			dt.AddSeconds(29);

			return dt;
		}

		private ushort findSamplingRate(byte rateIn)
		{
			byte rateOut;
			rateIn = (byte)(rateIn >> 4);
			if (rateIn > 7) rateIn -= 8;

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

			if (rateOut == 1) addMilli = 0;

			else addMilli = (ushort)((1 / (double)rateOut) * 1000);

			return rateOut;


		}

		private byte findRange(byte rangeIn)
		{
			rangeIn &= 15;
			if (rangeIn > 7) rangeIn -= 8;

			byte rangeOut = (byte)(Math.Pow(2, (rangeIn + 1)));

			return rangeOut;
		}

		private bool findBits(byte bitsIn)
		{
			bitsIn &= 15;
			//sogliaNeg = 127;
			//rendiNeg = 256;
			if (bitsIn < 8)
			{
				switch (range)
				{
					case 2:
						gCoeff = 0.01563;
						break;
					case 4:
						gCoeff = 0.03126;
						break;
					case 8:
						gCoeff = 0.06252;
						break;
					case 16:
						gCoeff = 0.18758;
						break;
				}
				return false;
			}
			else
			{
				switch (range)
				{
					case 2:
						gCoeff = 0.0039;
						break;
					case 4:
						gCoeff = 0.00782;
						break;
					case 8:
						gCoeff = 0.01563;
						break;
					case 16:
						gCoeff = 0.0469;
						break;
				}
				return true;
			}
		}

		private byte findBytesPerSample()
		{
			byte bitsDiv = 3;
			if (bits) bitsDiv = 4;
			return bitsDiv;
		}

		private void findAdcEn()
		{

		}
		private byte convertTimeStamp(byte tsType)
		{
			if (tsType > 0xfc) return tsType;

			byte tsTypeOut = 0;
			if (tsType > 9)
			{
				tsType -= 10;
				tsTypeOut += 8;
			}
			switch (tsType)
			{
				case 1:
					tsTypeOut += 2;
					break;
				case 3:
					tsTypeOut += 6;
					break;
				case 5:
					tsTypeOut += 2;
					break;
				case 6:
					tsTypeOut += 4;
					break;
			}
			return tsTypeOut;
		}

		private void decodeTimeStamp(ref BinaryReader ard, ref timeStamp tsc)
		{
			if (debugLevel == 3) ardPos = "  " + ard.BaseStream.Position.ToString("X");

			tsc.stopEvent = 0;
			tsc.tsType = ard.ReadByte();
			tsc.tsTypeExt1 = 0;

			if (firmTotA < 2000)
			{
				tsc.tsType = convertTimeStamp(tsc.tsType);
			}

			if (tsc.tsType >= 0xfd)
			{
				switch (tsc.tsType)
				{
					case 0xfd:
						tsc.stopEvent = 3;
						break;
					case 0xfe:
						tsc.stopEvent = 1;
						break;
					case 0xff:
						tsc.stopEvent = 2;
						break;
				}
				return;
			}

			if ((tsc.tsType & ts_ext1) == ts_ext1)
			{
				try
				{
					tsc.tsTypeExt1 = ard.ReadByte();
					if ((tsc.tsTypeExt1 & ts_ext2) == ts_ext2)
					{
						tsc.tsTypeExt2 = ard.ReadByte();
					}
				}
				catch
				{
					return;
				}
			}

			if ((tsc.tsType & ts_temperature) == ts_temperature)
			{
				try
				{
					tsc.temperature = ard.ReadByte() * 256 + ard.ReadByte();
				}
				catch
				{
					return;
				}
				if (((int)tsc.temperature & 0xff) == 0)
				{
					tsc.temperature /= 256;
					if (tsc.temperature > 127) tsc.temperature -= 256;  //x firmware su board axy5
					tsc.temperature += 25;
				}
				else
				{
					if (tsc.temperature > 511) tsc.temperature -= 1024;
					tsc.temperature = Math.Round(((tsc.temperature * 0.1221) + 22.5), 2, MidpointRounding.AwayFromZero);
				}
			}


			if ((tsc.tsType & ts_battery) == ts_battery)
			{
				tsc.batteryLevel = (float)Math.Round(((((float)((ard.ReadByte() * 256) + ard.ReadByte())) * 6) / 4096), 2);
			}

			tsc.orario = tsc.orario.AddSeconds(1);

			if ((tsc.tsType & ts_ext1) == 0) return;

			//ADC VALORE
			if ((tsc.tsTypeExt1 & ts_adcValue) == ts_adcValue)
			{
				tsc.adcVal = Math.Round((double)(ard.ReadByte() * 256 + ard.ReadByte()), 2);
				//gruppoCON[adcVal] = tsc.adcVal.ToString("0000");
			}

			if ((tsc.tsTypeExt1 & ts_mag) == ts_mag)
			{
				tsc.magX_A = ard.ReadByte();
				tsc.magX_A += (ard.ReadByte() * 256);
				if (tsc.magX_A > 32767)
				{
					tsc.magX_A -= 65536;
				}
				tsc.magX_A *= 1.5;

				tsc.magY_A = ard.ReadByte();
				tsc.magY_A += (ard.ReadByte() * 256);
				if (tsc.magY_A > 32767)
				{
					tsc.magY_A -= 65536;
				}
				tsc.magY_A *= 1.5;

				tsc.magZ_A = ard.ReadByte();
				tsc.magZ_A += (ard.ReadByte() * 256);
				if (tsc.magZ_A > 32767)
				{
					tsc.magZ_A -= 65536;
				}
				tsc.magZ_A *= 1.5;
			}

			if ((tsc.tsTypeExt1 & ts_time) == ts_time)
			{
				if (!overrideTime)
				{
					int anno = ard.ReadByte();
					anno = ((anno >> 4) * 10) + (anno & 15) + 2000;
					ard.ReadByte();

					int giorno = ard.ReadByte();
					giorno = ((giorno >> 4) * 10) + (giorno & 15);

					int mese = ard.ReadByte();
					mese = ((mese >> 4) * 10) + (mese & 15);

					int ore = ard.ReadByte();
					ore = ((ore >> 4) * 10) + (ore & 15);
					ard.ReadByte();

					int secondi = ard.ReadByte();
					secondi = ((secondi >> 4) * 10) + (secondi & 15);

					int minuti = ard.ReadByte();
					minuti = ((minuti >> 4) * 10) + (minuti & 15);

					try
					{
						tsc.orario = new DateTime(anno, mese, giorno, ore, minuti, secondi);
					}
					catch { }

				}
				else
				{
					ard.ReadBytes(8);
				}
			}
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

			csvHeader = csvHeader + csvSeparator + "accX" + csvSeparator + "accY" + csvSeparator + "accZ";
			if (magen > 0)
			{
				csvHeader += csvSeparator + "magX" + csvSeparator + "magY" + csvSeparator + "magZ";
			}
			csvHeader = csvHeader + csvSeparator + "Temp. (°C)";
			if (adcEn > 0)
			{
				csvHeader += csvSeparator + "Analog";
			}
			if (prefBattery)
			{
				csvHeader = csvHeader + csvSeparator + "Battery Voltage (V)";
			}

			if (metadata)
			{
				csvHeader = csvHeader + csvSeparator + "Metadata";
			}

			csvHeader += "\r\n";
			csv.Write(Encoding.ASCII.GetBytes(csvHeader));
		}

		private bool detectEof(ref BinaryReader ard)
		{
			if (ard.BaseStream.Position >= ard.BaseStream.Length)
			{
				return true;
			}
			return false;
		}

		public override void abortConf()
		{

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
