using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static System.Net.WebRequestMethods;

namespace X_Manager.Bootloader
{
	/// <summary>
	/// Logica di interazione per Bootloader_Gipsy6_OTA.xaml
	/// </summary>
	public partial class Bootloader_Gipsy6_OTA : Window
	{

		const int RETRY_COUNT = 8;
		public enum Result
		{
			RESULT_FLASH_OK = 0,
			RESULT_CONNECTION_LOST,
			RESULT_OPERATION_ABORTED
		}

		string filename;
		FTDI_Device ft;
		BackgroundWorker upload;
		public volatile Result result = Result.RESULT_OPERATION_ABORTED;
		byte[] rfAddress = new byte[3];
		byte remote;
		byte[] conf;
		volatile bool keepCurrentConf;
		volatile bool flashBim;
		volatile bool work;
		//public volatile bool workCompleted;
		public Bootloader_Gipsy6_OTA(string filename, byte[] conf)
		{
			InitializeComponent();
			this.filename = filename;
			ft = MainWindow.FTDI;
			remote = conf[540];
			Array.Copy(conf, 541, rfAddress, 0, 3);
			this.conf = conf;
			otaPB.Value = 0;
			var b = new FileInfo(filename);
			otaPB.Maximum = b.Length;
		}

		private void startB_Click(object sender, RoutedEventArgs e)
		{
			Console.Write("GGAL\r\n");
			byte[] command = new byte[] {   (byte)'T', (byte)'T', (byte)'T', (byte)'T', (byte)'T', (byte)'T', (byte)'T', (byte)'G',
											(byte)'G', (byte)'A', (byte)'L' };

			byte[] outBuffer = new byte[5];
			int res = 0;
			byte[] ack = new byte[1];

			ft.ReadTimeout = 2100;

			ft.ReadExisting();
			ft.Write(command, 0, 11);
			res = ft.Read(ack, 0, 1);
			if ((res != 1) || (ack[0] == 'l'))
			{
				Close();
				return;
			}
			startB.IsEnabled = false;
			Console.WriteLine("Risposta ok. Start");
			upload = new BackgroundWorker();
			upload.WorkerReportsProgress = true;
			upload.DoWork += Upload_DoWork;
			upload.ProgressChanged += Upload_ReportProgress;
			upload.RunWorkerCompleted += Upload_RunWorkerCompleted;
			keepCurrentConf = (bool)keepCurrentCB.IsChecked;
			flashBim = (bool)overwriteBimCB.IsChecked;
			upload.RunWorkerAsync();
		}

		private void Upload_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			Close();
		}

