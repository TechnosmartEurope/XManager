using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

#if X64
#else
using FT_HANDLE = System.UInt32;
#endif

namespace X_Manager.Remote
{
	/// <summary>
	/// Interaction logic for RemoteConnector.xaml
	/// </summary>

	public partial class MS_Connector : UserControl
	{

		string portShortName;

		//private int connectionResult = 0;
		private SerialPort sp;
		public FTDI_Device ft;
		volatile int stop = 0;

		private byte remoteCommunicationAddress = 2;
		private byte remoteWakeUpAddress = 0xff;

		int masterStationType = 0;
		bool autoClose = true;

		const int COLOR_STOP_PRESSED = 100;
		const int COLOR_WAKE = 101;
		const int COLOR_NOWAKE = 102;
		const int COLOR_WAKE_NOCONN = 103;

		byte[] firmware;


		MS_Main parent;

		SolidColorBrush grigio = new SolidColorBrush();
		SolidColorBrush blu = new SolidColorBrush();
		SolidColorBrush celeste = new SolidColorBrush();
		SolidColorBrush bianco = new SolidColorBrush();
		SolidColorBrush verde = new SolidColorBrush();
		SolidColorBrush rosso = new SolidColorBrush();
		SolidColorBrush ambra = new SolidColorBrush();

		string programDataListFile = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) +
			"\\TechnoSmArt Europe\\X Manager\\addressList.chn";
		string lastListPath = "";

		System.Windows.Shapes.Rectangle[] rAr;

		public MS_Connector(ref SerialPort serialPort, object p, string portShortName)
		{
			InitializeComponent();

			parent = (MS_Main)p;
			this.portShortName = portShortName;
			firmware = new byte[] { 0, 1, 0 };
			//parent.connectionResult = 0;

			grigio.Color = Color.FromArgb(0xff, 0x42, 0x42, 0x42);
			blu.Color = Color.FromArgb(0xff, 0x00, 0xaa, 0xde);
			celeste.Color = Color.FromArgb(0xff, 0x10, 0xc9, 0xff);
			bianco.Color = Color.FromArgb(0xff, 0xff, 0xff, 0xff);
			rosso.Color = Color.FromArgb(0xff, 0xff, 0x00, 0x00);
			verde.Color = Color.FromArgb(0xff, 0x16, 0xf5, 0x00);
			ambra.Color = Color.FromArgb(0xff, 0xff, 158, 0x03);

			rAr = new System.Windows.Shapes.Rectangle[] { r1, r2, r3, r4, ra, rb };

			channelListCB.Items.Clear();

			if (System.IO.File.Exists(programDataListFile))
			{
				loadChannelList();
			}
			sp = serialPort;

			//sp.Open();

			DragEnter += loadNewChannelList_Click;

			Loaded += loaded;
		}

		private void loaded(object sender, RoutedEventArgs e)
		{
			if (sp.IsOpen) sp.Close();
			ft = new FTDI_Device(sp.PortName);
			bootLoaderB.Visibility = Visibility.Hidden;
			masterStationType = 0;
			ft.Open();
			ft.BaudRate = 2000000;
			ft.ReadTimeout = 500;
			ft.Write("+++");
			try
			{
				Thread.Sleep(10);
				ft.ReadExisting();
				ft.Write(new byte[] { (byte)'A', (byte)'T', (byte)'V', (byte)'N' }, 0, 4);
				masterStationType = ft.ReadByte();
				masterStationType = ft.ReadByte();
				bootLoaderB.Visibility = Visibility.Visible;
				masterStationType = 1;
				firmware = new byte[3] { 1, 0, 2 };
				ft.Write(new byte[] { (byte)'A', (byte)'T', (byte)'F', (byte)'V' }, 0, 4);
				firmware[0] = ft.ReadByte();
				firmware[1] = ft.ReadByte();
				firmware[2] = ft.ReadByte();
				Thread.Sleep(190);
			}
			catch
			{

			}
			ft.Write(new byte[] { (byte)'A', (byte)'T', (byte)'X' }, 0, 3);
			firmwareL.Content = "Current Firmware Version: " + firmware[0].ToString() + "." + firmware[1].ToString() + "." + firmware[2].ToString();
		}

