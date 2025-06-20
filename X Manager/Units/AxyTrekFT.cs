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
	internal class AxyTrekFT : AxyTrek
	{
		double tempSpan, tempZero;

		public AxyTrekFT(object p) : base(p)
		{
			modelCode = model_axyTrekFT;
		}

		public override void getCoeffs()
		{
			coeffs[0] = 0;
			resetTimer();
			ft.Write("TTTTTTTTTTTTTGGAg");
			try
			{
				coeffs[1] = (ushort)(ft.ReadByte() * 256 + ft.ReadByte());
				coeffs[2] = (ushort)(ft.ReadByte() * 256 + ft.ReadByte());
				coeffs[3] = (ushort)(ft.ReadByte() * 256 + ft.ReadByte());
				coeffs[4] = (ushort)(ft.ReadByte() * 256 + ft.ReadByte());
				coeffs[5] = (ushort)(ft.ReadByte() * 256 + ft.ReadByte());
				coeffs[6] = (ushort)(ft.ReadByte() * 256 + ft.ReadByte());
				tempSpan = ft.ReadByte() * 256 + ft.ReadByte();
				tempZero = ft.ReadByte() * 256 + ft.ReadByte();
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
			resetTimer();
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
				conf[23] = ft.ReadByte();
				for (int i = 25; i < 29; i++)
				{
					conf[i] = ft.ReadByte();
				}
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
			return conf;
		}

		protected override void importHeaderValuesFromArd()
		{
			//importa el impostazioni del sensore Analogico
			ard.ReadByte();
			//adcThreshold = (ushort)(ard.ReadByte() * 256 + ard.ReadByte());
			ard.ReadByte(); ard.ReadByte();
			byte adcTemp = (byte)ard.ReadByte();
			if ((adcTemp & 8) == 8) adcStop = true;
			if ((adcTemp & 2) == 2) adcLog = true;

			//Importa i coefficienti di calibrazione del sensore depth
			convCoeffs = new double[] { 0, 0, 0, 0, 0, 0 };
			for (int u = 0; u <= 5; u++)
			{
				convCoeffs[u] = (ard.ReadByte() * 256) + ard.ReadByte();
			}

			//Importa i coeddicienti di calibrazione del sensore fast temperature
			tempSpan = ard.ReadByte() * 256 + ard.ReadByte();
			tempZero = ard.ReadByte() * 256 + ard.ReadByte();
			tempSpan /= 1000;       //span temperatura: da 0 a 65,535
			tempZero -= 32500;      //zero temperatura: da -32,5 a 32,5
			tempZero /= 1000;
		}

		public override void convert(string fileNameIn)
		{
			fileName = fileNameIn;
			List<long> sesAdd = null;
			convertInit(ref sesAdd);
			if (sesAdd == null)
			{
				txt.Close();
				csv.Close();
				kml.Close();
				placeMark.Close();
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.nextFile(true)));
				return;
			}

			convertData(sesAdd);

		}

		protected override void decodeTimeStamp()
		{
			timestamp.ardPosition = ard.Position;
			timestamp.stopEvent = 0;
			timestamp.slowData = 0;

			timestamp.tsType = ard.ReadByte();

			//Flag timestamp esteso
			if ((timestamp.tsType & 1) == 1)
			{
				timestamp.tsTypeExt1 = ard.ReadByte();
				if ((timestamp.tsTypeExt1 & 1) == 1)
				{
					timestamp.tsTypeExt2 = ard.ReadByte();
				}
				else
				{
					timestamp.tsTypeExt2 = 0;
				}
			}
			else
			{
				timestamp.tsTypeExt1 = 0;
			}

			//Fast temperature
			if ((timestamp.tsType & 2) == 2)
			{
				ard.ReadByte();
				timestamp.fastTemperature = ard.ReadByte() * 256 + ard.ReadByte();
				timestamp.fastTemperature *= 2.048;
				timestamp.fastTemperature /= 32768;
				timestamp.fastTemperature /= 2;
				timestamp.fastTemperature = (((timestamp.fastTemperature + 0.9943) / 0.0014957 / 1000) - 1) / 0.00381;

				timestamp.fastTemperature *= tempSpan;
				timestamp.fastTemperature += tempZero;
			}

			//Temperatura e pressione sensore piccolo
			if ((timestamp.tsType & 4) == 4)
			{
				timestamp.slowData++;
				if (pref_isDepth)
				{
					if (pressureDepth5837()) return;

				}
				else
				{
					if (pressureAir()) return;
				}
			}

			//Batteria
			if ((timestamp.tsType & 8) == 8)
			{
				timestamp.slowData++;
				timestamp.batteryLevel = ((ard.ReadByte() * 256 + ard.ReadByte()) * 6.0 / 4096);
			}

			//Coordinata
			if ((timestamp.tsType & 16) == 16)
			{
				timestamp.slowData++;
				ushort diffMask = (ushort)(ard.ReadByte() * 256 + ard.ReadByte());  //Legge la maschera
				byte[] fissi = new byte[6];//= ard.ReadBytes(6);                                    //Legge i dati fissi
				ard.Read(fissi, 0, 6);

				timestamp.data.secondi = unchecked(fissi[0] >> 2);

				timestamp.coord.latMinDecL = unchecked((fissi[0] & 3) << 5);
				timestamp.coord.latMinDecL += unchecked(fissi[1] >> 3);
				timestamp.coord.lonMinDecL = unchecked((fissi[1] & 7) << 4);
				timestamp.coord.lonMinDecL += unchecked(fissi[2] >> 4);

				timestamp.coord.DOPdec = unchecked((fissi[2] & 15) >> 1);

				timestamp.coord.vel = unchecked((fissi[2] & 1) << 5);
				timestamp.coord.vel += unchecked(fissi[3] >> 3);
				//timestamp.vel *= 2;

				timestamp.coord.nSat = (fissi[3] & 7);
				timestamp.coord.altL = fissi[4];

				timestamp.coord.latMinDecLL = unchecked(fissi[5] >> 4);
				timestamp.coord.lonMinDecLL = (fissi[5] & 15);

				timestamp.coord.altSegno = 0; timestamp.coord.ns = 0; timestamp.coord.eo = 0;
				if ((diffMask & 1) == 1) timestamp.coord.eo = 1;
				if ((diffMask & 2) == 2) timestamp.coord.ns = 1;
				if ((diffMask & 4) == 4) timestamp.coord.altSegno = 1;
				if ((diffMask & 8) == 8) timestamp.data.anno = ard.ReadByte();
				if ((diffMask & 0x10) == 0x10) timestamp.data.giorno = ard.ReadByte();
				if ((diffMask & 0x20) == 0x20) timestamp.coord.DOP = ard.ReadByte();
				if ((diffMask & 0x40) == 0x40) timestamp.coord.lonMinDecH = (byte)ard.ReadByte();
				if ((diffMask & 0x80) == 0x80) timestamp.coord.lonMinuti = (byte)ard.ReadByte();
				if ((diffMask & 0x100) == 0x100) timestamp.coord.lonGradi = (byte)ard.ReadByte();
				if ((diffMask & 0x200) == 0x200) timestamp.coord.latMinDecH = ard.ReadByte();
				if ((diffMask & 0x400) == 0x400) timestamp.coord.latMinuti = ard.ReadByte();
				if ((diffMask & 0x800) == 0x800) timestamp.coord.latGradi = ard.ReadByte();
				if ((diffMask & 0x1000) == 0x1000) timestamp.data.minuti = ard.ReadByte();
				if ((diffMask & 0x2000) == 0x2000) timestamp.data.ore = ard.ReadByte();
				if ((diffMask & 0x4000) == 0x4000)
				{
					int b = ard.ReadByte();
					timestamp.data.mese = unchecked(b >> 4);
					timestamp.coord.altH = b & 15;
				}
				timestamp.gsvSum = ard.ReadByte() * 256 + ard.ReadByte();
				if (pref_overrideSystemDate)
				{
					timestamp.orario = new DateTime(timestamp.data.anno, timestamp.data.mese, timestamp.data.giorno, timestamp.data.ore, timestamp.data.minuti, timestamp.data.secondi);
				}
				else
				{
					timestamp.orario = timestamp.orario.AddSeconds(timestamp.secondAmount);
				}
			}
			else
			{
				timestamp.orario = timestamp.orario.AddSeconds(timestamp.secondAmount);
			}


			//evento
			if ((timestamp.tsType & 32) == 32)
			{
				timestamp.slowData++;
				int b = ard.ReadByte();
				int debugCheck = ard.ReadByte();
				ard.Position -= 2;
				if ((b == debugStampId) && (debugCheck > 2))
				{
					timestamp.eventAr = new byte[debugStampLenght];
					ard.Read(timestamp.eventAr, 0, debugStampLenght);
					timestamp.eventAr[0] = 80;
				}
				else
				{
					ard.Read(timestamp.eventAr, 0, 5);
				}

				if (timestamp.eventAr[0] == 11) timestamp.stopEvent = 1;
				else if (timestamp.eventAr[0] == 12) timestamp.stopEvent = 2;
				else if (timestamp.eventAr[0] == 13) timestamp.stopEvent = 3;
				else if (timestamp.eventAr[0] == 14)
				{
					timestamp.stopEvent = 4;
				}

			}

			//Attività/acqua
			timestamp.inWater = 0;
			if ((timestamp.tsType & 128) == 128) timestamp.inWater = 1;

			//Parametri estesi
			timestamp.secondAmount = 1;
			if ((timestamp.tsType & 1) == 1)
			{
				//ADC log
				if ((timestamp.tsTypeExt1 & 2) == 2)
				{
					timestamp.slowData++;
					timestamp.ADC = (ard.ReadByte() * 256 + ard.ReadByte());
				}

				//ADC Threshold
				timestamp.inAdc = 0;
				if ((timestamp.tsTypeExt1 & 0x4) == 0x4)
				{
					timestamp.inAdc = 1;
				}

				//Timestamp multiplo
				if ((timestamp.tsTypeExt1 & 0x40) == 0x40)
				{
					timestamp.secondAmount = (byte)ard.ReadByte();
				}
			}
		}

		protected override void groupConverter(double[] group, ref string textOut, long offset)
		{
			if (group == null) return;
			short iend;
			if (group.Length == 0)
			{
				if (timestamp.slowData > 0)
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
			string additionalInfo;
			string dateS;
			NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;
			string activityWater = "";

			ushort contoTab = 0;

			dateS = timestamp.orario.ToString(pref_dateFormatParameter, CultureInfo.InvariantCulture);
			textOut += shortFileName + csvSeparator + dateS;

			if (pref_addGpsTime)
			{
				if ((timestamp.tsType & 16) == 16)
				{
					textOut += " (GPS: " + timestamp.data.ore.ToString("00") + ":" + timestamp.data.minuti.ToString("00") + ":" + timestamp.data.secondi.ToString("00") + ") ";
					DateTime dtDiff;
					try
					{
						dtDiff = new DateTime(timestamp.orario.Year, timestamp.orario.Month, timestamp.orario.Day,
						timestamp.data.ore, timestamp.data.minuti, timestamp.data.secondi);
						double ts = (dtDiff - timestamp.orario).TotalSeconds;
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
			if (pref_debugLevel > 0)
			{
				additionalInfo += csvSeparator + (timestamp.ardPosition + offset).ToString("X") + csvSeparator + timestamp.timeStampLength.ToString();
			}
			contoTab += 1;
			if ((timestamp.tsType & 64) == 64) activityWater = "Active";
			else activityWater = "Inactive";
			if ((timestamp.tsType & 128) == 128) activityWater += "/Wet";
			else activityWater += "/Dry";

			additionalInfo += csvSeparator + activityWater;

			contoTab += 1;
			additionalInfo += csvSeparator;
			if (((timestamp.tsType & 2) == 2) | pref_repeatEmptyValues) additionalInfo += timestamp.fastTemperature.ToString("0.0", nfi);

			if (pressureEnabled > 0)
			{
				contoTab += 2;
				if (((timestamp.tsType & 4) == 4) | pref_repeatEmptyValues)
				{
					additionalInfo += csvSeparator + timestamp.press.ToString("0.00", nfi) + csvSeparator + timestamp.temperature.ToString("0.00", nfi);
				}
				else
				{
					additionalInfo += csvSeparator + csvSeparator;
				}
			}

			//Inserire la coordinata.
			contoTab += 7;
			if (((timestamp.tsType & 16) == 16) | pref_repeatEmptyValues)
			{
				string altSegno, eo, ns;
				altSegno = eo = ns = "-";
				if (timestamp.coord.altSegno == 0) altSegno = "";
				if (timestamp.coord.eo == 1) eo = "";
				if (timestamp.coord.ns == 1) ns = "";
				double speed = timestamp.coord.vel * 3.704;
				double lon, lat = 0;

				lon = ((timestamp.coord.lonMinuti + (timestamp.coord.lonMinDecH / 100.0) + (timestamp.coord.lonMinDecL / 10000.0) + (timestamp.coord.lonMinDecLL / 100000.0)) / 60) + timestamp.coord.lonGradi;
				lat = ((timestamp.coord.latMinuti + (timestamp.coord.latMinDecH / 100.0) + (timestamp.coord.latMinDecL / 10000.0) + (timestamp.coord.latMinDecLL / 100000.0)) / 60) + timestamp.coord.latGradi;

				additionalInfo += csvSeparator + ns + lat.ToString("#00.00000", nfi);
				additionalInfo += csvSeparator + eo + lon.ToString("#00.00000", nfi);
				additionalInfo += csvSeparator + altSegno + ((timestamp.coord.altH * 256 + timestamp.coord.altL) * 2).ToString();
				additionalInfo += csvSeparator + speed.ToString("0.0", nfi);
				additionalInfo += csvSeparator + timestamp.coord.nSat.ToString();
				additionalInfo += csvSeparator + timestamp.coord.DOP.ToString() + "." + timestamp.coord.DOPdec.ToString();
				additionalInfo += csvSeparator + timestamp.gsvSum.ToString();
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
				if (((timestamp.tsTypeExt1 & 2) == 2) | pref_repeatEmptyValues) additionalInfo += timestamp.ADC.ToString("0000");
			}

			if (adcStop)
			{
				contoTab += 1;
				additionalInfo += csvSeparator;
				if (((timestamp.tsTypeExt1 & 4) == 4) | pref_repeatEmptyValues) additionalInfo += "Threshold crossed";
			}

			//Inserisce la batteria
			//if (pref_battery)
			//{
			contoTab += 1;
			additionalInfo += csvSeparator;
			if (((timestamp.tsType & 8) == 8) | pref_repeatEmptyValues) additionalInfo += timestamp.batteryLevel.ToString("0.00", nfi);
			//}

			//Inserisce i metadati
			if (pref_metadata)
			{
				contoTab += 1;
				additionalInfo += csvSeparator;
				if (timestamp.stopEvent > 0)
				{
					switch (timestamp.stopEvent)
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

			if (timestamp.stopEvent > 0) return;// textOut;

			if (!pref_repeatEmptyValues)
			{
				additionalInfo = "";
				for (ushort ui = 0; ui < contoTab; ui++) additionalInfo += csvSeparator;
			}

			timestamp.orario = timestamp.orario.AddMilliseconds(addMilli);
			if (timestamp.stopEvent > 0) bitsDiv = 1;

			for (short i = 3; i < iend; i += 3)
			{
				x = group[i] * gCoeff;
				y = group[i + 1] * gCoeff;
				z = group[i + 2] * gCoeff;

				//if (rate == 1)
				//{
				//	timestamp.orario = timestamp.orario.AddSeconds(1);
				dateS = timestamp.orario.ToString(pref_dateFormatParameter, CultureInfo.InvariantCulture);
				//}
				textOut += shortFileName + csvSeparator + dateS;

				textOut += csvSeparator + x.ToString(cifreDecString, nfi) + csvSeparator + y.ToString(cifreDecString, nfi) + csvSeparator + z.ToString(cifreDecString, nfi);

				textOut += additionalInfo + "\r\n";
				timestamp.orario = timestamp.orario.AddMilliseconds(addMilli);
			}
			timestamp.orario = timestamp.orario.AddSeconds(-1);

			//return textOut;
		}

		protected override DateTime findStartTime(long pos, bool isRem)
		{

			DateTime dt = orDateTime;
			if (isRem)
			{
				dt = new DateTime(1, 1, 1, 1, 1, 1);
			}

			if (pref_overrideTime) return dt;

			TimeStamp tsc = new TimeStamp();
			pos -= 1;

			byte timeStamp0 = 0;
			byte timeStamp1 = 0;
			uint secondiAdd = 0;
			byte[] coordinate = new byte[22];


			//ard.BaseStream.Position = 7;
			//if (ard.ReadByte() == 0) ard.BaseStream.Position = 25;
			//else ard.BaseStream.Position = 21;
			//var br = new MemoryStream(buf);

			ard.Position = pos;
			ushort secondAmount = 1;
			double brMax = ard.Length;
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Minimum = 0));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Maximum = brMax));

			while (ard.Position < ard.Length)
			{

				if (ard.ReadByte() == 0xab)
				{
					timeStamp0 = (byte)ard.ReadByte();
					secondAmount = 1;
					if (timeStamp0 != 0xab)
					{
						double ppos = ard.Position;
						Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Value = ppos));
						if ((timeStamp0 & 1) == 1) timeStamp1 = (byte)ard.ReadByte();
						if ((timeStamp0 & 2) == 2) ard.Position += 3;    //Fast temp.
						if ((timeStamp0 & 4) == 4) ard.Position += 6;    //Press + temp sensore piccolo
						if ((timeStamp0 & 8) == 8) ard.Position += 2;
						if ((timeStamp0 & 16) == 16)
						{
							ard.Read(coordinate, 0, 22);
							//break;
							if (coordinate[8] != 80) break;
						}
						if ((timeStamp0 & 32) == 32)
						{
							byte ev = (byte)ard.ReadByte();       //ex b
							int debugCheck = (byte)ard.ReadByte();

							ard.Position -= 2;
							if ((ev == debugStampId) && (debugCheck > 2))
							{
								//ard.ReadBytes(debugStampLenght);
								ard.Position += debugStampLenght;
							}
							else
							{
								//ard.ReadBytes(5);
								ard.Position += 5;
							}

						}
						if ((timeStamp0 & 1) == 1)
						{
							if ((timeStamp1 & 2) == 2) ard.Position += 2;
							if ((timeStamp1 & 0x40) == 0x40) secondAmount = (byte)ard.ReadByte();
						}
						secondiAdd += secondAmount;
					}
				}
			}
			if (ard.Position >= ard.Length)
			{
				//ard.Close();
				return dt;
			}

			// Valori di sicurezza
			timestamp.data.anno = 14;
			timestamp.data.giorno = 19;
			timestamp.data.mese = 11;
			timestamp.data.ore = 12;
			timestamp.data.minuti = 51;
			ushort diffMask = (ushort)(coordinate[0] * 256 + coordinate[1]);

			timestamp.data.secondi = unchecked(coordinate[2] >> 2);
			timestamp.coord.latMinDecL = unchecked((coordinate[2] & 3) << 5);
			timestamp.coord.latMinDecL += unchecked(coordinate[3] >> 3);
			timestamp.coord.lonMinDecL = unchecked((coordinate[3] & 7) << 4);
			timestamp.coord.lonMinDecL += unchecked((coordinate[4] >> 4));
			timestamp.coord.DOPdec = unchecked((coordinate[4] & 15) >> 1);
			timestamp.coord.vel = unchecked((coordinate[4] & 1) << 5);
			timestamp.coord.vel += unchecked((coordinate[5] >> 3));
			timestamp.coord.vel *= 2;
			timestamp.coord.nSat = (coordinate[5] & 7);
			timestamp.coord.altL = coordinate[6];
			timestamp.coord.latMinDecLL += unchecked(coordinate[7] >> 4);
			timestamp.coord.lonMinDecLL = (coordinate[7] & 15);
			byte cCounter = 8;

			//timestamp.altSegno = true; timestamp.ns = false; timestamp.eo = false;
			//if ((diffMask & 1) == 1) timestamp.eo = true;
			//if ((diffMask & 2) == 2) timestamp.ns = true;
			//if ((diffMask & 4) == 4) timestamp.altSegno = true;
			if ((diffMask & 8) == 8) { timestamp.data.anno = coordinate[cCounter]; cCounter += 1; }
			if ((diffMask & 0x10) == 0x10) { timestamp.data.giorno = coordinate[cCounter]; cCounter += 1; }
			if ((diffMask & 0x20) == 0x20) { timestamp.coord.DOP = coordinate[cCounter]; cCounter += 1; }
			if ((diffMask & 0x40) == 0x40) { timestamp.coord.lonMinDecH = coordinate[cCounter]; cCounter += 1; }
			if ((diffMask & 0x80) == 0x80) { timestamp.coord.lonMinuti = coordinate[cCounter]; cCounter += 1; }
			if ((diffMask & 0x100) == 0x100) { timestamp.coord.lonGradi = coordinate[cCounter]; cCounter += 1; }
			if ((diffMask & 0x200) == 0x200) { timestamp.coord.latMinDecH = coordinate[cCounter]; cCounter += 1; }
			if ((diffMask & 0x400) == 0x400) { timestamp.coord.latMinuti = coordinate[cCounter]; cCounter += 1; }
			if ((diffMask & 0x800) == 0x800) { timestamp.coord.latGradi = coordinate[cCounter]; cCounter += 1; }
			if ((diffMask & 0x1000) == 0x1000) { timestamp.data.minuti = coordinate[cCounter]; cCounter += 1; }
			if ((diffMask & 0x2000) == 0x2000) { timestamp.data.ore = coordinate[cCounter]; cCounter += 1; }
			if ((diffMask & 0x4000) == 0x4000)
			{
				timestamp.data.mese = unchecked(coordinate[cCounter] >> 4);
				timestamp.coord.altH = (coordinate[cCounter] & 15);
			}
			//ard.Close();
			secondiAdd += 1;    //Questo secondo sottratto in più viene reinserito al decoding del primo timestamp

			try
			{
				dt = new DateTime(2000 + timestamp.data.anno, timestamp.data.mese, timestamp.data.giorno, timestamp.data.ore, timestamp.data.minuti, timestamp.data.secondi);
				dt = dt.AddSeconds(-secondiAdd);
				dt = dt.AddSeconds(pref_leapSeconds * -1);
			}
			catch { }

			return dt;
		}

		protected override void csvPlaceHeader()
		{
			base.csvPlaceHeader();

			string csvHeader = csvSeparator + "FR-Temp. (°C)";
			if (pressureEnabled > 0)
			{
				if (pref_inMeters)
				{
					if (pref_isDepth)
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
				csvHeader += csvSeparator + "Temp. (°C)";
			}

			csvHeader += csvSeparator + "location-lat" + csvSeparator + "location-lon" + csvSeparator + "height-msl"
				+ csvSeparator + "ground-speed" + csvSeparator + "satellites" + csvSeparator + "hdop" + csvSeparator + "signal-strength";
			if (adcLog) csvHeader += csvSeparator + "Sensor Raw";
			if (adcStop) csvHeader += csvSeparator + "Sensor State";
			//if (pref_battery) csvHeader += csvSeparator + "Battery (V)";
			csvHeader += csvSeparator + "Battery (V)";
			if (pref_metadata) csvHeader += csvSeparator + "Metadata";

			csvHeader += "\r\n";

			csv.Write(Encoding.ASCII.GetBytes(csvHeader));
		}

	}
}
