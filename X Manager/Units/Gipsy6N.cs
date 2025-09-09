
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using System.Globalization;
using System.ComponentModel;
using System.Text.Json;
using System.Net.Http;
using System.Net;
using System.IO.Compression;
using Microsoft.VisualBasic;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Text.RegularExpressions;




//using static X_Manager.Units.AxyTreks.AxyTrek;




#if X64
using FT_HANDLE = System.UInt64;
#else
using FT_HANDLE = System.UInt32;
#endif

namespace X_Manager.Units.Gipsy6
{
	class Gipsy6N : Gipsy6
	{

		const double SEC_PER_DAY = 86400;
		const double GM_GPS = 3.986005e14;  // earth's universal gravitational parameter m^3/s^2
		const double GM_GAL = 3.986004418e14;  // earth's universal gravitational parameter m^3/s^2
		const double GM_BDS = 3.986004418e14;  // earth's universal gravitational parameter m^3/s^2
		const double PI2 = 6.2831853071795864;
		const double WGS84_EARTH_ROTATION_RATE_GPS = 7.2921151467e-5;
		const double WGS84_EARTH_ROTATION_RATE_GAL = 7.2921150e-5;
		const double WGS84_EARTH_ROTATION_RATE_BDS = 7.2921151467e-5;
		const double alpha = -5.0 * Math.PI / 180.0;
		#region DEFCONF

		public static readonly byte[] defConf = new byte[600] {    0xCF, 0x00, 0x00, 0x02,
		// 4	Nome unità: 27 caratteri + terminatore 0
		0x4e, 0x6f, 0x20, 0x4e, 0x61, 0x6d, 0x65, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		// 32	ACQ ON (240s) - ACQ OFF (6s)
		0xf0, 0x00, 0x58, 0x02,
		// 36	Alt on e SAT condition
		0xb4, 0x00,
		0x81,   //flag sat condition + n satelliti
		0x10,   //gsv minima
		// 40	Start delay (minuti, 32 bit)
		0x00, 0x00, 0x00, 0x80,
		// 44	Start delay (date)
		0x02, 0x05, 0xe5, 0x87,
		// 48	ADC soglia + magmin, flag trigger, flag log
		0x02, 0x00, 0x00, 0x00,
		// 52	P1: Schedule A (10 minuti)
		0x0a, 0x01,
		// 54	P1: Schedule B (1 ora)
		0x01, 0x02,
		// 56	Charging current (1 byte) + 57	Debug Events On + 58 Secondi Enhanced Accuracy + 59 Potenza Prossimità (-20dBm default)
		0x64, 0x00, 0x0f, 0xec,
		// 60 - P1: Orari (0 = off, 1 = sch.A, 2 = sch.B)
		0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
		// 84	P2: Schedule C (3 minuti)
		0x03, 0x01,
		// 86	P2: Schedule D (12 minuti)
		0x0c, 0x01,
		// 88	P2: Disponibile
		0x00, 0x0c, 0x00, 0x00,
		// 92 - P2: Orari (0 = off, 1 = sch.C, 2 = sch.D)
		0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
		// 116 - P2: Mesi validità
		0x80, 0x00,
		//118 Soglie batteria: batteryRefuseDownload
		0x9a, 0x09,
		//120 Disponibili
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		// 128 - G1: Vertici
		0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		//288 - G1: Schedule E (15 minuto)
		0x0f, 0x01,
		//290 - G1: Schedule F (20 secondi)
		0x14, 0x00,
		// 292	G1: Disponibile
		0x00, 0x0c, 0x00, 0x00,
		// 296	G1: Orari
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		// 320 - G2: Vertici
		0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		//480 - G2: Schedule G (1 ora)
		0x01, 0x02,
		//482 - G2: Schedule H (5 minuti)
		0x05, 0x01,
		// 484	G2: Disponibile
		0x00, 0x00, 0x00, 0x00,
		// 488	G2: Orari
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		// 512	G1/G2 Enable + 2 byte padding
		0x00, 0x00, 0x00, 0x00,
		//516 Flag. orari bitwise (0xFF), 517-519 orari remoto, 520-522 orari proximity, 523 minuti intervallo proximity
		0xff, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x0f,
		//524-526 Primo indirizzo gruppo prossimità, 527-529 Ultimo indirizzo gruppo prossimità
		0x00, 0x00, 0x02, 0x00, 0x00, 0x03,
		//530-539 Disponibili
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		//540	Unità remota/!locale, Indirizzo remoto
		0x01, 0xFF, 0xFF, 0xFF,
		//544	Soglie batteria
		0x00, 0x0a,     //3.75V		batteryStartLogging
		0x00, 0x0a,     //3.75V		batteryPauseLogging
		0x66, 0x0a,     //3.90V		batteryRestartLogging
		0x33, 0x09,     //3.45V		batteryLowRfStart
		0x77, 0x09,	 	//3.55V		batteryLowRfLog
		//554
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
		//600
		};

		#endregion

		struct Satellite_JSON
		{
			public int constellation;
			public int sv;
			public int cNo;
			public double doppler;
			public double codePhase;
			public double[] positionFromEphemeris;
			public double[] veloicyFromEphemeris;
		}


		class Ephemeris_GPS
		{
			public DateTime timestamp;
			public int leapSecond;
			public int m_week;
			public Satellite_Ephemeris_GPS[] satellites;
			public Ephemeris_GPS()
			{
				satellites = new Satellite_Ephemeris_GPS[33];
			}
		}

		List<Ephemeris_GPS> Ephemerides_GPS = new List<Ephemeris_GPS>();
		class Satellite_Ephemeris_GPS
		{
			public int svId;
			public struct TimeInterval
			{
				public DateTime startTime_UTC;
				public double af0, af1, af2;
				public double crs, deltan, M0;
				public double cuc, ecc, cus, roota;
				public double toe, cic, Omega0, cis;
				public double i0, crc, omega, Omegadot;
				public double idot;
			}
			public List<TimeInterval> timeIntervals;
			public Satellite_Ephemeris_GPS()
			{
				timeIntervals = new List<TimeInterval>();
			}
		}

		class Ephemeris_GALILEO
		{
			public DateTime timestamp;
			public int leapSecond;
			public int m_week;
			public Satellite_Ephemeris_GALILEO[] satellites;

			public Ephemeris_GALILEO()
			{
				satellites = new Satellite_Ephemeris_GALILEO[37];
			}
		}

		List<Ephemeris_GALILEO> Ephemerides_GALILEO = new List<Ephemeris_GALILEO>();
		class Satellite_Ephemeris_GALILEO
		{
			public int svId;
			public struct TimeInterval
			{
				public DateTime startTime_UTC;
				public double af0, af1, af2;
				public double crs, deltan, M0;
				public double cuc, ecc, cus, roota;
				public double toe, cic, Omega0, cis;
				public double i0, crc, omega, Omegadot;
				public double idot;
			}
			public List<TimeInterval> timeIntervals;
			public Satellite_Ephemeris_GALILEO()
			{
				timeIntervals = new List<TimeInterval>();
			}
		}


		class Ephemeris_BEIDOU
		{
			public DateTime timestamp;
			public int leapSecond;
			public int m_week;
			public Satellite_Ephemeris_BEIDOU[] satellites;
			public Ephemeris_BEIDOU()
			{
				satellites = new Satellite_Ephemeris_BEIDOU[63];
			}
		}

		List<Ephemeris_BEIDOU> Ephemerides_BEIDOU = new List<Ephemeris_BEIDOU>();
		class Satellite_Ephemeris_BEIDOU
		{
			public int svId;
			public struct TimeInterval
			{
				public DateTime startTime_UTC;
				public double af0, af1, af2;
				public double crs, deltan, M0;
				public double cuc, ecc, cus, roota;
				public double toe, cic, Omega0, cis;
				public double i0, crc, omega, Omegadot;
				public double idot;
			}
			public List<TimeInterval> timeIntervals;
			public Satellite_Ephemeris_BEIDOU()
			{
				timeIntervals = new List<TimeInterval>();
			}
		}

		struct TTime
		{
			public int MJDN;
			public double SoD;
		}

		int rfAddress = -1;
		string lastKnownRfAddressString = "N.A.";

		FileType fileType;
		enum FileType : byte
		{
			FILE_GP6 = 0,
			FILE_BS6
		}
		enum eventType : byte
		{
			E_POWER_ON = 0,
			E_SD_START,
			E_SD_STOP,
			E_ACQ_ON,
			E_ACQ_OFF,
			E_SCHEDULE,
			E_ALTON_START,
			E_ALTON_TIMEOUT,
			E_BATTERY_LOW,
			E_MEM_FULL,
			E_POWER_OFF,
			E_RESET,
			E_REMOTE_CONNECTION
		}

		static readonly int[] accuracySteps = { 1, 2, 5, 10, 15, 50, 100 };

		static readonly string[] events = {
			"Power ON.",
			"STARTDELAY - Beginning",
			"STARTDELAY - End",
			"Start searching for satellites...",
			"No visible satellite. Going to sleep...",
			"GPS Schedule: {0} {1}",
			"Fix Acquisition Start",
			"Fix Timeout (Checkpoint #{0})",
			"Low Battery.",
			"Memory Full.",
			"Power OFF",
			"Reset",
			"Remote Connection."
		};

		static readonly string[] scheduleEventTimings = {
			"second(s)",
			"minute(s)",
			"hour(s)",
		};

		static readonly DateTime gps_time = DateTime.SpecifyKind(new DateTime(1980, 1, 6, 0, 0, 0), DateTimeKind.Utc);   //Data inizio GPS in formato UTC

		//bool repeatEmptyValues = false;
		//public bool remoteConnection = false;

		//private static long lastTimestamp = 0;
		//private static long conversionDone = 0;

		new byte[] firmwareArray = new byte[3];

		public Gipsy6N(object p)
			: base(p)
		{
			modelCode = model_Gipsy6N;
			configureMovementButtonEnabled = true;
			configurePositionButtonEnabled = false;
			defaultArdExtension = "gp6";
		}

		public override string askName()
		{
			name = "";
			int retryMax = 1;
			byte[] nameAr = new byte[28];
			if (remoteConnection) retryMax = RETRY_MAX;
			for (int retry = 0; retry < retryMax; retry++)
			{
				if (!ask("N"))
				{
					throw new Exception(unitNotReady);
				}
				try
				{
					//byte nIn = 255;
					for (int i = 0; i < 28; i++)
					{
						nameAr[i] = (byte)ft.ReadByte();
					}
					for (int i = 0; i < 28; i++)
					{
						if (nameAr[i] != 0)
						{
							name += Convert.ToChar(nameAr[i]).ToString();
						}
						else
						{
							if (i == 0)
							{
								name = "[name empty]";
							}
							break;
						}
						//nIn = ft.ReadByte();
						//if (nIn != 0)
						//{
						//	name += Convert.ToChar(nIn).ToString();
						//}
						//else
						//{
						//	Thread.Sleep(100);
						//	int p = ft.ReadExisting();
						//	break;
						//}
					}
					break;
				}
				catch
				{
					if (retry == retryMax - 1) throw new Exception(unitNotReady);
				}
			}
			if (name == "") name = "[No Name]";
			unitName = name;
			return name;
		}

		public override void askBattery()
		{
			double bl = 0;
			int retryMax = 1;
			if (remoteConnection) retryMax = RETRY_MAX;
			for (int retry = 0; retry < retryMax; retry++)
			{
				if (!ask("B"))
				{
					throw new Exception(unitNotReady);
				}
				try
				{
					if (!remoteConnection) ft.ReadTimeout = 500;
					bl = ft.ReadByte(); bl *= 256;
					bl += ft.ReadByte();
					bl *= 6;
					batteryLevel = bl / 4096;
					break;
				}
				catch
				{
					if (retry == retryMax - 1) throw new Exception(unitNotReady);
				}
			}
		}

		public override void setPcTime()
		{
			byte[] dateAr = new byte[6];

			if (remoteConnection)
			{
				ft.ReadTimeout = 2200;
				byte[] command = new byte[] { 84, 84, 84, 84, 84, 84, 84, 71, 71, 65, 0x74, 0, 0, 0, 0, 0, 0 };
				bool ok = false;
				for (int retry = 0; retry < RETRY_MAX; retry++)
				{
					var dateToSend = DateTime.UtcNow;
					dateAr[0] = (byte)dateToSend.Second;
					dateAr[1] = (byte)dateToSend.Minute;
					dateAr[2] = (byte)dateToSend.Hour;
					dateAr[3] = (byte)dateToSend.Day;
					dateAr[4] = (byte)dateToSend.Month;
					dateAr[5] = (byte)(dateToSend.Year - 2000);
					Array.Copy(dateAr, 0, command, 11, dateAr.Length);
					ft.Write(command, 17);
					try
					{
						ft.ReadByte();
						ok = true;
						break;
					}
					catch { }
				}
				if (!ok)
				{
					throw new Exception(unitNotReady);
				}
			}
			else
			{
				if (!ask("t"))
				{
					throw new Exception(unitNotReady);
				}
				try
				{
					var dateToSend = DateTime.UtcNow;
					dateAr[0] = (byte)dateToSend.Second;
					dateAr[1] = (byte)dateToSend.Minute;
					dateAr[2] = (byte)dateToSend.Hour;
					dateAr[3] = (byte)dateToSend.Day;
					dateAr[4] = (byte)dateToSend.Month;
					dateAr[5] = (byte)(dateToSend.Year - 2000);
					Thread.Sleep(10);
					ft.Write(dateAr, 6);
					ft.ReadByte();
				}
				catch
				{
					throw new Exception(unitNotReady);
				}
			}

		}

		public override uint[] askMaxMemory()
		{
			UInt32 m;
			int retryMax = 1;
			if (remoteConnection) retryMax = RETRY_MAX;
			for (int retry = 0; retry < retryMax; retry++)
			{
				if (!ask("m"))
				{
					throw new Exception(unitNotReady);
				}
				try
				{
					m = ft.ReadByte(); m *= 256;
					m += ft.ReadByte(); m *= 256;
					m += ft.ReadByte(); m *= 256;
					m += ft.ReadByte();
					mem_min_physical_address = m;
					m = ft.ReadByte(); m *= 256;
					m += ft.ReadByte(); m *= 256;
					m += ft.ReadByte(); m *= 256;
					m += ft.ReadByte();
					mem_max_physical_address = m;
					break;
				}
				catch
				{
					if (retry == retryMax - 1) throw new Exception(unitNotReady);
				}
			}
			return new uint[] { mem_min_physical_address, mem_max_physical_address };
		}

		public override uint[] askMemory()
		{
			int retryMax = 1;
			if (remoteConnection) retryMax = RETRY_MAX;
			for (int retry = 0; retry < retryMax; retry++)
			{
				if (!ask("M"))
				{
					throw new Exception(unitNotReady);
				}
				try
				{
					mem_address = ft.ReadByte(); mem_address <<= 8;
					mem_address += ft.ReadByte(); mem_address <<= 8;
					mem_address += ft.ReadByte(); mem_address <<= 8;
					mem_address += ft.ReadByte();
					mem_max_logical_address = ft.ReadByte(); mem_max_logical_address <<= 8;
					mem_max_logical_address += ft.ReadByte(); mem_max_logical_address <<= 8;
					mem_max_logical_address += ft.ReadByte(); mem_max_logical_address <<= 8;
					mem_max_logical_address += ft.ReadByte();
					break;
				}
				catch
				{
					if (retry == retryMax - 1) throw new Exception(unitNotReady);
				}
			}
			return new uint[] { mem_max_logical_address, mem_address };
		}

