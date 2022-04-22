using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
//using System.Windows.Forms;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualBasic.FileIO;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.ComponentModel;
using X_Manager.Units;
using X_Manager.ConfigurationWindows;
using Xceed.Wpf.Toolkit.PropertyGrid;
using System.Security.RightsManagement;
using System.Windows.Media.Animation;



namespace X_Manager
{
	public partial class MainWindow : Parent
	{



		#region Dichiarazioni

		Unit oUnit;
		Unit cUnit;
		//ConfigurationWindow confForm;
		public int Baudrate_base = 115200;
		public const int Baudrate_1M = 1000000;
		public const int Baudrate_2M = 2000000;
		public const int Baudrate_3M = 3000000;

		//public const uint model_axy1 = 255;
		//public const uint model_axyDepth = 127;
		//public const uint model_axy2 = 126;
		//public const uint model_axy3 = 125;
		//public const uint model_axy4 = 124;
		//public const uint model_Co2Logger = 123;
		//public const uint model_axy5 = 122;
		//public const uint model_AGM1_calib = 10;
		//public const uint model_Gipsy6 = 10;
		//public const uint model_AGM1 = 9;
		//public const uint model_axyTrekS = 7;
		//public const uint model_axyTrek = 6;
		//public const uint model_axyQuattrok = 4;

		public bool realTimeStatus = false;
		public bool convertStop = false;
		public bool unitConnected = false;
		//List<bool> controlStatus;
		//ChartWindow charts;
		UInt16 convFileTot;
		UInt16 convFile;
		int realTimeType = 0;

		bool askOverwrite = true;

		byte[] unitFirmware = new byte[15];

		//byte[] unitModel = new byte[1];
		public byte[] axyconf = new byte[30];
		bool positionCanSend = false;
		BackgroundWorker startUpMonitorBW;

		//Costanti e pseudo constanti
		const string STR_noComPortAvailable = "No COM port available. Please connect a data cable.";
		const string STR_unitNotReady = "Unit not ready: please reconnect again.";
		const string STR_resumeDownloadQuestion = "A partial download has been found for this unit. Do you want to resume it or restart from the beginning?";
		const string STR_Yes = "Yes";
		const string STR_No = "No";
		const string STR_Resume = "Resume";
		const string STR_Restart = "Restart";
		const string STR_memoryEMpty = "Memory is empty.";

