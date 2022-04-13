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
		public Label etaLabel;
		public System.IO.Ports.SerialPort sp;

		public static string[] lastSettings;

		public string csvSeparator;
		public byte stDebugLevel = 0;
		public bool stOldUnitDebug;
		public bool remote;
		public bool addGpsTime;

		public string ftdiSerialNumber;
		public abstract void nextFile();
		public abstract void downloadFailed();
		public abstract void downloadFinished();

		public virtual string getStatusLabelContent()
		{
			return "";
		}

	}
}
