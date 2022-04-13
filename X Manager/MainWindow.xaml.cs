using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Management;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.ComponentModel;
using X_Manager.Units;

#if X64
using FT_HANDLE = System.UInt64;
#else
using FT_HANDLE = System.UInt32;
#endif

namespace X_Manager
{
	public partial class MainWindow : Window
	{

		#region DichiarazioniFTDI

		public string ftdiSerialNumber;
#if X64
		public const string ftdiLibraryName = "FTD2XX.dll";
#else
		public const string ftdiLibraryName = "FTD2XX.dll";
#endif

		public const int FT_LIST_NUMBER_ONLY = -2147483648;
		public const int FT_LIST_BY_INDEX = 0x40000000;
		public const int FT_LIST_ALL = 0x20000000;

		// FT_OpenEx Flags (See FT_OpenEx)
		public const int FT_OPEN_BY_SERIAL_NUMBER = 1;
		public const int FT_OPEN_BY_DESCRIPTION = 2;

		public enum FT_STATUS : int
		{
			FT_OK = 0,
			FT_INVALID_HANDLE,
			FT_DEVICE_NOT_FOUND,
			FT_DEVICE_NOT_OPENED,
			FT_IO_ERROR,
			FT_INSUFFICIENT_RESOURCES,
			FT_INVALID_PARAMETER,
			FT_INVALID_BAUD_RATE,
			FT_DEVICE_NOT_OPENED_FOR_ERASE,
			FT_DEVICE_NOT_OPENED_FOR_WRITE,
			FT_FAILED_TO_WRITE_DEVICE,
			FT_EEPROM_READ_FAILED,
			FT_EEPROM_WRITE_FAILED,
			FT_EEPROM_ERASE_FAILED,
			FT_EEPROM_NOT_PRESENT,
			FT_EEPROM_NOT_PROGRAMMED,
			FT_INVALID_ARGS,
			FT_OTHER_ERROR
		};
		// FT_ListDevices by number only
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_ListDevices(void* pvArg1, void* pvArg2, UInt32 dwFlags);

		//StringCode del dispositivo, passare il numero dispositivo, il puntatore al buffer di byte e (FT_LIST_BY_INDEX Or FT_OPEN_BY_SERIAL_NUMBER)
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_ListDevices(UInt32 pvArg1, void* pvArg2, UInt32 dwFlags);

		//Numero di device presenti, passare riferimento all'int numero di dispositivi, string null e FT_LIST_NUMBER_ONLY
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_ListDevices(ref int lngNumberOfDevices, string pvArg2, int lngFlags);

		//Apre il dispositivo identificato dal seriale
		[DllImport(ftdiLibraryName)]
		public static extern unsafe FT_STATUS FT_OpenEx(string stringCode, UInt32 dwFlags, ref FT_HANDLE ftHandle);


		[DllImport(ftdiLibraryName)]
		public static extern unsafe FT_STATUS FT_OpenEx(void* pvArg1, UInt32 dwFlags, ref FT_HANDLE ftHandle);

		//Restituisce il numero di porta com associato al dispositivo
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_GetComPortNumber(FT_HANDLE ftHandle, ref FT_HANDLE comNumber);


		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_Open(UInt32 uiPort, ref FT_HANDLE ftHandle);