		public override void eraseMemory()
		{
			int retryMax = 1;
			if (remoteConnection) retryMax = RETRY_MAX;
			for (int retry = 0; retry < retryMax; retry++)
			{
				if (!ask("E"))
				{
					throw new Exception(unitNotReady);
				}
				try
				{
					if (!remoteConnection) ft.ReadTimeout = 500;
					ft.ReadByte();
					break;
				}
				catch
				{
					if (retry == retryMax - 1) throw new Exception(unitNotReady);
				}
			}
		}

		public override void setName(string newName)
		{

			byte[] nameShort = Encoding.ASCII.GetBytes(newName);

			if (remoteConnection)
			{
				byte[] name = new byte[28 + 11];
				Array.Copy(nameShort, 0, name, 11, nameShort.Length);
				name[0] = (byte)'T';
				name[1] = (byte)'T';
				name[2] = (byte)'T';
				name[3] = (byte)'T';
				name[4] = (byte)'T';
				name[5] = (byte)'T';
				name[6] = (byte)'T';
				name[7] = (byte)'G';
				name[8] = (byte)'G';
				name[9] = (byte)'A';
				name[10] = (byte)'n';

				ft.ReadTimeout = 2200;
				bool ok = false;
				for (int retry = 0; retry < RETRY_MAX; retry++)
				{

					ft.Write(name, 0, 28 + 11);
					uint res = 0;
					try
					{
						res = ft.ReadByte();
						ok = true;
						break;
					}
					catch { }
				}
				if (!ok) throw new Exception(unitNotReady);
			}
			else
			{
				byte[] name = new byte[28];
				Array.Copy(nameShort, 0, name, 0, nameShort.Length);

				for (int k = 0; k < 4; k++)
				{
					if (!ask("n"))
					{
						throw new Exception(unitNotReady);
					}
					Thread.Sleep(10);
					ft.ReadTimeout = 200;
					ft.Write(name, 28);
					uint res = 0;
					try
					{
						res = ft.ReadByte();
					}
					catch
					{
						throw new Exception(unitNotReady);
					}
					if (res == "I".ToArray()[0])
					{
						break;
					}
					Thread.Sleep(100);
				}
			}
		}

		public override bool getRemote()
		{
			remote = false;
			int retryMax = 1;
			if (remoteConnection) retryMax = RETRY_MAX;
			for (int retry = 0; retry < retryMax; retry++)
			{
				if (!ask("l"))
				{
					throw new Exception(unitNotReady);
				}

				try
				{
					if (ft.ReadByte() == 1) remote = true;
					break;
				}
				catch
				{
					if (retry == retryMax - 1) throw new Exception(unitNotReady);
				}
			}
			return remote;
		}

		public override byte[] getConf()
		{
			if (remoteConnection)
			{
				return getConfRemote();
			}
			else
			{
				return getConfCable();
			}
		}

		private byte[] getConfRemote()
		{
			ft.ReadTimeout = 2500;
			byte[] conf = new byte[0x1000];
			byte[] command = new byte[] { 84, 84, 84, 84, 84, 84, 84, 71, 71, 65, 67, 255 };    //Crea il comando TTTTTTTGGAC + parametro 0xff = richiesta dimensione configurazione
			int size = 0;
			int sizeOk = 522;

			for (int retry = 0; retry < RETRY_MAX; retry++)
			{
				Debug.WriteLine("get-size-" + retry.ToString());
				ft.ReadExisting();
				ft.Write(command, 0, 12);
				try
				{
					size = ft.ReadByte();       //Riceve e controlla la dimensione del buffer di configurazione
					size <<= 8;
					size += ft.ReadByte();
					if (size != sizeOk)        //Questo poi andrà sistemato perché il software non sa a priori la dimensione del bufffer 
					{                       //di configurazione
						Debug.WriteLine("get-size=" + size.ToString() + " WRONG SIZE!");
						Thread.Sleep(500);
						retry = 0;
						continue;
					}
					break;
				}
				catch
				{
					if (retry == 3) throw new Exception(unitNotReady);
				}
			}

			Debug.WriteLine("get-size=" + size.ToString());

			//La configurazione viene inviata in pacchetti di max 64 byte
			byte nPack = (byte)(size / 64);         //Calcola il numero di pacchetti interi da 64 byte
			byte rPack = (byte)(size % 64);         //Calcola la dimensione dell'eventuale ultimo pacchetto < 64 byte

			for (byte i = 0; i < nPack; i++)    //Cicla per il numero di pacchetti INTERI da ricevere
			{
				command[11] = i;            //Il parametro del comando è il numero di pacchetto che si vuole ricevere
				if (firmTotA >= 2002000) command[10] = 72;      //In caso di firmware >= 2.2.0 manda il comando 'H' invece che 'C'. Con il firmware 2.2.0 ogni pacchetto viene inviato con un byte
																//aggiuntivo che indica il numero di pacchetto inviato; così si può controllare se il pacchetto ricevuto è effettivamente
																//quello richiesto																
				for (int retry = 0; retry < RETRY_MAX; retry++)
				{
					Debug.WriteLine("get-packet" + i.ToString() + "-" + retry.ToString());
					try
					{
						ft.ReadExisting();              //Svuota eventuali byte rimasti nel buffer seriale
						ft.Write(command, 0, 12);       //Invia il comando
						if (firmTotA <= 2001004)
						{
							ft.Read(conf, ((uint)i * 64) + 32, 64);     //In caso di firmware precedenti si ricevono solo 64 byte. In caso di mancata ricezione si va nel catch
							break;
						}
						else
						{
							byte[] rxBuff = new byte[65];       //In caso di firmware nuovo, si ricevono 65 byte in un buffer temporaneo (64 dati + 1 relativo al numero di pacchetto)
							ft.Read(rxBuff, 0, 65);
							if (rxBuff[64] == i)
							{
								Array.Copy(rxBuff, 0, conf, ((uint)i * 64) + 32, 64);
								break;
							}
							else
							{
								if (retry == 3) throw new Exception(unitNotReady);
							}
						}
					}
					catch
					{
						if (retry == 3) throw new Exception(unitNotReady);
					}
				}
			}

			command[11] = nPack;
			for (int retry = 0; retry < RETRY_MAX; retry++)
			{
				Debug.WriteLine("get-packet" + nPack.ToString() + "-" + retry.ToString());
				try
				{
					ft.ReadExisting();
					ft.Write(command, 0, 12);
					if (firmTotA < 2002000)
					{
						int read = ft.Read(conf, ((uint)nPack * 64) + 32, rPack);
						break;
					}
					else
					{
						byte[] rxBuff = new byte[rPack + 1];
						ft.Read(rxBuff, 0, (uint)(rPack + 1));
						if (rxBuff[rPack] == nPack)
						{
							Array.Copy(rxBuff, 0, conf, ((uint)nPack * 64) + 32, rPack);
							break;
						}
						else
						{
							if (retry == 3) throw new Exception(unitNotReady);
						}
					}
				}
				catch
				{
					if (retry == 3) throw new Exception(unitNotReady);
				}
			}

			//Se firmware >0 2.2.0, si manda TTTTTGGAH + 0xfe e si riceve indietro CRC solo della configurazione (no nome). Si calcola anche in locale e se non coincide si genera eccezione
			if (firmTotA >= 2002000)
			{
				try
				{
					byte[] crcGipsy = new byte[2];
					byte[] confl = new byte[size];
					Array.Copy(conf, 32, confl, 0, size);

					command[10] = 72;       //Già dovrebbe essere così
					command[11] = 0xfe;     //Parametro di richiesta crc
					ft.ReadExisting();
					ft.Write(command, 0, 12);
					ft.Read(crcGipsy, 2);
					if (!crcGipsy.SequenceEqual(Gipsy6.CRCcalc(confl))) throw new Exception(unitNotReady);
				}
				catch
				{
					throw new Exception(unitNotReady);
				}
			}

			ft.ReadExisting();

			return conf;
		}

		private byte[] getConfCable()
		{
			byte[] conf = new byte[0x1000];
			if (!ask("C"))
			{
				throw new Exception(unitNotReady);
			}
			try
			{
				//ACQ, Start delay e GSV			
				for (int i = 32; i < 32 + 20; i++)
				{
					conf[i] = ft.ReadByte();
				}
				//Schedule A e B
				for (int i = 52; i < 52 + 32; i++)
				{
					conf[i] = ft.ReadByte();
				}
				ft.Write(new byte[] { 1 }, 1);//***********************************************SYNC

				//Schedule C e D + mesi + primo quadrato geofencing 1
				for (int i = 84; i < 84 + 60; i++)
				{
					conf[i] = ft.ReadByte();
				}
				ft.Write(new byte[] { 2 }, 1);//***********************************************SYNC

				//Quadrati 2-5 geofencing-1
				for (int i = 144; i < 144 + 64; i++)
				{
					conf[i] = ft.ReadByte();
				}
				ft.Write(new byte[] { 3 }, 1);//***********************************************SYNC

				//Quadrati 6-9 geofencing-1
				for (int i = 208; i < 208 + 64; i++)
				{
					conf[i] = ft.ReadByte();
				}
				ft.Write(new byte[] { 4 }, 1);//***********************************************SYNC

				//Quadrato 10 geofencing-1 + Schedule E/F + Orari Geofencing 1 + Primo quadrato geofencing-2
				for (int i = 272; i < 272 + 64; i++)
				{
					conf[i] = ft.ReadByte();
				}
				ft.Write(new byte[] { 5 }, 1);//***********************************************SYNC

				//Quadrati 2-5 geofencing-2
				for (int i = 336; i < 336 + 64; i++)
				{
					conf[i] = ft.ReadByte();
				}
				ft.Write(new byte[] { 6 }, 1);//***********************************************SYNC

				//Quadrati 6-9 geofencing-2
				for (int i = 400; i < 400 + 64; i++)
				{
					conf[i] = ft.ReadByte();
				}
				ft.Write(new byte[] { 7 }, 1);//***********************************************SYNC

				//Ultimo quadrato geofencing 2 + Schedule G/H + orari Geofencing 2 + Enable Geofencing 1 e 2 + schedule remoto + unità locale/remota + indirizzo remoto
				for (int i = 464; i < 464 + 50; i++)
				{
					conf[i] = ft.ReadByte();
				}
				if (firmTotA > 1)
				{
					for (int i = 514; i < 514 + 14; i++)
					{
						conf[i] = ft.ReadByte();
					}
					ft.Write(new byte[] { 8 }, 1);//***********************************************SYNC
					int end = 12;
					if (firmTotA >= 1000000)
					{
						end = 26;
					}
					for (int i = 528; i < 528 + end; i++)
					{
						conf[i] = ft.ReadByte();
					}
				}
				ft.Write(new byte[] { 9 }, 1);//***********************************************SYNC
			}
			catch
			{
				Thread.Sleep(100);
				throw new Exception(unitNotReady);
			}

			return conf;
		}

		public override void setConf(byte[] conf)
		{
			if (remoteConnection)
			{
				setConfRemote(conf);
			}
			else
			{
				setConfCable(conf);
			}

		}

		private void setConfCable(byte[] conf)
		{
			for (int j = 0; j < 3; j++)
			{
				if (!ask("c"))
				{
					throw new Exception(unitNotReady);
				}
				ft.ReadTimeout = 400;
				uint packetLength = 28;
				uint packetNumber = 17;
				if (firmTotA < 1003001)
				{
					packetLength = 64;
					packetNumber = 7;
				}
				try
				{
					ft.ReadByte();
					for (uint i = 0; i < packetNumber; i++)
					{
						ft.Write(conf, (i * packetLength) + 32, packetLength);
						ft.ReadByte();
					}

					ft.Write(conf, 480, 20);
					if (firmTotA > 1003000)
					{
						ft.ReadByte();
					}

					ft.Write(conf, 500, 16);
					ft.ReadByte();

					if (firmTotA > 1)
					{
						ft.Write(conf, 516, 24);
					}
					if (firmTotA >= 1000000)
					{
						Thread.Sleep(5);
						ft.Write(conf, 540, 14);
					}
					ft.ReadByte();

					break;
				}
				catch
				{
					if (j == 2)
					{
						throw new Exception(unitNotReady);
					}
					else
					{
						Thread.Sleep(100);
					}
				}
			}
		}

		private void setConfRemote(byte[] conf)
		{
			int size = 0;
			ft.ReadTimeout = 2200;

			//Prima viene chiesta la dimensione della configurazione con il classico comando 'C'
			for (int retry = 0; retry < RETRY_MAX; retry++)
			{
				Debug.WriteLine("set-getSize-" + retry.ToString());
				try
				{
					ft.ReadExisting();
					//  0   1   2   3   4   5   6   7   8   9   10  11
					//  T   T   T   T   T   T   T   G   G   A   C   0xff	
					ft.Write(new byte[] { 84, 84, 84, 84, 84, 84, 84, 71, 71, 65, 67, 255 }, 0, 12);
					size = ft.ReadByte();
					size <<= 8;
					size += ft.ReadByte();
					if (size != 522)    //Questo poi andrà sistemato perché il software non sa a priori la dimensione del bufffer 
					{                   //di configurazione
						Debug.WriteLine("set-getSize=" + size.ToString() + " WRONG SIZE!");
						Thread.Sleep(500);
						retry = 0;
						continue;
					}
					break;
				}
				catch
				{
					if (retry == 3) throw new Exception(unitNotReady);
				}
			}

			Debug.WriteLine("set-getSize=" + size.ToString());

			//Si calcola il numero di pacchetti interi da 64 e la dimensione dell'ultimo parziale
			Thread.Sleep(1);
			byte nPack = (byte)(size / 64);
			byte rPack = (byte)(size % 64);

			byte[] command = new byte[76];
			for (int i = 0; i < 7; i++)
			{
				command[i] = 84;
			}
			command[7] = 71;
			command[8] = 71;
			command[9] = 65;
			command[10] = 99;                               //Si modifica il comando da 'C' a 'c'
			if (firmTotA >= 2002000) command[10] = 104;     //In caso di firmware >= 2.2.0 il comando diventa 'h', che include anche il crc check alla fine

			for (byte i = 0; i < nPack; i++)
			{
				command[11] = i;
				Array.Copy(conf, ((uint)i * 64) + 32, command, 12, 64);
				for (int retry = 0; retry < RETRY_MAX; retry++)
				{
					Debug.WriteLine("set-packet" + i.ToString() + "-" + retry.ToString());
					try
					{
						ft.ReadExisting();
						ft.Write(command, 0, 76);
						ft.ReadByte();
						Thread.Sleep(1);
						break;
					}
					catch
					{
						if (retry == 3) throw new Exception(unitNotReady);
					}
				}
			}
			command[11] = nPack;
			Array.Copy(conf, ((uint)nPack * 64) + 32, command, 12, rPack);
			for (int retry = 0; retry < RETRY_MAX; retry++)
			{
				Debug.WriteLine("set-packet" + nPack.ToString() + "-" + retry.ToString());
				try
				{
					ft.ReadExisting();
					ft.Write(command, 0, (uint)12 + rPack);
					ft.ReadByte();
					break;
				}
				catch
				{
					if (retry == 3) throw new Exception(unitNotReady);
				}
			}

			//In caso di firmware >= 2.2.0 calcolo il crc e lo invio col comando TTTTTTTGGAh + parametro 0xfe + CRC. Ricevo indietro un byte, se vale 1 è ok, se vale 0 non è arrivata
			//correttamente e si genera l'eccezione
			if (firmTotA >= 2002000)
			{
				byte[] confl = new byte[size];
				Array.Copy(conf, 32, confl, 0, size);

				byte[] locCrc = Gipsy6.CRCcalc(confl);
				command[11] = 0xfe;                             //Parametro 0xfe: invio CRC calcolato in lcoale
				Array.Copy(locCrc, 0, command, 12, 2);          //Due byte di CRC
				try
				{
					ft.ReadExisting();
					ft.Write(command, 0, 14);       //Invia il comando
					byte res = ft.ReadByte();       //Riceve un  byte di risposta: 1 = crc ok, 0 = crc ko
					if (res == 0) throw new Exception(unitNotReady);
				}
				catch
				{
					throw new Exception(unitNotReady);
				}

			}

		}

