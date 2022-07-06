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
	/// Logica di interazione per BS_NewUnitName.xaml
	/// </summary>
	public partial class BS_NewUnitName : Window
	{
		public string newName = "";
		public BS_NewUnitName(int address)
		{
			InitializeComponent();
		}

		private void sendB_Click(object sender, RoutedEventArgs e)
		{
			newName = unitNameTB.Text;
			Close();
		}

		private void unitNameTB_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter){
				e.Handled = true;
				newName = unitNameTB.Text;
				Close();
			}
		}

	}
}
