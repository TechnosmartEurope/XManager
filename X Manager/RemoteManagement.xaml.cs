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

		public RemoteManagement(ref SerialPort sp, object p)
		{
			InitializeComponent();
			parent = (MainWindow)p;
			//this.sp = sp;
			var rconn = new RemoteConnector(ref sp, this);
			connTab.Content = rconn;
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
			if (mainTab.SelectedIndex == 1)
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
	}
}
