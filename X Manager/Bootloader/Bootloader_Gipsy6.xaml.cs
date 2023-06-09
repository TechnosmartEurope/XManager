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
using System.IO;

namespace X_Manager.Bootloader
{
	/// <summary>
	/// Logica di interazione per Bootloader_Gipsy6.xaml
	/// </summary>
	public partial class Bootloader_Gipsy6 : Window
	{
		bool unitConnected = false;
		public bool success = false;
		string firmwareFile;
		string exportFile;
		readonly byte[] COMMAND_PING = { 0x03, 0x20, 0x20 };
		readonly byte[] COMMAND_GET_CHIP_ID = { 0x03, 0x28, 0x28 };
		readonly uint bootloaderBaudRate = 1000000;
		FTDI_Device ft;
		string device;
		const int NVS_PAGE_SIZE_CC1352 = 0x2000;
		const int NVS_PAGE_SIZE_CC2640R2 = 0x1000;
		const int NVS_DATA_START_CC1352 = 0x20000;
		const int NVS_DATA_START_CC2640R2 = 0xC000;
		const int NVS_DATA_STOP_CC1352 = 0x52000;
		const int NVS_DATA_STOP_CC2640R2 = 0x1D000;
		const int NVS_CONF_CC1352 = 0x52000;
		const int NVS_CONF_CC2640R2 = 0x1D000;
		const int NVS_POINTERS_CC1352 = 0x54000;
		const int NVS_POINTERS_CC2640R2 = 0x1E000;
		const int NVS_CCFG_CC1352 = 0x56000;
		const int NVS_CCFG_CC2640R2 = 0x1F000;
		const int NVS_FLASH_SIZE_CC1352 = 0x58000;
		const int NVS_FLASH_SIZE_CC2640R2 = 0x20000;

		BackgroundWorker bgw;
		int NVS_pageSize = NVS_PAGE_SIZE_CC1352;
		int NVS_dataStart = NVS_DATA_START_CC1352;
		int NVS_dataStop = NVS_DATA_STOP_CC1352;
		int NVS_conf = NVS_CONF_CC1352;
		int NVS_pointers = NVS_POINTERS_CC1352;
		int NVS_ccfg = NVS_CCFG_CC1352;
		int NVS_flashSize = NVS_FLASH_SIZE_CC1352;

		public Bootloader_Gipsy6(bool unitConnected, string device)
		{
			InitializeComponent();
			Loaded += loaded;
			Closing += closing;
			flashB.IsEnabled = false;
			ft = MainWindow.FTDI;
			if (ft is null)
			{
				MessageBox.Show("Can't open serial port.");
				return;
			}
			this.unitConnected = unitConnected;
			flashB.IsEnabled = false;
			ft.ReadTimeout = 300;
			this.device = device;
			titleL.Text = device + " Bootloader";

			connAtt();
		}