		public void UI_disconnected()
		{
			wakeB.Content = "WAKE";
			colorStep(100);
		}

		private void wakeClick(object sender, RoutedEventArgs e)
		{
			ft.OpenSimple();
			autoClose = true;
			if ((System.Windows.Forms.Control.ModifierKeys & System.Windows.Forms.Keys.Control) == System.Windows.Forms.Keys.Control)
			{
				autoClose = false;
			}

			//parent.setLatency(portShortName, 1);

			if (wakeB.Content.Equals("BREAK"))
			{
				ft.Write("TTTTTTTTTTTTTGGAO");
				//sp.Close();
				UI_disconnected();
				parent.connect(115200);
				return;
			}

			//if (!sp.IsOpen)
			//{
			//	sp.Open();
			//}
			stop = 0;

			ft.BaudRate = 2000000;
			ft.Write(new byte[] { 52 }, 0, 1);
			ft.ReadTimeout = 200;
			Thread.Sleep(50);
			ft.Write("+++");
			try
			{
				Thread.Sleep(10);
				ft.ReadExisting();
				ft.Write(new byte[] { (byte)'A', (byte)'T', (byte)'V', (byte)'N' }, 0, 4);
				ft.ReadByte();
				ft.ReadByte();
				Thread.Sleep(10);
				ft.Write("ATX");

				Thread.Sleep(100);
			}
			catch
			{

			}

			if (masterStationType == 0)
			{
				master0();
			}
			else
			{
				master1();
			}

		}

		private void master0()
		{
			ft.Write("+++");
			Thread.Sleep(200);
			ft.Write(new byte[] { (byte)'A', (byte)'T', (byte)'B', (byte)'R', 3 }, 0, 5);
			ft.BaudRate = 115200;
			Thread.Sleep(100);
			ft.Write("ATX");
			Thread.Sleep(250);
			ft.Write(new byte[] { (byte)'+', (byte)'+', (byte)'+' }, 0, 3);

			Thread.Sleep(250);
			int address;// = byte.Parse(channelListCB.Text);
			System.Globalization.NumberStyles ns = System.Globalization.NumberStyles.Integer;
			if (channelListCB.Text == "c" | channelListCB.Text == "C")
			{
				channelListCB.Text = "0xffffff";
			}
			else if (channelListCB.Text == "l" | channelListCB.Text == "L")
			{
				channelListCB.Text = "0xfffff0";
			}
			try
			{
				if (channelListCB.Text.Substring(0, 2).Equals("0x") | channelListCB.Text.Substring(0, 2).Equals("0X"))
				{
					ns = System.Globalization.NumberStyles.HexNumber;
					channelListCB.Text = channelListCB.Text.Remove(0, 2);
					channelListCB.Text = channelListCB.Text.ToLower();
					if (!int.TryParse(channelListCB.Text, ns, System.Globalization.CultureInfo.InvariantCulture, out address))
					{
						var w = new Warning("Invalid address.");
						w.picUri = "pack://application:,,,/Resources/alert2.png";
						w.ShowDialog();
						return;
					}
					ns = System.Globalization.NumberStyles.Integer;
					channelListCB.Text = address.ToString();
				}
			}
			catch { }
			if (!int.TryParse(channelListCB.Text, ns, System.Globalization.CultureInfo.InvariantCulture, out address))
			{
				var w = new Warning("Invalid address.");
				w.picUri = "pack://application:,,,/Resources/alert2.png";
				w.ShowDialog();
				return;
			}

			remoteWakeUpAddress = 2;
			if (address == 0xffffff)
			{
				remoteWakeUpAddress = 0xff;
			}
			ft.Write(new byte[] { (byte)'A', (byte)'T', (byte)'R', (byte)'A', remoteWakeUpAddress }, 0, 5);
			Thread.Sleep(100);
			channelListCB.IsEnabled = false;
			remoteCommunicationAddress++;
			if (remoteCommunicationAddress == 9)
			{
				remoteCommunicationAddress = 3;
			}
			ft.Write(new byte[] { (byte)'A', (byte)'T', (byte)'A', (byte)'W',
				(byte)((address >> 16) & 0xff), (byte)((address >> 8) & 0xff), (byte)(address & 0xff),
				1, remoteCommunicationAddress}, 0, 9);
			Thread antennaThread = new Thread(() => animateAntenna());
			antennaThread.Start();
			wakeB.IsEnabled = false;
			Thread commThread = new Thread(() => communicationAttempt(address, 0));
			commThread.SetApartmentState(ApartmentState.STA);
			commThread.Start();
			stopB.IsEnabled = true;
		}

