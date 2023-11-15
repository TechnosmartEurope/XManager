using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace X_Manager.Remote
{
	/// <summary>
	/// Logica di interazione per DriveListViewElement.xaml
	/// </summary>
	public partial class BS_listViewElement : UserControl
	{

		public DriveInfo Drive;
		public int Address;
		byte[] newConf;
		string newName = "";
		public string Text
		{
			get
			{
				return (string)driveL.Content;
			}
			set
			{
				driveL.Content = value;
			}
		}

		public byte[] NewConf
		{
			get
			{
				return newConf;
			}
			set
			{
				if (value == null)
				{
					newConfL.Foreground = new SolidColorBrush(Color.FromArgb(255, 0x24, 0x20, 0x20));
					newConfL.Background = new SolidColorBrush(Color.FromArgb(255, 0x24, 0x20, 0x20));
					newConf = null;
				}
				else
				{
					newConfL.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
					newConfL.Background = new SolidColorBrush(Color.FromArgb(255, 0x20, 0x60, 0x20));
					newConf = new byte[value.Length];
					Array.Copy(value, 0, newConf, 0, value.Length);
				}
			}
		}

		public string NewName
		{
			get
			{
				return newName;
			}
			set
			{
				if (value == "")
				{
					newNameL.Foreground = new SolidColorBrush(Color.FromArgb(255, 0x24, 0x20, 0x20));
					newNameL.Background = new SolidColorBrush(Color.FromArgb(255, 0x24, 0x20, 0x20));
					newName = "";
				}
				else
				{
					newNameL.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
					newNameL.Background = new SolidColorBrush(Color.FromArgb(255, 0x20, 0x60, 0x20));
					newName = value;
				}
			}
		}

		public new Brush Foreground
		{
			get
			{
				return driveL.Foreground;
			}
			set
			{
				SolidColorBrush s = value as SolidColorBrush;
				driveL.Foreground = new SolidColorBrush(s.Color);
			}
		}

		public BS_listViewElement(DriveInfo di)
		{
			InitializeComponent();
			Drive = di;
			Text = di.Name + " " + di.VolumeLabel;
		}

		public BS_listViewElement(int address, bool green)
		{
			InitializeComponent();
			if (green)
			{
				driveL.Foreground = new SolidColorBrush(Colors.Green);
			}
			this.Address = address;
			Text = address.ToString();
			Name = "N" + address.ToString();
		}
	}
}
