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
using System.Runtime.InteropServices;

namespace X_Manager.Remote
{
	/// <summary>
	/// Interaction logic for RemoteManagement.xaml
	/// </summary>
	public partial class MS_Main : Window
	{

		//public int connectionResult = 0;
		MainWindow parent;
		SerialPort sp;
		MS_Connector rconn;
		MS_Supervisor_DateTime_1 rtime;
		MS_Configurator_1 rconf;

		public MS_Main(ref SerialPort sp, object p, string portShortName)
		{
			InitializeComponent();
			parent = (MainWindow)p;
			Owner = parent;
			this.sp = sp;
			//this.sp = sp;
			rconn = new MS_Connector(ref sp, this, portShortName);
			connTab.Content = rconn;
			rtime = new MS_Supervisor_DateTime_1(ref sp, this);
			timeTab.Content = rtime;
			rconf = new MS_Configurator_1(ref sp, this);
			confTab.Content = rconf;
		}

		public bool connect(int baudRate)
		{
			System.Threading.Thread.Sleep(400);
			return parent.externConnect(baudRate);
		}


		public void MSbootloader()
		{
			parent.externBootloader();
		}

		public ref Units.Unit getUnit()
		{
			return ref parent.getReferenceUnit();
		}

		private void tabSelectionChanged(object sender, RoutedEventArgs e)
		{
			if (mainTab.SelectedIndex == (mainTab.Items.Count - 1))
			{
				((MS_Configurator_1)(confTab.Content)).read();
			}
		}

		public void UI_disconnected(int tab)
		{
			if (tab == 0)
			{
				((MS_Configurator_1)(confTab.Content)).read();
			}
			else if (tab == 1)
			{
				((MS_Connector)(connTab.Content)).UI_disconnected();
			}

		}

		private void remoteManagement_KeyDown(object sender, KeyEventArgs e)
		{
			if (Keyboard.Modifiers == ModifierKeys.Control)
			{
				if (e.Key == Key.C)
				{
					if (confTab.Visibility == Visibility.Hidden)
					{
						confTab.Visibility = Visibility.Visible;
					}
					else
					{
						if (mainTab.SelectedIndex == 2)
						{
							mainTab.SelectedIndex = 0;
						}
						confTab.Visibility = Visibility.Hidden;
					}
				}
			}

			//e.Handled = true;
		}

		private void remoteManagement_Loaded(object sender, RoutedEventArgs e)
		{
			confTab.Visibility = Visibility.Hidden;
		}

		private void remoteManagement_Closed(object sender, EventArgs e)
		{
			try
			{
				sp.Close();
			}
			catch { }
			try
			{
				rconn.ft.Close();
			}
			catch { }
		}

		public string setLatency(string portName, byte latency)
		{
			parent.setLatency(portName, latency);
			return "NULL";
		}

		public void close()
		{

			Close();
		}
	}
}