		public override void disconnect()
		{
			byte status = 0;
			connected = false;
			ask("O");
			try
			{
				if (remoteConnection)
				{
					ft.ReadTimeout = 2200;
					if (firmTotA < 2001000)
					{
						ft.ReadByte();
					}
				}

				if (firmTotA >= 2001000)
				{
					if (!remoteConnection) ft.ReadTimeout = 600;
					status = ft.ReadByte();
					string warningMessage = "";
					if ((status & 1) == 1)
					{
						warningMessage += "Warning: Low battery level.";
					}
					if ((status & 2) == 2)
					{
						warningMessage += "\r\nWarning: Memory full.";
					}
					if (status > 0)
					{
						var w = new Warning(warningMessage);
						w.ShowDialog();
					}
				}
			}
			catch
			{ }

			//if (remote)
			//{
			//	try
			//	{
			//		if (remoteConnection)
			//		{
			//			ft.ReadTimeout = 2200;
			//			ft.ReadByte();
			//		}
			//		if (readStatus) status = ft.ReadByte();
			//	}
			//	catch
			//	{
			//		return;
			//	}
			//}
			//else
			//{
			//	if (readStatus) status = ft.ReadByte();
			//}
			//if (readStatus)
			//{
			//	string warningMessage = "";
			//	if ((status & 1) == 1)
			//	{
			//		warningMessage += "Warning: Low battery level.";
			//	}
			//	if ((status & 2) == 2)
			//	{
			//		warningMessage += "\r\nWarning: Memory full.";
			//	}
			//	if (status > 0)
			//	{
			//		var w = new Warning(warningMessage);
			//		w.ShowDialog();
			//	}
			//}
		}

		public override void shutDown()
		{
			base.shutDown();
			ask("S");
		}

