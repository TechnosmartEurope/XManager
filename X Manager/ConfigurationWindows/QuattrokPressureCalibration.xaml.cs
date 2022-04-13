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
using System.Globalization;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace X_Manager.ConfigurationWindows
{
	/// <summary>
	/// Logica di interazione per QuattrokPressureCalibration.xaml
	/// </summary>
	public partial class QuattrokPressureCalibration : Window
	{
		int[] coeffs;
		public double zero;
		public double span;
		public double threshold;
		public bool mustWrite = false;
		NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;
		public QuattrokPressureCalibration(int[] coeffs)
		{
			InitializeComponent();
			this.coeffs = coeffs;
			Loaded += loaded;
		}

		private void loaded(object sender, RoutedEventArgs e)
		{
			zero = coeffs[0] * 256 + coeffs[1];
			span = coeffs[2] * 256 + coeffs[3];
			zero /= 100;
			span /= 100;
			zeroTB.Text = zero.ToString();
			spanTB.Text = span.ToString();

			double m = span / 100;  //Coefficiente retta (passa per zero per ora)
			threshold = m * 1.5;    //Calcolo uscita tensione dal sensore a 5m profondità

			thresholdTB.Text = Math.Round(threshold, 2).ToString();

		}

		private void closing(object sender, System.ComponentModel.CancelEventArgs e)
		{

		}

		private void zeroTB_LostFocus(object sender, RoutedEventArgs e)
		{
			validateZero();
		}

		private void zeroTB_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				validateZero();
			}
		}

		private void validateZero()
		{
			double zero;

			zeroTB.Text = zeroTB.Text.Replace(',', '.');
			if (!double.TryParse(zeroTB.Text, NumberStyles.Any, nfi, out zero))// | double.Parse(zeroTB.Text, nfi) > 50)
			{
				MessageBox.Show("Wrong value.");
				zeroTB.Text = this.zero.ToString();
				return;
			}
			this.zero = zero;
			double m = span / 100;  //Coefficiente retta (passa per zero per ora)
			threshold = m * 1.5;    //Calcolo uscita tensione dal sensore a 5m profondità

			thresholdTB.Text = Math.Round(threshold, 2).ToString();
		}

		private void spanTB_LostFocus(object sender, RoutedEventArgs e)
		{
			validateSpan();
		}

		private void spanTB_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				validateSpan();
			}
		}

		private void validateSpan()
		{
			double span;

			spanTB.Text = spanTB.Text.Replace(',', '.');
			if (!double.TryParse(spanTB.Text, NumberStyles.Any, nfi, out span))// | double.Parse(spanTB.Text) > 50)
			{
				MessageBox.Show("Wrong value.");
				spanTB.Text = this.span.ToString();
				return;
			}
			this.span = span;
			double m = span / 100;  //Coefficiente retta (passa per zero per ora)
			threshold = m * 1.5;    //Calcolo uscita tensione dal sensore a 5m profondità

			thresholdTB.Text = Math.Round(threshold, 2).ToString();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			mustWrite = true;
			this.Close();
		}
	}
}
