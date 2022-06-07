using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO.Ports;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace X_Manager.ConfigurationWindows
{
	/// <summary>
	/// Logica di interazione per DropOffConfigurationWindow.xaml
	/// </summary>
	public partial class DropOffConfigurationWindow : ConfigurationWindow
	{
		int hours = 0;
		FTDI_Device ft;
		public DropOffConfigurationWindow(byte[] axyconf, UInt32 unitFirm) : base()
		{
			InitializeComponent();
			hours = axyconf[0] * 256 + axyconf[1];
			timerTb.Text = hours.ToString();
			axyConfOut = new byte[2];
			mustWrite = false;
			ft = MainWindow.FTDI;
		}

		private void timerTb_LostFocus(object sender, RoutedEventArgs e)
		{
			validate();
		}

		private void timerTb_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				e.Handled = true;
				validate();
			}
		}

		private void validate()
		{
			int newTimer = 0;
			if (!int.TryParse(timerTb.Text, out newTimer))
			{
				timerTb.Text = hours.ToString();
				return;
			}
			if (newTimer < 0 | newTimer > 65000)
			{
				timerTb.Text = hours.ToString();
				return;
			}
			hours = newTimer;
		}

		private void sendB_Click(object sender, RoutedEventArgs e)
		{
			axyConfOut[0] = (byte)(hours / 256);
			axyConfOut[1] = (byte)(hours % 256);
			mustWrite = true;
			Close();
		}

		private void testB_Click(object sender, RoutedEventArgs e)
		{
			uint oldTimeout = ft.ReadTimeout;
			int res = 0;
			ft.ReadTimeout = 5000;
			ft.Write("TTTTTTTGGAV");
			try
			{
				currentTb.Text = ft.ReadLine();
				res = ft.ReadByte();
			}
			catch
			{
				MessageBox.Show("Unit not ready");
				return;
			}
			if (res == 0)
			{
				MessageBox.Show("WARNING: Test failed!");
			}
			else
			{
				MessageBox.Show("Test OK!");
			}
		}
	}
}
