using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
using static System.Net.WebRequestMethods;

namespace X_Manager.Bootloader
{
	/// <summary>
	/// Logica di interazione per Bootloader_Gipsy6_OTA.xaml
	/// </summary>
	public partial class Bootloader_Gipsy6_OTA : Window
	{
		string filename;
		FTDI_Device ft;
		BackgroundWorker upload;
		public Bootloader_Gipsy6_OTA(string filename)
		{
			InitializeComponent();
			this.filename = filename;
			ft = MainWindow.FTDI;
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			byte[] command = new byte[] {   (byte)'T', (byte)'T', (byte)'T', (byte)'T', (byte)'T', (byte)'T', (byte)'T', (byte)'G',
											(byte)'G', (byte)'A', (byte)'L' };

			byte[] outBuffer = new byte[5];
			int res = 0;
			byte[] ack = new byte[1];

			ft.ReadExisting();
			ft.Write(command, 0, 11);
			res = ft.Read(ack, 0, 1);
			if ((res != 1) || (ack[0] == 'l'))
			{
				Close();
				return;
			}

			upload = new BackgroundWorker();
			upload.DoWork += Upload_DoWork;
			upload.ProgressChanged += Upload_ReportProgress;
			upload.RunWorkerCompleted += Upload_RunWorkerCompleted;
		}

		private void Upload_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{

		}

		private unsafe void Upload_DoWork(object sender, DoWorkEventArgs e)
		{
			byte[] outbuffer = new byte[256];
			int res;
			byte[] ack = new byte[1];
			outbuffer[0] = (byte)'T';
			var fs = new BinaryReader(new FileStream(filename, FileMode.Open));
			int address = 0;
			bool mandaPagina = true;

			while (true)
			{
				if ((address % 0x2000) == 0)
				{
					outbuffer[1] = (byte)'A';
					outbuffer[2] = (byte)(address >> 24);
					outbuffer[3] = (byte)(address >> 16);
					outbuffer[4] = (byte)(address >> 8);

					ft.Write(outbuffer, 0, 5);
					res = ft.Read(ack, 0, 1);
					if ((res != 1) || (ack[0] != (byte)'A'))
					{
						continue;
					}
					mandaPagina = analizzaPagina(fs);
				}

				if (mandaPagina)
				{
					outbuffer[1] = (byte)'p';
					outbuffer[2] = (byte)(address % 0x2000 / 0x40);
					fs.Read(outbuffer, 3, 0x40);
					ft.Write(outbuffer, 0, 67);
					res = ft.Read(ack, 0, 1);
					if ((res == 1) && (ack[0] == (byte)'p'))
					{
						address += 0x40;
					}
					else
					{
						fs.BaseStream.Position -= 0x40;
					}
				}

				if ((address % 0x2000) == 0)
				{
					sendCRC();
					if (!mandaPagina) address += 0x2000;
				}

			}
		}

		private void sendCRC()
		{

		}
		private bool analizzaPagina(BinaryReader br)
		{
			byte[] buff = new byte[0x2000];
			br.Read(buff, 0, 0x2000);
			br.BaseStream.Position -= 0x2000;
			bool vuota = true;
			foreach (byte b in buff)
			{
				if (b != 0xff)
				{
					vuota = false;
					break;
				}
			}
			return vuota;
		}
		private void Upload_ReportProgress(object sender, ProgressChangedEventArgs e)
		{

		}

	}
}
