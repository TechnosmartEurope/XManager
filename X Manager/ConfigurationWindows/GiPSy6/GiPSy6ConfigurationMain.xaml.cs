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
using X_Manager.ConfigurationWindows;

namespace X_Manager.ConfigurationWindows
{
	/// <summary>
	/// Logica di interazione per GiPSy6ConfigurationMain.xaml
	/// </summary> 
	public partial class GiPSy6ConfigurationMain : ConfigurationWindow
	{
		int pagePointer = 0;
		BasicsConfiguration basicsConf;
		ScheduleConfiguration schedConf;
		GeofencigConfiguration geoConf;
		byte unitType;
		PageCopy[] pages;
		public GiPSy6ConfigurationMain(byte[] conf, byte unitType)
		{
			InitializeComponent();
			basicsConf = new BasicsConfiguration(ref conf);
			schedConf = new ScheduleConfiguration(ref conf);
			geoConf = new GeofencigConfiguration(ref conf);
			backB.IsEnabled = false;
			this.unitType = unitType;
			pages = new PageCopy[] { basicsConf, schedConf, geoConf };
			Gipsy6ConfigurationBrowser.Content = pages[0];
		}

		private void loaded(object sender, RoutedEventArgs e)
		{

		}

		private void backClick(object sender, RoutedEventArgs e)
		{
			PageCopy p;
			clearHistory();
			backB.IsEnabled = true;
			forthB.IsEnabled = true;

			p = pages[pagePointer];
			p.copyValues();
			
			pagePointer--;
			if (pagePointer == 0)
			{
				backB.IsEnabled = false;
			}
			Gipsy6ConfigurationBrowser.Content = pages[pagePointer];
		}

		private void forthClick(object sender, RoutedEventArgs e)
		{
			PageCopy p;
			clearHistory();
			backB.IsEnabled = true;
			forthB.IsEnabled = true;

			p = pages[pagePointer];
			p.copyValues();

			pagePointer++;
			if (pagePointer == (pages.Length - 1))
			{
				forthB.IsEnabled = false;
			}
			Gipsy6ConfigurationBrowser.Content = pages[pagePointer];
		}

		//private void backClick_old(object sender, RoutedEventArgs e)
		//{
		//	clearHistory();
		//	backB.IsEnabled = true;
		//	forthB.IsEnabled = true;
		//	pagePointer--;
		//	if (pagePointer == 2)
		//	{
		//		geoConf.copyValues();
		//		Gipsy6ConfigurationBrowser.Content = schedConf;
		//	}
		//	else if (pagePointer == 1)
		//	{
		//		schedConf.copyValues();
		//		Gipsy6ConfigurationBrowser.Content = basicsConf;
		//		backB.IsEnabled = false;
		//	}
		//}

		//private void forthClick_old(object sender, RoutedEventArgs e)
		//{
		//	clearHistory();
		//	backB.IsEnabled = true;
			
		//	forthB.IsEnabled = true;
		//	pagePointer++;
		//	if (pagePointer == 2)
		//	{
		//		basicsConf.copyValues();
		//		Gipsy6ConfigurationBrowser.Content = schedConf;
		//	}
		//	else if (pagePointer == 3)
		//	{
		//		schedConf.copyValues();
		//		Gipsy6ConfigurationBrowser.Content = geoConf;
		//		forthB.IsEnabled = false;
		//	}
		//}

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
			}
		}

		
	}
}
