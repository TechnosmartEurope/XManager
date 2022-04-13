using System;
using System.Collections.Generic;
using System.Linq;
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
using System.IO.Ports;
using System.Threading;
using System.Windows.Threading;
using System.Diagnostics;

namespace X_Manager
{
	/// <summary>
	/// Interaction logic for RemoteConnector.xaml
	/// </summary>

	public partial class RemoteConnector : UserControl
	{
		//private int connectionResult = 0;
		private SerialPort sp;
		volatile int stop = 0;

		private byte remoteCommunicationAddress = 2;
		private byte remoteWakeUpAddress = 0xff;

		RemoteManagement parent;

		SolidColorBrush grigio = new SolidColorBrush();
		SolidColorBrush blu = new SolidColorBrush();
		SolidColorBrush celeste = new SolidColorBrush();
		SolidColorBrush bianco = new SolidColorBrush();
		SolidColorBrush verde = new SolidColorBrush();
		SolidColorBrush rosso = new SolidColorBrush();

		string programDataListFile = System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData) +
			"\\TechnoSmArt Europe\\X Manager\\addressList.chn";
		string lastListPath = "";


		public RemoteConnector(ref System.IO.Ports.SerialPort serialPort, object p)
		{
			InitializeComponent();

			parent = (RemoteManagement)p;
			//parent.connectionResult = 0;

			grigio.Color = Color.FromArgb(0xff, 0x42, 0x42, 0x42);
			blu.Color = Color.FromArgb(0xff, 0x00, 0xaa, 0xde);
			celeste.Color = Color.FromArgb(0xff, 0x10, 0xc9, 0xff);
			bianco.Color = Color.FromArgb(0xff, 0xff, 0xff, 0xff);
			rosso.Color = Color.FromArgb(0xff, 0xff, 0x00, 0x00);
			verde.Color = Color.FromArgb(0xff, 0x16, 0xf5, 0x00);

			channelListCB.Items.Clear();

			if (System.IO.File.Exists(programDataListFile))
			{
				loadChannelList();
			}
			sp = serialPort;
			sp.Open();

			this.DragEnter += loadNewChannelList_Click;
		}

		//public new int ShowDialog()
		//{
		//	try
		//	{
		//		base.ShowDialog();
		//	}
		//	catch { }
		//	return connectionResult;
		//}

		public void UI_disconnected()
		{
			wakeB.Content = "WAKE";
			colorStep(100);
		}

