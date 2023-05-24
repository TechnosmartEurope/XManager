using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using X_Manager.Units.Gipsy6;

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
		//byte unitType;
		string appDataPath;
		public bool conn;
		//bool expertMode;
		int lastIndex;
		int firstIndex;
		//uint firmware;
		Units.Unit unit;
		public bool lockRfAddress
		{
			get
			{
				return basicsConf.lockRfAddress;
			}
			set
			{
				basicsConf.lockRfAddress = value;
			}
		}

		DispatcherTimer windowMovingTimer = new DispatcherTimer();

		public GiPSy6ConfigurationMain(byte[] conf, Units.Unit unit, MainWindow mw)
		{

			InitializeComponent();
			
			this.unit = unit;
			if (unit is null)
			{
				unit = new Gipsy6N(mw);
				unit.firmTotA = 999999999;
			}

			if (unit is Gipsy6N)
			{
				if (conf[58] < 15 || conf[58] > 60)
				{
					conf[58] = 15;
				}
				if (unit.firmTotA < 1004007)
				{
					conf[58] = 15;
				}
				if (conf[541] == 0x00 && conf[542] == 0x02 && conf[543] == 0x2b)
				{
					Title += " (d.c.)";
				}
			}

			expertCB.IsChecked = bool.Parse(X_Manager.Parent.getParameter("gipsy6ConfigurationExpertMode", "false"));

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
			axyConfOut = conf;
			//this.unitType = unitType;
			sbitmapAr = new List<SBitmap>();
			bitnameAr = new List<string>();

			//Carica in cache i file png salvati su disco. Se sono più di 1024, elimina quelli in più.
			string[] fileNames = System.IO.Directory.GetFiles(appDataPath, "*.png");
			int maxF = Math.Max(1024, fileNames.Length);
			for (int i = 0; i < Math.Min(fileNames.Length, 1024); i++)
			{
				sbitmapAr.Add(new SBitmap(fileNames[i]));
				bitnameAr.Add(System.IO.Path.GetFileName(fileNames[i]));
			}

			if (fileNames.Length > 1024)
			{
				for (int i = 1024; i < fileNames.Length; i++)
				{
					try
					{
						System.IO.File.Delete(fileNames[i]);
					}
					catch
					{
						break;
					}
				}
			}

			basicsConf = new BasicsConfiguration(axyConfOut, unit);
			schedConf = new ScheduleConfiguration(axyConfOut, unit);
			geoConf1 = new GeofencigConfiguration(axyConfOut, 1, this, unit);
			geoConf2 = new GeofencigConfiguration(axyConfOut, 2, this, unit);
			backB.IsEnabled = false;
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

			LocationChanged += locationChanged;
			windowMovingTimer.Tick += windowMovingEnded;
			windowMovingTimer.Interval = new TimeSpan(3000000);
		}

		private void loaded(object sender, RoutedEventArgs e)
		{
			Closed += closing;
			windowMovingEnded(this, new EventArgs());
		}

		private void locationChanged(object sender, EventArgs e)
		{
			windowMovingTimer.Stop();
			windowMovingTimer.Start();
		}

		private void windowMovingEnded(object sender, EventArgs e)
		{
			windowMovingTimer.Stop();
			var wih = new WindowInteropHelper(this);
			var screen = System.Windows.Forms.Screen.FromHandle(wih.Handle);
			var waWidth = screen.WorkingArea.Width;
			var waHeight = screen.WorkingArea.Height;
			double scale = 1;
			if (ExternalGrid.LayoutTransform is ScaleTransform)
			{
				scale = ((ScaleTransform)ExternalGrid.LayoutTransform).ScaleX;
			}
			var width = Width / scale;
			var height = Height / scale;

			scale = 1;
			while ((waWidth <= width) || (waHeight <= height))
			{
				scale -= .2;
				width *= scale;
				height *= scale;
				if (scale <= .2) break;

			}
			ExternalGrid.LayoutTransform = new ScaleTransform(scale, scale);
			if (Left < screen.Bounds.X)
			{
				Left = screen.Bounds.X + 5;
			}
			if (Top < screen.Bounds.Y)
			{
				Top = screen.Bounds.Y + 5;
			}

			//MessageBox.Show(screen.DeviceName + "  scale: " + scale.ToString() + "\r\nWidth: " + Width.ToString() + " Height: " + Height.ToString() +
			//	"\r\nScale Transform: " + st.ToString());
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
				if (axyConfOut[540] == 0)
				{
					//for (int i = 488; i < 488 + 24; i++)
					//{
					//	axyConfOut[i] = 0;
					//}
					axyConfOut[541] = 0xff;
					axyConfOut[542] = 0xff;
					axyConfOut[543] = 0xff;
				}
				Close();
				return;
			}
			else if ((string)forthB.Content == "CLOSE")
			{
				mustWrite = false;
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
				forthB.Content = lastForthContent;
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

			for (int i = pagesEnabled.Length - 1; i >= 0; i--)  //Calcola l'indice dell'ultima pagina abilitata
			{
				lastIndex = i;
				if (pagesEnabled[i] == true)
				{
					break;
				}
			}
			for (int i = 0; i < pagesEnabled.Length; i++)       //Calcola l'indice della prima pagina abilitata
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
			else
			{
				Gipsy6ConfigurationBrowser.Content = pages[pagePointer];
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

		private void importB_Click(object sender, RoutedEventArgs e)
		{

			var oldLockRfAddress = basicsConf.lockRfAddress;
			byte[] oldRfAddress = axyConfOut.Skip(541).Take(3).ToArray();

			var fopen = new System.Windows.Forms.OpenFileDialog();
			fopen.InitialDirectory = X_Manager.Parent.getParameter("gipsy6ConfigurationsFolder", System.IO.Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)));
			fopen.Filter = "Gipsy6 Configuration File | *.cfg";
			for (int i = 0; i < 2; i++)
			{
				try
				{
					var res = fopen.ShowDialog();
					if ((res == System.Windows.Forms.DialogResult.No) || (res == System.Windows.Forms.DialogResult.None) || (res == System.Windows.Forms.DialogResult.Cancel))
					{
						return;
					}
					break;
				}
				catch
				{
					fopen.InitialDirectory = "C:\\";
				}
			}

			var fp = fopen.FileName;
			var newConf = System.IO.File.ReadAllBytes(fp);
			X_Manager.Parent.updateParameter(MainWindow.INI_GIPSY6_SCHEDULE_PATH, System.IO.Path.GetDirectoryName(fp));
			if (!Encoding.ASCII.GetString(newConf.Take(16).ToArray()).Equals("--gipsy6Config--"))
			{
				MessageBox.Show("Invalid configuration file.");
				return;
			}

			basicsConf = null;
			schedConf = null;
			geoConf1 = null;
			geoConf2 = null;
			Gipsy6ConfigurationBrowser.Content = null;

			Array.Copy(newConf, 16, axyConfOut, 0, 0x22a);
			if (oldLockRfAddress)
			{
				Array.Copy(oldRfAddress, 0, axyConfOut, 541, 3); 
			}

			if (unit.firmTotA < 1004007)
			{
				axyConfOut[58] = 15;
			}

			basicsConf = new BasicsConfiguration(axyConfOut, unit);
			schedConf = new ScheduleConfiguration(axyConfOut, unit);
			geoConf1 = new GeofencigConfiguration(axyConfOut, 1, this, unit);
			geoConf2 = new GeofencigConfiguration(axyConfOut, 2, this, unit);
			lockRfAddress = oldLockRfAddress;
			pages = new PageCopy[] { schedConf, basicsConf, geoConf1, geoConf2 };
			pagesEnabled = new bool[] { true, false, false, false };
			lastIndex = 0;
			firstIndex = 0;
			forthB.Content = "SEND";
			backB.IsEnabled = false;
			forthB.IsEnabled = true;
			pagePointer = 0;
			if ((bool)expertCB.IsChecked)
			{
				pagesEnabled = new bool[] { true, true, true, true };
				forthB.Content = "-->";
				lastIndex = 3;
			}
			Gipsy6ConfigurationBrowser.Content = pages[0];
		}

		private void exportB_Click(object sender, RoutedEventArgs e)
		{
			var fsave = new System.Windows.Forms.SaveFileDialog();
			fsave.InitialDirectory = System.IO.Path.GetFullPath(MainWindow.getParameter("gipsy6ConfigurationsFolder", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)));
			fsave.Filter = "Gipsy6 Configuration File | *.cfg";

			for (int i = 0; i < 2; i++)
			{
				try
				{
					var res = fsave.ShowDialog();
					if ((res == System.Windows.Forms.DialogResult.No) || (res == System.Windows.Forms.DialogResult.None) || (res == System.Windows.Forms.DialogResult.Cancel))
					{
						return;
					}
					break;
				}
				catch
				{
					fsave.InitialDirectory = "C:\\";
				}
			}

			var cf = Gipsy6ConfigurationBrowser.Content as PageCopy;
			cf.copyValues();

			byte[] newConf = new byte[0x4096];
			Array.Copy(Encoding.ASCII.GetBytes("--gipsy6Config--"), newConf, 16);
			Array.Copy(axyConfOut, 0, newConf, 16, axyConfOut.Length);

			System.IO.File.WriteAllBytes(fsave.FileName, newConf);
		}
	}
}
