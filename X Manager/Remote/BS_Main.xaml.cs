using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace X_Manager.Remote
{
	/// <summary>
	/// Logica di interazione per BS_Main.xaml
	/// </summary>
	public partial class BS_Main : Window
	{

		string currentDriveLetter = "";
		BS_listViewElement currentListViewElement = null;
		List<BS_listViewElement[]> historyList;

		public BS_Main()
		{
			InitializeComponent();
			Point p = new Point(0, 0);
			listDrive();
			Loaded += loaded;
			historyList = new List<BS_listViewElement[]>();
		}

		private void loaded(Object sender, RoutedEventArgs e)
		{
			channelListGB.Visibility = Visibility.Hidden;
			plusB.Visibility = Visibility.Hidden;
			minusB.Visibility = Visibility.Hidden;
			notValidL.Content = "Please, select a drive\r\nfrom the list.";
		}

		private void listDrive()
		{
			undoB.Content = "<-";
			var dl = DriveInfo.GetDrives();
			foreach (var drive in dl)
			{
				try
				{
					var lve = new BS_listViewElement(drive);
					driveLV.Items.Add(lve);
				}
				catch { }
			}
		}

		private void driveItemSelected(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{

			var drive = ((BS_listViewElement)driveLV.SelectedItem).Drive;
			string driveLetter = drive.Name;
			if (currentDriveLetter == driveLetter) return;
			bool valid = false;

			while (true)
			{
				if (drive.DriveFormat != "FAT32") break;
				if (drive.VolumeLabel != "BaseStation") break;
				if (!File.Exists(driveLetter + "BS_CONF.dat")) break;
				if (!File.Exists(driveLetter + "BS_NAME.txt")) break;
				if (!File.Exists(driveLetter + "BS_SCHEDULE.txt")) break;
				if (!File.Exists(driveLetter + "UNITS.dat")) break;
				if (!Directory.Exists(driveLetter + "CONFIG")) break;
				valid = true;
				break;
			}
			if (valid)
			{

				channelListGB.Visibility = Visibility.Visible;
				plusB.Visibility = Visibility.Visible;
				minusB.Visibility = Visibility.Visible;
				undoB.Visibility = Visibility.Visible;
				notValidL.Visibility = Visibility.Hidden;

				fillChannelList(drive);
			}
			else
			{
				channelListGB.Visibility = Visibility.Hidden;
				plusB.Visibility = Visibility.Hidden;
				minusB.Visibility = Visibility.Hidden;
				undoB.Visibility = Visibility.Hidden;
				notValidL.Visibility = Visibility.Visible;
				notValidL.Content = "Not a valid BaseStation drive.";
			}

		}

		private void fillChannelList(DriveInfo d)
		{
			channelLV.Items.Clear();
			byte[] buff = File.ReadAllBytes(d.Name + "UNITS.dat");
			var s = buff.Skip(8).Take(8).ToArray();
			Array.Reverse(s);
			UInt64 units = BitConverter.ToUInt64(s, 0);
			int counter = 0x10;
			for (uint i = 0; i < units; i++)
			{
				int address = buff[counter] * 65536 + buff[counter + 1] * 256 + buff[counter + 2];

				bool green = buff[counter + 4] == 1 ? true : false;

				var lve = new BS_listViewElement(address, green);
				channelLV.Items.Add(lve);

				counter += 0x10;
			}
			historyList.Clear();

		}

		private void channelItemSelected(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			currentListViewElement = (BS_listViewElement)channelLV.SelectedItem;
		}

		private void plusB_Click(object sender, RoutedEventArgs e)
		{
			history_addItem();
		}

		private void minusB_Click(object sender, RoutedEventArgs e)
		{
			history_addItem();
			if (currentListViewElement != null)
			{
				channelLV.Items.Remove(currentListViewElement);
			}
		}

		private void undoB_Click(object sender, RoutedEventArgs e)
		{
			history_restoreItem();
		}

		private void history_addItem()
		{
			BS_listViewElement[] newHistoryItem = new BS_listViewElement[channelLV.Items.Count];
			channelLV.Items.CopyTo(newHistoryItem, 0);
			historyList.Add(newHistoryItem);
		}

		private void history_restoreItem()
		{
			if (historyList.Count == 0) return;

			channelLV.Items.Clear();
			foreach (var item in historyList[historyList.Count - 1])
			{
				channelLV.Items.Add(item);
			}
			historyList.RemoveAt(historyList.Count - 1);
		}

	}
}
