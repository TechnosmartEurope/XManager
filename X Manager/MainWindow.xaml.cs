using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualBasic.FileIO;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.ComponentModel;
using X_Manager.Units;
using X_Manager.ConfigurationWindows;
using X_Manager.Remote;
using X_Manager.Units.AxyTreks;
using X_Manager.Units.Gipsy6;
using System.Windows.Controls.Primitives;

namespace X_Manager
{
	public partial class MainWindow : Parent
	{

		public static Random random = new Random();
		//sviluppo
		private int start_error_code = 0;
		///sviluppo
		#region Dichiarazioni

		Unit oUnit;
		Unit cUnit;
		//ConfigurationWindow confForm;
		public int Baudrate_base = 115200;
		public const int Baudrate_1M = 1000000;
		public const int Baudrate_2M = 2000000;
		public const int Baudrate_3M = 3000000;

		public bool realTimeStatus = false;
		public bool convertStop = false;
		public bool unitConnected = false;
		//List<bool> controlStatus;
		//ChartWindow charts;
		UInt16 convFileTot;
		UInt16 convFile;
		int realTimeType = 0;

		bool askOverwrite = true;
		bool remoteConnection = false;

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

		public const string INI_BACKGROUND_IMAGE_PATH = "backgroundImagePath";
		public const string INI_DATA_SAVE_PATH = "dataSavePath";
		public const string INI_CONVERT_OPEN_PATH = "convertPath";
		public const string INI_PRESSURE_RANGE = "pressureRange";
		public const string INI_KEEP_MDP = "keepMdp";
		public const string INI_DOWNLOAD_SPEED = "downloadSpeed";
		public const string INI_CSV_SEPARATOR = "csvSeparator";
		public const string INI_DOWNLOAD_MDOE = "downloadMode";
		public const string INI_TREK_SCHEDULE_SAVE_PATH = "trekScheduleSavePath";
		public const string INI_TREK_SCHEDULE_OPEN_PATH = "trekScheduleOpenPath";
		public const string INI_AXY5_SCHEDULE_PATH = "axy5SchedulePath";
		public const string INI_GIPSY6_SCHEDULE_PATH = "gipsy6ConfigurationsFolder";
		public const string INI_GIPSY6_EXPERT_MODE = "gipsy6ConfigurationExpertMode";
		public const string INI_GIPSY6_BOOTLOADER_WIPE_DATA = "gipsy6BootloaderWipeData";
		public const string INI_GIPSY6_BOOTLOADER_WIPE_SETTINGS = "gipsy6BootloaderWipeSettings";


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

		public static FTDI_Device FTDI;

		#endregion

		#region Interfaccia

		public MainWindow()
		{
			InitializeComponent();
			FTDI = null;
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
			args = Environment.GetCommandLineArgs().ToList();
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
			try
			{
				Console.WriteLine("Loaded.");
				//sviluppo
				start_error_code = 0;
				///sviluppo
				loadUserPrefs();
				//sviluppo
				start_error_code = 1;
				///sviluppo
				uiDisconnected();
				//sviluppo
				start_error_code = 2;
				///sviluppo
				initPicture();
				//sviluppo
				start_error_code = 3;
				///sviluppo
				//scanButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
				scanPorts(true);
				//sviluppo
				start_error_code = 8;
				///sviluppo
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
				//sviluppo
				start_error_code = 9;
				///sviluppo
				//keepMdpItem.IsChecked = false;
				//if (lastSettings[6] == "true") keepMdpItem.IsChecked = true;
				//selectDownloadSpeed(lastSettings[7]);
				//csvSeparatorChanged(lastSettings[8]);
				selectDownloadSpeed(getParameter("downloadSpeed", "3"));
				//sviluppo
				start_error_code = 10;
				///sviluppo
				csvSeparatorChanged(getParameter("csvSeparator", "\t"));
				//sviluppo
				start_error_code = 11;
				///sviluppo
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
				//sviluppo
				start_error_code = 12;
				///sviluppo
				downloadManual.Checked += downloadModeChecked;
				downloadManual.Unchecked += downloadModeChecked;
				downloadAutomatic.Checked += downloadModeChecked;
				downloadAutomatic.Unchecked += downloadModeChecked;
				//sviluppo
				start_error_code = 13;
				///sviluppo
				progressBarStopButton.IsEnabled = false;
				progressBarStopButtonColumn.Width = new GridLength(0);
				//sviluppo
				start_error_code = 14;
				///sviluppo

				if (convFiles.Count > 0)
				{
					convertDataLaunch(convFiles);
				}
				//sviluppo
				start_error_code = 15;
				///sviluppo
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
#endif
				//var confForm = new GiPSy6ConfigurationMain(conf, Unit.model_Gipsy6);
				//confForm.ShowDialog();
				windowMovingEnded(this, new EventArgs());
				//sviluppo
				start_error_code = 16;
				///sviluppo
			}
			catch (Exception ex)
			{
				string mess = "S_ERR_CODE: " + start_error_code.ToString() + "\r\n" + ex.Message;
				MessageBox.Show(mess);
			}

		}

