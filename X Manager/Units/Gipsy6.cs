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

		protected int p_fileCsv_name = 0;
		protected int p_fileCsv_rfAddress = 1;
		protected int p_fileCsv_date = 2;
		protected int p_fileCsv_time = 3;
		protected int p_fileCsv_latitude = 4;
		protected int p_fileCsv_longitude = 5;
		protected int p_fileCsv_horizontalAccuracy = 6;
		protected int p_fileCsv_altitude = 7;
		protected int p_fileCsv_verticalAccuracy = 8;
		protected int p_fileCsv_speed = 9;
		protected int p_fileCsv_course = 10;
		protected int p_fileCsv_battery = 11;
		protected int p_fileCsv_proximity = 12;
		protected int p_fileCsv_proximityPower = 13;
		protected int p_fileCsv_event = 14;
		protected int p_fileCsv_position = 15;
		protected int p_fileCsv_length = 16;

		protected Gipsy6(object p)
					: base(p)
		{
		}

		public override void convert(string fileName, string[] prefs)
		{
			if (prefs[p_filePrefs_pressMetri] == "meters") pref_inMeters = true;
			pref_pressOffset = double.Parse(prefs[p_filePrefs_millibars]);

			pref_debugLevel = parent.stDebugLevel;
			pref_addGpsTime = parent.addGpsTime;
			if (Parent.getParameter("pressureRange") == "air")
			{
				pref_isDepth = false;
			}
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
			if (pref_addGpsTime)
			{
				pref_repeatEmptyValues = false;
				pref_sameColumn = true;
			}
			if (prefs[p_filePrefs_txt] == "True") pref_makeTxt = true;
			if (prefs[p_filePrefs_kml] == "True") pref_makeKml = true;
			if (prefs[p_filePrefs_battery] == "True") pref_battery = true;

			if (prefs[p_filePrefs_timeFormat] == "2") pref_angloTime = true;

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
			if (prefs[p_filePrefs_proximity] == "True") pref_proximity = true;
			pref_leapSeconds = int.Parse(prefs[p_filePrefs_leapSeconds]);
			pref_removeNonGps = bool.Parse(prefs[p_filePrefs_removeNonGps]);

		}

		protected void placeHeader(StreamWriter txtBW)//, ref byte[] columnPlace)
		{

			var headers = new List<string>() { "Name", "RF address", "Date", "Time", "Latitude", "Longitude", "Horizontal Acc.", "Altitude", "Vertical Acc.", "Speed", "Course", "Battery", "Nearby device",
										"Nearby device signal strenght", "Event", "gp6Pos" };

			string dateHeader = "Date";
			if (this is Gipsy6XS) headers.Remove("RF address");
			if (pref_sameColumn)
			{
				headers.Remove("Time");
				headers[headers.IndexOf("Date")] = "Timestamp";
				dateHeader = "Timestamp";
			}
			if (!pref_battery) headers.Remove("Battery");
			if (!pref_metadata) headers.Remove("Event");
			if (pref_debugLevel == 0) headers.Remove("gp6Pos");
			if (!pref_proximity)
			{
				headers.Remove("Nearby device");
				headers.Remove("Nearby device signal strenght");
			}

			p_fileCsv_date = headers.IndexOf(dateHeader);
			p_fileCsv_time = headers.IndexOf("Time");
			p_fileCsv_latitude = headers.IndexOf("Latitude");
			p_fileCsv_longitude = headers.IndexOf("Longitude");
			p_fileCsv_horizontalAccuracy = headers.IndexOf("Horizontal Acc.");
			p_fileCsv_altitude = headers.IndexOf("Altitude");
			p_fileCsv_verticalAccuracy = headers.IndexOf("Vertical Acc.");
			p_fileCsv_speed = headers.IndexOf("Speed");
			p_fileCsv_course = headers.IndexOf("Course");
			p_fileCsv_battery = headers.IndexOf("Battery");
			p_fileCsv_proximity = headers.IndexOf("Nearby device");
			p_fileCsv_proximityPower = headers.IndexOf("Nearby device signal strenght");
			p_fileCsv_event = headers.IndexOf("Event");
			p_fileCsv_position = headers.IndexOf("gp6Pos");
			p_fileCsv_length = headers.Count;

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
