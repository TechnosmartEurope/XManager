using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows;

namespace X_Manager.Units.AxyTreks
{
	internal class AxyTrekR : AxyTrek
	{
		struct timeStamp
		{
			public int tsType;
			public int tsTypeExt1;
			public int tsTypeExt2;
			public Data data;
			public double batteryLevel;
			public double temperature;
			public double press;
			public Coord coord;
			public int timeStampLength;
			public DateTime orario;
			public int gsvSum;
			public byte[] eventAr;
			public int stopEvent;
			public int inWater;
			public int inAdc;
			public int ADC;
			public int slowData;
			public long ardPosition;
		}

		public AxyTrekR(object p)
			: base(p)
		{
			modelCode = model_axyTrekR;
		}

		public override void getCoeffs()
		{
			if (firmTotA <= 1000009)
			{
				evitaSoglie = true;
				return;
			}

			coeffs[0] = 0;
			ft.Write("TTTTTTTTTTTTTGGAg");
			try
			{
				coeffs[1] = (ushort)(ft.ReadByte() * 256 + ft.ReadByte());
				coeffs[2] = (ushort)(ft.ReadByte() * 256 + ft.ReadByte());
				coeffs[3] = (ushort)(ft.ReadByte() * 256 + ft.ReadByte());
				coeffs[4] = (ushort)(ft.ReadByte() * 256 + ft.ReadByte());
				coeffs[5] = (ushort)(ft.ReadByte() * 256 + ft.ReadByte());
				coeffs[6] = (ushort)(ft.ReadByte() * 256 + ft.ReadByte());
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
			ft.Write("TTTTTTTTGGAC");
			try
			{
				for (int i = 2; i <= 4; i++)
				{
					conf[i] = ft.ReadByte();
				}
				for (int i = 15; i <= 21; i++)
				{
					conf[i] = ft.ReadByte();
				}
				conf[22] = ft.ReadByte();
				if (firmTotA > 2000000)
				{
					conf[23] = (byte)ft.ReadByte();
				}
				if (firmTotA >= 3008000)
				{
					for (int i = 25; i < 29; i++)
					{
						conf[i] = ft.ReadByte();
					}

				}
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
			return conf;
		}

		public override void convert(string fileName, string[] prefs)
		{
			convertInit(prefs);

			string[] names = convertPrepareOutputFiles(fileName);
			string fileNameCsv = names[0];
			string FileNametxt = names[1];
			string fileNameKml = names[2];
			string fileNamePlaceMark = names[3];
			string exten = names[4];
			string logFile = names[5];
			string shortFileName;
			BinaryReader ardFile = convertOpenArdFile(fileName);
			if (ardFile is null)
			{
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.nextFile()));
				return;
			}
			BinaryWriter[] bws = convertCreateOutputFiles(names, fileName);
			BinaryWriter csv = bws[0];
			BinaryWriter txt = bws[1];
			BinaryWriter kml = bws[2];
			BinaryWriter placeMark = bws[3];

			timeStamp timeStampO = new timeStamp();
			byte[] ev = new byte[5];
			string barStatus = "";
			timeStampO.eventAr = ev;
			timeStampO.inAdc = 0;
			timeStampO.inWater = 0;

			List<long> sesAdd = convertFillSessionList(exten, ardFile);
			int sesMax = sesAdd.Count;
			int sesCounter = 1;

			bool headerMissing = true;

			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => barStatus = (string)parent.statusLabel.Content));

			while (sesAdd.Count > 0)
			{

				MemoryStream ard = convertExtractSession(fileName, sesAdd, txt, sesCounter);

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
					//adcThreshold = (ushort)(ard.ReadByte() * 256 + ard.ReadByte());
					ard.ReadByte(); ard.ReadByte();
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
				checkTdEn((byte)ard.ReadByte(), this, exten);
				checkAccParam((byte)ard.ReadByte());

				findDebugStampPar();
				Array.Resize(ref lastGroup, rate * 3);

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


				long pos = ard.Position;
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

				if (exten.Contains("rem") && !convertStop)
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
						csv.Write(Encoding.ASCII.GetBytes(sBuffer));
						break;
					}