		[DllImport(ftdiLibraryName)]
		public static extern unsafe FT_STATUS FT_Close(FT_HANDLE ftHandle);
		[DllImport(ftdiLibraryName)]
		public static extern unsafe FT_STATUS FT_Read(FT_HANDLE ftHandle, void* lpBuffer, UInt32 dwBytesToRead, ref UInt32 lpdwBytesReturned);
		[DllImport(ftdiLibraryName)]
		public static extern unsafe FT_STATUS FT_Write(FT_HANDLE ftHandle, void* lpBuffer, UInt32 dwBytesToRead, ref UInt32 lpdwBytesWritten);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_SetBaudRate(FT_HANDLE ftHandle, UInt32 dwBaudRate);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_SetDataCharacteristics(FT_HANDLE ftHandle, byte uWordLength, byte uStopBits, byte uParity);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_SetFlowControl(FT_HANDLE ftHandle, char usFlowControl, byte uXon, byte uXoff);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_SetDtr(FT_HANDLE ftHandle);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_ClrDtr(FT_HANDLE ftHandle);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_SetRts(FT_HANDLE ftHandle);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_ClrRts(FT_HANDLE ftHandle);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_GetModemStatus(FT_HANDLE ftHandle, ref UInt32 lpdwModemStatus);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_SetChars(FT_HANDLE ftHandle, byte uEventCh, byte uEventChEn, byte uErrorCh, byte uErrorChEn);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_Purge(FT_HANDLE ftHandle, UInt32 dwMask);
		[DllImport(ftdiLibraryName)]
		public static extern unsafe FT_STATUS FT_SetTimeouts(FT_HANDLE ftHandle, UInt32 dwReadTimeout, UInt32 dwWriteTimeout);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_GetQueueStatus(FT_HANDLE ftHandle, ref UInt32 lpdwAmountInRxQueue);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_SetBreakOn(FT_HANDLE ftHandle);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_SetBreakOff(FT_HANDLE ftHandle);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_GetStatus(FT_HANDLE ftHandle, ref UInt32 lpdwAmountInRxQueue, ref UInt32 lpdwAmountInTxQueue, ref UInt32 lpdwEventStatus);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_SetEventNotification(FT_HANDLE ftHandle, UInt32 dwEventMask, void* pvArg);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_ResetDevice(FT_HANDLE ftHandle);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_SetDivisor(FT_HANDLE ftHandle, char usDivisor);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_GetLatencyTimer(FT_HANDLE ftHandle, ref byte pucTimer);
		[DllImport(ftdiLibraryName)]
		public static extern unsafe FT_STATUS FT_SetLatencyTimer(FT_HANDLE ftHandle, byte ucTimer);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_GetBitMode(FT_HANDLE ftHandle, ref byte pucMode);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_SetBitMode(FT_HANDLE ftHandle, byte ucMask, byte ucEnable);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_SetUSBParameters(FT_HANDLE ftHandle, UInt32 dwInTransferSize, UInt32 dwOutTransferSize);
		#endregion

		#region Dichiarazioni

		Units.Unit oUnit;
		Units.Unit cUnit;
		//ConfigurationWindow confForm;
		public const int Baudrate_base = 115200;
		public const int Baudrate_1M = 1000000;
		public const int Baudrate_2M = 2000000;
		public const int Baudrate_3M = 3000000;

		public const uint model_axy1 = 255;
		public const uint model_axyDepth = 127;
		public const uint model_axy2 = 126;
		public const uint model_axy3 = 125;
		public const uint model_axy4 = 124;
		public const uint model_Co2Logger = 123;
		public const uint model_axy5 = 122;
		public const uint model_AGM1_calib = 10;
		public const uint model_Gipsy6 = 10;
		public const uint model_AGM1 = 9;
		public const uint model_axyTrekS = 7;
		public const uint model_axyTrek = 6;

		public bool realTimeStatus = false;
		public bool convertStop = false;
		//List<bool> controlStatus;
		//ChartWindow charts;
		UInt16 convFileTot;
		UInt16 convFile;
		int realTimeType = 0;

		public static string[] lastSettings;
		public System.IO.Ports.SerialPort sp;
		byte[] unitFirmware = new byte[15];

		//byte firmwareDigitCount;
		//UInt32 firmTotA = 0;
		//UInt32 firmTotB = 0;
		//string unitModelString;
		byte[] unitModel = new byte[1];
		//bool completeCommand = false;
		public byte[] axyconf = new byte[30];
		bool positionCanSend = false;
		//UInt32 maxMemory = 0;
		public string csvSeparator;
		//string[] stConversionFileNames;
		BackgroundWorker startUpMonitorBW;
		public byte stDebugLevel;
		public bool oldUnitDebug;
		public bool remote = false;
		public bool addGpsTime = false;


		//Costanti e pseudo constanti
		const string STR_noComPortAvailable = "No COM port available. Please connect a data cable and press Scan";
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
		string iniFile = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + companyFolder + appFolder + "\\settings.ini";
		public string prefFile = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + companyFolder + appFolder + "\\convPrefs.ini";

		List<string> convFiles = new List<string>();

