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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace X_Manager.ConfigurationWindows
{
	/// <summary>
	/// Interaction logic for AccDayIntervals.xaml
	/// </summary>
	public partial class AccDayIntervals : UserControl
	{
		public AccDayIntervals()
		{
			InitializeComponent();
		}

		private void minusB(object sender, RoutedEventArgs e)
		{

		}

		private void plusB(object sender, RoutedEventArgs e)
		{

		}

		public void importSchedule(byte[] schedule)
		{
			bool hide = false;
			int startInt = 0;
			for (int i = 0; i < 5; i++)
			{
				var v = (StackPanel)(mainGrid.Children[i + 2]);
				if (hide)
				{
					((TextBox)(v.Children[0])).Text = startInt.ToString();
					((TextBox)(v.Children[1])).Text = 24.ToString();
					((ComboBox)(v.Children[2])).SelectedIndex = 0;
					((ComboBox)(v.Children[3])).SelectedIndex = 0;
					((ComboBox)(v.Children[4])).SelectedIndex = 0;
					((ComboBox)(v.Children[5])).SelectedIndex = 0;
					((ComboBox)(v.Children[6])).SelectedIndex = 0;
					v.Visibility = Visibility.Hidden;
				}
				else
				{
					((TextBox)(v.Children[0])).Text = startInt.ToString();
					startInt = schedule[0 + (i * 6)];
					if (startInt == 24)
					{
						hide = true;
					}
					((TextBox)(v.Children[1])).Text = startInt.ToString();
					((ComboBox)(v.Children[2])).SelectedItem = schedule[1 + (i * 6)];
					((ComboBox)(v.Children[3])).SelectedItem = schedule[2 + (i * 6)];
					((ComboBox)(v.Children[4])).SelectedItem = schedule[3 + (i * 6)];
					((ComboBox)(v.Children[5])).SelectedItem = schedule[4 + (i * 6)];
					((ComboBox)(v.Children[6])).SelectedItem = schedule[5 + (i * 6)];
				}
			}


		}

		public byte[] exportSchedule()
		{
			return new byte[] { (byte)0 };
		}

	}

}
