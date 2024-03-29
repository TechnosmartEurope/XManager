﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using System.Globalization;
using System.ComponentModel;
#if X64
using FT_HANDLE = System.UInt64;
#else
    using FT_HANDLE = System.UInt32;
#endif

namespace X_Manager.Units
{
	class AxyDepth_2 : AxyDepth
	{
		ushort[] coeffs = new ushort[7];
		double[] convCoeffs = new double[7];
		double tempSpan, tempZero;
		//bool evitaSoglie = false;
		bool disposed = false;
		new byte[] firmwareArray = new byte[6];
		struct timeStamp
		{
			public byte tsType, tsTypeExt1, tsTypeExt2;
			public float batteryLevel;
			public double temp, fastTemp, press, pressOffset;
			public DateTime orario;
			public byte stopEvent;
			public byte timeStampLength;
			public double adcVal;
			public double magX_A, magY_A, magZ_A, magX_B, magY_B, magZ_B;
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
		int magen;
		int adcEn = 0;
		//byte[] magData_A = new byte[6];
		//byte[] magData_B = new byte[6];

		public override string modelName
		{
			get
			{
				return _modelName;
			}
			set
			{
				_modelName = value;
				if (value == "Axy-Depth")
				{
					modelCode = model_axyDepth_legacy;
				}
				else if (value == "Axy-Depth.5")
				{
					modelCode = model_axyDepth;
				}
				else
				{
					modelCode = model_axyDepthFast;
				}
			}
		}
		public AxyDepth_2(object p)
			: base(p)
		{
			positionCanSend = false;
			configurePositionButtonEnabled = false;
			modelCode = model_axyDepth;
			//modelName = "Axy-Depth";
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

		public override void getCoeffs()
		{
			coeffs[0] = 0;
			ft.Write("TTTTTTTTTTTGGAg");
			try
			{
				for (int i = 1; i <= 6; i++)
				{
					coeffs[i] = (ushort)(ft.ReadByte() * 256 + ft.ReadByte());
				}
				if (modelCode == model_axyDepthFast && firmTotA > 3005000)
				{
					ft.ReadByte(); ft.ReadByte(); ft.ReadByte(); ft.ReadByte();
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

		public override void setPcTime()
		{
			if (firmTotA < 2006000) return;

			ft.Write("TTTTTTTTGGAt");
			try
			{
				ft.ReadByte();
				byte[] dateAr = new byte[6];
				var dateToSend = DateTime.UtcNow;
				dateAr[0] = (byte)dateToSend.Second;
				dateAr[1] = (byte)dateToSend.Minute;
				dateAr[2] = (byte)dateToSend.Hour;
				dateAr[3] = (byte)dateToSend.Day;
				dateAr[4] = (byte)dateToSend.Month;
				dateAr[5] = (byte)(dateToSend.Year - 2000);
				ft.Write(dateAr, 0, 6);
				ft.ReadByte();
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
		}

		public override byte[] getConf()
		{
			byte[] conf = new byte[30];
			for (byte i = 0; i < 30; i++) conf[i] = 0xff;

			conf[22] = 0;
			conf[25] = modelCode;

			ft.ReadExisting();
			ft.Write("TTTTTTTTTTTTGGAC");
			try
			{
				for (int i = 15; i < 19; i++)
				{
					conf[i] = (byte)ft.ReadByte();
				}
				if (firmTotA >= 3000000)
				{
					if (firmTotA >= 3001000)
					{
						conf[19] = (byte)ft.ReadByte();
					}
					conf[21] = (byte)ft.ReadByte();
					if (firmTotA >= 3002000)
					{
						conf[22] = (byte)ft.ReadByte();
					}
					if (firmTotA >= 3005000)
					{
						conf[2] = ft.ReadByte();
						conf[3] = ft.ReadByte();
						conf[4] = ft.ReadByte();
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
			ft.ReadExisting();
			ft.Write("TTTTTTTTTTTTGGAc");
			try
			{
				ft.ReadByte();
				ft.Write(conf, 15, 4);
				if (firmTotA >= 3000000)
				{
					if (firmTotA >= 3001000)
					{
						ft.Write(conf, 19, 1);
					}
					ft.Write(conf, 21, 1);
					if (firmTotA >= 3002000)
					{
						ft.Write(conf, 22, 1);
					}
					if (firmTotA >= 3005000)
					{
						ft.Write(conf, 2, 3);
					}
				}
				ft.ReadByte();


			}
			catch
			{
				throw new Exception(unitNotReady);
			}
		}

		public override void abortConf()
		{

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

		public override void disconnect()
		{
			base.disconnect();
			ft.Write("TTTTTTTTTGGAO");
		}

		//private uint[] calcolaSoglieDepth()
		//{
		//	uint[] soglie = new uint[18];
		//	double[] temps = new double[] { -15, -10, 0, 10, 20, 30, 40, 50 };
		//	double[] tempsInt = new double[] { -18, -12, -5, 5, 15, 25, 35, 45, 55 };

		//	for (int i = 0; i <= 7; i++) soglie[i] = (uint)guessD2(temps[i]);

		//	for (int i = 8; i <= 16; i++) soglie[i] = guessD1(guessD2(tempsInt[i - 8]), 1520);

		//	soglie[17] = 1;
		//	return soglie;
		//}

		//private double guessD2(double t)
		//{
		//	double max = 16777215;
		//	double min = 0;
		//	double d2 = (max - min) / 2;
		//	double temp;

		//	while (Math.Abs(max - min) > 4)
		//	{
		//		temp = pTemp(d2);
		//		if (temp > t) max = d2;
		//		else min = d2;
		//		d2 = ((max - min) / 2) + min;
		//	}
		//	return d2;
		//}

		//private uint guessD1(double d2, double p)
		//{
		//	double max = 16777215;
		//	double min = 0;
		//	double d1 = (max - min) / 2;
		//	double press;

		//	while (Math.Abs(max - min) > 4)
		//	{
		//		press = pDepth(d1, d2);
		//		if (press > p) max = d1;
		//		else min = d1;
		//		d1 = ((max - min) / 2) + min;
		//	}
		//	return (uint)d1;
		//}

		//private double pDepth(double d1, double d2)
		//{
		//	double dT;
		//	double off;
		//	double sens;
		//	double temp;
		//	double[] c = new double[7];

		//	for (int count = 1; count <= 6; count++)
		//	{
		//		c[count - 1] = coeffs[count];
		//	}
		//	dT = d2 - c[4] * 256;
		//	temp = 2000 + (dT * c[5]) / 8388608;
		//	off = c[1] * 65536 + (c[3] * dT) / 128;
		//	sens = c[0] * 32768 + (c[2] * dT) / 256;
		//	if (temp > 2000)
		//	{
		//		temp -= ((2 * Math.Pow(dT, 2)) / 137438953472);
		//		off -= ((Math.Pow((temp - 2000), 2)) / 16);
		//	}
		//	else
		//	{
		//		off -= 3 * ((Math.Pow((temp - 2000), 2)) / 2);
		//		sens -= 5 * ((Math.Pow((temp - 2000), 2)) / 8);
		//		if (temp < -1500)
		//		{
		//			off -= 7 * Math.Pow((temp + 1500), 2);
		//			sens -= 4 * Math.Pow((temp + 1500), 2);
		//		}
		//		temp -= (3 * (Math.Pow(dT, 2))) / 8589934592;
		//	}
		//	sens = d1 * sens / 2097152;
		//	sens -= off;
		//	sens /= 81920;
		//	return sens;
		//}

		//private double pTemp(double d2)
		//{
		//	double ti;
		//	double dt;
		//	double temp;
		//	double[] c = new double[7];

		//	for (int count = 1; count <= 6; count++)
		//	{
		//		c[count] = coeffs[count];
		//	}
		//	dt = d2 - c[5] * 256;
		//	temp = 2000 + ((dt * c[6]) / 8388608);
		//	if ((temp / 100) < 20) ti = 3 * (Math.Pow(dt, 2)) / 8388608;
		//	else ti = 2 * (Math.Pow(dt, 2)) / 137438953472;
		//	temp = (temp - ti) / 100;
		//	return temp;
		//}

		public unsafe override void download(string fileName, uint fromMemory, uint toMemory, int baudrate)
		{
			convertStop = false;
			uint actMemory = fromMemory;
			FileMode fm = System.IO.FileMode.Create;
			if (fromMemory != 0) fm = System.IO.FileMode.Append;
			string fileNameMdp = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + ".mdp";
			var fo = new System.IO.BinaryWriter(File.Open(fileNameMdp, fm));

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

			Thread.Sleep(70);
			byte b = (byte)(baudrate / 1000000);
			ft.Write(new byte[] { b }, 0, 1);
			ft.BaudRate = (uint)baudrate;

			Thread.Sleep(200);
			if (firmTotA < 2006004)
			{
				Thread.Sleep(750);
			}

			ft.Write("S");
			Thread.Sleep(200);

			int dieCount = 0;
			try
			{
				dieCount = ft.ReadByte();
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

			byte[] outBuffer = new byte[50];
			byte[] tempBuffer = new byte[2048];
			byte[] address = new byte[8];
			byte[] fileBuffer = new byte[toMemory];
			uint bytesToWrite = 0;
			int bytesReturned = 0;

			ft.BaudRate = (uint)baudrate;
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
				bytesReturned = ft.Read(fileBuffer, actMemory, 0x1000);

				if (bytesReturned < 0)
				{
					outBuffer[0] = 88;
					ft.Write(outBuffer, 1);
					Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFailed()));
					fo.Write(fileBuffer, 0, (int)actMemory);
					fo.Close();
					return;
				}
				else if (bytesReturned != 4096)
				{
					firstLoop = true;
					continue;
				}
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
							ft.Write(outBuffer, bytesToWrite);
							ft.Read(fileBuffer, actMemory, 0x800);
							actMemory += 2048;
						}
						firstLoop = true;
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
			fo.Write(new byte[] { modelCode, (byte)254 }, 0, 2);

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
							} while (File.Exists(fileNameArd));
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
						if (File.Exists(newFileNameMdp)) fDel(newFileNameMdp);
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
			timeStamp timeStampO = new timeStamp();
			string barStatus = "";

			string shortFileName;
			string addOn = "";
			string exten = Path.GetExtension(fileName);
			if (exten.Length > 4)
			{
				addOn = ("_S" + exten.Remove(0, 4));
			}
			string fileNameCsv = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + addOn + ".csv";
			BinaryReader ard = new BinaryReader(File.Open(fileName, FileMode.Open));
			BinaryWriter csv = new BinaryWriter(File.OpenWrite(fileNameCsv));

			ard.BaseStream.Position = 1;
			firmTotA = (uint)(ard.ReadByte() * 1000 + ard.ReadByte());
			if (firmTotA > 2004)
			{
				firmTotA *= 1000;
				firmTotA += ard.ReadByte();
			}

			//Legge i coefficienti sensore di profondità
			convCoeffs = new double[] { 0, 0, 0, 0, 0, 0 };
			for (int i = 0; i < 6; i++)
			{
				convCoeffs[i] = ard.ReadByte() * 256 + ard.ReadByte();
			}

			//Se depthfast legge i coefficienti di calibrazione
			if (modelCode == model_axyDepthFast && firmTotA > 3005000)
			{
				tempSpan = ard.ReadByte() * 256 + ard.ReadByte();
				tempZero = ard.ReadByte() * 256 + ard.ReadByte();
				tempSpan /= 1000;       //span temperatura: da 0 a 65,535
				tempZero -= 32500;      //zero temperatura: da -32,5 a 32,5
				tempZero /= 1000;
			}
			else
			{
				tempSpan = 1;
				tempZero = 0;
			}

			//Imposta le preferenze di conversione

			if (prefs[p_filePrefs_fillEmpty] == "False")
			{
				pref_repeatEmptyValues = false;
			}

			dateSeparator = csvSeparator;
			if (prefs[p_filePrefs_sameColumn] == "True")
			{
				pref_sameColumn = true;
				dateSeparator = " ";
			}

			if (prefs[p_filePrefs_battery] == "True")
			{
				pref_battery = true;
			}

			dateCi = new CultureInfo("it-IT");
			if (prefs[p_filePrefs_timeFormat] == "2")
			{
				dateCi = new CultureInfo("en-US");
				pref_angloTime = true;
			}

			if (Parent.getParameter("pressureRange") == "air")
			{
				pref_isDepth = false;
			}

			if (prefs[p_filePrefs_pressMetri] == "meters")
			{
				pref_inMeters = true;
			}

			timeStampO.pressOffset = double.Parse(prefs[p_filePrefs_millibars]);
			pref_dateFormat = byte.Parse(prefs[p_filePrefs_dateFormat]);
			//timeFormat = byte.Parse(prefs[pref_timeFormat]);
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

			if (prefs[p_filePrefs_overrideTime] == "True") pref_overrideTime = true;

			if (prefs[p_filePrefs_metadata] == "True") pref_metadata = true;

			//Legge i parametri di logging
			ard.ReadByte();

			byte rrb = ard.ReadByte();
			rate = findSamplingRate(rrb);
			rateComp = rate;
			if (rate == 1 & firmTotA < 3001000)
			{
				rateComp = 5;
			}
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

			Array.Resize(ref lastGroup, rateComp * 3);
			nOutputs = rateComp;

			cifreDec = 3;
			cifreDecString = "0.000";
			if (bits)
			{
				cifreDec = 4;
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

			//Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Maximum = ard.BaseStream.Length - 1));
			progMax = ard.BaseStream.Length - 1;
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
				new Action(() => parent.statusProgressBar.Maximum = ard.BaseStream.Length - 1));
			progressWorker.RunWorkerAsync();

			while (!convertStop)
			{
				while (Interlocked.Exchange(ref progLock, 2) > 0) { }

				progVal = ard.BaseStream.Position;
				Interlocked.Exchange(ref progLock, 0);
				//Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Value = ard.BaseStream.Position));
				if (detectEof(ref ard)) break;

				try
				{
					decodeTimeStamp(ref ard, ref timeStampO);
				}
				catch
				{
					MessageBox.Show("Timestamp: " + ard.BaseStream.Position.ToString("X8"));
				}


				if (timeStampO.stopEvent > 0)
				{
					csv.Write(Encoding.ASCII.GetBytes(groupConverter(ref timeStampO, lastGroup, shortFileName)));
					break;
				}

				try
				{
					csv.Write(Encoding.ASCII.GetBytes(groupConverter(ref timeStampO, extractGroup(ref ard, ref timeStampO), shortFileName)));
				}
				catch
				{ }

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
						group[position] = 0xab;
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
						File.AppendAllText(((FileStream)ard.BaseStream).Name + "errorList.txt", "-> " + ard.BaseStream.Position.ToString("X8") + "\r\n");
					}
				}
			} while ((dummy != (byte)0xab) && (ard.BaseStream.Position < ard.BaseStream.Length));

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
			//if (group.Length == 0) return ("");

			double x, y, z;
			string ampm = "";
			string textOut, dateTimeS, additionalInfo, magAdditionalInfo;
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

			//textOut = "";

			x = group[0]; y = group[1]; z = group[2];

			x *= gCoeff; x = Math.Round(x, cifreDec);
			y *= gCoeff; y = Math.Round(y, cifreDec);
			z *= gCoeff; z = Math.Round(z, cifreDec);

			textOut = unitName + csvSeparator + dateTimeS + ".000";
			if (pref_angloTime) textOut += " " + ampm;
			textOut += csvSeparator + x.ToString(cifreDecString, nfi) + csvSeparator + y.ToString(cifreDecString, nfi) + csvSeparator + z.ToString(cifreDecString, nfi);

			additionalInfo = "";
			magAdditionalInfo = "";

			contoTab = 0;
			//Inserisce il magnetometro
			if (magen > 0)
			{
				if (((tsLoc.tsTypeExt1 & 8) == 8) | pref_repeatEmptyValues)
				{
					magAdditionalInfo += csvSeparator + tsLoc.magX_A.ToString("#.0", nfi);
					magAdditionalInfo += csvSeparator + tsLoc.magY_A.ToString("#.0", nfi);
					magAdditionalInfo += csvSeparator + tsLoc.magZ_A.ToString("#.0", nfi);
				}
				else
				{
					additionalInfo += csvSeparator + csvSeparator + csvSeparator;
				}
			}

			//Inserisce l'adc
			if (adcEn > 0)
			{
				contoTab++;
				additionalInfo += csvSeparator;
				if (((tsLoc.tsTypeExt1 & 2) == 2) | pref_repeatEmptyValues) additionalInfo += tsLoc.adcVal.ToString(nfi);
			}

			//Inserisce la pressione e la temperatura
			contoTab += 1;
			additionalInfo += csvSeparator;
			if (((tsLoc.tsType & 4) == 4) | pref_repeatEmptyValues) additionalInfo += tsLoc.press.ToString(nfi);

			contoTab += 1;
			additionalInfo += csvSeparator;
			if (((tsLoc.tsType & 2) == 2) | pref_repeatEmptyValues) additionalInfo += tsLoc.temp.ToString(nfi);

			//In caso di DepthFast inserisce la temperatura rapida
			if (modelCode == model_axyDepthFast)
			{
				contoTab += 1;
				additionalInfo += csvSeparator;
				if (((tsLoc.tsType & 16) == 16) | pref_repeatEmptyValues) additionalInfo += tsLoc.fastTemp.ToString(nfi);
			}

			//Inserisce la batteria
			if (pref_battery)
			{
				contoTab += 1;
				additionalInfo += csvSeparator;
				if (((tsLoc.tsType & 8) == 8) | pref_repeatEmptyValues) additionalInfo += tsLoc.batteryLevel.ToString(nfi);
			}

			//Inserisce i metadati
			if (pref_metadata)
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
					textOut += magAdditionalInfo + additionalInfo + "\r\n";
					return textOut;
				}
			}

			textOut += magAdditionalInfo + additionalInfo + "\r\n";

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
			milli += addMilli;

			if (!pref_repeatEmptyValues)
			{
				if (magen > 0)
				{
					magAdditionalInfo = csvSeparator + csvSeparator + csvSeparator;
				}
				additionalInfo = "";
				for (ushort ui = 0; ui < contoTab; ui++) additionalInfo += csvSeparator;
			}

			dateTimeS += ".";
			if (tsLoc.stopEvent > 0) bitsDiv = 1;

			var iend2 = (short)(rateComp * 3);
			var iend1 = iend2 / 2;
			iend1 -= (iend1 % 3);

			for (short i = 3; i < iend1; i += 3)
			{
				x = group[i];
				y = group[i + 1];
				z = group[i + 2];

				x *= gCoeff; x = Math.Round(x, cifreDec);
				y *= gCoeff; y = Math.Round(y, cifreDec);
				z *= gCoeff; z = Math.Round(z, cifreDec);

				textOut += unitName + csvSeparator + dateTimeS + milli.ToString("D3");

				if (pref_angloTime) textOut += " " + ampm;
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
			if (pref_angloTime) textOut += " " + ampm;
			textOut += csvSeparator + x.ToString(cifreDecString, nfi) + csvSeparator + y.ToString(cifreDecString, nfi) + csvSeparator + z.ToString(cifreDecString, nfi);

			if (magen == 2)
			{
				magAdditionalInfo = csvSeparator + tsLoc.magX_B.ToString("#.0", nfi);
				magAdditionalInfo += csvSeparator + tsLoc.magY_B.ToString("#.0", nfi);
				magAdditionalInfo += csvSeparator + tsLoc.magZ_B.ToString("#.0", nfi);
			}

			textOut += magAdditionalInfo + additionalInfo + "\r\n";
			if (!pref_repeatEmptyValues)
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

				if (pref_angloTime) textOut += " " + ampm;
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

			if ((tsc.tsType & 1) == 1)
			{
				try
				{
					tsc.tsTypeExt1 = ard.ReadByte();
					if ((tsc.tsTypeExt1 & 1) == 1) tsc.tsTypeExt2 = ard.ReadByte();
				}
				catch
				{
					return;
				}
			}

			if (((tsc.tsType & 2) == 2) | ((tsc.tsType & 4) == 4))
			{
				if (pref_isDepth)
				{
					if (pressureDepth5837(ref ard, ref tsc)) return;
				}
				else
				{
					if (pressureAir(ref ard, ref tsc)) return;
				}
			}


			if ((tsc.tsType & 8) == 8)
			{
				tsc.batteryLevel = (float)Math.Round(((((float)((ard.ReadByte() * 256) + ard.ReadByte())) * 6) / 4096), 2);
			}

			if ((tsc.tsType & 16) == 16)
			{
				tsc.fastTemp = ard.ReadByte() * 256 + ard.ReadByte();
				tsc.fastTemp *= 2.048;
				tsc.fastTemp /= 32768;
				tsc.fastTemp /= 2;
				//tsc.fastTemp *= 64;
				tsc.fastTemp = (((tsc.fastTemp + 0.9943) / 0.0014957 / 1000) - 1) / 0.00381;
				tsc.fastTemp *= tempSpan;
				tsc.fastTemp += tempZero;

				tsc.fastTemp = Math.Round(tsc.fastTemp, 2);
			}

			tsc.orario = tsc.orario.AddSeconds(1);

			if ((tsc.tsType & ts_ext1) == 0) return;

			//ADC VALORE
			if ((tsc.tsTypeExt1 & ts_adcValue) == ts_adcValue)
			{
				tsc.adcVal = Math.Round((double)(ard.ReadByte() * 256 + ard.ReadByte()), 2);
				//gruppoCON[adcVal] = tsc.adcVal.ToString("0000");
			}

			if ((tsc.tsTypeExt1 & 8) == 8)
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

			if ((tsc.tsTypeExt1 & 32) == 32)
			{
				if (!pref_overrideTime)
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
				else
				{
					ard.ReadBytes(8);
				}
			}
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
			tsc.temp = (2000 + (dT * convCoeffs[5]) / 8388608);
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
			tsc.temp -= t2;
			off -= off2;
			sens -= sens2;
			tsc.temp /= 100;
			tsc.temp = Math.Round(tsc.temp, 1);

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
				tsc.press = ((d1 * sens / 2097152) - off) / 32768;
				tsc.press /= 100;
				tsc.press = Math.Round(tsc.press, 2);
			}
			return false;
		}

		private bool pressureDepth5837(ref BinaryReader ard, ref timeStamp tsc)
		{
			double dT;
			double off;
			double sens;
			double d1, d2;

			try
			{
				d2 = (ard.ReadByte() * 65536 + ard.ReadByte() * 256 + ard.ReadByte());
			}
			catch
			{
				return true;
			}

			dT = d2 - convCoeffs[4] * 256;
			tsc.temp = (2000 + (dT * convCoeffs[5]) / 8388608);
			off = convCoeffs[1] * 65536 + (convCoeffs[3] * dT) / 128;
			sens = convCoeffs[0] * 32768 + (convCoeffs[2] * dT) / 256;
			if (tsc.temp > 2000)
			{
				tsc.temp -= (2 * Math.Pow(dT, 2)) / 137438953472;
				off -= ((Math.Pow((tsc.temp - 2000), 2)) / 16);
			}
			else
			{
				off -= 3 * ((Math.Pow((tsc.temp - 2000), 2)) / 2);
				sens -= 5 * ((Math.Pow((tsc.temp - 2000), 2)) / 8);
				if (tsc.temp < -1500)
				{
					off -= 7 * Math.Pow((tsc.temp + 1500), 2);
					sens -= 4 * Math.Pow((tsc.temp + 1500), 2);
				}
				tsc.temp -= (3 * (Math.Pow(dT, 2))) / 8589934592;
			}
			tsc.temp = Math.Round((tsc.temp / 100), 1);
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
				tsc.press = Math.Round((((d1 * sens / 2097152) - off) / 81920), 1);
				if (pref_inMeters)
				{
					tsc.press -= tsc.pressOffset;
					if (tsc.press <= 0) tsc.press = 0;
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

			if (!pref_inMeters)
			{
				csvHeader += csvSeparator + "Pressure";
			}
			else
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

			csvHeader += csvSeparator + "Temp. (°C)";

			if (modelCode == model_axyDepthFast)
			{
				csvHeader += csvSeparator + "Fast Temp. (°C)";
			}

			if (adcEn > 0)
			{
				csvHeader += csvSeparator + "Analog";
			}

			if (pref_battery)
			{
				csvHeader = csvHeader + csvSeparator + "Battery Voltage (V)";
			}

			if (pref_metadata)
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

