using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualBasic.FileIO;
using System.Windows;
using System.IO;
using System.Windows.Threading;
using System.Threading;
using System.Globalization;
#if X64
using FT_HANDLE = System.UInt64;
#else
using FT_HANDLE = System.UInt32;
#endif

namespace X_Manager.Units
{
	class AGM : Units.Unit
	{
		const double teslaCoeff = 0.1459536;
		ushort[] coeffs = new ushort[7];
		double[] convCoeffs = new double[7];
		new byte[] firmwareArray = new byte[3];
		struct timeStamp
		{
			public byte tsType, tsTypeExt1, tsTypeExt2;
			//public byte ore, secondi, minuti, anno, mese, giorno;
			public float batteryLevel;
			public double temp, press, pressOffset;
			public DateTime orario;
			public byte logEvent;
			public bool agmSyncEventFlag;
			public ushort agmSyncEventPosition;
			public double[] gyro;
			public double[] comp;
		}
		string dateFormatParameter;
		byte dateFormat;
		byte timeFormat;
		bool sameColumn = false;
		bool prefBattery = false;
		bool repeatEmptyValues = true;
		bool isDepth = true;
		bool inMeters = false;
		bool angloTime = false;
		ushort rate;
		ushort accRange;
		ushort gyroRange;
		byte gyroMode = 0;
		byte compassMode = 0;
		byte depthMode = 0;
		double gCoeff;
		double dpsCoeff;
		uint sogliaNeg;
		uint rendiNeg;
		bool bit16 = true;
		byte sampleLength;
		ushort addMilli;
		bool metadata;
		//bool overrideTime;
		CultureInfo dateCi;
		new byte[] lastGroup;
		byte cifreDec;
		string cifreDecString;
		string gyroSep;
		string compSep;

		public AGM(object p)
			: base(p)
		{
			base.positionCanSend = true;
			configurePositionButtonEnabled = false;
			modelCode = model_AGM1;
			modelName = "AGM-1";
		}

		public override void abortConf()
		{

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
			battery = Math.Round(battLevel, 2).ToString("0.00") + "V";
			return battery;
		}

		public override string askFirmware()
		{
			byte[] f = new byte[3];
			string firmware = "";

			ft.ReadExisting();
			ft.Write("TTTTTTTGGAF");
			ft.ReadTimeout = 400;
			int i = 0;
			try
			{
				for (i = 0; i < 3; i++) f[i] = (byte)ft.ReadByte();
			}
			catch
			{
				throw new Exception(unitNotReady);
			}

			firmwareArray = f;

			firmTotA = (uint)(f[0] * 1000000 + f[1] * 1000 + f[2]);

			firmware = f[0].ToString() + "." + f[1].ToString() + "." + f[2].ToString();

			return firmware;
		}

		public override uint[] askMaxMemory()
		{
			UInt32 m;
			ft.Write("TTTTTTTGGAm");
			try
			{
				m = (UInt32)ft.ReadByte(); m *= 256;
				m += (UInt32)ft.ReadByte(); m *= 256;
				m += (UInt32)ft.ReadByte(); m *= 256;
				m += (UInt32)ft.ReadByte();
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
				m = (UInt32)ft.ReadByte(); m *= 256;
				m += (UInt32)ft.ReadByte(); m *= 256;
				m += (UInt32)ft.ReadByte(); m *= 256;
				m += (UInt32)ft.ReadByte();
				mem_address = m;
			}
			catch
			{
				throw new Exception(unitNotReady);
			}

			mem_max_logical_address = 0;
			return new uint[] { mem_max_logical_address, mem_address };
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

		public override int askRealTime()
		{
			return 2;
		}

		public override void setName(string newName)
		{
			if (newName.Length < 10)
			{
				for (int i = newName.Length; i <= 9; i++) newName += " ";
			}
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

		public unsafe override void download(string fileName, uint fromMemory, uint toMemory, int baudrate)
		{
			convertStop = false;
			uint actMemory = fromMemory;
			FileMode fm = FileMode.Create;
			if (fromMemory != 0) fm = FileMode.Append;
			string fileNameMdp = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + ".mdp";
			var fo = new BinaryWriter(File.Open(fileNameMdp, fm));

			ft.Write("TTTTTTTTTTTTGGAe");
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

			byte b = (byte)(baudrate / 1000000);
			ft.Write(new byte[] { b }, 0, 1);
			ft.BaudRate = (uint)baudrate;

			Thread.Sleep(200);
			ft.Write("S");
			Thread.Sleep(1100);

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
			byte[] address = new byte[5];

			uint bytesToWrite = 0;
			int bytesReturned = 0;

			ft.ReadTimeout = 1000;

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
				ft.Write(outBuffer, bytesToWrite);
				bytesReturned = ft.Read(inBuffer, 0x1000);

				if (bytesReturned < 0)
				{
					outBuffer[0] = 88;
					ft.Write(outBuffer, 1);
					Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFailed()));
					fo.Write(inBuffer);
					fo.Close();
					return;
				}
				else if (bytesReturned < 0x1000)
				{
					firstLoop = true;
					continue;
				}

				actMemory += 0x1000;
				if ((actMemory % 0x20000) == 0)
				{
					if (dieCount == 2)
					{
						actMemory -= 0x1000;
						for (int i = 0; i < 2; i++)
						{
							address = BitConverter.GetBytes(actMemory);
							Array.Reverse(address);
							Array.Copy(address, 0, outBuffer, 1, 3);
							outBuffer[0] = 97;
							bytesToWrite = 4;
							ft.Write(outBuffer, bytesToWrite);
							ft.Read(inBuffer, 0x800);
							fo.Write(inBuffer, 0, 0x800);
							actMemory += 0x800;
						}
						firstLoop = true;
					}
					else
					{
						fo.Write(inBuffer, 0, 0x1000);
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


				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Value = actMemory));

				if (convertStop) actMemory = toMemory;
			}