		public unsafe override void download(string fileName, uint fromMemory, uint toMemory, int baudrate)
		{

			byte[] outBuffer = new byte[50];
			byte[] inBuffer;
			byte[] tempBuffer = new byte[2048];
			byte[] address = new byte[8];

			ft.ReadTimeout = 1000;

			uint buffSize;
			if (mem_address > mem_max_logical_address)
			{
				buffSize = mem_address - mem_max_logical_address;
			}
			else
			{
				buffSize = (mem_max_physical_address - mem_max_logical_address) + (mem_address - mem_min_physical_address);
			}

			if ((buffSize & 0x1ff) != 0)
			{
				buffSize &= 0xfffffe00;
				buffSize += 0x200;
			}

			convertStop = false;
			inBuffer = new byte[buffSize + 2];
			uint buffPointer = 0;

			string fileNameMdp = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + ".gp6";

			if (!ask("D"))
			{
				throw new Exception(unitNotReady);
			}

			ft.ReadTimeout = 1600;

			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.progressBarStopButton.IsEnabled = true));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.progressBarStopButtonColumn.Width = new GridLength(80)));

			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.IsIndeterminate = false));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Minimum = 0));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Maximum = buffSize));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Value = buffPointer));

			address = BitConverter.GetBytes(mem_max_logical_address);
			//Array.Reverse(address);
			Array.Copy(address, 1, outBuffer, 1, 3);
			outBuffer[0] = 0x65;        //load address
			ft.Write(outBuffer, 4);
			try
			{
				ft.ReadByte();
			}
			catch
			{
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFailed()));
				return;
			}

			bool ok = true;

			while (buffPointer < buffSize)
			{

				if (convertStop)
				{
					break;
				}

				ft.Write(new byte[] { 66 }, 1);
				if (ft.Read(inBuffer, buffPointer, 0x200) < 0x200)
				{
					ok = false;
					if (buffPointer != 0)
					{
						var foC = new BinaryWriter(File.Open(fileNameMdp, FileMode.Create));
						foC.Write(inBuffer, 0, inBuffer.Length);
						foC.Close();
					}
					Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFailed()));
					break;
				}
				buffPointer += 0x200;
				if (MainWindow.keepAliveTimer != null)
				{
					MainWindow.keepAliveTimer.Stop();
					MainWindow.keepAliveTimer.Start();
				}
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Value = buffPointer));

			}

			inBuffer[inBuffer.Length - 2] = modelCode;
			inBuffer[inBuffer.Length - 1] = 0x00;

			var fo = new BinaryWriter(File.Open(fileNameMdp, FileMode.Create));

			fo.Write(inBuffer, 0, inBuffer.Length);
			fo.Close();

			if (ok)
			{
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFinished()));
			}
		}

		public unsafe override void downloadRemote(string fileName, uint fromMemory, uint toMemory, int baudrate)
		{

			//Globale mem_address è fino a dove bisogna scaricare
			//Globale mem_max_logical_address è l'indirizzo da cui iniziare a scaricare

			byte[] inBuffer;

			bool resume = false;
			if (fileName.Contains("incomplete")) resume = true;

			ft.ReadTimeout = 2200;
			MainWindow.keepAliveTimer.Stop();
			FileStream fs = null;

			uint maxLogical = mem_max_logical_address;
			if (resume)
			{
				fs = File.OpenRead(fileName);
				byte[] addArr = new byte[4];
				try
				{
					fs.Position = fs.Length - 10;
					fs.Read(addArr, 0, 4);
				}
				catch { }

				maxLogical = BitConverter.ToUInt32(addArr, 0);
			}

			uint buffSize;
			if (mem_address > mem_max_logical_address)
			{
				buffSize = mem_address - mem_max_logical_address;
			}
			else
			{
				buffSize = (mem_max_physical_address - mem_max_logical_address) + (mem_address - mem_min_physical_address);
			}

			if ((buffSize & 0x1ff) != 0)
			{
				buffSize &= 0xfffffe00;
				buffSize += 0x200;
			}
			uint buffPointer = 0;
			inBuffer = new byte[buffSize];
			if (resume)
			{
				fs.Position = 0;
				fs.Read(inBuffer, 0, (int)(fs.Length - 10));
				buffPointer = (uint)(fs.Length - 10);
				fs.Close();
			}

			convertStop = false;
			uint address = maxLogical;

			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.progressBarStopButton.IsEnabled = true));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.progressBarStopButtonColumn.Width = new GridLength(80)));

			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.IsIndeterminate = false));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Minimum = 0));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Maximum = buffSize));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Value = buffPointer));

			bool sendCommand = true;

			byte[] command = new byte[] {   (byte)'T', (byte)'T', (byte)'T', (byte)'T', (byte)'T', (byte)'T', (byte)'T', (byte)'G',
											(byte)'G', (byte)'A', (byte)'D' };

			byte[] outBuffer = new byte[5];
			int res = 0;

			byte[] ack = new byte[1];
			//byte seqNumber = 0;
			outBuffer[0] = (byte)'T';
			while (buffPointer < buffSize)
			{
				if (convertStop)
				{
					break;
				}
				if (sendCommand)
				{
					Debug.WriteLine("command-D address:" + address.ToString("X8"));
					ft.ReadExisting();
					ft.Write(command, 0, 11);
					res = ft.Read(ack, 0, 1);
					if (res != 1)
					{
						continue;
					}
					sendCommand = false;
					//address &= 0xfffffe00;
					//buffPointer &= 0xfffffe00;
				}
				if ((address % 0x200) == 0)
				{
					Debug.WriteLine("dl-A-" + address.ToString("X8"));
					outBuffer[1] = (byte)'A';
					outBuffer[2] = (byte)(address >> 24);
					outBuffer[3] = (byte)(address >> 16);
					outBuffer[4] = (byte)(address >> 8);

					ft.Write(outBuffer, 0, 5);
					res = ft.Read(ack, 0, 1);
					if (res != 1)
					{
						sendCommand = true;
						continue;
					}
					outBuffer[1] = (byte)'P';
					Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Value = buffPointer));
				}
				outBuffer[2] = (byte)(address % 0x200 / 0x40);
				Debug.WriteLine("dl-P-" + outBuffer[2].ToString());
				ft.Write(outBuffer, 0, 3);
				res = ft.Read(inBuffer, buffPointer, 0x40);
				if (res < 0x40)
				{
					ft.ReadExisting();
					sendCommand = true;
					continue;
				}
				else
				{
					//seqNumber++;
					//outBuffer[2] = seqNumber;
					buffPointer += 0x40;
					address += 0x40;
					if (address == mem_max_physical_address)
					{
						address = mem_min_physical_address;
					}
				}
			}
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Value = buffPointer));
			command[1] = (byte)'x';
			ft.Write(command, 0, 3);
			ft.Read(command, 0, 1);
			Thread.Sleep(100);

			if (buffPointer > 0)
			{
				try
				{
					string fileNameMdp = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName);
					if (File.Exists(fileNameMdp + ".gp6")) File.Delete(fileNameMdp + ".gp6");
					if (resume)
					{
						fileNameMdp = fileNameMdp.Remove(fileNameMdp.IndexOf("_incomplete"), 11);
					}
					if (buffPointer < buffSize)
					{
						Array.Resize(ref inBuffer, ((int)buffPointer / 0x200) * 0x200);
						address = (address & 0xfffffe00);
						fileNameMdp += "_incomplete";
					}
					fileNameMdp += ".gp6";
					Array.Resize(ref inBuffer, inBuffer.Length + 10);
					Array.Copy(BitConverter.GetBytes(address), 0, inBuffer, inBuffer.Length - 10, 4);
					Array.Copy(BitConverter.GetBytes(mem_address), 0, inBuffer, inBuffer.Length - 6, 4);
					inBuffer[inBuffer.Length - 2] = modelCode;
					inBuffer[inBuffer.Length - 1] = 0x00;
					var fo = new BinaryWriter(File.Open(fileNameMdp, FileMode.Create));

					fo.Write(inBuffer, 0, inBuffer.Length);
					fo.Close();
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message);
					Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFailed()));
					return;
				}

			}


			if (buffPointer > 0)
			{
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFinished()));
			}
			try
			{
				MainWindow.keepAliveTimer.Start();
			}
			catch { }

		}

		public override void abortConf() { }

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
							File.Delete(fileNameArd);
						}
						else
						{
							do
							{
								fileNameArd = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileNameArd) + " (1)" + ".ard";
							} while (File.Exists(fileNameArd));
						}
					}   //Richiesta overwrite se file già esistente

					ard = new BinaryWriter(File.Open(fileNameArd, FileMode.Create));
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
				//if (Parent.getParameter("keepMdp").Equals("false"))
				if (!Properties.Settings.Default.INI_KEEP_MDP)
				{
					File.Delete(fileNameMdp);
				}
				else
				{
					if (!Path.GetExtension(fileNameMdp).Contains("Dump"))
					{
						string newFileNameMdp = Path.GetDirectoryName(fileNameMdp) + "\\" + Path.GetFileNameWithoutExtension(fileNameMdp) + ".memDump";
						if (File.Exists(newFileNameMdp)) File.Delete(newFileNameMdp);
						//string newFileNameMdp = Path.GetFileNameWithoutExtension(fileNameMdp) + ".memDump";
						File.Move(fileNameMdp, newFileNameMdp);
					}
				}
			}
			catch { }
			if (!fromDownload) Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.nextFile(false)));
		}

		public override void convert(string fileName)
		{
			base.convert(fileName);

			//Stabilisce se è un download diretto o da basestation
			if (Path.GetExtension(fileName).IndexOf("bs6", StringComparison.InvariantCultureIgnoreCase) != -1)
			{
				fileType = FileType.FILE_BS6;       //Basestation
			}
			else
			{
				fileType = FileType.FILE_GP6;       //Diretto
			}

			//Attribuisce un nome all'unità
			if (unitName == "")
			{
				string newName = Path.GetFileNameWithoutExtension(fileName);
				if (newName.Contains("_p"))
				{
					newName = newName.Remove(newName.IndexOf("_p"), 2);
				}
				unitName = newName;

				if ((fileType == FileType.FILE_BS6) && (pref_debugLevel == 0))
				{
					if (unitName.Length > 8)
					{
						unitName = unitName.Substring(0, 8);
					}
				}
				lastKnownUnitName = unitName;

				//Triplica la progress bar
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
				{
					parent.statusProgressBar.Height = 6;
					parent.txtProgressBar.Height = 6;
					parent.kmlProgressBar.Height = 6;
					parent.statusProgressBar.Margin = new Thickness(10, 5, 10, 0);
					parent.txtProgressBar.Margin = new Thickness(10, 0, 10, 0);
					parent.kmlProgressBar.Margin = new Thickness(10, 0, 10, 0);
				}));

				//Reinizializza le variabili statiche (in caso di più file da convertire)
				//conversionDone = 0;
				//lastTimestamp = 0;

				//Carica il file gp6 in memoria
				BinaryReader gp6File = new BinaryReader(new FileStream(fileName, FileMode.Open));
				int filePointer = 0;
				byte[] gp6 = null;
				int headerLength = 0;
				if (fileType == FileType.FILE_BS6)
				{
					headerLength = 0x600;
				}

				long buffLen = gp6File.BaseStream.Length;
				buffLen -= 2;               //Toglie gli ultimi due byte col tipo di unità dal calcolo
				buffLen -= headerLength;    //Toglie l'eventuale header
				long partH = buffLen / 0x200;
				if (buffLen % 0x200 > 0)
				{
					partH += 1;
				}
				buffLen -= partH * 2;
				gp6 = new byte[buffLen];

				if (fileType == FileType.FILE_BS6)  //Compone il nome dell'unità e l'eventuale schedule
				{
					//gp6File.BaseStream.Position = 0x16;
					//string add = Encoding.ASCII.GetString(gp6File.ReadBytes(28));
					//add = add.Trim(Path.GetInvalidFileNameChars());
					//int stringPos = add.Length - 1;
					//while (add[add.Length - 1] == ' ')
					//{
					//	add = add.Substring(0, add.Length - 1);
					//}
					//string addDebug = Path.GetFileName(fileName).Substring(8, 16);
					//fileName = Path.GetDirectoryName(fileName) + "\\" + add;
					//if (pref_debugLevel > 0)
					//{
					//	fileName += addDebug;
					//}

					string add = Path.GetFileNameWithoutExtension(fileName);
					if (add.Length > 8) add = add.Substring(0, 8);
					while ((add[0] == '0') && add.Length > 1)
					{
						add = add.Substring(1, add.Length - 1);
					}
					fileName = Path.GetDirectoryName(fileName) + "\\" + add;

					gp6File.BaseStream.Position = 0x15;
					byte newConfP = gp6File.ReadByte();
					if (newConfP != 0)
					{
						byte[] config = new byte[616];
						Array.Copy(Encoding.ASCII.GetBytes("--gipsy6Config--"), config, 16);
						gp6File.BaseStream.Position = 0x32;
						gp6File.Read(config, 0x10, 600);
						int year = config[88 + 0x10] + 2000;
						int month = config[89 + 0x10];
						int day = config[90 + 0x10];
						int hour = config[91 + 0x10];
						DateTime confDate = DateTime.Now;
						try
						{
							confDate = new DateTime(year, month, day, hour, 0, 0);
							string fn = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName)
														+ "_configuration (" + confDate.ToString("dd-MM-yyyy HH.mm.ss") + ").cfg";
							File.WriteAllBytes(fn, config);
						}
						catch
						{
							File.WriteAllBytes(Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + ".cfg", config);
						}

					}
					gp6File.BaseStream.Position = 0x600;
				}

				for (int i = 0; i < ((gp6File.BaseStream.Length - headerLength) / 0x200); i++)
				{
					var testByte = gp6File.ReadByte();
					gp6File.ReadBytes(1);
					gp6File.BaseStream.Read(gp6, filePointer, 510);
					if (testByte == 0x55) filePointer += 510;
				}
				if (gp6File.BaseStream.Length - gp6File.BaseStream.Position > 2)
				{
					gp6File.BaseStream.Position += 2;
					gp6File.BaseStream.Read(gp6, filePointer, (int)(gp6File.BaseStream.Length - gp6File.BaseStream.Position - 2));
				}
				gp6File.Close();

				//Crea e avvia il thread per la scrittura del file txt
				string txtName = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + ".txt";
				List<TimeStamp> txtList = new List<TimeStamp>();
				txtSem = new Semaphore(0, 1);
				txtSemBack = new Semaphore(1, 1);
				txtBGW = new BackgroundWorker();
				txtBGW.DoWork += (s, args) =>
				{
					txtBGW_doWork(ref txtList, txtName);
				};
				txtBGW.RunWorkerAsync();

				List<TimeStamp> kmlList = null;
				//if (pref_makeKml)
				//{
				//Crea e avvia il thread per la scrittura del file kml
				string kmlName = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName);
				kmlList = new List<TimeStamp>();
				kmlSem = new Semaphore(0, 1);
				kmlSemBack = new Semaphore(1, 1);
				kmlBGW = new BackgroundWorker();
				kmlBGW.DoWork += (s, args) =>
				{
					kmlBGW_doWork(ref kmlList, kmlName);
				};
				kmlBGW.RunWorkerAsync();
				//}

				//Crea e avvia il thread per la scrittura del file raw
				string rawName = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + "_";// + ".json";
				List<FarlocData> rawList = new List<FarlocData>();
				rawSem = new Semaphore(0, 1);
				rawSemBack = new Semaphore(1, 1);
				rawBGW = new BackgroundWorker();
				rawBGW.DoWork += (s, args) =>
				{
					rawBGW_doWork(rawList, rawName);
				};
				rawBGW.RunWorkerAsync();

				//Inizializza le variabili
				//int pos = 0;
				ramPos = 0;
				int end = gp6.Length;
				TimeStamp timeStamp = new TimeStamp();
				timeStamp.dateTime = new DateTime(2, 1, 1, 1, 0, 0);
				List<byte> noStampBuffer = new List<byte>();

				//Inizializza la progress bar
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
				{
					parent.statusProgressBar.IsIndeterminate = false;
					parent.statusProgressBar.Minimum = 0;
					parent.statusProgressBar.Maximum = end;
					parent.statusProgressBar.Value = 0;
					parent.txtProgressBar.Minimum = 0;
					parent.txtProgressBar.Maximum = end;
					parent.txtProgressBar.Value = 0;
					parent.kmlProgressBar.Minimum = 0;
					parent.kmlProgressBar.Maximum = end;
					parent.kmlProgressBar.Value = 0;
				}));

				//Importa il livello di debug per la conversione
				pref_debugLevel = parent.stDebugLevel;
				if (pref_debugLevel > 0)
				{
					//pref_battery = true;
					pref_metadata = true;
					pref_proximity = true;
				}

				int progressBarCounter = 0;
				//Cicla nel buffer decodificando i timestamp e aggiungendoli alla pila
				while (ramPos < end)
				{
					//actPos = (((ramPos / 510) + 1) * 2) + ramPos;

					try
					{

						noStampBuffer = decodeTimeStamp(ref gp6, ref timeStamp);   //decodifica il timestamp

					}
					catch
					{
						continue;
					}


					if (noStampBuffer.Count == 1)   //Segnale di fine file
					{
						break;
					}

					//Aggiorna la progress bar del producer
					progressBarCounter++;
					if (progressBarCounter >= 100)
					{
						Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
						{
							parent.statusProgressBar.Value = ramPos;
						}));
						progressBarCounter = 0;
					}

					if (timeStamp.txtAllowed > 0)
					{
						txtSemBack.WaitOne();
						//txtList.Add(timeStamp.clone()); //aggiunge il timestamp alla pila txt
						txtList.Add(timeStamp); //aggiunge il timestamp alla pila txt
						txtSem.Release();
					}

					if ((kmlList != null) && timeStamp.kmlAllowed)
					{
						kmlSemBack.WaitOne();
						kmlList.Add(timeStamp); //aggiunge il timestamp alla pila kml
						kmlSem.Release();
					}

					if (timeStamp.rawPreset)
					{
						rawSemBack.WaitOne();
						rawList.Add(timeStamp.cloneRaw());
						rawSem.Release();
					}

					if (noStampBuffer.Count == 1)
					{
						break;
					}
				}
				//Aggiorna per l'ultima volta la progress bar del producer
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
				{
					parent.statusProgressBar.Value = ramPos;
				}));

				//if (pref_makeKml)
				//{
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.kmlProgressBar.Maximum = kmlList.Count));
				//}
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.txtProgressBar.Maximum = txtList.Count));
				//Interlocked.Increment(ref lastTimestamp);   //Segnala ai thread che non saranno più aggiunti timestamp alle pile
				txtSemBack.WaitOne();
				//if (pref_makeKml)
				//{
				kmlSemBack.WaitOne();
				kmlSem.Release();   //rilascia il thread kml per l'ultima volta
									//}
				txtSem.Release();   //rilascia il thread txt per l'ultima volta


				//while (Interlocked.Read(ref conversionDone) < 2)
				//{
				//	Thread.Sleep(200);
				//}
				//aspetta che i thread abbiano finito di scrivere i rispettivi file
				txtSemBack.WaitOne();
				//if (pref_makeKml) kmlSemBack.WaitOne();
				kmlSemBack.WaitOne();

				Application.Current.Dispatcher.Invoke(new Action(() =>
				{
					parent.statusProgressBar.Height = 20;
					parent.txtProgressBar.Height = 0;
					parent.kmlProgressBar.Height = 0;
					parent.statusProgressBar.Margin = new Thickness(10, 5, 10, 10);
				}));

				rawSemBack.WaitOne();
				rawSem.Release();
				rawSemBack.WaitOne();

				if (fileType == FileType.FILE_GP6)
				{
					Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
										new Action(() => parent.nextFile(true)));
				}
				else
				{
					Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
										new Action(() => parent.nextFile(false)));
				}
			}
		}

		private void txtBGW_doWork(ref List<TimeStamp> tL, string txtName)
		{

			bool fileExisting = File.Exists(txtName);
			StreamWriter txtBW = new StreamWriter(new FileStream(txtName, FileMode.Append));

			placeHeader(txtBW, !fileExisting);//, ref columnPlace);

			string[] tabs = new string[p_fileCsv_length];
			tabs[p_fileCsv_name] = lastKnownUnitName;
			tabs[p_fileCsv_rfAddress] = lastKnownRfAddressString;
			//if (fileType == FileType.FILE_BS6)
			//{
			//	unitName = Path.GetFileNameWithoutExtension(txtName);
			//}
			//var t = new TimeStamp();
			while (true)
			{
				txtSem.WaitOne();
				if (tL.Count == 0)  //Se non ci sono più timestamp nella pila, si esce dal loop
				{
					break;
				}
				//t = tL[0];
				//tL.RemoveAt(0);

				//Si scrive il timestamp nel txt
				if (!pref_repeatEmptyValues)
				{
					tabs = new string[p_fileCsv_length];
				}

				tabs[p_fileCsv_name] = tL[0].unitNameTxt;
				tabs[p_fileCsv_rfAddress] = tL[0].rfAddressString;

				tabs[p_fileCsv_date] = tL[0].dateTime.ToString(pref_dateFormatParameter, CultureInfo.InvariantCulture);

				if ((tL[0].tsType & ts_battery) == ts_battery)
				{
					tabs[p_fileCsv_battery] = tL[0].batteryLevel.ToString("0.00") + "V";
				}
				if ((tL[0].tsType & ts_coordinate) == ts_coordinate)
				{
					tabs[p_fileCsv_latitude] = tL[0].lat.ToString("00.0000000", nfi);
					tabs[p_fileCsv_longitude] = tL[0].lon.ToString("000.0000000", nfi);
					if (tL[0].hAcc == 7)
					{
						tabs[p_fileCsv_horizontalAccuracy] = "200";
					}
					else
					{
						tabs[p_fileCsv_horizontalAccuracy] = String.Format("{0}", accuracySteps[tL[0].hAcc]);
					}

					tabs[p_fileCsv_altitude] = tL[0].altitude.ToString();
					if (tL[0].vAcc == 7)
					{
						tabs[p_fileCsv_verticalAccuracy] = "200";
					}
					else
					{
						tabs[p_fileCsv_verticalAccuracy] = String.Format("{0}", accuracySteps[tL[0].vAcc]);
					}
					tabs[p_fileCsv_speed] = tL[0].speed.ToString("0.0");
					tabs[p_fileCsv_course] = tL[0].cog.ToString("0.0");
				}

				if (((tL[0].tsTypeExt1 & ts_proximity) == ts_proximity) && pref_proximity)
				{
					tabs[p_fileCsv_proximity] = tL[0].proximityAddress.ToString();
					tabs[p_fileCsv_proximityPower] = tL[0].proximityPower.ToString();
				}

				if (pref_metadata)
				{
					if ((tL[0].tsType & ts_event) == ts_event)
					{
						tabs[p_fileCsv_event] = decodeEvent(tL[0].eventAr);
					}
					else
					{
						tabs[p_fileCsv_event] = "";
					}
				}

				if (pref_debugLevel > 0)
				{
					tabs[p_fileCsv_position] = tL[0].pos.ToString("X8");
				}

				tL.RemoveAt(0);

				for (int i = 0; i < p_fileCsv_length - 1; i++)
				{
					txtBW.Write(tabs[i] + "\t");
				}
				txtBW.Write(tabs[p_fileCsv_length - 1] + "\r\n");
				txtSemBack.Release();

			}

			txtBW.Close();
			txtSemBack.Release();

			//Interlocked.Increment(ref conversionDone);
		}

		private void rawBGW_doWork(List<FarlocData> tL, string rawName)
		{

			StreamWriter rawFileStream;

			while (true)
			{

				rawSem.WaitOne();
				if (tL.Count == 0)  //Se non ci sono più timestamp nella pila, si esce dal loop
				{
					break;
				}

				if (tL[0].rawData[0] == 0)  //Non ci sono satelliti, si esce subito senza creare il json
				{
					rawSemBack.Release();
					continue;
				}

				//string rawNameComp = Path.GetDirectoryName(rawName) + "\\" + Path.GetFileNameWithoutExtension(rawName) + "_" + tL[0].fixDateTime.ToString("yyyyMMdd_HHmmss") + ".json";
				string rawNameComp = rawName + tL[0].fixDateTime.ToString("yyyyMMdd_HHmmss") + ".json";

				if (File.Exists(rawNameComp))
				{
					File.Delete(rawNameComp);
				}

				byte[] rawd = tL[0].rawData;
				var rawFix = new RawFix();
				var rawFixGps = rawFix.GPS_data;
				var rawFixGalileo = rawFix.GALILEO_data;
				var rawFixBeidou = rawFix.BEIDOU_data;
				rawFix.timestamp = tL[0].fixDateTime.ToString("yyyy-MM-ddTHH:mm:ss");
				rawFix.location = new Location(tL[0].position, tL[0].speed);

				List<int> sv;
				List<double> codePhase;
				List<double> doppler;
				List<int> cNo;
				List<double[]> position;
				List<double[]> velocity;
				bool jsonok = true;
				for (int i = 0; i < tL[0].rawData[0]; i++)
				{
					int pp = (i * 11) + 1;
					if (rawd[pp + 1] == 255) continue; //In caso di satellite con svid 255 si scarta e si passa al satellite successivo
					int constellation = rawd[pp];
					//if (constellation == 3)				//In caso di satelliti Beidou GEO, per ora li salta perché non riusciamo a calcolare la pos.
					//{
					//	if ((rawd[pp + 1] <= 5) || (rawd[pp + 1] >= 59))
					//	{
					//		continue;
					//	}
					//}

					switch (constellation)
					{
						case 0:
							sv = rawFixGps.sv_gps;
							codePhase = rawFixGps.codePhase_gps;
							doppler = rawFixGps.doppler_gps;
							cNo = rawFixGps.cNo_gps;
							position = rawFixGps.position_gps;
							velocity = rawFixGps.velocity_gps;
							break;
						case 2:
							sv = rawFixGalileo.sv_galileo;
							codePhase = rawFixGalileo.codePhase_galileo;
							doppler = rawFixGalileo.doppler_galileo;
							cNo = rawFixGalileo.cNo_galileo;
							position = rawFixGalileo.position_galileo;
							velocity = rawFixGalileo.velocity_galileo;
							break;
						case 3:
							sv = rawFixBeidou.sv_beidou;
							codePhase = rawFixBeidou.codePhase_beidou;
							doppler = rawFixBeidou.doppler_beidou;
							cNo = rawFixBeidou.cNo_beidou;
							position = rawFixBeidou.position_beidou;
							velocity = rawFixBeidou.velocity_beidou;
							break;
						default:
							continue;
					}

					sv.Add(rawd[pp + 1]);
					cNo.Add(rawd[pp + 2]);
					doppler.Add(BitConverter.ToInt32(rawd.Skip(pp + 3).Take(4).ToArray(), 0) * .04);
					codePhase.Add(BitConverter.ToInt32(rawd.Skip(pp + 7).Take(4).ToArray(), 0) / 2097152.0);

					try
					{
						double[] posVel = new double[6];
						switch (constellation)
						{
							case 0:
								posVel = extractPositionFromEphemeris_GPS(tL[0].fixDateTime, rawd[pp + 1]);
								break;
							case 2:
								posVel = extractPositionFromEphemeris_GALILEO(tL[0].fixDateTime, rawd[pp + 1]);
								break;
							case 3:
								posVel = extractPositionFromEphemeris_BEIDOU(tL[0].fixDateTime, rawd[pp + 1]);
								break;
						}
						position.Add(posVel.Take(3).ToArray());
						velocity.Add(posVel.Skip(3).Take(3).ToArray());
					}
					catch
					{
						jsonok = false;
					}
				}

				tL.RemoveAt(0);

				if (jsonok == true)
				{
					rawFileStream = new StreamWriter(rawNameComp, true);
					var options = new JsonSerializerOptions
					{
						WriteIndented = true,
						Converters = { new JsonFloatConverter() }
					};
					string jsonString = JsonSerializer.Serialize(rawFix, options);
					rawFileStream.Write(jsonString);
					rawFileStream.Close();
				}

				rawSemBack.Release();
			}


			rawSemBack.Release();
		}

		private bool downloadRinex(DateTime timestamp, string localFileName)
		{
			string year = timestamp.Year.ToString("0000");
			string day = timestamp.DayOfYear.ToString("000");
			string effDayUrl = string.Format("https://igs.bkg.bund.de/root_ftp/IGS/BRDC/{0}/{1}/", year, day);

			List<string> remoteFileNames = new List<string>();
			using (HttpClient client = new HttpClient())
			{
				try
				{
					string html = client.GetStringAsync(effDayUrl).Result;
					Regex regex = new Regex(@"<a href=""([^""]+\.\w+)"">", RegexOptions.IgnoreCase);
					MatchCollection matches = regex.Matches(html);
					foreach (Match match in matches)
					{
						remoteFileNames.Add(match.Groups[1].Value);
					}
				}
				catch
				{
					return false;
				}
			}

			string remoteFileName = "";
			foreach (string f in remoteFileNames)
			{
				if (f.Contains("BRD") && f.Contains("MN") && f.Contains(year) && f.Contains(day))
				{
					remoteFileName = f;
					break;
				}
			}
			if (remoteFileName == "") return false;

			byte[] data;
			effDayUrl += remoteFileName;
			try
			{
				using (WebClient request = new WebClient())     //effettua il download	
				{
					data = request.DownloadData(effDayUrl);
				}
			}
			catch
			{
				return false;
			}

			//string dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + MainWindow.companyFolder + MainWindow.appFolder + "\\brdc";
			//var outs = Directory.CreateDirectory(dir);
			//string localFileName = dir + "\\" + timestamp.ToString("yyyyMMdd") + ".dat.gz";

			using (FileStream file = File.Create(localFileName))   //Salva l'array di byte in un file
			{
				file.Write(data, 0, data.Length);
				file.Close();
			}

			return true;
		}

		private double[] extractPositionFromEphemeris_GPS(DateTime timestamp, int svId)
		{
			CultureInfo cfi = CultureInfo.InvariantCulture;
			Ephemeris_GPS ephemeris = new Ephemeris_GPS();
			//sviluppo
			//timestamp = new DateTime(2023, 10, 10, 15, 49, 09);
			///sviluppo

			bool addNewEphemeris = true;
			if (Ephemerides_GPS.Count > 0)          //Controlla se l'effemeride è gia in memoria
			{
				for (int i = 0; i < Ephemerides_GPS.Count; i++)
				{
					if (Ephemerides_GPS[i].timestamp.Date == timestamp.Date)
					{
						ephemeris = Ephemerides_GPS[i];
						addNewEphemeris = false;
						break;
					}
				}
			}

			if (addNewEphemeris)        //Se non è in memoria la estrae dal relativo file (se il file non è presente, prima lo scarica da internet)
			{
				string dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
								MainWindow.companyFolder + MainWindow.appFolder + "\\brdc";
				var outs = Directory.CreateDirectory(dir);
				string fileName = dir + "\\" + timestamp.ToString("yyyyMMdd") + ".dat.gz";

				//Controlla se esiste il file, in caso contrario lo scarica
				if (!File.Exists(fileName))
				{
					//Se non esiste scarica l'effemeride nel file
					bool result = downloadRinex(timestamp, fileName);
					if (!result)
					{
						throw new Exception("No valid RINEX file found on server.");
					}

				}

				//Il file è sicuramente presente, si estrae l'effemeride
				List<byte> buff = new List<byte>();
				ephemeris = new Ephemeris_GPS();
				ephemeris.timestamp = new DateTime(timestamp.Date.Year, timestamp.Date.Month, timestamp.Date.Day);

				using (FileStream gzStream = File.Open(fileName, FileMode.Open))
				{
					using (GZipStream gzDecompressor = new GZipStream(gzStream, CompressionMode.Decompress))
					{
						int b = gzDecompressor.ReadByte();
						while (b > 0)
						{
							buff.Add((byte)b);
							b = gzDecompressor.ReadByte();
						}
					}
				}

				//Crea uno stream dell'array di byte per leggere sequenzialmente le stringhe su cui fare il parsing
				MemoryStream ms = new MemoryStream(buff.ToArray());
				StreamReader ifs = new StreamReader(ms);
				buff = null;
				string str = null;

				//Cerca i dati sulla ionosfera che saranno allegati a tutte le effemeridi del file
				ephemeris.leapSecond = 18;
				/*
				 Bisogna convertire l'ora del punto in formato NTP (vedi https://weirdo.cloud/) e confrontarla con l'elenco che si trova qui:
				https://data.iana.org/time-zones/data/leap-seconds.list. Per comodità vedere anche: 
				https://www.cnmoc.usff.navy.mil/Our-Commands/United-States-Naval-Observatory/Precise-Time-Department/Global-Positioning-System/USNO-GPS-Time-Transfer/Leap-Seconds/
				*/
				ephemeris.m_week = -1;
				for (int i = 0; i < 100; i++)
				{
					str = ifs.ReadLine();
					//if (str != null && str.Contains("ION ALPHA"))
					//{
					//	ephemeris.ion[0] = double.Parse(str.Replace('D', 'E').Substring(3, 12), CultureInfo.InvariantCulture);
					//	ephemeris.ion[1] = double.Parse(str.Replace('D', 'E').Substring(15, 12), CultureInfo.InvariantCulture);
					//	ephemeris.ion[2] = double.Parse(str.Replace('D', 'E').Substring(27, 12), CultureInfo.InvariantCulture);
					//	ephemeris.ion[3] = double.Parse(str.Replace('D', 'E').Substring(39, 12), CultureInfo.InvariantCulture);
					//}
					//if (str != null && str.Contains("ION BETA"))
					//{
					//	ephemeris.ion[4] = double.Parse(str.Replace('D', 'E').Substring(3, 12), CultureInfo.InvariantCulture);
					//	ephemeris.ion[5] = double.Parse(str.Replace('D', 'E').Substring(15, 12), CultureInfo.InvariantCulture);
					//	ephemeris.ion[6] = double.Parse(str.Replace('D', 'E').Substring(27, 12), CultureInfo.InvariantCulture);
					//	ephemeris.ion[7] = double.Parse(str.Replace('D', 'E').Substring(39, 12), CultureInfo.InvariantCulture);
					//}
					//if (str != null && str.Contains("LEAP SECONDS"))
					//{
					//	ephemeris.leapSecond = int.Parse(str.Substring(3, 4));
					//}
					//if (str != null && str.Contains("DELTA-UTC: A0,A1,T,W")) ephemeris.m_week = int.Parse(str.Substring(54, 5));

					if (str != null && str.Contains("END OF HEADER")) break;
				}

				while (str != null)
				{
					//Aspetta un satellite GPS che inizia per "G"
					while (true)
					{
						str = ifs.ReadLine();
						if (str == null) break;
						if (str.StartsWith("G"))
						{
							break;
						}
					}

					if (str == null) break;
					int prn = int.Parse(str.Substring(1, 2), cfi);
					if (ephemeris.satellites[prn] == null) ephemeris.satellites[prn] = new Satellite_Ephemeris_GPS();

					Satellite_Ephemeris_GPS sat = ephemeris.satellites[prn];
					sat.svId = prn;

					Satellite_Ephemeris_GPS.TimeInterval newTimeInterval = new Satellite_Ephemeris_GPS.TimeInterval();
					int year = int.Parse(str.Substring(4, 4), cfi);
					int month = int.Parse(str.Substring(9, 2), cfi);
					int day = int.Parse(str.Substring(12, 2), cfi);
					int hour = int.Parse(str.Substring(15, 2), cfi);
					int minute = int.Parse(str.Substring(18, 2), cfi);
					double second = double.Parse(str.Substring(21, 2), cfi);
					newTimeInterval.startTime_UTC = new DateTime(year, month, day, hour, minute, (int)second);
					newTimeInterval.startTime_UTC = newTimeInterval.startTime_UTC.AddSeconds(-ephemeris.leapSecond);
					newTimeInterval.af0 = double.Parse(str.Substring(23, 19), cfi);
					newTimeInterval.af1 = double.Parse(str.Substring(42, 19), cfi);
					newTimeInterval.af2 = double.Parse(str.Substring(61, 19), cfi);

					//Linea 2
					str = ifs.ReadLine(); if (str == null) break;
					//newTimeInterval.IODE = double.Parse(str.Substring(4, 19), cfi);
					newTimeInterval.crs = double.Parse(str.Substring(23, 19), cfi);
					newTimeInterval.deltan = double.Parse(str.Substring(42, 19), cfi);
					newTimeInterval.M0 = double.Parse(str.Substring(61, 19), cfi);

					//Linea 3
					str = ifs.ReadLine(); if (str == null) break;
					newTimeInterval.cuc = double.Parse(str.Substring(4, 19), cfi);
					newTimeInterval.ecc = double.Parse(str.Substring(23, 19), cfi);
					newTimeInterval.cus = double.Parse(str.Substring(42, 19), cfi);
					newTimeInterval.roota = double.Parse(str.Substring(61, 19), cfi);

					//Linea 4
					str = ifs.ReadLine(); if (str == null) break;
					newTimeInterval.toe = double.Parse(str.Substring(4, 19), cfi);
					newTimeInterval.cic = double.Parse(str.Substring(23, 19), cfi);
					newTimeInterval.Omega0 = double.Parse(str.Substring(42, 19), cfi);
					newTimeInterval.cis = double.Parse(str.Substring(61, 19), cfi);

					//Linea 5
					str = ifs.ReadLine(); if (str == null) break;
					newTimeInterval.i0 = double.Parse(str.Substring(4, 19), cfi);
					newTimeInterval.crc = double.Parse(str.Substring(23, 19), cfi);
					newTimeInterval.omega = double.Parse(str.Substring(42, 19), cfi);
					newTimeInterval.Omegadot = double.Parse(str.Substring(61, 19), cfi);

					//Linea 6
					str = ifs.ReadLine(); if (str == null) break;
					newTimeInterval.idot = double.Parse(str.Substring(4, 19), cfi);
					//newTimeInterval.codes = double.Parse(str.Substring(23, 19), cfi);
					//newTimeInterval.weekno = double.Parse(str.Substring(42, 19), cfi);
					//newTimeInterval.L2flag = double.Parse(str.Substring(61, 19), cfi);

					//Linea 7
					str = ifs.ReadLine(); if (str == null) break;
					//newTimeInterval.svaccur = double.Parse(str.Substring(4, 19), cfi);
					//newTimeInterval.svhealth = double.Parse(str.Substring(23, 19), cfi);
					//newTimeInterval.tgd = double.Parse(str.Substring(42, 19), cfi);

					//Linea 8
					str = ifs.ReadLine(); if (str == null) break;
					//newTimeInterval.tom = double.Parse(str.Substring(4, 19), cfi);
					//newTimeInterval.fit = double.Parse(str.Substring(23, 19), cfi);

					sat.timeIntervals.Add(newTimeInterval);
				}
				Ephemerides_GPS.Add(ephemeris);
			}

			//Ora abbiamo le effemeridi complete del giorno, possiamo prelevare quella relativa al satellite e all'orario e calcolare la posizione del satellite

			//Estraiamo il timeinterval più migliore assai
			//sviluppo
			//svId = 5;
			///sviluppo
			Satellite_Ephemeris_GPS satellite = ephemeris.satellites[svId];
			Satellite_Ephemeris_GPS.TimeInterval bestTimeInterval = new Satellite_Ephemeris_GPS.TimeInterval();
			int leapSeconds = ephemeris.leapSecond;
			int tmin = 86400;
			for (int i = 0; i < satellite.timeIntervals.Count; i++)
			{
				int t = (int)Math.Abs((satellite.timeIntervals[i].startTime_UTC - timestamp).TotalSeconds);
				if (t < tmin)
				{
					tmin = t;
					bestTimeInterval = satellite.timeIntervals[i];
				}
				else
				{
					break;
				}
			}

			double[] position = getSatellitePosition_GPS(bestTimeInterval, timestamp, leapSeconds);
			var loc1 = getSatellitePosition_GPS(bestTimeInterval, timestamp.AddMilliseconds(100), leapSeconds);
			var loc2 = getSatellitePosition_GPS(bestTimeInterval, timestamp.AddMilliseconds(-100), leapSeconds);
			var xv = (loc1[0] - loc2[0]) * 0.5 / 0.1;
			var yv = (loc1[1] - loc2[1]) * 0.5 / 0.1;
			var zv = (loc1[2] - loc2[2]) * 0.5 / 0.1;

			return new double[6] { position[0], position[1], position[2], xv, yv, zv };

		}

		private double[] getSatellitePosition_GPS(Satellite_Ephemeris_GPS.TimeInterval ti, DateTime t, int leapSeconds)
		{
			//t = DateTime.SpecifyKind(new DateTime(t.Year, t.Month, t.Day, t.Hour, t.Minute, t.Second), DateTimeKind.Utc); //Data del fix in formato UTC

			int m_day_of_year = t.DayOfYear;
			double gps_sec = (t - gps_time).TotalSeconds + leapSeconds;  //GPOESSE valutare quanti secondi di offset ci sono dopo il 2018
			double gps_days = Math.Floor(gps_sec / SEC_PER_DAY);
			double m_week = Math.Floor(gps_days / 7.0);
			var sow = gps_sec - m_week * 7 * SEC_PER_DAY;
			sow = Math.Round(sow, 1);

			var a = Math.Pow(ti.roota, 2);     // Semi major axis
											   //var tk = Util.check_t(sow - ti.toe);          // tk = sow-@toe
			double tk;
			var tt = sow - ti.toe;
			const double half_week = 302400.0;
			if (tt > half_week) tk = tt - 2 * half_week;
			else if (tt < -half_week) tk = tt + 2 * half_week;
			else tk = tt;

			var n0 = Math.Sqrt(GM_GPS / Math.Pow(a, 3));    // Computed mean motion 
			var n = n0 + ti.deltan;                        // Corrected mean motion
			var m = ti.M0 + n * tk;                        // Mean anomaly

			m = (m + PI2) - PI2 * (int)Math.Floor((m + PI2) / PI2);
			var e = m;
			for (int j = 0; j < 15; j++)
			{
				var e_old = e;
				e = m + ti.ecc * Math.Sin(e_old);
				var dE = (e + e_old) - PI2 * (int)Math.Floor((e - e_old) / PI2);
				if (Math.Abs(dE) < 1.0e-15) break;
			}
			e = (e + PI2) - PI2 * (int)Math.Floor((e + PI2) / PI2);
			var v = Math.Atan2(Math.Sqrt(1.0 - Math.Pow(ti.ecc, 2)) * Math.Sin(e), Math.Cos(e) - ti.ecc);
			var phi = v + ti.omega;
			phi = phi - PI2 * (int)Math.Floor(phi / PI2);
			var phi2 = 2.0 * phi;

			var cosphi2 = Math.Cos(phi2);
			var sinphi2 = Math.Sin(phi2);

			var u = phi + ti.cuc * cosphi2 + ti.cus * sinphi2;
			var r = a * (1.0 - ti.ecc * Math.Cos(e)) + ti.crc * cosphi2 + ti.crs * sinphi2;
			var i = ti.i0 + ti.idot * tk + ti.cic * cosphi2 + ti.cis * sinphi2;
			var om = ti.Omega0 + (ti.Omegadot - WGS84_EARTH_ROTATION_RATE_GPS) * tk - WGS84_EARTH_ROTATION_RATE_GPS * ti.toe;
			//om = Util.rem2pi(om + Util.PI2);
			om = (om + PI2) - PI2 * (int)Math.Floor((om + PI2) / PI2);
			var x1 = Math.Cos(u) * r;
			var y1 = Math.Sin(u) * r;

			var x = x1 * Math.Cos(om) - y1 * Math.Cos(i) * Math.Sin(om);
			var y = x1 * Math.Sin(om) + y1 * Math.Cos(i) * Math.Cos(om);

			var z = y1 * Math.Sin(i);

			return new double[3] { x, y, z };
		}

		private double[] extractPositionFromEphemeris_GALILEO(DateTime timestamp, int svId)
		{
			CultureInfo cfi = CultureInfo.InvariantCulture;
			Ephemeris_GALILEO ephemeris = new Ephemeris_GALILEO();
			//sviluppo
			//timestamp = new DateTime(2023, 10, 10, 15, 49, 09);
			///sviluppo

			bool addNewEphemeris = true;
			if (Ephemerides_GALILEO.Count > 0)          //Controlla se l'effemeride è gia in memoria
			{
				for (int i = 0; i < Ephemerides_GALILEO.Count; i++)
				{
					if (Ephemerides_GALILEO[i].timestamp.Date == timestamp.Date)
					{
						ephemeris = Ephemerides_GALILEO[i];
						addNewEphemeris = false;
						break;
					}
				}
			}

			if (addNewEphemeris)        //Se non è in memoria la estrae dal relativo file (se il file non è presente, prima lo scarica da internet)
			{
				string dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
				MainWindow.companyFolder + MainWindow.appFolder + "\\brdc";
				var outs = Directory.CreateDirectory(dir);
				string fileName = dir + "\\" + timestamp.ToString("yyyyMMdd") + ".dat.gz";

				//Controlla se esiste il file, in caso contrario lo scarica
				if (!File.Exists(fileName))
				{
					//Se non esiste scarica l'effemeride nel file
					bool result = downloadRinex(timestamp, fileName);
					if (!result)
					{
						throw new Exception("No valid RINEX file found on server.");
					}
				}

				//Il file è sicuramente presente, si estrae l'effemeride
				List<byte> buff = new List<byte>();
				ephemeris = new Ephemeris_GALILEO();
				ephemeris.timestamp = new DateTime(timestamp.Date.Year, timestamp.Date.Month, timestamp.Date.Day);

				using (FileStream gzStream = File.Open(fileName, FileMode.Open))
				{
					using (GZipStream gzDecompressor = new GZipStream(gzStream, CompressionMode.Decompress))
					{
						int b = gzDecompressor.ReadByte();
						while (b > 0)
						{
							buff.Add((byte)b);
							b = gzDecompressor.ReadByte();
						}
					}
				}

				//Crea uno stream dell'array di byte per leggere sequenzialmente le stringhe su cui fare il parsing
				MemoryStream ms = new MemoryStream(buff.ToArray());
				StreamReader ifs = new StreamReader(ms);
				buff = null;
				string str = null;

				//Cerca i dati sulla ionosfera che saranno allegati a tutte le effemeridi del file
				ephemeris.leapSecond = 18;
				//Cercare un server che fornisca questa informazione perché già a giugno 2025
				//i secondi intercalare potrebbero diventare 19	
				ephemeris.m_week = -1;
				for (int i = 0; i < 100; i++)
				{
					str = ifs.ReadLine();
					//if (str != null && str.Contains("ION ALPHA"))
					//{
					//	ephemeris.ion[0] = double.Parse(str.Replace('D', 'E').Substring(3, 12), CultureInfo.InvariantCulture);
					//	ephemeris.ion[1] = double.Parse(str.Replace('D', 'E').Substring(15, 12), CultureInfo.InvariantCulture);
					//	ephemeris.ion[2] = double.Parse(str.Replace('D', 'E').Substring(27, 12), CultureInfo.InvariantCulture);
					//	ephemeris.ion[3] = double.Parse(str.Replace('D', 'E').Substring(39, 12), CultureInfo.InvariantCulture);
					//}
					//if (str != null && str.Contains("ION BETA"))
					//{
					//	ephemeris.ion[4] = double.Parse(str.Replace('D', 'E').Substring(3, 12), CultureInfo.InvariantCulture);
					//	ephemeris.ion[5] = double.Parse(str.Replace('D', 'E').Substring(15, 12), CultureInfo.InvariantCulture);
					//	ephemeris.ion[6] = double.Parse(str.Replace('D', 'E').Substring(27, 12), CultureInfo.InvariantCulture);
					//	ephemeris.ion[7] = double.Parse(str.Replace('D', 'E').Substring(39, 12), CultureInfo.InvariantCulture);
					//}
					//if (str != null && str.Contains("LEAP SECONDS"))
					//{
					//	ephemeris.leapSecond = int.Parse(str.Substring(3, 4));
					//}
					//if (str != null && str.Contains("DELTA-UTC: A0,A1,T,W")) ephemeris.m_week = int.Parse(str.Substring(54, 5));

					if (str != null && str.Contains("END OF HEADER")) break;
				}

				while (str != null)
				{
					//Aspetta un satellite GALILEO che inizia per "E"
					while (true)
					{
						str = ifs.ReadLine();
						if (str == null) break;
						if (str.StartsWith("E"))
						{
							break;
						}
					}

					if (str == null) break;

					int prn = int.Parse(str.Substring(1, 2), cfi);
					if (ephemeris.satellites[prn] == null) ephemeris.satellites[prn] = new Satellite_Ephemeris_GALILEO();

					Satellite_Ephemeris_GALILEO sat = ephemeris.satellites[prn];
					sat.svId = prn;

					Satellite_Ephemeris_GALILEO.TimeInterval newTimeInterval = new Satellite_Ephemeris_GALILEO.TimeInterval();
					int year = int.Parse(str.Substring(4, 4), cfi);
					int month = int.Parse(str.Substring(9, 2), cfi);
					int day = int.Parse(str.Substring(12, 2), cfi);
					int hour = int.Parse(str.Substring(15, 2), cfi);
					int minute = int.Parse(str.Substring(18, 2), cfi);
					double second = double.Parse(str.Substring(21, 2), cfi);
					newTimeInterval.startTime_UTC = new DateTime(year, month, day, hour, minute, (int)second);
					newTimeInterval.startTime_UTC = newTimeInterval.startTime_UTC.AddSeconds(-ephemeris.leapSecond);
					newTimeInterval.af0 = double.Parse(str.Substring(23, 19), cfi);
					newTimeInterval.af1 = double.Parse(str.Substring(42, 19), cfi);
					newTimeInterval.af2 = double.Parse(str.Substring(61, 19), cfi);

					//Linea 2
					str = ifs.ReadLine(); if (str == null) break;
					//str = str.Replace('D', 'E');
					//newTimeInterval.IODE = double.Parse(str.Substring(4, 19), cfi);
					newTimeInterval.crs = double.Parse(str.Substring(23, 19), cfi);
					newTimeInterval.deltan = double.Parse(str.Substring(42, 19), cfi);
					newTimeInterval.M0 = double.Parse(str.Substring(61, 19), cfi);

					//Linea 3
					str = ifs.ReadLine(); if (str == null) break;
					//str = str.Replace('D', 'E');
					newTimeInterval.cuc = double.Parse(str.Substring(4, 19), cfi);
					newTimeInterval.ecc = double.Parse(str.Substring(23, 19), cfi);
					newTimeInterval.cus = double.Parse(str.Substring(42, 19), cfi);
					newTimeInterval.roota = double.Parse(str.Substring(61, 19), cfi);

					//Linea 4
					str = ifs.ReadLine(); if (str == null) break;
					//str = str.Replace('D', 'E');
					newTimeInterval.toe = double.Parse(str.Substring(4, 19), cfi);
					newTimeInterval.cic = double.Parse(str.Substring(23, 19), cfi);
					newTimeInterval.Omega0 = double.Parse(str.Substring(42, 19), cfi);
					newTimeInterval.cis = double.Parse(str.Substring(61, 19), cfi);

					//Linea 5
					str = ifs.ReadLine(); if (str == null) break;
					//str = str.Replace('D', 'E');
					newTimeInterval.i0 = double.Parse(str.Substring(4, 19), cfi);
					newTimeInterval.crc = double.Parse(str.Substring(23, 19), cfi);
					newTimeInterval.omega = double.Parse(str.Substring(42, 19), cfi);
					newTimeInterval.Omegadot = double.Parse(str.Substring(61, 19), cfi);

					//Linea 6
					str = ifs.ReadLine(); if (str == null) break;
					//str = str.Replace('D', 'E');
					newTimeInterval.idot = double.Parse(str.Substring(4, 19), cfi);
					//newTimeInterval.codes = double.Parse(str.Substring(23, 19), cfi);
					//newTimeInterval.weekno = double.Parse(str.Substring(42, 19), cfi);
					//newTimeInterval.L2flag = double.Parse(str.Substring(61, 19), cfi);

					//Linea 7
					str = ifs.ReadLine(); if (str == null) break;
					//str = str.Replace('D', 'E');
					//newTimeInterval.svaccur = double.Parse(str.Substring(4, 19), cfi);
					//newTimeInterval.svhealth = double.Parse(str.Substring(23, 19), cfi);
					//newTimeInterval.tgd = double.Parse(str.Substring(42, 19), cfi);

					//Linea 8
					str = ifs.ReadLine(); if (str == null) break;
					//newTimeInterval.tom = double.Parse(str.Substring(4, 19), cfi);
					//newTimeInterval.fit = double.Parse(str.Substring(23, 19), cfi);

					sat.timeIntervals.Add(newTimeInterval);
				}
				Ephemerides_GALILEO.Add(ephemeris);
			}

			//Ora abbiamo le effemeridi complete del giorno, possiamo prelevare quella relativa al satellite e all'orario e calcolare la posizione del satellite

			//Estraiamo il timeinterval più migliore assai
			//sviluppo
			//svId = 5;
			///sviluppo
			Satellite_Ephemeris_GALILEO satellite = ephemeris.satellites[svId];
			Satellite_Ephemeris_GALILEO.TimeInterval bestTimeInterval = new Satellite_Ephemeris_GALILEO.TimeInterval();
			int leapSeconds = ephemeris.leapSecond;
			int tmin = 86400;
			for (int i = 0; i < satellite.timeIntervals.Count; i++)
			{
				int t = (int)Math.Abs((satellite.timeIntervals[i].startTime_UTC - timestamp).TotalSeconds);
				if (t < tmin)
				{
					tmin = t;
					bestTimeInterval = satellite.timeIntervals[i];
				}
				else
				{
					break;
				}
			}

			double[] position = getSatellitePosition_GALILEO(bestTimeInterval, timestamp, leapSeconds);
			var loc1 = getSatellitePosition_GALILEO(bestTimeInterval, timestamp.AddMilliseconds(100), leapSeconds);
			var loc2 = getSatellitePosition_GALILEO(bestTimeInterval, timestamp.AddMilliseconds(-100), leapSeconds);
			var xv = (loc1[0] - loc2[0]) * 0.5 / 0.1;
			var yv = (loc1[1] - loc2[1]) * 0.5 / 0.1;
			var zv = (loc1[2] - loc2[2]) * 0.5 / 0.1;

			return new double[6] { position[0], position[1], position[2], xv, yv, zv };
		}

		private double[] getSatellitePosition_GALILEO(Satellite_Ephemeris_GALILEO.TimeInterval ti, DateTime t, int leapSeconds)
		{
			//t = DateTime.SpecifyKind(new DateTime(t.Year, t.Month, t.Day, t.Hour, t.Minute, t.Second), DateTimeKind.Utc); //Data del fix in formato UTC

			int m_day_of_year = t.DayOfYear;
			double gps_sec = (t - gps_time).TotalSeconds + leapSeconds;  //GPOESSE valutare quanti secondi di offset ci sono dopo il 2018
			double gps_days = Math.Floor(gps_sec / SEC_PER_DAY);
			double m_week = Math.Floor(gps_days / 7.0);
			var sow = gps_sec - m_week * 7 * SEC_PER_DAY;
			sow = Math.Round(sow, 1);

			var a = Math.Pow(ti.roota, 2);     // Semi major axis
											   //var tk = Util.check_t(sow - ti.toe);          // tk = sow-@toe
			double tk;
			var tt = sow - ti.toe;
			const double half_week = 302400.0;
			if (tt > half_week) tk = tt - 2 * half_week;
			else if (tt < -half_week) tk = tt + 2 * half_week;
			else tk = tt;

			var n0 = Math.Sqrt(GM_GAL / Math.Pow(a, 3));    // Computed mean motion 
			var n = n0 + ti.deltan;                        // Corrected mean motion
			var m = ti.M0 + n * tk;                        // Mean anomaly

			m = (m + PI2) - PI2 * (int)Math.Floor((m + PI2) / PI2);
			var e = m;
			for (int j = 0; j < 15; j++)
			{
				var e_old = e;
				e = m + ti.ecc * Math.Sin(e_old);
				var dE = (e + e_old) - PI2 * (int)Math.Floor((e - e_old) / PI2);
				if (Math.Abs(dE) < 1.0e-15) break;
			}
			e = (e + PI2) - PI2 * (int)Math.Floor((e + PI2) / PI2);
			var v = Math.Atan2(Math.Sqrt(1.0 - Math.Pow(ti.ecc, 2)) * Math.Sin(e), Math.Cos(e) - ti.ecc);
			var phi = v + ti.omega;
			phi = phi - PI2 * (int)Math.Floor(phi / PI2);
			var phi2 = 2.0 * phi;

			var cosphi2 = Math.Cos(phi2);
			var sinphi2 = Math.Sin(phi2);

			var u = phi + ti.cuc * cosphi2 + ti.cus * sinphi2;
			var r = a * (1.0 - ti.ecc * Math.Cos(e)) + ti.crc * cosphi2 + ti.crs * sinphi2;
			var i = ti.i0 + ti.idot * tk + ti.cic * cosphi2 + ti.cis * sinphi2;
			var om = ti.Omega0 + (ti.Omegadot - WGS84_EARTH_ROTATION_RATE_GAL) * tk - WGS84_EARTH_ROTATION_RATE_GAL * ti.toe;
			//om = Util.rem2pi(om + Util.PI2);
			om = (om + PI2) - PI2 * (int)Math.Floor((om + PI2) / PI2);
			var x1 = Math.Cos(u) * r;
			var y1 = Math.Sin(u) * r;

			var x = x1 * Math.Cos(om) - y1 * Math.Cos(i) * Math.Sin(om);
			var y = x1 * Math.Sin(om) + y1 * Math.Cos(i) * Math.Cos(om);

			var z = y1 * Math.Sin(i);

			return new double[3] { x, y, z };

		}

		private double[] extractPositionFromEphemeris_BEIDOU(DateTime timestamp, int svId)
		{
			CultureInfo cfi = CultureInfo.InvariantCulture;
			Ephemeris_BEIDOU ephemeris = new Ephemeris_BEIDOU();

			bool addNewEphemeris = true;

			if (Ephemerides_BEIDOU.Count > 0)          //Controlla se l'effemeride è gia in memoria
			{
				for (int i = 0; i < Ephemerides_BEIDOU.Count; i++)
				{
					if (Ephemerides_BEIDOU[i].timestamp.Date == timestamp.Date)
					{
						ephemeris = Ephemerides_BEIDOU[i];
						addNewEphemeris = false;
						break;
					}
				}
			}

			if (addNewEphemeris)        //Se non è in memoria la estrae dal relativo file (se il file non è presente, prima lo scarica da internet)
			{
				string dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
								MainWindow.companyFolder + MainWindow.appFolder + "\\brdc";
				var outs = Directory.CreateDirectory(dir);
				string fileName = dir + "\\" + timestamp.ToString("yyyyMMdd") + ".dat.gz";

				//Controlla se esiste il file, in caso contrario lo scarica
				if (!File.Exists(fileName))
				{
					//Se non esiste scarica l'effemeride nel file
					bool result = downloadRinex(timestamp, fileName);
					if (!result)
					{
						throw new Exception("No valid RINEX file found on server.");
					}

				}

				//Il file è sicuramente presente, si estrae l'effemeride
				List<byte> buff = new List<byte>();
				ephemeris = new Ephemeris_BEIDOU();
				ephemeris.timestamp = new DateTime(timestamp.Date.Year, timestamp.Date.Month, timestamp.Date.Day);

				using (FileStream gzStream = File.Open(fileName, FileMode.Open))
				{
					using (GZipStream gzDecompressor = new GZipStream(gzStream, CompressionMode.Decompress))
					{
						int b = gzDecompressor.ReadByte();
						while (b > 0)
						{
							buff.Add((byte)b);
							b = gzDecompressor.ReadByte();
						}
					}
				}

				//Crea uno stream dell'array di byte per leggere sequenzialmente le stringhe su cui fare il parsing
				MemoryStream ms = new MemoryStream(buff.ToArray());
				StreamReader ifs = new StreamReader(ms);
				buff = null;
				string str = null;

				//Cerca i dati sulla ionosfera che saranno allegati a tutte le effemeridi del file
				ephemeris.leapSecond = 4;   //Nel BEIDOU i leap second sono solo 4. Per il confronto con gLab, nel programma bisogna comunque inserire
				/*
				 Bisogna convertire l'ora del punto in formato NTP (vedi https://weirdo.cloud/) e confrontarla con l'elenco che si trova qui:
				https://data.iana.org/time-zones/data/leap-seconds.list. Per comodità vedere anche: 
				https://www.cnmoc.usff.navy.mil/Our-Commands/United-States-Naval-Observatory/Precise-Time-Department/Global-Positioning-System/USNO-GPS-Time-Transfer/Leap-Seconds/
				*/                        //18, perché glab vuole il tempo GPS, qualsiasi costellazione si calcoli
				ephemeris.m_week = -1;
				for (int i = 0; i < 100; i++)
				{
					str = ifs.ReadLine();
					//if (str != null && str.Contains("ION ALPHA"))
					//{
					//	ephemeris.ion[0] = double.Parse(str.Replace('D', 'E').Substring(3, 12), CultureInfo.InvariantCulture);
					//	ephemeris.ion[1] = double.Parse(str.Replace('D', 'E').Substring(15, 12), CultureInfo.InvariantCulture);
					//	ephemeris.ion[2] = double.Parse(str.Replace('D', 'E').Substring(27, 12), CultureInfo.InvariantCulture);
					//	ephemeris.ion[3] = double.Parse(str.Replace('D', 'E').Substring(39, 12), CultureInfo.InvariantCulture);
					//}
					//if (str != null && str.Contains("ION BETA"))
					//{
					//	ephemeris.ion[4] = double.Parse(str.Replace('D', 'E').Substring(3, 12), CultureInfo.InvariantCulture);
					//	ephemeris.ion[5] = double.Parse(str.Replace('D', 'E').Substring(15, 12), CultureInfo.InvariantCulture);
					//	ephemeris.ion[6] = double.Parse(str.Replace('D', 'E').Substring(27, 12), CultureInfo.InvariantCulture);
					//	ephemeris.ion[7] = double.Parse(str.Replace('D', 'E').Substring(39, 12), CultureInfo.InvariantCulture);
					//}
					//if (str != null && str.Contains("LEAP SECONDS"))
					//{
					//	ephemeris.leapSecond = int.Parse(str.Substring(3, 4));
					//}
					//if (str != null && str.Contains("DELTA-UTC: A0,A1,T,W")) ephemeris.m_week = int.Parse(str.Substring(54, 5));

					if (str != null && str.Contains("END OF HEADER")) break;
				}

				while (str != null)
				{
					//Aspetta un satellite BEIDOU che inizia per "C"
					while (true)
					{
						str = ifs.ReadLine();
						if (str == null) break;
						if (str.StartsWith("C"))
						{
							break;
						}
					}

					//str = ifs.ReadLine();
					if (str == null) break;
					//Linea 0
					int prn = int.Parse(str.Substring(1, 2), cfi);
					if (ephemeris.satellites[prn] == null) ephemeris.satellites[prn] = new Satellite_Ephemeris_BEIDOU();

					Satellite_Ephemeris_BEIDOU sat = ephemeris.satellites[prn];
					sat.svId = prn;

					Satellite_Ephemeris_BEIDOU.TimeInterval newTimeInterval = new Satellite_Ephemeris_BEIDOU.TimeInterval();
					int year = int.Parse(str.Substring(4, 4), cfi);
					int month = int.Parse(str.Substring(9, 2), cfi);
					int day = int.Parse(str.Substring(12, 2), cfi);
					int hour = int.Parse(str.Substring(15, 2), cfi);
					int minute = int.Parse(str.Substring(18, 2), cfi);
					double second = double.Parse(str.Substring(21, 2), cfi);
					newTimeInterval.startTime_UTC = new DateTime(year, month, day, hour, minute, (int)second);
					newTimeInterval.startTime_UTC = newTimeInterval.startTime_UTC.AddSeconds(-ephemeris.leapSecond);
					newTimeInterval.af0 = double.Parse(str.Substring(23, 19), cfi);
					newTimeInterval.af1 = double.Parse(str.Substring(42, 19), cfi);
					newTimeInterval.af2 = double.Parse(str.Substring(61, 19), cfi);

					//Linea 1
					str = ifs.ReadLine(); if (str == null) break;
					//newTimeInterval.IODE = double.Parse(str.Substring(4, 19), cfi);
					newTimeInterval.crs = double.Parse(str.Substring(23, 19), cfi);
					newTimeInterval.deltan = double.Parse(str.Substring(42, 19), cfi);
					newTimeInterval.M0 = double.Parse(str.Substring(61, 19), cfi);

					//Linea 2
					str = ifs.ReadLine(); if (str == null) break;
					newTimeInterval.cuc = double.Parse(str.Substring(4, 19), cfi);
					newTimeInterval.ecc = double.Parse(str.Substring(23, 19), cfi);
					newTimeInterval.cus = double.Parse(str.Substring(42, 19), cfi);
					newTimeInterval.roota = double.Parse(str.Substring(61, 19), cfi);

					//Linea 3
					str = ifs.ReadLine(); if (str == null) break;
					newTimeInterval.toe = double.Parse(str.Substring(4, 19), cfi);
					newTimeInterval.cic = double.Parse(str.Substring(23, 19), cfi);
					newTimeInterval.Omega0 = double.Parse(str.Substring(42, 19), cfi);
					newTimeInterval.cis = double.Parse(str.Substring(61, 19), cfi);

					//Linea 4
					str = ifs.ReadLine(); if (str == null) break;
					newTimeInterval.i0 = double.Parse(str.Substring(4, 19), cfi);
					newTimeInterval.crc = double.Parse(str.Substring(23, 19), cfi);
					newTimeInterval.omega = double.Parse(str.Substring(42, 19), cfi);
					newTimeInterval.Omegadot = double.Parse(str.Substring(61, 19), cfi);

					//Linea 5
					str = ifs.ReadLine(); if (str == null) break;
					newTimeInterval.idot = double.Parse(str.Substring(4, 19), cfi);
					//newTimeInterval.codes = double.Parse(str.Substring(23, 19), cfi);
					//newTimeInterval.GPSweek = double.Parse(str.Substring(42, 19), cfi);
					//newTimeInterval.L2flag = double.Parse(str.Substring(61, 19), cfi);

					//Linea 6
					str = ifs.ReadLine(); if (str == null) break;
					//newTimeInterval.svaccur = double.Parse(str.Substring(4, 19), cfi);
					//newTimeInterval.svhealth = double.Parse(str.Substring(23, 19), cfi);
					//newTimeInterval.tgd = double.Parse(str.Substring(42, 19), cfi);

					//Linea 7
					str = ifs.ReadLine(); if (str == null) break;
					//newTimeInterval.tom = double.Parse(str.Substring(4, 19), cfi);
					//newTimeInterval.fit = double.Parse(str.Substring(23, 19), cfi);

					sat.timeIntervals.Add(newTimeInterval);
				}

				Ephemerides_BEIDOU.Add(ephemeris);
			}

			//Ora abbiamo le effemeridi complete del giorno, possiamo prelevare quella relativa al satellite e all'orario e calcolare la posizione del satellite

			//Estraiamo il timeinterval più migliore assai
			//sviluppo
			//svId = 5;
			///sviluppo
			Satellite_Ephemeris_BEIDOU satellite = ephemeris.satellites[svId];
			Satellite_Ephemeris_BEIDOU.TimeInterval bestTimeInterval = new Satellite_Ephemeris_BEIDOU.TimeInterval();
			int leapSeconds = ephemeris.leapSecond;
			int tmin = 86400;
			for (int i = 0; i < satellite.timeIntervals.Count; i++)
			{
				int t = (int)Math.Abs((satellite.timeIntervals[i].startTime_UTC - timestamp).TotalSeconds);
				if (t < tmin)
				{
					tmin = t;
					bestTimeInterval = satellite.timeIntervals[i];
				}
				else
				{
					break;
				}
			}

			double[] loc1, loc2, position;
			if ((svId <= 5) || (svId >= 59))
			{
				position = getSatellitePosition_BEIDOU_GEO(bestTimeInterval, timestamp, leapSeconds);
				loc1 = getSatellitePosition_BEIDOU_GEO(bestTimeInterval, timestamp.AddMilliseconds(100), leapSeconds);
				loc2 = getSatellitePosition_BEIDOU_GEO(bestTimeInterval, timestamp.AddMilliseconds(-100), leapSeconds);
			}
			else
			{
				position = getSatellitePosition_BEIDOU_IGSOMEO(bestTimeInterval, timestamp, leapSeconds);
				loc1 = getSatellitePosition_BEIDOU_IGSOMEO(bestTimeInterval, timestamp.AddMilliseconds(100), leapSeconds);
				loc2 = getSatellitePosition_BEIDOU_IGSOMEO(bestTimeInterval, timestamp.AddMilliseconds(-100), leapSeconds);
			}

			var xv = (loc1[0] - loc2[0]) * 0.5 / 0.1;
			var yv = (loc1[1] - loc2[1]) * 0.5 / 0.1;
			var zv = (loc1[2] - loc2[2]) * 0.5 / 0.1;

			return new double[6] { position[0], position[1], position[2], xv, yv, zv };



		}

		private double[] getSatellitePosition_BEIDOU_GEO(Satellite_Ephemeris_BEIDOU.TimeInterval ti, DateTime t, int leapSeconds)
		{
			int m_day_of_year = t.DayOfYear;
			double gps_sec = (t - gps_time).TotalSeconds + leapSeconds;  //GPOESSE valutare quanti secondi di offset ci sono dopo il 2018
			double gps_days = Math.Floor(gps_sec / SEC_PER_DAY);
			double m_week = Math.Floor(gps_days / 7.0);
			var sow = gps_sec - m_week * 7 * SEC_PER_DAY;
			sow = Math.Round(sow, 1);

			var a = Math.Pow(ti.roota, 2);     // Semi major axis
											   //var tk = Util.check_t(sow - ti.toe);          // tk = sow-@toe
			double tk;
			var tt = sow - ti.toe;
			const double half_week = 302400.0;
			if (tt > half_week) tk = tt - 2 * half_week;
			else if (tt < -half_week) tk = tt + 2 * half_week;
			else tk = tt;

			var n0 = Math.Sqrt(GM_BDS / Math.Pow(a, 3));    // Computed mean motion 
			var n = n0 + ti.deltan;                        // Corrected mean motion
			var m = ti.M0 + n * tk;                        // Mean anomaly

			m = (m + PI2) - PI2 * (int)Math.Floor((m + PI2) / PI2);
			var e = m;
			for (int j = 0; j < 15; j++)
			{
				var e_old = e;
				e = m + ti.ecc * Math.Sin(e_old);
				var dE = (e + e_old) - PI2 * (int)Math.Floor((e - e_old) / PI2);
				if (Math.Abs(dE) < 1.0e-15) break;
			}
			e = (e + PI2) - PI2 * (int)Math.Floor((e + PI2) / PI2);
			var v = Math.Atan2(Math.Sqrt(1.0 - Math.Pow(ti.ecc, 2)) * Math.Sin(e), Math.Cos(e) - ti.ecc);
			var phi = v + ti.omega;
			phi = phi - PI2 * (int)Math.Floor(phi / PI2);
			var phi2 = 2.0 * phi;

			var cosphi2 = Math.Cos(phi2);
			var sinphi2 = Math.Sin(phi2);

			var u = phi + ti.cuc * cosphi2 + ti.cus * sinphi2;
			var r = a * (1.0 - ti.ecc * Math.Cos(e)) + ti.crc * cosphi2 + ti.crs * sinphi2;
			var i = ti.i0 + ti.idot * tk + ti.cic * cosphi2 + ti.cis * sinphi2;
			//var om = ti.Omega0 + (ti.Omegadot - WGS84_EARTH_ROTATION_RATE_GPS) * tk - WGS84_EARTH_ROTATION_RATE_GPS * ti.toe;
			//OMk = block->OMEGA + (block->OMEGADOT - om_eB) * diff - om_eB * block->toe;
			var om = ti.Omega0 + (ti.Omegadot * tk) - (WGS84_EARTH_ROTATION_RATE_BDS * ti.toe);
			om = (om + PI2) - PI2 * (int)Math.Floor((om + PI2) / PI2);

			var x1 = Math.Cos(u) * r;
			var y1 = Math.Sin(u) * r;

			var X = x1 * Math.Cos(om) - y1 * Math.Cos(i) * Math.Sin(om);
			var Y = x1 * Math.Sin(om) + y1 * Math.Cos(i) * Math.Cos(om);
			var Z = y1 * Math.Sin(i);
			var beta = WGS84_EARTH_ROTATION_RATE_BDS * tk;

			var x = X * Math.Cos(beta) + Y * Math.Sin(beta) * Math.Cos(alpha) + Z * Math.Sin(beta) * Math.Sin(alpha);
			var y = X * (-Math.Sin(beta)) + Y * Math.Cos(beta) * Math.Cos(alpha) + Z * Math.Cos(beta) * Math.Sin(alpha);
			var z = Y * (-Math.Sin(alpha)) + Z * Math.Cos(alpha);

			return new double[3] { x, y, z };
		}

		//private double[] getSatellitePosition_BEIDOU_IGSOMEO(Satellite_Ephemeris_BEIDOU.TimeInterval ti, DateTime t, int leapSeconds)
		////private double[] getSatellitePosition_BEIDOU(Satellite_Ephemeris_BEIDOU.TimeInterval ti, DateTime t, int leapSeconds)
		//{
		//	//t = DateTime.SpecifyKind(new DateTime(t.Year, t.Month, t.Day, t.Hour, t.Minute, t.Second), DateTimeKind.Utc); //Data del fix in formato UTC

		//	//int m_day_of_year = t.DayOfYear;
		//	////double bdt_sec = (t - gps_time).TotalSeconds + leapSeconds;
		//	//double bdt_sec = (t - gps_time).TotalSeconds + 4;
		//	//double bdt_days = Math.Floor(bdt_sec / SEC_PER_DAY);
		//	//double m_week = Math.Floor(bdt_days / 7.0);
		//	//var sow = bdt_sec - m_week * 7 * SEC_PER_DAY;
		//	//sow = Math.Round(sow, 1);

		//	var A0 = Math.Pow(ti.roota, 2);     // Semi major axis
		//	double diff = 0;
		//	if (ti.startTime_UTC > t)
		//		diff = (ti.startTime_UTC - t).TotalSeconds;
		//	else
		//	{
		//		diff = (t - ti.startTime_UTC).TotalSeconds;
		//	}
		//	diff += 118;
		//	//TTime Ttoe;
		//	//int DoW;
		//	//if (ti.toe < 0)
		//	//{
		//	//	ti.toe += 604800;
		//	//	ti.GPSweek--;
		//	//}
		//	//else if (ti.toe >= 604800)
		//	//{
		//	//	ti.toe -= 604800;
		//	//	ti.GPSweek++;
		//	//}
		//	//DoW = (int)(ti.toe / 86400.0);
		//	//Ttoe.MJDN = (int)(44244 + ti.GPSweek * 7.0 + DoW);
		//	//Ttoe.SoD = ti.toe - DoW * 86400.0;

		//	//double tk;
		//	//var tt = sow - ti.toe;
		//	//const double half_week = 302400.0;
		//	//if (tt > half_week) tk = tt - 2 * half_week;
		//	//else if (tt < -half_week) tk = tt + 2 * half_week;
		//	//else tk = tt;
		//	//var diff = tk;




		//	//var diff = sow - ti.toe;
		//	var Mk = ti.M0 + (Math.Sqrt(GM_BDS) / (A0 * ti.roota) + ti.deltan) * diff;                        // Mean anomaly
		//	var OMk = ti.OMEGA + (ti.OMEGADOT - om_eB) * diff - om_eB * ti.toe;

		//	const double tol = 1e-14;
		//	double p, p0, p1, p2;
		//	double dd;

		//	Mk = Math.Atan2(Math.Sin(Mk), Math.Cos(Mk));
		//	p = Mk;
		//	while (true)
		//	{
		//		p0 = p;
		//		p1 = Mk + ti.ecc * Math.Sin(p0);
		//		p2 = Mk + ti.ecc * Math.Sin(p1);

		//		dd = Math.Abs(p2 - 2 * p1 + p0);
		//		if (dd < tol) break;
		//		p = p0 - (p1 - p0) * (p1 - p0) / (p2 - 2 * p1 + p0);
		//		if (Math.Abs(p - p0) <= tol) break;
		//	}
		//	var Ek = p;

		//	if (Ek < 0) Ek += 2 * Math.PI;
		//	if (Ek > 2 * Math.PI) Ek -= 2 * Math.PI;
		//	var fk = Math.Atan2(Math.Sqrt(1 - ti.ecc * ti.ecc) * Math.Sin(Ek), Math.Cos(Ek) - ti.ecc);
		//	if (fk < 0) fk += 2 * Math.PI;
		//	if (fk > 2 * Math.PI) fk -= 2 * Math.PI;

		//	var uk = ti.OMEGA + fk + ti.cuc * Math.Cos(2 * (ti.OMEGA + fk)) + ti.cus * Math.Sin(2 * (ti.OMEGA + fk));
		//	var rk = A0 * (1.0 - ti.ecc * Math.Cos(Ek)) + ti.crc * Math.Cos(2.0 * (ti.OMEGA + fk)) + ti.crs * Math.Sin(2.0 * (ti.OMEGA + fk));
		//	var ik = ti.i0 + ti.idot * diff + ti.cic * Math.Cos(2 * (ti.OMEGA + fk)) + ti.cis * Math.Sin(2 * (ti.OMEGA + fk));

		//	var xp = rk * Math.Cos(uk);
		//	var yp = rk * Math.Sin(uk);

		//	var x = xp * Math.Cos(OMk) - yp * Math.Cos(ik) * Math.Sin(OMk);
		//	var y = xp * Math.Sin(OMk) + yp * Math.Cos(ik) * Math.Cos(OMk);

		//	var z = yp * Math.Sin(ik);

		//	return new double[3] { x, y, z };
		//}

		private double[] getSatellitePosition_BEIDOU_IGSOMEO(Satellite_Ephemeris_BEIDOU.TimeInterval ti, DateTime t, int leapSeconds)
		{
			//t = DateTime.SpecifyKind(new DateTime(t.Year, t.Month, t.Day, t.Hour, t.Minute, t.Second), DateTimeKind.Utc); //Data del fix in formato UTC

			int m_day_of_year = t.DayOfYear;
			double gps_sec = (t - gps_time).TotalSeconds + leapSeconds;  //GPOESSE valutare quanti secondi di offset ci sono dopo il 2018
			double gps_days = Math.Floor(gps_sec / SEC_PER_DAY);
			double m_week = Math.Floor(gps_days / 7.0);
			var sow = gps_sec - m_week * 7 * SEC_PER_DAY;
			sow = Math.Round(sow, 1);

			var a = Math.Pow(ti.roota, 2);     // Semi major axis
											   //var tk = Util.check_t(sow - ti.toe);          // tk = sow-@toe
			double tk;
			var tt = sow - ti.toe;
			const double half_week = 302400.0;
			if (tt > half_week) tk = tt - 2 * half_week;
			else if (tt < -half_week) tk = tt + 2 * half_week;
			else tk = tt;

			var n0 = Math.Sqrt(GM_BDS / Math.Pow(a, 3));    // Computed mean motion 
			var n = n0 + ti.deltan;                        // Corrected mean motion
			var m = ti.M0 + n * tk;                        // Mean anomaly

			m = (m + PI2) - PI2 * (int)Math.Floor((m + PI2) / PI2);
			var e = m;
			for (int j = 0; j < 15; j++)
			{
				var e_old = e;
				e = m + ti.ecc * Math.Sin(e_old);
				var dE = (e + e_old) - PI2 * (int)Math.Floor((e - e_old) / PI2);
				if (Math.Abs(dE) < 1.0e-15) break;
			}
			e = (e + PI2) - PI2 * (int)Math.Floor((e + PI2) / PI2);
			var v = Math.Atan2(Math.Sqrt(1.0 - Math.Pow(ti.ecc, 2)) * Math.Sin(e), Math.Cos(e) - ti.ecc);
			var phi = v + ti.omega;
			phi = phi - PI2 * (int)Math.Floor(phi / PI2);
			var phi2 = 2.0 * phi;

			var cosphi2 = Math.Cos(phi2);
			var sinphi2 = Math.Sin(phi2);

			var u = phi + ti.cuc * cosphi2 + ti.cus * sinphi2;
			var r = a * (1.0 - ti.ecc * Math.Cos(e)) + ti.crc * cosphi2 + ti.crs * sinphi2;
			var i = ti.i0 + ti.idot * tk + ti.cic * cosphi2 + ti.cis * sinphi2;
			var om = ti.Omega0 + (ti.Omegadot - WGS84_EARTH_ROTATION_RATE_BDS) * tk - WGS84_EARTH_ROTATION_RATE_BDS * ti.toe;
			//om = Util.rem2pi(om + Util.PI2);
			om = (om + PI2) - PI2 * (int)Math.Floor((om + PI2) / PI2);
			var x1 = Math.Cos(u) * r;
			var y1 = Math.Sin(u) * r;

			var x = x1 * Math.Cos(om) - y1 * Math.Cos(i) * Math.Sin(om);
			var y = x1 * Math.Sin(om) + y1 * Math.Cos(i) * Math.Cos(om);

			var z = y1 * Math.Sin(i);

			return new double[3] { x, y, z };
		}

		private List<byte> decodeTimeStamp(ref byte[] gp6, ref TimeStamp t)
		{

			//Cerca un timestamp o un nostamp
			List<byte> bufferNoStamp = new List<byte>();
			byte test = gp6[ramPos];
			int max = gp6.Length - 1;
			t.txtAllowed = 0;
			t.kmlAllowed = false;
			t.rawPreset = false;
			t.pos = gp6Pos;
			//if (pref_debugLevel > 0) t.txtAllowed++;
			while (true)
			{
				while ((test != 0xab) & (ramPos < max))
				{
					ramPos++;
					test = gp6[ramPos];
				}
				//if (test == 0xff | test == 0x0a)
				if (test == 0x0a | ramPos == max)
				{
					List<byte> lout = new List<byte>();
					lout.Add(0xff);
					return lout;
				}
				if (test == 0xac)
				{
					//no stamp: 0xAC seguito da due byte di buffersize b e b byte di buffer
					int noSize = gp6[ramPos + 1] * 256 + gp6[ramPos + 2];
					ramPos += 3;
					bufferNoStamp.AddRange(gp6.Skip(ramPos).Take(noSize));
					ramPos += noSize;
					test = gp6[ramPos];
					continue;
				}
				else
				{
					break;
				}
			}

			ramPos++;
			t.tsType = gp6[ramPos];
			ramPos++;
			t.tsTypeExt1 = t.tsTypeExt2 = 0;
			if ((t.tsType & ts_ext1) == ts_ext1)
			{
				t.tsTypeExt1 = gp6[ramPos];
				ramPos++;
			}
			if ((t.tsTypeExt1 & ts_ext2) == ts_ext2)
			{
				t.tsTypeExt2 = gp6[ramPos];
				ramPos++;
			}

			//Inserire Pressione
			//Inserire Temperatura

			//Batteria
			if ((t.tsType & ts_battery) == ts_battery)
			{
				t.batteryLevel = gp6[ramPos] * 256;
				t.batteryLevel += gp6[ramPos + 1];
				t.batteryLevel = (t.batteryLevel * 6) / 4096; //Rimettere *6 dopo sviluppo
				ramPos += 2;
				//if (pref_battery)
				//{
				t.txtAllowed++;
				//}
			}

			//Coordinata
			if ((t.tsType & ts_coordinate) == ts_coordinate)
			{
				int llon;
				llon = (gp6[ramPos] & 0x7f) << 24;
				llon += gp6[ramPos + 1] << 16;
				llon += gp6[ramPos + 2] << 8;
				llon += gp6[ramPos + 6] & 0xc0;
				if ((gp6[ramPos] & 0x80) == 0x80)
				{
					llon = -llon;
				}
				t.lon = llon / 10000000.0;

				int llat;
				llat = (gp6[ramPos + 3] & 0x3f) << 24;
				llat += gp6[ramPos + 4] << 16;
				llat += gp6[ramPos + 5] << 8;
				llat += (gp6[ramPos + 6] & 0x30) << 2;
				if ((gp6[ramPos + 3] & 0x40) == 0x40)
				{
					llat = -llat;
				}
				t.lat = llat / 10000000.0;

				t.altitude = (gp6[ramPos + 6] & 0x0f) << 8;
				t.altitude = t.altitude + gp6[ramPos + 7] - 191;

				t.hAcc = (gp6[ramPos + 8] & 0xe0) >> 5;
				t.vAcc = (gp6[ramPos + 8] & 0x1c) >> 2;

				t.speed = (gp6[ramPos + 8] & 3) << 6;
				t.speed += (gp6[ramPos + 9] & 0xfc) >> 2;
				t.speed *= 0.9;

				t.cog = (gp6[ramPos + 9] & 3) << 2;
				if ((gp6[ramPos + 10] & 0x80) == 0x80)
				{
					t.cog += 2;
				}
				if ((gp6[ramPos + 3] & 0x80) == 0x80)
				{
					t.cog += 1;
				}
				t.cog *= 23;
				t.cog += 11;

				t.GPS_second = gp6[ramPos + 10] & 0x3f;
				ramPos += 11;
				t.sat = 8;  //forzata per il kml
				t.txtAllowed++;
				t.kmlAllowed = true;
			}

			//Evento
			if ((t.tsType & ts_event) == ts_event)
			{
				t.eventAr = new byte[10];
				int evLength = gp6[ramPos] + 2;
				Array.Copy(gp6, ramPos, t.eventAr, 0, evLength);
				ramPos += evLength;
				if (pref_metadata)
				{
					t.txtAllowed++;
				}
			}

			//Inserire Flag Attività
			//Inserire Accelerometro

			//if ((t.tsTypeExt1 == 0) & (t.tsTypeExt2 == 0))
			//{
			//	return bufferNoStamp;
			//}

			//Raw
			if ((t.tsTypeExt1 & ts_raw) == ts_raw)
			{
				int length = (gp6[ramPos] * 11) + 1;
				t.raw = new byte[length];
				Array.Copy(gp6, ramPos, t.raw, 0, length);
				ramPos += length;
				t.rawPreset = true;
			}

			//Info
			if ((t.tsTypeExt1 & ts_info) == ts_info)
			{
				byte infoLength = gp6[ramPos];
				ramPos++;
				t.infoAr = new byte[infoLength];
				Array.Copy(gp6, ramPos, t.infoAr, 0, infoLength);
				if (infoLength > 3)
				{
					rfAddress = t.infoAr[3] * 65536 + t.infoAr[4] * 256 + t.infoAr[5];
					if (rfAddress != 0 && rfAddress != 0xffffff)
					{
						lastKnownRfAddressString = rfAddress.ToString();
					}
				}
				if (infoLength > 6)
				{
					byte[] nomeArr = new byte[28];
					Array.Copy(t.infoAr, 6, nomeArr, 0, 28);
					lastKnownUnitName = Encoding.ASCII.GetString(nomeArr);//
					lastKnownUnitName = lastKnownUnitName.Split('\0')[0];
				}
				t.unitNameTxt = lastKnownUnitName;
				t.rfAddressString = lastKnownRfAddressString;
				ramPos += infoLength;
			}

			//Inserire Magnetometro

			//Data e Ora
			if ((t.tsTypeExt1 & ts_time) == ts_time)
			{
				DateTime oldDate = t.dateTime;
				byte second = gp6[ramPos];
				int year, month, day, hour, minute;
				bool timeAndDate = false;
				if (second > 0x7f)
				{
					timeAndDate = true;
					second -= 0x80;
					year = gp6[ramPos + 6] * 256 + gp6[ramPos + 5];
					month = gp6[ramPos + 4];
					day = gp6[ramPos + 3];
					hour = gp6[ramPos + 2];
					minute = gp6[ramPos + 1];
					ramPos += 7;
				}
				else
				{
					year = t.dateTime.Year;
					month = t.dateTime.Month;
					day = t.dateTime.Day;
					hour = gp6[ramPos + 2];
					minute = gp6[ramPos + 1];
					//t.dateTime = new DateTime(t.dateTime.Year, t.dateTime.Month, t.dateTime.Day, hour, minute, second);
					ramPos += 3;
				}
				try
				{
					t.dateTime = new DateTime(year, month, day, hour, minute, second);
				}
				catch
				{
					t.dateTime = new DateTime(1000, 1, 1, 0, 0, 0);
				}

				if ((t.dateTime < oldDate.AddHours(-1)) && !timeAndDate)
				{
					t.dateTime = t.dateTime.AddDays(1);
				}
			}
			else
			{
				t.dateTime.AddSeconds(1);
			}

			//Prossimità
			t.proximityAddress = 0;
			if ((t.tsTypeExt1 & ts_proximity) == ts_proximity)
			{
				int proxLength = gp6[ramPos];
				t.proximityAddress = gp6[ramPos + 1] * 65536 + gp6[ramPos + 2] * 256 + gp6[ramPos + 3];
				if (proxLength > 3)
				{
					t.proximityPower = (sbyte)gp6[ramPos + 4];
					ramPos += 1;
				}
				ramPos += 4;
				if (pref_proximity)
				{
					t.txtAllowed++;
				}
			}

			//Controlla se il timestamp ha una data superiore al precedente convertito; in caso negativo ne impedisce la scrittura nel txt
			//DEVE RIMANERE IN FONDO AL DECODE TIMESTAMP
			if (pref_debugLevel == 0)
			{
				if (t.dateTime <= parent.convertingStartDate)
				{
					t.txtAllowed = 0;
				}
				else
				{
					parent.convertingStartDate = t.dateTime;
				}
			}

			return bufferNoStamp;

		}

		private string decodeEvent(byte[] eventAr)
		{
			string outs;
			switch ((eventType)eventAr[1])
			{
				case eventType.E_SCHEDULE:

					if (eventAr[2] == 3)
					{
						outs = "GPS Schedule: OFF";
					}
					else
					{
						try
						{
							outs = string.Format(events[eventAr[1]], eventAr[3], scheduleEventTimings[eventAr[2]]);
						}
						catch
						{
							outs = "B_EVENT";
							break;
						}
					}
					if ((eventAr[2] != 3) && (eventAr[0] == 3))
					{
						outs += " / Geofencing " + eventAr[4].ToString();
					}
					break;
				case eventType.E_SD_START:
					outs = events[eventAr[1]];
					break;
				case eventType.E_ALTON_TIMEOUT:
					outs = String.Format(events[eventAr[1]], eventAr[2]);
					break;
				default:
					outs = events[eventAr[1]];
					break;
			}
			return outs;
		}

		public override void Dispose()
		{
			base.Dispose();
		}


	}

}
