using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace X_Manager
{
	public abstract class Parent : System.Windows.Window
	{
		public Label statusLabel;
		public Button progressBarStopButton;
		public ColumnDefinition progressBarStopButtonColumn;
		public ProgressBar statusProgressBar;
		public ProgressBar txtProgressBar;
		public ProgressBar kmlProgressBar;
		public Label etaLabel;

		public static string[] settings;

		public string csvSeparator;
		public byte stDebugLevel = 0;
		public bool stOldUnitDebug;
		public bool remote;
		public bool addGpsTime;

		public DateTime convertingStartDate;
		public string convertingFileName;

		public string ftdiSerialNumber;
		public abstract void nextFile();

		public abstract void downloadFailed();

		public abstract void downloadIncomplete();

		public abstract void downloadFinished();
		public virtual unsafe string setLatency(string portName, byte latency)
		{
			return "NULL";
		}
		public virtual string getStatusLabelContent()
		{
			return "";
		}

		public static string getParameter(string parName)
		{
			string parOut = "";
			for (int i = 0; i < settings.Length; i++)
			{
				string par = settings[i];
				if (parName == par.Split('=')[0])
				{
					parOut = par.Split('=')[1];
					break;
				}
			}
			//foreach (string par in settings)
			//{
			//	if (parName == par.Split('=')[0])
			//	{
			//		parOut = par.Split('=')[1];
			//		break;
			//	}
			//}
			return parOut;
		}

		public static string getParameter(string parName, string defaultValue)
		{
			string parOut = "";
			foreach (string par in settings)
			{
				if (parName == par.Split('=')[0])
				{
					parOut = par.Split('=')[1];
					break;
				}
			}
			if (parOut == "")
			{
				Array.Resize(ref settings, settings.Length + 1);
				settings[settings.Length - 1] = parName + "=" + defaultValue;
				System.IO.File.Delete(MainWindow.iniFile);
				System.IO.File.WriteAllLines(MainWindow.iniFile, settings);
				parOut = defaultValue;
			}
			return parOut;
		}
		public static void updateParameter(string parName, string parValue)
		{
			for (int i = 0; i < settings.Length; i++)
			{
				if (parName == settings[i].Split('=')[0])
				{
					settings[i] = parName + "=" + parValue;
					System.IO.File.WriteAllLines(MainWindow.iniFile, settings);
					return;
				}
			}
			Array.Resize(ref settings, settings.Length + 1);
			settings[settings.Length - 1] = parName + "=" + parValue;
			System.IO.File.WriteAllLines(MainWindow.iniFile, settings);
		}

	}
}
