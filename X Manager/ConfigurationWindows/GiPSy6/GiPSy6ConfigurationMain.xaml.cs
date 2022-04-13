using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using X_Manager.ConfigurationWindows;

namespace X_Manager.ConfigurationWindows
{
	/// <summary>
	/// Logica di interazione per GiPSy6ConfigurationMain.xaml
	/// </summary> 
	public partial class GiPSy6ConfigurationMain : ConfigurationWindow
	{
		int pagePointer = 0;
		BasicsConfiguration basicsConf;
		ScheduleConfiguration schedConf;
		GeofencigConfiguration geoConf1;
		GeofencigConfiguration geoConf2;
		PageCopy[] pages;
		bool[] pagesEnabled;
		public volatile List<SBitmap> sbitmapAr;
		public volatile List<string> bitnameAr;
		byte unitType;
		public string appDataPath;
		public bool conn;
		public bool expertMode;
		int lastIndex;
		int firstIndex;
		//byte[] conf;
		public GiPSy6ConfigurationMain(byte[] conf, byte unitType)
		{
			InitializeComponent();

			expertCB.IsChecked = bool.Parse(MainWindow.getParameter("gipsy6ConfigurationExpertMode", "false"));

			appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\TechnoSmArt Europe\\X Manager\\map cache\\";
			System.IO.Directory.CreateDirectory(appDataPath);

			conn = false;
			try
			{
				using (var client = new WebClient())
				using (client.OpenRead("http://google.com/generate_204"))
					conn = true;
			}
			catch
			{ }

			sbitmapAr = new List<SBitmap>();
			bitnameAr = new List<string>();
			foreach (string filename in System.IO.Directory.GetFiles(appDataPath, "*.png"))
			{
				using (SBitmap sb = new SBitmap())
				{
					sbitmapAr.Add(new SBitmap(filename));
				}
				bitnameAr.Add(System.IO.Path.GetFileName(filename));
			}

			axyConfOut = conf;
			basicsConf = new BasicsConfiguration(axyConfOut);
			schedConf = new ScheduleConfiguration(axyConfOut);
			geoConf1 = new GeofencigConfiguration(axyConfOut, 1, this);
			geoConf2 = new GeofencigConfiguration(axyConfOut, 2, this);
			backB.IsEnabled = false;
			this.unitType = unitType;
			pages = new PageCopy[] { schedConf, basicsConf, geoConf1, geoConf2 };
			pagesEnabled = new bool[] { true, false, false, false };
			lastIndex = 0;
			firstIndex = 0;
			forthB.Content = "SEND";
			if ((bool)expertCB.IsChecked)
			{
				pagesEnabled = new bool[] { true, true, true, true };
				forthB.Content = "-->";
				lastIndex = 3;
			}
			Gipsy6ConfigurationBrowser.Content = pages[0];
		}

		private void loaded(object sender, RoutedEventArgs e)
		{
			Closed += closing;
		}

		private void backClick(object sender, RoutedEventArgs e)
		{
			forthB.Content = "-->";

			clearHistory();
			backB.IsEnabled = true;
			forthB.IsEnabled = true;

			PageCopy p;
			p = pages[pagePointer];
			p.copyValues();

			do
			{
				pagePointer--;
			} while (pagesEnabled[pagePointer] == false);

			if (pagePointer == firstIndex)
			{
				backB.IsEnabled = false;
			}
			Gipsy6ConfigurationBrowser.Content = pages[pagePointer];
		}

		private void forthClick(object sender, RoutedEventArgs e)
		{
			if ((string)forthB.Content == "SEND")
			{
				pages[lastIndex].copyValues();
				mustWrite = true;
				Close();
				return;
			}

			clearHistory();
			backB.IsEnabled = true;
			forthB.IsEnabled = true;

			PageCopy p;
			p = pages[pagePointer];
			p.copyValues();

			do
			{
				pagePointer++;
			} while (pagesEnabled[pagePointer] == false);

			if (pagePointer == lastIndex)
			{
				forthB.Content = "SEND";
			}
			Gipsy6ConfigurationBrowser.Content = pages[pagePointer];
		}

		private void closing(object sender, EventArgs e)
		{
			//geoConf1.saveCache();
			if (conn)
			{
				GeofencigConfiguration.saveCache(bitnameAr, sbitmapAr, appDataPath);
			}
		}

		private void clearHistory()
		{
			try
			{

				var je = Gipsy6ConfigurationBrowser.NavigationService.RemoveBackEntry();
				while (je != null)
				{
					je = Gipsy6ConfigurationBrowser.NavigationService.RemoveBackEntry();
				}
			}
			catch
			{
			}
		}

		private void expertCB_Checked(object sender, RoutedEventArgs e)
		{
			MainWindow.updateParameter("gipsy6ConfigurationExpertMode", "true");

			pagesEnabled = new bool[] { true, true, true, true };
			for (int i = pagesEnabled.Length - 1; i >= 0; i--)
			{
				lastIndex = i;
				if (pagesEnabled[i] == true)
				{
					break;
				}
			}
			for (int i = 0; i < pagesEnabled.Length; i++)
			{
				firstIndex = i;
				if (pagesEnabled[i] == true)
				{
					break;
				}
			}
			backB.IsEnabled = !(pagePointer == firstIndex);
			forthB.Content = "-->";
			if (pagePointer == lastIndex)
			{
				forthB.Content = "SEND";
			}
		}

		private void expertCB_Unchecked(object sender, RoutedEventArgs e)
		{
			MainWindow.updateParameter("gipsy6ConfigurationExpertMode", "false");

			pagesEnabled = new bool[] { true, false, false, false };

			for (int i = pagesEnabled.Length - 1; i >= 0; i--)
			{
				lastIndex = i;
				if (pagesEnabled[i] == true)
				{
					break;
				}
			}
			for (int i = 0; i < pagesEnabled.Length; i++)
			{
				firstIndex = i;
				if (pagesEnabled[i] == true)
				{
					break;
				}
			}
			for (int i = 0; i < pagesEnabled.Length; i++)
			{
				if (pagesEnabled[i] == false)
				{
					pages[i].disable();
				}
			}

			if (pagesEnabled[pagePointer] == false)
			{
				Gipsy6ConfigurationBrowser.Content = pages[firstIndex];
				pagePointer = firstIndex;
			}
			if (pagePointer == firstIndex)
			{
				backB.IsEnabled = false;
			}
			if (pagePointer == lastIndex)
			{
				forthB.Content = "SEND";
			}
		}
	}
}