		private void locationChanged(object sender, EventArgs e)
		{
			windowMovingTimer.Stop();
			windowMovingTimer.Start();
		}

		private void windowMovingEnded(object sender, EventArgs e)
		{
			try
			{
				var scr = System.Windows.Forms.Screen.AllScreens;

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
			}
			catch
			{
				LocationChanged -= locationChanged;
				windowMovingTimer.Stop();
				MessageBox.Show("WR_Err");
			}

			//if (Left < screen.Bounds.X)
			//{
			//	Left = screen.Bounds.X + 5;
			//}
			//if (Top < screen.Bounds.Y)
			//{
			//	Top = screen.Bounds.Y + 5;
			//}

			//MessageBox.Show(screen.DeviceName + "  scale: " + scale.ToString() + "\r\nWidth: " + Width.ToString() + " Height: " + Height.ToString() +
			//	"\r\nScale Transform: " + st.ToString());
		}

		private void ComPortComboBox_DropDownOpened(object sender, EventArgs e)
		{
			scanPorts(false);
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
			remoteConnection = false;
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
			remoteButton.Content = "Remote Management";
			remoteButton.IsEnabled = true;
			Title = "X MANAGER";
			configureMovementButton.Content = "Accelerometer configuration";
			realTimeSP.Visibility = Visibility.Hidden;
			configurePositionButton.Visibility = Visibility.Visible;
			configureMovementButton.Content = "Accelerometer Configuration";
			configurePositionButton.Content = "GPS configuration";
			manageConfform = true;
			dumpBaudrateCB.Visibility = Visibility.Hidden;
			dumpClearB.Visibility = Visibility.Hidden;
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
				case Unit.model_Gipsy6N:
				case Unit.model_Gipsy6XS:
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
				if (FTDI is null)
				{
					MessageBox.Show("Please, connect a data cable first");
					return;
				}

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
						FTDI.Write("TTTTTTTTTTTTTGGA4");
					}
					Console.Beep();
				}

				//Axy4d come axy depth
				else if (e.Key == Key.D5)
				{
					if (connectButton.Content.Equals("Connect"))
					{
						FTDI.Write("TTTTTTTTTTTTTGGA5");
					}

					Console.Beep();
				}

				//Axy4d come axy depth
				else if (e.Key == Key.D6)
				{
					if (connectButton.Content.Equals("Connect"))
					{
						FTDI.Write("TTTTTTTTTTTTTGGA6");
					}

					Console.Beep();
				}

				//Trek o Quattrok locale
				else if (e.Key == Key.L)
				{
					startUpMonitor.Text = "";
					if (connectButton.Content.Equals("Connect"))
					{
						FTDI.Write("T");
						Thread.Sleep(5);
						FTDI.Write("TTTTTTTTTTTTTGGAr");
						Console.Beep();
					}

				}

				//Trek o Quattrok remoto
				else if (e.Key == Key.R)
				{
					startUpMonitor.Text = "";
					if (connectButton.Content.Equals("Connect"))
					{
						FTDI.Write("T");
						Thread.Sleep(5);
						FTDI.Write("TTTTTTTTTTTTTGGAR");
					}
					Console.Beep();
				}

				//Trek o Quattrok solare
				else if (e.Key == Key.S)
				{
					startUpMonitor.Text = "";
					if (connectButton.Content.Equals("Connect"))
					{
						FTDI.Write("T");
						Thread.Sleep(5);
						FTDI.Write("TTTTTTTTTTTTTGGAS");
					}
					Console.Beep();
				}

				//Trek o Quattrok non solare
				else if (e.Key == Key.N)
				{
					startUpMonitor.Text = "";
					if (connectButton.Content.Equals("Connect"))
					{
						FTDI.Write("T");
						Thread.Sleep(5);
						FTDI.Write("TTTTTTTTTTTTTGGAs");
					}
					Console.Beep();
				}

