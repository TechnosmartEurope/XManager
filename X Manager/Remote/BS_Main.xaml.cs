using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Threading;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Diagnostics;
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
		List<BS_listViewElement[]> historyList;
		TextBox newChannelTB;
		System.Timers.Timer saveTimer;
		System.Timers.Timer listDriveTimer;
		DriveInfo[] oldDriveList;
		DriveInfo currentSelectedDrive;
		volatile bool saving = false;
		volatile bool closingP = false;
		BS_listViewElement tempBsLvElement;
		int tempBsLvIndex;
		List<CheckBox> scheduleCBArr;

		const string FILE_CONF = "BS_CONF.dat";
		const string FILE_NAME = "BS_NAME.txt";
		const string FILE_SCHEDULE = "BS_SCHEDULE.txt";
		const string FILE_UNITS = "BS_UNITS.dat";
		const string FOLDER_CONFIG = "CONFIG";

		public BS_Main()
		{
			InitializeComponent();
			Point p = new Point(0, 0);
			listDrive();
			Loaded += loaded;
			SizeChanged += sizeChanged;
			Closing += closing;
			historyList = new List<BS_listViewElement[]>();
			currentSelectedDrive = null;

			saveTimer = new System.Timers.Timer();
			saveTimer.Interval = 1000;
			saveTimer.Elapsed += saveTimerElapsed;

			listDriveTimer = new System.Timers.Timer();
			listDriveTimer.Interval = 500;
			listDriveTimer.AutoReset = false;
			listDriveTimer.Elapsed += listDriveTimerElapsed;

			Thickness thickness = new Thickness(5, 20, 0, 0);
			scheduleCBArr = new List<CheckBox>();
			for (int i = 0; i < 24; i++)
			{
				if ((i == 8) || (i == 16))
				{
					thickness.Left += 55;
					thickness.Top = 20;
				}

				var chb = new CheckBox();
				chb.Name = "N" + i.ToString();
				chb.VerticalAlignment = VerticalAlignment.Top;
				chb.HorizontalAlignment = HorizontalAlignment.Left;
				chb.Margin = thickness;
				chb.Content = i.ToString();
				chb.HorizontalContentAlignment = HorizontalAlignment.Left;
				chb.Padding = new Thickness(0);
				chb.FontSize = 10;
				scheduleG.Children.Add(chb);
				scheduleCBArr.Add(chb);
				thickness.Top += 40;
			}
		}

		private void loaded(Object sender, RoutedEventArgs e)
		{
			channelListGB.Visibility = Visibility.Hidden;
			scheduleGB.Visibility = Visibility.Visible;
			plusB.Visibility = Visibility.Hidden;
			minusB.Visibility = Visibility.Hidden;
			plusPlusB.Visibility = Visibility.Hidden;
			undoB.Visibility = Visibility.Hidden;
			notValidL.Content = "Please, select a drive\r\nfrom the list.";
			listDriveTimer.Start();
		}

		private void sizeChanged(object sender, SizeChangedEventArgs e)
		{
			resizeLVelements();
		}

		private void resizeLVelements()
		{
			var newWidth = driveLV.ActualWidth;
			foreach (BS_listViewElement element in driveLV.Items)
			{
				element.Width = newWidth - 15;
			}

			newWidth = channelLV.ActualWidth;
			foreach (BS_listViewElement element in channelLV.Items)
			{
				element.Width = newWidth - 15;
			}
			if (newChannelTB != null)
			{
				newChannelTB.Width = channelLV.Width;
			}
		}

		private void listDrive()
		{
			undoB.Content = "<-";
			var dl = DriveInfo.GetDrives();
			oldDriveList = new DriveInfo[dl.Count()];
			//foreach (var drive in dl)
			for (int i = 0; i < dl.Count(); i++)
			{
				{
					try
					{
						var lve = new BS_listViewElement(dl[i]);
						driveLV.Items.Add(lve);
						Array.Copy(dl, oldDriveList, dl.Count());
					}
					catch { }
				}
			}
		}

		private void driveLV_MouseUp(object sender, MouseButtonEventArgs e)
		{
			driveLvItemClicked();
		}

		private void driveLvItemClicked()
		{
			if (closingP || saving) return;
			var drive = ((BS_listViewElement)driveLV.SelectedItem).Drive;
			currentSelectedDrive = ((BS_listViewElement)driveLV.SelectedItem).Drive;
			if (drive == null) return;
			if (currentDriveLetter == drive.Name) return;

			currentDriveLetter = drive.Name;
			if (validateDrive(drive))
			{

				channelListGB.Visibility = Visibility.Visible;
				plusB.Visibility = Visibility.Visible;
				minusB.Visibility = Visibility.Visible;
				undoB.Visibility = Visibility.Visible;
				plusPlusB.Visibility = Visibility.Visible;
				undoB.Visibility = Visibility.Visible;
				notValidL.Visibility = Visibility.Hidden;

				fillChannelList(drive);
				fillScheduleList(drive);
				resizeLVelements();
			}
			else
			{
				channelListGB.Visibility = Visibility.Hidden;
				plusB.Visibility = Visibility.Hidden;
				minusB.Visibility = Visibility.Hidden;
				undoB.Visibility = Visibility.Hidden;
				plusPlusB.Visibility = Visibility.Hidden;
				undoB.Visibility = Visibility.Hidden;
				notValidL.Visibility = Visibility.Visible;
				notValidL.Content = "Not a valid BaseStation drive.";
			}
		}

		private bool validateDrive(DriveInfo drive)
		{
			if (drive.DriveFormat != "FAT32") return false;
			if (drive.VolumeLabel != "BaseStation") return false;
			if (!File.Exists(drive.Name + FILE_CONF)) return false;
			if (!File.Exists(drive.Name + FILE_NAME)) return false;
			if (!File.Exists(drive.Name + FILE_SCHEDULE)) return false;
			if (!File.Exists(drive.Name + FILE_UNITS)) return false;
			if (!Directory.Exists(drive.Name + FOLDER_CONFIG)) return false;
			return true;
		}

		private void listDriveTimerElapsed(Object source, ElapsedEventArgs e)
		{
			listDriveTimer.Stop();
			var newDriveList = DriveInfo.GetDrives();
			bool changed = true;
			if (newDriveList.Length == oldDriveList.Length)
			{
				changed = false;
				for (int i = 0; i < newDriveList.Length; i++)
				{
					if (oldDriveList[i].Name != newDriveList[i].Name)
					{
						changed = true;
						break;
					}
				}
			}
			//if (!Enumerable.SequenceEqual(oldDriveList, newDriveList))
			if (changed)
			{
				oldDriveList = new DriveInfo[newDriveList.Length];
				Array.Copy(newDriveList, oldDriveList, newDriveList.Length);
				Application.Current.Dispatcher.Invoke(() => driveLV.Items.Clear());
				Application.Current.Dispatcher.Invoke(() => listDrive());

				try
				{
					if (currentSelectedDrive != null)
					{
						string driveName = "";
						string compDriveName = "!";
						Application.Current.Dispatcher.Invoke(() => compDriveName = currentSelectedDrive.Name);
						bool click = false;
						for (int i = 0; i < newDriveList.Length; i++)
						{
							Application.Current.Dispatcher.Invoke(() => driveName = ((BS_listViewElement)driveLV.Items[i]).Drive.Name);
							if (driveName.Equals(compDriveName))
							{
								click = true;
								Application.Current.Dispatcher.Invoke(() => driveLV.SelectedIndex = i);
								break;
							}
						}
						if (click)
						{
							Application.Current.Dispatcher.Invoke(() => driveLvItemClicked());
						}
					}
				}
				catch
				{
					if (driveLV.Items.Count > 0)
					{
						Application.Current.Dispatcher.Invoke(() => driveLV.SelectedIndex = 0);
					}
				}

			}
			listDriveTimer.Start();
		}

		private void fillChannelList(DriveInfo d)
		{
			channelLV.Items.Clear();
			byte[] buff = File.ReadAllBytes(d.Name + FILE_UNITS);
			var s = buff.Skip(8).Take(8).ToArray();
			Array.Reverse(s);
			UInt64 units = BitConverter.ToUInt64(s, 0);
			int counter = 0x10;
			var addsIn = new List<Tuple<int, bool>>();
			int add;
			for (uint i = 0; i < units; i++)
			{
				add = buff[counter] * 65536 + buff[counter + 1] * 256 + buff[counter + 2];
				bool green = buff[counter + 4] == 1 ? true : false;
				int pos = 0;
				for (int j = 0; j < addsIn.Count; j++)
				{
					if (add < addsIn[j].Item1)
					{
						break;
					}
					pos++;
					//add = buff[counter] * 65536 + buff[counter + 1] * 256 + buff[counter + 2];
					//green = buff[counter + 4] == 1 ? true : false;
				}
				addsIn.Insert(pos, new Tuple<int, bool>(add, green));
				counter += 0x10;
			}

			for (int i = 0; i < (int)units; i++)
			{
				var lve = new BS_listViewElement(addsIn[i].Item1, addsIn[i].Item2);
				channelLV.Items.Add(lve);

				counter += 0x10;
			}
			historyList.Clear();

		}

		private void fillScheduleList(DriveInfo d)
		{
			string[] schedule = File.ReadAllLines(d.Name + FILE_SCHEDULE);
		}
		private void plusB_Click(object sender, RoutedEventArgs e)
		{
			plusB.Click -= plusB_Click;
			var tb = new TextBox();
			Grid.SetColumn(tb, 1);
			tb.Width = channelListGB.ActualWidth;
			tb.VerticalAlignment = VerticalAlignment.Bottom;
			tb.Background = new SolidColorBrush(Colors.White);
			tb.Margin = new Thickness(5, 0, 5, 35);
			tb.PreviewKeyDown += tbKD;
			tb.Name = "plusTB";
			newChannelTB = tb;
			MainGrid.Children.Add(tb);
			tb.Focus();
		}

		private void plusPlusB_Click(object sender, RoutedEventArgs e)
		{
			history_addItem();
			if (channelLV.SelectedItem == null)
			{
				channelLV.SelectedItem = channelLV.Items[channelLV.Items.Count - 1];
			}
			int pos = channelLV.SelectedIndex;
			int val = int.Parse(((BS_listViewElement)channelLV.SelectedItem).Text) + 1;
			{
				while (pos < channelLV.Items.Count - 1)
				{
					int compVal = int.Parse(((BS_listViewElement)channelLV.Items[pos + 1]).Text);
					if (val == compVal)
					{
						val++;
						pos++;
					}
					else
					{
						break;
					}
				}
				pos++;
				channelLV.Items.Insert(pos, new BS_listViewElement(val, false));
				channelLV.SelectedIndex = pos;
				saving = true;
				saveTimer.Stop();
				saveTimer.Start();
			}
		}

		private void tbKD(object sender, KeyEventArgs e)
		{
			var tb = (TextBox)sender;
			if (e.Key == Key.Enter)
			{
				int newCh = -1;
				if (int.TryParse(tb.Text, out newCh))
				{
					if (newCh >= 0)
					{
						bool add = true;
						int pos = 0;
						for (int i = 0; i < channelLV.Items.Count; i++)
						{
							BS_listViewElement lv = (BS_listViewElement)channelLV.Items[i];
							if (int.Parse(lv.Text) == newCh)
							{
								add = false;
								break;
							}
							else if (newCh < int.Parse(lv.Text))
							{
								add = true;
								break;
							}
							pos++;
						}
						if (add)
						{
							history_addItem();
							var lvv = new BS_listViewElement(newCh, false);
							lvv.Width = channelLV.ActualWidth - 15;
							channelLV.Items.Insert(pos, lvv);
							channelLV.ScrollIntoView(lvv);
							saveUnitList();
						}
					}
				}
			}
			if (e.Key == Key.Enter || e.Key == Key.Escape)
			{
				tb.PreviewKeyDown -= tbKD;
				MainGrid.Children.Remove(tb);
				e.Handled = true;
				plusB.Click -= plusB_Click;
				plusB.Click += plusB_Click;
				newChannelTB = null;
			}
		}

		private void tbKD2(object sender, KeyEventArgs e)
		{
			var tb = (TextBox)sender;
			bool add = false;
			if (e.Key == Key.Enter)
			{
				e.Handled = true;
				int newCh = -1;
				if (int.TryParse(tb.Text, out newCh))
				{
					if (newCh >= 0)
					{
						channelLV.Items.RemoveAt(tempBsLvIndex);
						int pos = 0;
						for (int i = 0; i < channelLV.Items.Count; i++)
						{
							BS_listViewElement lv = (BS_listViewElement)channelLV.Items[i];
							if (int.Parse(lv.Text) == newCh)
							{
								channelLV.Items.Insert(tempBsLvIndex, tempBsLvElement);
								add = false;
								break;
							}
							else if (newCh < int.Parse(lv.Text))
							{
								add = true;
								break;
							}
							pos++;
							if (i == channelLV.Items.Count - 1)
							{
								add = true;
							}
						}
						if (add)
						{

							tempBsLvElement = new BS_listViewElement(newCh, false);
							tempBsLvElement.Width = channelLV.ActualWidth - 15;
							channelLV.Items.Insert(pos, tempBsLvElement);
							channelLV.ScrollIntoView(tempBsLvElement);
							saveUnitList();
						}
					}
				}
			}
			if (e.Key == Key.Enter || e.Key == Key.Escape)
			{
				e.Handled = true;
				if (!add)
				{
					channelLV.Items.RemoveAt(tempBsLvIndex);
					channelLV.Items.Insert(tempBsLvIndex, tempBsLvElement);
					historyList.RemoveAt(historyList.Count - 1);
				}
				tempBsLvElement = null;
				enableControls(MainGrid);
				channelLV.MouseDoubleClick -= channelLV_MouseDoubleClick;
				channelLV.MouseDoubleClick += channelLV_MouseDoubleClick;
			}

		}

		private void minusB_Click(object sender, RoutedEventArgs e)
		{
			history_addItem();
			int oldPos = channelLV.SelectedIndex;
			oldPos--;
			if (oldPos == -1) oldPos = 0;
			while (channelLV.SelectedItems.Count > 0)
			{
				channelLV.Items.Remove(channelLV.SelectedItems[0]);
			}
			channelLV.SelectedIndex = oldPos;
			saving = true;
			saveTimer.Stop();
			saveTimer.Start();
		}

		private void undoB_Click(object sender, RoutedEventArgs e)
		{
			history_restoreItem();
			saving = true;
			saveTimer.Stop();
			saveTimer.Start();
		}

		private void channelLV_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			history_addItem();
			channelLV.MouseDoubleClick -= channelLV_MouseDoubleClick;
			tempBsLvElement = (BS_listViewElement)channelLV.SelectedItem;
			tempBsLvIndex = channelLV.SelectedIndex;
			var index = channelLV.SelectedIndex;
			var tb = new TextBox();
			tb.Background = new SolidColorBrush(Colors.White);
			tb.Width = ((BS_listViewElement)channelLV.SelectedItem).ActualWidth;
			tb.Height = ((BS_listViewElement)channelLV.SelectedItem).ActualHeight;
			tb.Text = tempBsLvElement.address.ToString();
			tb.PreviewKeyDown += tbKD2;
			tb.Name = "doubleClickTB";
			channelLV.Items.RemoveAt(index);
			channelLV.Items.Insert(index, tb);

			disableControls(MainGrid);

		}

		private void disableControls(FrameworkElement c)
		{
			if (c is Panel)
			{
				foreach (FrameworkElement cIn in ((Panel)c).Children)
				{
					disableControls(cIn);
				}
			}
			else
			{
				if (c is Button)
				{
					c.IsEnabled = false;
				}
			}
		}

		private void enableControls(FrameworkElement c)
		{
			if (c is Panel)
			{
				foreach (FrameworkElement cIn in ((Panel)c).Children)
				{
					enableControls(cIn);
				}
			}
			else
			{
				if (c is Button)
				{
					c.IsEnabled = true;
				}
			}
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

		private void saveTimerElapsed(Object source, ElapsedEventArgs e)
		{
			saveTimer.Stop();
			Application.Current.Dispatcher.Invoke(() => saveUnitList());

			saving = false;
		}

		private void saveUnitList()
		{
			DriveInfo dr;
			byte[] buff;
			try
			{
				dr = ((BS_listViewElement)driveLV.SelectedItem).Drive;
				if (!validateDrive(dr)) return;
				buff = new byte[(channelLV.Items.Count * 16) + 16];
				byte[] unitNum = BitConverter.GetBytes((UInt64)channelLV.Items.Count);
				Array.Reverse(unitNum);
				Array.Copy(unitNum, 0, buff, 8, 8);
				for (int i = 0; i < channelLV.Items.Count; i++)
				{
					var add = BitConverter.GetBytes((UInt32)((BS_listViewElement)channelLV.Items[i]).address);
					Array.Reverse(add);
					Array.Copy(add, 1, buff, (i + 1) * 16, 3);
				}
			}
			catch
			{
				return;
			}
			File.WriteAllBytes(dr.Name + FILE_UNITS, buff);
		}

		private void closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			saveTimer.Stop();
			closingP = true;

			saveUnitList();
		}

		private void allOnB_Click(object sender, RoutedEventArgs e)
		{

		}

		private void allOffB_Click(object sender, RoutedEventArgs e)
		{

		}
	}
}

