﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO.Ports;
using System.Threading;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace X_Manager.Bootloader
{
	/// <summary>
	/// Logica di interazione per Bootloader_Gipsy6.xaml
	/// </summary>
	public partial class Bootloader_Gipsy6 : Window
	{
		SerialPort sp;
		bool unitConnected;
		public bool success = false;
		string firmwareFile;
		readonly byte[] baudrateSYnc = { 0x55, 0x55 };
		readonly byte[] COMMAND_PING = { 0x03, 0x20, 0x20 };
		readonly byte[] COMMAND_GET_CHIP_ID = { 0x03, 0x28, 0x28 };

		BackgroundWorker bgw;

		public Bootloader_Gipsy6(SerialPort sp, bool unitConnected)
		{
			InitializeComponent();
			Loaded += loaded;
			this.sp = sp;
			this.unitConnected = unitConnected;
			flashB.IsEnabled = false;

			sp.ReadTimeout = 30;
			if (!sp.IsOpen)
			{
				sp.Open();
			}
			sp.ReadExisting();

			if (unitConnected == true)
			{
				int test = 0;
				sp.Write("TTTTTTGGAL");
				try
				{
					test = sp.ReadByte();   //Byte di risposta per verifica correttezza comando
				}
				catch { }

				if ((char)test != 'L')
				{
					MessageBox.Show("Firmware updating not ready. Please try again...");
					//Close();
					return;
				}

				sp.Write("K");
				Thread.Sleep(10);
			}
		}

		struct program
		{
			public int address;
			public int size;
			public byte[] data;
		}

		struct section
		{
			public int address;
			public int size;
			public int sh_type;
			public int sh_flags;
			public byte[] data;
		}

		struct fPage
		{
			public int address;
			public int crc;
			public byte[] data;
		}

		List<fPage> pages;

		private void loaded(object sender, RoutedEventArgs e)
		{
			if (!sp.IsOpen)
			{
				sp.Open();
			}
			sp.BaudRate = 115200;
			Thread.Sleep(400);
			sp.ReadExisting();
			sp.ReadTimeout = 200;
			sp.Write(COMMAND_PING, 0, COMMAND_PING[0]);
			try
			{
				sp.ReadByte();
				sp.ReadByte();
			}
			catch
			{
				for (int i = 0; i < 2; i++)
				{
					sp.Write(baudrateSYnc, 0, 2);
					Thread.Sleep(100);
					var resp = new byte[sp.BytesToRead];
					sp.Read(resp, 0, resp.Length);
					if (resp.Length != 0 && resp[resp.Length - 1] == 0xcc)
					{
						break;
					}
					Thread.Sleep(500);
					if (i == 1)
					{
						MessageBox.Show("Bootloader. Please try again...");
						//Close();
						return;
					}
				}
			}
			firmwareFile = X_Manager.Parent.getParameter("gipsy6FirmwareFile", "");
			fileTB.Text = firmwareFile;
			connectB.IsEnabled = false;
			getChipId();
			flashB.IsEnabled = true;
		}

		void getChipId()
		{
			byte[] resp = new byte[8];
			sp.Write(COMMAND_GET_CHIP_ID, 0, COMMAND_GET_CHIP_ID[0]);
			try
			{
				byte test = 0;
				while (test == 0)
				{
					test = (byte)sp.ReadByte();
				}
				if (test == 0x33)
				{
					MessageBox.Show("Bootloader not ready. Please try again...");
					Close();
					return;
				}
				for (int i = 0; i < 6; i++)
				{
					resp[i] = (byte)sp.ReadByte();
				}
				sp.Write(new byte[] { 0xcc }, 0, 1);
			}
			catch
			{
				MessageBox.Show("Bootloader not ready. Please try again...");
				Close();
				return;
			}
			string PG_REV = (resp[2] >> 4).ToString();
			string VER = ((resp[2] >> 2) & 0b11).ToString();
			string PA = "P";
			if ((resp[2] & 0b10) == 0)
			{
				PA = "R";
			}
			string CC13 = "CC13xx";
			if ((resp[3] & 0b10000000) == 0)
			{
				CC13 = "CC26xx";
			}
			string SEQ = ((resp[3] >> 3) & 0b1111).ToString();
			string PKG = "";
			switch (resp[3] & 0b111)
			{
				case 0:
					PKG = "4x4mm QFN (RHB) package";
					break;
				case 1:
					PKG = "5x5mm QFN (RSM) package";
					break;
				case 2:
					PKG = "7x7mm QFN (RGZ) package";
					break;
				case 3:
					PKG = "Wafer sale package (naked die)";
					break;
				case 4:
					PKG = "WCSP (YFV)";
					break;
				case 5:
					PKG = "7x7mm QFN package with Wettable Flanks";
					break;
			}
			string PROTOCOL = "";
			byte pr = (byte)(resp[4] >> 4);
			if ((pr & 1) == 1) PROTOCOL += "BLE ";
			if ((pr & 2) == 2) PROTOCOL += "RF4CE ";
			if ((pr & 4) == 4) PROTOCOL += "Zigbee/6lowpan ";
			if ((pr & 8) == 8) PROTOCOL += "Proprietary";

			statusTB.Text += CC13 + PA + "1F" + PG_REV + "R" + "\r\n" + PKG + "\r\n";
			statusTB.Text += "VER:" + VER + " SEQ:" + SEQ;
			statusTB.Text += "\r\nProtocol(s): " + PROTOCOL;
			return;
		}

		private void openFileB_Click(object sender, RoutedEventArgs e)
		{
			var open = new System.Windows.Forms.OpenFileDialog();
			open.Filter = "BIN flash image|*.bin";
			try
			{
				open.InitialDirectory = System.IO.Path.GetDirectoryName(X_Manager.Parent.getParameter("gipsy6FirmwareFile"));
			}
			catch { }
			if ((open.ShowDialog() == System.Windows.Forms.DialogResult.OK) & (open.FileName != ""))
			{
				firmwareFile = open.FileName;
				X_Manager.Parent.updateParameter("gipsy6FirmwareFile", firmwareFile);
				fileTB.Text = firmwareFile;
			}
			else
			{
				return;
			}
		}

		private void flashB_Click(object sender, RoutedEventArgs e)
		{
			if (!System.IO.File.Exists(firmwareFile))
			{
				MessageBox.Show("File not valid.");
				return;
			}

			byte[] file = System.IO.File.ReadAllBytes(firmwareFile);
			pages = new List<fPage>();

			for (int i = 0; i < (file.Length / 0x02000); i++)
			{
				fPage fp = new fPage();
				fp.address = i * 0x2000;
				fp.data = new byte[0x2000];
				Array.Copy(file, i * 0x2000, fp.data, 0, 0x2000);
				uint sum = 0;
				foreach (byte b in fp.data)
				{
					sum += b;
				}
				if (sum == 0)
				{
					fp.data = null;
				}
				fp.crc = (byte)(sum & 0xff);
				pages.Add(fp);
			}

			bool[] settings = new bool[2];
			settings[0] = wipeDataCB.IsEnabled;
			settings[1] = wipeSettingsCB.IsEnabled;
			bgw = new BackgroundWorker();
			bgw.WorkerReportsProgress = true;
			bgw.WorkerSupportsCancellation = true;
			bgw.DoWork += flashWork;
			bgw.ProgressChanged += flashProgressChanged;
			bgw.RunWorkerAsync(argument: settings);
		}

		void flashWork(object sender, DoWorkEventArgs e)
		{
			bool wipeData = ((bool[])e.Argument)[0];
			bool wipeSettings = ((bool[])e.Argument)[1];
			int oldReadTimeout = sp.ReadTimeout;
			sp.ReadTimeout = 1;
			bgw.ReportProgress(-1);
			uint address = 0;
			for (int i = 0; i < 0x2b; i++)
			{
				address = (uint)(i * 0x2000);
				if (address >= 0x10000 & address < 0x20000)
				{
					if (wipeData)
					{
						sectorErase(address);
					}
				}
				else if (address == 0x20000)
				{
					if (wipeSettings)
					{
						sectorErase(address);
					}
				}
				else if (address == 0x22000)
				{
					if (wipeData)
					{
						sectorErase(address);
					}
				}
				else
				{
					sectorErase(address);
				}
				bgw.ReportProgress(i);
			}
			sp.ReadExisting();
			bgw.ReportProgress(-2);
			bgw.ReportProgress(0);
			sp.ReadTimeout = 1;
			foreach (fPage fp in pages)
			{
				{
					if ((fp.address >= 0x10000) & (fp.address < 0x20000))
					{
						if (wipeData)
						{
							if (fp.data != null)
							{
								pageProgram(fp);
							}
						}
					}
					else if (fp.address == 0x20000)
					{
						if (wipeSettings)
						{
							if (fp.data != null)
							{
								pageProgram(fp);
							}
						}
					}
					else if (fp.address == 0x22000)
					{
						if (wipeData)
						{
							if (fp.data != null)
							{
								pageProgram(fp);
							}
						}
					}
					else if (fp.address == 0x56000)
					{
						break;
					}
					else
					{
						if (fp.data != null)
						{
							pageProgram(fp);
						}
					}
					bgw.ReportProgress(fp.address / 0x2000);
				}
			}

			bgw.ReportProgress(0x56000 / 0x2000);

			bgw.ReportProgress(-3);
			sectorErase(0x56000);
			pageProgram(pages[pages.Count - 1]);
			//Thread.Sleep(200);
			bgw.ReportProgress(-4);
			sp.Write(new byte[] { 0x03, 0x25, 0x25 }, 0, 3);
			ackRes();
			Thread.Sleep(500);
			bgw.ReportProgress(-5);
			sp.ReadTimeout = oldReadTimeout;
		}

		void flashProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			if (e.ProgressPercentage < 0)
			{
				switch (e.ProgressPercentage)
				{
					case -1:
						statusL.Content = "Erasing flash...";
						break;
					case -2:
						statusL.Content = "Writing firmware...";
						break;
					case -3:
						statusL.Content = "Writing ccfg...";
						break;
					case -4:
						statusL.Content = "Restarting...";
						break;
					case -5:
						statusL.Content = "FIRMWARE UPDATED.";
						statusTB.Text = "";
						connectB.IsEnabled = true;
						statusPB.Value = 0;
						flashB.IsEnabled = false;
						break;
				}
			}
			else
			{
				statusPB.Value = e.ProgressPercentage;
			}
		}

		void sectorErase(uint address)
		{
			byte[] erComm = new byte[7];
			erComm[0] = 7;
			erComm[2] = 0x26;
			byte[] adArr = BitConverter.GetBytes(address);
			Array.Reverse(adArr);
			Array.Copy(adArr, 0, erComm, 3, 4);
			crc(erComm);
			sp.Write(erComm, 0, 7);
			Thread.Sleep(1);
			ackRes();
		}

		void pageProgram(fPage page)
		{
			byte[] buff = new byte[256];
			int status = 0;
			while (status != 0x40)
			{
				byte[] comm = new byte[11];
				comm[0] = 11;
				comm[2] = 0x21;
				byte[] adArr = BitConverter.GetBytes(page.address);
				Array.Reverse(adArr);
				Array.Copy(adArr, 0, comm, 3, 4);
				comm[10] = 0x00;
				comm[9] = 0x20;
				crc(comm);
				sp.Write(comm, 0, comm[0]);
				ackRes();
				status = getStatus();
			}

			byte[] data = new byte[131];
			data[0] = 131;
			data[2] = 0x24;
			for (int i = 0; i < 64; i++)
			{
				Array.Copy(page.data, i * 128, data, 3, 128);
				crc(data);
				sp.Write(data, 0, 131);
				ackRes();

				if (getStatus() != 0x40)
				{
					i--;
				}
			}
		}

		void crc(byte[] dataIn)
		{
			int crc = 0;
			foreach (byte b in dataIn.Skip(2).ToArray())
			{
				crc += b;
			}
			crc &= 0xff;
			dataIn[1] = (byte)(crc & 0xff);
		}

		bool ackRes()
		{
			while (sp.BytesToRead < 2) ;
			sp.ReadByte();
			int g = sp.ReadByte();
			if (g == 0xcc) return true;
			else return false;
		}

		int getStatus()
		{
			sp.Write(new byte[] { 0x03, 0x023, 0x23 }, 0, 3);
			ackRes();
			while (sp.BytesToRead < 3) ;
			sp.ReadByte();
			sp.ReadByte();
			int status = sp.ReadByte();
			sp.Write(new byte[] { 0xcc }, 0, 1);
			return status;
		}

		private void old_flashB_Click(object sender, RoutedEventArgs e)
		{
			if (!System.IO.File.Exists(firmwareFile))
			{
				MessageBox.Show("File not valid.");
				return;
			}

			byte[] file = System.IO.File.ReadAllBytes(firmwareFile);

			if (file.Length < 4 || !(new byte[] { 0x7f, 0x45, 0x4c, 0x46 }).SequenceEqual(file.Take(4)))
			{
				MessageBox.Show("File not valid.");
				return;
			}

			var POINTER_programHeaderTable = BitConverter.ToInt32(file, 0x1c);
			var POINTER_sectionHeaderTable = BitConverter.ToInt32(file, 0x20);
			var SIZE_header = BitConverter.ToInt16(file, 0x28);
			var SIZE_programHeaderEntry = BitConverter.ToInt16(file, 0x2a);
			var SIZE_sectionHeaderEntry = BitConverter.ToInt16(file, 0x2e);
			var NUMBER_programHeaderEntries = BitConverter.ToInt16(file, 0x2c);
			var NUMBER_sectionHeaderEntries = BitConverter.ToInt16(file, 0x30);
			var INDEX_sectionNamesEntry = BitConverter.ToInt16(file, 0x32);
			var POINTER_fileFirmwareOffset = BitConverter.ToInt32(file, POINTER_programHeaderTable + 0x04);
			var POINTER_mcuFirmwareOffset = BitConverter.ToInt32(file, POINTER_programHeaderTable + 0x0c);

			var programs = new List<program>();
			var sections = new List<section>();

			for (int i = 0; i < NUMBER_programHeaderEntries; i++)
			{
				int fileLocation = BitConverter.ToInt32(file, POINTER_programHeaderTable + (i * SIZE_programHeaderEntry) + 0x04);
				int fileSize = BitConverter.ToInt32(file, POINTER_programHeaderTable + (i * SIZE_programHeaderEntry) + 0x10);
				if (fileSize > 0)
				{
					var p = new program();
					p.address = BitConverter.ToInt32(file, POINTER_programHeaderTable + (i * SIZE_programHeaderEntry) + 0x0c);
					p.size = BitConverter.ToInt32(file, POINTER_programHeaderTable + (i * SIZE_programHeaderEntry) + 0x14);
					p.data = new byte[p.size];
					Array.Copy(file, fileLocation, p.data, 0, p.size);
					programs.Add(p);
					string filename = "C:\\Users\\marco\\Desktop\\files\\P-" + p.address.ToString("X6") + "-" + (p.address + p.size).ToString("X6");
					System.IO.File.WriteAllBytes(filename, p.data);
				}
			}

			for (int i = 0; i < NUMBER_sectionHeaderEntries; i++)
			{
				int fileLocation = BitConverter.ToInt32(file, POINTER_sectionHeaderTable + (i * SIZE_sectionHeaderEntry) + 0x10);
				int fileSize = BitConverter.ToInt32(file, POINTER_sectionHeaderTable + (i * SIZE_sectionHeaderEntry) + 0x14);
				if (fileSize > 0)
				{
					var s = new section();
					s.address = BitConverter.ToInt32(file, POINTER_sectionHeaderTable + (i * SIZE_sectionHeaderEntry) + 0x0c);
					s.size = BitConverter.ToInt32(file, POINTER_sectionHeaderTable + (i * SIZE_sectionHeaderEntry) + 0x14);
					s.sh_type = BitConverter.ToInt32(file, POINTER_sectionHeaderTable + (i * SIZE_sectionHeaderEntry) + 0x04);
					s.sh_flags = BitConverter.ToInt32(file, POINTER_sectionHeaderTable + (i * SIZE_sectionHeaderEntry) + 0x08);
					s.data = new byte[s.size];
					Array.Copy(file, fileLocation, s.data, 0, s.size);
					sections.Add(s);
					string filename = "C:\\Users\\marco\\Desktop\\files\\S-" + s.address.ToString("X6") + "-" + (s.address + s.size).ToString("X6");
					System.IO.File.WriteAllBytes(filename, s.data);
				}
			}
		}

		private void connectB_Click(object sender, RoutedEventArgs e)
		{
			if (!sp.IsOpen)
			{
				sp.Open();
			}
			sp.BaudRate = 115200;
			//Thread.Sleep(400);
			sp.ReadExisting();
			sp.ReadTimeout = 10;
			sp.Write(COMMAND_PING, 0, COMMAND_PING[0]);
			try
			{
				sp.ReadByte();
				sp.ReadByte();
			}
			catch
			{
				for (int i = 0; i < 2; i++)
				{
					sp.Write(baudrateSYnc, 0, 2);
					Thread.Sleep(1);
					var resp = new byte[sp.BytesToRead];
					sp.Read(resp, 0, resp.Length);
					if (resp.Length != 0 && resp[resp.Length - 1] == 0xcc)
					{
						break;
					}
					Thread.Sleep(5);
					if (i == 1)
					{
						MessageBox.Show("Bootloader. Please try again...");
						//Close();
						return;
					}
				}
			}
			firmwareFile = X_Manager.Parent.getParameter("gipsy6FirmwareFile", "");
			fileTB.Text = firmwareFile;
			connectB.IsEnabled = false;
			getChipId();
			statusL.Content = "CONNECTED.";
			flashB.IsEnabled = true;
		}
	}
}