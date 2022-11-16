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
using System.Runtime.CompilerServices;
using X_Manager.Units;
using X_Manager.Units.AxyTreks;

namespace X_Manager.ConfigurationWindows
{
	/// <summary>
	/// Logica di interazione per QuattrokPressureCalibration.xaml
	/// </summary>
	public partial class DepthFastTrekHDFTCalibration : Window
	{
		int[] coeffs;
		public double pressZero, pressSpan, pressThreshold, tempZero, tempSpan, pressTcoeff;
		//public double t1, t2, t3, t4, t5, t6, t7, p1, p2, p3, p4, p5, p6, p7, p8;
		public double[] tempOut = new double[8];
		public double[] pressOut = new double[8];
		public bool mustWrite = false;
		Unit unit;
		NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;
		public DepthFastTrekHDFTCalibration(int[] coeffs, Unit unit)
		{
			InitializeComponent();
			this.coeffs = coeffs;
			Loaded += loaded;
			this.unit = unit;
		}

		private void loaded(object sender, RoutedEventArgs e)
		{

			InputValuesB.IsEnabled = false;
			InputValuesB.Visibility = Visibility.Hidden;
			NeutralValuesB.IsEnabled = false;
			NeutralValuesB.Visibility = Visibility.Hidden;

			if (unit is AxyTrekFT || unit.modelCode == Unit.model_axyDepthFast)
			{
				tempSpan = coeffs[0] * 256 + coeffs[1];
				tempZero = coeffs[2] * 256 + coeffs[3];
				//span temperatura: da 0 a 65,535
				tempSpan /= 1000;
				//zero temperatura: da -32,5 a 32,5
				tempZero -= 32500;
				tempZero /= 1000;

				foreach (FrameworkElement fe in mainGrid.Children)
				{
					if (Grid.GetColumn(fe) == 1)
					{
						fe.Visibility = Visibility.Hidden;
					}
					mainGrid.ColumnDefinitions[1].Width = new GridLength(0);
					InputValuesB.IsEnabled = true;
					InputValuesB.Visibility = Visibility.Visible;
					NeutralValuesB.IsEnabled = true;
					NeutralValuesB.Visibility = Visibility.Visible;

					tempZeroTB.Text = tempZero.ToString();
					tempSpanTB.Text = tempSpan.ToString();

					NeutralValuesB.HorizontalAlignment = HorizontalAlignment.Center;
					InputValuesB.HorizontalAlignment = HorizontalAlignment.Center;
					sendB.HorizontalAlignment = HorizontalAlignment.Center;

				}

			}
			else if (unit.firmTotA < 3009001)
			{
				pressZero = coeffs[0] * 256 + coeffs[1];
				pressSpan = coeffs[2] * 256 + coeffs[3];

				pressZero -= 32500;
				pressSpan /= 1000;

				pressZeroTB.Text = pressZero.ToString();
				pressSpanTB.Text = pressSpan.ToString();

				if (unit.firmTotA < 3009000)
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
					tempZero /= 1000;
					tempSpan /= 1000;

					tempZeroTB.Text = tempZero.ToString();
					tempSpanTB.Text = tempSpan.ToString();
				}
				genThreshold();
			}
			else
			{
				tempSpan = coeffs[0] * 256 + coeffs[1];
				tempZero = coeffs[2] * 256 + coeffs[3];
				pressSpan = coeffs[4] * 256 + coeffs[5];
				pressZero = coeffs[6] * 256 + coeffs[7];
				pressTcoeff = coeffs[8] * 256 + coeffs[9];

				//span temperatura: da 0 a 65,535
				tempSpan /= 1000;
				//zero temperatura: da -32,5 a 32,5
				tempZero -= 32500;
				tempZero /= 1000;
				//span pressione: da 0 a 655,35
				pressSpan /= 100;
				//zero pressione: da -32500 a 32500
				pressZero -= 32500;
				//Tcoeff pressione:	da -325,00 a +325,00 
				pressTcoeff -= 32500;
				pressTcoeff /= 100;

				pressTcoeffTB.IsEnabled = true;
				InputValuesB.IsEnabled = true;
				InputValuesB.Visibility = Visibility.Visible;
				NeutralValuesB.IsEnabled = true;
				NeutralValuesB.Visibility = Visibility.Visible;

				tempZeroTB.Text = tempZero.ToString();
				tempSpanTB.Text = tempSpan.ToString();

				pressZeroTB.Text = pressZero.ToString();
				pressSpanTB.Text = pressSpan.ToString();
				pressTcoeffTB.Text = pressTcoeff.ToString();
			}
		}

