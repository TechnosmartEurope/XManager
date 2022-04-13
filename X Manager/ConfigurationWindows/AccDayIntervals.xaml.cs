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
						((TextBox)(v.Children[1])).IsEnabled = false;
					}
					((TextBox)(v.Children[1])).Text = startInt.ToString();
					((ComboBox)(v.Children[2])).SelectedIndex = schedule[1 + (i * 6)];
					((ComboBox)(v.Children[3])).SelectedIndex = schedule[2 + (i * 6)];
					((ComboBox)(v.Children[4])).SelectedIndex = schedule[3 + (i * 6)];
					((ComboBox)(v.Children[5])).SelectedIndex = schedule[4 + (i * 6)];
					((ComboBox)(v.Children[6])).SelectedIndex = schedule[5 + (i * 6)];
				}
			}


		}

		public byte[] exportSchedule()
		{
			byte[] schedule = new byte[30];

			for (int i = 0; i < 5; i++)
			{
				var v = (StackPanel)(mainGrid.Children[i + 2]);
				schedule[i * 6] = byte.Parse(((TextBox)(v.Children[1])).Text);
				schedule[(i * 6) + 1] = ((byte)((ComboBox)(v.Children[2])).SelectedIndex);
				schedule[(i * 6) + 2] = ((byte)((ComboBox)(v.Children[3])).SelectedIndex);
				schedule[(i * 6) + 3] = ((byte)((ComboBox)(v.Children[4])).SelectedIndex);
				schedule[(i * 6) + 4] = ((byte)((ComboBox)(v.Children[5])).SelectedIndex);
				schedule[(i * 6) + 5] = ((byte)((ComboBox)(v.Children[6])).SelectedIndex);
			}
			return schedule;
		}

	}

}

