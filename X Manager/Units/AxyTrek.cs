using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows;
using System.Globalization;
using System.Windows.Controls;

namespace X_Manager.Units.AxyTreks
{
	public abstract class AxyTrek : Unit

	{

		protected ushort[] coeffs = new ushort[7];
		protected double[] convCoeffs = new double[7];

		protected bool disposed = false;
		protected bool evitaSoglie = false;

		protected int temperatureEnabled;
		protected int pressureEnabled;
		protected byte range;
		protected ushort rate;
		protected bool bits;
		protected byte bitsDiv;
		protected ushort addMilli;
		protected double gCoeff;
		protected int debugStampId = 13;
		protected int debugStampLenght = 15;

		protected bool adcLog = false;
		protected bool adcStop = false;

		protected uint contoCoord;
		protected bool addGpsTime;
		protected bool angloTime = false;
		protected string dateFormatParameter;
		protected byte dateFormat;
		protected bool inMeters = false;
		protected bool prefBattery = false;
		protected bool repeatEmptyValues = true;
		protected int isDepth = 1;
		protected bool primaCoordinata;
		protected bool sameColumn = false;
		protected string cifreDecString;
		protected bool metadata;
		protected int leapSeconds;
		protected long infRemPosition;
		protected bool makeTxt = false;
		protected bool makeKml = false;
		protected bool removeNonGps = false;

		protected DateTime nullDate = new DateTime(1970, 1, 1, 0, 0, 0);
		protected DateTime recoveryDate = new DateTime(1970, 1, 1, 0, 0, 0);
		protected struct coordKml
		{
			public string cSstring;
			public string cPlacemark;
			public string cName;
			public string cClass;
		}

		protected struct Coord
		{
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
		}

		protected struct Data
		{
			public int anno;
			public int mese;
			public int giorno;
			public int ore;
			public int minuti;
			public int secondi;
		}

		protected AxyTrek(object p)
			: base(p)
		{
			firmwareArray = new byte[6];
			positionCanSend = true;
			configurePositionButtonEnabled = true;
		}