		public static string companyFolder = "\\" + System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetEntryAssembly().Location).CompanyName;
		public static string appFolder = "\\" + System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetEntryAssembly().Location).ProductName;

		string iniPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
		public static string iniFile = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + companyFolder + appFolder + "\\settings.ini";
		public static string prefFile = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + companyFolder + appFolder + "\\convPrefs.ini";

		List<string> convFiles = new List<string>();

		[DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DeleteObject([In] IntPtr hObject);

		DispatcherTimer windowMovingTimer = new DispatcherTimer();

		public static bool adminUser = false;
		public static System.Timers.Timer keepAliveTimer;
		ConfigurationWindow confForm;
		bool manageConfform = true;

		#endregion

		#region Interfaccia

		public MainWindow()
		{
			InitializeComponent();
			sp = new System.IO.Ports.SerialPort();
			Loaded += mainWindowLoaded;
			parseArgIn();
			progressBarStopButton = progressBarStopButtonM;
			statusProgressBar = statusProgressBarM;
			txtProgressBar = txtProgressBarM;
			kmlProgressBar = kmlProgressBarM;
			statusLabel = statusLabelM;
			etaLabel = etaLabelM;
			progressBarStopButtonColumn = progressBarStopButtonColumnM;
			LocationChanged += locationChanged;
			windowMovingTimer.Tick += windowMovingEnded;
			windowMovingTimer.Interval = new TimeSpan(3000000);
		}

		private void parseArgIn()
		{
			List<string> args = new List<string>();
			args = Environment.GetCommandLineArgs().ToList<string>();
			args.RemoveAt(0);
			int parCount = 0;
			while (args.Count > 0)
			{
				parCount++;
				if (parCount == 1000)
				{
					break;
				}
				string arg = args[0];
				args.RemoveAt(0);
				switch (arg)
				{
					case "-c":
						Console.WriteLine("Provola");
						break;
					case "-f":
						try
						{
							convFiles.Add(args[0]);
							args.RemoveAt(0);
							askOverwrite = true;
						}
						catch { }
						break;
					case "-i":
						askOverwrite = false;
						break;
					case "-v":
						string testo = "";
						foreach (var a in Environment.GetCommandLineArgs())
						{
							testo += a + "\r\n";
						}
						MessageBox.Show(testo);
						break;
				}
			}
		}

		private void mainWindowLoaded(object sender, EventArgs e)
		{
			Console.WriteLine("Loaded.");

			loadUserPrefs();
			uiDisconnected();
			initPicture();
			//scanButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			scanPorts();
			string press = getParameter("pressureRange", "depth");
			switch (press)
			{
				case "depth":
					depthSubItem.IsChecked = true;
					airSubItem.IsChecked = false;
					break;
				case "air":
					depthSubItem.IsChecked = false;
					airSubItem.IsChecked = true;
					break;
			}
			keepMdpItem.IsChecked = bool.Parse(getParameter("keepMdp", "true"));
			//keepMdpItem.IsChecked = false;
			//if (lastSettings[6] == "true") keepMdpItem.IsChecked = true;
			//selectDownloadSpeed(lastSettings[7]);
			//csvSeparatorChanged(lastSettings[8]);
			selectDownloadSpeed(getParameter("downloadSpeed", "3"));
			csvSeparatorChanged(getParameter("csvSeparator", "\t"));
			normalViewTabItem.IsSelected = true;

			switch (getParameter("downloadMode", "A"))
			{
				case "A":
					downloadAutomatic.IsChecked = true;
					break;
				case "M":
					downloadManual.IsChecked = true;
					break;
			}

			//if (lastSettings[9].Equals("A")) downloadAutomatic.IsChecked = true;
			//if (lastSettings[9].Equals("M")) downloadManual.IsChecked = true;

			downloadManual.Checked += downloadModeChecked;
			downloadManual.Unchecked += downloadModeChecked;
			downloadAutomatic.Checked += downloadModeChecked;
			downloadAutomatic.Unchecked += downloadModeChecked;

			progressBarStopButton.IsEnabled = false;
			progressBarStopButtonColumn.Width = new GridLength(0);

			if (convFiles.Count > 0)
			{
				convertDataLaunch();
			}

#if DEBUG
			byte[] conf = new byte[514] { 0xCF, 0x55, 0x01, 0x00,
					// 4	Nome unità: 27 caratteri + terminatore 0
					0, 0, 0, 0, 0, 0, 0,  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
					0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
					// 32	ACQ ON (240s) - ACQ OFF (1800s)
					0xf0, 0x00, 0x08, 0x07,
					// 36	Start delay (secondi, 32 bit)
					0x00, 0x00, 0x00, 0x00,
					// 40	Start delay (date)
					0xe3, 0x07, 0x01, 0x01,
					// 44	Soglie GSV
					0x01,		//n satelliti dopo 20 sec
					0x3c,		//gsv minima dopo 40 satelliti
					0x00, 0x00,	//usi futuri
					// 48	ADC soglia + magmin, flag trigger, flag log
					0x02, 0x00, 0x00, 0x00,
					// 52	P1: Schedule A (10 secondi)
					0x00, 0x00, 0x00, 0x0A,
					// 56	P1: Schedule B (mezz'ora)
					0x00, 0x00, 0x07, 0x08,
					// 60 - P1: Orari (0 = off, 1 = sch.A, 2 = sch.B)
					0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
					0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
					// 84	P2: Schedule C (1 minuto)
					0x00, 0x00, 0x00, 0x3C,
					// 88	P2: Schedule D (due ore)
					0x00, 0x00, 0x1C, 0x20,
					// 92 - P2: Orari (0 = off, 1 = sch.C, 2 = sch.D)
					0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
					0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
					// 116 - P2: Mesi validità
					0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
					// 128 - G1: Vertici
					0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
					0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
					0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
					0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
					0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
					0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
					0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
					0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
					0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
					0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
					//288 - G1: Schedule E (1 minuto)
					0x00, 0x00, 0x00, 0x3C,
					// 292	G1: Schedule F (due ore)
					0x00, 0x00, 0x1C, 0x20,
					// 296	G1: Orari
					0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
					0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
					// 320 - G1: Vertici
					0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
					0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
					0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
					0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
					0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
					0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
					0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
					0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
					0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
					0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
					//480 - G2: Schedule E (1 minuto)
					0x00, 0x00, 0x00, 0x3C,
					// 484	G2: Schedule F (due ore)
					0x00, 0x00, 0x1C, 0x20,
					// 488	G2: Orari
					0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
					0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
					// 512	G1/G2 Enable
					0x00, 0x00
					//514
					};
			//var confForm = new GiPSy6ConfigurationMain(conf, Unit.model_Gipsy6);
			//confForm.ShowDialog();
			windowMovingEnded(this, new EventArgs());

#endif
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

		private void ComPortComboBox_DropDownOpened(object sender, EventArgs e)
		{
			scanPorts();
		}

		private void loadUserPrefs()
		{
			try
			{
				settings = File.ReadAllLines(iniFile);
			}
			catch
			{
				settings = new string[] { "\r\n" };
			}

			if (!File.Exists(iniFile) | !settings[0].Contains("="))
			{
				if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + companyFolder + appFolder))
				{

					Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + companyFolder + appFolder);
				}

				string fileBody = "";
				//crea il file ini nella cartella e scrive la prima riga per l\\immagine di sfondo (0)
				fileBody = "backgroundImagePath=null\r\n";
				//Scrive la cartella per il salvataggio dei file di schedule (1)
				fileBody += "trekScheduleSavePath=null\r\n";
				//Scrive la cartella per l//apertura dei file di schedule (2)
				fileBody += "trekScheduleOpenPath=null\r\n";
				//scrive il file ini per la cartella file Save (3)
				fileBody += "dataSavePath=" + Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + companyFolder + "\\Downloads\r\n";
				if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + companyFolder + "\\Downloads"))
				{
					Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + companyFolder + "\\Downloads");
				}
				//Scrive il file ini per la cartella Convert (4)
				fileBody += "convertPath=" + Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + companyFolder + "\\Downloads\r\n";
				//Scrive il tipo di conversione del sensore di pressione
				fileBody += "pressureRange=depth\r\n";
				//Scrive l//opzione per lasciare su disco il file mdp dopo il download
				fileBody += "keepMdp=true\r\n";
				//Scrive la velocità di download
				fileBody += "downloadSpeed=3\r\n";
				//Scrive il separatore csv
				fileBody += "csvSeparator=\t\r\n";
				//Scrive la modalità download
				fileBody += "downloadMode=A\r\n";
				//Scrive il percorso schedule Axy5
				fileBody += "axy5SchedulePath=" + Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + companyFolder + "\\Axy5Schedule\r\n";
				if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + companyFolder + "\\Axy5Schedule"))
				{
					Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + companyFolder + "\\Axy5Schedule");
				}
				File.WriteAllText(iniFile, fileBody);
			}

			settings = File.ReadAllLines(iniFile);
		}

		private void uiDisconnected()
		{
			try
			{
				oUnit.Dispose();
			}
			catch { }
			Baudrate_base = 115200;
			unitConnected = false;
			modelLabel.Content = "";
			firmwareLabel.Content = "";
			unitNameTextBox.Text = "";
			batteryLabel.Content = "";
			unitNameTextBox.IsEnabled = false;
			unitNameButton.IsEnabled = false;
			comPortComboBox.IsEnabled = true;
			connectButton.Content = "Connect";
			scanButton.IsEnabled = true;
			downloadButton.IsEnabled = false;
			eraseButton.IsEnabled = false;
			configureMovementButton.IsEnabled = false;
			configurePositionButton.IsEnabled = true;
			statusLabel.Content = "Not connected.";
			statusProgressBar.IsIndeterminate = false;
			statusProgressBar.Maximum = 1;
			statusProgressBar.Value = 0;
			positionCanSend = false;
			dumpViewTabItem.IsEnabled = true;
			remoteButton.Content = "Remote";
			remoteButton.IsEnabled = true;
			Title = "X MANAGER";
			configureMovementButton.Content = "Accelerometer configuration";
			realTimeSP.Visibility = Visibility.Hidden;
			configurePositionButton.Visibility = Visibility.Visible;
			configureMovementButton.Content = "Accelerometer Configuration";
			configurePositionButton.Content = "GPS configuration";
			manageConfform = true;
			if (sp.IsOpen) sp.Close();
			batteryRefreshB.Visibility = Visibility.Hidden;
			batteryRefreshB.Click -= refreshBattery;
			if (keepAliveTimer != null)
			{
				keepAliveTimer.Stop();
				keepAliveTimer = null;
			}
		}

		private void uiConnected()
		{
			unitConnected = true;
			unitNameTextBox.IsEnabled = true;
			unitNameButton.IsEnabled = true;
			comPortComboBox.IsEnabled = false;
			connectButton.Content = "Disconnect";
			scanButton.IsEnabled = false;
			downloadButton.IsEnabled = true;
			eraseButton.IsEnabled = true;
			configureMovementButton.IsEnabled = true;
			statusLabel.Content = "Connected.";
			statusProgressBar.IsIndeterminate = false;
			//statusProgressBar.Value = 0;
			dumpViewTabItem.IsEnabled = false;
			unitNameTextBox.MaxLength = 10;
			configurePositionButton.IsEnabled = oUnit.configurePositionButtonEnabled;
			configureMovementButton.IsEnabled = oUnit.configureMovementButtonEnabled;
			if (realTimeType > 0)
			{
				realTimeSP.Visibility = Visibility.Visible;
			}

			switch (oUnit.modelCode)
			{
				case Unit.model_Co2Logger:
					configureMovementButton.Content = "Logger configuration";
					break;
				case Unit.model_AGM1:
					configureMovementButton.Content = "Movement configuration";
					break;
				case Unit.model_Gipsy6:
					unitNameTextBox.MaxLength = 27;
					configureMovementButton.IsEnabled = true;
					configurePositionButton.IsEnabled = true;
					configureMovementButton.Content = "CONFIGURATION";
					configurePositionButton.Content = "Upload new firmware";
					break;
				case Unit.model_drop_off:
					configureMovementButton.Content = "Drop-off timer configuration";
					break;
				default:
					configureMovementButton.Content = "Accelerometer configuration";
					break;
			}
			//sviluppo
			realTimeSP.IsEnabled = true;
			realTimeB.IsEnabled = true;
			batteryRefreshB.Visibility = Visibility.Visible;
			batteryRefreshB.Click += refreshBattery;
		}

		void ctrlManager(object sender, KeyEventArgs e)
		{
			if (Keyboard.Modifiers == ModifierKeys.Control)
			{
				//Apertura cartella program Data
				if (e.Key == Key.D)
				{
					System.Diagnostics.Process.Start(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + companyFolder + appFolder);
				}

				//Arresto conversione
				else if (e.Key == Key.P)
				{
					convertStop = true;
				}

				//Axy4d come axy4
				else if (e.Key == Key.D4)
				{
					if (connectButton.Content.Equals("Connect"))
					{
						if (sp.IsOpen)
						{
							sp.Write("TTTTTTTTTTTTTGGA4");
						}
					}
					Console.Beep();
				}

				//Axy4d come axy depth
				else if (e.Key == Key.D5)
				{
					if (connectButton.Content.Equals("Connect"))
					{
						if (sp.IsOpen)
						{
							sp.Write("TTTTTTTTTTTTTGGA5");
						}
					}

					Console.Beep();
				}

				//Trek o Quattrok locale
				else if (e.Key == Key.L)
				{
					startUpMonitor.Text = "";
					if (connectButton.Content.Equals("Connect"))
					{
						if (sp.IsOpen)
						{
							sp.Write("T");
							Thread.Sleep(5);
							sp.Write("TTTTTTTTTTTTTGGAr");
						}
						Console.Beep();
					}

				}

				//Trek o Quattrok remoto
				else if (e.Key == Key.R)
				{
					startUpMonitor.Text = "";
					if (connectButton.Content.Equals("Connect"))
					{
						if (sp.IsOpen)
						{
							sp.Write("T");
							Thread.Sleep(5);
							sp.Write("TTTTTTTTTTTTTGGAR");
						}
					}
					Console.Beep();
				}

				//Trek o Quattrok solare
				else if (e.Key == Key.S)
				{
					startUpMonitor.Text = "";
					if (connectButton.Content.Equals("Connect"))
					{
						if (sp.IsOpen)
						{
							sp.Write("T");
							Thread.Sleep(5);
							sp.Write("TTTTTTTTTTTTTGGAS");
						}
					}
					Console.Beep();
				}

				//Trek o Quattrok non solare
				else if (e.Key == Key.N)
				{
					startUpMonitor.Text = "";
					if (connectButton.Content.Equals("Connect"))
					{
						if (sp.IsOpen)
						{
							sp.Write("T");
							Thread.Sleep(5);
							sp.Write("TTTTTTTTTTTTTGGAs");
						}
					}
					Console.Beep();
				}

				else if (e.Key == Key.B)
				{
					bool uConn = true;
					if (!((string)configurePositionButton.Content).Contains("firmware"))
					{
						try
						{
							string portShortName;
							portShortName = comPortComboBox.Text.Substring(comPortComboBox.Text.IndexOf("(") + 1);
							portShortName = portShortName.Remove(portShortName.IndexOf(")"), portShortName.Length - portShortName.IndexOf(")"));
							sp.PortName = portShortName;
							sp.BaudRate = 115200;
							//sp.Open();
							uConn = false;

						}
						catch (Exception ex)
						{
							MessageBox.Show(ex.Message);
							return;
						}
					}
					uiDisconnected();
					var boot = new Bootloader.Bootloader_Gipsy6(uConn, this);
					boot.ShowDialog();
					if (sp.IsOpen) sp.Close();
				}

				//Pubblicazione help
				else
				{
					startUpMonitor.Text = "LISTA COMANDI:\r\n";
					startUpMonitor.Text += "\tD: Apertura cartella ProgramData";
					startUpMonitor.Text += "\t4: Axy4D come Axy4";
					startUpMonitor.Text += "\t5: Axy4D come AxyDepth";
					startUpMonitor.Text += "\tR: AxyTrek Remoto";
					startUpMonitor.Text += "\tL: AxyTrek Locale";
					startUpMonitor.Text += "\tS: AxyTrek Solare";
					startUpMonitor.Text += "\tN: AxyTrek Non solare";
					startUpMonitor.Text += "\tC: AxyQuattrok: imposta coefficienti";
					startUpMonitor.Text += "\tB: GiPSy6 Bootloader";
				}

			}

		}

		public override void downloadFinished()
		{
			warningShow("Download completed.");
			mainGrid.IsEnabled = true;
			statusLabel.Content = "Connected";
			progressBarStopButton.IsEnabled = false;
			progressBarStopButtonColumn.Width = new GridLength(0);
			try
			{
				if (sp.IsOpen) sp.ReadExisting();
				Thread.Sleep(100);
				uint[] maxM = oUnit.askMaxMemory();
				Thread.Sleep(100);
				uint[] actM = oUnit.askMemory();
				setPBMemory(actM, maxM);
				Thread.Sleep(100);
			}
			catch
			{
				downloadFailed();
			}
		}

		public override void downloadFailed()
		{
			badShow(STR_unitNotReady);
			mainGrid.IsEnabled = true;
			uiDisconnected();
		}

		private void setPBMemory(uint[] actM, uint[] maxM)
		{
			if (actM.Length == 0)
			{
				return;
			}

			statusProgressBar.Maximum = maxM[1] - maxM[0];
			statusProgressBar.Minimum = 0;

			if (actM[0] < actM[1])
			{
				statusProgressBar.Value = actM[1] - actM[0];
			}
			else if (actM[0] == actM[1])
			{
				statusProgressBar.Value = 0;
			}
			else
			{
				statusProgressBar.Value = (maxM[1] - actM[0]) + (actM[1] - maxM[0]);
			}
		}

		private void openDataFolder(object sender, RoutedEventArgs e)
		{
			//lastSettings = File.ReadAllLines(iniFile);
			System.Diagnostics.Process.Start(getParameter("dataSavePath"));
		}

		void tabControlTabChanged(object sender, RoutedEventArgs e)
		{
			if (mainTabControl.SelectedIndex == 0)
			{
				try
				{
					if (startUpMonitorBW != null & startUpMonitorBW.IsBusy) startUpMonitorBW.CancelAsync();
				}
				catch { }
				uiDisconnected();
				connectButton.IsEnabled = true;
				if (sp.IsOpen) sp.Close();
			}
			else
			{
				string portShortName = comPortComboBox.Text.Substring(comPortComboBox.Text.IndexOf("(") + 1);
				if (portShortName == "" | string.IsNullOrEmpty(portShortName))
				{
					mainTabControl.SelectedIndex = 0;
					return;
				}
				portShortName = portShortName.Remove(portShortName.IndexOf(")"), portShortName.Length - portShortName.IndexOf(")"));
				sp.BaudRate = Baudrate_base;
				sp.ReadTimeout = 5;
				sp.NewLine = "\r\n";

				try
				{
					sp.PortName = portShortName;
					sp.Open();
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message);
					return;
				}

				comPortComboBox.IsEnabled = false;
				connectButton.IsEnabled = false;
				scanButton.IsEnabled = false;
				downloadButton.IsEnabled = false;
				eraseButton.IsEnabled = false;
				configureMovementButton.IsEnabled = false;
				statusLabel.Content = "Dump view...";
				statusProgressBar.IsIndeterminate = false;
				statusProgressBar.Value = 0;
				dumpViewTabItem.IsEnabled = false;
				configurePositionButton.IsEnabled = false;

				startUpMonitor.Text = "";

				startUpMonitorBW = new BackgroundWorker();
				startUpMonitorBW.WorkerReportsProgress = false;
				startUpMonitorBW.WorkerSupportsCancellation = true;

				startUpMonitorBW.DoWork += startUpMonitorBW_DoWork;
				startUpMonitorBW.RunWorkerCompleted += startUpMonitorBW_RunWorkerCompleted;

				startUpMonitorBW.RunWorkerAsync();
				mainTabControl.Focus();
			}
		}

		private void startUpMonitorBW_DoWork(object sender, DoWorkEventArgs e)
		{
			var sb = new System.Text.StringBuilder();

			while (true)
			{
				while (!startUpMonitorBW.CancellationPending && sp.BytesToRead == 0) ;


				if (startUpMonitorBW.CancellationPending)
				{
					e.Cancel = true;
					return;
				}
				sb.Clear();
				while (sb.Length < 4095)
				{
					try
					{
						sb.Append(System.Convert.ToChar(sp.ReadByte()));
					}
					catch
					{
						break;
					}
				}
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => startUpMonitor.Text += sb.ToString()));
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => startUpMonitor.ScrollToEnd()));
			}

		}

		private void startUpMonitorBW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			try
			{
				sp.Close();
			}
			catch { }
		}

		void selectAddressMouse(object sender, MouseButtonEventArgs e)
		{
			selectAddress(sender);
		}

		void selectAddressKeyboard(object sender, KeyboardFocusChangedEventArgs e)
		{
			selectAddress(sender);
		}

		void selectAddress(object sender)
		{
			var tb = (TextBox)sender;
			if (tb != null)
			{
				tb.SelectAll();
			}
		}

		void selectivelyIgnoreMouseButton(object sender, RoutedEventArgs e)
		{
			var tb = (TextBox)sender;
			if (tb != null)
			{
				if (!tb.IsKeyboardFocusWithin)
				{
					e.Handled = true;
					tb.Focus();
				}
			}
		}

		void unitNameTextBoxKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				setName();
				connectButton.Focus();
				e.Handled = true;
				connectButton.Focus();
			}
		}

		#endregion

		#region Menù

		void realTimeConfigurationClick(object sender, RoutedEventArgs e)
		{

		}

		private void downloadModeChecked(object sender, RoutedEventArgs e)
		{
			downloadManual.Checked -= downloadModeChecked;
			downloadAutomatic.Checked -= downloadModeChecked;
			downloadManual.Unchecked -= downloadModeChecked;
			downloadAutomatic.Unchecked -= downloadModeChecked;
			//lastSettings = System.IO.File.ReadAllLines(iniFile);
			if (((MenuItem)sender).Name.Equals("downloadAutomatic"))
			{
				downloadAutomatic.IsChecked = true;
				downloadManual.IsChecked = false;
				updateParameter("downloadMode", "A");
				//lastSettings[9] = "A";
				//System.IO.File.WriteAllLines(iniFile, lastSettings);
			}
			else
			{
				downloadManual.IsChecked = true;
				downloadAutomatic.IsChecked = false;
				updateParameter("downloadMode", "M");
				//lastSettings[9] = "M";
				//System.IO.File.WriteAllLines(iniFile, lastSettings);
			}
			downloadManual.Checked += downloadModeChecked;
			downloadAutomatic.Checked += downloadModeChecked;
			downloadManual.Unchecked += downloadModeChecked;
			downloadAutomatic.Unchecked += downloadModeChecked;
		}

		void unlockUnit_Click(object sender, RoutedEventArgs e)
		{
			bool eraAperta = true;
			if (!sp.IsOpen)
			{
				if (comPortComboBox.Items.Count == 0)
				{
					warningShow("Please connect a data cable and press 'Scan for port'.");
					return;
				}
				eraAperta = false;
				string portShortName = comPortComboBox.Text.Substring(comPortComboBox.Text.IndexOf("(") + 1);
				portShortName = portShortName.Remove(portShortName.IndexOf(")"), portShortName.Length - portShortName.IndexOf(")"));
				sp.PortName = portShortName;
				sp.NewLine = "\r\n";
			}

			var w = new Warning("Please wait...");
			w.Owner = this;
			w.picUri = "pack://application:,,,/resources/alert2.png";
			w.Show();

			byte[] arr = new byte[5]; //0 to 4
			sp.BaudRate = Baudrate_base;
			sp.ReadTimeout = 1000;
			try
			{
				sp.Open();
			}
			catch (Exception ex)
			{
				w.Close();
				warningShow(ex.Message);
				return;
			}

			arr[0] = 0xf;
			for (int i = 0; i < 3; i++)
			{
				try
				{
					sp.ReadByte();
				}
				catch
				{
					if (i == 2)
					{
						w.Close();
						warningShow("Unit not reachable.");
						if (!eraAperta)
						{
							if (sp.IsOpen) sp.Close();
						}
						return;
					}
				}
			}

			arr[0] = 8;
			arr[1] = 8;
			arr[2] = 0x81;
			arr[3] = 4;
			sp.Write(arr, 0, 4);
			warningShow("Unit unlocked.");
			if (!eraAperta)
			{
				if (sp.IsOpen) sp.Close();
			}
		}

		void changePictureClick(object sender, RoutedEventArgs e)
		{
			var openPicture = new Microsoft.Win32.OpenFileDialog();
			openPicture.DefaultExt = ("JPG Files|*.jpg");
			openPicture.Filter = ("JPG Files|*.jpg|PNG Files|*.png|BMP Files|*.bmp");
			if (File.Exists(System.IO.Path.GetDirectoryName(getParameter("backgroundImagePath"))))
			{
				openPicture.InitialDirectory = System.IO.Path.GetDirectoryName(getParameter("backgroundImagePath"));
			}


			if (!(bool)openPicture.ShowDialog(this)) //Se si preme Annulla, termina la procedura
			{
				return;
			}
			updateParameter("backgroundImagePath", openPicture.FileName);
			//lastSettings[0] = openPicture.FileName;
			//System.IO.File.WriteAllLines(iniFile, lastSettings);
			try
			{
				var bmp = new BitmapImage();
				bmp.BeginInit();
				bmp.UriSource = new Uri(openPicture.FileName, UriKind.Absolute);
				bmp.EndInit();
				pictureBox.Source = bmp;
			}
			catch { }

		}

		void aboutInfoClick(object sender, RoutedEventArgs e)
		{
			var about = new About();
			about.Owner = this;
			about.ShowDialog();
		}

		void airSensorSelected(object sender, RoutedEventArgs e)
		{
			//loadUserPrefs();
			//lastSettings = System.IO.File.ReadAllLines(iniFile);
			//lastSettings[5] = "air";
			//System.IO.File.WriteAllLines(iniFile, lastSettings);
			updateParameter("pressureRange", "air");
			depthSubItem.IsChecked = false;
			airSubItem.IsChecked = true;
		}

		void depthSensorSelected(object sender, RoutedEventArgs e)
		{
			//loadUserPrefs();
			//lastSettings = System.IO.File.ReadAllLines(iniFile);
			//lastSettings[5] = "depth";
			//System.IO.File.WriteAllLines(iniFile, lastSettings);
			updateParameter("pressureRange", "depth");
			depthSubItem.IsChecked = true;
			airSubItem.IsChecked = false;
		}

		void keepMdpClicked(object sender, RoutedEventArgs e)
		{
			//lastSettings = System.IO.File.ReadAllLines(iniFile);
			if (keepMdpItem.IsChecked)
			{
				//lastSettings[6] = "true";
				updateParameter("keepMdp", "true");
			}
			else
			{
				//lastSettings[6] = "false";
				updateParameter("keepMdp", "false");
			}
			//System.IO.File.WriteAllLines(iniFile, lastSettings);
		}

		void selectDownloadSpeed(string speed)
		{
			speedLegacy.IsChecked = false;
			speed1.IsChecked = false;
			speed2.IsChecked = false;
			speed3.IsChecked = false;
			switch (speed)
			{
				case "legacy":
					speedLegacy.IsChecked = true;
					break;
				case "1":
					speed1.IsChecked = true;
					break;
				case "2":
					speed2.IsChecked = true;
					break;
				case "3":
					speed3.IsChecked = true;
					break;
			}
			updateParameter("downloadSpeed", speed);
			//lastSettings[7] = speed;
			//System.IO.File.WriteAllLines(iniFile, lastSettings);
		}

		void speedLegacySelected(object sender, RoutedEventArgs e)
		{
			selectDownloadSpeed("legacy");
		}

		void speed1Selected(object sender, RoutedEventArgs e)
		{
			selectDownloadSpeed("1");
		}

		void speed2Selected(object sender, RoutedEventArgs e)
		{
			selectDownloadSpeed("2");
		}

		void speed3Selected(object sender, RoutedEventArgs e)
		{
			selectDownloadSpeed("3");
		}

		void csvSeparatorChanged(string sep)
		{
			csvSeparator = sep;
			commaSubItem.IsChecked = false;
			semicolonSubItem.IsChecked = false;
			tabSubItem.IsChecked = false;
			switch (sep)
			{
				case ",":
					commaSubItem.IsChecked = true;
					break;
				case ";":
					semicolonSubItem.IsChecked = true;
					break;
				case "\t":
					tabSubItem.IsChecked = true;
					break;
			}
			updateParameter("csvSeparator", sep);
			//lastSettings[8] = sep;
			//File.WriteAllLines(iniFile, lastSettings);
		}

		void tabSepSel(object sender, RoutedEventArgs e)
		{
			csvSeparatorChanged("\t");
		}

		void semiSepSel(object sender, RoutedEventArgs e)
		{
			csvSeparatorChanged(";");
		}

		void commaSepSel(object sender, RoutedEventArgs e)
		{
			csvSeparatorChanged(",");
		}

		#endregion

		#region Pulsanti

		void scanPorts()
		{
			if (sp.IsOpen)
			{
				sp.Close();
			}
			this.comPortComboBox.Items.Clear();
			System.Management.ManagementObjectSearcher moSearch = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity");
			//System.Management.ManagementObjectCollection moReturn = moSearch.Get();
			//System.Management.ManagementObject mo;// = moReturn.ite
			string deviceName = "";
			foreach (System.Management.ManagementObject mo in moSearch.Get())
			{
				try
				{

					deviceName = "";
					try
					{
						deviceName = mo["Caption"].ToString();
					}
					catch { }
					//deviceName = mo["Caption"].ToString();
					if (deviceName.Contains("COM"))// && !deviceName.Contains("XDS110") && !deviceName.Contains("COM1"))
					{
						if (!deviceName.Contains("XDS") && !deviceName.Contains("RFC"))
						{
							if (!deviceName.Contains("COM1)"))
							{
								comPortComboBox.Items.Add(deviceName);
							}
						}
					}
				}
				catch
				{
					//MessageBox.Show(ex.Message);
				}
			}
			if (comPortComboBox.Items.Count == 0)
			{
				//comPortComboBox.IsEnabled = false;
				connectButton.IsEnabled = false;
				//warningShow(STR_noComPortAvailable);
			}
			else
			{
				comPortComboBox.IsEnabled = true;
				comPortComboBox.SelectedIndex = 0;
				connectButton.IsEnabled = true;
			}
		}

		private async Task pbTask()
		{
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => statusLabel.Content = "Connecting. Please wait..."));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => statusProgressBar.IsIndeterminate = true));
		}

		async void connectClick(object sender, RoutedEventArgs e)
		{

			if ((string)connectButton.Content == "Connect")
			{
				Task pbbTask = pbTask();
				await pbbTask;

				sp.BaudRate = Baudrate_base;
				sp.ReadTimeout = 550;
				sp.NewLine = "\r\n";

				//isola la porta COM selezionata
				string portShortName;
				portShortName = comPortComboBox.Text.Substring(comPortComboBox.Text.IndexOf("(") + 1);
				portShortName = portShortName.Remove(portShortName.IndexOf(")"), portShortName.Length - portShortName.IndexOf(")"));
				if (sp.IsOpen) sp.Close();

				//Imposta a 1ms il latency del buffer ftdi e tenta di aprire la porta
				//ftdiSerialNumber = setLatency(portShortName, 1);
				try
				{
					sp.PortName = portShortName;
					sp.Open();
				}
				catch (Exception ex)
				{
					badShow(ex.Message);
					uiDisconnected();
					return;
				}

				string response;
				sp.ReadExisting();

				sp.Write("T");
				try
				{
					int rs = sp.ReadByte();
					if (rs == 0x23)
					{
						//completeCommand = false;
						Thread.Sleep(55);
						sp.ReadExisting();
						sp.Write("T");
						Thread.Sleep(5);
						sp.Write("TTTTTTTTTTTTTTGGAP");
					}
					else if (rs == '6')     //In caso di Gipsy6 bisogna mandare una stringa corta
					{
						sp.Write("TTTTGGAP");
					}
					else
					{
						sp.Write("TTTTTTTTTTTGGAP");
					}

					if (remote) Thread.Sleep(400);
					response = sp.ReadLine();
				}
				catch
				{
					try
					{
						sp.Write("TTTTTTTTTTTTTTGGAP");
						Thread.Sleep(200);
						response = sp.ReadLine();
						//completeCommand = false;
						//if (response.Contains("RReady")) completeCommand = true;
					}
					catch
					{
						badShow(STR_unitNotReady);
						if (sp.IsOpen) sp.Close();
						uiDisconnected();
						return;
					}
				}
				sp.ReadTimeout = 1100;

				bool esito = true;
				if (response.Contains("Ready"))
				{
					try
					{
						Thread.Sleep(10);
						string model = Unit.askModel(ref sp);
						switch (model)
						{
							case "Axy-Trek":
								oUnit = new AxyTrek(this);
								break;
							case "Axy-4":
							case "Axy-4.5":
								oUnit = new Axy4_2(this);
								break;
							case "Axy-5":
								oUnit = new Axy5(this);
								break;
							case "Axy-Depth":
							case "Axy-Depth.5":
								oUnit = new AxyDepth_2(this);
								break;
							case "AGM-1":
								oUnit = new AGM(this);
								break;
							case "CO2 Logger":
								oUnit = new CO2_Logger(this);
								break;
							case "Axy-Quattrok":
								oUnit = new AxyQuattrok(this);
								break;
							case "GiPSy-6":
								oUnit = new Gipsy6(this);   //Nel costruttore viene chiusa la porta seriale e riaperta mediante driver ftdi
								if (sender is MainWindow)
								{
									oUnit.msBaudrate();
								}
								break;
							case "Drop-Off":
								oUnit = new Drop_Off(this);
								break;
						}
						modelLabel.Content = model;
						getRemote();
						getConf();
						getSolar();
						if (oUnit.solar)
						{
							modelLabel.Content += " (s)";
						}
						oUnit.connected = true;
					}
					catch
					{
						esito = false;
					}
				}
				else if (response.Contains("Progmode"))
				{
					try
					{
						string model = Unit.askLegacyModel(ref sp);
						switch (model)
						{
							case "Axy-4":
								oUnit = new Axy4_1(this);
								break;
							case "Axy-3":
								oUnit = new Axy3(this);
								break;
							case "Axy-Depth":
								oUnit = new AxyDepth_1(this);
								break;
						}
						modelLabel.Content = model;
						getRemote();
						getConf();
					}
					catch
					{
						esito = false;
					}
				}
				else
				{
					badShow(STR_unitNotReady);
					if (sp.IsOpen) sp.Close();
					uiDisconnected();
				}

				if (!esito)
				{
					badShow(STR_unitNotReady);
					if (sp.IsOpen) sp.Close();
					uiDisconnected();
					return;
				}
			}
			else
			{
				oUnit.disconnect();
				oUnit.Dispose();
				uiDisconnected();
				oUnit = null;
			}

		}

		void unitNameButtonClick(object sender, RoutedEventArgs e)
		{
			setName();
			connectButton.Focus();
		}

		void eraseButtonClick(object sender, RoutedEventArgs e)
		{
			oUnit.closeSerialPort(sp);
			YesNo yn = new YesNo("Do you really want to erase the memory?", "MEMORY ERASING");
			yn.Owner = this;
			if (yn.ShowDialog() == 1)
			{
				try
				{
					oUnit.eraseMemory();
					Thread.Sleep(50);
					uint[] maxM = oUnit.askMaxMemory();
					Thread.Sleep(50);
					uint[] actM = oUnit.askMemory();
					setPBMemory(actM, maxM);
				}
				catch (Exception ex)
				{
					warningShow(ex.Message);
					uiDisconnected();
				}
				var okF = new Ok("Memory Erased.");
				okF.Owner = this;
				okF.ShowDialog();
			}
		}

		void configureMovementButtonClick(object sender, RoutedEventArgs e)
		{
			oUnit.closeSerialPort(sp);
			byte[] conf;
			byte[] accSchedule;
			try
			{
				conf = oUnit.getConf();
				accSchedule = oUnit.getAccSchedule();
			}
			catch (Exception ex)
			{
				badShow(ex.Message);
				uiDisconnected();
				return;
			}
			//ConfigurationWindow confForm = new ConfigurationWindow();
			if (oUnit.modelCode == Unit.model_AGM1)
			{
				confForm = new AgmConfigurationWindow(conf, oUnit.modelCode);
			}
			else if (oUnit.modelCode == Unit.model_Gipsy6)
			{
				confForm = new GiPSy6ConfigurationMain(conf, oUnit.modelCode);
			}
			else if (oUnit.modelCode == Unit.model_axy5)
			{
				confForm = new Axy5ConfigurationWindow(conf, accSchedule, oUnit.firmTotA);
			}
			else if (oUnit.modelCode == Unit.model_axyTrek | oUnit.modelCode == Unit.model_axyQuattrok)
			{
				confForm = new TrekMovementConfigurationWindow(conf, oUnit.firmTotA, ref sp);
			}
			else if (oUnit.modelCode == Unit.model_drop_off)
			{
				confForm = new DropOffConfigurationWindow(conf, oUnit.firmTotA, ref sp);
			}
			else
			{
				confForm = new AxyConfigurationWindow(conf, oUnit.firmTotA);
			}

			confForm.Owner = this;
			System.Windows.Forms.Integration.ElementHost.EnableModelessKeyboardInterop(confForm);
			confForm.ShowDialog();

			if (confForm != null)
			{
				try
				{
					if (manageConfform)
					{
						if (confForm.mustWrite)
						{
							oUnit.setConf(confForm.axyConfOut);
							oUnit.setAccSchedule(confForm.axyScheduleOut);

							Ok okf = new Ok("Movement configuration succesfully updated.");
							okf.ShowDialog();
						}
						else
						{
							oUnit.abortConf();
						}
					}
					else
					{
						manageConfform = true;
					}

				}
				catch (Exception ex)
				{
					if (keepAliveTimer != null)
					{
						keepAliveTimer.Stop();
					}
					warningShow(ex.Message);
					uiDisconnected();
				}

				confForm = null;
			}
		}

		private void configurePoistionClick(object sender, RoutedEventArgs e)
		{

			ConfigurationWindow conf = null;
			int type = 0;

			if (oUnit == null)
			{
				var tg = new YesNo("No unit connected. Please select the schedule type you want to configure.", "UNIT TYPE", "", "TREK Family", "GIPSY family");
				type = tg.ShowDialog();
			}
			else
			{
				oUnit.closeSerialPort(sp);
				if ((oUnit is AxyTrek) | (oUnit is AxyQuattrok))
				{
					type = 1;
				}
				else if (oUnit is Gipsy6)
				{
					type = 2;
				}
			}

			if (type == 0)
			{
				return;
			}
			else if (type == 1)
			{
				conf = new TrekPositionConfigurationWindow(ref oUnit);
			}
			else
			{
				var tg = new YesNo("WARNING: you are entering the GiPSy6 bootloader!\r\nPlease, proceed only if you have a new firmware to upload, else please leave or your unit could potentially get bricked.", "GiPSy6 Bootloader", "", "Yes", "No");
				if (tg.ShowDialog() == 2) return;
				uiDisconnected();
				string portShortName = comPortComboBox.Text.Substring(comPortComboBox.Text.IndexOf("(") + 1);
				portShortName = portShortName.Remove(portShortName.IndexOf(")"), portShortName.Length - portShortName.IndexOf(")"));
				//ftdiSerialNumber = setLatency(portShortName, 1);
				var boot = new Bootloader.Bootloader_Gipsy6(true, this);
				boot.ShowDialog();
				return;
			}

			//conf = new TrekPositionConfigurationWindow(ref oUnit);

			conf.Owner = this;

			bool? uiAfter = conf.ShowDialog();

			if (uiAfter == false)
			{
				uiDisconnected();
			}

		}

		private void downloadButtonClick(object sender, RoutedEventArgs e)
		{
			oUnit.closeSerialPort(sp);
			uint[] memoryLogical;
			uint[] memoryPhysical;
			try
			{
				Thread.Sleep(3);
				memoryLogical = oUnit.askMemory();
				Thread.Sleep(3);
				memoryPhysical = oUnit.askMaxMemory();
			}
			catch (Exception ex)
			{
				badShow(ex.Message);
				uiDisconnected();
				return;
			}

			//Salvataggio stato progress bar
			double oldMax = statusProgressBar.Maximum;
			double oldMin = statusProgressBar.Minimum;
			double oldVal = statusProgressBar.Value;
			string oldCon = (string)statusLabel.Content;
			statusProgressBar.IsIndeterminate = true;
			statusLabel.Content = "Downloading...";
			//lastSettings = File.ReadAllLines(iniFile);

			if (getParameter("downloadMode").Equals("A"))    //Controllo memoria vuota
			{
				if (oUnit.mem_address == oUnit.mem_max_logical_address)
				{
					statusProgressBar.Maximum = oldMax;
					statusProgressBar.Minimum = oldMin;
					statusProgressBar.Value = oldVal;
					statusLabel.Content = oldCon;
					statusProgressBar.IsIndeterminate = false;
					warningShow(STR_memoryEMpty);
					return;
				}
			}
			Microsoft.Win32.SaveFileDialog saveRaw = new Microsoft.Win32.SaveFileDialog();
			saveRaw.OverwritePrompt = true;
			saveRaw.AddExtension = true;
			saveRaw.FileName = unitNameTextBox.Text;

			saveRaw.DefaultExt = "Ard file|*." + oUnit.defaultArdExtension;
			saveRaw.Filter = "Ard file|*." + oUnit.defaultArdExtension;

			if (File.Exists(getParameter("dataSavePath")))
			{
				saveRaw.InitialDirectory = getParameter("dataSavePath");
			}

			if ((saveRaw.ShowDialog() == false))
			{
				statusProgressBar.Maximum = oldMax;
				statusProgressBar.Minimum = oldMin;
				statusProgressBar.Value = oldVal;
				statusLabel.Content = oldCon;
				statusProgressBar.IsIndeterminate = false;
				return;
			}
			updateParameter("dataSavePath", System.IO.Path.GetDirectoryName(saveRaw.FileName));
			//lastSettings[3] = System.IO.Path.GetDirectoryName(saveRaw.FileName);
			//File.WriteAllLines(iniFile, lastSettings);
			UInt32 fromMemory = 0;
			UInt32 toMemory = 0;

			if (getParameter("downloadMode").Equals("A"))
			{
				if ((oUnit is Axy5) | (oUnit is Gipsy6))
				{
					fromMemory = oUnit.mem_max_logical_address;
					toMemory = oUnit.mem_address;
				}
				else          //Chiede di sovrascrivere o continuare il download in caso di memorie senza effetto pacman
				{
					if (File.Exists((System.IO.Path.GetDirectoryName(saveRaw.FileName) + ("\\" + (System.IO.Path.GetFileNameWithoutExtension(saveRaw.FileName) + ".mdp")))))
					{
						YesNo yn = new YesNo(STR_resumeDownloadQuestion, "RESUME?", "", STR_Resume, STR_Restart);
						switch (yn.ShowDialog())
						{
							case 0:
								statusProgressBar.Maximum = oldMax;
								statusProgressBar.Minimum = oldMin;
								statusProgressBar.Value = oldVal;
								statusLabel.Content = oldCon;
								statusProgressBar.IsIndeterminate = false;
								return;
							case 1:
								FileInfo fi = new FileInfo((System.IO.Path.GetDirectoryName(saveRaw.FileName) + ("\\" + (System.IO.Path.GetFileNameWithoutExtension(saveRaw.FileName) + ".mdp"))));
								fromMemory = Convert.ToUInt32(fi.Length);
								break;
							case 2:
								fromMemory = 0;
								break;
						}
					}
					toMemory = (memoryLogical[1] & 0xfffff000) + 0x1000;
				}
			}
			else
			{
				var dr = new DownloadRangeInput();
				if (oUnit is Axy5)
				{
					dr.startAddress = memoryLogical[0];
					dr.finalAddress = memoryLogical[1] & 0xfffff000;
				}
				else if (oUnit is Gipsy6)
				{
					dr.startAddress = memoryLogical[0];
					dr.finalAddress = (memoryLogical[1] & 0xfffff000) + 0x1000;
				}
				else
				{
					dr.startAddress = 0;
					dr.finalAddress = ((memoryLogical[0] / 4096) * 4096) + 4096;
				}

				if (!(bool)dr.ShowDialog())
				{
					statusProgressBar.Maximum = oldMax;
					statusProgressBar.Minimum = oldMin;
					statusProgressBar.Value = oldVal;
					statusLabel.Content = oldCon;
					statusProgressBar.IsIndeterminate = false;
					return;
				}
				fromMemory = dr.startAddress;
				toMemory = dr.finalAddress;
				if ((oUnit is Gipsy6) | (oUnit is Axy5))
				{
					oUnit.mem_address = toMemory;
					oUnit.mem_max_logical_address = fromMemory;
				}

			}


			int baudrate = 3000000;
			if (speedLegacy.IsChecked)
			{
				baudrate = Baudrate_base;
			}
			else if (speed1.IsChecked)
			{
				baudrate = Baudrate_1M;
			}
			else if (speed2.IsChecked)
			{
				baudrate = Baudrate_2M;
			}
			else
			{
				baudrate = Baudrate_3M;
			}

			// sviluppo per remoto
			if (remote)
			{
				baudrate = 115200;
			}

			// /sviluppo
			mainGrid.IsEnabled = false;
			Thread downloadThread = new Thread(() => oUnit.download(saveRaw.FileName, fromMemory, toMemory, baudrate));
			if (oUnit.remote)
			{
				downloadThread = new Thread(() => oUnit.downloadRemote(saveRaw.FileName, fromMemory, toMemory, baudrate));
			}

			downloadThread.SetApartmentState(ApartmentState.STA);
			downloadThread.Start();
		}

		private void convertDataClick(object sender, RoutedEventArgs e)
		{
			askOverwrite = true;
			//lastSettings = System.IO.File.ReadAllLines(iniFile);
			Microsoft.Win32.OpenFileDialog fOpen = new Microsoft.Win32.OpenFileDialog();
			if (File.Exists(getParameter("convertPath")))
			{
				fOpen.InitialDirectory = getParameter("convertPath");
			}

			//fOpen.FileName = "*.ard";
			fOpen.Filter = "Sensor Raw Data | *.ard;*.rem;*.gp6;*.memDump;*.mdp";

			fOpen.Multiselect = true;
			if ((fOpen.ShowDialog() == false))
			{
				return;
			}

			updateParameter("convertPath", System.IO.Path.GetDirectoryName(fOpen.FileName));
			//File.WriteAllLines(iniFile, lastSettings);
			convFiles = new List<string>();
			convFiles.AddRange(fOpen.FileNames);
			convertDataLaunch();
		}

		private void convertDataLaunch()
		{
			ConversionPreferences cp = new ConversionPreferences();
			cp.Owner = this;
			cp.ShowDialog();
			if ((cp.goOn == false))
			{
				if (!askOverwrite)  //In caso di chiamata esterna, termina l'app
				{
					Close();
				}
				return;
			}

			stDebugLevel = cp.debugLevel;
			stOldUnitDebug = cp.OldUnitDebug;
			addGpsTime = cp.addGpsTime;

			mainGrid.IsEnabled = false;
			convFileTot = (ushort)convFiles.Count;
			convFile = 0;
			if ((convFileTot != 0))
			{
				nextFile();
			}
		}

		private void remoteClick(object sender, RoutedEventArgs e)
		{
			if (connectButton.Content.Equals("Connect"))
			{
				sp.BaudRate = Baudrate_base;
				sp.ReadTimeout = 400;
				sp.NewLine = "\r\n";

				//isola la porta COM selezionata
				string portShortName;
				portShortName = comPortComboBox.Text.Substring(comPortComboBox.Text.IndexOf("(") + 1);
				try
				{
					portShortName = portShortName.Remove(portShortName.IndexOf(")"), portShortName.Length - portShortName.IndexOf(")"));
				}
				catch
				{
					MessageBox.Show(STR_noComPortAvailable);
					return;
				}

				if (sp.IsOpen) sp.Close();
				sp.PortName = portShortName;

				//Imposta a 1ms il latency del buffer ftdi e tenta di aprire la porta
				try
				{
					sp.Open();
					ftdiSerialNumber = setLatency(portShortName, 1);
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message);
					return;
				}
				if (sp.IsOpen) sp.Close();

				var remoteManagement = new RemoteManagement(ref sp, this, portShortName);
				remoteManagement.ShowDialog();
				if ((string)connectButton.Content == "Disconnect")
				{
					if (sp.IsOpen == false)
					{
						try
						{
							sp.Open();
						}
						catch { }
					}
				}
				return;

				//var remote = new RemoteConnector(ref sp);
				//if (remote.ShowDialog() == 1)
				//{
				//	remoteButton.IsEnabled = false;
				//	connectClick(connectButton, new System.Windows.RoutedEventArgs());
				//}
				//else if (remote.ShowDialog() == 2)
				//{
				//	remoteButton.Content = "Configure Remote Unit";
				//	connectClick(connectButton, new System.Windows.RoutedEventArgs());
				//}
			}
			else
			{
				if (remoteButton.Content.ToString().Contains("figure"))
				{
					//isola la porta COM selezionata
					string portShortName;
					portShortName = comPortComboBox.Text.Substring(comPortComboBox.Text.IndexOf("(") + 1);
					try
					{
						portShortName = portShortName.Remove(portShortName.IndexOf(")"), portShortName.Length - portShortName.IndexOf(")"));
					}
					catch
					{
						MessageBox.Show(STR_noComPortAvailable);
						return;
					}

					if (sp.IsOpen) sp.Close();
					sp.PortName = portShortName;

					//Imposta a 1ms il latency del buffer ftdi e tenta di aprire la porta
					ftdiSerialNumber = setLatency(portShortName, 1);
					try
					{
						sp.Open();
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message);
						return;
					}
					if (sp.IsOpen) sp.Close();
					//Far partire finestra di configurazione unità remota con indirizzo 0xffffff
				}
			}
		}

		void realTimeClick(object sender, RoutedEventArgs e)
		{
			//Implementare il tipo di realtime a seconda dell'unità connessa (per ora solo AGM)
			object charts;
			switch (realTimeType)
			{
				case (0):
					return;
				case (2):
					oUnit.closeSerialPort(sp);
					charts = new ChartWindowAGM(sp.PortName);
					((ChartWindowAGM)charts).WindowState = WindowState.Maximized;
					((ChartWindowAGM)charts).ShowDialog();
					break;
			}

		}

		public bool externConnect(int baudRate)
		{
			Baudrate_base = baudRate;
			connectClick(this, new RoutedEventArgs());
			if (unitConnected && oUnit is Gipsy6)       //In caso di gipsy6 remoto disabilita il pulsante per l'upload del firmware
			{
				keepAliveTimer = new System.Timers.Timer();
				keepAliveTimer.Elapsed += keepAliveTimerElapsed;
				keepAliveTimer.Interval = 5000;
				keepAliveTimer.AutoReset = true;
				keepAliveTimer.Enabled = true;
				configurePositionButton.IsEnabled = false;
			}
			return connectButton.Content.Equals("Disconnect");
		}

		void refreshBattery(object sender, RoutedEventArgs e)
		{
			try
			{
				batteryLabel.Content = oUnit.askBattery();
				//BackgroundWorker brBW = new BackgroundWorker();
				//brBW.DoWork += new DoWorkEventHandler(rotateBattery);
				//brBW.RunWorkerAsync();
			}
			catch (Exception ex)
			{
				warningShow(ex.Message);
				uiDisconnected();
			}
		}

		//void rotateBattery(object sender, DoWorkEventArgs e)
		//{
		//	Storyboard sb = new Storyboard();

		//	// Create a DoubleAnimation to animate the width of the button.
		//	DoubleAnimation myDoubleAnimation = new DoubleAnimation();
		//	myDoubleAnimation.From = 0;
		//	myDoubleAnimation.To = 360;
		//	myDoubleAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(10000));

		//	Storyboard.SetTargetName(myDoubleAnimation, "rtAngle");
		//	PropertyPath PropP = new PropertyPath(RotateTransform.AngleProperty);
		//	Storyboard.SetTargetProperty(myDoubleAnimation, PropP);

		//	sb.Children.Add(myDoubleAnimation);
		//	sb.Begin(batteryRefreshI);
		//}
		#endregion

		#region Unità

		void stopThreadOperations(object sender, RoutedEventArgs e)
		{
			try
			{
				oUnit.convertStop = true;
				return;
			}
			catch { }

			try
			{
				cUnit.convertStop = true;
			}
			catch { }
		}

		private void setName()
		{
			if (string.IsNullOrEmpty(unitNameTextBox.Text)) return;
			oUnit.closeSerialPort(sp);
			try
			{
				oUnit.setName(unitNameTextBox.Text);
			}
			catch (Exception ex)
			{
				warningShow(ex.Message);
				uiDisconnected();
				return;
			}

			goodShow("New name set.");
			statusLabel.Content = ("New name set.");

		}

		private void getConf()
		{
			positionCanSend = oUnit.positionCanSend;
			oUnit.closeSerialPort(sp);
			try
			{
				Thread.Sleep(10);
				oUnit.changeBaudrate(ref sp, 1);
				Thread.Sleep(10);
				firmwareLabel.Content = oUnit.askFirmware();
				Thread.Sleep(10);
				unitNameTextBox.Text = oUnit.askName();
				Thread.Sleep(10);
				if (!(oUnit is Gipsy6)) sp.ReadExisting();
				Thread.Sleep(10);
				batteryLabel.Content = oUnit.askBattery();
				Thread.Sleep(10);
				uint[] maxM = oUnit.askMaxMemory();
				Thread.Sleep(10);
				uint[] actM = oUnit.askMemory();
				Thread.Sleep(10);
				setPBMemory(actM, maxM);
				oUnit.getCoeffs();
				Thread.Sleep(10);
				oUnit.setPcTime();
				Thread.Sleep(10);
				realTimeType = oUnit.askRealTime();
				uiConnected();
			}
			catch (Exception ex)
			{
				warningShow(ex.Message);
				uiDisconnected();
			}
		}

		private void getRemote()
		{
			oUnit.closeSerialPort(sp);
			Thread.Sleep(10);
			remote = false;
			string title = "X MANAGER";
			if (oUnit.getRemote())
			{
				remote = true;
				title += " REMOTE";
			}
			Title = title;
		}

		private void getSolar()
		{
			oUnit.closeSerialPort(sp);
			Thread.Sleep(10);
			remote = false;
			string title = "X MANAGER";
			if (oUnit.getRemote())
			{
				remote = true;
				title += " REMOTE";
			}
			this.Title = title;
		}

		//private void spurgo()
		//{
		//	oUnit.closeSerialPort(sp);
		//	while (sp.BytesToRead != 0) sp.ReadByte();
		//}

		private void keepAliveTimerElapsed(Object source, System.Timers.ElapsedEventArgs e)
		{
			try
			{
				oUnit.keepAlive();
			}
			catch
			{
				keepAliveTimer.Stop();
				if (confForm != null)
				{
					try
					{
						manageConfform = false;
						Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => confForm.Close()));
					}
					catch { }
					confForm = null;
				}
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => uiDisconnected()));
				//uiDisconnected();
			}

		}
		
	public ref Unit getReferenceUnit()
		{
			return ref oUnit;
		}

		#endregion

		#region Finestre di servizio
		private void warningShow(string t)
		{
			string pUri = "pack://application:,,,/Resources/alert2.png";
			Warning w = new Warning(t);
			w.Owner = this;
			w.picUri = pUri;
			w.ShowDialog();
		}

		private void goodShow(string t)
		{
			string pUri = "pack://application:,,,/Resources/ok.png";
			Warning w = new Warning(t);
			w.Owner = this;
			w.picUri = pUri;
			w.Title = "INFO";
			w.ShowDialog();
		}

		private void badShow(string t)
		{
			string pUri = "pack://application:,,,/Resources/bad.png";
			Warning w = new Warning(t);
			w.Owner = this;
			w.picUri = pUri;
			w.ShowDialog();
		}

		#endregion

		#region Picture Manager

		private ImageSource ImageSourceForBitmap(System.Drawing.Bitmap bmp)
		{
			var handle = bmp.GetHbitmap();
			try
			{
				return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
			}
			finally { DeleteObject(handle); }
		}
		private void initPicture()
		{
			if (getParameter("backgroundImagePath").Contains("null") | !File.Exists(getParameter("backgroundImagePath")))
			{
				var png = new BitmapImage();
				png.BeginInit();

				png.UriSource = new Uri("pack://application:,,,/Resources/technosmartLogoBlurred.png", UriKind.Absolute);
				png.EndInit();
				pictureBox.Source = png;
				return;
			}
			else
			{
				//pictureBox.Source= ImageSourceForBitmap(new Uri(lastSettings[0]));
				var png = new BitmapImage();
				png.BeginInit();
				png.UriSource = new Uri(getParameter("backgroundImagePath"), UriKind.Absolute);
				png.EndInit();
				pictureBox.Source = png;
			}
		}

		private void pictureStackPanel_DragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				e.Effects = DragDropEffects.Copy;
			}
		}

		private void pictureStackPanel_DragDrop(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				string[] files;
				UInt16 newCount = 0;
				files = (string[])e.Data.GetData(DataFormats.FileDrop);
				if (files.Length == 1)
				{
					if (System.IO.Path.GetExtension(files[0]) == ".jpg" | System.IO.Path.GetExtension(files[0]) == ".bmp" | System.IO.Path.GetExtension(files[0]) == ".png")
					{
						updateParameter("backgroundImagePath", files[0]);
						//File.WriteAllLines(iniFile, lastSettings);
						var bmp = new BitmapImage();
						bmp.BeginInit();
						bmp.UriSource = new Uri(files[0], UriKind.Absolute);
						bmp.EndInit();
						pictureBox.Source = bmp;
						return;
					}
					else if (System.IO.Path.GetExtension(files[0]) == ".mdp" | System.IO.Path.GetExtension(files[0]) == ".memDump")
					{
						//Implementare il fakeDownload dalle unità!
					}
				}

				List<string> fAr = new List<string>();
				for (int i = 0; i < files.Length; i++)
				{
					if (System.IO.Path.GetExtension(files[i]) == ".ard")
					{
						newCount += 1;
					}
					fAr.Add(files[i]);
				}
				if (newCount == 0)
				{
					MessageBox.Show("No compatible file selected.");
					return;
				}

				var cp = new ConversionPreferences();
				cp.Owner = this;
				cp.ShowDialog();
				if (!cp.goOn) return;
				foreach (string filename in fAr)
				{
#pragma warning disable
					if (System.IO.Path.GetExtension(filename).Contains("ard")) ;
					askOverwrite = true;
					//Implementare drag drop di file ard
#pragma warning restore

				}


			}
		}

		private void binConvSub(string filename)
		{
			string filnameIn = filename;
			string filenameOut = System.IO.Path.GetDirectoryName(filename) + "\\" + System.IO.Path.GetFileNameWithoutExtension(filename) + ".bin";
			string[] dataIn = System.IO.File.ReadAllLines(filnameIn);
			byte[] dataOut = new byte[dataIn.Length];
			UInt32 counter = 0;
			foreach (string s in dataIn)
			{
				if (s != "")
				{
					string sOut = s.Substring(2);
					dataOut[counter] = Convert.ToByte(sOut, 16);
					counter++;
				}
			}
			System.IO.File.WriteAllBytes(filenameOut, dataOut);
		}

		#endregion

		#region Servizi FTDI



		#endregion

		#region Conversione

		public override void nextFile()
		{
			const int type_ard = 1;
			const int type_rem = 2;
			const int type_mdp = 3;
			const int type_gp6 = 4;


			convFile++;
			int fileType = type_ard;
			string fileHeader = "ARD ";
			string fileName = "";
			string fileNameCsv;
			string fileNametxt;
			string fileNameKml;
			string fileNamePlaceMark;
			string[] nomiFile = new string[] { "" };
			string addOn = "";
			bool GoOn = true;

			try
			{
				if (cUnit != null)
				{
					if (cUnit.convertStop)
					{
						GoOn = false;
						cUnit.convertStop = false;
					}
					cUnit.Dispose();
					cUnit = null;
				}
			}
			catch { }

			try
			{
				fileName = convFiles[0];
			}
			catch
			{
				GoOn = false;
				if (!askOverwrite)  //Chiamata esterna: al termine della conversione chiude il programma
				{
					Close();
				}
			}

			if (GoOn)
			{
				string exten = System.IO.Path.GetExtension(fileName);
				if ((exten.Length > 4))
				{
					addOn = ("_S" + exten.Remove(0, 4));
				}
				fileNameCsv = System.IO.Path.GetDirectoryName(fileName) + "\\" + System.IO.Path.GetFileNameWithoutExtension(fileName) + addOn + ".csv";
				fileNametxt = System.IO.Path.GetDirectoryName(fileName) + "\\" + System.IO.Path.GetFileNameWithoutExtension(fileName) + addOn + ".txt";
				fileNameKml = System.IO.Path.GetDirectoryName(fileName) + "\\" + System.IO.Path.GetFileNameWithoutExtension(fileName) + addOn + "_temp" + ".kml";
				fileNamePlaceMark = System.IO.Path.GetDirectoryName(fileName) + "\\" + System.IO.Path.GetFileNameWithoutExtension(fileName) + addOn + ".kml";
				nomiFile = new string[] { fileNameCsv, fileNametxt, fileNamePlaceMark };
			}

			if (GoOn)
			{
				if ((System.IO.Path.GetExtension(fileName).Contains("Dump") || (System.IO.Path.GetExtension(fileName).Contains("dump") || System.IO.Path.GetExtension(fileName).Contains("mdp"))))
				{
					fileHeader = "MEMDUMP ";
					fileType = type_mdp;
				}
				else
				{
					if (System.IO.Path.GetExtension(fileName).Contains("rem"))
					{
						fileHeader = "REM ";
					}
					if (System.IO.Path.GetExtension(fileName).Contains("gp6"))
					{
						fileType = type_gp6;
					}
					foreach (string nomefile in nomiFile)
					{
						if (File.Exists(nomefile) & askOverwrite)
						{
							YesNo yn = new YesNo((System.IO.Path.GetFileName(nomefile) + " already exists. Do you want to overwrite it?"), "OVERWRITE");
							if ((yn.ShowDialog() == YesNo.no))
							{
								GoOn = false;
								yn.Close();
								break;
							}
							yn.Close();
						}
					}
				}
			}

			if (GoOn)
			{
				foreach (string nomefile in nomiFile)
				{
					try
					{
						FileSystem.DeleteFile(nomefile);
						//File.Delete(nomefile);

					}
					catch { }
				}
			}

			if (!GoOn)
			{
				mainGrid.IsEnabled = true;
				statusProgressBar.IsIndeterminate = false;
				statusLabel.Content = "Done.";
				statusProgressBar.Value = 0;
				progressBarStopButton.IsEnabled = false;
				progressBarStopButtonColumn.Width = new GridLength(0);
				if (!(oUnit == null))
				{
					if (oUnit.connected)
					{
						Thread.Sleep(10);
						uint[] maxM = oUnit.askMaxMemory();
						Thread.Sleep(10);
						uint[] actM = oUnit.askMemory();
						setPBMemory(actM, maxM);
						statusLabel.Content = "Connected.";
					}
				}
				return;
			}

			statusProgressBar.IsIndeterminate = true;

			statusLabel.Content = fileHeader + "File " + convFile.ToString() + "/" + convFileTot.ToString() + ": " + System.IO.Path.GetFileName(fileName) + " ";
			convFiles.RemoveAt(0);
			FileStream fs = File.OpenRead(fileName);
			byte model;
			byte fw = 0; //COntrollare cosa succede in caso di ardfile=false alla riga 1555 e poi al caso successivo (Depth)
			if (fileType == type_ard)
			{
				model = (byte)fs.ReadByte();
			}
			else if (fileType == type_gp6)
			{
				fs.Position = fs.Length - 2;
				model = (byte)fs.ReadByte();
			}
			else
			{
				try
				{
					model = findMdpModel(ref fs)[0]; //Implementare
					fw = findMdpModel(ref fs)[1];  //Implementare
				}
				catch (Exception ex)
				{
					warningShow(fileName + ": " + ex.Message);
					nextFile();
					return;
				}

			}

			switch (model)
			{
				case Unit.model_axy3:
					cUnit = new Axy3(this);
					break;
				case Unit.model_axy4:
					if (fileType == type_ard) fw = (byte)fs.ReadByte();
					if ((fw < 2)) cUnit = new Axy4_1(this);
					else cUnit = new Axy4_2(this);
					break;
				case Unit.model_axy5:
					cUnit = new Axy5(this);
					break;
				case Unit.model_axyDepth:
					if (fileType == type_ard) fw = (byte)fs.ReadByte();
					if ((fw < 2)) cUnit = new AxyDepth_1(this);
					else cUnit = new AxyDepth_2(this);
					break;
				case Unit.model_axyTrek:
					cUnit = new AxyTrek(this);
					break;
				case Unit.model_axyQuattrok:
					cUnit = new AxyQuattrok(this);
					break;
				case Unit.model_AGM1:
					cUnit = new AGM(this);
					break;
				case Unit.model_Co2Logger:
					cUnit = new CO2_Logger(this);
					break;
				case Unit.model_Gipsy6:
					cUnit = new Gipsy6(this);
					break;
				default:
					cUnit = new AxyTrek(this);
					break;
			}

			UInt32 FileLength = (UInt32)fs.Length;
			fs.Close();
			mainGrid.IsEnabled = false;
			cUnit.convertStop = false;
			Thread conversionThread;
			string[] prefsOut = File.ReadAllLines(prefFile);
			if ((fileType == type_ard) | (fileType == type_rem) | (fileType == type_gp6))
			{
				statusProgressBar.Maximum = FileLength;
				progressBarStopButton.IsEnabled = true;
				progressBarStopButtonColumn.Width = new GridLength(80);
				conversionThread = new Thread(() => cUnit.convert(fileName, prefsOut));
			}
			else
			{
				conversionThread = new Thread(() => cUnit.extractArds(fileName, fileName, false));
			}

			conversionThread.SetApartmentState(ApartmentState.STA);
			conversionThread.Start();
		}

		private void findFirmware(string fn, byte unitType, ref uint fTotA, ref uint fTotB, ref byte[] uf)
		{
			var fileIn = new BinaryReader(System.IO.File.Open(fn, FileMode.Open));
			fileIn.ReadByte();

			switch (unitType)
			{
				case (byte)Unit.model_axyTrek:
					fileIn.Read(uf, 0, 6);
					fTotA = uf[0] * (uint)1000000 + uf[1] * (uint)1000 + uf[2];
					fTotB = uf[3] * (uint)1000000 + uf[4] * (uint)1000 + uf[5];
					uf[6] = 254;
					break;
				case Unit.model_axy3:
					fileIn.Read(uf, 0, 2);
					fTotA = uf[0] * (uint)1000 + uf[1];
					uf[2] = 254;
					break;
				case Unit.model_axy4:
				case Unit.model_axyDepth:
					fileIn.Read(uf, 0, 2);
					fTotA = uf[0] * (uint)1000 + uf[1];
					uf[2] = 254;
					if (fTotA > 2004)
					{
						uf[2] = fileIn.ReadByte();
						fTotA = fTotA * (uint)1000 + uf[2];
						uf[3] = 254;
					}
					break;
				case Unit.model_AGM1:
					fileIn.Read(uf, 0, 3);
					fTotA = uf[0] * (uint)1000000 + uf[1] * (uint)1000 + uf[2];
					uf[3] = 254;
					break;
			}

		}

		private byte[] findMdpModel(ref FileStream iFile)
		{
			long pos = iFile.Position;
			byte[] outt = new byte[2];
			iFile.Position = iFile.Length - 1;
			if (iFile.ReadByte() == 254)
			{
				iFile.Position = iFile.Length - 2;
				outt[0] = (byte)iFile.ReadByte();
				iFile.Position = iFile.Length - 5;
				outt[1] = (byte)iFile.ReadByte();
			}
			else
			{
				outt[0] = 0xff;
				outt[1] = 0xff;
				iFile.Close();
				throw new Exception("Unknown mdp model");
			}
			iFile.Position = pos;
			return outt;
		}

		public override string getStatusLabelContent()
		{
			return (string)statusLabel.Content;
		}

		private void Window_Closing(object sender, CancelEventArgs e)
		{

		}



		#endregion


		//internal static int FT_OpenEx(string p1, FT_HANDLE p2, ref int FT_Handle)
		//{
		//    throw new NotImplementedException();
		//}
	}

}
