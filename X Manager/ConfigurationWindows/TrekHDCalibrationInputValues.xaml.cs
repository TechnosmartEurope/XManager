using System;
using System.Collections.Generic;
using System.Globalization;
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

namespace X_Manager.ConfigurationWindows
{
	/// <summary>
	/// Logica di interazione per TrekHDCalibrationInputValues.xaml
	/// </summary>
	public partial class TrekHDCalibrationInputValues : Window
	{

		public double rt1, rt2, t1, t2, p1, p2;

		private void rt1TB_GotFocus(object sender, RoutedEventArgs e)
		{
			var tb = (TextBox)sender;
			tb.Select(0, tb.Text.Length);
		}

		public bool calculate = false;
		private void Button_Click(object sender, RoutedEventArgs e)
		{
			NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;
			var tbar = new TextBox[] { rt1TB, rt2TB, t1TB, t2TB, p1TB, p2TB };
			for (int i = 0; i < 6; i++)
			{
				TextBox tb = tbar[i];
				tb.Text = tb.Text.Replace(',', '.');
			}

			rt1 = double.Parse(rt1TB.Text, NumberStyles.Any, nfi);
			rt2 = double.Parse(rt2TB.Text, NumberStyles.Any, nfi);
			t1 = double.Parse(t1TB.Text, NumberStyles.Any, nfi);
			t2 = double.Parse(t2TB.Text, NumberStyles.Any, nfi);
			p1 = double.Parse(p1TB.Text, NumberStyles.Any, nfi);
			p2 = double.Parse(p2TB.Text, NumberStyles.Any, nfi);
			calculate = true;
			Close();
		}

		public TrekHDCalibrationInputValues()
		{
			InitializeComponent();
		}
	}
}
