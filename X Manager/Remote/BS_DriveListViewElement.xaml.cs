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
		public string Text
		{
			get { return (string)driveL.Content; }
			set { driveL.Content = value; }
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
			Text = address.ToString();
		}
	}
}
