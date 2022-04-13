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
using System.Windows.Threading;
using System.Threading;
using System.IO.Ports;

namespace X_Manager
{
	/// <summary>
	/// Interaction logic for RemoteSupervisor.xaml
	/// </summary>
	public partial class RemoteSupervisor : UserControl
	{
		SerialPort sp;
		RemoteManagement parent;


		public RemoteSupervisor(ref SerialPort sp, object p)
		{
			InitializeComponent();
			this.sp = sp;
			sp.ReadTimeout = 800;
			parent = (RemoteManagement)p;
			var c = new Thread(clock);
			c.Start();
		}

		private void remoteSupervisor_Loaded(object sender, RoutedEventArgs e)
		{

		}

		private void SuperB_Click(object sender, RoutedEventArgs e)
		{
			if (!sp.IsOpen)
			{
				sp.Open();
			}
			Thread.Sleep(10);
			sp.Write("+++");
			Thread.Sleep(50);
			sp.Write(new byte[] { (byte)'A', (byte)'T', (byte)'I', (byte)'D', 0xfc }, 0, 5);
			Thread.Sleep(50);
			sp.Write(new byte[] { (byte)'A', (byte)'T', (byte)'R', (byte)'A', 0x00 }, 0, 5);
			Thread.Sleep(50);
			sp.Write(new byte[] { (byte)'A', (byte)'T', (byte)'X' }, 0, 3);
			Thread.Sleep(50);

			string pUri = "pack://application:,,,/Resources/ok.png";
			Warning w = new Warning("Master Station set as Supervisor Station.");
			w.Owner = parent;
			w.picUri = pUri;
			w.Title = "OK";
			w.ShowDialog();
		}

		private void TimeB_Click(object sender, RoutedEventArgs e)
		{
			if (!sp.IsOpen)
			{
				sp.Open();
			}
			sp.BaudRate = 115200;
			Thread.Sleep(150);
			sp.Write("+++");
			Thread.Sleep(150);
			sp.Write(new byte[] { (byte)'A', (byte)'T', (byte)'I', (byte)'D', 0xfc }, 0, 5);
			Thread.Sleep(150);
			sp.Write(new byte[] { (byte)'A', (byte)'T', (byte)'R', (byte)'A', 0x00 }, 0, 5);
			Thread.Sleep(150);
			sp.Write(new byte[] { (byte)'A', (byte)'T', (byte)'X' }, 0, 3);
			Thread.Sleep(250);
			var orario = DateTime.Now;
			var orarioAr = new byte[10];

			orarioAr[0] = (byte)'R';
			orarioAr[1] = (byte)'T';
			orarioAr[2] = (byte)'C';
			orarioAr[3] = reverseByte(dec2BCD((byte)(orario.Year - 2000)));
			orarioAr[4] = reverseByte(dec2BCD((byte)orario.Month));
			orarioAr[5] = reverseByte(dec2BCD((byte)orario.Day));
			orarioAr[6] = reverseByte(dec2BCD((byte)orario.DayOfWeek));
			orarioAr[7] = reverseByte(dec2BCD((byte)orario.Hour));
			orarioAr[8] = reverseByte(dec2BCD((byte)orario.Minute));
			orarioAr[9] = reverseByte(dec2BCD((byte)orario.Second));

			byte[] orarioComp = new byte[15];
			orarioComp[0] = (byte)'R';
			orarioComp[1] = (byte)'T';
			orarioComp[2] = (byte)'C';
			orarioComp[3] = (byte)' ';
			orarioComp[4] = (byte)'S';
			orarioComp[5] = (byte)'E';
			orarioComp[6] = (byte)'T';
			orarioComp[7] = (byte)((orario.Hour / 10) + 0x30);
			orarioComp[8] = (byte)((orario.Hour % 10) + 0x30);
			orarioComp[9] = (byte)':';
			orarioComp[10] = (byte)((orario.Minute / 10) + 0x30);
			orarioComp[11] = (byte)((orario.Minute % 10) + 0x30);
			orarioComp[12] = (byte)':';
			orarioComp[13] = (byte)((orario.Second / 10) + 0x30);
			orarioComp[14] = (byte)((orario.Second % 10) + 0x30);
			while (sp.BytesToRead != 0)
			{
				sp.ReadExisting();
				Thread.Sleep(200);
			}

			sp.Write(orarioAr, 0, 10);

			var orarioIn = new byte[15];

			try
			{

				for (int i = 0; i < 15; i++)
				{
					orarioIn[i] = (byte)sp.ReadByte();
				}
				if (!orarioComp.SequenceEqual(orarioIn))
				{
					throw new Exception();
				}
			}
			catch
			{
				string pUri = "pack://application:,,,/Resources/bad.png";
				Warning w = new Warning("Basestation not ready!");
				w.Owner = parent;
				w.picUri = pUri;
				w.Title = "";
				w.ShowDialog();
				//parent.connect();
				return;
			}

			string pUri2 = "pack://application:,,,/Resources/ok.png";
			var w2 = new Warning("Date and Time sent to Basestation.");
			w2.Owner = parent;
			w2.picUri = pUri2;
			w2.Title = "OK";
			w2.ShowDialog();

		}

		private byte dec2BCD(byte inb)
		{
			byte outb = 0;

			outb = (byte)(inb / 10);
			outb *= 16;
			outb += (byte)(inb % 10);

			return outb;
		}

		private byte reverseByte(byte inb)
		{
			byte outb = 0;

			outb = (byte)((inb << 7) & 0b1000_0000);
			outb += (byte)((inb << 5) & 0b0100_0000);
			outb += (byte)((inb << 3) & 0b0010_0000);
			outb += (byte)((inb << 1) & 0b0001_0000);
			outb += (byte)((inb >> 1) & 0b0000_1000);
			outb += (byte)((inb >> 3) & 0b0000_0100);
			outb += (byte)((inb >> 5) & 0b0000_0010);
			outb += (byte)((inb >> 7) & 0b0000_0001);

			return outb;
		}

		private void clock()
		{
			while (true)
			{
				try
				{
					Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => this.showTime()));
					Thread.Sleep(999);
				}
				catch
				{
					break;
				}
			}
		}

		public void showTime()
		{
			timeB.Content = "Time to BS (" + DateTime.Now.ToString("dd-MM-" + "20" + "yy HH:mm:ss") + ")";
		}
	}
}
