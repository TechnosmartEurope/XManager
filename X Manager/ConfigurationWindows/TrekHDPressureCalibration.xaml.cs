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
	public partial class TrekHDPressureCalibration : Window
	{
		int[] coeffs;
		uint firmTotA;
		public double pressZero, pressSpan, pressThreshold, tempZero, tempSpan;
		public bool mustWrite = false;
		NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;
		public TrekHDPressureCalibration(int[] coeffs, uint firmIn)
		{
			InitializeComponent();
			this.coeffs = coeffs;
			Loaded += loaded;
			firmTotA = firmIn;
		}

		private void loaded(object sender, RoutedEventArgs e)
		{
			pressZero = coeffs[0] * 256 + coeffs[1];
			pressSpan = coeffs[2] * 256 + coeffs[3];

			pressZero -= 32500;
			pressSpan /= 1000;

			pressZeroTB.Text = pressZero.ToString();
			pressSpanTB.Text = pressSpan.ToString();

			genThreshold();
			//double m = span / 100;  //Coefficiente retta (passa per zero per ora)
			//threshold = m * 1.5;    //Calcolo uscita tensione dal sensore a 5m profondità

			//thresholdTB.Text = Math.Round(threshold, 2).ToString();



			if (firmTotA < 3009000)
			{
				foreach (FrameworkElement fe in mainGrid.Children)
				{
					if (Grid.GetColumn(fe) == 2)
					{
						fe.Visibility = Visibility.Hidden;
					}
				}
				mainGrid.ColumnDefinitions[2].Width = new GridLength(10);
			}
			else
			{
				tempZero = coeffs[6] * 256 + coeffs[7];
				tempSpan = coeffs[8] * 256 + coeffs[9];

				tempZero -= 32500;
				tempSpan /= 1000;

				tempZeroTB.Text = tempZero.ToString();
				tempSpanTB.Text = tempSpan.ToString();
			}


		}

		private void closing(object sender, System.ComponentModel.CancelEventArgs e)
		{

		}

		#region gestione_zero_pressione
		private void pressZeroTB_LostFocus(object sender, RoutedEventArgs e)
		{
			validatePressZero();
		}

		private void pressZeroTB_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				validatePressZero();
			}
		}

		private void validatePressZero()
		{
			double zero;

			pressZeroTB.Text = pressZeroTB.Text.Replace(',', '.');
			if (!double.TryParse(pressZeroTB.Text, NumberStyles.Any, nfi, out zero))// | double.Parse(zeroTB.Text, nfi) > 50)
			{
				MessageBox.Show("Wrong value.");
				pressZeroTB.Text = pressZero.ToString();
				return;
			}
			pressZero = zero;
			genThreshold();
			//zero += 1000;
			//zero *= 1000;

			//double m = span / 100;  //Coefficiente retta (passa per zero per ora)
			//threshold = m * 1.5;    //Calcolo uscita tensione dal sensore a 5m profondità

			//thresholdTB.Text = Math.Round(threshold, 2).ToString();
		}

		#endregion

		#region gestione_span_pressione

		private void pressSpanTB_LostFocus(object sender, RoutedEventArgs e)
		{
			validatePressSpan();
		}

		private void pressSpanTB_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				validatePressSpan();
			}
		}

		private void validatePressSpan()
		{
			double span;

			pressSpanTB.Text = pressSpanTB.Text.Replace(',', '.');
			if (!double.TryParse(pressSpanTB.Text, NumberStyles.Any, nfi, out span))// | double.Parse(spanTB.Text) > 50)
			{
				MessageBox.Show("Wrong value.");
				pressSpanTB.Text = pressSpan.ToString();
				return;
			}

			pressSpan = span;
			genThreshold();
			//span *= 1000;

			//threshold = m * 1.5;    //Calcolo uscita tensione dal sensore a 5m profondità

			//thresholdTB.Text = Math.Round(threshold, 2).ToString();
		}

		#endregion

		#region gestione_zero_temperatura
		private void tempZeroTB_LostFocus(object sender, RoutedEventArgs e)
		{
			validateTempZero();
		}

		private void tempZeroTB_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				validateTempZero();
			}
		}

		private void validateTempZero()
		{
			double zero;

			tempZeroTB.Text = tempZeroTB.Text.Replace(',', '.');
			if (!double.TryParse(tempZeroTB.Text, NumberStyles.Any, nfi, out zero))// | double.Parse(zeroTB.Text, nfi) > 50)
			{
				MessageBox.Show("Wrong value.");
				tempZeroTB.Text = tempZero.ToString();
				return;
			}
			tempZero = zero;
		}
		#endregion

		#region gestione_span_temperatura
		private void tempSpanTB_LostFocus(object sender, RoutedEventArgs e)
		{
			validateTempSpan();
		}

		private void tempSpanTB_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				validateTempSpan();
			}
		}

		private void validateTempSpan()
		{
			double span;

			tempSpanTB.Text = tempSpanTB.Text.Replace(',', '.');
			if (!double.TryParse(tempSpanTB.Text, NumberStyles.Any, nfi, out span))// | double.Parse(zeroTB.Text, nfi) > 50)
			{
				MessageBox.Show("Wrong value.");
				tempSpanTB.Text = tempSpan.ToString();
				return;
			}
			tempSpan = span;
		}
		#endregion

		private void genThreshold()
		{
			double threshold = ((1500 - pressZero) * pressSpan / 100000);// - zero;
			threshold = Math.Round(threshold, 2);
			pressThresholdTB.Text = threshold.ToString();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			mustWrite = true;
			Close();
		}
	}
}
