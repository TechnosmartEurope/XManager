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
using System.Windows.Shapes;

namespace X_Manager.Remote
{
	/// <summary>
	/// Logica di interazione per Remote_Main.xaml
	/// </summary>

	public partial class Remote_Main : Window
	{
		int res = 0;
		MainWindow parent;

		public Remote_Main(MainWindow parent)
		{
			this.parent = parent;
			InitializeComponent();
			var location = parent.PointToScreen(new Point(0, 0));
			location.X += 10;
			location.Y += 100;
			Left = location.X;
			Top = location.Y;
		}

		private void msB_Click(object sender, RoutedEventArgs e)
		{
			res = 1;
			Close();
		}

		private void bsB_Click(object sender, RoutedEventArgs e)
		{
			res = 2;
			Close();
		}

		public int showDialog()
		{
			ShowDialog();
			return res;
		}
	}
}