		private unsafe void Upload_DoWork(object sender, DoWorkEventArgs e)
		{
			work = true;
			byte[] outbuffer = new byte[256];
			int res;
			byte[] ack_A = new byte[2];
			byte[] ack_p = new byte[2];
			byte[] ack_C = new byte[2];
			outbuffer[0] = (byte)'T';
			var fss = new BinaryReader(new FileStream(filename, FileMode.Open));
			byte[] firmware = new byte[fss.BaseStream.Length];
			fss.Read(firmware, 0, firmware.Length);
			fss.Close();
			try
			{
				if (keepCurrentConf == true)
				{
					Array.Copy(conf, 0, firmware, 0x52000, 540);
				}
			}
			catch (Exception ex)
			{
				string s = ex.Message;
			}
			firmware[0x52000] = 0xcc;
			firmware[0x52001] = 0xff;
			firmware[0x5221c] = remote;
			Array.Copy(rfAddress, 0, firmware, 0x5221d, 3);
			if (flashBim == false)
			{
				for (int i = 0x56000; i < 57800; i++)
				{
					firmware[i] = 0;
				}
			}

			var fs = new MemoryStream(firmware);

			int address = 0;
			bool paginaPiena = true;
			int retryCounter = 0;
			ft.ReadTimeout = 2100;

			while (work)
			{
				Thread.Sleep(1);
				//try
				//{
				upload.ReportProgress(address);// * 100 / (int)fs.Length);
											   //}
											   //catch (Exception ex)
											   //{
											   //	Console.WriteLine(ex.Message);
											   //}
				if ((address % 0x2000) == 0)
				{
					ack_A = new byte[2] { 0xaa, 0xaa };

					outbuffer[1] = (byte)'A';
					outbuffer[2] = (byte)(address >> 24);
					outbuffer[3] = (byte)(address >> 16);
					outbuffer[4] = (byte)(address >> 8);

					ft.Write(outbuffer, 0, 5);
					res = ft.Read(ack_A, 0, 1);
					Console.WriteLine("A - address: " + address.ToString("X6") + " - res: " + res.ToString() + " - ack:" + ack_A[0].ToString());
					if ((res != 1) || (ack_A[0] != (byte)'A'))
					{
						ft.ReadExisting();
						retryCounter++;
						if (retryCounter == RETRY_COUNT)
						{
							result = Result.RESULT_CONNECTION_LOST;
							work = false;
						}
						continue;
					}
					retryCounter = 0;
					paginaPiena = checkPaginaPiena(fs);
					if (paginaPiena)
					{
						Console.WriteLine("address: " + address.ToString("X6") + " pagina piena ");
					}
					else
					{
						Console.WriteLine("address: " + address.ToString("X6") + " pagina vuota ");
					}
				}

				if (paginaPiena)
				{
					//ft.ReadExisting();
					ack_p = new byte[2] { 0xaa, 0xaa };
					string lettera = "v";
					outbuffer[1] = (byte)'v';
					outbuffer[2] = (byte)(address % 0x2000 / 0x40);
					uint len = 3;
					fs.Read(outbuffer, 3, 0x40);
					for (int i = 3; i < 0x43; i++)
					{
						if (outbuffer[i] != 0)
						{
							len = 67;
							outbuffer[1] = (byte)'p';
							lettera = "p";
							break;
						}
					}
					ft.Write(outbuffer, 0, len);
					res = ft.Read(ack_p, 0, 1);
					Console.WriteLine(lettera + " - address: " + address.ToString("X6") + " - res: " + res.ToString() + " - ack:" + ack_p[0].ToString());
					if ((res == 1) && (ack_p[0] == (byte)'p'))
					{
						address += 0x40;
						retryCounter = 0;
					}
					else
					{
						retryCounter++;
						if (retryCounter == RETRY_COUNT)
						{
							result = Result.RESULT_CONNECTION_LOST;
							work = false;
						}
						ft.ReadExisting();
						fs.Position -= 0x40;
						continue;
					}
				}
				else
				{
					retryCounter = 0;
					Thread.Sleep(1);
					address += 0x2000;
					fs.Position += 0x2000;
				}

				if (((address % 0x2000) == 0) && address > 0)
				{
					//ft.ReadExisting();
					ack_C = new byte[2] { 0xaa, 0xaa };

					uint crc = calcolaCRC(fs);
					//ft.ReadTimeout = 4500;
					outbuffer[1] = (byte)'C';
					if (paginaPiena)
					{
						outbuffer[2] = (byte)(crc >> 24);
						outbuffer[3] = (byte)(crc >> 16);
						outbuffer[4] = (byte)(crc >> 8);
						outbuffer[5] = (byte)(crc & 0xff);
					}
					else
					{
						outbuffer[2] = 0x53;
						outbuffer[3] = 0xd3;
						outbuffer[4] = 0x6b;
						outbuffer[5] = 0xd2;
					}
					ft.Write(outbuffer, 0, 6);
					res = ft.Read(ack_C, 0, 1);
					Console.WriteLine("C - address: " + (address - 0x2000).ToString("X6") + " - res: " + res.ToString() + " - ack0:" + ack_C[0].ToString());
					if ((res == 1) && (ack_C[0] == (byte)'C'))
					{
						ack_C[0] = 0xAA;
						ack_C[1] = 0xAA;
						Thread.Sleep(20);
						outbuffer[1] = (byte)'c';
						ft.Write(outbuffer, 0, 2);							
						res = ft.Read(ack_C, 0, 1);
						Console.WriteLine("c - address: " + (address - 0x2000).ToString("X6") + " - res: " + res.ToString() + " - ack0:" + ack_C[0].ToString());
						if ((res != 1) || (ack_C[0] != 0x01))
						{
							retryCounter++;
							if (retryCounter == RETRY_COUNT)
							{
								result = Result.RESULT_CONNECTION_LOST;
								work = false;
							}
							address -= 0x2000;
							fs.Position -= 0x2000;
							ft.ReadExisting();
							continue;
						}
						retryCounter = 0;
					}
					else
					{
						retryCounter++;
						if (retryCounter == RETRY_COUNT)
						{
							result = Result.RESULT_CONNECTION_LOST;
							work = false;
						}
						address -= 0x2000;
						fs.Position -= 0x2000;
						ft.ReadExisting();
						continue;
					}
				}

				if (address >= fs.Length)
				{
					ack_A = new byte[2] { 0xaa, 0xaa };
					outbuffer[1] = (byte)'x';
					ft.Write(outbuffer, 0, 2);
					res = ft.Read(ack_A, 0, 1);
					result = Result.RESULT_FLASH_OK;
					retryCounter = 0;
					break; 
				}
			}
			if (result == Result.RESULT_OPERATION_ABORTED)
			{
				outbuffer[1] = (byte)'s';
				ft.Write(outbuffer, 0, 2);
				ft.Read(ack_A, 0, 1);
			}

		}

		private uint calcolaCRC(MemoryStream filein)
		{
			filein.Position -= 0x2000;
			byte[] buff = new byte[0x2000];
			filein.Read(buff, 0, 0x2000);
			uint crc = 0xFFFFFFFF;

			for (int counter = 0; counter < 8192; counter++)
			{
				byte c = buff[counter];
				for (int i = 0x80; i != 0; i >>= 1)
				{
					bool bit = (crc & 0x80000000) != 0;
					if ((c & i) != 0)
					{
						bit = !bit;
					}
					crc <<= 1;
					if (bit)
					{
						crc ^= 0x04C11DB7;
					}
				}
			}
			return crc;
		}

		private bool checkPaginaPiena(MemoryStream br)
		{
			byte[] buff = new byte[0x2000];
			br.Read(buff, 0, 0x2000);
			br.Position -= 0x2000;
			bool piena = false;
			foreach (byte b in buff)
			{
				if (b != 0x00)
				{
					piena = true;
					break;
				}
			}
			return piena;
		}

		private void Upload_ReportProgress(object sender, ProgressChangedEventArgs e)
		{
			otaPB.Value = e.ProgressPercentage;
		}

		private void stopB_Click(object sender, RoutedEventArgs e)
		{
			work = false;
		}
	}
}
