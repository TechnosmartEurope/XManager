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
		public volatile List<SBitmap> sbitmapAr;
		public volatile List<string> bitnameAr;
		byte unitType;
		public string appDataPath;
		public bool conn;
		//byte[] conf;
		public GiPSy6ConfigurationMain(byte[] conf, byte unitType)
		{
			InitializeComponent();

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
			pages = new PageCopy[] { basicsConf, schedConf, geoConf1, geoConf2 };
			Gipsy6ConfigurationBrowser.Content = pages[0];

		}

		private void loaded(object sender, RoutedEventArgs e)
		{
			Closed += closing;
		}

		private void backClick(object sender, RoutedEventArgs e)
		{
			forthB.Content = "-->";

			PageCopy p;
			clearHistory();
			backB.IsEnabled = true;
			forthB.IsEnabled = true;

			p = pages[pagePointer];
			p.copyValues();

			pagePointer--;
			if (pagePointer == 0)
			{
				backB.IsEnabled = false;
			}
			Gipsy6ConfigurationBrowser.Content = pages[pagePointer];
		}

		private void forthClick(object sender, RoutedEventArgs e)
		{
			if ((string)forthB.Content == "SEND")
			{
				geoConf2.copyValues();
				mustWrite = true;
				Close();
				return;
			}

			PageCopy p;
			clearHistory();
			backB.IsEnabled = true;
			forthB.IsEnabled = true;

			p = pages[pagePointer];
			p.copyValues();

			pagePointer++;
			if (pagePointer == (pages.Length - 1))
			{
				//forthB.IsEnabled = false;
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


	}
}
