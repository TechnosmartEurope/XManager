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
#if X64
using FT_HANDLE = System.UInt64;
#else
using FT_HANDLE = System.UInt32;
#endif

namespace X_Manager.Units
{
	class Gipsy6 : Units.Unit
	{
#pragma warning disable
		struct timeStamp
		{
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
			public int altSegno;
			public int eo;
			public int ns;
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
			public double dop;
			public int sat;
			public int gsvSum;

			public int timeStampLength;
			public DateTime orario;

			public byte[] eventAr;
			public bool isEvent;
			public int stopEvent;
			public int inWater;
			public int inAdc;
			public int ADC;
		}

		public struct coordKml
		{
			public string cSstring;
			public string cPlacemark;
			public string cName;
			public string cClass;
		}

		byte debugLevel;
		byte dateFormat;
		byte timeFormat;
		bool inMeters = false;
		bool prefBattery = false;
		bool repeatEmptyValues = true;
		int isDepth = 1;
		bool sameColumn = false;
		bool angloTime = false;
		string dateFormatParameter;
		bool metadata;
		ushort adcThreshold = 0;
		bool adcLog = false;
		bool adcStop = false;
		bool primaCoordinata;
		uint contoCoord;
		NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;

		new byte[] firmwareArray = new byte[3];

		public Gipsy6(object p)
			: base(p)
		{
			modelCode = model_Gipsy6;
			configureMovementButtonEnabled = true;
			configurePositionButtonEnabled = false;
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
			try
			{
				sp.ReadExisting();
				sp.Write("TTTTTTTGGAN");
				byte nIn = 255;
				for (int i = 0; i < 28; i++)
				{
					nIn = (byte)sp.ReadByte();
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
			battLevel = battLevel + (battLevel - 3) * .05 + .14;

			battery = Math.Round(battLevel, 2).ToString("0.00") + "V";
			return battery;
		}

		public override void setPcTime()
		{
			sp.Write("TGGAt");
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

		public override uint askMaxMemory()
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
			maxMemory = m;
			return maxMemory;
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
			return new uint[] { m };
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

		public override void setName(string newName)
		{
			byte[] nameShort = Encoding.ASCII.GetBytes(newName);
			byte[] name = new byte[28];
			Array.Copy(nameShort, 0, name, 0, nameShort.Length);

			sp.Write("TTTTTTTGGAn");
			try
			{
				sp.ReadByte();
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
			sp.Write(name, 0, 28);
			if (sp.ReadByte() != 'I')
			{
				askName();
			}
		}

		public override bool isRemote()
		{
			remote = false;
			sp.Write("TTTTTTTTTTGGAl");
			try
			{
				if (sp.ReadByte() == 1) remote = true;
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
			return remote;
		}

		public override byte[] getConf()
		{
			sp.Write("TTTTTTTTGGAC");
			byte[] conf = new byte[0x1000];
			try
			{
				//ACQ, Start delay e GSV
				for (int i = 32; i < 52; i++)
				{
					conf[i] = (byte)sp.ReadByte();
				}
				//Schedule A e B
				for (int i = 52; i < 52 + 32; i++)
				{
					conf[i] = (byte)sp.ReadByte();
				}
				//Schedule C e D + mesi
				for (int i = 84; i < 84+44; i++)
				{
					conf[i] = (byte)sp.ReadByte();
				}
			}
			catch
			{
				throw new Exception(unitNotReady);
			}


			return conf;
		}

		//public override byte[] getGpsSchedule()
		//{
		//	byte[] schedule = new byte[256];
		//	sp.ReadExisting();
		//	sp.Write("TTTTTTTTTTTTTGGAS");
		//	try
		//	{
		//		for (int i = 0; i < 256; i++)
		//		{
		//			schedule[i] = (byte)sp.ReadByte();
		//		}
		//		//for (int i = 0; i <= 63; i++) { schedule[i] = (byte)sp.ReadByte(); }
		//		//if (remote) sp.Write(new byte[] { 2 }, 0, 1);

		//		//for (int i = 64; i <= 127; i++) { schedule[i] = (byte)sp.ReadByte(); }
		//		//if (remote) sp.Write(new byte[] { 2 }, 0, 1);

		//		//for (int i = 128; i <= 171; i++) { schedule[i] = (byte)sp.ReadByte(); }
		//	}
		//	catch
		//	{
		//		throw new Exception(unitNotReady);
		//	}
		//	return schedule;
		//}

		//public override void setGpsSchedule(byte[] schedule)
		//{
		//	sp.Write("TTTTTTTTGGAs");
		//	try
		//	{
		//		sp.ReadByte();
		//		sp.Write(schedule, 0, 64);
		//		if (remote)
		//		{
		//			sp.ReadByte();
		//		}
		//		sp.Write(schedule, 64, 64);
		//		if (remote)
		//		{
		//			sp.ReadByte();
		//		}
		//		sp.Write(schedule, 128, 64);
		//		if (remote)
		//		{
		//			sp.ReadByte();
		//		}
		//		sp.Write(schedule, 192, 64);
		//		if (remote)
		//		{
		//			sp.ReadByte();
		//		}
		//		sp.ReadByte();
		//	}
		//	catch
		//	{
		//		throw new Exception(unitNotReady);
		//	}
		//}

		public override void disconnect()
		{
			base.disconnect();
			sp.Write("TTTTTTTTTGGAO");
		}

		public override void download(string fileName, uint fromMemory, uint toMemory, int baudrate)
		{

			//Passa alla gestione FTDI D2XX
			try
			{
				sp.Close();
			}
			catch { }


			MainWindow.FT_STATUS FT_Status;
			FT_HANDLE FT_Handle = 0;
			byte[] outBuffer = new byte[50];
			//byte[] add = new byte[3] { 0, 0, 0 };
			byte[] inBuffer = new byte[4096];
			byte[] tempBuffer = new byte[2048];
			byte[] address = new byte[8];

			uint bytesToWrite = 0, bytesWritten = 0, bytesReturned = 0;

			FT_Status = MainWindow.FT_OpenEx(parent.ftdiSerialNumber, (UInt32)1, ref FT_Handle);

			MainWindow.FT_SetLatencyTimer(FT_Handle, (byte)1);
			MainWindow.FT_SetTimeouts(FT_Handle, (uint)1000, (uint)1000);

			MainWindow.FT_Close(FT_Handle);

			//Fine gestione FTDI

			sp.Open();

			convertStop = false;
			uint actMemory = fromMemory;
			System.IO.FileMode fm = System.IO.FileMode.Create;
			if (fromMemory != 0) fm = System.IO.FileMode.Append;
			string fileNameMdp = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + ".mdp";
			var fo = new BinaryWriter(File.Open(fileNameMdp, fm));

			sp.Write("TTTTTTTTTTTTGGAD");
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

			Thread.Sleep(200);
			byte b = (byte)(baudrate / 1000000);
			sp.Write(new byte[] { b }, 0, 1);
			Thread.Sleep(10);
			sp.BaudRate = baudrate;

			Thread.Sleep(400);
			sp.Write("S");
			Thread.Sleep(100);
			if (sp.ReadByte() != (byte)0x53)
			{
				Thread.Sleep(10);
				sp.ReadExisting();
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



			bool firstLoop = true;
			byte temp;

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

				sp.Write(outBuffer, 0, (int)1);
				if (bytesToWrite > 1)
				{
					Thread.Sleep(1);
					sp.Write(outBuffer, 1, (int)(bytesToWrite - 1));
				}

				try
				{
					temp = (byte)sp.ReadByte();
					for (int i = 0; i < 4096; i++)
					{
						inBuffer[i] = (byte)(sp.ReadByte());
					}
				}
				catch
				{
					firstLoop = true;
				}

				actMemory += 4096;
				if ((actMemory % 0x20000) == 0)
				{
					actMemory -= 4096;
					for (int i = 0; i < 2; i++)
					{
						address = BitConverter.GetBytes(actMemory);
						Array.Reverse(address);
						Array.Copy(address, 0, outBuffer, 1, 3);
						outBuffer[0] = 97;
						bytesToWrite = 4;

						sp.Write(outBuffer, 0, (int)1);
						Thread.Sleep(1);
						sp.Write(outBuffer, 1, (int)(3));

						try
						{
							temp = (byte)sp.ReadByte();
							for (int j = 0; j < 2048; j++)
							{
								inBuffer[j] = (byte)(sp.ReadByte());
							}
						}
						catch
						{
							firstLoop = true;
						}

						fo.Write(inBuffer, 0, 2048);
						actMemory += 2048;
					}
					firstLoop = true;
				}
				else
				{
					fo.Write(inBuffer, 0, 4096);
				}

				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Value = actMemory));

				if (convertStop) actMemory = toMemory;
			}

			fo.Write(firmwareArray, 0, 3);
			fo.Write(new byte[] { model_Gipsy6, (byte)254 }, 0, 2);
			fo.Close();
			sp.Write("X");

			sp.BaudRate = 115200;

			if (!convertStop) extractArds(fileNameMdp, fileName, true);
			else
			{
				if (Parent.lastSettings[6].Equals("false"))
				{
					try
					{
						System.IO.File.Delete(fileNameMdp);
					}
					catch { }
				}
			}
			Thread.Sleep(300);
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFinished()));
		}

		//public unsafe override void download(MainWindow parent, string fileName, uint fromMemory, uint toMemory, int baudrate)
		//{
		//	convertStop = false;
		//	uint actMemory = fromMemory;
		//	System.IO.FileMode fm = System.IO.FileMode.Create;
		//	if (fromMemory != 0) fm = System.IO.FileMode.Append;
		//	string fileNameMdp = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + ".mdp";
		//	var fo = new BinaryWriter(File.Open(fileNameMdp, fm));

		//	sp.Write("TTTTTTTTTTTTGGAD");
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

		//	Thread.Sleep(200);
		//	byte b = (byte)(baudrate / 1000000);
		//	sp.Write(new byte[] { b }, 0, 1);
		//	Thread.Sleep(10);
		//	sp.BaudRate = baudrate;

		//	Thread.Sleep(400);
		//	sp.Write("S");
		//	Thread.Sleep(100);
		//	if (sp.ReadByte() != (byte)0x53)
		//	{
		//		Thread.Sleep(10);
		//		sp.ReadExisting();
		//		Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFailed()));
		//		try
		//		{
		//			fo.Close();
		//		}
		//		catch { }
		//		return;
		//	}

		//	//sviluppo
		//	//byte[] ob = new byte[4]{ 0x41, 0, 0, 0 };
		//	//byte[] ib = new byte[4096];
		//	//sp.Write(ob, 0, 1);
		//	//Thread.Sleep(1);
		//	//sp.Write(ob, 1, 3);
		//	//for (int d = 0; d < 4096; d++)
		//	//{
		//	//	ib[d] = (byte)sp.ReadByte();
		//	//}
		//	///sviluppo

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
		//	//byte[] add = new byte[3] { 0, 0, 0 };
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
		//			if (bytesToWrite == 0)
		//			{
		//				FT_Status = MainWindow.FT_Write(FT_Handle, outP, bytesToWrite, ref bytesWritten);
		//			}
		//			else
		//			{
		//				FT_Status = MainWindow.FT_Write(FT_Handle, outP, 0, ref bytesWritten);
		//				Thread.Sleep(1);
		//				FT_Status = MainWindow.FT_Write(FT_Handle, (outP + 1), 3, ref bytesWritten);
		//				//MessageBox.Show((*outP).ToString() + " " + (*(outP + 1)).ToString() + " " + (*(outP + 2)).ToString() + " " + (*(outP + 3)).ToString());
		//			}
		//			//sviluppo
		//			FT_Status = MainWindow.FT_Read(FT_Handle, inP, (uint)1, ref bytesReturned);
		//			///sviluppo
		//			FT_Status = MainWindow.FT_Read(FT_Handle, inP, (uint)4096, ref bytesReturned);

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
		//				actMemory -= 4096;
		//				for (int i = 0; i < 2; i++)
		//				{
		//					address = BitConverter.GetBytes(actMemory);
		//					Array.Reverse(address);
		//					Array.Copy(address, 0, outBuffer, 1, 3);
		//					outBuffer[0] = 97;
		//					bytesToWrite = 4;
		//					fixed (byte* outP = outBuffer, inP = inBuffer)
		//					{
		//						FT_Status = MainWindow.FT_Write(FT_Handle, outP, bytesToWrite, ref bytesWritten);
		//						FT_Status = MainWindow.FT_Read(FT_Handle, inP, (uint)2048, ref bytesReturned);
		//					}
		//					fo.Write(inBuffer, 0, 2048);
		//					actMemory += 2048;
		//				}
		//				firstLoop = true;
		//			}
		//			else
		//			{
		//				fo.Write(inBuffer, 0, 4096);
		//			}
		//		}

		//		Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Value = actMemory));

		//		if (convertStop) actMemory = toMemory;
		//	}

		//	fo.Write(firmwareArray, 0, 3);
		//	fo.Write(new byte[] { model_axyTrek, (byte)254 }, 0, 2);

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
		//				System.IO.File.Delete(fileNameMdp);
		//			}
		//			catch { }
		//		}
		//	}
		//	Thread.Sleep(300);
		//	Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFinished()));
		//}

		public override void abortConf() { }

		public override void setConf(byte[] conf) { }

		//public override byte[] getConf() { return new byte[] { 0 }; }

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
				if (Parent.lastSettings[6].Equals("false"))
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

			bool makeTxt = false;
			bool makeKml = false;
			timeStamp timeStampO = new timeStamp();
			byte[] ev = new byte[5];
			string barStatus = "";
			debugLevel = parent.stDebugLevel;

			string shortFileName;

			string addOn = "";
			string exten = Path.GetExtension(fileName);
			if ((exten.Length > 4)) addOn = ("_S" + exten.Remove(0, 4));

			string fileNameCsv = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + addOn + ".csv";
			string FileNametxt = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + addOn + ".txt";
			string fileNameKml = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + addOn + "_temp" + ".kml";
			string fileNamePlaceMark = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + addOn + ".kml";

			BinaryReader ardFile = new System.IO.BinaryReader(System.IO.File.Open(fileName, FileMode.Open));
			byte[] ardBuffer = new byte[ardFile.BaseStream.Length];
			ardFile.Read(ardBuffer, 0, (int)ardFile.BaseStream.Length);
			ardFile.Close();

			MemoryStream ard = new MemoryStream(ardBuffer);

			BinaryWriter csv = new System.IO.BinaryWriter(System.IO.File.OpenWrite(fileNameCsv));
			BinaryWriter txt = BinaryWriter.Null;
			BinaryWriter kml = BinaryWriter.Null;
			BinaryWriter placeMark = BinaryWriter.Null;

			//Imposta le preferenze di conversione
			timeStampO.eventAr = ev;
			if ((Parent.lastSettings[5] == "air")) isDepth = 0;

			if ((prefs[pref_fillEmpty] == "False")) repeatEmptyValues = false;

			dateSeparator = csvSeparator;
			if ((prefs[pref_sameColumn] == "True"))
			{
				sameColumn = true;
				dateSeparator = " ";
			}

			if (prefs[pref_txt] == "True") makeTxt = true;
			if (prefs[pref_kml] == "True") makeKml = true;
			if (prefs[pref_battery] == "True") prefBattery = true;
			if (prefs[pref_pressMetri] == "meters") inMeters = true;
			if (prefs[pref_timeFormat] == "2") angloTime = true;

			timeStampO.pressOffset = double.Parse(prefs[pref_millibars]);
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
			metadata = false;
			if (prefs[pref_metadata] == "True") metadata = true;
			timeStampO.inAdc = 0;
			timeStampO.inWater = 0;

			//byte[] uf = new byte[16];
			ard.Position = 1;
			firmTotA = (uint)ard.ReadByte() * (uint)1000000 + (uint)ard.ReadByte() * (uint)1000 + (uint)ard.ReadByte();

			//long pos = ard.Position;

			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.IsIndeterminate = false));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
				new Action(() => parent.statusLabel.Content = barStatus + " - Converting"));

			shortFileName = Path.GetFileNameWithoutExtension(fileName);

			if (makeTxt)
			{
				if (System.IO.File.Exists(FileNametxt)) System.IO.File.Delete(FileNametxt);
				txt = new System.IO.BinaryWriter(File.OpenWrite(FileNametxt));
			}
			if (makeKml)
			{
				if (System.IO.File.Exists(fileNameKml)) System.IO.File.Delete(fileNameKml);
				if (System.IO.File.Exists(fileNamePlaceMark)) System.IO.File.Delete(fileNamePlaceMark);
				kml = new System.IO.BinaryWriter(File.OpenWrite(fileNameKml));
				placeMark = new System.IO.BinaryWriter(File.OpenWrite(fileNamePlaceMark));
				primaCoordinata = true;
				contoCoord = 0;
				//string
				kml.Write(System.Text.Encoding.ASCII.GetBytes(X_Manager.Properties.Resources.Folder_Path_Top));
				kml.Write(System.Text.Encoding.ASCII.GetBytes(X_Manager.Properties.Resources.Path_Top));
				//placemark
				placeMark.Write(System.Text.Encoding.ASCII.GetBytes(X_Manager.Properties.Resources.Final_Top_1 +
					Path.GetFileNameWithoutExtension(fileName) + X_Manager.Properties.Resources.Final_Top_2));
			}

			csvPlaceHeader(ref csv);
			ard.Position = 0xa3;

			//Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Maximum = ard.Length - 1));
			progMax = ard.Length - 1;
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
				new Action(() => parent.statusProgressBar.Maximum = ard.Length - 1));
			progressWorker.RunWorkerAsync();

			string sBuffer = "";

			while (!convertStop)
			{

				//if ((ard.Position % 128) == 0)
				//{

				//	Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
				//				new Action(() => parent.statusProgressBar.Value = ard.Position));
				//}
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
					groupConverter(ref timeStampO, lastGroup, shortFileName, ref sBuffer);
					csv.Write(System.Text.Encoding.ASCII.GetBytes(sBuffer));
					break;
				}

				try
				{
					groupConverter(ref timeStampO, extractGroup(ref ard, ref timeStampO), shortFileName, ref sBuffer);

					if (sBuffer.Length > 0x400)
					{
						csv.Write(System.Text.Encoding.ASCII.GetBytes(sBuffer));
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

			csv.Write(System.Text.Encoding.ASCII.GetBytes(sBuffer));

			if (makeTxt) txtWrite(ref timeStampO, ref txt);

			while (Interlocked.Exchange(ref progLock, 2) > 0) { }
			progVal = ard.Position;
			Thread.Sleep(300);
			progVal = -1;
			Interlocked.Exchange(ref progLock, 0);
			//Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
			//	new Action(() => parent.statusProgressBar.Value = ard.Position));

			if (makeKml)
			{
				//Scrive il segnaposto di stop nel fime kml dei placemarks
				placeMark.Write(System.Text.Encoding.ASCII.GetBytes(X_Manager.Properties.Resources.Folder_Bot));
				placeMark.Write(System.Text.Encoding.ASCII.GetBytes(X_Manager.Properties.Resources.Placemarks_Stop_Top +
					(decoderKml(ref timeStampO)).cPlacemark + X_Manager.Properties.Resources.Placemarks_Stop_Bot));
				kml.Close();
				placeMark.Close();

				//Scrive l'header finale nel file kml string
				System.IO.File.AppendAllText(fileNameKml, X_Manager.Properties.Resources.Path_Bot);
				System.IO.File.AppendAllText(fileNameKml, X_Manager.Properties.Resources.Folder_Bot);

				//Accorpa kml placemark e string
				System.IO.File.AppendAllText(fileNamePlaceMark, System.IO.File.ReadAllText(fileNameKml));

				//Chiude il kml placemark
				System.IO.File.AppendAllText(fileNamePlaceMark, X_Manager.Properties.Resources.Final_Bot);
				//Elimina il kml string temporaneo
				System.IO.File.Delete(fileNameKml);
			}

			if (makeTxt) txt.Close();
			csv.Close();
			ard.Close();

			//sw.Stop();
			//MessageBox.Show(sw.Elapsed.TotalSeconds.ToString());

			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
				new Action(() => parent.nextFile()));
		}

		private double[] extractGroup(ref MemoryStream ard, ref timeStamp tsc)
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

		private void groupConverter(ref timeStamp tsLoc, double[] group, string unitName, ref string sBuffer)
		{
			string dateTimeS;
			string ampm = "";
			string dateS = "";
			//int contoTab = 0;

			//Compila data e ora
			dateS = tsLoc.orario.ToString(dateFormatParameter);
			var dateCi = new CultureInfo("it-IT");
			if (angloTime) dateCi = new CultureInfo("en-US");
			dateTimeS = dateS + dateSeparator + tsLoc.orario.ToString("T", dateCi);
			if (angloTime)
			{
				ampm = dateTimeS.Split(' ')[dateTimeS.Split(' ').Length - 1];
				dateTimeS = dateTimeS.Remove(dateTimeS.Length - 3, 3);
			}

			sBuffer += unitName + csvSeparator + dateTimeS + ".000";

			//Inserisce le coordinate
			if (((tsLoc.tsType & ts_coordinate) == ts_coordinate) | repeatEmptyValues)
			{
				sBuffer += csvSeparator + tsLoc.lon.ToString("000.0000000") + csvSeparator + tsLoc.lat.ToString("00.0000000");
				sBuffer += csvSeparator + tsLoc.altitude.ToString("0.00") + csvSeparator + tsLoc.speed.ToString("0.00") + csvSeparator + tsLoc.dop.ToString("0.00");
				sBuffer += csvSeparator + tsLoc.sat.ToString() + csvSeparator + tsLoc.gsvSum.ToString();    //rimuovere "X4" dopo sviluppo
			}
			else
			{
				sBuffer += csvSeparator + csvSeparator + csvSeparator + csvSeparator + csvSeparator + csvSeparator + csvSeparator;
			}
			//contoTab=7;

			//Inserisce la batteria
			if (prefBattery)
			{
				if (((tsLoc.tsType & ts_battery) == ts_battery) | repeatEmptyValues)
				{
					sBuffer += csvSeparator + (tsLoc.batteryLevel.ToString("0.00"));
				}
				else
				{
					sBuffer += csvSeparator;
				}
				//contoTab++;
			}

			//Inserisce i metadati
			if (metadata)
			{
				if (((tsLoc.tsType & ts_event) == ts_event) | repeatEmptyValues)
				{
					sBuffer += csvSeparator + decodeEvent(ref tsLoc);
				}
				else
				{
					sBuffer += csvSeparator;
				}
				//contoTab++;
			}

			sBuffer += "\r\n";



			//double x, y, z;
			//int milli = 0;
			//string activityWater = "";

			//textOut = "";
			//x = group[0] * gCoeff;
			//y = group[1] * gCoeff;
			//z = group[2] * gCoeff;

			//textOut += unitName + csvSeparator + dateTimeS + ".000";
			//if (angloTime) textOut += " " + ampm;
			//textOut += csvSeparator + x.ToString(cifreDecString, nfi) + csvSeparator + y.ToString(cifreDecString, nfi) + csvSeparator + z.ToString(cifreDecString, nfi);

			//additionalInfo = "";
			//if (debugLevel > 2) additionalInfo += csvSeparator + tsLoc.timeStampLength.ToString();  //sviluppo

			//contoTab += 1;
			//if ((tsLoc.tsType & 64) == 64) activityWater = "Active";
			//else activityWater = "Inactive";
			//if ((tsLoc.tsType & 128) == 128) activityWater += "/Wet";
			//else activityWater += "/Dry";

			//additionalInfo += csvSeparator + activityWater;

			//if (pressureEnabled > 0)
			//{
			//	contoTab += 1;
			//	additionalInfo += csvSeparator;
			//	if (((tsLoc.tsType & 4) == 4) | repeatEmptyValues) additionalInfo += tsLoc.press.ToString("0.00", nfi);
			//}
			//if (temperatureEnabled > 0)
			//{
			//	contoTab += 1;
			//	additionalInfo += csvSeparator;
			//	if (((tsLoc.tsType & 2) == 2) | repeatEmptyValues) additionalInfo += tsLoc.temperature.ToString("0.0", nfi);
			//}

			////Inserire la coordinata.
			//contoTab += 7;
			//if (((tsLoc.tsType & 16) == 16) | repeatEmptyValues)
			//{
			//	string altSegno, eo, ns;
			//	altSegno = eo = ns = "-";
			//	if (tsLoc.altSegno == 0) altSegno = "";
			//	if (tsLoc.eo == 1) eo = "";
			//	if (tsLoc.ns == 1) ns = "";
			//	double speed = tsLoc.vel * 3.704;
			//	double lon, lat = 0;

			//	lon = ((tsLoc.lonMinuti + (tsLoc.lonMinDecH / 100.0) + (tsLoc.lonMinDecL / 10000.0) + (tsLoc.lonMinDecLL / 100000.0)) / 60) + tsLoc.lonGradi;
			//	lat = ((tsLoc.latMinuti + (tsLoc.latMinDecH / 100.0) + (tsLoc.latMinDecL / 10000.0) + (tsLoc.latMinDecLL / 100000.0)) / 60) + tsLoc.latGradi;

			//	additionalInfo += csvSeparator + ns + lat.ToString("#00.00000", nfi);
			//	additionalInfo += csvSeparator + eo + lon.ToString("#00.00000", nfi);
			//	additionalInfo += csvSeparator + altSegno + ((tsLoc.altH * 256 + tsLoc.altL) * 2).ToString();
			//	additionalInfo += csvSeparator + speed.ToString("0.0", nfi);
			//	additionalInfo += csvSeparator + tsLoc.nSat.ToString();
			//	additionalInfo += csvSeparator + tsLoc.DOP.ToString() + "." + tsLoc.DOPdec.ToString();
			//	additionalInfo += csvSeparator + tsLoc.gsvSum.ToString();
			//}
			//else
			//{
			//	additionalInfo += csvSeparator + csvSeparator + csvSeparator + csvSeparator + csvSeparator + csvSeparator + csvSeparator;
			//}

			////Inserisce il sensore analogico
			//if (adcLog)
			//{
			//	contoTab += 1;
			//	additionalInfo += csvSeparator;
			//	if (((tsLoc.tsTypeExt1 & 2) == 2) | repeatEmptyValues) additionalInfo += tsLoc.ADC.ToString("0000");
			//}

			//if (adcStop)
			//{
			//	contoTab += 1;
			//	additionalInfo += csvSeparator;
			//	if (((tsLoc.tsTypeExt1 & 4) == 4) | repeatEmptyValues) additionalInfo += "Threshold crossed";
			//}

			////Inserisce la batteria
			//if (prefBattery)
			//{
			//	contoTab += 1;
			//	additionalInfo += csvSeparator;
			//	if (((tsLoc.tsType & 8) == 8) | repeatEmptyValues) additionalInfo += tsLoc.batteryLevel.ToString("0.00", nfi);
			//}

			////Inserisce i metadati
			//if (metadata)
			//{
			//	contoTab += 1;
			//	additionalInfo += csvSeparator;
			//	if (tsLoc.stopEvent > 0)
			//	{
			//		switch (tsLoc.stopEvent)
			//		{
			//			case 1:
			//				additionalInfo += "Low battery.";
			//				break;
			//			case 2:
			//				additionalInfo += "Power off command received.";
			//				break;
			//			case 3:
			//				additionalInfo += "Memory full.";
			//				break;
			//		}
			//		textOut += additionalInfo + "\r\n";
			//		return;// textOut;
			//	}
			//}

			//textOut += additionalInfo + "\r\n";

			//if (tsLoc.stopEvent > 0) return;// textOut;

			//if (!repeatEmptyValues)
			//{
			//	additionalInfo = "";
			//	for (ushort ui = 0; ui < contoTab; ui++) additionalInfo += csvSeparator;
			//}

			//milli += addMilli;
			//dateTimeS += ".";
			//if (tsLoc.stopEvent > 0) bitsDiv = 1;

			//var iend = (short)(rate * 3);

			//for (short i = 3; i < iend; i += 3)
			//{
			//	x = group[i] * gCoeff;
			//	y = group[i + 1] * gCoeff;
			//	z = group[i + 2] * gCoeff;

			//	if (rate == 1)
			//	{
			//		tsLoc.orario = tsLoc.orario.AddSeconds(1);
			//		dateTimeS = dateS + csvSeparator + tsLoc.orario.ToString("T", dateCi) + ".";
			//	}
			//	textOut += unitName + csvSeparator + dateTimeS + milli.ToString("D3");

			//	if (angloTime) textOut += " " + ampm;
			//	textOut += csvSeparator + x.ToString(cifreDecString, nfi) + csvSeparator + y.ToString(cifreDecString, nfi) + csvSeparator + z.ToString(cifreDecString, nfi);

			//	textOut += additionalInfo + "\r\n";
			//	milli += addMilli;
			//}
		}

		private void decodeTimeStamp(ref MemoryStream ard, ref timeStamp tsc, uint fTotA)
		{
			tsc.stopEvent = 0;
			int secondAmount = 1;

			tsc.tsType = ard.ReadByte();

			//Flag timestamp esteso
			if ((tsc.tsType & ts_ext1) == ts_ext1)
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

			//Temperatura (evntualmente anche pressione)	//Ancora non implementate!
			//if ((tsc.tsType & 2) == 2)
			//{
			//	if (temperatureEnabled == 2)
			//	{
			//		tsc.temperature = ard.ReadByte() + ard.ReadByte() * 256;
			//		tsc.temperature = (uint)(tsc.temperature) >> 6;
			//		if (tsc.temperature > 511)
			//		{
			//			tsc.temperature -= 1024;
			//		}
			//		tsc.temperature = (tsc.temperature * 0.1221) + 22.5;
			//	}
			//	else
			//	{
			//		if (isDepth == 1)
			//		{
			//			if (fTotA > 2000000)
			//			{
			//				if (pressureDepth5837(ref ard, ref tsc)) return;
			//			}
			//			else
			//			{
			//				if (pressureDepth(ref ard, ref tsc)) return;
			//			}
			//		}
			//		else
			//		{
			//			if (pressureAir(ref ard, ref tsc)) return;
			//		}
			//	}
			//}

			//Batteria
			if ((tsc.tsType & ts_battery) == ts_battery)
			{
				tsc.batteryLevel = ((ard.ReadByte() * 256 + ard.ReadByte()) * 6.0 / 4096);
			}

			//Coordinata
			if ((tsc.tsType & ts_coordinate) == ts_coordinate)
			{
				int anno = ard.ReadByte() * 256 + ard.ReadByte();
				int mese = ard.ReadByte();
				int giorno = ard.ReadByte();
				int ore = ard.ReadByte();
				int minuti = ard.ReadByte();
				int secondi = ard.ReadByte();

				ard.ReadByte(); //FixType

				tsc.lon = ard.ReadByte() + (ard.ReadByte() << 8) + (ard.ReadByte() << 16) + (ard.ReadByte() << 24);
				if (tsc.lon > 2_147_483_647) tsc.lon -= 4_294_967_296;
				tsc.lon /= 10000000;
				tsc.lat = ard.ReadByte() + (ard.ReadByte() << 8) + (ard.ReadByte() << 16) + (ard.ReadByte() << 24);
				if (tsc.lat > 2_147_483_647) tsc.lat -= 4_294_967_296;
				tsc.lat /= 10000000;

				tsc.altitude = ard.ReadByte() + (ard.ReadByte() << 8) + (ard.ReadByte() << 16) + (ard.ReadByte() << 24);
				if (tsc.altitude > 2_147_483_647) tsc.altitude -= 4_294_967_296;
				tsc.altitude /= 1000;

				tsc.speed = ard.ReadByte() + (ard.ReadByte() << 8) + (ard.ReadByte() << 16) + (ard.ReadByte() << 24);
				if (tsc.speed > 2_147_483_647) tsc.speed -= 4_294_967_296;
				tsc.speed *= 3.6;
				tsc.speed /= 10000;

				tsc.dop = ard.ReadByte() + (ard.ReadByte() << 8);
				tsc.dop /= 100;
				tsc.sat = ard.ReadByte();
				tsc.gsvSum = ard.ReadByte() * 256 + ard.ReadByte();

			}


			//evento
			if ((tsc.tsType & ts_event) == ts_event)
			{
				int eventType = ard.ReadByte();
				int eventLenght = eventType / 10;
				ard.Read(tsc.eventAr, 1, eventLenght);
				tsc.eventAr[0] = (byte)eventType;

				if (tsc.eventAr[0] == 11) tsc.stopEvent = 1;
				else if (tsc.eventAr[0] == 12) tsc.stopEvent = 2;
				else if (tsc.eventAr[0] == 13) tsc.stopEvent = 3;

			}

			//Attività/acqua
			tsc.inWater = 0;
			if ((tsc.tsType & ts_water) == ts_water) tsc.inWater = 1;

			//Parametri estesi
			if ((tsc.tsType & ts_ext1) == ts_ext1)
			{
				if ((tsc.tsTypeExt1 & ts_adcValue) == ts_adcValue) tsc.ADC = (ard.ReadByte() * 256 + ard.ReadByte());
				tsc.inAdc = 0;
				if ((tsc.tsTypeExt1 & ts_adcThreshold) == ts_adcThreshold)
				{
					tsc.inAdc = 1;
				}
				if ((tsc.tsTypeExt1 & ts_time) == ts_time)
				{
					int anno = ard.ReadByte();
					anno = ((anno / 16) * 10) + (anno % 16);
					anno += 2000;
					int mese = ard.ReadByte();
					mese = ((mese / 16) * 10) + (mese % 16);
					int giorno = ard.ReadByte();
					giorno = ((giorno / 16) * 10) + (giorno % 16);
					int ore = ard.ReadByte();
					ore = ((ore / 16) * 10) + (ore % 16);
					int minuti = ard.ReadByte();
					minuti = ((minuti / 16) * 10) + (minuti % 16);
					int secondi = ard.ReadByte();
					secondi = ((secondi / 16) * 10) + (secondi % 16);
					tsc.orario = new DateTime(anno, mese, giorno, ore, minuti, secondi);
					secondAmount = 0;
				}
			}

			tsc.orario = tsc.orario.AddSeconds(secondAmount);

		}

		private string decodeEvent(ref timeStamp ts)
		{
			string outS = "";
			switch (ts.eventAr[0])
			{
				case 0:
					return "Start searching for satellites...";
				case 1:
					return "Poor signal.";
				case 3:
					return "Delayed Start";
				case 4:
					return "Start";
				case 5:
					return "Switching to Continuous";
				case 6:
					return "Switchinf to Off";
				case 20:
					outS = "Fix obtained (" + ts.eventAr[1].ToString() + "-" + ts.eventAr[2].ToString() + ")";
					return outS;
				case 30:
					outS = "Signal lost (nsat: " + ts.eventAr[1].ToString() + " gsv:" + (ts.eventAr[2] * 256 + ts.eventAr[3]).ToString() + ")";
					return outS;
				default:
					return "Unknown event.";
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

			//csvHeader = csvHeader + csvSeparator + "X" + csvSeparator + "Y" + csvSeparator + "Z";
			//csvHeader = csvHeader + csvSeparator + "Activity";
			//if (pressureEnabled > 0)
			//{
			//	if (inMeters)
			//	{
			//		if (isDepth == 1)
			//		{
			//			csvHeader = csvHeader + csvSeparator + "Depth";
			//		}
			//		else
			//		{
			//			csvHeader = csvHeader + csvSeparator + "Altitude";
			//		}

			//	}
			//	else
			//	{
			//		csvHeader = csvHeader + csvSeparator + "Pressure";
			//	}
			//}
			//if (temperatureEnabled > 0)
			//{
			//	csvHeader += csvSeparator + "Temp. (°C)";
			//}

			csvHeader += csvSeparator + "loc.lat" + csvSeparator + "loc.lon" + csvSeparator + "alt"
				+ csvSeparator + "speed" + csvSeparator + "hdop" + csvSeparator + "sat.count" + csvSeparator + "signal";
			if (adcLog) csvHeader += csvSeparator + "Sensor Raw";
			if (adcStop) csvHeader += csvSeparator + "Sensor State";
			if (prefBattery) csvHeader += csvSeparator + "batt(V)";
			if (metadata) csvHeader += csvSeparator + "metadata";

			csvHeader += "\r\n";

			csv.Write(System.Text.Encoding.ASCII.GetBytes(csvHeader));

		}

		private void txtWrite(ref timeStamp timeStampO, ref BinaryWriter txt)
		{

		}

		private void kmlWrite(ref timeStamp timeStampO, ref BinaryWriter kml, ref BinaryWriter placeMark)
		{

		}

		private coordKml decoderKml(ref timeStamp tsc)
		{
			return new coordKml();
		}

		private bool detectEof(ref MemoryStream ard)
		{
			if (ard.Position >= ard.Length) return true;
			else return false;
		}

#pragma warning enable
	}

}