		public override string askBattery()
		{
			string battery = "";
			double battLevel;
			ft.Write("TTTTGGAB");
			try
			{
				battLevel = ft.ReadByte(); battLevel *= 256;
				battLevel += ft.ReadByte();
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
				ft.Write("TTTTTTTGGAN");
				unitNameBack = ft.ReadLine();
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
			ft.Write("TTTTTTTGGAm");
			try
			{
				m = ft.ReadByte(); m *= 256;
				m += ft.ReadByte(); m *= 256;
				m += ft.ReadByte(); m *= 256;
				m += ft.ReadByte();
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
			ft.Write("TTTTTTTGGAM");
			try
			{
				m = ft.ReadByte(); m *= 256;
				m += ft.ReadByte(); m *= 256;
				m += ft.ReadByte(); m *= 256;
				m += ft.ReadByte();
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
			ft.Write("TTTTTTTTGGAE");
			try
			{
				ft.ReadByte();
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
		}

		public override bool getRemote()
		{
			if (firmTotA > 3001000)
			{
				ft.Write("TTTTTTTTTTGGAl");
				try
				{
					if (ft.ReadByte() == 1) remote = true;
				}
				catch { throw new Exception(unitNotReady); }
			}
			return remote;
		}

		public override bool isSolar()
		{
			if (firmTotA >= 3008001)
			{
				ft.Write("TTTTTTTTTTGGAi");
				try
				{
					if (ft.ReadByte() == 1) solar = true;
				}
				catch { throw new Exception(unitNotReady); }
			}
			return remote;
		}

		public override void abortConf()
		{

		}

		public override void setConf(byte[] conf)
		{
			ft.Write("TTTTTTTTTGGAc");
			try
			{
				ft.ReadByte();
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
			ft.Write(conf, 2, 3);
			ft.Write(conf, 15, 7);
			ft.Write(conf, 22, 1);
			if (firmTotA > 2000000)
			{
				ft.Write(conf, 23, 1);
			}
			if (firmTotA >= 3008000)
			{
				ft.Write(conf, 25, 4);
			}
			try
			{
				ft.ReadByte();
				ft.ReadExisting();
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
			if (!evitaSoglie)
			{
				uint[] soglie = calcolaSoglieDepth();
				ft.Write("TTTTTTTTTTGGAG");
				try
				{
					ft.ReadByte();
				}
				catch
				{
					throw new Exception(unitNotReady);
				}
				for (int s = 0; s <= 16; s++)
				{
					ft.Write(new byte[] { BitConverter.GetBytes(soglie[s])[2] }, 0, 1);
					ft.Write(new byte[] { BitConverter.GetBytes(soglie[s])[1] }, 0, 1);
					ft.Write(new byte[] { BitConverter.GetBytes(soglie[s])[0] }, 0, 1);
				}
			}
		}

		public override byte[] getGpsSchedule()
		{
			byte[] schedule = new byte[200];
			ft.ReadExisting();
			ft.Write("TTTTTTTTTTTTTGGAS");
			Thread.Sleep(200);
			try
			{
				for (int i = 0; i <= 63; i++) { schedule[i] = ft.ReadByte(); }
				if (remote) ft.Write(new byte[] { 2 }, 0, 1);

				for (int i = 64; i <= 127; i++) { schedule[i] = ft.ReadByte(); }
				if (remote) ft.Write(new byte[] { 2 }, 0, 1);

				for (int i = 128; i <= 171; i++) { schedule[i] = ft.ReadByte(); }
				if (firmTotB > 3003999)
				{
					for (int i = 172; i <= 178; i++) { schedule[i] = ft.ReadByte(); }
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
			ft.Write("TTTTTTTTTTTTTGGAs");
			Thread.Sleep(200);
			try
			{
				ft.ReadByte();
				ft.Write(schedule, 0, 64);
				if (remote) ft.ReadByte();
				ft.Write(schedule, 64, 64);
				if (remote) ft.ReadByte();
				if (firmTotB < 3004000)
				{
					ft.Write(schedule, 128, 44);
				}
				else
				{
					ft.Write(schedule, 128, 51);
				}

				Thread.Sleep(200);
				ft.ReadByte();
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
		}

		public override string askFirmware()
		{
			byte[] f = new byte[6];
			bool firmValid = false;
			int tentativi = 0;
			string firmware = "";
			uint oldTimeOut = ft.ReadTimeout;

			ft.ReadTimeout = 50;
			while ((!firmValid) && (tentativi < 5))
			{
				ft.ReadExisting();
				ft.Write("TTTTTTTGGAF");
				ft.ReadTimeout = 400;
				Thread.Sleep(600);
				f[0] = f[1] = f[2] = f[3] = f[4] = f[5] = 0xff;
				try
				{
					for (int i = 0; i < 6; i++)
					{
						f[i] = ft.ReadByte();
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
			ft.ReadTimeout = oldTimeOut;
			return firmware;
		}

		public override void setName(string newName)
		{
			if (newName.Length < 10)
			{
				for (int i = newName.Length; i < 10; i++) newName += " ";
				ft.Write("TTTTTTTGGAn");
				try
				{
					ft.ReadByte();
				}
				catch
				{
					throw new Exception(unitNotReady);
				}
				ft.WriteLine(newName);
			}
		}

		public override void disconnect()
		{
			base.disconnect();
			if (!ft.IsOpen)
			{
				ft.Open();
			}
			ft.Write("TTTTTTTGGAO");
		}

		public unsafe override void download(string fileName, uint fromMemory, uint toMemory, int baudrate)
		{
			convertStop = false;
			uint actMemory = fromMemory;
			FileMode fm = FileMode.Create;
			if (fromMemory != 0) fm = FileMode.Append;
			string fileNameMdp = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + ".mdp";
			var fo = new BinaryWriter(File.Open(fileNameMdp, fm));

			string command = "e";
			if (firmTotA < 1000007) command = "D";

			ft.Write("TTTTTTTTTTTTGGA" + command);
			try
			{
				ft.ReadByte();
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
			if (command == "D") ft.BaudRate = MainWindow.Baudrate_3M;
			else
			{
				byte b = (byte)(baudrate / 1000000);
				ft.Write(new byte[] { b }, 0, 1);
				Thread.Sleep(100);
				ft.BaudRate = (uint)baudrate;
				Thread.Sleep(100);
			}

			Thread.Sleep(200);
			ft.Write("S");
			Thread.Sleep(1000);
			int dieCount = ft.ReadByte();
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


			byte[] outBuffer = new byte[50];
			byte[] inBuffer = new byte[4096];
			byte[] tempBuffer = new byte[2048];
			byte[] address = new byte[8];

			uint bytesToWrite = 0;
			int bytesReturned = 0;

			ft.BaudRate = (uint)baudrate;
			bool firstLoop = true;
			bool mem4 = false;
			if (firmTotA > 2999999) mem4 = true;
			bool success = true;
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
				ft.Write(outBuffer, bytesToWrite);
				bytesReturned = ft.Read(inBuffer, 4096);
				if (bytesReturned < 0)
				{
					success = false;
					break;
				}
				else if (bytesReturned < 4096)
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
								ft.Write(outBuffer, bytesToWrite);
								if (ft.Read(inBuffer, 2048) < 0)
								{
									success = false;
									break;
								}
								fo.Write(inBuffer, 0, 2048);
								actMemory += 2048;
							}
							if (success == false) break;
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
			fo.Write(new byte[] { modelCode, 254 }, 0, 2);

			fo.Close();

			if (!success)
			{
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFailed()));
				try
				{
					fo.Close();
				}
				catch { }
				return;
			}

			outBuffer[0] = 88;
			bytesToWrite = 1;
			ft.Write(outBuffer, bytesToWrite);
			ft.BaudRate = 115200;
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
			FileMode fm = FileMode.Create;
			if (fromMemory != 0) fm = FileMode.Append;
			string fileNameMdp = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + ".mdp";
			var fo = new BinaryWriter(File.Open(fileNameMdp, fm));

			byte mdrSpeed = 9;
			mdrSpeed = 8;
			string br = "D";
			if (mdrSpeed == 9) br = "H";
			ft.Write("TTTTTTTTTTTTTTGGA" + br);
			int dieCount = 0;
			try
			{
				ft.ReadByte();
				Thread.Sleep(100);
				ft.Write("+++");
				Thread.Sleep(200);
				ft.Write(new byte[] { 0x41, 0x54, 0x42, 0x52, mdrSpeed }, 0, 5);
				Thread.Sleep(200);
				if (mdrSpeed == 9) ft.BaudRate = 2000000;
				else ft.BaudRate = 1500000;
				Thread.Sleep(300);
				ft.Write("ATX");
				Thread.Sleep(900);
				ft.Write("R");
				//Thread.Sleep(100);
				dieCount = ft.ReadByte();
				ft.ReadExisting();
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
					ft.ReadByte();
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

			byte[] outBuffer = new byte[50];
			byte[] inBuffer = new byte[4096];
			byte[] tempBuffer = new byte[2048];
			byte[] address = new byte[8];

			uint bytesToWrite = 0;
			int bytesReturned = 0;

			bool firstLoop = true;
			bool mem4 = false;
			bool success = true;
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
				ft.Write(outBuffer, bytesToWrite);
				bytesReturned = ft.Read(inBuffer, 4096);

				if (bytesReturned < 0)
				{
					success = false;
					break;
				}
				else if (bytesReturned < 4096)
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
								ft.Write(outBuffer, bytesToWrite);
								if (ft.Read(inBuffer, 2048) < 2048)
								{
									success = false;
									break;
								}
								fo.Write(inBuffer, 0, 2048);
								actMemory += 2048;
							}
							if (!success) break;
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
			fo.Write(new byte[] { modelCode, 254 }, 0, 2);
			fo.Close();

			if (!success)
			{
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFailed()));
				try
				{
					fo.Close();
				}
				catch { }
				return;
			}

			outBuffer[0] = 88;
			bytesToWrite = 1;
			ft.Write(outBuffer, bytesToWrite);

			Thread.Sleep(50);
			ft.Write("+++");
			Thread.Sleep(200);
			ft.Write(new byte[] { 0x41, 0x54, 0x42, 0x52, 3 }, 0, 5);
			ft.BaudRate = 115200;
			Thread.Sleep(100);
			ft.Write("ATX");

			ft.BaudRate = 115200;

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

		protected void convertInit(string[] prefs)
		{
			debugLevel = parent.stDebugLevel;
			addGpsTime = parent.addGpsTime;
			if (Parent.getParameter("pressureRange") == "air")
			{
				isDepth = 0;
			}
			if (prefs[pref_fillEmpty] == "False")
			{
				repeatEmptyValues = false;
			}
			dateSeparator = csvSeparator;
			if (prefs[pref_sameColumn] == "True")
			{
				sameColumn = true;
				dateSeparator = " ";
			}
			if (addGpsTime)
			{
				repeatEmptyValues = false;
				sameColumn = true;
			}
			if (prefs[pref_txt] == "True") makeTxt = true;
			if (prefs[pref_kml] == "True") makeKml = true;
			if (prefs[pref_battery] == "True") prefBattery = true;
			if (prefs[pref_pressMetri] == "meters") inMeters = true;
			if (prefs[pref_timeFormat] == "2") angloTime = true;

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
		}

		protected string[] convertPrepareOutputFiles(string fileName)
		{
			string addOn = "";
			string exten = Path.GetExtension(fileName);
			if (exten.Length > 4) addOn = "_S" + exten.Remove(0, 4);
			string[] names = new string[6];
			names[0] = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + addOn + ".csv";
			names[1] = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + addOn + ".txt";
			names[2] = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + addOn + "_temp" + ".kml";
			names[3] = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + addOn + ".kml";
			names[4] = exten;
			names[5] = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileName(fileName) + ".log";
			try
			{
				fDel(names[5]);
			}
			catch { }
			return names;
		}

		protected BinaryWriter[] convertCreateOutputFiles(string[] names, string fileName)
		{
			BinaryWriter csv = new BinaryWriter(File.OpenWrite(names[0]));
			BinaryWriter txt = BinaryWriter.Null;
			BinaryWriter kml = BinaryWriter.Null;
			BinaryWriter placeMark = BinaryWriter.Null;

			if (makeTxt)
			{
				if ((File.Exists(names[1])) & (names[5].Contains("ard"))) fDel(names[1]);
				txt = new BinaryWriter(File.OpenWrite(names[1]));
			}
			if (makeKml)
			{
				if ((File.Exists(names[2])) & (names[5].Contains("ard"))) fDel(names[2]);
				if ((File.Exists(names[3])) & (names[5].Contains("ard"))) fDel(names[3]);
				kml = new BinaryWriter(File.OpenWrite(names[2]));
				placeMark = new BinaryWriter(File.OpenWrite(names[3]));
				primaCoordinata = true;
				contoCoord = 0;
				//string
				kml.Write(Encoding.ASCII.GetBytes(Properties.Resources.Folder_Path_Top));
				kml.Write(Encoding.ASCII.GetBytes(Properties.Resources.Path_Top));
				//placemark
				placeMark.Write(Encoding.ASCII.GetBytes(Properties.Resources.Final_Top_1 +
					Path.GetFileNameWithoutExtension(fileName) + Properties.Resources.Final_Top_2));
			}
			return new BinaryWriter[] { csv, txt, kml, placeMark };
		}

		protected List<long> convertFillSessionList(string exten, BinaryReader ardFile)
		{
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
			ardFile.Close();
			return sesAdd;
		}

		protected BinaryReader convertOpenArdFile(string fileName)
		{
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
						//Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
						//				new Action(() => parent.nextFile()));
						ardFile = null;
					}
					Thread.Sleep(1000);
				}
			}
			return ardFile;
		}

		protected MemoryStream convertExtractSession(string fileName, List<long> sesAdd, BinaryWriter txt, int sesCounter)
		{
			BinaryReader ardFile = new BinaryReader(File.Open(fileName, FileMode.Open));
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
			byte[] ardBuffer = new byte[bufLen];
			ardFile.Read(ardBuffer, 0, (int)bufLen);

			ardFile.Close();

			MemoryStream ard = new MemoryStream(ardBuffer);
			ard.Position = 1;
			firmTotA = (uint)ard.ReadByte() * 1000000 + (uint)ard.ReadByte() * 1000 + (uint)ard.ReadByte();
			firmTotB = (uint)ard.ReadByte() * 1000000 + (uint)ard.ReadByte() * 1000 + (uint)ard.ReadByte();

			if (debugLevel > 0)
			{
				txt.Write(Encoding.ASCII.GetBytes("\r\n********************************* SESSION #" + sesCounter.ToString() + " (0x" +
				(ard.Position + infRemPosition).ToString("X4") + ")\r\n"));
			}

			return ard;
		}

		protected virtual double[] extractGroup(ref MemoryStream ard, ref int timeStampLength)
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

			timeStampLength = group.Count / bitsDiv;

			int resultCode = 0;
			if (group.Count == 0)
			{
				return new double[] { };
			}

			double[] doubleResult = new double[3 * nOutputs];
			if (bits)
			{
				resultCode = resample4(group.ToArray(), timeStampLength, doubleResult, nOutputs);
			}
			else
			{
				resultCode = resample3(group.ToArray(), timeStampLength, doubleResult, nOutputs);
			}
			return doubleResult;
		}

		protected uint[] calcolaSoglieDepth()
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
				c[count - 1] = coeffs[count];
			}
			dT = d2 - c[4] * 256;
			temp = 2000 + dT * c[5] / 8388608;
			off = c[1] * 65536 + c[3] * dT / 128;
			sens = c[0] * 32768 + c[2] * dT / 256;
			if (temp > 2000)
			{
				temp -= 2 * Math.Pow(dT, 2) / 137438953472;
				off -= Math.Pow(temp - 2000, 2) / 16;
			}
			else
			{
				off -= 3 * Math.Pow(temp - 2000, 2) / 2;
				sens -= 5 * Math.Pow(temp - 2000, 2) / 8;
				if (temp < -1500)
				{
					off -= 7 * Math.Pow(temp + 1500, 2);
					sens -= 4 * Math.Pow(temp + 1500, 2);
				}
				temp -= 3 * Math.Pow(dT, 2) / 8589934592;
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
				c[count] = coeffs[count];
			}
			dt = d2 - c[5] * 256;
			temp = 2000 + (dt * c[6] / 8388608);
			if ((temp / 100) < 20) ti = 3 * Math.Pow(dt, 2) / 8388608;
			else ti = 2 * Math.Pow(dt, 2) / 137438953472;
			temp = (temp - ti) / 100;
			return temp;
		}

		protected void checkTdEn(byte bin, object unit, string ex)
		{
			pressureEnabled = bin;
			temperatureEnabled = pressureEnabled;
			pressureEnabled /= 16;
			temperatureEnabled &= 15;
			if (unit is AxyTrekFT || unit is AxyTrekCO2)
			{
				temperatureEnabled = 1;
			}
			if (ex.Contains("rem"))
			{
				pressureEnabled = 1;
				if (temperatureEnabled == 0)
				{
					temperatureEnabled = 1;
				}
			}
		}

		protected void checkAccParam(byte bin)
		{
			rate = findSamplingRate(bin);
			range = findRange(bin);
			bits = findBits(bin);
			bitsDiv = findBytesPerSample();
			nOutputs = rate;
		}

		protected virtual void csvPlaceHeader(ref BinaryWriter csv)
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

			csv.Write(Encoding.ASCII.GetBytes(csvHeader));
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
			if (rateOut == 1) addMilli = 0;
			else addMilli = (ushort)((1.0 / rateOut) * 1000);

			return rateOut;

		}

		private byte findRange(byte rangeIn)
		{
			rangeIn = unchecked((byte)(rangeIn & 15));
			if (rangeIn > 7)
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

		protected void findDebugStampPar()
		{
			debugStampId = 80;
			debugStampLenght = 15;
			if (firmTotB < 003002000) debugStampId = 13;
			if (firmTotB < 003001000) debugStampId = 200;
			if (firmTotB < 003000000) debugStampId = 13;
			if (firmTotB < 001000004) debugStampLenght = 7;
		}

		protected bool pressureDepth5803(ref MemoryStream ard, ref double temperature, ref double press, double pressOffset, ref int tsType)
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
			temperature = 2000 + dT * convCoeffs[5] / 8_388_608;
			off = convCoeffs[1] * 65_536 + convCoeffs[3] * dT / 128;
			sens = convCoeffs[0] * 32_768 + convCoeffs[2] * dT / 256;
			if (temperature > 2000)
			{
				temperature -= 7 * Math.Pow(dT, 2) / 137_438_953_472;
				off -= Math.Pow(temperature - 2000, 2) / 16;
			}
			else
			{
				temperature -= 3 * Math.Pow(dT, 2) / 8_589_934_592;
				off -= 3 * (Math.Pow(temperature - 2000, 2) / 2);
				sens -= 5 * (Math.Pow(temperature - 2000, 2) / 8);
				if (temperature < -1500)
				{
					off -= 7 * Math.Pow(temperature + 1500, 2);
					sens -= 4 * Math.Pow(temperature + 1500, 2);
				}
			}
			temperature = temperature / 100;
			if ((tsType & 4) == 4)
			{
				try
				{
					d1 = ard.ReadByte() * 65536 + ard.ReadByte() * 256 + ard.ReadByte();
				}
				catch
				{
					return true;
				}
				press = ((d1 * sens / 2_097_152) - off) / 81_920;
				if (inMeters)
				{
					press -= pressOffset;
					if (press < 0) press = 0;
					else
					{
						press = press / 98.1;
					}
				}
			}
			return false;
		}

		protected bool pressureAir(ref MemoryStream ard, ref double temperature, ref double press, double pressOffset, ref int tsType)
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
			temperature = 2000 + (dT * convCoeffs[5]) / 8388608;
			off = convCoeffs[1] * 131072 + (convCoeffs[3] * dT) / 64;
			sens = convCoeffs[0] * 65536 + (convCoeffs[2] * dT) / 128;
			if (temperature > 2000)
			{
				t2 = 0;
				off2 = 0;
				sens2 = 0;
			}
			else
			{
				t2 = Math.Pow(dT, 2) / 2147483648;
				off2 = 61 * Math.Pow(temperature - 2000, 2) / 16;
				sens2 = 2 * Math.Pow(temperature - 2000, 2);
				if (temperature < -1500)
				{
					off2 += 20 * Math.Pow(temperature + 1500, 2);
					sens2 += 12 * Math.Pow(temperature + 1500, 2);
				}
			}
			temperature -= t2;
			off -= off2;
			sens -= sens2;
			temperature /= 100;

			if ((tsType & 4) == 4)
			{
				try
				{
					d1 = ard.ReadByte() * 65536 + ard.ReadByte() * 256 + ard.ReadByte();
				}
				catch
				{
					return true;
				}
				press = ((d1 * sens / 2097152) - off) / 32768;
				press /= 100;
			}
			return false;
		}

		protected bool pressureDepth5837(ref MemoryStream ard, ref double temperature, ref double press, double pressOffset, ref int tsType)
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
			temperature = 2000 + dT * convCoeffs[5] / 8_388_608;
			off = convCoeffs[1] * 65_536 + convCoeffs[3] * dT / 128;
			sens = convCoeffs[0] * 32_768 + convCoeffs[2] * dT / 256;
			if (temperature > 2000)
			{
				temperature -= 2 * Math.Pow(dT, 2) / 137_438_953_472;
				off -= Math.Pow(temperature - 2000, 2) / 16;
			}
			else
			{
				temperature -= 3 * Math.Pow(dT, 2) / 8_589_934_592;
				off -= 3 * (Math.Pow(temperature - 2000, 2) / 2);
				sens -= 5 * (Math.Pow(temperature - 2000, 2) / 8);
				if (temperature < -1500)
				{
					off -= 7 * Math.Pow(temperature + 1500, 2);
					sens -= 4 * Math.Pow(temperature + 1500, 2);
				}
			}
			temperature /= 100;
			if ((tsType & 4) == 4)
			{
				try
				{
					d1 = ard.ReadByte() * 65536 + ard.ReadByte() * 256 + ard.ReadByte();
				}
				catch
				{
					return true;
				}
				press = (((d1 * sens / 2_097_152) - off) / 81_920);
				if (inMeters)
				{
					press -= pressOffset;
					if (press <= 0) press = 0;
					else
					{
						press = press / 98.1;
					}
				}
			}
			return false;
		}