				else if (e.Key == Key.B)
				{
					if (!((string)configurePositionButton.Content).Contains("firmware"))
					{
						uiDisconnected();
						FTDI.BaudRate = 1000000;
						var boot = new Bootloader.Bootloader_Gipsy6(false, "CC1352");
						boot.ShowDialog();
					}
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
				FTDI.ReadExisting();
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

		public override void downloadIncomplete()
		{
			warningShow("Download incomplete!");
			mainGrid.IsEnabled = true;
			statusLabel.Content = "Connected";
			progressBarStopButton.IsEnabled = false;
			progressBarStopButtonColumn.Width = new GridLength(0);
			try
			{
				FTDI.ReadExisting();
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
			System.Diagnostics.Process.Start(getParameter(INI_DATA_SAVE_PATH));
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
			}
			else
			{
				if (FTDI is null)
				{
					mainTabControl.SelectedIndex = 0;
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
				dumpBaudrateCB.Visibility = Visibility.Visible;
				dumpClearB.Visibility = Visibility.Visible;

				startUpMonitor.Text = "";

				string baudrate = "";
				baudrate = ((ComboBoxItem)dumpBaudrateCB.SelectedItem).Content as string;
				baudrate = baudrate.Split(' ')[0];
				FTDI.BaudRate = uint.Parse(baudrate);

				startUpMonitorBW = new BackgroundWorker();
				startUpMonitorBW.WorkerReportsProgress = false;
				startUpMonitorBW.WorkerSupportsCancellation = true;

				startUpMonitorBW.DoWork += startUpMonitorBW_DoWork;
				startUpMonitorBW.RunWorkerCompleted += startUpMonitorBW_RunWorkerCompleted;

				startUpMonitorBW.RunWorkerAsync();
				mainTabControl.Focus();
			}
		}

		private void dumpBaudrateCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (FTDI is null) return;
			string baudrate = "";
			baudrate = ((ComboBoxItem)dumpBaudrateCB.SelectedItem).Content as string;
			baudrate = baudrate.Split(' ')[0];
			FTDI.BaudRate = uint.Parse(baudrate);
		}

		private void dumpClearB_Click(object sender, RoutedEventArgs e)
		{
			startUpMonitor.Text = "";
		}

		private void startUpMonitorBW_DoWork(object sender, DoWorkEventArgs e)
		{
			var sb = new System.Text.StringBuilder();
			uint oldTimeout = FTDI.ReadTimeout;
			FTDI.ReadTimeout = 1;

			while (true)
			{
				while (!startUpMonitorBW.CancellationPending && FTDI.BytesToRead() == 0) ;


				if (startUpMonitorBW.CancellationPending)
				{
					e.Cancel = true;
					FTDI.ReadTimeout = oldTimeout;
					return;
				}
				sb.Clear();
				while (sb.Length < 4095)
				{
					try
					{
						sb.Append(Convert.ToChar(FTDI.ReadByte()));
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

		}

		void changePictureClick(object sender, RoutedEventArgs e)
		{
			var openPicture = new Microsoft.Win32.OpenFileDialog();
			openPicture.DefaultExt = ("JPG Files|*.jpg");
			openPicture.Filter = ("JPG Files|*.jpg|PNG Files|*.png|BMP Files|*.bmp");
			string path = "";
			try
			{
				path = Path.GetFullPath(Path.GetDirectoryName(getParameter(INI_BACKGROUND_IMAGE_PATH)));
			}
			catch
			{
				try
				{
					path = Path.GetFullPath(SpecialDirectories.MyPictures);
				}
				catch
				{
					path = "C:\\";
				}
			}

			if (Directory.Exists(path))
			{
				openPicture.InitialDirectory = Path.GetFullPath(path);
			}

			for (int i = 0; i < 2; i++)
			{
				try
				{
					if (!(bool)openPicture.ShowDialog(this)) //Se si preme Annulla, termina la procedura
					{
						return;
					}
					break;
				}
				catch
				{
					openPicture.InitialDirectory = "C:\\";
				}
			}
			updateParameter(INI_BACKGROUND_IMAGE_PATH, openPicture.FileName);
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

		void licenseClick(object sender, RoutedEventArgs e)
		{
			var license = new License();
			license.Owner = this;
			license.ShowDialog();
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

		void scanPorts(bool select)
		{
			if (FTDI != null)
			{
				FTDI.Close();
			}
			while (true)
			{
				try
				{
					comPortComboBox.Items.Clear();
					comPortComboBox.Text = "";
					break;
				}
				catch
				{
				}
			}
			System.Management.ManagementObjectSearcher moSearch = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity");
			//System.Management.ManagementObjectCollection moReturn = moSearch.Get();
			//System.Management.ManagementObject mo;// = moReturn.ite
			//sviluppo
			start_error_code = 4;
			///sviluppo
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
							if (!deviceName.Contains("COM1)") && !deviceName.Contains("COM2)"))
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
			//sviluppo
			start_error_code = 5;
			///sviluppo
			if (comPortComboBox.Items.Count == 0)
			{
				comPortComboBoxClear();
				connectButton.IsEnabled = false;
				FTDI = null;
			}
			else
			{
				comPortComboBox.IsEnabled = true;
				connectButton.IsEnabled = true;
				if (select)
				{
					try
					{
						comPortComboBox.SelectedIndex = -1;
						comPortComboBox.SelectedIndex = 0;
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message);
					}
				}
			}
		}

		private void comPortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			//comPortComboBox.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 0x45, 0x45, 0x45));
			//if (comPortComboBox.SelectedIndex == -1) return;
			comPortComboBox.Foreground = new SolidColorBrush(Color.FromArgb(255, 0xba, 0xba, 0xba));
			if (!(FTDI is null))
			{
				FTDI.Close();
			}
			if ((string)comPortComboBox.SelectedItem == "" || comPortComboBox.SelectedItem is null)
			{
				FTDI = null;
				return;
			}
			//sviluppo
			start_error_code = 6;
			///sviluppo

			for (int i = 0; i < 3; i++)
			{
				FTDI = null;
				string portShortName = (string)comPortComboBox.SelectedItem;
				portShortName = portShortName.Substring(portShortName.IndexOf("(") + 1);
				portShortName = portShortName.Remove(portShortName.IndexOf(")"), portShortName.Length - portShortName.IndexOf(")"));
				if (portShortName.IndexOf("com", StringComparison.InvariantCultureIgnoreCase) >= 0)
				{
					try
					{
						FTDI = new FTDI_Device(portShortName);
						FTDI.ReadTimeout = 550;
						if (FTDI.Open()) break;
						Thread.Sleep(150);
					}
					catch
					{
						break;
					}
				}
			}
			//sviluppo
			start_error_code = 7;
			///sviluppo
			if (!FTDI.Open())
			{
				MessageBox.Show("Invalid datacable or port already open.");
				//comPortComboBoxClear();
				uiDisconnected();
				FTDI = null;
				return;
			}

			//comPortComboBox.BorderBrush = new SolidColorBrush(Color.FromArgb(0xff, 0x00, 0xaa, 0xde));
			comPortComboBox.Foreground = new SolidColorBrush(Colors.LightGreen);
		}

		private void comPortComboBoxClear()
		{
			comPortComboBox.SelectionChanged -= comPortComboBox_SelectionChanged;
			comPortComboBox.Items.Add("");
			comPortComboBox.SelectedIndex = 0;
			comPortComboBox.Items.Clear();
			comPortComboBox.SelectionChanged += comPortComboBox_SelectionChanged;
		}

		private async Task pbTask()
		{
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => statusLabel.Content = "Connecting. Please wait..."));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => statusProgressBar.IsIndeterminate = true));
		}

