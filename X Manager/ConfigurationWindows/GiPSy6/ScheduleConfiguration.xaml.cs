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
	/// Logica di interazione per ScheduleConfiguration.xaml
	/// </summary>
	public partial class ScheduleConfiguration : PageCopy
	{
		byte[] conf;
		public ScheduleConfiguration(ref byte[] conf)
		{
			InitializeComponent();
			this.conf = conf;
		}

		private void CheckBox_Checked(object sender, RoutedEventArgs e)
		{
			conf[0] = 0xbb;
		}

		public override void copyValues()
		{

		}
	}
}
