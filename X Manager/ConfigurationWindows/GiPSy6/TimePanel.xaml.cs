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
	/// Logica di interazione per TimePanel.xaml
	/// </summary>

	public partial class TimePanel : UserControl
	{
		int timeP = 23;
		public string sch1;
		public string sch2;
		public int sel
		{
			get
			{
				return settingsCB.SelectedIndex;
			}
			set
			{
				settingsCB.SelectedIndex = value;
			}
		}

		public int time
		{
			get
			{
				return int.Parse(timeTB.Text);
			}
			set
			{
				timeTB.Text = value.ToString("00") + "-" + (value + 1).ToString("00");
				timeP = value;
			}
		}

		public bool isChecked
		{
			get
			{
				return (bool)enabledCB.IsChecked;
			}
			set
			{
				enabledCB.IsChecked = value;
			}
		}

		private EventHandler onUnChecked;

		public event EventHandler unChecked
		{
			add
			{
				onUnChecked += value;
			}
			remove
			{
				onUnChecked -= value;
			}
		}

		public TimePanel(string sch1, string sch2)
		{
			InitializeComponent();
			settingsCB.ItemsSource = new string[] { sch1 };
			settingsCB.SelectedIndex = 0;
			enabledCB.Checked += enabledCB_Checked;
			enabledCB.Unchecked += enabledCB_Checked;
			this.sch1 = sch1;
			this.sch2 = sch2;
		}

		private void enabledCB_Checked(object sender, RoutedEventArgs e)
		{
			if (((CheckBox)sender).IsChecked == true)
			{
				settingsCB.Visibility = Visibility.Visible;
			}
			else
			{
				settingsCB.Visibility = Visibility.Hidden;
				EventArgs ee = new EventArgs();
				if (onUnChecked != null)
				{
					onUnChecked.Invoke(this, ee);
				}
			}
		}

		public void setA()
		{
			//settingsCB.Items.Clear();
			settingsCB.ItemsSource = new string[] { sch1 };
			settingsCB.SelectedIndex = 0;
		}

		public void setAB()
		{
			if (settingsCB.Items.Count == 1)
			{
				//settingsCB.Items.Add("B");
				settingsCB.ItemsSource = new string[] { sch1, sch2 };
				settingsCB.SelectedIndex = 0;
			}
		}
	}
}