		private void wakeClick(object sender, RoutedEventArgs e)
		{
			if (wakeB.Content.Equals("BREAK"))
			{
				sp.Write("TTTTTTTTTTTTTGGAO");
				//sp.Close();
				UI_disconnected();
				parent.connect();
				return;
			}

			if (!sp.IsOpen)
			{
				sp.Open();
			}
			stop = 0;
			sp.Write(new byte[] { 52 }, 0, 1);

			sp.BaudRate = 2000000;
			Thread.Sleep(50);
			sp.Write("+++");
			Thread.Sleep(200);
			sp.Write("+++");
			Thread.Sleep(200);
			sp.Write(new byte[] { (byte)'A', (byte)'T', (byte)'B', (byte)'R', 3 }, 0, 5);
			sp.BaudRate = 115200;
			Thread.Sleep(100);
			sp.Write("ATX");

			Thread.Sleep(250);
			sp.Write(new byte[] { (byte)'+', (byte)'+', (byte)'+' }, 0, 3);

			Thread.Sleep(250);
			int address;// = byte.Parse(channelListCB.Text);
			System.Globalization.NumberStyles ns = System.Globalization.NumberStyles.Integer;
			if (channelListCB.Text == "c" | channelListCB.Text == "C")
			{
				channelListCB.Text = "16777215";
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
			sp.Write(new byte[] { (byte)'A', (byte)'T', (byte)'R', (byte)'A', remoteWakeUpAddress }, 0, 5);
			Thread.Sleep(100);
			channelListCB.IsEnabled = false;
			remoteCommunicationAddress++;
			if (remoteCommunicationAddress == 9)
			{
				remoteCommunicationAddress = 3;
			}
			sp.Write(new byte[] { (byte)'A', (byte)'T', (byte)'A', (byte)'W',
				(byte)((address >> 16) & 0xff), (byte)((address >> 8) & 0xff), (byte)(address & 0xff),
				1, remoteCommunicationAddress}, 0, 9);
			Thread antennaThread = new Thread(() => animateAntenna());
			antennaThread.Start();
			wakeB.IsEnabled = false;
			Thread commThread = new Thread(() => communicationAttempt(address));
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

		private void communicationAttempt(int address)
		{
			//Stopwatch sw = new Stopwatch();
			sp.ReadTimeout = 3000;
			byte status = 0;
			byte connCount = 0;
			while (stop == 0)
			{
				try
				{
					status = (byte)sp.ReadByte();
					//sw.Start();
					if (status < 3)
					{
						connCount++;
					}
					if (connCount == 15)
					{
						status = 2;
					}
				}
				catch
				{
					stop = 102;
					var w = new Warning("Base Station not ready.");
					w.picUri = "pack://application:,,,/Resources/alert2.png";
					w.ShowDialog();
					Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => this.channelListCB.IsEnabled = true));
					Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => this.wakeB.IsEnabled = true));
					Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => this.finalize(0)));
					status = 255;
					return;
				}
				if (status == 0)
				{
					if (stop == 0)
					{
						//Thread.Sleep(800);
						//sw.Stop();
						//Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => this.sviluppoTB.Text = sw.Elapsed.Milliseconds.ToString()));
						//sw.Reset();
						sp.Write(new byte[] { (byte)'A', (byte)'T', (byte)'A', (byte)'W', (byte)((address >> 16) & 0xff), (byte)((address >> 8) & 0xff), (byte)(address & 0xff), 1, remoteCommunicationAddress }, 0, 9);
					}
					else
					{
						sp.Write("ATX");
					}
				}
				else
				{
					if (status == 1)    //L'unità ha risposto
					{
						int unitType = 1;   //Unità normale
						if (address == 1677216) unitType = 2;
						stop = 1;
						Thread.Sleep(100);
						sp.Write("ATX");
						Thread.Sleep(500);
						Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => this.channelListCB.IsEnabled = true));
						Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => this.wakeB.IsEnabled = true));
						Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => this.finalize(unitType)));
					}
					else if (status == 2)   //Raggiunto massimo numero di tentativi
					{
						stop = 2;
						Thread.Sleep(100);
						sp.Write("ATX");
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

		private void finalize(int result)
		{
			//parent.connectionResult = result;
			if (result == 1 | result==2)
			{
				if (parent.connect())
				{
					wakeB.Content = "BREAK";
					if (result == 2)
					{
						parent.close();
					}
				}
			}
		}

		private void colorStep(int counter)
		{
			switch (counter)
			{
				case 0:
					r1.Fill = blu;
					r2.Fill = grigio;
					r3.Fill = grigio;
					r4.Fill = grigio;
					ra.Fill = grigio;
					rb.Fill = grigio;
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
				case 100:               //E' stato premuto stop
					r1.Fill = grigio;
					r2.Fill = grigio;
					r3.Fill = grigio;
					r4.Fill = grigio;
					ra.Fill = grigio;
					rb.Fill = grigio;
					break;
				case 101:               //L'unità ha risposto
					r1.Fill = verde;
					r2.Fill = verde;
					r3.Fill = verde;
					r4.Fill = verde;
					ra.Fill = verde;
					rb.Fill = verde;
					break;
				case 102:               //L'unità non ha risposto
					r1.Fill = rosso;
					r2.Fill = rosso;
					r3.Fill = rosso;
					r4.Fill = rosso;
					ra.Fill = rosso;
					rb.Fill = rosso;
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
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => this.colorStep(resp)));

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

		private void close()
		{
			parent.close();
		}
	}
}