		//CONNECT
		async void connectClick(object sender, RoutedEventArgs e)
		{

			if ((string)connectButton.Content == "Connect")
			{

				if (comPortComboBox.Items.Count == 0)
				{
					MessageBox.Show("No valid DataCable found.");
					return;
				}

				if (comPortComboBox.SelectedIndex == -1 || FTDI is null)
				{

					int newIndex = comPortComboBox.Text == "" ? 0 : comPortComboBox.Items.IndexOf(comPortComboBox.Text);

					//comPortComboBox.SelectionChanged -= comPortComboBox_SelectionChanged;
					comPortComboBox.SelectedIndex = -1;
					//comPortComboBox.SelectionChanged += comPortComboBox_SelectionChanged;
					comPortComboBox.SelectedIndex = newIndex;
					if (FTDI is null || FTDI.IsOpen == false)
					{
						return;
					}

				}

				FTDI.Open();
				if (FTDI.IsOpen == false)
				{
					MessageBox.Show("Invalid DataCable or port already open.");
				}

				Task pbbTask = pbTask();
				await pbbTask;

				string response;
				FTDI.Open();
				if (!FTDI.IsOpen)
				{

				}
				if (sender is Button)       //Se la connessione è stata chiamata da pulsante, utilizza il baudrate principale, altrimenti potrebbe essere stata chiamata da
				{                           //Masterstation, in questo caso il baudrate deve rimanere com'è
					FTDI.BaudRate = 115200;
				}
				FTDI.ReadExisting();
				FTDI.ReadTimeout = 400;

				int errCode = 0;
				FTDI.Write("T");
				int wr = -1;
				int rs = -1;
				try
				{
					rs = FTDI.ReadByte();
					errCode = rs;
					if (rs == 0x23)
					{
						//completeCommand = false;
						Thread.Sleep(55);
						FTDI.ReadExisting();
						FTDI.Write("T");
						Thread.Sleep(5);
						FTDI.Write("TTTTTTTTTTTTTTGGAP");
					}
					else if (rs == '6')     //In caso di Gipsy6 bisogna mandare una stringa corta
					{
						Thread.Sleep(100);
						wr = FTDI.Write("TTTTTTTGGAP");
					}
					else
					{
						FTDI.Write("TTTTTTTTTTTGGAP");
					}
					errCode = 2;
					if (remote)
					{
						Thread.Sleep(400);
					}
					errCode = 3;
					response = FTDI.ReadLine();
					errCode = 4;
				}
				catch
				{
					try
					{
						FTDI.Write("TTTTTTTTTTTTTTGGAP");
						Thread.Sleep(200);
						response = FTDI.ReadLine();
						//completeCommand = false;
						//if (response.Contains("RReady")) completeCommand = true;
					}
					catch
					{
						badShow(STR_unitNotReady + " (FRR_CODE = " + errCode.ToString("000") + ")");
						uiDisconnected();
						return;
					}
				}
				FTDI.ReadTimeout = 1100;

				bool esito = true;
				if (response.Contains("Ready"))
				{
					try
					{
						Thread.Sleep(10);
						string model = Unit.askModel();
						switch (model)
						{
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
							case "Axy-Depth/Fast.5":
								oUnit = new AxyDepth_2(this);
								oUnit.modelCode = Unit.model_axyDepthFast;
								break;
							case "AGM-1":
								oUnit = new AGM(this);
								break;
							case "CO2 Logger":
								oUnit = new CO2_Logger(this);
								break;
							case "Axy-Trek":
								oUnit = new AxyTrekN(this);
								break;
							case "Axy-Quattrok":
							case "Axy-Trek HD":
								oUnit = new AxyTrekHD(this);
								break;
							case "Axy-Trek FT":
								oUnit = new AxyTrekFT(this);
								break;
							case "Axy-Trek CO2":
								oUnit = new AxyTrekCO2(this);
								break;
							case "Axy-Trek R":
								oUnit = new AxyTrekR(this);
								break;
							case "GiPSy-6":
							case "GiPSy-6 N":
								oUnit = new Gipsy6N(this);
								((Gipsy6N)oUnit).remoteConnection = remoteConnection;
								if (sender is MainWindow)
								{
									oUnit.msBaudrate();
								}
								break;
							case "GiPSy-6 XS":
								oUnit = new Gipsy6XS(this);
								break;
							case "Drop-Off":
								oUnit = new Drop_Off(this);
								break;
						}
						modelLabel.Content = model;
						oUnit.modelName = model;
						//if (!getConf()) return;
						getConf();
						errCode = 30;
						getRemote();
						errCode = 31;
						getSolar();
						errCode = 32;
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
						string model = Unit.askLegacyModel();
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
					badShow(STR_unitNotReady + " (ERR_CODE = " + errCode.ToString("000") + ")");
					uiDisconnected();
				}

				if (!esito)
				{
					badShow(STR_unitNotReady + " (ERR_CODE = " + errCode.ToString("000") + ")");
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

		//NAME
		void unitNameButtonClick(object sender, RoutedEventArgs e)
		{
			setName();
			connectButton.Focus();
		}

		//ERASE
		void eraseButtonClick(object sender, RoutedEventArgs e)
		{
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

		//CONFIGURATION (movements)
		void configureMovementButtonClick(object sender, RoutedEventArgs e)
		{
			byte[] conf;
			byte[] accSchedule;
			try
			{
				conf = oUnit.getConf();
				Thread.Sleep(200);
				accSchedule = oUnit.getAccSchedule();
			}
			catch (Exception ex)
			{
				badShow(ex.Message);
				uiDisconnected();
				return;
			}
			//ConfigurationWindow confForm = new ConfigurationWindow();
			if (oUnit is AGM)
			{
				confForm = new AgmConfigurationWindow(conf, oUnit.modelCode);
			}
			else if (oUnit is Gipsy6)
			{
				if ((System.Windows.Forms.Control.ModifierKeys & System.Windows.Forms.Keys.Control) == System.Windows.Forms.Keys.Control)
				{
					try
					{
						string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\corrupted_conf.dat";
						File.WriteAllBytes(path, conf);
					}
					catch { }
					conf = new byte[600];
					Array.Copy(Gipsy6N.defConf, conf, Gipsy6N.defConf.Length);
					if (oUnit is Gipsy6N)
					{
						conf[541] = 0x00;
						conf[542] = 0x02;
						conf[543] = 0x2b;
					}
				}
				confForm = new GiPSy6ConfigurationMain(conf, oUnit, this);
			}
			else if (oUnit is Axy5)
			{
				confForm = new Axy5ConfigurationWindow(conf, accSchedule, oUnit.firmTotA);
			}
			else if (oUnit is AxyTrek)
			{
				confForm = new TrekMovementConfigurationWindow(conf, oUnit);
			}
			else if (oUnit is Drop_Off)
			{
				confForm = new DropOffConfigurationWindow(conf, oUnit.firmTotA);
			}
			else
			{
				confForm = new AxyConfigurationWindow(conf, oUnit);
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
							Thread.Sleep(200);
							oUnit.setAccSchedule(confForm.axyScheduleOut);

							Ok okf = new Ok("Movement configuration succesfully updated.");
							okf.Top = Top + (Height / 3);
							okf.Left = Left + (Width / 3);
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

		//CONFIGURATION (position/bootloader)
		private void configurePositionClick(object sender, RoutedEventArgs e)
		{

			ConfigurationWindow conf = null;
			int type = 0;

			if (oUnit == null)
			{
				var tg = new YesNo("No unit connected. Please select the schedule type you want to configure.", "UNIT TYPE", "", "TREK Family", "GIPSY family");
				type = tg.ShowDialog();
			}
			else    //Qui bisogna implementare dei menù a tendina per il sottotipo di unità (axytrek r-n-hs ecc, gipsy6 n-xs) Per ora solo unità n
			{
				if (oUnit is AxyTrek)
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
				conf = new TrekPositionConfigurationWindow(this, oUnit);
			}
			else
			{
				if (oUnit == null)
				{
					byte[] axyconf = new byte[Gipsy6N.defConf.Length];
					Array.Copy(Gipsy6N.defConf, axyconf, Gipsy6N.defConf.Length);
					var unit = new Gipsy6N(this);
					unit.firmTotA = 999999999;
					conf = new GiPSy6ConfigurationMain(axyconf, unit, this);
					conf.lastForthContent = "CLOSE";
				}
				else
				{
					var tg = new YesNo("WARNING: you are entering the GiPSy6 bootloader!\r\nPlease, " +
										"proceed only if you have a new firmware to upload, " +
										"else please leave or your unit could potentially get bricked.",
										"GiPSy6 Bootloader", "", "Yes", "No");
					tg.Owner = this;
					if (tg.ShowDialog() == 2) return;
					uiDisconnected();
					var boot = new Bootloader.Bootloader_Gipsy6(true, "GiPSy-6");
					boot.Owner = this;
					boot.ShowDialog();
					return;
				}
			}

			//conf = new TrekPositionConfigurationWindow(ref oUnit);

			conf.Owner = this;

			bool? uiAfter = conf.ShowDialog();

			if (uiAfter == false)
			{
				uiDisconnected();
			}

		}

		//DOWNLOAD
		private void downloadButtonClick(object sender, RoutedEventArgs e)
		{
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
			saveRaw.RestoreDirectory = false;
			saveRaw.OverwritePrompt = true;
			saveRaw.AddExtension = true;
			saveRaw.FileName = unitNameTextBox.Text;

			saveRaw.DefaultExt = "RAW file|*." + oUnit.defaultArdExtension;
			saveRaw.Filter = "RAW file|*." + oUnit.defaultArdExtension;

			if (oUnit.defaultArdExtension.Contains("gp6") || oUnit.defaultArdExtension.Contains("bs6"))
			{
				saveRaw.OverwritePrompt = false;
			}


			if (Directory.Exists(Path.GetFullPath(getParameter(INI_DATA_SAVE_PATH))))
			{
				saveRaw.InitialDirectory = Path.GetFullPath(getParameter(INI_DATA_SAVE_PATH));
			}
			else
			{
				saveRaw.InitialDirectory = Path.GetFullPath(SpecialDirectories.MyDocuments);
			}

			bool saveShowResult = false;
			for (int i = 0; i < 2; i++)
			{
				try
				{
					saveShowResult = (bool)saveRaw.ShowDialog();
					break;
				}
				catch
				{
					saveRaw.InitialDirectory = "C:\\";
				}
			}

			if (saveShowResult == false)
			{
				statusProgressBar.Maximum = oldMax;
				statusProgressBar.Minimum = oldMin;
				statusProgressBar.Value = oldVal;
				statusLabel.Content = oldCon;
				statusProgressBar.IsIndeterminate = false;
				return;
			}
			updateParameter(INI_DATA_SAVE_PATH, Path.GetDirectoryName(saveRaw.FileName));
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
					if (File.Exists(Path.GetDirectoryName(saveRaw.FileName) + "\\" + Path.GetFileNameWithoutExtension(saveRaw.FileName) + ".mdp"))
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
								FileInfo fi = new FileInfo(Path.GetDirectoryName(saveRaw.FileName) + "\\" + Path.GetFileNameWithoutExtension(saveRaw.FileName) + ".mdp");
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
					dr.finalAddress = ((memoryLogical[0] / 4096) * 4096) + 0x1000;
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
			if (oUnit is Gipsy6N)
			{
				if (((Gipsy6N)oUnit).remoteConnection)
				{
					downloadThread = new Thread(() => oUnit.downloadRemote(saveRaw.FileName, fromMemory, toMemory, baudrate));
				}
			}
			else
			{
				if (oUnit.remote)
				{
					downloadThread = new Thread(() => oUnit.downloadRemote(saveRaw.FileName, fromMemory, toMemory, baudrate));
				}
			}

			downloadThread.SetApartmentState(ApartmentState.STA);
			downloadThread.Start();
		}

		//CONVERT
		private void convertDataClick(object sender, RoutedEventArgs e)
		{
			askOverwrite = true;
			//lastSettings = System.IO.File.ReadAllLines(iniFile);
			Microsoft.Win32.OpenFileDialog fOpen = new Microsoft.Win32.OpenFileDialog();
			if (Directory.Exists(Path.GetFullPath(Path.GetFullPath(getParameter("convertPath")))))
			{
				fOpen.InitialDirectory = Path.GetFullPath(getParameter("convertPath"));
			}

			//fOpen.FileName = "*.ard";
			fOpen.Filter = "Sensor Raw Data | *.ard;*.rem;*.gp6;*.bs6;*.memDump;*.mdp";

			fOpen.Multiselect = true;
			for (int i = 0; i < 2; i++)
			{
				try
				{
					if (fOpen.ShowDialog() == false)
					{
						return;
					}
					break;
				}
				catch
				{
					fOpen.InitialDirectory = "C:\\";
				}
			}

			updateParameter("convertPath", Path.GetFullPath(Path.GetDirectoryName(fOpen.FileName)));
			//File.WriteAllLines(iniFile, lastSettings);
			convFiles = new List<string>();
			convFiles.AddRange(fOpen.FileNames);
			convertDataLaunch(convFiles);
		}

		private void convertDataLaunch(List<string> files)
		{
			ConversionPreferences cp = new ConversionPreferences(files[0]);
			cp.Owner = this;
			cp.ShowDialog();
			if (cp.goOn == false)
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
			if (convFileTot != 0)
			{
				nextFile();
			}
		}

		//REMOTE
		private void remoteManagementClick(object sender, RoutedEventArgs e)
		{
			if (connectButton.Content.Equals("Connect"))
			{

				var remoteMain = new Remote_Main(this);
				var res = remoteMain.showDialog();
				if (res == 1)               //Gestione Master Station
				{
					if (FTDI is null)
					{
						MessageBox.Show("Please connect a data cable first.");
						return;
					}
					var remoteManagement = new MS_Main(this);
					remoteManagement.ShowDialog();
				}
				else if (res == 2)              //Gestione Base Station
				{
					var remoteManagement = new BS_Main(this);
					remoteManagement.Owner = this;
					remoteManagement.ShowDialog();
				}
				return;
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
					charts = new ChartWindowAGM();
					((ChartWindowAGM)charts).WindowState = WindowState.Maximized;
					((ChartWindowAGM)charts).ShowDialog();
					break;
			}

		}

		public bool externConnect(int baudRate)
		{

			Baudrate_base = baudRate;
			remoteConnection = true;
			connectClick(this, new RoutedEventArgs());
			if (unitConnected)
			{
				keepAliveTimer = new System.Timers.Timer();
				keepAliveTimer.Elapsed += keepAliveTimerElapsed;
				keepAliveTimer.Interval = 5000;
				keepAliveTimer.AutoReset = true;
				keepAliveTimer.Enabled = true;
				if (oUnit is Gipsy6)    //In caso di gipsy6 remoto disabilita il pulsante per l'upload del firmware
				{
					configurePositionButton.IsEnabled = false;
				}
			}
			return connectButton.Content.Equals("Disconnect");
		}

		public void externBootloader()
		{
			FTDI.BaudRate = 1000000;
			var boot = new Bootloader.Bootloader_Gipsy6(false, "MasterStation");
			boot.ShowDialog();
		}

		//BATTERY REFRESH
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
			try
			{
				Thread.Sleep(10);
				oUnit.changeBaudrate(1);
				Thread.Sleep(10);
				firmwareLabel.Content = oUnit.askFirmware();
				Thread.Sleep(10);
				unitNameTextBox.Text = oUnit.askName();
				Thread.Sleep(10);
				FTDI.ReadExisting();
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
			catch
			{
				throw (new Exception());
			}
		}

		private void getRemote()
		{
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

		private void keepAliveTimerElapsed(Object source, System.Timers.ElapsedEventArgs e)
		{
			try
			{
				oUnit.keepAlive();      //Rimettere dopo sviluppo
			}
			catch
			{
				try
				{
					keepAliveTimer.Stop();
				}
				catch { }
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
			if (getParameter(INI_BACKGROUND_IMAGE_PATH).Contains("null") | !File.Exists(getParameter(INI_BACKGROUND_IMAGE_PATH)))
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
				png.UriSource = new Uri(getParameter(INI_BACKGROUND_IMAGE_PATH), UriKind.Absolute);
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
					if (Path.GetExtension(files[0]) == ".jpg" | Path.GetExtension(files[0]) == ".bmp" | Path.GetExtension(files[0]) == ".png")
					{
						updateParameter(INI_BACKGROUND_IMAGE_PATH, files[0]);
						//File.WriteAllLines(iniFile, lastSettings);
						var bmp = new BitmapImage();
						bmp.BeginInit();
						bmp.UriSource = new Uri(files[0], UriKind.Absolute);
						bmp.EndInit();
						pictureBox.Source = bmp;
						return;
					}
					else if (Path.GetExtension(files[0]) == ".mdp" | Path.GetExtension(files[0]) == ".memDump")
					{
						//Implementare il fakeDownload dalle unità!
					}
				}

				List<string> fAr = new List<string>();
				for (int i = 0; i < files.Length; i++)
				{
					if ((Path.GetExtension(files[i]) == ".ard") || (Path.GetExtension(files[i]) == ".gp6") || (Path.GetExtension(files[i]) == ".bs6"))
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

				var cp = new ConversionPreferences(fAr[0]);
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
				string exten = Path.GetExtension(fileName);
				if (exten.Length > 4)
				{
					addOn = ("_S" + exten.Remove(0, 4));
				}
				fileNameCsv = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + addOn + ".csv";
				fileNametxt = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + addOn + ".txt";
				fileNameKml = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + addOn + "_temp" + ".kml";
				fileNamePlaceMark = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + addOn + ".kml";
				nomiFile = new string[] { fileNameCsv, fileNametxt, fileNamePlaceMark };
			}

			if (GoOn)
			{
				if (Path.GetExtension(fileName).Contains("Dump") || Path.GetExtension(fileName).Contains("dump") || Path.GetExtension(fileName).Contains("mdp"))
				{
					fileHeader = "MEMDUMP ";
					fileType = type_mdp;
				}
				else
				{
					if (Path.GetExtension(fileName).Contains("rem"))
					{
						fileHeader = "REM ";
						fileType = type_rem;

					}
					if (Path.GetExtension(fileName).Contains("gp6") || Path.GetExtension(fileName).Contains("bs6"))
					{
						fileType = type_gp6;
					}

					foreach (string nomefile in nomiFile)
					{
						if (File.Exists(nomefile) & askOverwrite)
						{
							YesNo yn = new YesNo(Path.GetFileName(nomefile) + " already exists. Do you want to overwrite it?", "OVERWRITE");
							if (yn.ShowDialog() == YesNo.NO)
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
			uint fw = 0; //Controllare cosa succede in caso di ardfile=false alla riga 1555 e poi al caso successivo (Depth)
			if (fileType == type_ard)           //nell'ard il tipo di unità è scritto nel primo byte
			{
				model = (byte)fs.ReadByte();
			}
			else if (fileType == type_gp6)
			{
				if (fs.Length == 0)
				{
					nextFile();
					return;
				}
				fs.Position = fs.Length - 2;
				model = (byte)fs.ReadByte();
			}
			else if (fileType == type_rem)      //nel rem bisogna saltare l'header con la posizione della lista sessioni per leggere il byte con tipo unità
			{
				fs.Position = 0x1000;
				model = (byte)fs.ReadByte();
				fs.Position = 0;
			}
			else
			{
				try
				{
					uint[] par = findMdpModel(ref fs);
					model = (byte)par[0];
					fw = par[1];  //Implementare
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
				case Unit.model_axy4_legacy:
					if (fileType == type_ard)
					{
						fw = (byte)fs.ReadByte();
					}
					if (fw < 2)
					{
						cUnit = new Axy4_1(this);
					}
					else cUnit = new Axy4_2(this);
					break;
				case Unit.model_axy5:
					cUnit = new Axy5(this);
					break;
				case Unit.model_axyDepth:
					if (fileType == type_ard) fw = (byte)fs.ReadByte();
					if (fw < 2)
					{
						cUnit = new AxyDepth_1(this);
						cUnit.modelCode = Unit.model_axyDepth_legacy;
					}
					else cUnit = new AxyDepth_2(this);
					break;
				case Unit.model_axyDepthFast:
					cUnit = new AxyDepth_2(this);
					cUnit.modelCode = Unit.model_axyDepthFast;
					break;
				case Unit.model_axyTrekN:
					cUnit = new AxyTrekN(this);
					break;
				case Unit.model_axyTrekHD:
					cUnit = new AxyTrekHD(this);
					break;
				case Unit.model_axyTrekFT:
					cUnit = new AxyTrekFT(this);
					break;
				case Unit.model_axyTrekCO2:
					cUnit = new AxyTrekCO2(this);
					break;
				case Unit.model_axyTrekR:
					cUnit = new AxyTrekR(this);
					break;
				case Unit.model_AGM1:
					cUnit = new AGM(this);
					break;
				case Unit.model_Co2Logger:
					cUnit = new CO2_Logger(this);
					break;
				case Unit.model_Gipsy6N:
					cUnit = new Gipsy6N(this);
					break;
				case Unit.model_Gipsy6XS:
					cUnit = new Gipsy6XS(this);
					break;
				default:
					cUnit = new AxyTrekN(this);
					break;
			}
			cUnit.firmTotA = fw;
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

		private uint[] findMdpModel(ref FileStream iFile)
		{
			long pos = iFile.Position;
			uint[] outt = new uint[2];
			iFile.Position = iFile.Length - 1;
			if (iFile.ReadByte() == 254)
			{
				iFile.Position = iFile.Length - 2;
				outt[0] = (uint)iFile.ReadByte();
				iFile.Position = iFile.Length - 5;
				if (outt[0] <= 7)
				{
					iFile.Position -= 3;
				}
				outt[1] = (uint)(iFile.ReadByte() * 1000000 + iFile.ReadByte() * 1000 + iFile.ReadByte());
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
			if (!(FTDI is null))
			{
				FTDI.Close();
			}
		}




		#endregion

	}

}