		private void master1()
		{
			ft.Write("+++");
			Thread.Sleep(250);
			int address;// = byte.Parse(channelListCB.Text);
			System.Globalization.NumberStyles ns = System.Globalization.NumberStyles.Integer;
			if (channelListCB.Text == "c" | channelListCB.Text == "C")
			{
				channelListCB.Text = "0xffffff";
			}
			if (channelListCB.Text == "l" | channelListCB.Text == "L")
			{
				channelListCB.Text = "0xfffff0";
			}
			try
			{
				if (channelListCB.Text.Substring(0, 2).Equals("0x") | channelListCB.Text.Substring(0, 2).Equals("0X"))
				{
					ns = System.Globalization.NumberStyles.HexNumber;
					channelListCB.Text = channelListCB.Text.Remove(0, 2);
					channelListCB.Text = channelListCB.Text.ToLower();
					if (!int.TryParse(channelListCB.Text, ns, System.Globalization.CultureInfo.InvariantCulture, out address))
					{
						var w = new Warning("Invalid address.");
						w.picUri = "pack://application:,,,/Resources/alert2.png";
						w.ShowDialog();
						return;
					}
					ns = System.Globalization.NumberStyles.Integer;
					channelListCB.Text = address.ToString();
				}
			}
			catch { }
			if (!int.TryParse(channelListCB.Text, ns, System.Globalization.CultureInfo.InvariantCulture, out address))
			{
				var w = new Warning("Invalid address.");
				w.picUri = "pack://application:,,,/Resources/alert2.png";
				w.ShowDialog();
				return;
			}

			channelListCB.IsEnabled = false;

			ft.Write(new byte[] { (byte)'A', (byte)'T', (byte)'A', (byte)'W',0,0,1,
				(byte)((address >> 16) & 0xff), (byte)((address >> 8) & 0xff), (byte)(address & 0xff)}, 0, 10);
			Thread antennaThread = new Thread(() => animateAntenna());
			antennaThread.Start();
			wakeB.IsEnabled = false;
			Thread commThread = new Thread(() => communicationAttempt(address, 1));
			commThread.SetApartmentState(ApartmentState.STA);
			commThread.Start();
			stopB.IsEnabled = true;
		}

