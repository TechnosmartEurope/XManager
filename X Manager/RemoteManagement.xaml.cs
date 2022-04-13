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

namespace X_Manager
{
	/// <summary>
	/// Interaction logic for RemoteManagement.xaml
	/// </summary>
	public partial class RemoteManagement : Window
	{

		//public int connectionResult = 0;
		MainWindow parent;
		SerialPort sp;

		public RemoteManagement(ref SerialPort sp, object p)
		{
			InitializeComponent();
			parent = (MainWindow)p;
			this.sp = sp;
			//this.sp = sp;
			var rconn = new RemoteConnector(ref sp, this);
			connTab.Content = rconn;
			var rtime = new RemoteSupervisor(ref sp, this);
			timeTab.Content = rtime;
			var rconf = new RemoteConfigurator(ref sp, this);
			confTab.Content = rconf;
		}

		public bool connect()
		{
			return parent.externConnect();
		}

		public ref Units.Unit getUnit()
		{
			return ref parent.getReferenceUnit();
		}

		private void tabSelectionChanged(object sender, RoutedEventArgs e)
		{
			if (mainTab.SelectedIndex == (mainTab.Items.Count - 1)) 
			{
				((RemoteConfigurator)(confTab.Content)).read();
			}
		}

		public void UI_disconnected(int tab)
		{
			if (tab == 0)
			{
				((RemoteConfigurator)(confTab.Content)).read();
			}
			else if (tab == 1)
			{
				((RemoteConnector)(connTab.Content)).UI_disconnected();
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
		}
	}
}