			fo.Write(firmwareArray, 0, 3);
			fo.Write(new byte[] { model_AGM1, (byte)254 }, 0, 2);

			fo.Close();
			outBuffer[0] = 88;
			bytesToWrite = 1;
			ft.Write(outBuffer, bytesToWrite);
			ft.BaudRate = 115200;
			if (!convertStop) extractArds(fileNameMdp, fileName, true);
			else
			{
				if (Parent.getParameter("keepMdp").Equals("false"))
					fDel(fileNameMdp);
			}

			Thread.Sleep(300);
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFinished()));
		}

		public override void eraseMemory()
		{
			ft.Write("TTTTTTTTTGGAE");
			try
			{
				ft.ReadByte();
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
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
				if ((Parent.getParameter("keepMdp").Equals("false")) | (!connected)) fDel(fileNameMdp); //System.IO.File.Delete(fileNameMdp);
				else
				{
					if (!Path.GetExtension(fileNameMdp).Contains("Dump"))
					{
						string newFileNameMdp = Path.GetDirectoryName(fileNameMdp) + "\\" + Path.GetFileNameWithoutExtension(fileNameMdp) + ".memDump";
						if (System.IO.File.Exists(newFileNameMdp)) fDel(newFileNameMdp); //System.IO.File.Delete(newFileNameMdp);
																						 //string newFileNameMdp = Path.GetFileNameWithoutExtension(fileNameMdp) + ".memDump";
						System.IO.File.Move(fileNameMdp, newFileNameMdp);
					}
				}
			}
			catch { }
			if (!fromDownload) Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.nextFile()));
		}

		public override byte[] getConf()
		{
			byte[] conf = new byte[30];
			for (byte i = 0; i < 30; i++) conf[i] = 0xff;

			conf[22] = 0;
			conf[26] = modelCode;

			ft.ReadExisting();
			ft.Write("TTTTTTTTTTTTGGAC");
			try
			{
				for (int i = 0; i <= 25; i++) { conf[i] = (byte)ft.ReadByte(); }
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
			return conf;
		}

		public override void setConf(byte[] conf)
		{
			ft.ReadExisting();
			ft.Write("TTTTTTTTTTTTGGAc");
			try
			{
				ft.ReadByte();
				ft.Write(conf, 2, 3);
				ft.Write(conf, 15, 10);
				ft.Write(conf, 25, 1);
				ft.ReadByte();
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
		}

		public override void disconnect()
		{
			base.disconnect();
			ft.Write("TTTTTTTTTGGAO");
		}

		public override void convert(string fileName, string[] prefs)
		{

			timeStamp timeStampO = new timeStamp();
			string barStatus = "";

			BinaryReader ard = null;
			BinaryWriter csv = BinaryWriter.Null;

			string shortFileName;
			string addOn = "";
			string exten = Path.GetExtension(fileName);
			if (exten.Length > 4)
			{
				addOn = ("_S" + exten.Remove(0, 4));
			}
			string fileNameCsv = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + addOn + ".csv";

			try
			{
				ard = new System.IO.BinaryReader(System.IO.File.Open(fileName, FileMode.Open));
				csv = new System.IO.BinaryWriter(System.IO.File.OpenWrite(fileNameCsv));
			}
			catch (Exception ex)
			{
				try
				{
					ard.Close();
				}
				catch { }
				try
				{
					csv.Close();
				}
				catch { }
				var w = new Warning(ex.Message);
				w.ShowDialog();
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.nextFile()));
				return;
			}


			ard.BaseStream.Position = 1;
			uint fTotA = (uint)(ard.ReadByte() * 1000000 + ard.ReadByte() * 1000 + ard.ReadByte());
			while (ard.ReadByte() == 0) ;
			ard.BaseStream.Position -= 1;

			//Imposta le preferenze di conversione

			dateSeparator = csvSeparator;
			if (prefs[pref_sameColumn] == "True")
			{
				sameColumn = true;
				dateSeparator = " ";
			}

			if (Parent.getParameter("pressureRange") == "air")
			{
				isDepth = false;
			}

			if (prefs[pref_pressMetri] == "meters")
			{
				inMeters = true;
			}

			if (prefs[pref_fillEmpty] == "False")
			{
				repeatEmptyValues = false;
			}

			if (prefs[pref_battery] == "True")
			{
				prefBattery = true;
			}

			dateCi = new CultureInfo("it-IT");
			if (prefs[pref_timeFormat] == "2")
			{
				dateCi = new CultureInfo("en-US");
				angloTime = true;
			}

			timeStampO.pressOffset = float.Parse(prefs[pref_millibars]);
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

			cifreDec = 4;
			cifreDecString = "0.0000";
			if (!bit16)
			{
				cifreDec = 3;
				cifreDecString = "0.000";
			}


			//overrideTime = false;
			//if (prefs[pref_override_time] == "True") overrideTime = true;

			metadata = false;
			if (prefs[pref_metadata] == "True") metadata = true;

			//Legge i parametri di logging
			//ard.BaseStream.Position = 7;
			rate = findSamplingRate(ard.ReadByte());
			findAccRanges(ref ard);
			ard.BaseStream.Position -= 2;
			gyroMode = ard.ReadByte();
			gyroSep = "";
			if (gyroMode > 0)
			{
				gyroSep = csvSeparator;
			}
			ard.ReadByte();
			compassMode = ard.ReadByte();
			compSep = "";
			if (compassMode > 0)
			{
				compSep = csvSeparator;
			}
			depthMode = ard.ReadByte();

			sogliaNeg = 32767;
			rendiNeg = 65536;
			gCoeff = (accRange * (double)2) / 65536;
			dpsCoeff = (gyroRange * (double)2) / 65536;

			timeStampO.gyro = new double[] { 0, 0, 0 };
			timeStampO.comp = new double[] { 0, 0, 0 };
			timeStampO.agmSyncEventFlag = false;

			sampleLength = findBytesPerSample(ref ard);
			lastGroup = new byte[(sampleLength * rate)];

			//Legge i coefficienti sensore di profondità
			convCoeffs = new double[] { 0, 0, 0, 0, 0, 0 };
			for (int i = 0; i < 6; i++)
			{
				convCoeffs[i] = ard.ReadByte() * 256 + ard.ReadByte();
			}

			ard.ReadBytes(4); //Salta i coefficienti di calibrazione del magnetometro e l'header timestamp

			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => barStatus = (string)parent.statusLabel.Content));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.IsIndeterminate = false));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
				new Action(() => parent.statusLabel.Content = barStatus + " - Converting"));
			timeStampO.orario = findStartTime(fileName, ref prefs);

			shortFileName = Path.GetFileNameWithoutExtension(fileName);

			convertStop = false;

			//ard.BaseStream.Position = 0x1d;

			csvPlaceHeader(csvSeparator, ref csv);

			progMax = ard.BaseStream.Length - 1;
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
				new Action(() => parent.statusProgressBar.Maximum = ard.BaseStream.Length - 1));
			progressWorker.RunWorkerAsync();

			while (!convertStop)
			{
				while (Interlocked.Exchange(ref progLock, 2) > 0) { }

				progVal = ard.BaseStream.Position;
				Interlocked.Exchange(ref progLock, 0);

				//Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, 
				//	new Action(() => parent.statusProgressBar.Value = ard.BaseStream.Position));
				if (detectEof(ref ard)) break;

				if (decodeTimeStamp(ref ard, ref timeStampO, fTotA))
				{
					csv.Write(System.Text.Encoding.ASCII.GetBytes(groupConverter(ref timeStampO, lastGroup, shortFileName)));
					break;
				}

				csv.Write(System.Text.Encoding.ASCII.GetBytes(groupConverter(ref timeStampO, extractGroup(ref ard, ref timeStampO), shortFileName)));

				if (detectEof(ref ard)) break;

			}

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

		private byte[] extractGroup(ref BinaryReader ard, ref timeStamp tsc)
		{
			byte[] group = new byte[4000];
			bool badGroup = false;
			long position = 0;
			byte dummy, dummyExt;
			//bool mustExit = false;
			ushort badPosition = 3600;

			if (ard.BaseStream.Position == ard.BaseStream.Length) return lastGroup;

			tsc.agmSyncEventFlag = false;

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
					else if (dummyExt == 0x20)
					{
						try
						{
							ard.ReadBytes(2);
							dummyExt = ard.ReadByte();
						}
						catch
						{
							return lastGroup;
						}
						if (dummyExt == 4)
						{
							tsc.agmSyncEventFlag = true;
							tsc.agmSyncEventPosition = (ushort)position;
							dummy = 0;
						}
						else
						{
							ard.BaseStream.Position -= 4;
						}
					}
					else
					{
						ard.BaseStream.Position -= 1;
						if (badGroup)
						{
							System.IO.File.AppendAllText(((FileStream)ard.BaseStream).Name + "errorList.txt", "-> " + ard.BaseStream.Position.ToString("X8") + "\r\n");
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

			Array.Resize(ref group, (int)position);

			return group;
		}

		private string groupConverter(ref timeStamp tsLoc, byte[] group, string unitName)
		{
			double x;
			double y;
			double z;
			string gyrox = "";
			string gyroy = "";
			string gyroz = "";
			//string gyroSep = "";
			string compx = "";
			string compy = "";
			string compz = "";
			//string compSep = "";
			string act = "";
			string press = "";
			string pressSep = "";
			string temp = "";
			string volt = "";
			string battSep = "";
			string metadataSep = "";
			string metadataContent = "";

			string ampm = "";
			string textOut;
			string dateTimeS;
			string dateS = "";
			UInt16 milli;
			NumberFormatInfo nfi = (new CultureInfo("en-US", false).NumberFormat);

			dateS = tsLoc.orario.ToString(dateFormatParameter);
			dateTimeS = dateS + dateSeparator + tsLoc.orario.ToString("T", dateCi);

			if (angloTime)
			{
				ampm = dateTimeS.Split(' ')[dateTimeS.Split(' ').Length - 1];
				dateTimeS = dateTimeS.Remove(dateTimeS.Length - 3, 3);
			}
			milli = 0;

			var oldLength = group.Length / sampleLength;
			if (oldLength < rate)
			{
				byte[] oldGroup = group;
				int mezzo = (oldLength / 2) * sampleLength;
				Array.Resize(ref group, rate * sampleLength);
				Array.Clear(group, 0, group.Length);
				Array.Copy(oldGroup, 0, group, 0, mezzo);
				Array.Copy(oldGroup, mezzo, group, mezzo + sampleLength, (oldLength * sampleLength - mezzo));

				for (int ui = 0; ui < sampleLength; ui += 2)
				{
					int un = oldGroup[mezzo - sampleLength + ui] * 256 + oldGroup[mezzo - sampleLength + ui + 1];
					int up = oldGroup[mezzo + ui] * 256 + oldGroup[mezzo + ui + 1];
					if (un > 32767) un -= 65536;
					if (up > 32767) up -= 65536;
					int um = ((up - un) / 2) + un;
					if (um < 0) um += 65536;
					group[mezzo + ui] = (byte)(um >> 8);
					group[mezzo + ui + 1] = (byte)(um & 0xff);
				}
			}

			textOut = "";

			x = group[0] * 256 + group[1];
			y = group[2] * 256 + group[3];
			z = group[4] * 256 + group[5];

			if (x > sogliaNeg) x -= rendiNeg;
			x *= gCoeff;
			x = Math.Round(x, cifreDec);

			if (y > sogliaNeg) y -= rendiNeg;
			y *= gCoeff;
			y = Math.Round(y, cifreDec);

			if (z > sogliaNeg) z -= rendiNeg;
			z *= gCoeff;
			z = Math.Round(z, cifreDec);

			textOut += unitName + csvSeparator + dateTimeS + ".000";

			if (angloTime) textOut += " " + ampm;

			textOut += csvSeparator + x.ToString(cifreDecString, nfi) + csvSeparator + y.ToString(cifreDecString, nfi) + csvSeparator + z.ToString(cifreDecString, nfi);

			byte more = 0;
			if (gyroMode == 2)
			{
				x = group[6] * 256 + group[7];
				y = group[8] * 256 + group[9];
				z = group[10] * 256 + group[11];

				if (x > sogliaNeg) x -= rendiNeg;
				x *= dpsCoeff;
				x = Math.Round(x, cifreDec);

				if (y > sogliaNeg) y -= rendiNeg;
				y *= dpsCoeff;
				y = Math.Round(y, cifreDec);

				if (z > sogliaNeg) z -= rendiNeg;
				z *= dpsCoeff;
				z = Math.Round(z, cifreDec);

				gyrox = x.ToString(cifreDecString, nfi);
				gyroy = y.ToString(cifreDecString, nfi);
				gyroz = z.ToString(cifreDecString, nfi);
				// gyroSep = csvSeparator;
				more += 6;
			}
			else if (gyroMode == 1)
			{
				//Inserisce il giroscopio a 1 Hz
				if (((tsLoc.tsType & 128) == 128) | repeatEmptyValues)
				{
					gyrox = (Math.Round(tsLoc.gyro[0], cifreDec)).ToString(cifreDecString, nfi);
					gyroy = (Math.Round(tsLoc.gyro[1], cifreDec)).ToString(cifreDecString, nfi);
					gyroz = (Math.Round(tsLoc.gyro[2], cifreDec)).ToString(cifreDecString, nfi);
					// gyroSep = csvSeparator;
				}
			}

			if (compassMode == 2)
			{
				x = group[7 + more] * 256 + group[6 + more];
				y = group[9 + more] * 256 + group[8 + more];
				z = group[11 + more] * 256 + group[10 + more];

				if (x > sogliaNeg) x -= rendiNeg;
				x *= teslaCoeff;
				x = Math.Round(x, cifreDec);

				if (y > sogliaNeg) y -= rendiNeg;
				y *= teslaCoeff;
				y = Math.Round(y, cifreDec);

				if (z > sogliaNeg) z -= rendiNeg;
				z *= teslaCoeff;
				z = Math.Round(z, cifreDec);

				compx = x.ToString(cifreDecString, nfi);
				compy = y.ToString(cifreDecString, nfi);
				compz = z.ToString(cifreDecString, nfi);
				//compSep = csvSeparator;
			}
			else if (compassMode == 1)
			{
				//Inserisce il magnetometro a 1 Hz
				if (((tsLoc.tsType & 2) == 2) | repeatEmptyValues)
				{
					compx = (Math.Round(tsLoc.comp[0], cifreDec)).ToString(cifreDecString, nfi);
					compy = (Math.Round(tsLoc.comp[1], cifreDec)).ToString(cifreDecString, nfi);
					compz = (Math.Round(tsLoc.comp[2], cifreDec)).ToString(cifreDecString, nfi);
					//compSep = csvSeparator;
				}
			}


			//Inserisce l'attività
			if ((tsLoc.tsType & 0x40) == 0x40)
			{
				act = "Active";
			}
			else
			{
				act = "Inactive";
			}

			//Presisone
			if (depthMode > 0) pressSep = csvSeparator;
			if ((tsLoc.tsType & 4) == 4)
			{
				press = tsLoc.press.ToString(nfi);
			}

			//Temperatura
			temp = Math.Round(tsLoc.temp, 2).ToString(nfi);

			//Inserisce la batteria
			if (prefBattery)
			{
				battSep = csvSeparator;
				if (((tsLoc.tsType & 8) == 8) | repeatEmptyValues)
				{
					volt = tsLoc.batteryLevel.ToString(nfi);
				}
			}

			//Inserisce i metadati
			if (metadata)
			{
				metadataSep = csvSeparator;
				if (tsLoc.logEvent > 0)
				{
					switch (tsLoc.logEvent)
					{
						case 1:
							metadataContent = "Low battery.";
							break;
						case 2:
							metadataContent = "Power off command received.";
							break;
						case 3:
							metadataContent = "Memory full.";
							break;
					}

					if (gyroMode == 1) gyroSep = csvSeparator;
					if (compassMode == 1) compSep = csvSeparator;

					textOut += gyroSep + gyrox + gyroSep + gyroy + gyroSep + gyroz;
					textOut += compSep + compx + compSep + compy + compSep + compz;
					textOut += csvSeparator + act;
					textOut += pressSep + press;
					textOut += csvSeparator + temp;
					textOut += battSep + volt;
					textOut += metadataSep + metadataContent + "\r\n";
					return textOut;
				}
				if ((tsLoc.agmSyncEventFlag) && (tsLoc.agmSyncEventPosition == 0))
				{
					tsLoc.agmSyncEventFlag = false;
					textOut += "TIMEMARK";
				}
			}

			textOut += gyroSep + gyrox + gyroSep + gyroy + gyroSep + gyroz;
			textOut += compSep + compx + compSep + compy + compSep + compz;
			textOut += csvSeparator + act;
			textOut += pressSep + press;
			textOut += csvSeparator + temp;
			textOut += battSep + volt;
			textOut += metadataSep + metadataContent + "\r\n";

			if (tsLoc.logEvent > 0) return textOut;

			metadataContent = "";

			if (!repeatEmptyValues)
			{
				gyrox = "";
				gyroy = "";
				gyroz = "";
				compx = "";
				compy = "";
				compz = "";
				act = "";
				press = "";
				temp = "";
				volt = "";
			}

			milli += addMilli;
			dateTimeS += ".";

			int iend = rate * sampleLength;
			for (int i = sampleLength; i < iend; i += sampleLength)
			{
				x = group[i] * 256 + group[i + 1];

				y = group[i + 2] * 256 + group[i + 3];

				z = group[i + 4] * 256 + group[i + 5];

				if (x > sogliaNeg) x -= rendiNeg;
				x *= gCoeff;
				x = Math.Round(x, cifreDec);

				if (y > sogliaNeg) y -= rendiNeg;
				y *= gCoeff;
				y = Math.Round(y, cifreDec);

				if (z > sogliaNeg) z -= rendiNeg;
				z *= gCoeff;
				z = Math.Round(z, cifreDec);

				if (rate == 1)
				{
					tsLoc.orario = tsLoc.orario.AddSeconds(1);
					dateTimeS = dateS + dateSeparator + tsLoc.orario.ToString("T", dateCi) + ".";
				}
				textOut += unitName + csvSeparator + dateTimeS + milli.ToString("D3");

				if (angloTime) textOut += " " + ampm;

				textOut += csvSeparator + x.ToString(cifreDecString, nfi) + csvSeparator + y.ToString(cifreDecString, nfi) + csvSeparator + z.ToString(cifreDecString, nfi);

				more = 0;
				if (gyroMode == 2)
				{
					x = group[i + 6] * 256 + group[i + 7];
					y = group[i + 8] * 256 + group[i + 9];
					z = group[i + 10] * 256 + group[i + 11];

					if (x > sogliaNeg) x -= rendiNeg;
					x *= dpsCoeff;
					x = Math.Round(x, cifreDec);

					if (y > sogliaNeg) y -= rendiNeg;
					y *= dpsCoeff;
					y = Math.Round(y, cifreDec);

					if (z > sogliaNeg) z -= rendiNeg;
					z *= dpsCoeff;
					z = Math.Round(z, cifreDec);

					gyrox = x.ToString(cifreDecString, nfi);
					gyroy = y.ToString(cifreDecString, nfi);
					gyroz = z.ToString(cifreDecString, nfi);

					more += 6;
				}

				if (compassMode == 2)
				{
					x = group[i + 7 + more] * 256 + group[i + 6 + more];
					y = group[i + 9 + more] * 256 + group[i + 8 + more];
					z = group[i + 11 + more] * 256 + group[i + 10 + more];

					if (x > sogliaNeg) x -= rendiNeg;
					x *= teslaCoeff;
					x = Math.Round(x, cifreDec);

					if (y > sogliaNeg) y -= rendiNeg;
					y *= teslaCoeff;
					y = Math.Round(y, cifreDec);

					if (z > sogliaNeg) z -= rendiNeg;
					z *= teslaCoeff;
					z = Math.Round(z, cifreDec);

					compx = x.ToString(cifreDecString, nfi);
					compy = y.ToString(cifreDecString, nfi);
					compz = z.ToString(cifreDecString, nfi);
				}

				textOut += gyroSep + gyrox + gyroSep + gyroy + gyroSep + gyroz;
				textOut += compSep + compx + compSep + compy + compSep + compz;
				textOut += csvSeparator + act;
				textOut += pressSep + press;
				textOut += csvSeparator + temp;
				textOut += battSep + volt;
				textOut += metadataSep;

				if (tsLoc.agmSyncEventFlag)
				{
					if (i == tsLoc.agmSyncEventPosition)
					{
						if (metadata)
						{
							tsLoc.agmSyncEventFlag = false;
							textOut += "TIMEMARK!";
						}
					}
				}
				textOut += "\r\n";

				milli += addMilli;
			}

			return textOut;
		}

		private bool decodeTimeStamp(ref BinaryReader ard, ref timeStamp tsc, uint fTotA)
		{
			tsc.tsType = ard.ReadByte();
			tsc.logEvent = 0;

			//tsmask.0  Esteso
			if ((tsc.tsType & 1) == 1)
			{
				tsc.tsTypeExt1 = ard.ReadByte();
				if ((tsc.tsTypeExt1 & 1) == 1)
				{
					tsc.tsTypeExt2 = ard.ReadByte();
				}
			}

			//tsmask.1   Magnetometro 1Hz
			if ((tsc.tsType & 2) == 2)
			{
				tsc.comp[0] = ard.ReadByte() + ard.ReadByte() * 256;
				if (tsc.comp[0] > sogliaNeg) tsc.comp[0] -= rendiNeg;
				tsc.comp[0] *= teslaCoeff;

				tsc.comp[1] = ard.ReadByte() + ard.ReadByte() * 256;
				if (tsc.comp[1] > sogliaNeg) tsc.comp[1] -= rendiNeg;
				tsc.comp[1] *= teslaCoeff;

				tsc.comp[2] = ard.ReadByte() + ard.ReadByte() * 256;
				if (tsc.comp[2] > sogliaNeg) tsc.comp[2] -= rendiNeg;
				tsc.comp[2] *= teslaCoeff;
			}

			//tsmask.2   Temperatura da MS5837
			if ((tsc.tsType & 4) == 4)
			{
				if (isDepth)
				{
					pressureDepth5837(ref ard, ref tsc);
				}
				else
				{
					pressureAir(ref ard, ref tsc);
				}

			}
			else
			{
				tsc.temp = ard.ReadByte() * 256 + ard.ReadByte();
				if (tsc.temp > 32767) tsc.temp -= 65536;
				tsc.temp = (float)((tsc.temp / 333.87) + 21);
			}

			//tsmask.3   Batteria
			if ((tsc.tsType & 8) == 8)
			{
				tsc.batteryLevel = (float)(Math.Round((((ard.ReadByte() * 256 + ard.ReadByte()) * 6.0) / 4095), 2));
			}

			//tsmask.4   Vuoto

			//tsmask.5   Evento
			if ((tsc.tsType & 0x20) == 0x20)
			{
				tsc.logEvent = ard.ReadByte();
				if (tsc.logEvent < 4)
				{
					tsc.orario = tsc.orario.AddSeconds(1);
					return true;
				}
			}

			//tsmask.6   Attività

			//tsmask.7   Giroscopio 1Hz
			if ((tsc.tsType & 0x80) == 0x80)
			{
				tsc.gyro[0] = ard.ReadByte() * 256 + ard.ReadByte();
				if (tsc.gyro[0] > sogliaNeg) tsc.gyro[0] -= rendiNeg;
				tsc.gyro[0] *= dpsCoeff;

				tsc.gyro[1] = ard.ReadByte() * 256 + ard.ReadByte();
				if (tsc.gyro[1] > sogliaNeg) tsc.gyro[1] -= rendiNeg;
				tsc.gyro[1] *= dpsCoeff;

				tsc.gyro[2] = ard.ReadByte() * 256 + ard.ReadByte();
				if (tsc.gyro[2] > sogliaNeg) tsc.gyro[2] -= rendiNeg;
				tsc.gyro[2] *= dpsCoeff;
			}

			tsc.orario = tsc.orario.AddSeconds(1);

			return false;

		}

		private DateTime findStartTime(string fileName, ref string[] prefs)
		{
			const int pref_h = 9;
			const int pref_m = 10;
			const int pref_s = 11;
			const int pref_date_year = 12;
			const int pref_date_month = 13;
			const int pref_date_day = 14;

			DateTime dt = new DateTime(int.Parse(prefs[pref_date_year]), int.Parse(prefs[pref_date_month]), int.Parse(prefs[pref_date_day]),
			   int.Parse(prefs[pref_h]), int.Parse(prefs[pref_m]), int.Parse(prefs[pref_s]), 0, DateTimeKind.Utc);

			dt.AddSeconds(19);

			return dt;
		}

		private ushort findSamplingRate(byte rateIn)
		{
			byte rateOut;

			switch (rateIn)
			{
				case 1:
					rateOut = 1;
					break;
				case 2:
					rateOut = 10;
					break;
				case 3:
					rateOut = 25;
					break;
				case 4:
					rateOut = 50;
					break;
				case 5:
					rateOut = 100;
					break;
				default:
					rateOut = 50;
					break;
			}

			if (rateOut == 1) addMilli = 0;

			else addMilli = (ushort)((1 / (double)rateOut) * 1000);

			return rateOut;
		}

		private byte findBytesPerSample(ref BinaryReader fileIn)
		{
			byte bytes = 6;
			fileIn.BaseStream.Position -= 4;

			if (fileIn.ReadByte() == 2) bytes += 6;
			fileIn.ReadByte();

			if (fileIn.ReadByte() == 2) bytes += 6;

			if (fileIn.ReadByte() == 2) bytes += 6;

			return bytes;
		}

		private void findAccRanges(ref BinaryReader fileIn)
		{
			//fileIn.BaseStream.Position = 8;

			byte rangeA = fileIn.ReadByte();

			rangeA &= 15;
			if (rangeA > 7) rangeA -= 8;

			accRange = (ushort)(Math.Pow(2, (rangeA + 1)));

			gyroRange = 0;
			fileIn.ReadByte();
			byte rangeG = fileIn.ReadByte();

			gyroRange = (ushort)(250 * Math.Pow(2, rangeG));
		}

		private void csvPlaceHeader(string csvSeparator, ref BinaryWriter csv)
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

			csvHeader += csvSeparator + "accX" + csvSeparator + "accY" + csvSeparator + "accZ";
			if (gyroMode > 0)
			{
				csvHeader += csvSeparator + "gyroX" + csvSeparator + "gyroY" + csvSeparator + "gyroZ";
			}
			if (compassMode > 0)
			{
				csvHeader += csvSeparator + "compX" + csvSeparator + "compY" + csvSeparator + "compZ";
			}

			csvHeader += csvSeparator + "Activity";

			if (depthMode > 0)
			{
				if (inMeters)
				{
					csvHeader += csvSeparator + "Depth";
				}
				else
				{
					csvHeader += csvSeparator + "Pressure";
				}
			}

			csvHeader += csvSeparator + "Temp. (°C)";

			if (prefBattery)
			{
				csvHeader += csvSeparator + "Battery Voltage (V)";
			}

			if (metadata)
			{
				csvHeader += csvSeparator + "Metadata";
			}

			csvHeader += "\r\n";
			csv.Write(System.Text.Encoding.ASCII.GetBytes(csvHeader));
		}

		private bool pressureAir(ref BinaryReader ard, ref timeStamp tsc)
		{
			double dT, off, sens, t2, off2, sens2;
			double d1, d2;

			try
			{
				d2 = (uint)(ard.ReadByte() * 65536 + ard.ReadByte() * 256 + ard.ReadByte());
			}
			catch
			{
				return true;
			}

			dT = d2 - convCoeffs[4] * 256;
			tsc.temp = (float)(2000 + (dT * convCoeffs[5]) / 8388608);
			off = convCoeffs[1] * 131072 + (convCoeffs[3] * dT) / 64;
			sens = convCoeffs[0] * 65536 + (convCoeffs[2] * dT) / 128;
			if (tsc.temp > 2000)
			{
				t2 = 0;
				off2 = 0;
				sens2 = 0;
			}
			else
			{
				t2 = Math.Pow(dT, 2) / 2147483648;
				off2 = 61 * Math.Pow((tsc.temp - 2000), 2) / 16;
				sens2 = 2 * Math.Pow((tsc.temp - 2000), 2);
				if (tsc.temp < -1500)
				{
					off2 += 20 * Math.Pow((tsc.temp + 1500), 2);
					sens2 += 12 * Math.Pow((tsc.temp + 1500), 2);
				}
			}
			tsc.temp -= (float)t2;
			off -= off2;
			sens -= sens2;
			tsc.temp /= 100;
			tsc.temp = (float)Math.Round(tsc.temp, 1);

			if ((tsc.tsType & 4) == 4)
			{
				try
				{
					d1 = (uint)(ard.ReadByte() * 65536 + ard.ReadByte() * 256 + ard.ReadByte());
				}
				catch
				{
					return true;
				}
				tsc.press = (float)(((d1 * sens / 2097152) - off) / 32768);
				tsc.press /= 100;
				tsc.press = (float)Math.Round(tsc.press, 2);
			}
			return false;
		}

		//private bool pressureDepth5837(ref BinaryReader ard, ref timeStamp tsc)
		//{
		//	double dT;
		//	double off;
		//	double sens;
		//	double d1, d2;

		//	try
		//	{
		//		d2 = (ard.ReadByte() * 65536 + ard.ReadByte() * 256 + ard.ReadByte());
		//	}
		//	catch
		//	{
		//		return true;
		//	}

		//	dT = d2 - convCoeffs[4] * 256;
		//	tsc.temp = (float)(2000 + (dT * convCoeffs[5]) / 8388608);
		//	off = convCoeffs[1] * 65536 + (convCoeffs[3] * dT) / 128;
		//	sens = convCoeffs[0] * 32768 + (convCoeffs[2] * dT) / 256;
		//	if (tsc.temp > 2000)
		//	{
		//		tsc.temp -= (float)((2 * Math.Pow(dT, 2)) / 137438953472);
		//		off -= ((Math.Pow((tsc.temp - 2000), 2)) / 16);
		//	}
		//	else
		//	{
		//		off -= 3 * ((Math.Pow((tsc.temp - 2000), 2)) / 2);
		//		sens -= 5 * ((Math.Pow((tsc.temp - 2000), 2)) / 8);
		//		if (tsc.temp < -1500)
		//		{
		//			off -= 7 * Math.Pow((tsc.temp + 1500), 2);
		//			sens -= 4 * Math.Pow((tsc.temp + 1500), 2);
		//		}
		//		tsc.temp -= (float)(3 * (Math.Pow(dT, 2))) / 8589934592;
		//	}
		//	tsc.temp = (float)Math.Round((tsc.temp / 100), 1);
		//	if ((tsc.tsType & 4) == 4)
		//	{
		//		try
		//		{
		//			d1 = (uint)(ard.ReadByte() * 65536 + ard.ReadByte() * 256 + ard.ReadByte());
		//		}
		//		catch
		//		{
		//			return true;
		//		}
		//		tsc.press = (float)Math.Round((((d1 * sens / 2097152) - off) / 81920), 1);
		//		if (inMeters)
		//		{
		//			tsc.press -= tsc.pressOffset;
		//			if (tsc.press <= 0) tsc.press = 0;
		//			else
		//			{
		//				tsc.press = (float)(tsc.press / 98.1);
		//				tsc.press = (float)Math.Round(tsc.press, 2);
		//			}
		//		}
		//	}
		//	return false;
		//}

		private bool pressureDepth5837(ref BinaryReader ard, ref timeStamp tsc)
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
			tsc.temp = 2000 + (dT * convCoeffs[5]) / 8_388_608;
			off = convCoeffs[1] * 65_536 + (convCoeffs[3] * dT) / 128;
			sens = convCoeffs[0] * 32_768 + (convCoeffs[2] * dT) / 256;
			if (tsc.temp > 2000)
			{
				tsc.temp -= (2 * Math.Pow(dT, 2)) / 137_438_953_472;
				off -= ((Math.Pow((tsc.temp - 2000), 2)) / 16);
			}
			else
			{
				tsc.temp -= 3 * (Math.Pow(dT, 2)) / 8_589_934_592;
				off -= 3 * ((Math.Pow((tsc.temp - 2000), 2)) / 2);
				sens -= 5 * ((Math.Pow((tsc.temp - 2000), 2)) / 8);
				if (tsc.temp < -1500)
				{
					off -= 7 * Math.Pow((tsc.temp + 1500), 2);
					sens -= 4 * Math.Pow((tsc.temp + 1500), 2);
				}
			}
			tsc.temp /= 100;
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




		public override void downloadRemote(string fileName, uint fromMemory, uint toMemory, int baudrate)
		{
			throw new NotImplementedException();
		}















		public override void getCoeffs()
		{
			base.getCoeffs();
		}

		public override void setPcTime()
		{
			base.setPcTime();
		}

		public override bool getRemote()
		{
			return base.getRemote();
		}

		public override byte[] getAccSchedule()
		{
			return base.getAccSchedule();
		}



		public override void setAccSchedule(byte[] schedule)
		{
			base.setAccSchedule(schedule);
		}

		private bool detectEof(ref BinaryReader ard)
		{
			if (ard.BaseStream.Position >= ard.BaseStream.Length)
			{
				return true;
			}
			return false;
		}
	}
}