		private void closing(object sender, System.ComponentModel.CancelEventArgs e)
		{

		}

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
			genThreshold();
		}

		private void Button_Click_1(object sender, RoutedEventArgs e)
		{

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
			genThreshold();
		}
		#endregion

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
			if (!double.TryParse(pressSpanTB.Text, NumberStyles.Any, nfi, out span))
			{
				MessageBox.Show("Wrong value.");
				pressSpanTB.Text = pressSpan.ToString();
				return;
			}

			pressSpan = span;
			genThreshold();
		}

		#endregion

		#region gestione_Tcoeff_pressione
		private void pressTcoeffTB_LostFocus(object sender, RoutedEventArgs e)
		{
			validatePressTcoeff();
		}

		private void pressTcoeffTB_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				validatePressTcoeff();
			}
		}

		private void validatePressTcoeff()
		{
			double span;

			pressTcoeffTB.Text = pressTcoeffTB.Text.Replace(',', '.');
			if (!double.TryParse(pressTcoeffTB.Text, NumberStyles.Any, nfi, out span))// | double.Parse(spanTB.Text) > 50)
			{
				MessageBox.Show("Wrong value.");
				pressTcoeffTB.Text = pressTcoeff.ToString();
				return;
			}

			pressTcoeff = span;
			genThreshold();
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
			var temps = new double[] { -15, -10, 0, 10, 20, 30, 40 };
			var tempC = new double[] { -30, -12, -5, 5, 15, 25, 35, 50 };
			for (int i = 0; i < 7; i++)
			{
				double temp = (temps[i] - tempZero) / tempSpan;
				temp *= .00381;
				temp++;
				temp *= 1.4957;
				temp -= 0.9943;
				temp *= 65536;
				temp /= 2.048;
				tempOut[i] = temp;
			}
			tempOut[7] = 0xabcd;

			for (int i = 0; i < 8; i++)
			{
				pressOut[i] = 1520.0 + pressZero + (tempC[i] * pressTcoeff) - 890;
				pressOut[i] = pressOut[i] * pressSpan / 100000;
				pressOut[i] *= 55.0;
				pressOut[i] += 200.0;
				pressOut[i] /= 1000.0;
				pressOut[i] *= 32768.0;
				pressOut[i] /= 2.048;
			}

			mustWrite = true;
			Close();
		}

		private void NeutralValues_Click(object sender, RoutedEventArgs e)
		{
			tempSpan = 1;
			tempZero = 0;
			pressZero = 0;
			pressTcoeff = 0;

			tempSpanTB.Text = tempSpan.ToString();
			tempZeroTB.Text = tempZero.ToString();
			pressZeroTB.Text = pressZero.ToString();
			pressTcoeffTB.Text = pressTcoeff.ToString();

		}

		private void Values_Click(object sender, RoutedEventArgs e)
		{
			var iv = new DepthFastTrekHDFTCalibrationInputValues(unit);
			iv.ShowDialog();
			if (iv.calculate)
			{
				tempSpan = Math.Round((iv.rt2 - iv.rt1) / (iv.t2 - iv.t1), 2);
				tempZero = Math.Round(iv.rt1 - tempSpan * iv.t1, 2);

				tempSpanTB.Text = tempSpan.ToString();
				tempZeroTB.Text = tempZero.ToString();

				if (!(unit is AxyTrekHD)) return;

				pressTcoeff = Math.Round((iv.p1 - iv.p2) / (iv.rt1 - iv.rt2), 2);
				pressZero = Math.Round(iv.p1 - 1016 - (pressTcoeff * iv.rt1), 2);

				pressZeroTB.Text = pressZero.ToString();
				pressTcoeffTB.Text = pressTcoeff.ToString();

				genThreshold();
			}
		}
	}
}
