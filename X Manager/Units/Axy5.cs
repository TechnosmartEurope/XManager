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
			public double magX;
			public double magY;
			public double magZ;
			public DateTime orario;
			public int stopEvent;
			public int timeStampLength;
			public int metadataPresent;
			public long ardPosition;
		}

		byte dateFormat;
		byte timeFormat;
		bool sameColumn = false;
		int prefBattery = 0;
		bool repeatEmptyValues = true;
		//byte bitsDiv;
		bool angloTime = false;
		bool inMeters;
		double gCoeff;
		string dateFormatParameter;
		ushort addMilli;
		CultureInfo dateCi;
		//byte cifreDec;
		const string cifreDecString = "0.00000";
		int metadata = 0;
		bool overrideTime;
		int temperatureEn;
		int isDepth;
		int pressureEn;
		double pressOffset;
		int magEn;
		byte[] eventAr;
		double[] convCoeffs;
		int rate;
		int range;
		int bit;
		double x, y, z;
		NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;

		public Axy5(object p)
			: base(p)
		{
			base.positionCanSend = false;
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

		public override uint askMemory()
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
			memory = m;
			return memory;
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
			string fileNameMdp = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + ".mdp";
			var fo = new System.IO.BinaryWriter(File.Open(fileNameMdp, fm));

			byte downInit = 0;
			byte dummy;

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


				downInit = (byte)sp.ReadByte();         // <- S

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
				if (((actMemory % 0x2000000) == 0) | (firstLoop))       // A
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
					if ((actMemory % 0x20000) == 0)         //a
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
			for (int i = firmwareArray.Length; i <= 2; i++)
			{
				fo.Write(new byte[] { 0xff }, 0, 1);
			}
			fo.Write(new byte[] { model_axy5, (byte)254 }, 0, 2);

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
			mdrSpeed=8;
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
			const int pref_pressMetri = 0;
			const int pref_millibars = 1;
			const int pref_dateFormat = 2;
			const int pref_timeFormat = 3;
			const int pref_fillEmpty = 4;
			const int pref_sameColumn = 5;
			const int pref_battery = 6;
			const int pref_override_time = 15;
			const int pref_metadata = 16;

			string barStatus = "";
			string[] prefs = System.IO.File.ReadAllLines(parent.prefFile);

			//Imposta le preferenze di conversione
			if ((prefs[pref_fillEmpty] == "False")) repeatEmptyValues = false;

			dateSeparator = csvSeparator;
			if ((prefs[pref_sameColumn] == "True"))
			{
				sameColumn = true;
				dateSeparator = " ";
			}

			if (prefs[pref_battery] == "True") prefBattery = 1;

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
			BinaryReader ardFile = new System.IO.BinaryReader(System.IO.File.Open(fileName, FileMode.Open));
			byte[] ardBuffer = new byte[ardFile.BaseStream.Length];
			ardFile.Read(ardBuffer, 0, (int)ardFile.BaseStream.Length);
			ardFile.Close();

			MemoryStream ard = new MemoryStream(ardBuffer);

			//BinaryReader ard = new System.IO.BinaryReader(System.IO.File.Open(fileName, FileMode.Open));
			BinaryWriter csv = new System.IO.BinaryWriter(System.IO.File.OpenWrite(fileNameCsv));

			ard.Position = 1;
			firmTotA = (uint)(ard.ReadByte() * 1000000 + ard.ReadByte() * 1000 + ard.ReadByte());

			//Legge i parametri di logging
			convCoeffs = new double[6];
			double convSum = 0;
			for (int convc = 0; convc < 6; convc++)
			{
				convCoeffs[convc] = ard.ReadByte() * 256 + ard.ReadByte();
				convSum += convCoeffs[convc];
			}

			findTDEnable(ard.ReadByte());   //Temperatura e pressione abilitate
			ard.ReadByte();                 //TD periodo di logging (non serve al software)
			findMagEnable(ard.ReadByte());  //Magnetometro abilitato
			ard.ReadByte();                 //Byte per futura estensione dell'header

			eventAr = new byte[5];

			//cifreDec = 3; cifreDecString = "0.000";
			//if (bit == 4)
			//{
			//	cifreDec = 4; cifreDecString = "0.0000";
			//}
			//else if (bit == 5)
			//{
			//	cifreDec = 5; cifreDecString = "0.00000";
			//}

			timeStamp timeStampO = new timeStamp();
			timeStampO.tsType = timeStampO.tsTypeExt1 = timeStampO.tsTypeExt2 = 0;
			timeStampO.metadataPresent = 0;

			timeStampO.orario = findStartTime(ref prefs);

			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => barStatus = (string)parent.statusLabel.Content));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.IsIndeterminate = false));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
				new Action(() => parent.statusLabel.Content = barStatus + " - Converting"));

			shortFileName = Path.GetFileNameWithoutExtension(fileName);

			convertStop = false;

			ard.ReadByte();

			csvPlaceHeader(ref csv);

			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
				new Action(() => parent.statusProgressBar.Maximum = ard.Length - 1));

			while (!convertStop)
			{
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
							new Action(() => parent.statusProgressBar.Value = ard.Position));
				if (detectEof(ref ard)) break;

				decodeTimeStamp(ref ard, ref timeStampO);

				if (timeStampO.stopEvent > 0)
				{
					timeStampO.metadataPresent = 1;
					csv.Write(System.Text.Encoding.ASCII.GetBytes(groupConverter(ref timeStampO, lastGroup, shortFileName)));
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
			int dummy, dummyExt;
			ushort badPosition = 600;

			if (ard.Position == ard.Length) return lastGroup;

			do
			{
				dummy = ard.ReadByte();
				if (dummy == 0xab)
				{
					if (ard.Position < ard.Length)
					{
						dummyExt = ard.ReadByte();
					}
					else
					{
						return lastGroup;
					}

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
							//System.IO.File.AppendAllText(((FileStream)ard).Name + "errorList.txt", "-> " + ard.Position.ToString("X8") + "\r\n");
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
			} while ((dummy != (byte)0xab) & (ard.Position < ard.Length));

			//Array.Resize(ref group, (int)position);

			tsc.timeStampLength = (int)(position / (3 + bit));
			if (position == 0)
			{
				return new double[0];
			}
			//sviluppo
			//contoFreq++;
			//mediaFreq += tsc.timeStampLength;
			///sviluppo

			//IntPtr doubleResultArray = Marshal.AllocCoTaskMem(sizeof(double) * nOutputs * 3);
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
			//doubleResult = new double[(nOutputs * 3)];
			//Marshal.Copy(doubleResultArray, doubleResult, 0, nOutputs * 3);
			//Marshal.FreeCoTaskMem(doubleResultArray);
			return doubleResultArray;
		}

		private string groupConverter(ref timeStamp tsLoc, double[] group, string unitName)
		{
			if (group.Length == 0)
			{
				if (nOutputs == 0)
				{
					group = new double[3] { 0, 0, 0 };
				}
				else
				{
					group = new double[3] { x, y, z };
				}
			}

			string ampm = "";
			string textOut, dateTimeS, additionalInfo;
			string dateS = "";
			int milli;

			int contoTab = 0;

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

			additionalInfo = "";

			//Inserisce la temperatura
			if (temperatureEn > 0)
			{
				contoTab += 1;
				additionalInfo += csvSeparator;
				if (((tsLoc.tsType & 2) == 2) | repeatEmptyValues)
				{
					additionalInfo += tsLoc.temperature.ToString("#0.00", nfi);
				}
			}

			//Inserisce il magnetometro
			if (magEn == 1)
			{
				contoTab += 3;
				if (((tsLoc.tsTypeExt1 & 8) == 8) | repeatEmptyValues)
				{
					additionalInfo += csvSeparator + tsLoc.magX.ToString("#0.0", nfi) + csvSeparator + tsLoc.magY.ToString("#0.0", nfi) + csvSeparator + tsLoc.magZ.ToString("#0.0", nfi);
				}
				else
				{
					additionalInfo += csvSeparator + csvSeparator + csvSeparator;
				}
			}

			//Inserire la pressione in fase di sviluppo depth

			//Inserisce la batteria
			if (prefBattery == 1)
			{
				contoTab += 1;
				additionalInfo += csvSeparator;
				if (((tsLoc.tsType & 8) == 8) | repeatEmptyValues)
				{
					additionalInfo += tsLoc.batteryLevel.ToString("#0.00#", nfi);
				}
			}

			//Inserisce i metadati
			if (metadata == 1)
			{
				contoTab += 1;
				additionalInfo += csvSeparator;
				if (tsLoc.metadataPresent > 0)
				{
					tsLoc.metadataPresent = 0;

					if ((tsLoc.tsTypeExt1 & 0b1_0000) == 0b1_0000)
					{
						additionalInfo += "Switching to ";
						if (nOutputs == 0)
						{
							additionalInfo += "OFF  ";
						}
						else
						{
							additionalInfo += nOutputs.ToString() + "Hz " + Math.Pow(2, (range + 1)).ToString() + "g " + (8 + (bit * 2)).ToString() + "bit  ";
						}
						//sviluppo
						additionalInfo += tsLoc.ardPosition.ToString("X");

					}

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
								additionalInfo += "Remote connection established.";
								break;
						}
						textOut += additionalInfo + "\r\n";
						return textOut;
					}
				}

			}

			textOut += additionalInfo + "\r\n";

			//if (tsLoc.stopEvent > 0) return textOut;

			if (!repeatEmptyValues)
			{
				additionalInfo = "";
				for (ushort ui = 0; ui < contoTab; ui++) additionalInfo += csvSeparator;
			}

			milli += addMilli;
			dateTimeS += ".";
			//if (tsLoc.stopEvent > 0) bitsDiv = 1;

			//var iend = (short)((rate * 3));

			int iend = group.Length;// * 3;

			for (int i = 3; i < iend; i += 3)
			{
				x = group[i];
				y = group[i + 1];
				z = group[i + 2];

				x *= gCoeff;// x = Math.Round(x, cifreDec);
				y *= gCoeff;// y = Math.Round(y, cifreDec);
				z *= gCoeff;// z = Math.Round(z, cifreDec);

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

			dt.AddSeconds(19);

			return dt;
		}

		//private ushort findSamplingRate(int rateIn)
		//{
		//	byte rateOut;

		//	switch (rateIn)
		//	{
		//		case 0:
		//			rateOut = 0;
		//			break;
		//		case 1:
		//			rateOut = 1;
		//			break;
		//		case 2:
		//			rateOut = 10;
		//			break;
		//		case 3:
		//			rateOut = 25;
		//			break;
		//		case 4:
		//			rateOut = 50;
		//			break;
		//		default:
		//			rateOut = 100;
		//			break;
		//	}

		//	if (rateOut == 1) addMilli = 0;

		//	else addMilli = (ushort)((1 / (double)rateOut) * 1000);

		//	return rateOut;

		//}

		private byte findRange(byte rangeIn)
		{
			byte rangeOut = (byte)(Math.Pow(2, (rangeIn + 1)));

			return rangeOut;
		}

		private void findTDEnable(int td)
		{
			temperatureEn = 0;
			isDepth = 0;
			if ((td & 0b1) == 0b1) temperatureEn = 1;
			if ((td & 0b10) == 0b10) isDepth = 1;

			pressureEn = td >> 4;
			//temperatureEn = 0;
			//if ((td & 15) == 1)
			//{
			//	temperatureEn = 1;
			//}
			//pressureEn = 0;
			//if ((td >> 4) == 1)
			//{
			//	pressureEn = 1;
			//}
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

		private byte findBytesPerSample()
		{
			byte bitsDiv = 3;
			if (bit == 10) bitsDiv = 4;
			else if (bit == 12) bitsDiv = 5;
			return bitsDiv;
		}

		private void decodeTimeStamp(ref MemoryStream ard, ref timeStamp tsc)
		{

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
			if ((tsc.tsType & 2) == 2)
			{
				if (isDepth == 0)
				{
					try
					{
						tsc.temperature = ((ard.ReadByte() * 256 + ard.ReadByte()) >> 6);
					}
					catch
					{
						return;
					}
					if (tsc.temperature > 511) tsc.temperature -= 1024;
					tsc.temperature = ((tsc.temperature * 0.1221) + 22.5);
				}
				else
				{
					dt5837(ref ard, ref tsc);   //Tiene conto anche della pressione se tsType & 4 == 4
				}
			}

			//Batteria
			if ((tsc.tsType & 8) == 8)
			{
				tsc.batteryLevel = (((ard.ReadByte() * 256.0 + ard.ReadByte()) * 6) / 4096);
			}

			//Evento
			if ((tsc.tsType & 32) == 32)
			{
				for (int i = 0; i < 5; i++)
				{
					eventAr[i] = (byte)ard.ReadByte();
				}
				if (eventAr[0] == 11) tsc.stopEvent = 1;
				else if (eventAr[0] == 12) tsc.stopEvent = 2;
				else if (eventAr[0] == 13) tsc.stopEvent = 3;
			}

			tsc.orario = tsc.orario.AddSeconds(1);

			if (tsc.tsTypeExt1 == 0)
			{
				goto _fineDecodeTimestamp; //Non ci sono informazioni dal timestamp esteso 1
			}

			//Timestamp esteso 1: Magnetometro
			if ((tsc.tsTypeExt1 & 8) == 8)
			{
				tsc.magX = ard.ReadByte();
				tsc.magX += (ard.ReadByte() * 256);
				if (tsc.magX > 32767)
				{
					tsc.magX -= 65536;
				}
				tsc.magX *= 1.5;

				tsc.magY = ard.ReadByte();
				tsc.magY += (ard.ReadByte() * 256);
				if (tsc.magY > 32767)
				{
					tsc.magY -= 65536;
				}
				tsc.magY *= 1.5;

				tsc.magZ = ard.ReadByte();
				tsc.magZ += (ard.ReadByte() * 256);
				if (tsc.magZ > 32767)
				{
					tsc.magZ -= 65536;
				}
				tsc.magZ *= 1.5;
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
						break;
					case 1:
						nOutputs = 1;
						addMilli = 0;
						break;
					case 2:
						nOutputs = 10;
						addMilli = 100;
						break;
					case 3:
						nOutputs = 25;
						addMilli = 40;
						break;
					case 4:
						nOutputs = 50;
						addMilli = 20;
						break;
					case 5:
						nOutputs = 100;
						addMilli = 10;
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

				tsc.orario = new DateTime(anno, mese, giorno, ore, minuti, secondi);

			}

		_fineDecodeTimestamp:

			return;

		}

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
			if (temperatureEn > 0)
			{
				csvHeader = csvHeader + csvSeparator + "Temp. (°C)";
			}

			if (pressureEn > 0)
			{
				csvHeader = csvHeader + csvSeparator + "Press. (mBar)";
			}

			if (magEn > 0)
			{
				csvHeader += csvSeparator + "Mag.X(mgauss)";
				csvHeader += csvSeparator + "Mag.Y(mgauss)";
				csvHeader += csvSeparator + "Mag.Z(mgauss)";
			}

			if (prefBattery == 1)
			{
				csvHeader = csvHeader + csvSeparator + "Batt. V. (V)";
			}

			if (metadata == 1)
			{
				csvHeader = csvHeader + csvSeparator + "Metadata";
			}

			csvHeader += "\r\n";
			csv.Write(System.Text.Encoding.ASCII.GetBytes(csvHeader));
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