		private void channelListCB_Click(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				e.Handled = true;
				wakeClick(this, new RoutedEventArgs());
			}
		}

		private void communicationAttempt(int address, byte masteSTationType)
		{
			//Stopwatch sw = new Stopwatch();
			ft.ReadTimeout = 3000;
			byte status = 0;
			byte connCount = 0;
			byte connCountMax = 15;
			if (masterStationType == 1) connCountMax = 6;
			while (stop == 0)
			{
				try
				{
					status = ft.ReadByte();
					//sw.Start();
					if (status < 3)
					{
						connCount++;
					}
					if (connCount == connCountMax + 1)
					{
						status = 2;
					}
				}
				catch
				{
					stop = 102;
					var w = new Warning("Master Station not ready.");
					w.picUri = "pack://application:,,,/Resources/alert2.png";
					w.ShowDialog();
					Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => this.channelListCB.IsEnabled = true));
					Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => this.wakeB.IsEnabled = true));
					Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => this.finalize(0, masteSTationType)));
					status = 255;
					return;
				}
				if (status == 0)
				{
					if (stop == 0)
					{
						if (masterStationType == 0)
						{
							ft.Write(new byte[] { (byte)'A', (byte)'T', (byte)'A', (byte)'W', (byte)((address >> 16) & 0xff), (byte)((address >> 8) & 0xff), (byte)(address & 0xff), 1, remoteCommunicationAddress }, 0, 9);
						}
						else
						{
							ft.Write(new byte[] { (byte)'A', (byte)'T', (byte)'A', (byte)'W',0,0,1,
				(byte)((address >> 16) & 0xff), (byte)((address >> 8) & 0xff), (byte)(address & 0xff) }, 0, 10);
						}
					}
					else
					{
						ft.Write("ATX");
					}
				}
				else
				{
					if (status == 1)    //L'unità ha risposto
					{
						//int unitType = 1;   //Unità normale
						//if (address == 1677216) unitType = 2;   //Chiarire!
						stop = 1;
						Thread.Sleep(100);
						ft.Write("ATX");
						Thread.Sleep(500);
						Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => channelListCB.IsEnabled = true));
						Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => wakeB.IsEnabled = true));
						Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => finalize(1, masteSTationType)));
					}
					else if (status == 2)   //Raggiunto massimo numero di tentativi
					{
						stop = 2;
						Thread.Sleep(100);
						ft.Write("ATX");
						Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => this.channelListCB.IsEnabled = true));
						Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => this.wakeB.IsEnabled = true));
					}
				}
			}
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => this.channelListCB.IsEnabled = true));
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => this.wakeB.IsEnabled = true));
		}

		private void stopClick(object sender, RoutedEventArgs e)
		{
			stop = 100;
			stopB.IsEnabled = false;
		}

		private void finalize(int result, int masteStationType)
		{
			//parent.connectionResult = result;
			ft.Close();
			if (result == 1)
			{
				int baudRate = 115200;
				if (masterStationType == 1)
				{
					baudRate = 2000000;
				}
				if (parent.connect(baudRate))
				{
					if (!autoClose)
					{
						autoClose = true;
					}
					else
					{
						wakeB.Content = "BREAK";
						parent.close();
					}
				}
				else
				{
					colorStep(COLOR_WAKE_NOCONN);
					//ft.Close();
				}
			}
		}

		private void colorStep(int counter)
		{
			switch (counter)
			{
				case 0:
					foreach (System.Windows.Shapes.Rectangle r in rAr)
					{
						r.Fill = grigio;
					}
					r1.Fill = blu;
					break;
				case 1:
					r1.Fill = grigio;
					r2.Fill = blu;
					break;
				case 2:
					r2.Fill = grigio;
					r3.Fill = blu;
					break;
				case 3:
					r3.Fill = grigio;
					r4.Fill = blu;
					break;
				case 4:
					r1.Fill = celeste;
					r4.Fill = blu;
					break;
				case 5:
					r1.Fill = grigio;
					r2.Fill = celeste;
					break;
				case 6:
					r2.Fill = grigio;
					r3.Fill = celeste;
					break;
				case 7:
					r3.Fill = grigio;
					r4.Fill = celeste;
					ra.Fill = blu;
					rb.Fill = blu;
					break;
				case 8:
					r4.Fill = grigio;
					ra.Fill = bianco;
					rb.Fill = bianco;
					break;
				case COLOR_STOP_PRESSED:               //E' stato premuto stop
					foreach (System.Windows.Shapes.Rectangle r in rAr)
					{
						r.Fill = grigio;
					}
					break;
				case COLOR_WAKE:               //L'unità ha risposto
					foreach (System.Windows.Shapes.Rectangle r in rAr)
					{
						r.Fill = verde;
					}
					break;
				case COLOR_NOWAKE:               //L'unità non ha risposto
					foreach (System.Windows.Shapes.Rectangle r in rAr)
					{
						r.Fill = rosso;
					}
					break;
				case COLOR_WAKE_NOCONN:               //L'unità ha risposto ma la connessione non è andata a buon fine
					foreach (System.Windows.Shapes.Rectangle r in rAr)
					{
						r.Fill = ambra;
					}
					break;
			}
		}

		private void animateAntenna()
		{
			int counter = 0;
			while (stop == 0)
			{
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => this.colorStep(counter)));
				switch (counter)
				{
					case 0:
						Thread.Sleep(200);
						break;
					case 1:
						Thread.Sleep(200);
						break;
					case 2:
						Thread.Sleep(200);
						break;
					case 3:
						Thread.Sleep(50);
						break;
					case 4:
						Thread.Sleep(50);
						break;
					case 5:
						Thread.Sleep(50);
						break;
					case 6:
						Thread.Sleep(50);
						break;
					case 7:
						Thread.Sleep(50);
						break;
					case 8:
						Thread.Sleep(150);
						break;
				}
				counter++;
				if (counter == 9)
				{
					counter = 0;
				}
			}
			int resp = stop;
			if (resp < 100) resp += 100;
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => colorStep(resp)));
		}

		private void channelListSelectionChangedEvent(object sender, SelectionChangedEventArgs e)
		{
			colorStep(100);
		}

		private void loadChannelList()
		{
			channelListCB.SelectionChanged -= channelListSelectionChangedEvent;
			List<String> addresses = System.IO.File.ReadAllLines(programDataListFile).ToList<string>();
			lastListPath = addresses[0];
			addresses.RemoveAt(0);
			int res;
			channelListCB.Items.Clear();
			foreach (string address in addresses)
			{
				if (int.TryParse(address, out res))
				{
					channelListCB.Items.Add(address);
				}
			}
			channelListCB.FontSize = 40;
			channelListCB.SelectedIndex = 0;
			stopB.IsEnabled = false;
			channelListCB.SelectionChanged += channelListSelectionChangedEvent;
		}

		private void loadNewChannelList(string fileName)
		{
			if (string.IsNullOrEmpty(fileName))
			{
				var fl = new System.Windows.Forms.OpenFileDialog();
				if (!string.IsNullOrEmpty(lastListPath))
				{
					fl.InitialDirectory = lastListPath;
				}
				fl.Filter = "Channel List Files (*.chn)|*.chn";
				if (!(fl.ShowDialog() == System.Windows.Forms.DialogResult.OK))
				{
					return;
				}
				fileName = fl.FileName;
			}

			string[] newAddresses = System.IO.File.ReadAllLines(fileName);
			System.IO.File.WriteAllText(programDataListFile, fileName + "\r\n");
			int res;
			foreach (string address in newAddresses)
			{
				if (int.TryParse(address, out res))
				{
					System.IO.File.AppendAllText(programDataListFile, address + "\r\n");
				}
			}

			loadChannelList();
		}

		private void loadNewChannelList_Click(object sender, RoutedEventArgs e)
		{
			loadNewChannelList(null);
		}

		private void loadNewChannelList_Click(object sender, DragEventArgs e)
		{
			string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
			loadNewChannelList(files[0]);
		}

		private void firmwareUploadClick(object sender, RoutedEventArgs e)
		{
			var tg = new YesNo("WARNING: you are entering the Master Station bootloader!\r\nPlease, proceed only if you have a new firmware to upload,\r\nelse please close this or your Master Station could potentially get bricked.", "Master Station Bootloader", "", "Yes", "No");
			if (tg.ShowDialog() == 2) return;
			if (sp.IsOpen) sp.Close();
			var ft = new FTDI_Device(sp.PortName);
			ft.Open();
			ft.BaudRate = 2000000;
			ft.Write("+++");
			Thread.Sleep(200);
			ft.Write("ATBL");
			Thread.Sleep(200);
			string res = ft.ReadLine();
			ft.Close();
			parent.close();
			parent.MSbootloader();

		}

		//private void close()
		//{
		//	parent.close();
		//}
	}
}