		protected coordKml decoderKml(ref Coord coord, ref DateTime orario)
		{
			double LATgradi, LATminuti, LONgradi, LONminuti;
			char[] Kmlarr;
			coordKml strOut = new coordKml();

			LATminuti = coord.latMinDecLL + (coord.latMinDecL * 10) + (coord.latMinDecH * 1000);
			LONminuti = coord.lonMinDecLL + (coord.lonMinDecL * 10) + (coord.lonMinDecH * 1000);
			LATminuti = ((LATminuti / 100000) + coord.latMinuti) / 60;
			LATgradi = coord.latGradi + LATminuti;
			LONminuti = ((LONminuti / 100000) + coord.lonMinuti) / 60;
			LONgradi = coord.lonGradi + LONminuti;

			strOut.cSstring = "";

			if (coord.eo == 0) strOut.cSstring += "-";
			strOut.cSstring += LONgradi.ToString("00.000000") + ",";

			if (coord.ns == 0) strOut.cSstring += "-";
			strOut.cSstring += LATgradi.ToString("00.000000") + ",";

			if (coord.altSegno == 1) strOut.cSstring += "-";
			strOut.cSstring += ((coord.altH * 256) + coord.altL).ToString("0000.0");
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
			strOut.cName = orario.ToString("dd/MM/yyyy") + " " + orario.ToString("HH:mm:ss");
			double nVel = coord.vel * 3.6;
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

		protected void kmlWrite(ref Coord coord, ref DateTime orario, ref BinaryWriter kmlW, ref BinaryWriter placeMarkW)
		{
			coordKml coordinataKml = new coordKml();
			byte[] buffer;
			if (coord.nSat > 0)
			{
				coordinataKml = decoderKml(ref coord, ref orario);
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
						Properties.Resources.lookat1 + lookAtValues[0] + Properties.Resources.lookat2 + lookAtValues[1]
						+ Properties.Resources.lookat3 + lookAtValues[2] + Properties.Resources.lookat4);
					placeMarkW.Write(buffer, 0, buffer.Length - 1);
					buffer = Encoding.ASCII.GetBytes(Properties.Resources.Placemarks_Start_Top + coordinataKml.cPlacemark
						+ Properties.Resources.Placemarks_Start_Bot);
					placeMarkW.Write(buffer, 0, buffer.Length);
					placeMarkW.Write(Encoding.ASCII.GetBytes(Properties.Resources.Folder_Generics_Top));
				}
				buffer = Encoding.ASCII.GetBytes(
					Properties.Resources.Placemarks_Generic_Top_1 + coordinataKml.cName + Properties.Resources.Placemarks_Generic_Top_2 + coordinataKml.cClass
					+ Properties.Resources.Placemarks_Generic_Top_3 + ((coord.altH * 256 + coord.altL) * 2).ToString()
					+ Properties.Resources.Placemarks_Generic_Top_4 + (coord.vel * 3.704).ToString()
					+ Properties.Resources.Placemarks_Generic_Top_5 + coordinataKml.cPlacemark + Properties.Resources.Placemarks_Generic_Bot);
				placeMarkW.Write(buffer, 0, buffer.Length);
			}
		}

