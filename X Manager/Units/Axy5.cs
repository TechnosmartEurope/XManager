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
using System.ComponentModel;
using System.Runtime.InteropServices;

#if X64
using FT_HANDLE = System.UInt64;
#else
using FT_HANDLE = System.UInt32;
#endif

/// <summary>
/// DA FARE.
/// Implementare caricamento di tutto l'ard in ram per velocizzare conversione (come in axy trek)
/// </summary>

namespace X_Manager.Units
{
	class Axy5 : Units.Unit
	{
		bool disposed = false;
		new byte[] firmwareArray = new byte[3];
		struct timeStamp
		{
			public int tsType, tsTypeExt1, tsTypeExt2;
			public double batteryLevel;
			public double temperature;
			public double pressure;
			public double[] magX;
			public double[] magY;
			public double[] magZ;
			public DateTime orario;
			public int stopEvent;
			public int timeStampLength;
			public int metadataPresent;
			public long ardPosition;
		}

		string[] gruppoCON;// = new string[];
		string[] gruppoSENZA;// = new string[];
							 //int[] gruppoPar = new int[13];

		int tagId = 0;
		int date = 1;
		int accx = 2;
		int accy = 3;
		int accz = 4;
		int magx = 7;
		int magy = 8;
		int magz = 9;
		int temp = 5;
		int press = 6;
		int batt = 10;
		int meta = 11;

		const int pref_pressMetri = 0;
		const int pref_millibars = 1;
		const int pref_dateFormat = 2;
		const int pref_timeFormat = 3;
		const int pref_fillEmpty = 4;
		const int pref_sameColumn = 5;
		const int pref_battery = 6;
		const int pref_override_time = 15;
		const int pref_metadata = 16;

		int iend1;
		int iend2;

		//byte dateFormatPreference;
		//byte timeFormatPreference;
		string[] prefs;
		int prefBattery = 0;
		bool repeatEmptyValues = true;
		//byte bitsDiv;
		//bool angloTime = false;
		bool inMeters;
		double gCoeff;
		//string dateFormat;
		int addMilli;
		//int addMilli2Hz;
		CultureInfo dateCi;
		//byte cifreDec;
		const string cifreDecString = "0.00000";
		int metadata = 0;
		bool overrideTime;
		int temperatureEn;
		int isDepth;
		int pressureEn;
		double pressOffset;
		int dtPeriod;
		int magEn;
		byte[] schedule = null;
		byte[] remSched = null;
		byte[] eventAr;
		double[] convCoeffs;
		int rate;
		int range;
		int bit;
		double x, y, z;
		NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;
		string dateTimeFormat;