					try
					{
						groupConverter(ref timeStampO, extractGroup(ref ard, ref timeStampO.timeStampLength), shortFileName, ref sBuffer, ref infRemPosition);

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
						if (makeTxt)
						{
							txtWrite(ref timeStampO.tsType, ref timeStampO.coord, ref timeStampO.orario, ref timeStampO.data, ref timeStampO.gsvSum,
									ref timeStampO.ardPosition, ref timeStampO.eventAr, ref txt);
						}
					}
					if ((timeStampO.tsType & 16) == 16)
					{
						if (makeKml) kmlWrite(ref timeStampO.coord, ref timeStampO.orario, ref kml, ref placeMark);
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

				if (makeTxt)
				{
					txtWrite(ref timeStampO.tsType, ref timeStampO.coord, ref timeStampO.orario, ref timeStampO.data, ref timeStampO.gsvSum,
									ref timeStampO.ardPosition, ref timeStampO.eventAr, ref txt);
				}
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
					placeMark.Write(Encoding.ASCII.GetBytes(Properties.Resources.Folder_Bot));
					placeMark.Write(Encoding.ASCII.GetBytes(Properties.Resources.Placemarks_Stop_Top +
						decoderKml(ref timeStampO.coord, ref timeStampO.orario).cPlacemark + Properties.Resources.Placemarks_Stop_Bot));
					kml.Close();
					placeMark.Close();

					//Scrive l'header finale nel file kml string
					File.AppendAllText(fileNameKml, Properties.Resources.Path_Bot);
					File.AppendAllText(fileNameKml, Properties.Resources.Folder_Bot);

					//Accorpa kml placemark e string
					File.AppendAllText(fileNamePlaceMark, File.ReadAllText(fileNameKml));

					//Chiude il kml placemark
					File.AppendAllText(fileNamePlaceMark, Properties.Resources.Final_Bot);
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
					tsc.temperature = (uint)tsc.temperature >> 6;
					if (tsc.temperature > 511)
					{
						tsc.temperature -= 1024;
					}
					tsc.temperature = (tsc.temperature * 0.1221) + 22.5;
				}
				else
				{
					if (isDepth)
					{
						if (fTotA > 2000000)
						{
							if (pressureDepth5837(ref ard, ref tsc.temperature, ref tsc.press, pressOffset, ref tsc.tsType)) return;
						}
						else
						{
							if (pressureDepth5803(ref ard, ref tsc.temperature, ref tsc.press, pressOffset, ref tsc.tsType)) return;
						}
					}
					else
					{
						if (pressureAir(ref ard, ref tsc.temperature, ref tsc.press, pressOffset, ref tsc.tsType)) return;
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

				tsc.data.secondi = unchecked(fissi[0] >> 2);

				tsc.coord.latMinDecL = unchecked((fissi[0] & 3) << 5);
				tsc.coord.latMinDecL += unchecked(fissi[1] >> 3);
				tsc.coord.lonMinDecL = unchecked((fissi[1] & 7) << 4);
				tsc.coord.lonMinDecL += unchecked(fissi[2] >> 4);

				tsc.coord.DOPdec = unchecked((fissi[2] & 15) >> 1);

				tsc.coord.vel = unchecked((fissi[2] & 1) << 5);
				tsc.coord.vel += unchecked(fissi[3] >> 3);
				//tsc.vel *= 2;

				tsc.coord.nSat = (fissi[3] & 7);
				tsc.coord.altL = fissi[4];

				tsc.coord.latMinDecLL = unchecked(fissi[5] >> 4);
				tsc.coord.lonMinDecLL = (fissi[5] & 15);

				tsc.coord.altSegno = 0; tsc.coord.ns = 0; tsc.coord.eo = 0;
				if ((diffMask & 1) == 1) tsc.coord.eo = 1;
				if ((diffMask & 2) == 2) tsc.coord.ns = 1;
				if ((diffMask & 4) == 4) tsc.coord.altSegno = 1;
				if ((diffMask & 8) == 8) tsc.data.anno = ard.ReadByte();
				if ((diffMask & 0x10) == 0x10) tsc.data.giorno = ard.ReadByte();
				if ((diffMask & 0x20) == 0x20) tsc.coord.DOP = ard.ReadByte();
				if ((diffMask & 0x40) == 0x40) tsc.coord.lonMinDecH = (byte)ard.ReadByte();
				if ((diffMask & 0x80) == 0x80) tsc.coord.lonMinuti = (byte)ard.ReadByte();
				if ((diffMask & 0x100) == 0x100) tsc.coord.lonGradi = (byte)ard.ReadByte();
				if ((diffMask & 0x200) == 0x200) tsc.coord.latMinDecH = ard.ReadByte();
				if ((diffMask & 0x400) == 0x400) tsc.coord.latMinuti = ard.ReadByte();
				if ((diffMask & 0x800) == 0x800) tsc.coord.latGradi = ard.ReadByte();
				if ((diffMask & 0x1000) == 0x1000) tsc.data.minuti = ard.ReadByte();
				if ((diffMask & 0x2000) == 0x2000) tsc.data.ore = ard.ReadByte();
				if ((diffMask & 0x4000) == 0x4000)
				{
					int b = ard.ReadByte();
					tsc.data.mese = unchecked(b >> 4);
					tsc.coord.altH = b & 15;
				}
				tsc.gsvSum = ard.ReadByte() * 256 + ard.ReadByte();

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

			textOut += unitName + csvSeparator + dateTimeS + ".000";
			if (angloTime)
			{
				textOut += " " + ampm;
			}
			if (addGpsTime)
			{
				if ((tsLoc.tsType & 16) == 16)
				{
					textOut += " (GPS: " + tsLoc.data.ore.ToString("00") + ":" + tsLoc.data.minuti.ToString("00") + ":" + tsLoc.data.secondi.ToString("00") + ") ";
					DateTime dtDiff;
					try
					{
						dtDiff = new DateTime(tsLoc.orario.Year, tsLoc.orario.Month, tsLoc.orario.Day,
						tsLoc.data.ore, tsLoc.data.minuti, tsLoc.data.secondi);
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
				additionalInfo += csvSeparator + (tsLoc.ardPosition + offset).ToString("X") + csvSeparator + tsLoc.timeStampLength.ToString();
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
				if (tsLoc.coord.altSegno == 0) altSegno = "";
				if (tsLoc.coord.eo == 1) eo = "";
				if (tsLoc.coord.ns == 1) ns = "";
				double speed = tsLoc.coord.vel * 3.704;
				double lon, lat = 0;

				lon = ((tsLoc.coord.lonMinuti + (tsLoc.coord.lonMinDecH / 100.0) + (tsLoc.coord.lonMinDecL / 10000.0) + (tsLoc.coord.lonMinDecLL / 100000.0)) / 60) + tsLoc.coord.lonGradi;
				lat = ((tsLoc.coord.latMinuti + (tsLoc.coord.latMinDecH / 100.0) + (tsLoc.coord.latMinDecL / 10000.0) + (tsLoc.coord.latMinDecLL / 100000.0)) / 60) + tsLoc.coord.latGradi;

				additionalInfo += csvSeparator + ns + lat.ToString("#00.00000", nfi);
				additionalInfo += csvSeparator + eo + lon.ToString("#00.00000", nfi);
				additionalInfo += csvSeparator + altSegno + ((tsLoc.coord.altH * 256 + tsLoc.coord.altL) * 2).ToString();
				additionalInfo += csvSeparator + speed.ToString("0.0", nfi);
				additionalInfo += csvSeparator + tsLoc.coord.nSat.ToString();
				additionalInfo += csvSeparator + tsLoc.coord.DOP.ToString() + "." + tsLoc.coord.DOPdec.ToString();
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


			//long fileLength = br.Length;

			DateTime dt = new DateTime(int.Parse(prefs[pref_date_year]), int.Parse(prefs[pref_date_month]), int.Parse(prefs[pref_date_day]),
				int.Parse(prefs[pref_h]), int.Parse(prefs[pref_m]), int.Parse(prefs[pref_s]));
			if (isRem)
			{
				dt = new DateTime(1, 1, 1, 1, 1, 1);
			}

			if (overrideTime) return dt;

			timeStamp tsc = new timeStamp();
			pos -= 1;

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
			tsc.data.anno = 14;
			tsc.data.giorno = 19;
			tsc.data.mese = 11;
			tsc.data.ore = 12;
			tsc.data.minuti = 51;
			ushort diffMask = (ushort)(coordinate[0] * 256 + coordinate[1]);

			tsc.data.secondi = unchecked(coordinate[2] >> 2);
			tsc.coord.latMinDecL = unchecked((coordinate[2] & 3) << 5);
			tsc.coord.latMinDecL += unchecked(coordinate[3] >> 3);
			tsc.coord.lonMinDecL = unchecked((coordinate[3] & 7) << 4);
			tsc.coord.lonMinDecL += unchecked((coordinate[4] >> 4));
			tsc.coord.DOPdec = unchecked((coordinate[4] & 15) >> 1);
			tsc.coord.vel = unchecked((coordinate[4] & 1) << 5);
			tsc.coord.vel += unchecked((coordinate[5] >> 3));
			tsc.coord.vel *= 2;
			tsc.coord.nSat = (coordinate[5] & 7);
			tsc.coord.altL = coordinate[6];
			tsc.coord.latMinDecLL += unchecked(coordinate[7] >> 4);
			tsc.coord.lonMinDecLL = (coordinate[7] & 15);
			byte cCounter = 8;

			//tsc.altSegno = true; tsc.ns = false; tsc.eo = false;
			//if ((diffMask & 1) == 1) tsc.eo = true;
			//if ((diffMask & 2) == 2) tsc.ns = true;
			//if ((diffMask & 4) == 4) tsc.altSegno = true;
			if ((diffMask & 8) == 8) { tsc.data.anno = coordinate[cCounter]; cCounter += 1; }
			if ((diffMask & 0x10) == 0x10) { tsc.data.giorno = coordinate[cCounter]; cCounter += 1; }
			if ((diffMask & 0x20) == 0x20) { tsc.coord.DOP = coordinate[cCounter]; cCounter += 1; }
			if ((diffMask & 0x40) == 0x40) { tsc.coord.lonMinDecH = coordinate[cCounter]; cCounter += 1; }
			if ((diffMask & 0x80) == 0x80) { tsc.coord.lonMinuti = coordinate[cCounter]; cCounter += 1; }
			if ((diffMask & 0x100) == 0x100) { tsc.coord.lonGradi = coordinate[cCounter]; cCounter += 1; }
			if ((diffMask & 0x200) == 0x200) { tsc.coord.latMinDecH = coordinate[cCounter]; cCounter += 1; }
			if ((diffMask & 0x400) == 0x400) { tsc.coord.latMinuti = coordinate[cCounter]; cCounter += 1; }
			if ((diffMask & 0x800) == 0x800) { tsc.coord.latGradi = coordinate[cCounter]; cCounter += 1; }
			if ((diffMask & 0x1000) == 0x1000) { tsc.data.minuti = coordinate[cCounter]; cCounter += 1; }
			if ((diffMask & 0x2000) == 0x2000) { tsc.data.ore = coordinate[cCounter]; cCounter += 1; }
			if ((diffMask & 0x4000) == 0x4000)
			{
				tsc.data.mese = unchecked(coordinate[cCounter] >> 4);
				tsc.coord.altH = (coordinate[cCounter] & 15);
			}
			//br.Close();
			secondiAdd += 1;    //Questo secondo sottratto in più viene reinserito al decoding del primo timestamp

			try
			{
				dt = new DateTime(2000 + tsc.data.anno, tsc.data.mese, tsc.data.giorno, tsc.data.ore, tsc.data.minuti, tsc.data.secondi);
				dt = dt.AddSeconds(-secondiAdd);
				dt = dt.AddSeconds(leapSeconds * -1);
			}
			catch { }

			return dt;
		}

		protected override void csvPlaceHeader(ref BinaryWriter csv)
		{
			base.csvPlaceHeader(ref csv);
			string csvHeader = "";

			if (pressureEnabled > 0)
			{
				if (inMeters)
				{
					if (isDepth)
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
			if (adcLog) csvHeader += csvSeparator + "Radar Signal RAW";
			if (adcStop) csvHeader += csvSeparator + "Sensor State";
			if (prefBattery) csvHeader += csvSeparator + "Battery (V)";
			if (metadata) csvHeader += csvSeparator + "Metadata";

			csvHeader += "\r\n";

			csv.Write(Encoding.ASCII.GetBytes(csvHeader));
		}

	}
}