		protected void txtWrite(ref int tsType, ref Coord coord, ref DateTime orario, ref Data data, ref int gsvSum, ref long ardPosition,
				ref byte[] eventAr, ref BinaryWriter txtW)
		{
			string altSegno, eo, ns, coords;
			var nfi = new CultureInfo("en-US", false).NumberFormat;
			string dateS = orario.ToString(dateFormatParameter);
			string dateTimeS = dateS + csvSeparator + orario.ToString("HH:mm:ss");

			if (((tsType & 32) == 32) && (debugLevel > 0))
			{
				coords = dateTimeS + '\t';
				string coords2 = "";
				decodeEvent(ref coords2, eventAr);
				if (coords2 != "")
				{
					coords += coords2 + "\r\n";
					txtW.Write(Encoding.ASCII.GetBytes(coords));
				}
			}

			if ((tsType & 16) == 16)
			{
				altSegno = "-"; if (coord.altSegno == 0) altSegno = "";
				eo = "-"; if (coord.eo == 1) eo = "";
				ns = "-"; if (coord.ns == 1) ns = "";
				double speed = coord.vel * 3.704;
				double lat, lon; lat = lon = 0;
				lon = ((coord.lonMinuti + (coord.lonMinDecH / (double)100) + (coord.lonMinDecL / (double)10000) + (coord.lonMinDecLL / (double)100000)) / 60) + coord.lonGradi;
				lat = ((coord.latMinuti + (coord.latMinDecH / (double)100) + (coord.latMinDecL / (double)10000) + (coord.latMinDecLL / (double)100000)) / 60) + coord.latGradi;
				coords = dateTimeS;
				coords += "\t" + ns + lat.ToString("#00.00000", nfi);
				coords += "\t" + eo + lon.ToString("#00.00000", nfi);
				coords += "\t" + altSegno + ((coord.altH * 256 + coord.altL) * 2).ToString() + "\t" + speed.ToString("0.0") + "\t";
				coords += coord.nSat.ToString() + "\t" + coord.DOP.ToString() + "." + coord.DOPdec.ToString();
				coords += "\t" + gsvSum.ToString();
				if (debugLevel > 2)
				{
					coords += " " + data.ore.ToString("00") + ":" + data.minuti.ToString("00") + ":" + data.secondi.ToString("00") + " " +
									data.giorno.ToString("00") + "-" + data.mese.ToString("00") + "-20" + data.anno.ToString("00") +
									"  --  " + ardPosition.ToString("X8");
				}
				coords += "\r\n";
				txtW.Write(Encoding.ASCII.GetBytes(coords));
			}

		}
		protected void decodeEvent(ref string s, byte[] eventAr)
		{
			switch (eventAr[0])
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
					s = s + ((eventAr[1] * 256) + eventAr[2]).ToString() + " minutes.";
					break;
				case 4:
					s += "End of start delay. Start searching for satellites...";
					break;
				case 5:
					s += "Signal lost -> ACQ ON";
					s = s + "   GSV Sum: " + ((eventAr[1] * 256) + eventAr[2]).ToString();
					break;
				case 6:
					s += "Signal lost -> ACQ OFF";
					s = s + "   GSV Sum: " + ((eventAr[1] * 256) + eventAr[2]).ToString();
					break;
				case 7:
					s = s + "Schedule: " + eventAr[1].ToString() + " " + eventAr[2].ToString() + " " + eventAr[3].ToString();
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
						s += "Debug. Fase: " + eventAr[1].ToString();
						s += " OnOff: " + (eventAr[2] & 1).ToString();
						s += " InWater: " + ((eventAr[2] & 2) >> 1).ToString();
						s += " InAdc: " + ((eventAr[2] & 4) >> 2).ToString();
						s += " MinutiRtcc: " + eventAr[3].ToString();
						s += " SecRtcc: " + eventAr[7].ToString();
						s += " N_Ora: " + eventAr[9].ToString();
						s += " SecReg: " + eventAr[8].ToString();
						s += " ContoNoSignal: " + eventAr[10].ToString();
						s += " ContoFixAcq: " + (eventAr[11] * 256 + eventAr[12]).ToString();
						s += " ContoDop: " + eventAr[14].ToString();
						s += " Activity: " + eventAr[13].ToString();
					}
					else s = "";
					break;
			}
		}

		protected bool detectEof(ref MemoryStream ard)
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
