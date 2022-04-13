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
			var rc = new RemoteConnector(ref sp, this);
			connTab.Content = rc;
		}

		public bool connect()
		{
			return parent.externConnect();
		}
	}
}