		private void connAtt()
		{
			if (unitConnected == true)
			{
				ft.ReadTimeout = 1000;
				if (device.IndexOf("basestation", StringComparison.InvariantCultureIgnoreCase) >= 0)
				{
					ft.BaudRate = 115200;
					Thread.Sleep(100);
					byte[] piu = new byte[3] { 0x2b, 0x2b, 0x2b };
					ft.Write(piu, 0, 3);
					Thread.Sleep(200);
					ft.Write("ATBL");
					try
					{
						ft.ReadLine();
					}
					catch
					{
						MessageBox.Show("Firmware updating not ready. Please try again...");
						ft.BaudRate = 115200;
						return;
					}
				}
				else
				{
					ft.BaudRate = 3000000;
					int test = 0;
					ft.Write("TTTTTTGGAL");
					try
					{
						test = ft.ReadByte();   //Byte di risposta per verifica correttezza comando
					}
					catch { }
					if ((char)test != 'L')
					{
						MessageBox.Show("Firmware updating not ready. Please try again...");
						ft.BaudRate = 115200;
						return;
					}
					ft.Write("K");
					Thread.Sleep(10);
				}
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
			ft.Close();
			ft.Open(bootloaderBaudRate);
			Thread.Sleep(100);
			ft.ReadExisting();
			ft.ReadTimeout = 600;
			ft.Write(new byte[] { 0x55, 0x55 }, 0, 2);
			//ft.BaudRate = 1000000;

			try
			{
				ft.ReadByte();
				ft.ReadByte();
			}
			catch
			{
				ft.ReadTimeout = 10;
				for (int i = 0; i < 500; i++)
				{
					ft.Write(new byte[] { 0x02 }, 0, 1);
					try
					{
						ft.ReadByte();
						break;
					}
					catch
					{
						if (i == 499)
						{
							MessageBox.Show("Bootloader not ready. Please try again...");
							ft.BaudRate = 115200;
							return;

						}
						continue;
					}
				}
				ft.ReadByte();

			}

			//Thread.Sleep(600);
			//ft.ReadExisting();
			ft.ReadTimeout = 800;

			ft.Write(COMMAND_PING, 0, COMMAND_PING[0]);
			try
			{
				ft.ReadByte();
				ft.ReadByte();
			}
			catch
			{
				MessageBox.Show("Bootloader not ready. Please try again...");
				ft.BaudRate = 115200;
				return;
			}

			firmwareFile = X_Manager.Parent.getParameter("gipsy6FirmwareFile", "");
			fileTB.Text = firmwareFile;
			if (!File.Exists(firmwareFile))
			{
				filePropertiesTB.Text = "File not found.";
			}
			else
			{
				filePropertiesTB.Text = "Firmware date: " + File.GetLastWriteTime(firmwareFile);

			}
			wipeDataCB.IsChecked = false;
			if (X_Manager.Parent.getParameter("gipsy6BootloaderWipeData", "true") == "true")
			{
				wipeDataCB.IsChecked = true;
			}
			wipeSettingsCB.IsChecked = false;
			if (X_Manager.Parent.getParameter("gipsy6BootloaderWipeSettings", "true") == "true")
			{
				wipeSettingsCB.IsChecked = true;
			}

			connectB.IsEnabled = false;
			getChipId();
			flashB.IsEnabled = true;
		}

		private void closing(object sender, CancelEventArgs e)
		{
			string b = "false";
			if ((bool)wipeSettingsCB.IsChecked)
			{
				b = "true";
			}
			X_Manager.Parent.updateParameter("gipsy6BootloaderWipeSettings", b);
			b = "false";
			if ((bool)wipeDataCB.IsChecked)
			{
				b = "true";
			}
			X_Manager.Parent.updateParameter("gipsy6BootloaderWipeData", b);

			ft.BaudRate = 115200;
		}

		void getChipId()
		{
			byte[] resp = new byte[8];
			ft.Write(COMMAND_GET_CHIP_ID, 0, COMMAND_GET_CHIP_ID[0]);
			try
			{
				byte test = 0;
				while (test == 0)
				{
					test = ft.ReadByte();
				}
				if (test == 0x33)
				{
					MessageBox.Show("Bootloader not ready. Please try again...");
					ft.BaudRate = 115200;
					Close();
					return;
				}
				for (int i = 0; i < 6; i++)
				{
					resp[i] = ft.ReadByte();
				}
				ft.Write(new byte[] { 0xcc }, 0, 1);
			}
			catch
			{
				MessageBox.Show("Bootloader not ready. Please try again...");
				ft.BaudRate = 115200;
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
					NVS_pageSize = NVS_PAGE_SIZE_CC2640R2;
					NVS_dataStart = NVS_DATA_START_CC2640R2;
					NVS_dataStop = NVS_DATA_STOP_CC2640R2;
					NVS_conf = NVS_CONF_CC2640R2;
					NVS_pointers = NVS_POINTERS_CC2640R2;
					NVS_ccfg = NVS_CCFG_CC2640R2;
					NVS_flashSize = NVS_FLASH_SIZE_CC2640R2;
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
				open.InitialDirectory = System.IO.Path.GetFullPath(System.IO.Path.GetDirectoryName(X_Manager.Parent.getParameter("gipsy6FirmwareFile")));
			}
			catch { }
			for (int i = 0; i < 2; i++)
			{
				try
				{
					if ((open.ShowDialog() == System.Windows.Forms.DialogResult.OK) & (open.FileName != ""))
					{
						firmwareFile = open.FileName;
						X_Manager.Parent.updateParameter("gipsy6FirmwareFile", firmwareFile);
						fileTB.Text = firmwareFile;
						filePropertiesTB.Text = "Firmware date: " + File.GetLastWriteTime(firmwareFile);
					}
					else
					{
						return;
					}
					break;
				}
				catch
				{
					open.InitialDirectory = "C:\\";
				}
			}
		}

		private void flashB_Click(object sender, RoutedEventArgs e)
		{
			if (!File.Exists(firmwareFile))
			{
				MessageBox.Show("File not valid.");
				return;
			}

			byte[] file = File.ReadAllBytes(firmwareFile);
			pages = new List<fPage>();

			for (int i = 0; i < (file.Length / NVS_pageSize); i++)
			{
				fPage fp = new fPage();
				fp.address = i * NVS_pageSize;
				fp.data = new byte[NVS_pageSize];
				Array.Copy(file, i * NVS_pageSize, fp.data, 0, NVS_pageSize);
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

			statusPB.Maximum = (NVS_ccfg / NVS_pageSize) - 1;

			bool[] settings = new bool[2];
			settings[0] = (bool)wipeDataCB.IsChecked;
			settings[1] = (bool)wipeSettingsCB.IsChecked;
			bgw = new BackgroundWorker();
			bgw.WorkerReportsProgress = true;
			bgw.WorkerSupportsCancellation = true;
			bgw.DoWork += flashWork;
			bgw.ProgressChanged += flashProgressChanged;
			bgw.RunWorkerAsync(argument: settings);
		}

		private void exportB_Click(object sender, RoutedEventArgs e)
		{
			exportFile = "";
			var save = new System.Windows.Forms.SaveFileDialog();
			save.Filter = "BIN flash image|*.bin";
			try
			{
				save.InitialDirectory = System.IO.Path.GetFullPath(System.IO.Path.GetDirectoryName(X_Manager.Parent.getParameter("gipsy6FirmwareFile")));
			}
			catch { }
			for (int i = 0; i < 2; i++)
			{
				try
				{
					if ((save.ShowDialog() == System.Windows.Forms.DialogResult.OK) & (save.FileName != ""))
					{
						exportFile = save.FileName;
						X_Manager.Parent.updateParameter("gipsy6FirmwareFile", exportFile);
					}
					else
					{
						return;
					}
					break;
				}
				catch
				{
					save.InitialDirectory = "C:\\";
				}
			}

			statusL.Content = "Reading flash...";
			statusPB.Maximum = (NVS_flashSize / 128) - 1;

			bgw = new BackgroundWorker();
			bgw.WorkerReportsProgress = true;
			bgw.WorkerSupportsCancellation = true;
			bgw.DoWork += exportWork;
			bgw.ProgressChanged += exportProgressChanged;
			bgw.RunWorkerAsync();
		}

		void exportWork(object sender, DoWorkEventArgs e)
		{
			var bw = new BinaryWriter(new FileStream(exportFile, FileMode.Create));

			bool firmwareGood = false;
			int address = 0;
			int noPages = (NVS_ccfg + NVS_pageSize) / NVS_pageSize;
			for (int i = 0; i < noPages; i++)
			{
				int j = 0;
				while (j < NVS_pageSize / 128)
				{
					byte[] comm = new byte[9];
					comm[0] = 9;
					comm[2] = 0x2a;
					byte[] adArr = BitConverter.GetBytes(address);
					Array.Reverse(adArr);
					Array.Copy(adArr, 0, comm, 3, 4);
					comm[7] = 0;
					comm[8] = 128;
					crc(comm);
					ft.Write(comm, 0, comm[0]);
					ackRes();

					int rxSize = 0;
					while (rxSize == 0)
					{
						rxSize = ft.ReadByte();
					}
					rxSize -= 2;
					byte checkSum = ft.ReadByte();
					byte[] buff = new byte[rxSize];
					for (int k = 0; k < rxSize; k++)
					{
						buff[k] = ft.ReadByte();
					}
					if (checkSum == crcRx(buff))
					{
						if (address == 0)
						{
							int somma = 0;
							foreach (byte b in buff)
							{
								somma += b;
							}
							if (somma != 0x7F80)
							{
								firmwareGood = true;
							}
						}
						if ((j == (NVS_pageSize / 128) - 1) && (i == noPages - 1))
						{
							buff[0x6c] = 0;
							buff[0x6d] = 0;
							buff[0x6e] = 0;
							buff[0x6f] = 0;
						}
						address += 128;
						j++;
						bw.Write(buff);
						bgw.ReportProgress(i);
					}
					ft.Write(new byte[] { 0x00, 0xcc }, 2);
				}
			}
			bw.Close();
			bgw.ReportProgress(-1);

			if (firmwareGood)
			{
				byte[] comm = new byte[11];
				comm[0] = 11;
				comm[2] = 0x2d;
				comm[3] = 0;
				comm[4] = 0;
				comm[5] = 0;
				comm[6] = 1;
				comm[7] = 0;
				comm[8] = 0;
				comm[9] = 0;
				comm[10] = 0;
				crc(comm);
				ft.Write(comm, 0, comm[0]);
				ackRes();

				comm = new byte[3];
				comm[0] = 3;
				comm[2] = 0x25;
				crc(comm);
				ft.Write(comm, 0, comm[0]);
				ackRes();
			}
			bgw.ReportProgress(-2);
		}

		void exportProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			if (e.ProgressPercentage == -1)
			{
				statusL.Content = "Done.";
				statusPB.Value = 0;
			}
			else if (e.ProgressPercentage == -2)
			{
				Thread.Sleep(500);
				Close();
			}
			else
			{
				statusPB.Value++;
			}
		}

		void flashWork(object sender, DoWorkEventArgs e)
		{
			bool wipeData = ((bool[])e.Argument)[0];
			bool wipeSettings = ((bool[])e.Argument)[1];
			//bool wipeData = (bool)wipeDataCB.IsChecked;
			//bool wipeSettings = (bool)wipeSettingsCB.IsChecked;
			uint oldReadTimeout = ft.ReadTimeout;
			ft.Open();
			ft.ReadTimeout = 300;
			bgw.ReportProgress(-1);
			uint address = 0;
			//Cancella le pagine interessate
			for (int i = 0; i < NVS_ccfg / NVS_pageSize; i++)
			{
				address = (uint)(i * NVS_pageSize);
				if (address >= NVS_dataStart & address < NVS_dataStop)
				{
					if (wipeData)
					{
						sectorErase(address);
					}
				}
				else if (address == NVS_conf)
				{
					if (wipeSettings)
					{
						sectorErase(address);
					}
				}
				else if (address == NVS_pointers)
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
			ft.ReadExisting();
			bgw.ReportProgress(-2);
			bgw.ReportProgress(0);
			ft.ReadTimeout = 300;
			//Scrive le pagine non vuote
			foreach (fPage fp in pages)
			{
				{
					if ((fp.address >= NVS_dataStart) & (fp.address < NVS_dataStop))
					{
						if (wipeData)
						{
							if (fp.data != null)
							{
								pageProgram(fp);
							}
						}
					}
					else if (fp.address == NVS_conf)
					{
						if (wipeSettings)
						{
							if (fp.data != null)
							{
								pageProgram(fp);
							}
						}
					}
					else if (fp.address == NVS_pointers)
					{
						if (wipeData)
						{
							if (fp.data != null)
							{
								pageProgram(fp);
							}
						}
					}
					else if (fp.address == NVS_ccfg)
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
					bgw.ReportProgress(fp.address / NVS_pageSize);
				}
			}

			bgw.ReportProgress(NVS_dataStop / NVS_dataStart);

			bgw.ReportProgress(-3);
			sectorErase((uint)NVS_ccfg);
			pageProgram(pages[pages.Count - 1]);
			//Thread.Sleep(200);
			bgw.ReportProgress(-4);
			ft.Write(new byte[] { 0x03, 0x25, 0x25 }, 0, 3);
			ackRes();
			Thread.Sleep(500);
			bgw.ReportProgress(-5);
			ft.ReadTimeout = oldReadTimeout;
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
			ft.Write(erComm, 0, 7);
			Thread.Sleep(1);
			ackRes();
		}

		void pageProgram(fPage page)
		{
			//byte[] buff = new byte[256];
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
				comm[9] = (byte)(NVS_pageSize >> 8);
				crc(comm);
				ft.Write(comm, 0, comm[0]);
				ackRes();
				status = getStatus();
			}

			byte[] data = new byte[131];
			data[0] = 131;
			data[2] = 0x24;
			for (int i = 0; i < (NVS_pageSize / 128); i++)
			{
				Array.Copy(page.data, i * 128, data, 3, 128);
				crc(data);
				ft.Write(data, 0, 131);
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

		byte crcRx(byte[] dataIn)
		{
			int crc = 0;
			foreach (byte b in dataIn)
			{
				crc += b;
			}
			crc &= 0xff;
			return (byte)crc;
		}

		bool ackRes()
		{
			while (ft.BytesToRead() < 2) ;
			ft.ReadByte();
			int g = ft.ReadByte();
			if (g == 0xcc) return true;
			else return false;
		}

		int getStatus()
		{
			ft.Write(new byte[] { 0x03, 0x023, 0x23 }, 0, 3);
			ackRes();
			while (ft.BytesToRead() < 3) ;
			ft.ReadByte();
			ft.ReadByte();
			int status = ft.ReadByte();
			ft.Write(new byte[] { 0xcc }, 0, 1);
			return status;
		}

		private void old_flashB_Click(object sender, RoutedEventArgs e)
		{
			if (!File.Exists(firmwareFile))
			{
				MessageBox.Show("File not valid.");
				return;
			}

			byte[] file = File.ReadAllBytes(firmwareFile);

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
					File.WriteAllBytes(filename, p.data);
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
			ft.Open();
			ft.BaudRate = bootloaderBaudRate;
			//Thread.Sleep(400);
			ft.ReadExisting();
			ft.ReadTimeout = 300;
			ft.Write(COMMAND_PING, 0, COMMAND_PING[0]);
			try
			{
				ft.ReadByte();
				ft.ReadByte();
			}
			catch
			{
				for (int i = 0; i < 2; i++)
				{
					ft.Write(new byte[] { 0x55, 0x55 }, 0, 2);
					Thread.Sleep(1);
					var resp = new byte[ft.BytesToRead()];
					ft.Read(resp, 0, (uint)resp.Length);
					if (resp.Length != 0 && resp[resp.Length - 1] == 0xcc)
					{
						break;
					}
					Thread.Sleep(5);
					if (i == 1)
					{
						MessageBox.Show("Bootloader not ready. Please try again...");
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