		[DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DeleteObject([In] IntPtr hObject);

		#endregion

		#region Interfaccia
		public MainWindow()
		{
			InitializeComponent();
			sp = new System.IO.Ports.SerialPort();
			this.Loaded += mainWindowLoaded;
		}

		private void mainWindowLoaded(object sender, EventArgs e)
		{

			//Sviluppo
			//string tempName = "C:\\Users\\marco\\Desktop\\files\\finti depth\\I_13_addon.temp";
			//var temp = new BinaryWriter(System.IO.File.OpenWrite(tempName));
			//byte[] tstemp = new byte[] { 0xab, 2, 0, 0 };
			//byte[] ts = new byte[] { 0xab, 0, };
			//byte[] sample = new byte[] { 0, 0, 0, 0 };
			//int nSamp = 26;
			//int tsCounter = 1;
			//while (temp.BaseStream.Length < 150000)
			//{
			//    for (int mcount = 0; mcount < 26; mcount++)
			//    {
			//        if ((tsCounter % 5) == 0)
			//        {
			//            temp.Write(tstemp);
			//        }
			//        else
			//        {
			//            temp.Write(ts);
			//        }
			//        tsCounter++;
			//        if (mcount < 11)
			//        {
			//            nSamp = 24;
			//        }
			//        else
			//        {
			//            nSamp = 25;
			//        }
			//        for (int c = 0; c < nSamp; c++)
			//        {
			//            temp.Write(sample);
			//        }
			//    }
			//}

			//temp.Close();

			//var tempReader = new BinaryReader(System.IO.File.OpenRead(tempName));
			//string outmdpName = "C:\\Users\\marco\\Desktop\\files\\finti depth\\I_13_addon.mpd";
			//var mdp = new BinaryWriter(System.IO.File.OpenWrite(outmdpName));
			//byte[] buffer;
			//byte[] header = new byte[] { 0x55 };
			//while (true)
			//{
			//    if ((tempReader.BaseStream.Length - tempReader.BaseStream.Position) >= 255)
			//    {
			//        buffer = tempReader.ReadBytes(255);
			//        mdp.Write(header);
			//        mdp.Write(buffer);
			//    }
			//    else
			//    {
			//        mdp.Write(header);
			//        while (tempReader.BaseStream.Position < tempReader.BaseStream.Length)
			//        {
			//            mdp.Write(tempReader.ReadByte());
			//        }
			//        break;
			//    }

			//}
			//tempReader.Close();
			//mdp.Close();
			//int g = 0;

			///sviluppo


			loadUserPrefs();
			uiDisconnected();
			initPicture();
			scanButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			switch (lastSettings[5])
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
			keepMdpItem.IsChecked = false;
			if (lastSettings[6] == "true") keepMdpItem.IsChecked = true;
			selectDownloadSpeed(lastSettings[7]);
			csvSeparatorChanged(lastSettings[8]);
			normalViewTabItem.IsSelected = true;

			progressBarStopButton.IsEnabled = false;
			progressBarStopButtonColumn.Width = new GridLength(0);
		}

		private void loadUserPrefs()
		{
			try
			{
				lastSettings = System.IO.File.ReadAllLines(iniFile);
			}
			catch
			{
				lastSettings = new string[] { "\r\n" };
			}

			if (!System.IO.File.Exists(iniFile) | lastSettings.Length != 9)
			{
				if (!System.IO.Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + companyFolder + appFolder))
				{

					System.IO.Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + companyFolder + appFolder);
				}

				string fileBody = "";
				//crea il file ini nella cartella e scrive la prima riga per l\\immagine di sfondo (0)
				fileBody = "null\r\n";
				//Scrive la cartella per il salvataggio dei file di schedule (1)
				fileBody += "null\r\n";
				//Scrive la cartella per l//apertura dei file di schedule (2)
				fileBody += "null\r\n";
				//scrive il file ini per la cartella file Save (3)
				fileBody += Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + companyFolder + "\\Downloads\r\n";
				if (!System.IO.Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + companyFolder + "\\Downloads"))
				{
					System.IO.Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + companyFolder + "\\Downloads");
				}
				//Scrive il file ini per la cartella Convert (4)
				fileBody += Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + companyFolder + "\\Downloads\r\n";
				//Scrive il tipo di conversione del sensore di pressione
				fileBody += "depth\r\n";
				//Scrive l//opzione per lasciare su disco il file mdp dopo il download
				fileBody += "false\r\n";
				//Scrive la velocità di download
				fileBody += "3\r\n";
				//Scrive il separatore csv
				fileBody += ",\r\n";
				System.IO.File.WriteAllText(iniFile, fileBody);
			}

			lastSettings = System.IO.File.ReadAllLines(iniFile);
		}

		private void uiDisconnected()
		{
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
			statusProgressBar.Value = 0;
			positionCanSend = false;
			dumpViewTabItem.IsEnabled = true;
			remoteButton.Content = "Remote";
			remoteButton.IsEnabled = true;
			this.Title = "X MANAGER";
			configureMovementButton.Content = "Accelerometer configuration";
			realTimeSP.Visibility = Visibility.Hidden;
			try
			{
				sp.Close();
			}
			catch
			{

			}
		}

		private void uiConnected()
		{
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
			configurePositionButton.IsEnabled = oUnit.configurePositionButtonEnabled;
			configureMovementButton.IsEnabled = oUnit.configureMovementButtonEnabled;
			if (realTimeType > 0)
			{
				realTimeSP.Visibility = Visibility.Visible;
			}

			switch (oUnit.modelCode)
			{
				case Units.Unit.model_Co2Logger:
					configureMovementButton.Content = "Logger configuration";
					break;
				case Units.Unit.model_AGM1:
					configureMovementButton.Content = "Movement configuration";
					break;
				default:
					configureMovementButton.Content = "Accelerometer configuration";
					break;
			}
		}

		void ctrlManager(object sender, KeyEventArgs e)
		{
			if (Keyboard.Modifiers == ModifierKeys.Control)
			{
				if (e.Key == Key.D)
				{
					System.Diagnostics.Process.Start(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + companyFolder + appFolder);
				}
				else if (e.Key == Key.S)
				{
					convertStop = true;
				}
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

			}

		}

		public void downloadFinished()
		{
			warningShow("Download completed.");
			mainGrid.IsEnabled = true;
			statusLabel.Content = "Connected";
			progressBarStopButton.IsEnabled = false;
			progressBarStopButtonColumn.Width = new GridLength(0);
			try
			{
				sp.ReadExisting();
				Thread.Sleep(100);
				statusProgressBar.Maximum = oUnit.askMaxMemory();
				statusProgressBar.Minimum = 0;
				Thread.Sleep(100);
				statusProgressBar.Value = oUnit.askMemory();
				Thread.Sleep(100);
			}
			catch
			{
				downloadFailed();
			}
		}

		public void downloadFailed()
		{
			badShow(STR_unitNotReady);
			mainGrid.IsEnabled = true;
			uiDisconnected();
		}

		private void openDataFolder(object sender, RoutedEventArgs e)
		{
			lastSettings = System.IO.File.ReadAllLines(iniFile);
			System.Diagnostics.Process.Start(lastSettings[4]);
		}

		void tabControlTabChanged(object sender, RoutedEventArgs e)
		{
			if (mainTabControl.SelectedIndex == 0)
			{
				try
				{
					if (startUpMonitorBW.IsBusy) startUpMonitorBW.CancelAsync();
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
							sp.Close();
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
				sp.Close();
			}
		}

		void changePictureClick(object sender, RoutedEventArgs e)
		{
			var openPicture = new Microsoft.Win32.OpenFileDialog();
			openPicture.DefaultExt = ("JPG Files|*.jpg");
			openPicture.Filter = ("JPG Files|*.jpg|PNG Files|*.png|BMP Files|*.bmp");
			if (System.IO.File.Exists(System.IO.Path.GetDirectoryName(lastSettings[0])))
			{
				openPicture.InitialDirectory = System.IO.Path.GetDirectoryName(lastSettings[0]);
			}


			if (!(bool)openPicture.ShowDialog(this)) //Se si preme Annulla, termina la procedura
			{
				return;
			}
			lastSettings[0] = openPicture.FileName;
			System.IO.File.WriteAllLines(iniFile, lastSettings);
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
			loadUserPrefs();
			lastSettings = System.IO.File.ReadAllLines(iniFile);
			lastSettings[5] = "air";
			System.IO.File.WriteAllLines(iniFile, lastSettings);

			depthSubItem.IsChecked = false;
			airSubItem.IsChecked = true;
		}

		void depthSensorSelected(object sender, RoutedEventArgs e)
		{
			loadUserPrefs();
			lastSettings = System.IO.File.ReadAllLines(iniFile);
			lastSettings[5] = "depth";
			System.IO.File.WriteAllLines(iniFile, lastSettings);

			depthSubItem.IsChecked = true;
			airSubItem.IsChecked = false;
		}

		void keepMdpClicked(object sender, RoutedEventArgs e)
		{
			lastSettings = System.IO.File.ReadAllLines(iniFile);
			if (keepMdpItem.IsChecked)
			{
				lastSettings[6] = "true";
			}
			else
			{
				lastSettings[6] = "false";
			}
			System.IO.File.WriteAllLines(iniFile, lastSettings);
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
			lastSettings[7] = speed;
			System.IO.File.WriteAllLines(iniFile, lastSettings);
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
				case "\r\n":
					tabSubItem.IsChecked = true;
					break;
			}
			lastSettings[8] = sep;
			System.IO.File.WriteAllLines(iniFile, lastSettings);
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

		void scanClick(object sender, RoutedEventArgs e)
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
					if ((deviceName.Contains("COM") && deviceName.Contains("olific")) | (deviceName.Contains("COM") && deviceName.Contains("USB Serial")))
					{
						comPortComboBox.Items.Add(deviceName);
					}
				}
				catch
				{
					//MessageBox.Show(ex.Message);
				}
			}
			if (comPortComboBox.Items.Count == 0)
			{
				comPortComboBox.IsEnabled = false;
				connectButton.IsEnabled = false;
				warningShow(STR_noComPortAvailable);
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
				setLatency(portShortName, 1);
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
				spurgo();

				//completeCommand = true;

				sp.Write("T");
				try
				{
					int rs = sp.ReadByte();
					if (rs == 0x23)
					{
						//completeCommand = false;
						Thread.Sleep(55);
						spurgo();
						sp.Write("T");
						Thread.Sleep(5);
					}
					sp.Write("TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTGGAP");
					if (remote) Thread.Sleep(400);
					response = sp.ReadLine();
				}
				catch
				{
					try
					{
						sp.Write("TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTGGAP");
						Thread.Sleep(200);
						response = sp.ReadLine();
						//completeCommand = false;
						//if (response.Contains("RReady")) completeCommand = true;
					}
					catch
					{
						badShow(STR_unitNotReady);
						sp.Close();
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
						string model = Unit.askModel(ref sp);
						switch (model)
						{
							case "Axy-Trek":
								oUnit = new AxyTrek(this);
								break;
							case "Axy-4":
								oUnit = new Axy4_2(this);
								break;
							case "Axy-5":
								oUnit = new Axy5(this);
								break;
							case "Axy-Depth":
								oUnit = new AxyDepth_2(this);
								break;
							case "AGM-1":
								oUnit = new AGM(this);
								break;
							case "CO2 Logger":
								oUnit = new CO2_Logger(this);
								break;
							case "GiPSy-6":
								oUnit = new Gipsy6(this);
								break;
						}
						modelLabel.Content = model;
						getConf();
						getRemote();
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
						getConf();
						getRemote();
					}
					catch
					{
						esito = false;
					}
				}
				else
				{
					badShow(STR_unitNotReady);
					sp.Close();
					uiDisconnected();
				}

				if (!esito)
				{
					badShow(STR_unitNotReady);
					sp.Close();
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
			if (!(bool)sp.IsOpen)
			{
				sp.Open();
			}
			YesNo yn = new YesNo("Do you really want to erase the memory?", "MEMORY ERASING");
			yn.Owner = this;
			if (yn.ShowDialog() == 1)
			{
				try
				{
					oUnit.eraseMemory();
					Thread.Sleep(50);
					statusProgressBar.Maximum = oUnit.askMaxMemory();
					Thread.Sleep(50);
					statusProgressBar.Value = oUnit.askMemory();
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
			if (!(bool)sp.IsOpen)
			{
				sp.Open();
			}
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
			ConfigurationWindow confForm;
			if (oUnit.modelCode == model_AGM1)
			{
				confForm = new AgmConfigurationWindow(conf, oUnit.modelCode);
			}
			else if (oUnit.modelCode == model_axy5)
			{
				if (oUnit.firmTotA < 1001000)
				{
					confForm = new ConfigurationWindows.Axy5ConfigurationWindow_Legacy(conf, oUnit.firmTotA);

				}
				else
				{
					confForm = new Axy5ConfigurationWindow(conf, accSchedule, oUnit.firmTotA);
				}

			}
			else if (oUnit.modelCode == model_axyTrek)
			{
				confForm = new TrekMovementConfigurationWindow(conf, oUnit.firmTotA);
			}
			else
			{
				confForm = new AxyConfigurationWindow(conf, oUnit.firmTotA);
			}

			confForm.Owner = this;
			System.Windows.Forms.Integration.ElementHost.EnableModelessKeyboardInterop(confForm);
			confForm.ShowDialog();

			try
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
			catch (Exception ex)
			{
				warningShow(ex.Message);
				uiDisconnected();
				return;
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
				if (!(bool)sp.IsOpen)
				{
					sp.Open();
				}
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
				conf = new ConfigurationWindows.TrekPositionConfigurationWindow(ref oUnit);
			}
			else if (type == 2)
			{
				conf = new ConfigurationWindows.GipsyPositionConfigurationWindow(ref oUnit);
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
			if (!(bool)sp.IsOpen)
			{
				sp.Open();
			}
			UInt32 memoryocc = 0;
			try
			{
				memoryocc = oUnit.askMemory();
			}
			catch (Exception ex)
			{
				badShow(ex.Message);
				uiDisconnected();
				return;
			}

			if ((memoryocc == 0))
			{
				warningShow(STR_memoryEMpty);
				return;
			}

			double oldMax = statusProgressBar.Maximum;
			double oldMin = statusProgressBar.Minimum;
			double oldVal = statusProgressBar.Value;
			string oldCon = (string)statusLabel.Content;
			statusProgressBar.IsIndeterminate = true;
			statusLabel.Content = "Downloading...";
			lastSettings = System.IO.File.ReadAllLines(iniFile);

			Microsoft.Win32.SaveFileDialog saveRaw = new Microsoft.Win32.SaveFileDialog();
			saveRaw.OverwritePrompt = true;
			saveRaw.AddExtension = true;
			saveRaw.FileName = unitNameTextBox.Text;
			if (((unitModel[0] == model_axyTrek) || (unitModel[0] == model_Co2Logger)))
			{
				saveRaw.DefaultExt = "Ard file|*.ard";
				saveRaw.Filter = "Ard file|*.ard";
			}
			else
			{
				saveRaw.DefaultExt = "Ard file|*.ard1";
				saveRaw.Filter = "Ard file|*.ard1";
			}

			if (System.IO.File.Exists(lastSettings[3]))
			{
				saveRaw.InitialDirectory = lastSettings[3];
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

			lastSettings[3] = System.IO.Path.GetDirectoryName(saveRaw.FileName);
			System.IO.File.WriteAllLines(iniFile, lastSettings);
			UInt32 fromMemory = 0;
			if (System.IO.File.Exists((System.IO.Path.GetDirectoryName(saveRaw.FileName) + ("\\" + (System.IO.Path.GetFileNameWithoutExtension(saveRaw.FileName) + ".mdp")))))
			{

				//YesNo yn = new YesNo(STR_resumeDownloadQuestion, "RESUME?", STR_Resume, STR_Restart);
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
						System.IO.FileInfo fi = new System.IO.FileInfo((System.IO.Path.GetDirectoryName(saveRaw.FileName) + ("\\" + (System.IO.Path.GetFileNameWithoutExtension(saveRaw.FileName) + ".mdp"))));
						fromMemory = System.Convert.ToUInt32(fi.Length);
						break;
					case 2:
						fromMemory = 0;
						break;
				}
			}

			UInt32 toMemory = (memoryocc / 4096);
			toMemory *= 4096;
			toMemory += 4096;
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
			Thread downloadThread = new Thread(() => oUnit.download(this, saveRaw.FileName, fromMemory, toMemory, baudrate));
			if (oUnit.remote)
			{
				downloadThread = new Thread(() => oUnit.downloadRemote(this, saveRaw.FileName, fromMemory, toMemory, baudrate));
			}

			downloadThread.SetApartmentState(ApartmentState.STA);
			downloadThread.Start();
		}

		private void convertDataClick(object sender, RoutedEventArgs e)
		{
			lastSettings = System.IO.File.ReadAllLines(iniFile);
			Microsoft.Win32.OpenFileDialog fOpen = new Microsoft.Win32.OpenFileDialog();
			if (File.Exists(lastSettings[4]))
			{
				fOpen.InitialDirectory = lastSettings[4];
			}

			fOpen.FileName = "*.ard";
			fOpen.Filter = "Axy Raw Data|*.ard|Memory dump file|*.memDump;*.mdp";
			fOpen.Multiselect = true;
			if ((fOpen.ShowDialog() == false))
			{
				return;
			}

			ConversionPreferences cp = new ConversionPreferences();
			cp.Owner = this;
			cp.ShowDialog();
			if ((cp.goOn == false))
			{
				return;
			}

			lastSettings[4] = System.IO.Path.GetDirectoryName(fOpen.FileName);
			System.IO.File.WriteAllLines(iniFile, lastSettings);
			stDebugLevel = cp.debugLevel;
			oldUnitDebug = cp.OldUnitDebug;
			addGpsTime = cp.addGpsTime;
			convFiles = new List<string>();
			convFiles.AddRange(fOpen.FileNames);
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
				setLatency(portShortName, 1);
				try
				{
					sp.Open();
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message);
					return;
				}
				sp.Close();

				var remoteManagement = new RemoteManagement(ref sp, this);
				remoteManagement.ShowDialog();
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
					setLatency(portShortName, 1);
					try
					{
						sp.Open();
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message);
						return;
					}
					sp.Close();
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
					charts = new ChartWindowAGM(sp.PortName);
					break;
			}
		}

		public bool externConnect()
		{
			bool res = false;
			connectClick(this, new RoutedEventArgs());
			if (connectButton.Content.Equals("Disconnect"))
			{
				res = true;
			}
			return res;
		}

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
			if (!(bool)sp.IsOpen)
			{
				sp.Open();
			}
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
			if (!(bool)sp.IsOpen)
			{
				sp.Open();
			}
			try
			{
				Thread.Sleep(10);
				firmwareLabel.Content = oUnit.askFirmware();
				Thread.Sleep(10);
				unitNameTextBox.Text = oUnit.askName();
				Thread.Sleep(10);
				sp.ReadExisting();
				Thread.Sleep(10);
				batteryLabel.Content = oUnit.askBattery();
				Thread.Sleep(10);
				statusProgressBar.Maximum = oUnit.askMaxMemory();
				statusProgressBar.Minimum = 0;
				Thread.Sleep(10);
				statusProgressBar.Value = oUnit.askMemory();
				Thread.Sleep(10);
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
			if (!(bool)sp.IsOpen)
			{
				sp.Open();
			}
			Thread.Sleep(10);
			remote = false;
			string title = "X MANAGER";
			if (oUnit.isRemote())
			{
				remote = true;
				title += " REMOTE";
			}
			this.Title = title;
		}

		private void spurgo()
		{
			if (!(bool)sp.IsOpen)
			{
				sp.Open();
			}
			while (sp.BytesToRead != 0) sp.ReadByte();
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
			if (lastSettings[0].Contains("null") | !System.IO.File.Exists(lastSettings[0]))
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
				png.UriSource = new Uri(lastSettings[0], UriKind.Absolute);
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
						lastSettings[0] = files[0];
						System.IO.File.WriteAllLines(iniFile, lastSettings);
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

		private unsafe void setLatency(string targetSerialPortName, byte latency)
		{
			int deviceCount = 0;
			string stringCode;
			byte[] stringCodeBuffer = new byte[64];
			FT_HANDLE FT_Handle = 0;
			FT_HANDLE comNumber = 0;
			string comNumberS;
			//string FT_Serial_Number;

			// Recupera il numero di dispositivi connessi
			if (FT_ListDevices(ref deviceCount, null, FT_LIST_NUMBER_ONLY) != FT_STATUS.FT_OK) return;

			for (int i = 0; i <= deviceCount; i++)
			{
				//Ottiene il serial number
				fixed (byte* pBuf = stringCodeBuffer)
				{
					if (FT_ListDevices((UInt32)i, pBuf, (FT_LIST_BY_INDEX | FT_OPEN_BY_SERIAL_NUMBER)) != FT_STATUS.FT_OK) continue;
					StringBuilder sb = new StringBuilder();
					for (int j = 0; stringCodeBuffer[j] != 0; j++)
					{
						sb.Append(Convert.ToChar(stringCodeBuffer[j]));
					}
					stringCode = sb.ToString();
				}

				//Apre il dispositivo identificato dallo StringCode
				if (FT_OpenEx(stringCode, (UInt32)1, ref FT_Handle) != FT_STATUS.FT_OK) continue;

				//Chiede il numero di porta COM associata al dispositivo
				if (FT_GetComPortNumber(FT_Handle, ref comNumber) != FT_STATUS.FT_OK) continue;
				comNumberS = "COM" + comNumber.ToString();

				//Se la porta è quella desiderata, imposta il tempo di latenza del buffer a 1ms
				if (comNumberS == targetSerialPortName)
				{
					if (FT_SetLatencyTimer(FT_Handle, latency) != FT_STATUS.FT_OK) return;
					ftdiSerialNumber = stringCode;
				}
				FT_Close(FT_Handle);
			}

		}

		#endregion

		#region Conversione

		public void nextFile()
		{
			convFile++;
			bool ardFile = true;
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
				fileName = convFiles[0];
			}
			catch
			{
				GoOn = false;
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
					ardFile = false;
				}
				else
				{
					foreach (string nomefile in nomiFile)
					{
						if (System.IO.File.Exists(nomefile))
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
						System.IO.File.Delete(nomefile);
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
						statusProgressBar.Maximum = oUnit.askMaxMemory();
						statusProgressBar.Minimum = 0;
						Thread.Sleep(10);
						statusProgressBar.Value = oUnit.askMemory();
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
			if (ardFile)
			{
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
					if (ardFile) fw = (byte)fs.ReadByte();
					if ((fw < 2)) cUnit = new Axy4_1(this);
					else cUnit = new Axy4_2(this);
					break;
				case Unit.model_axy5:
					cUnit = new Axy5(this);
					break;
				case Unit.model_axyDepth:
					if (ardFile) fw = (byte)fs.ReadByte();
					if ((fw < 2)) cUnit = new AxyDepth_1(this);
					else cUnit = new AxyDepth_2(this);
					break;
				case Unit.model_axyTrek:
					cUnit = new AxyTrek(this);
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
			if (ardFile)
			{
				statusProgressBar.Maximum = FileLength;
				progressBarStopButton.IsEnabled = true;
				progressBarStopButtonColumn.Width = new GridLength(80);
				conversionThread = new Thread(() => cUnit.convert(this, fileName, prefFile));
			}
			else
			{
				conversionThread = new Thread(() => cUnit.extractArds(fileName, fileName, false));
			}

			conversionThread.SetApartmentState(ApartmentState.STA);
			conversionThread.Start();
		}

		private void findFirmware(string fn, byte unitType, ref UInt32 fTotA, ref UInt32 fTotB, ref byte[] uf)
		{
			var fileIn = new BinaryReader(System.IO.File.Open(fn, FileMode.Open));
			fileIn.ReadByte();

			switch (unitType)
			{
				case (byte)model_axyTrek:
					fileIn.Read(uf, 0, 6);
					fTotA = uf[0] * (uint)1000000 + uf[1] * (uint)1000 + uf[2];
					fTotB = uf[3] * (uint)1000000 + uf[4] * (uint)1000 + uf[5];
					uf[6] = 254;
					break;
				case (byte)model_axy3:
					fileIn.Read(uf, 0, 2);
					fTotA = uf[0] * (uint)1000 + uf[1];
					uf[2] = 254;
					break;
				case (byte)model_axy4:
				case (byte)model_axyDepth:
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
				case (byte)model_AGM1:
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

		#endregion


		//internal static int FT_OpenEx(string p1, FT_HANDLE p2, ref int FT_Handle)
		//{
		//    throw new NotImplementedException();
		//}
	}

}
