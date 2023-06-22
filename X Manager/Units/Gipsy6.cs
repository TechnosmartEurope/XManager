using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace X_Manager.Units.Gipsy6
{
	public abstract class Gipsy6 : Unit
	{

		protected string unitName = "";
		protected string lastKnownUnitName = "";
		//protected byte[] columnPlace = new byte[17];
		//protected enum COLUMN : int
		//{
		//	COL_NAME = 0,
		//	COL_RF_ADDRESS,
		//	COL_DATE,
		//	COL_TIME,
		//	COL_LATITUDE,
		//	COL_LONGITUDE,
		//	COL_HORIZONTAL_ACCURACY,
		//	COL_ALTITUDE,
		//	COL_VERTICAL_ACCURACY,
		//	COL_SPEED,
		//	COL_COURSE,
		//	COL_BATTERY,
		//	COL_PROXIMITY,
		//	COL_PROXIMITY_POWER,
		//	COL_EVENT,
		//	COL_POSITION_IN_FILE,
		//	COL_LENGTH
		//}

		protected int p_name = 0;
		protected int p_rfAddress = 1;
		protected int p_date = 2;
		protected int p_time = 3;
		protected int p_latitude = 4;
		protected int p_longitude = 5;
		protected int p_horizontalAccuracy = 6;
		protected int p_altitude = 7;
		protected int p_verticalAccuracy = 8;
		protected int p_speed = 9;
		protected int p_course = 10;
		protected int p_battery = 11;
		protected int p_proximity = 12;
		protected int p_proximityPower = 13;
		protected int p_event = 14;
		protected int p_position = 15;
		protected int p_length = 16;

		//protected string[] headers = {"Name", "RF address", "Date", "Time", "Latitude", "Longitude", "Horizontal Acc.", "Altitude", "Vertical Acc.", "Speed", "Course", "Battery", "Nearby device",
		//								"Nearby device signal strenght", "Event", "gp6Pos" };
		protected Gipsy6(object p)
					: base(p)
		{
		}

		public override void convert(string fileName, string[] prefs)
		{
			if (prefs[pref_pressMetri] == "meters") inMeters = true;
			pressOffset = double.Parse(prefs[pref_millibars]);

			debugLevel = parent.stDebugLevel;
			addGpsTime = parent.addGpsTime;
			if (Parent.getParameter("pressureRange") == "air")
			{
				isDepth = false;
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
			if (prefs[pref_override_time] == "True") overrideTime = true;
			if (prefs[pref_metadata] == "True") metadata = true;
			leapSeconds = int.Parse(prefs[pref_leapSeconds]);
			removeNonGps = bool.Parse(prefs[pref_removeNonGps]);
		}

		protected void placeHeader(StreamWriter txtBW)//, ref byte[] columnPlace)
		{
			//txtBW.Write("\tLatitude (deg)\tLongitude (deg)\tHor. Acc. (m)" +
			//							"\tAltitude (m)\tVert. Acc. (m)\tSpeed (km/h)\tCourse (deg)\tBattery (v)\tNearby Device\tNearby Device Received Signal Strength\tEvent\r\n");

			//txtBW.Write("\tLat\tLon\tHorAcc (m)" +
			//										"\tAlt (m)\tVertAcc (m)\tSpeed (km/h)\tCourse\tBatt (v)\tNearby\tStrength\tEvent\r\n");

			var headers = new List<string>() { "Name", "RF address", "Date", "Time", "Latitude", "Longitude", "Horizontal Acc.", "Altitude", "Vertical Acc.", "Speed", "Course", "Battery", "Nearby device",
										"Nearby device signal strenght", "Event", "gp6Pos" };

			string dateHeader = "Date";
			if (this is Gipsy6XS) headers.Remove("RF address");
			if (sameColumn)
			{
				headers.Remove("Time");
				headers[headers.IndexOf("Date")] = "Timestamp";
				dateHeader = "Timestamp";
			}
			if (!prefBattery) headers.Remove("Battery");
			if (!metadata) headers.Remove("Event");
			if (debugLevel < 3) headers.Remove("gp6Pos");

			p_date = headers.IndexOf(dateHeader);
			p_time = headers.IndexOf("Time");
			p_latitude = headers.IndexOf("Latitude");
			p_longitude = headers.IndexOf("Longitude");
			p_horizontalAccuracy = headers.IndexOf("Horizontal Acc.");
			p_altitude = headers.IndexOf("Altitude");
			p_verticalAccuracy = headers.IndexOf("Vertical Acc.");
			p_speed = headers.IndexOf("Speed");
			p_course = headers.IndexOf("Course");
			p_battery = headers.IndexOf("Battery");
			p_proximity = headers.IndexOf("Nearby device");
			p_proximityPower = headers.IndexOf("Nearby device signal strenght");
			p_event = headers.IndexOf("Event");
			p_position = headers.IndexOf("gp6Pos");
			p_length = headers.Count;

			//byte place = 0;
			//for (COLUMN i = 0; i < COLUMN.COL_LENGTH; i++)
			//{
			//	columnPlace[(int)i] = place;
			//	switch (i)
			//	{
			//		case COLUMN.COL_DATE: if (!sameColumn) place++; break;
			//		case COLUMN.COL_BATTERY: if (prefBattery) place++; break;
			//		case COLUMN.COL_EVENT: if (metadata) place++; break;
			//		case COLUMN.COL_POSITION_IN_FILE: if (debugLevel >= 3) place++; break;
			//		default: place++; break;
			//	}
			//}
			//columnPlace[(int)COLUMN.COL_LENGTH] = place++;

			//var heads = new List<string>();
			//foreach (string s in headers)
			//{
			//	heads.Add(s);
			//}

			//if (debugLevel < 3) heads.RemoveAt(15);
			//if (!metadata) heads.RemoveAt(14);
			//if (!prefBattery) heads.RemoveAt(11);
			//if (sameColumn)
			//{
			//	heads.RemoveAt(3);
			//	heads[2] = "Timestamp";
			//}
			for (int i = 0; i < headers.Count - 1; i++)
			{
				txtBW.Write(headers[i] + "\t");
			}
			txtBW.Write(headers[headers.Count - 1] + "\r\n");

		}

	}
}