		public Axy5(object p)
			: base(p)
		{
			positionCanSend = false;
			configurePositionButtonEnabled = false;
			modelCode = model_axy5;
			modelName = "Axy-5";
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
			UInt32 mStart;
			UInt32 mStop;
			sp.Write("TTTTTTTGGAM");
			try
			{
				mStart = (UInt32)sp.ReadByte(); mStart *= 256;
				mStart += (UInt32)sp.ReadByte(); mStart *= 256;
				mStart += (UInt32)sp.ReadByte(); mStart *= 256;
				mStart += (UInt32)sp.ReadByte();

				mStop = (UInt32)sp.ReadByte(); mStop *= 256;
				mStop += (UInt32)sp.ReadByte(); mStop *= 256;
				mStop += (UInt32)sp.ReadByte(); mStop *= 256;
				mStop += (UInt32)sp.ReadByte();
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
			if (mStart > mStop)
			{
				memory = mStart - mStop;
			}
			else if (mStart < mStop)
			{
				memory = maxMemory - mStop + mStart;
			}
			else
			{
				memory = 0;
			}

			return new UInt32[] { memory, mStop, mStart };
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

		public override bool isRemote()
		{
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

			sp.ReadExisting();
			sp.Write("TTTTTTTTTTTTGGAC");
			try
			{
				//Legge rate e range per firmware senza schedule
				if (firmTotA < 1000002)
				{
					for (int i = 15; i < 17; i++) { conf[i] = (byte)sp.ReadByte(); }
				}

				//Legge la configura<ione comune a tutti i firmware
				for (int i = 17; i <= 25; i++) { conf[i] = (byte)sp.ReadByte(); }

				//Legge lo schedule remoto
				if (firmTotA > 1001000)
				{
					for (int i = 26; i <= 28; i++)
					{
						conf[i] = (byte)sp.ReadByte();
					}
				}
				else
				{
					conf[26] = 0;
					conf[27] = 0;
					conf[28] = 1;
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
				if (firmTotA < 001000002)
				{
					sp.Write(conf, 15, 2);
				}
				sp.Write(conf, 17, 9);
				if (firmTotA > 1001000)
				{
					sp.Write(conf, 26, 3);
				}
				sp.ReadByte();
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
		}

		public override byte[] getAccSchedule()
		{
			if (firmTotA < 1001000)
			{
				return new byte[] { 0 };
			}

			byte[] schedule = new byte[30];
			sp.Write("TTTTTTTTTTTTTGGAS");
			Thread.Sleep(200);
			try
			{
				for (int i = 0; i < 30; i++)
				{
					schedule[i] = (byte)sp.ReadByte();
				}
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
			return schedule;
		}

		public override void setAccSchedule(byte[] schedule)
		{
			sp.Write("TTTTTTTTTTTTTGGAs");
			if (firmTotA < 1001000)
			{
				return;
			}
			Thread.Sleep(200);
			try
			{
				sp.ReadByte();
				sp.Write(schedule, 0, 30);
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

		public unsafe override void download(MainWindow parent, string fileName, uint fromMemory, uint toMemory, int baudrate)
		{
			convertStop = false;
			uint actMemory = fromMemory;
			System.IO.FileMode fm = System.IO.FileMode.Create;
			if (fromMemory != 0) fm = System.IO.FileMode.Append;
			//string fileNameMdp_base = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + ".mdp";
			string fileNameMdp_base = Path.GetFileNameWithoutExtension(fileName);
			int numAct = -1;
			foreach (string file in Directory.GetFiles(Path.GetDirectoryName(fileName)))
			{
				string fileNoExt = Path.GetFileNameWithoutExtension(file);
				if (fileNoExt.Contains(fileNameMdp_base))
				{
					int numPr = -1;
					int numStart = fileNoExt.LastIndexOf("_");
					if (numStart > 0)
					{
						numStart++;
						string suff = fileNoExt.Substring(numStart, fileNoExt.Length - numStart);
						if (!string.IsNullOrEmpty(suff))
						{
							if (int.TryParse(suff, out numPr))
							{
								if (numPr > numAct)
								{
									numAct = numPr;
								}
							}
						}
					}
				}
			}
			numAct++;


			string fileNameMdp = Path.GetDirectoryName(fileName) + "\\" + fileNameMdp_base + "_" + numAct.ToString("0000") + ".mdp";

			//byte downInit = 0;
			byte dummy;
			int dieCount;
			try
			{
				sp.Write("TTTTTTTTTTTTGGAe");           // TTTTGGAe ->
				sp.ReadByte();                          // <- e
				Thread.Sleep(70);
				byte b = (byte)(baudrate / 1000000);
				sp.Write(new byte[] { b }, 0, 1);       // 3 ->
				dummy = (byte)sp.ReadByte();            // <- 3
				sp.BaudRate = baudrate;

				Thread.Sleep(400);
				sp.Write("S");                          // S ->
				Thread.Sleep(200);


				dieCount = sp.ReadByte();
				if (dieCount == 'S')    //<- S
				{
					dieCount = 2;
				}
				else if (dieCount == 's')   //<- s
				{
					dieCount = 1;
				}
				else //((dieCount != 1) & (dieCount != 2))
				{
					Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFailed()));
					return;
				}
			}
			catch
			{

				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFailed()));
				return;
			}

			int position = 0;
			int toBeDownloaded;
			if (fromMemory < toMemory)
			{
				toBeDownloaded = (int)toMemory - (int)fromMemory;
			}
			else
			{
				toBeDownloaded = (int)maxMemory - (int)fromMemory + (int)toMemory;
			}

			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.progressBarStopButton.IsEnabled = true));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.progressBarStopButtonColumn.Width = new GridLength(80)));

			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.IsIndeterminate = false));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Minimum = 0));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Maximum = toBeDownloaded));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Value = 0));

			//Passa alla gestione FTDI D2XX
			sp.Close();

			MainWindow.FT_STATUS FT_Status;
			FT_HANDLE FT_Handle = 0;
			byte[] outBuffer = new byte[50];
			byte[] inBuffer = new byte[0x_2000_0000];
			byte[] tempBuffer = new byte[2048];
			byte[] address = new byte[8];

			//byte[] fileBuffer = new byte[0x_2000_0000];
			//MemoryStream fileOut = new MemoryStream(fileBuffer);

			uint bytesToWrite = 0, bytesWritten = 0, bytesReturned = 0;

			FT_Status = MainWindow.FT_OpenEx(parent.ftdiSerialNumber, (UInt32)1, ref FT_Handle);
			if (FT_Status != MainWindow.FT_STATUS.FT_OK)
			{
				MainWindow.FT_Close(FT_Handle);
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFailed()));
			}

			MainWindow.FT_SetLatencyTimer(FT_Handle, (byte)1);
			MainWindow.FT_SetTimeouts(FT_Handle, (uint)1000, (uint)1000);

			int firstLoop = 1;

			uint blockSize;

			if (dieCount == 1)
			{
				blockSize = 0x40000;
				goto _loopSingleDie;
			}
			else
			{
				blockSize = 0x20000;
				goto _loopDualDie;
			}

		_loopSingleDie:

			while (actMemory != (toMemory + 0x1000))
			{
				//COSTRUZIONE COMANDO
				if (firstLoop > 0)          //Inizio blocco o richiesta puntatore specifico, si invia 'A' con i tre byte di indirizzo (il quarto è assunto essere zero)
				{
					address = BitConverter.GetBytes(actMemory);
					Array.Reverse(address);
					Array.Copy(address, 0, outBuffer, 1, 3);
					if (firstLoop == 1)
					{
						outBuffer[0] = (byte)'A';
					}
					else
					{
						outBuffer[0] = (byte)'B';
					}
					bytesToWrite = 4;
					firstLoop = 0;
				}
				else
				{
					outBuffer[0] = (byte)'O';                       //Pagina successiva: si invia soltanto 'O'
					bytesToWrite = 1;
				}

				//INVIO COMANDO
				fixed (byte* outP = outBuffer, inP = inBuffer)
				{
					FT_Status = MainWindow.FT_Write(FT_Handle, outP, bytesToWrite, ref bytesWritten);
					FT_Status = MainWindow.FT_Read(FT_Handle, inP + position, (uint)4096, ref bytesReturned);
				}

				//ANOMALIA SERIALE
				if (FT_Status != MainWindow.FT_STATUS.FT_OK)
				{
					Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFailed()));
					break;
				}

				//RITORNATO BUFFER VUOTO O INCOMPLETO
				else if (bytesReturned != 4096)                     //La pagina è arrivata incompleta, la si richiede nuovamente iducendo il loop a invare 'A' al prossimo comando
				{
					firstLoop = 1;
					continue;
				}

				//BUFFER ARRIVATO OK
				actMemory += 4096;
				if (actMemory == 0x_2000_0000)
				{
					actMemory = 0;
				}
				position += 4096;

				if ((actMemory % 0x_0004_0000) == 0)
				{
					firstLoop = 1;
					//if (!manual) firstLoop++;

				}

				//fileOut.Write(inBuffer, 0, 4096);
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Value = position)); //Aggiornamento progress bar
				if (convertStop) break;      //Premuto il tasto stop
			}

			goto _endLoop;

		_loopDualDie:
			while (actMemory != (toMemory + 0x1000))
			{
				if (firstLoop > 0)       // A
				{
					address = BitConverter.GetBytes(actMemory);
					Array.Reverse(address);
					Array.Copy(address, 0, outBuffer, 1, 3);
					if (firstLoop == 1)
					{
						outBuffer[0] = (byte)'A';
					}
					else
					{
						outBuffer[0] = (byte)'B';
					}
					bytesToWrite = 4;
					firstLoop = 0;
				}
				else
				{
					outBuffer[0] = (byte)'O';
					bytesToWrite = 1;
				}
				fixed (byte* outP = outBuffer, inP = inBuffer)
				{
					FT_Status = MainWindow.FT_Write(FT_Handle, outP, bytesToWrite, ref bytesWritten);
					FT_Status = MainWindow.FT_Read(FT_Handle, inP + position, (uint)4096, ref bytesReturned);
				}

				if (FT_Status != MainWindow.FT_STATUS.FT_OK)
				{
					Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFailed()));
					break;
				}
				else if (bytesReturned != 4096)
				{
					firstLoop = 1;
				}
				else
				{
					actMemory += 4096;
					position += 4096;
					if (((actMemory + 4096) % 0x20000) == 0)         //a
					{
						for (int i = 1; i < 3; i++)
						{
							address = BitConverter.GetBytes(actMemory);
							Array.Reverse(address);
							Array.Copy(address, 0, outBuffer, 1, 3);
							outBuffer[0] = (byte)'a';
							bytesToWrite = 4;
							fixed (byte* outP = outBuffer, inP = inBuffer)
							{
								FT_Status = MainWindow.FT_Write(FT_Handle, outP, bytesToWrite, ref bytesWritten);
								FT_Status = MainWindow.FT_Read(FT_Handle, inP + position, (uint)2048, ref bytesReturned);
							}
							if (bytesReturned < 2048)
							{
								i--;
								continue;
							}
							if (FT_Status != MainWindow.FT_STATUS.FT_OK)
							{
								//outBuffer[0] = (byte)'X';
								//fixed (byte* outP = outBuffer) { FT_Status = MainWindow.FT_Write(FT_Handle, outP, bytesToWrite, ref bytesWritten); }
								//MainWindow.FT_Close(FT_Handle);
								Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFailed()));
								break;
							}
							actMemory += 2048;
							if (actMemory == 0x20000000)
							{
								actMemory = 0;
							}
							position += 2048;
						}
						firstLoop = 1;
						//if (!manual) firstLoop = 2;
					}
				}

				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Value = position));

				if (convertStop) break;// actMemory = toMemory;
			}

		_endLoop:

			//if ((position == toBeDownloaded) && !manual)			//Inserirlo nel remoto!!!!!
			//{
			//	actMemory = ((actMemory / blockSize) * blockSize) + blockSize;

			//	address = BitConverter.GetBytes(actMemory);
			//	Array.Reverse(address);
			//	Array.Copy(address, 0, outBuffer, 1, 3);
			//	outBuffer[0] = (byte)'C';
			//	bytesToWrite = 4;
			//	fixed (byte* outP = outBuffer, inP = inBuffer)
			//	{
			//		FT_Status = MainWindow.FT_Write(FT_Handle, outP, bytesToWrite, ref bytesWritten);
			//	}
			//}

			Thread.Sleep(400);
			outBuffer[0] = (byte)'X';
			bytesToWrite = 1;
			fixed (byte* outP = outBuffer)
			{
				FT_Status = MainWindow.FT_Write(FT_Handle, outP, bytesToWrite, ref bytesWritten);
			}
			MainWindow.FT_Close(FT_Handle);

			if (position > 0)
			{
				var fo = new System.IO.BinaryWriter(File.Open(fileNameMdp, fm));
				fo.Write(inBuffer, 0, (position / 4096) * 4096);

				fo.Write(firmwareArray, 0, firmwareArray.Length);
				for (int i = firmwareArray.Length; i <= 2; i++)
				{
					fo.Write(new byte[] { 0xff }, 0, 1);
				}
				fo.Write(new byte[] { model_axy5, (byte)254 }, 0, 2);

				fo.Close();
			}

			sp.BaudRate = 115200;
			sp.Open();
			if (position > 0) extractArds(fileNameMdp, fileName, true);
			else
			{
				if (MainWindow.lastSettings[6].Equals("false"))
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

		public unsafe override void downloadRemote(MainWindow parent, string fileName, uint fromMemory, uint toMemory, int baudrate)
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
				if (MainWindow.lastSettings[6].Equals("false"))
				{
					try
					{
						System.IO.File.Delete(fileNameMdp);
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

			fileName = Path.GetFileNameWithoutExtension(fileNameMdp);

			while (mdp.BaseStream.Position < mdp.BaseStream.Length)
			{
				testByte = mdp.ReadByte();
				if (testByte == 0xcf)
				{
					testByte2 = mdp.ReadByte();
					if ((testByte2 == 0x55) | (mdp.BaseStream.Position == 2))
					{
						if (ard != BinaryWriter.Null)
						{
							ard.Close();
						}
						counter++;
						fileNameArd = Path.GetDirectoryName(fileNameMdp) + "\\" + fileName + "_S" + counter.ToString() + ".ard";
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
					else
					{
						ard.Write(new byte[] { 0xab, 0x80 });
						ard.Write(mdp.ReadBytes(254));
					}
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
				if (MainWindow.lastSettings[6].Equals("false"))
				{
					System.IO.File.Delete(fileNameMdp);
				}
				else
				{
					if (!Path.GetExtension(fileNameMdp).Contains("Dump"))
					{
						string newFileNameMdp = Path.GetDirectoryName(fileNameMdp) + "\\" + Path.GetFileNameWithoutExtension(fileNameMdp) + ".memDump";
						if (System.IO.File.Exists(newFileNameMdp))
						{
							System.IO.File.Delete(newFileNameMdp);
						}
						//string newFileNameMdp = Path.GetFileNameWithoutExtension(fileNameMdp) + ".memDump";
						System.IO.File.Move(fileNameMdp, newFileNameMdp);
					}
				}
			}
			catch { }
			if (!fromDownload) Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.nextFile()));
		}

		public override void convert(MainWindow parent, string fileName, string preferenceFile)
		{

			string barStatus = "";
			prefs = File.ReadAllLines(MainWindow.prefFile);

			//Imposta le preferenze di conversione
			if ((prefs[pref_fillEmpty] == "False")) repeatEmptyValues = false;

			if (prefs[pref_battery] == "True") prefBattery = 1;

			switch (int.Parse(prefs[pref_dateFormat]))
			{
				case 1:
					dateTimeFormat = "dd/MM/yyyy";
					break;
				case 2:
					dateTimeFormat = "MM/dd/yyyy";
					break;
				case 3:
					dateTimeFormat = "yyyy/MM/dd";
					break;
				case 4:
					dateTimeFormat = "yyyy/dd/MM";
					break;
			}

			if (prefs[pref_sameColumn] == "True")
			{
				dateTimeFormat += " ";
			}
			else
			{
				dateTimeFormat += csvSeparator;
			}


			if (prefs[pref_timeFormat] == "2")
			{
				dateCi = new CultureInfo("en-US");
				dateTimeFormat += "hh:mm:ss.fff tt";
			}
			else
			{
				dateCi = new CultureInfo("it-IT");
				dateTimeFormat += "HH:mm:ss.fff";
			}

			if (prefs[pref_pressMetri] == "meters")
			{
				inMeters = true;
			}
			else
			{
				inMeters = false;
			}
			pressOffset = double.Parse(prefs[pref_millibars]);

			overrideTime = false;
			if (prefs[pref_override_time] == "True") overrideTime = true;

			metadata = 0;
			if (prefs[pref_metadata] == "True") metadata = 1;

			//Imposta i file di lettura e di scrittura
			string shortFileName;
			string addOn = "";
			string exten = Path.GetExtension(fileName);
			if ((exten.Length > 4)) addOn = ("_S" + exten.Remove(0, 4));
			string fileNameCsv = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + addOn + ".csv";
			string fileNameInfo = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + addOn + ".txt";
			BinaryReader ardFile = new System.IO.BinaryReader(System.IO.File.Open(fileName, FileMode.Open));
			byte[] ardBuffer = new byte[ardFile.BaseStream.Length];
			ardFile.Read(ardBuffer, 0, (int)ardFile.BaseStream.Length);
			ardFile.Close();

			MemoryStream ard = new MemoryStream(ardBuffer);

			BinaryWriter csv = new BinaryWriter(System.IO.File.OpenWrite(fileNameCsv));

			ard.Position = 1;
			firmTotA = (uint)(ard.ReadByte() * 1000000 + ard.ReadByte() * 1000 + ard.ReadByte());

			int padding = 0;

			//Legge i parametri di logging
			convCoeffs = new double[6];
			double convSum = 0;
			for (int convc = 0; convc < 6; convc++)
			{
				convCoeffs[convc] = ard.ReadByte() * 256 + ard.ReadByte();
				convSum += convCoeffs[convc];
			}

			findTDEnable(ard.ReadByte());   //Temperatura e pressione abilitate

			dtPeriod = ard.ReadByte();                 //TD periodo di logging (non serve al software)

			findMagEnable(ard.ReadByte());  //Frequenza magnetometro

			if (ard.ReadByte() == 1)        //Controlla se è presente la prima estensione dell'header con schedule e schedule remoto
			{
				padding = 8;
				int movThreshold = ard.ReadByte();              //Legge le soglie di movimento
				int movLatency = ard.ReadByte();
				schedule = new byte[30];
				ard.Read(schedule, 0, 30);
				remSched = new byte[3];
				ard.Read(remSched, 0, 3);
				ard.ReadByte();             //Byte per futura estensione dello schedule
			}

			for (int i = 0; i < padding; i++)
			{
				ard.ReadByte();
			}

			writeInfo(fileNameInfo);

			eventAr = new byte[5];

			timeStamp timeStampO = new timeStamp();
			timeStampO.tsType = timeStampO.tsTypeExt1 = timeStampO.tsTypeExt2 = 0;
			timeStampO.metadataPresent = 0;
			timeStampO.magX = new double[2];
			timeStampO.magY = new double[2];
			timeStampO.magZ = new double[2];
			timeStampO.orario = findStartTime(ref prefs);

			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => barStatus = (string)parent.statusLabel.Content));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.IsIndeterminate = false));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
				new Action(() => parent.statusLabel.Content = barStatus + " - Converting"));

			shortFileName = Path.GetFileNameWithoutExtension(fileName);

			convertStop = false;

			ard.ReadByte();

			csvPlaceHeader(ref csv);

			gruppoCON[0] = shortFileName;
			gruppoSENZA[0] = shortFileName;

			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
				new Action(() => parent.statusProgressBar.Maximum = ard.Length - 1));

			while (!convertStop)
			{
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
							new Action(() => parent.statusProgressBar.Value = ard.Position));
				if (detectEof(ref ard)) break;

				decodeTimeStamp(ref ard, ref timeStampO, true);

				if (timeStampO.stopEvent > 0)
				{
					timeStampO.metadataPresent = 1;
					csv.Write(System.Text.Encoding.ASCII.GetBytes(groupConverter(ref timeStampO, new double[] { 0, 0, 0 }, shortFileName)));
					break;
				}

				try
				{
					csv.Write(System.Text.Encoding.ASCII.GetBytes(
						groupConverter(ref timeStampO, extractGroup(ref ard, ref timeStampO), shortFileName)));
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message);
				}

			}

			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Value = ard.Position));

			csv.Close();
			ard.Close();

			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.nextFile()));
		}

		private double[] extractGroup(ref MemoryStream ard, ref timeStamp tsc)
		{
			byte[] group = new byte[2000];
			bool badGroup = false;
			int position = 0;
			int dummy, dummyExt1, dummyExt2;
			ushort badPosition = 600;

			if (ard.Position == ard.Length) return lastGroup;



			do
			{

				dummy = ard.ReadByte();
				if (dummy == 0xab)
				{
					if (ard.Position < ard.Length)
					{
						dummyExt1 = ard.ReadByte();
					}
					else
					{
						return lastGroup;
					}

					if (dummyExt1 == 0x01)
					{
						dummyExt2 = ard.ReadByte();
						if (dummyExt2 >= 0x80)
						{
							if ((dummyExt2 & 0x40) == 0x40)
							{
								dummy = ard.ReadByte();
								while (dummy != 0xab)
								{
									dummy = ard.ReadByte();
								}
								ard.Read(new byte[0x3e], 0, 0x3e);
								dummy = 0xab;
							}
							else if ((dummyExt2 & 8) == 8)
							{
								group[position] = 0xab;
								position += 1;
								dummy = 0;
							}
						}
						else
						{
							ard.Position -= 2;
						}
					}
					else
					{
						ard.Position -= 1;
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
					}
				}
			} while ((dummy != (byte)0xab) & (ard.Position < ard.Length));

			try
			{
				if (ard.ReadByte() == 2)
				{
					tsc.tsType = tsc.tsTypeExt1 = tsc.tsTypeExt2 = 0;
				}
			}
			catch
			{
				return lastGroup;
			}

			decodeTimeStamp(ref ard, ref tsc, false);

			tsc.timeStampLength = (int)(position / (3 + bit));
			if (position == 0)
			{
				return new double[0];
			}

			int resultCode = 0;
			double[] doubleResultArray = new double[nOutputs * 3];
			if (bit == 0)   //ricampionamento a 8 bit
			{
				resultCode = resample3(group, tsc.timeStampLength, doubleResultArray, nOutputs);
			}
			else if (bit == 1)  //ricampionamento a 10 bit
			{
				resultCode = resample4(group, tsc.timeStampLength, doubleResultArray, nOutputs);
			}
			else if (bit == 2) //ricampionamento a 12 bit
			{

				resultCode = resample5(group, (int)tsc.timeStampLength, doubleResultArray, nOutputs);
			}
			return doubleResultArray;
		}

		private string groupConverter(ref timeStamp tsLoc, double[] group, string unitName)
		{
			if (group.Length == 0)
			{
				if (nOutputs == 0)
				{
					group = new double[3] { 0, 0, 0 };
					iend1 = 3;
					iend2 = 0;
				}
				else
				{
					group = new double[3] { double.Parse(gruppoCON[2], new CultureInfo("en-US")) / gCoeff,
											double.Parse(gruppoCON[3], new CultureInfo("en-US")) / gCoeff,
											double.Parse(gruppoCON[4], new CultureInfo("en-US")) / gCoeff };
					iend1 = 3;
					iend2 = 0;
				}
			}

			string textOut;

			gruppoCON[1] = tsLoc.orario.ToString(dateTimeFormat);

			gruppoCON[2] = (group[0] * gCoeff).ToString(cifreDecString, nfi);
			gruppoCON[3] = (group[1] * gCoeff).ToString(cifreDecString, nfi);
			gruppoCON[4] = (group[2] * gCoeff).ToString(cifreDecString, nfi);

			textOut = string.Join(csvSeparator, gruppoCON) + "\r\n";

			if (iend2 == 0) return textOut;

			string[] gruppo = gruppoCON;

			if (!repeatEmptyValues)
			{
				gruppo = gruppoSENZA;
			}

			for (int i = 3; i < iend1; i += 3)
			{
				tsLoc.orario = tsLoc.orario.AddMilliseconds(addMilli);
				gruppo[1] = tsLoc.orario.ToString(dateTimeFormat);

				gruppo[2] = (group[i] * gCoeff).ToString(cifreDecString, nfi);
				gruppo[3] = (group[i + 1] * gCoeff).ToString(cifreDecString, nfi);
				gruppo[4] = (group[i + 2] * gCoeff).ToString(cifreDecString, nfi);

				textOut += string.Join(csvSeparator, gruppo) + "\r\n";
			}

			tsLoc.orario = tsLoc.orario.AddMilliseconds(addMilli);
			gruppo[1] = tsLoc.orario.ToString(dateTimeFormat);

			gruppo[2] = (group[iend1] * gCoeff).ToString(cifreDecString, nfi);
			gruppo[3] = (group[iend1 + 1] * gCoeff).ToString(cifreDecString, nfi);
			gruppo[4] = (group[iend1 + 2] * gCoeff).ToString(cifreDecString, nfi);
			if (magEn == 2)
			{
				gruppo[magx] = tsLoc.magX[1].ToString("#0.0", nfi);
				gruppo[magy] = tsLoc.magY[1].ToString("#0.0", nfi);
				gruppo[magz] = tsLoc.magZ[1].ToString("#0.0", nfi);
				textOut += string.Join(csvSeparator, gruppo) + "\r\n";
				if (!repeatEmptyValues)
				{
					gruppo[magx] = gruppo[magy] = gruppo[magz] = "";
				}
			}
			else
			{
				textOut += string.Join(csvSeparator, gruppo) + "\r\n";
			}

			//if (addMilli2Hz > 0)
			//{
			//	tsLoc.orario = tsLoc.orario.AddMilliseconds(addMilli2Hz);
			//	gruppo[1] = tsLoc.orario.ToString(dateTimeFormat);
			//	if (!repeatEmptyValues)
			//	{
			//		gruppo[2] = gruppo[3] = gruppo[4] = "";
			//	}
			//	gruppo[magx] = tsLoc.magX[1].ToString(cifreDecString, nfi);
			//	gruppo[magy] = tsLoc.magY[1].ToString(cifreDecString, nfi);
			//	gruppo[magz] = tsLoc.magZ[1].ToString(cifreDecString, nfi);
			//	textOut += string.Join(csvSeparator, gruppo) + "\r\n";
			//	if (!repeatEmptyValues)
			//	{
			//		gruppo[magx] = gruppo[magy] = gruppo[magz] = "";
			//	}
			//}
			for (int i = iend1 + 3; i < iend2; i += 3)
			{
				tsLoc.orario = tsLoc.orario.AddMilliseconds(addMilli);
				gruppo[1] = tsLoc.orario.ToString(dateTimeFormat);

				gruppo[2] = (group[i] * gCoeff).ToString(cifreDecString, nfi);
				gruppo[3] = (group[i + 1] * gCoeff).ToString(cifreDecString, nfi);
				gruppo[4] = (group[i + 2] * gCoeff).ToString(cifreDecString, nfi);

				textOut += string.Join(csvSeparator, gruppo) + "\r\n";
			}

			return textOut;
		}

		private void decodeTimeStamp(ref MemoryStream ard, ref timeStamp tsc, bool header)
		{
		_inizio:

			if (!header) goto _footer;

			tsc.ardPosition = ard.Position;

			tsc.tsTypeExt1 = 0;
			//tsc.tsTypeExt2 = 0;
			tsc.stopEvent = 0;

			tsc.tsType = ard.ReadByte();

			//Timestamp esteso
			if ((tsc.tsType & 1) == 1)
			{
				try
				{
					tsc.tsTypeExt1 = ard.ReadByte();
					//if ((tsc.tsTypeExt1 & 1) == 1)
					//{
					//	tsc.tsTypeExt2 = ard.ReadByte();
					//}
					//else
					//{
					//	tsc.tsTypeExt2 = 0;
					//}
				}
				catch
				{
					return;
				}
			}
			//else
			//{
			//	tsc.tsTypeExt1 = 0;
			//}

			//Temperatura
			if (temperatureEn > 0)
			{
				if ((tsc.tsType & 2) == 2)
				{
					if (isDepth == 0)
					{
						try
						{
							tsc.temperature = ard.ReadByte() * 256 + ard.ReadByte();
						}
						catch
						{
							return;
						}
						if (tsc.temperature > 127) tsc.temperature -= 256;
						tsc.temperature += 25;
						gruppoCON[temp] = tsc.temperature.ToString();
					}
				}
				else
				{
					gruppoCON[temp] = "";
				}
			}
			//Evento
			if ((tsc.tsType & 32) == 32)
			{

				for (int i = 0; i < 5; i++)
				{
					eventAr[i] = (byte)ard.ReadByte();
				}
				if (eventAr[0] == 11) { tsc.stopEvent = 1; gruppoCON[meta] = "Low battery."; addMilli = 0; }
				else if (eventAr[0] == 12) { tsc.stopEvent = 2; gruppoCON[meta] = "Power off command."; addMilli = 0; }
				else if (eventAr[0] == 13) { tsc.stopEvent = 3; gruppoCON[meta] = "Memory full."; addMilli = 0; }
				else if (eventAr[0] == 14) { tsc.stopEvent = 3; gruppoCON[meta] = "Remote connection established."; addMilli = 0; }
				else
				{
					if (metadata == 1)
					{
						string eve;
						eve = "Switching to ";
						if (nOutputs == 0)
						{
							eve += "OFF  ";
						}
						else
						{
							eve += nOutputs.ToString() + "Hz " + Math.Pow(2, (range + 1)).ToString() + "g " + (8 + (bit * 2)).ToString() + "bit  ";
						}
						//sviluppo
						eve += tsc.ardPosition.ToString("X");
						///sviluppo
						gruppoCON[meta] = eve;
					}
				}
			}
			else
			{
				if (metadata == 1)
				{
					gruppoCON[meta] = "";
				}
			}

			//tsc.orario = tsc.orario.AddSeconds(1);
			//tsc.orario.AddMilliseconds(-tsc.orario.Millisecond);
			tsc.orario = tsc.orario.AddMilliseconds(addMilli);

			if (tsc.tsTypeExt1 == 0)
			{
				goto _fineDecodeTimestamp; //Non ci sono informazioni dal timestamp esteso 1
			}

			//Cambio Schedule
			if ((tsc.tsTypeExt1 & 16) == 16)
			{
				tsc.metadataPresent = 1;
				rate = ard.ReadByte();
				range = ard.ReadByte();
				bit = ard.ReadByte();
				ard.ReadByte(); ard.ReadByte(); //Salta i due byte del microschedule perché ancora non implementato
				switch (rate)
				{
					case 0:
						nOutputs = 0;
						addMilli = 0;
						iend2 = 0;
						break;
					case 1:
						nOutputs = 1;
						addMilli = 1000;
						iend1 = 3;
						iend2 = 0;
						break;
					case 2:
						nOutputs = 10;
						addMilli = 100;
						iend1 = 15;
						iend2 = 30;
						break;
					case 3:
						nOutputs = 25;
						addMilli = 40;
						iend1 = 36;
						iend2 = 75;
						break;
					case 4:
						nOutputs = 50;
						addMilli = 20;
						iend1 = 75;
						iend2 = 150;
						break;
					case 5:
						nOutputs = 100;
						addMilli = 10;
						iend1 = 150;
						iend2 = 300;
						break;
				}

				findBits(bit);

				Array.Resize(ref lastGroup, (nOutputs * (3 + bit)));

			}

			//Timestamp esteso 1: Orario
			if (((tsc.tsTypeExt1 & 32) == 32) && (!overrideTime))
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

				tsc.orario = new DateTime(anno, mese, giorno, ore, minuti, secondi, 0);

			}

			if ((tsc.tsTypeExt1 & 0x80) == 0x80)
			{
				if ((tsc.tsTypeExt1 & 0x40) == 0x40)
				{
					byte t = 0;
					while (t != 0xab) t = (byte)ard.ReadByte();
					ard.Read(new byte[0x3e], 0, 0x3e);
					header = true;
					goto _inizio;
				}
			}

		_fineDecodeTimestamp:

			return;

		_footer:

			//Pressione e temperatura
			if (pressureEn > 0)
			{
				if ((tsc.tsType & 4) == 4)
				{
					try
					{
						dt5837(ref ard, ref tsc);   //Tiene conto anche della pressione se tsType & 4 == 4
						gruppoCON[temp] = tsc.temperature.ToString("0.00");   //Sviluppo: trovare la giusta formattazione per temperatura e pressione da sensore esterno
						gruppoCON[press] = tsc.pressure.ToString("0.00");
					}
					catch
					{
						return;
					}
				}
				else
				{
					gruppoCON[temp] = "";
					gruppoCON[press] = "";
				}
			}
			//Batteria
			if (prefBattery == 1)
			{
				if ((tsc.tsType & 8) == 8)
				{
					tsc.batteryLevel = (((ard.ReadByte() * 256.0 + ard.ReadByte()) * 6) / 4096);
					gruppoCON[batt] = tsc.batteryLevel.ToString("0.00");
				}
				else
				{
					gruppoCON[batt] = "";
				}
			}

			//if (tsc.tsTypeExt1 == 0)
			//{
			//	goto _fineDecodeTimestampFooter; //Non ci sono informazioni dal timestamp esteso 1
			//}

			//Timestamp esteso 1: Magnetometro
			if (magEn > 0)
			{
				if ((tsc.tsTypeExt1 & 8) == 8)
				{
					for (int i = 0; i < magEn; i++)
					{

						tsc.magX[i] = ard.ReadByte();
						tsc.magX[i] += (ard.ReadByte() * 256);
						if (tsc.magX[i] > 32767)
						{
							tsc.magX[i] -= 65536;
						}
						tsc.magX[i] *= 1.5;

						tsc.magY[i] = ard.ReadByte();
						tsc.magY[i] += (ard.ReadByte() * 256);
						if (tsc.magY[i] > 32767)
						{
							tsc.magY[i] -= 65536;
						}
						tsc.magY[i] *= 1.5;

						tsc.magZ[i] = ard.ReadByte();
						tsc.magZ[i] += (ard.ReadByte() * 256);
						if (tsc.magZ[i] > 32767)
						{
							tsc.magZ[i] -= 65536;
						}
						tsc.magZ[i] *= 1.5;
					}
					gruppoCON[magx] = tsc.magX[0].ToString("#0.0", nfi);
					gruppoCON[magy] = tsc.magY[0].ToString("#0.0", nfi);
					gruppoCON[magz] = tsc.magZ[0].ToString("#0.0", nfi);
				}
				else
				{
					gruppoCON[magx] = gruppoCON[magy] = gruppoCON[magz] = "";
				}
			}

			//_fineDecodeTimestampFooter:
			ard.ReadByte();
			return;

		}

		private void csvPlaceHeader(ref BinaryWriter csv)
		{

			int contoPlace;

			string csvHeader = "Tag ID";
			if (prefs[pref_sameColumn] == "True")
			{
				csvHeader = csvHeader + csvSeparator + "Timestamp";
			}
			else
			{
				csvHeader = csvHeader + csvSeparator + "Date" + csvSeparator + "Time";
			}

			csvHeader = csvHeader + csvSeparator + "X" + csvSeparator + "Y" + csvSeparator + "Z";
			contoPlace = 5;

			if (temperatureEn > 0)
			{
				csvHeader = csvHeader + csvSeparator + "Temp. (°C)";
				contoPlace++;
			}
			else
			{
				press--;
				batt--;
				meta--;
				magx--;
				magy--;
				magz--;
			}

			if (pressureEn > 0)
			{
				csvHeader = csvHeader + csvSeparator + "Press. (mBar)";
				contoPlace++;
			}
			else
			{
				batt--;
				meta--;
				magx--;
				magy--;
				magz--;
			}

			if (magEn > 0)
			{
				csvHeader += csvSeparator + "MagX";
				csvHeader += csvSeparator + "MagY";
				csvHeader += csvSeparator + "MagZ";
				contoPlace += 3;
			}
			else
			{
				batt -= 3;
				meta -= 3;
			}

			if (prefBattery == 1)
			{
				csvHeader = csvHeader + csvSeparator + "Batt. V. (V)";
				contoPlace++;
			}
			else
			{
				meta--;
			}

			if (metadata == 1)
			{
				csvHeader = csvHeader + csvSeparator + "Metadata";
				contoPlace++;
			}

			gruppoCON = Enumerable.Repeat("", contoPlace).ToArray();
			gruppoSENZA = Enumerable.Repeat("", contoPlace).ToArray();

			csvHeader += "\r\n";
			csv.Write(Encoding.ASCII.GetBytes(csvHeader));
		}

		void writeInfo(string fileNameInfo)
		{
			//Scrive l'l'intestazione nel file info
			File.WriteAllText(fileNameInfo, Path.GetFileNameWithoutExtension(fileNameInfo) + "\r\n\r\n");

			string en = "dis";
			if (temperatureEn == 1) en = "en";
			File.AppendAllText(fileNameInfo, string.Format("Temperature logging is {0}abled.\r\n", en));
			en = "dis";
			if (pressureEn == 1) en = "en";
			File.AppendAllText(fileNameInfo, string.Format("Pressure logging is {0}abled.\r\n", en));
			if (temperatureEn == 1 | pressureEn == 1)
			{
				File.AppendAllText(fileNameInfo, string.Format("Temperature and/or Pressure logging period is {0} second(s)\r\n", dtPeriod.ToString()));
			}
			File.AppendAllText(fileNameInfo, "\r\n");

			en = "dis";
			if (magEn > 0) en = "en";
			File.AppendAllText(fileNameInfo, string.Format("Magnetometer is {0}abled", en));
			if (magEn > 0)
			{
				File.AppendAllText(fileNameInfo, string.Format(" and is sampled at {0}Hz", magEn));
			}
			File.AppendAllText(fileNameInfo, ".\r\n\r\n");

			if (schedule == null) return;

			string descSched = "";
			int nInt = 0;
			int start = 0;
			for (int i = 0; i < 5; i++)
			{
				int j = i * 6;
				nInt++;
				descSched += ("Interval #" + nInt.ToString() + string.Format(" from {0} to {1}: accelerometer ", start.ToString("x2"), schedule[j].ToString("x2")));
				start = schedule[j];
				if (schedule[j + 1] == 0)
				{
					descSched += "is off.\r\n";
				}
				else
				{
					int hz = 1;
					switch (schedule[j + 1])
					{
						case 1: hz = 1; break;
						case 2: hz = 10; break;
						case 3: hz = 25; break;
						case 4: hz = 50; break;
						case 5: hz = 100; break;
					}

					int fs = 1;
					switch (schedule[j + 2])
					{
						case 0: fs = 2; break;
						case 1: fs = 4; break;
						case 2: fs = 8; break;
						case 3: fs = 16; break;
					}

					int bit = 1;
					switch (schedule[j + 3])
					{
						case 0: bit = 8; break;
						case 1: bit = 10; break;
						case 2: bit = 12; break;
					}

					descSched += "runs at " + hz.ToString() + "Hz with a full scale of +/- " + fs.ToString() + "g and a resolution of " + bit.ToString() + " bit.\r\n";
				}
				if (schedule[j] == 0x24) break;
			}
			descSched += "\r\n";
			File.AppendAllText(fileNameInfo, string.Format("Day is divided into {0} interval(s).\r\n", nInt));
			File.AppendAllText(fileNameInfo, descSched);
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

			dt.AddSeconds(19);

			return dt;
		}

		//private byte findRange(byte rangeIn)
		//{
		//	byte rangeOut = (byte)(Math.Pow(2, (rangeIn + 1)));

		//	return rangeOut;
		//}

		private void findTDEnable(int td)
		{
			temperatureEn = 0;
			isDepth = 0;
			if ((td & 0b1) == 0b1) temperatureEn = 1;
			if ((td & 0b10) == 0b10) isDepth = 1;

			pressureEn = td >> 4;
		}

		private void findMagEnable(int m)
		{
			magEn = m;
		}

		private void findBits(int bitsIn)
		{

			switch (bitsIn)
			{
				case 0:
					switch (range)
					{
						case 0:
							gCoeff = 15.63;
							break;
						case 1:
							gCoeff = 31.26;
							break;
						case 2:
							gCoeff = 62.52;
							break;
						case 3:
							gCoeff = 187.58;
							break;
					}
					break;
				case 1:
					switch (range)
					{
						case 0:
							gCoeff = 3.9;
							break;
						case 1:
							gCoeff = 7.82;
							break;
						case 2:
							gCoeff = 15.63;
							break;
						case 3:
							gCoeff = 46.9;
							break;
					}
					break;
				case 2:
					switch (range)
					{
						case 0:
							gCoeff = 0.98;
							break;
						case 1:
							gCoeff = 1.95;
							break;
						case 2:
							gCoeff = 3.9;
							break;
						case 3:
							gCoeff = 11.72;
							break;
					}
					break;
			}
			gCoeff /= 1000;
			return;
		}

		//private byte findBytesPerSample()
		//{
		//	byte bitsDiv = 3;
		//	if (bit == 10) bitsDiv = 4;
		//	else if (bit == 12) bitsDiv = 5;
		//	return bitsDiv;
		//}

		private void dt5837(ref MemoryStream ard, ref timeStamp tsc)
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
				return;
			}

			dT = d2 - convCoeffs[4] * 256;
			tsc.temperature = 2000 + (dT * convCoeffs[5]) / 8388608;
			off = convCoeffs[1] * 65536 + (convCoeffs[3] * dT) / 128;
			sens = convCoeffs[0] * 32768 + (convCoeffs[2] * dT) / 256;
			if (tsc.temperature > 2000)
			{
				tsc.temperature -= (2 * Math.Pow(dT, 2)) / 137438953472;
				off -= ((Math.Pow((tsc.temperature - 2000), 2)) / 16);
			}
			else
			{
				off -= 3 * ((Math.Pow((tsc.temperature - 2000), 2)) / 2);
				sens -= 5 * ((Math.Pow((tsc.temperature - 2000), 2)) / 8);
				if (tsc.temperature < -1500)
				{
					off -= 7 * Math.Pow((tsc.temperature + 1500), 2);
					sens -= 4 * Math.Pow((tsc.temperature + 1500), 2);
				}
				tsc.temperature -= 3 * (Math.Pow(dT, 2)) / 8589934592;
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
					return;
				}
				tsc.pressure = (((d1 * sens / 2097152) - off) / 81920);
				if (inMeters)
				{
					tsc.pressure -= pressOffset;
					if (tsc.pressure <= 0) tsc.pressure = 0;
					else
					{
						tsc.pressure = tsc.pressure / 98.1;
						//tsc.press = Math.Round(tsc.press, 2);
					}
				}
			}
		}

		private bool detectEof(ref MemoryStream ard)
		{
			if (ard.Position >= ard.Length)
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
