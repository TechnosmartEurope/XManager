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

namespace X_Manager.ConfigurationWindows.GiPSy6
{
	/// <summary>
	/// Logica di interazione per GiPSy6ConfigurationMain.xaml
	/// </summary>
	public partial class GiPSy6ConfigurationMain : Window
	{
		int pagePointer = 1;
		BasicsConfiguration basicsConf = new BasicsConfiguration();
		ScheduleConfiguration schedConf = new ScheduleConfiguration();
		GeofencigConfiguration geoConf = new GeofencigConfiguration();
		public GiPSy6ConfigurationMain(byte[] conf)
		{
			InitializeComponent();
			//Gipsy6ConfigurationBrowser.Content = new BasicsConfiguration();
			Gipsy6ConfigurationBrowser.Content = basicsConf;
			backB.IsEnabled = false;
		}

		private void backClick(object sender, RoutedEventArgs e)
		{
			clearHistory();
			backB.IsEnabled = true;
			forthB.IsEnabled = true;
			pagePointer--;
			if (pagePointer == 2)
			{
				//Gipsy6ConfigurationBrowser.Content = new ScheduleConfiguration();
				Gipsy6ConfigurationBrowser.Content = schedConf;
			}
			else if (pagePointer == 1)
			{
				//Gipsy6ConfigurationBrowser.Content = new BasicsConfiguration();
				Gipsy6ConfigurationBrowser.Content = basicsConf;
				backB.IsEnabled = false;
			}
		}

		private void forthClick(object sender, RoutedEventArgs e)
		{
			clearHistory();
			backB.IsEnabled = true;
			forthB.IsEnabled = true;
			pagePointer++;
			if (pagePointer == 2)
			{
				//Gipsy6ConfigurationBrowser.Content = new ScheduleConfiguration();
				Gipsy6ConfigurationBrowser.Content = schedConf;
			}
			else if (pagePointer == 3)
			{
				//Gipsy6ConfigurationBrowser.Content = new GeofencigConfiguration();
				Gipsy6ConfigurationBrowser.Content = geoConf;
				forthB.IsEnabled = false;
			}
		}

		private void clearHistory()
		{
			try
			{

				var je = Gipsy6ConfigurationBrowser.NavigationService.RemoveBackEntry();
				while (je != null)
				{
					je = Gipsy6ConfigurationBrowser.NavigationService.RemoveBackEntry();
				}
			}
			catch
			{
				int b = 0;
			}
			//var entry = Gipsy6ConfigurationBrowser.RemoveBackEntry();
			//while (entry != null)
			//{
			//	entry = Gipsy6ConfigurationBrowser.RemoveBackEntry();
			//}
		}

		private void loaded(object sender, RoutedEventArgs e)
		{

		}
	}
}
