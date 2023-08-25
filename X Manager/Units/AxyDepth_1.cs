using System;
using System.Text;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using System.IO;
#if X64
using FT_HANDLE = System.UInt64;
#else
    using FT_HANDLE = System.UInt32;
#endif

namespace X_Manager.Units
{
	class AxyDepth_1 : AxyDepth
	{
		ushort[] coeffs = new ushort[7];
		double[] convCoeffs = new double[7];
		new byte[] firmwareArray = new byte[6];
		struct timeStamp
		{
			public byte tsType;
			public float batteryLevel;
			public double temp;
			public double press;
			public float pressOffset;
			public DateTime orario;
			public byte stopEvent;
			public byte timeStampLength;
		}

		bool bits;
		byte bitsDiv;
		ushort rate;
		ushort rateComp;
		byte range;
		double gCoeff;
		ushort addMilli;
		byte cifreDec;
		string cifreDecString;
		CultureInfo dateCi;

		public AxyDepth_1(object p)
			: base(p)
		{
			base.positionCanSend = false;
			configurePositionButtonEnabled = false;
			modelCode = model_axyDepth;
			//modelName = "Axy-Depth";
			defaultArdExtension = "ard1";
		}

		public override string askFirmware()
		{
			byte[] f = new byte[2];
			string firmware;
			try
			{
				firmware = ft.ReadLine();
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
			f[0] = byte.Parse(firmware.Split('.')[0]);
			f[1] = byte.Parse(firmware.Split('.')[1]);

			firmTotA = 0;
			for (int i = 0; i < 3; i++)
			{
				firmTotA *= 1000;
				firmTotA += f[i];
			}

			firmwareArray = f;

			return firmware;
		}

		public override string askBattery()
		{
			string battery = "";
			double battLevel;
			ft.Write("b");
			try
			{
				battLevel = ft.ReadByte(); battLevel *= 256;
				battLevel += ft.ReadByte();
				battLevel *= 6;
				battLevel /= 1024;
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
			string unitNameBack = ft.ReadExistingOut();

			name = formatName(unitNameBack); ;
			return name;
		}

		public override void setName(string newName)
		{
			if (newName.Length < 10)
			{
				for (int i = newName.Length; i <= 9; i++) newName += " ";
			}
			ft.Write("N");
			try
			{
				ft.ReadByte();
				ft.Write(newName);
				ft.ReadByte();
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
		}

		public override void getCoeffs()
		{
			coeffs[0] = 0;
			ft.Write("A");
			try
			{
				for (int i = 1; i <= 6; i++)
				{
					coeffs[i] = (ushort)(ft.ReadByte() * 256 + ft.ReadByte());
				}
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
			uint sommaSoglie = 0;
			try
			{
				sommaSoglie = (uint)(coeffs[1] + coeffs[2] + coeffs[3]);
			}
			catch
			{
				sommaSoglie = 1;
			}
			if ((sommaSoglie == 0) | (sommaSoglie == 0x2fd))
			{
				//evitaSoglie = true;
			}
		}

		public override uint[] askMaxMemory()
		{
			uint m;
			ft.Write("m");
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
			uint m;
			ft.Write("M");
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
			mem_address = m;
			mem_max_logical_address = 0;
			return new uint[] { mem_max_logical_address, mem_address };
		}

		public override byte[] getConf()
		{
			byte[] conf = new byte[30];
			for (byte i = 0; i < 30; i++) conf[i] = 0xff;

			conf[22] = 0;
			conf[25] = modelCode;

			ft.ReadExisting();
			ft.Write("C");
			try
			{
				ft.ReadByte();
				for (int i = 15; i <= 19; i++) { conf[i] = (byte)ft.ReadByte(); }
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
			return conf;
		}

		public override void setConf(byte[] conf)
		{
			if (conf[15] > 7) conf[15] -= 8;
			ft.Write(conf, 15, 5);
			try
			{
				ft.ReadLine();
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
		}

		public override void abortConf()
		{
			ft.Write(new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff }, 0, 5);
			try
			{
				ft.ReadLine();
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
		}

		public override void eraseMemory()
		{
			ft.Write("E");
			try
			{
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
			ft.Write("B");
		}

		public unsafe override void download(string fileName, uint fromMemory, uint toMemory, int baudrate)
		{
			convertStop = false;
			uint actMemory = fromMemory;
			System.IO.FileMode fm = System.IO.FileMode.Create;
			if (fromMemory != 0) fm = System.IO.FileMode.Append;

			string fileNameMdp = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + ".mdp";
			var fo = new System.IO.BinaryWriter(File.Open(fileNameMdp, fm));

			ft.Write("d");
			try
			{
				ft.ReadByte();
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

			ft.BaudRate = 3000000;

			Thread.Sleep(200);

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
					bytesToWrite = 2;
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
				fo.Write(inBuffer, 0, 0x1000);

				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Value = actMemory));

				if (convertStop) actMemory = toMemory;
			}

			fo.Write(firmwareArray, 0, firmwareArray.Length);
			for (int i = firmwareArray.Length; i <= 2; i++)
			{
				fo.Write(new byte[] { 0xff }, 0, 1);
			}
			fo.Write(new byte[] { model_axy4, (byte)254 }, 0, 2);

			fo.Close();
			outBuffer[0] = 88;
			bytesToWrite = 1;
			ft.Write(outBuffer, 1);
			ft.BaudRate = 115200;
			if (!convertStop) extractArds(fileNameMdp, fileName, true);
			else
			{
				if (Parent.getParameter("keepMdp").Equals("false"))
				{
					try
					{
						//System.IO.File.Delete(fileNameMdp);
						fDel(fileNameMdp);
					}
					catch { }
				}
			}

			Thread.Sleep(300);
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
				if ((Parent.getParameter("keepMdp").Equals("false")) | (!connected)) fDel(fileNameMdp);
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

		public override void convert(string fileName, string[] prefs)
		{
			timeStamp timeStampO = new timeStamp();
			string barStatus = "";

			string shortFileName;
			string addOn = "";
			string exten = Path.GetExtension(fileName);
			if ((exten.Length > 4)) addOn = ("_S" + exten.Remove(0, 4));
			string fileNameCsv = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + addOn + ".csv";
			BinaryReader ard = new BinaryReader(File.Open(fileName, FileMode.Open));
			BinaryWriter csv = new BinaryWriter(File.OpenWrite(fileNameCsv));

			ard.BaseStream.Position = 1;
			firmTotA = (uint)(ard.ReadByte() * 1000 + ard.ReadByte());
			if (firmTotA > 2004)
			{
				firmTotA *= 1000; firmTotA += ard.ReadByte();
			}

			//Legge i coefficienti sensore di profondità
			convCoeffs = new double[] { 0, 0, 0, 0, 0, 0 };
			for (int i = 0; i < 6; i++)
			{
				convCoeffs[i] = ard.ReadByte() * 256 + ard.ReadByte();
			}

			//Imposta le preferenze di conversione

			if (prefs[p_filePrefs_fillEmpty] == "False") pref_repeatEmptyValues = false;

			dateSeparator = csvSeparator;
			if (prefs[p_filePrefs_sameColumn] == "True")
			{
				pref_sameColumn = true;
				dateSeparator = " ";
			}

			if (prefs[p_filePrefs_battery] == "True") pref_battery = true;

			dateCi = new CultureInfo("it-IT");
			if (prefs[p_filePrefs_timeFormat] == "2")
			{
				pref_angloTime = true;
				dateCi = new CultureInfo("en-US");
			}

			if (Parent.getParameter("pressureRange") == "air")
			{
				pref_isDepth = false;
			}

			if (prefs[p_filePrefs_pressMetri] == "meters")
			{
				pref_inMeters = true;
			}

			timeStampO.pressOffset = float.Parse(prefs[p_filePrefs_millibars]);
			pref_dateFormat = byte.Parse(prefs[p_filePrefs_dateFormat]);
			pref_timeFormat = byte.Parse(prefs[p_filePrefs_timeFormat]);
			switch (pref_dateFormat)
			{
				case 1:
					pref_dateFormatParameter = "dd/MM/yyyy";
					break;
				case 2:
					pref_dateFormatParameter = "MM/dd/yyyy";
					break;
				case 3:
					pref_dateFormatParameter = "yyyy/MM/dd";
					break;
				case 4:
					pref_dateFormatParameter = "yyyy/dd/MM";
					break;
			}

			if (prefs[p_filePrefs_metadata] == "True") pref_metadata = true;

			//Legge i parametri di logging
			ard.ReadByte();
			byte rrb = ard.ReadByte();
			rate = findSamplingRate(rrb);
			rateComp = rate;
			if (rate == 1) rateComp = 5;
			range = findRange(rrb);
			bits = findBits(rrb);
			bitsDiv = findBytesPerSample();

			//Array.Resize(ref lastGroup, ((rate * 3)));
			lastGroup = new double[(rateComp * 3)];

			nOutputs = rateComp;

			cifreDec = 3; cifreDecString = "0.000";
			if (bits)
			{
				cifreDec = 4; cifreDecString = "0.0000";
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
			//    new Action(() => parent.statusProgressBar.Maximum = ard.BaseStream.Length - 1));
			progMax = ard.BaseStream.Length - 1;
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
				new Action(() => parent.statusProgressBar.Maximum = ard.BaseStream.Length - 1));
			progressWorker.RunWorkerAsync();

			while (!convertStop)
			{
				//Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
				//            new Action(() => parent.statusProgressBar.Value = ard.BaseStream.Position));
				while (Interlocked.Exchange(ref progLock, 2) > 0) { }

				progVal = ard.BaseStream.Position;
				Interlocked.Exchange(ref progLock, 0);

				if (detectEof(ref ard)) break;

				try
				{
					decodeTimeStamp(ref ard, ref timeStampO);
				}
				catch
				{
					csv.Write(Encoding.ASCII.GetBytes(groupConverter(ref timeStampO, lastGroup, shortFileName) + "\r\n"));
					break;
				}


				if (timeStampO.stopEvent > 0)
				{
					csv.Write(Encoding.ASCII.GetBytes(groupConverter(ref timeStampO, lastGroup, shortFileName) + "\r\n"));
					break;
				}

				try
				{
					csv.Write(Encoding.ASCII.GetBytes(
						groupConverter(ref timeStampO, extractGroup(ref ard, ref timeStampO), shortFileName)));
				}
				catch
				{
					csv.Write("\r\nERROR\r\n");
					break;
				}

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
						File.AppendAllText(((FileStream)ard.BaseStream).Name + "errorList.txt", "-> " + ard.BaseStream.Position.ToString("X8") + "\r\n");
					}
				}
			} while ((dummy != 0xab) && (ard.BaseStream.Position < ard.BaseStream.Length));

			//Array.Resize(ref group, (int)position);

			tsc.timeStampLength = (byte)(position / bitsDiv);
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
			double x, y, z;
			string ampm = "";
			string textOut, dateTimeS, additionalInfo;
			string dateS = "";
			ushort milli;
			NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;
			ushort contoTab = 0;

			dateS = tsLoc.orario.ToString(pref_dateFormatParameter);

			dateTimeS = dateS + dateSeparator + tsLoc.orario.ToString("T", dateCi);
			if (pref_angloTime)
			{
				ampm = dateTimeS.Split(' ')[dateTimeS.Split(' ').Length - 1];
				dateTimeS = dateTimeS.Remove(dateTimeS.Length - 3, 3);
			}

			milli = 0;

			textOut = "";

			x = group[0]; y = group[1]; z = group[2];

			x *= gCoeff; x = Math.Round(x, cifreDec);
			y *= gCoeff; y = Math.Round(y, cifreDec);
			z *= gCoeff; z = Math.Round(z, cifreDec);

			textOut = unitName + csvSeparator + dateTimeS + ".000";
			if (pref_angloTime) textOut += " " + ampm;
			textOut += csvSeparator + x.ToString(cifreDecString, nfi) + csvSeparator + y.ToString(cifreDecString, nfi) + csvSeparator + z.ToString(cifreDecString, nfi);

			additionalInfo = "";

			contoTab += 2;
			additionalInfo += csvSeparator;
			if (pref_repeatEmptyValues)
			{
				tsLoc.tsType = 13;
			}
			byte rem = (byte)(tsLoc.tsType % 10);
			switch (rem)
			{
				case 3:
					additionalInfo += tsLoc.press.ToString(nfi) + csvSeparator;
					additionalInfo += tsLoc.temp.ToString(nfi);
					break;
				case 5:
					additionalInfo += csvSeparator + tsLoc.temp.ToString(nfi);
					break;
				case 6:
					additionalInfo += tsLoc.press.ToString(nfi) + csvSeparator;
					break;
			}

			//Inserisce la batteria
			if (pref_battery)
			{
				contoTab += 1;
				additionalInfo += csvSeparator;
				if (tsLoc.tsType > 9)
				{
					additionalInfo += tsLoc.batteryLevel.ToString(nfi);
				}
			}

			//Inserisce i metadati
			if (pref_metadata)
			{
				contoTab += 1;
				additionalInfo += csvSeparator;
				if (tsLoc.stopEvent == 4)
				{
					additionalInfo += "Power off command / Low battery.";
					textOut += additionalInfo + "\r\n";
					return textOut;
				}
			}

			textOut += additionalInfo + "\r\n";

			if (tsLoc.stopEvent > 0) return textOut;

			if (!pref_repeatEmptyValues)
			{
				additionalInfo = "";
				for (ushort ui = 0; ui < contoTab; ui++) additionalInfo += csvSeparator;
			}

			milli += addMilli;
			dateTimeS += ".";
			if (tsLoc.stopEvent > 0) bitsDiv = 1;

			var iend = (short)((rateComp * 3));

			for (short i = 3; i < iend; i += 3)
			{
				x = group[i];
				y = group[i + 1];
				z = group[i + 2];

				x *= gCoeff; x = Math.Round(x, cifreDec);
				y *= gCoeff; y = Math.Round(y, cifreDec);
				z *= gCoeff; z = Math.Round(z, cifreDec);

				if (rate == 1)
				{
					tsLoc.orario = tsLoc.orario.AddSeconds(1);
					dateTimeS = dateS + csvSeparator + tsLoc.orario.ToString("T", dateCi) + ".";
				}
				textOut += unitName + csvSeparator + dateTimeS + milli.ToString("D3");

				if (pref_angloTime) textOut += " " + ampm;
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

			else addMilli = (ushort)((1.0 / rateOut) * 1000);

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

		private void decodeTimeStamp(ref BinaryReader ard, ref timeStamp tsc)
		{
			tsc.stopEvent = 0;

			tsc.tsType = ard.ReadByte();

			if (tsc.tsType == 0xff)
			{
				tsc.stopEvent = 4;
				return;
			}


			if ((tsc.tsType % 10) > 0)
			{
				if (pref_isDepth)
				{
					pressureDepth(ref ard, ref tsc);
				}
				else
				{
					pressureAir(ref ard, ref tsc);
				}

			}

			if (tsc.tsType > 9)
			{
				try
				{
					tsc.batteryLevel = (float)Math.Round((((ard.ReadByte() * 256.0 + ard.ReadByte()) * 6) / 1024), 2);
				}
				catch
				{
					return;
				}
			}
			tsc.orario = tsc.orario.AddSeconds(1);
		}

		private bool pressureAir(ref BinaryReader ard, ref timeStamp tsc)
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
			tsc.temp = Math.Round(tsc.temp, 1);

			if (((tsc.tsType % 10) == 3) | ((tsc.tsType % 10) == 6))
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
				tsc.press = Math.Round(tsc.press, 2);
			}
			return false;
		}

		private bool pressureDepth(ref BinaryReader ard, ref timeStamp tsc)
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
			tsc.temp = (2000 + (dT * convCoeffs[5]) / 8_388_608);
			off = convCoeffs[1] * 65_536 + (convCoeffs[3] * dT) / 128;
			sens = convCoeffs[0] * 32_768 + (convCoeffs[2] * dT) / 256;
			if (tsc.temp > 2000)
			{
				tsc.temp -= (7 * Math.Pow(dT, 2)) / 137_438_953_472;
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
			tsc.temp = tsc.temp / 100;
			tsc.temp = Math.Round(tsc.temp, 2);
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
				if (pref_inMeters)
				{
					tsc.press -= tsc.pressOffset;
					if (tsc.press < 0) tsc.press = 0;
					else
					{
						tsc.press = tsc.press / 98.1;
						tsc.press = Math.Round(tsc.press, 2);
					}
				}
			}
			return false;
		}

		private void csvPlaceHeader(ref BinaryWriter csv)
		{
			string csvHeader = "TagID";
			if (pref_sameColumn)
			{
				csvHeader += csvSeparator + "Timestamp";
			}
			else
			{
				csvHeader += csvSeparator + "Date" + csvSeparator + "Time";
			}

			csvHeader += csvSeparator + "X" + csvSeparator + "Y" + csvSeparator + "Z";

			if (pref_inMeters)
			{
				if (pref_isDepth)
				{
					csvHeader += csvSeparator + "Depth";
				}
				else
				{
					csvHeader += csvSeparator + "Altitude";
				}
			}
			else
			{
				csvHeader += csvSeparator + "Pressure";

			}

			csvHeader += csvSeparator + "Temp. (°C)";

			if (pref_battery)
			{
				csvHeader += csvSeparator + "Battery Voltage (V)";
			}

			if (pref_metadata)
			{
				csvHeader += csvSeparator + "Metadata";
			}

			csvHeader += "\r\n";
			csv.Write(System.Text.Encoding.ASCII.GetBytes(csvHeader));
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
