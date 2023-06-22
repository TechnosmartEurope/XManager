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
	/// Logica di interazione per TrekRemoteIntervals.xaml
	/// </summary>
	
	public partial class TrekRemoteIntervals : UserControl
	{
		object parent;
		bool listen = true;
		public TrekRemoteIntervals(object p)
		{
			InitializeComponent();
			parent = p;
			numberOfIntervalsCB.SelectionChanged += nIntCh; 
		}

		public object Clone()
		{
			return MemberwiseClone();
		}

		private void nIntCh(object sender, RoutedEventArgs e)
		{
			listen = false;
			int nInt = numberOfIntervalsCB.SelectedIndex;
			if (nInt < 3)
			{
				I3B.Text = "25";
				I3A.Text = "25";
				I3.Visibility = Visibility.Hidden;
			}
			else
			{
				I3B.SelectedIndex = 24;
				I3A.SelectedIndex = 23;
				I3.Visibility = Visibility.Visible;
			}

			if (nInt < 2)
			{
				I2B.Text = "25";
				I2A.Text = "25";
				I2.Visibility = Visibility.Hidden;
			}
			else
			{
				I2B.SelectedIndex = 13;
				I2A.SelectedIndex = 12;
				I2.Visibility = Visibility.Visible;
			}

			if (nInt < 1)
			{
				I1B.Text = "25";
				I1A.Text = "25";
				I1.Visibility = Visibility.Hidden;
			}
			else
			{
				I1B.SelectedIndex = 1;
				I1A.SelectedIndex = 0;
				I1.Visibility = Visibility.Visible;
			}
			listen = true;
		}

		public void import(double[] schSettings)
		{
			listen = false;
			double nInt = schSettings[16];
			numberOfIntervalsCB.SelectedIndex = (int)nInt;
			listen = false;
			if (nInt < 3)
			{
				I3B.Text = "25";
				I3A.Text = "25";
				I3.Visibility = Visibility.Hidden;
			}
			else
			{
				I3B.SelectedIndex = (int)schSettings[22];
				I3A.SelectedIndex = (int)schSettings[21];
				I3.Visibility = Visibility.Visible;
			}

			if (nInt < 2)
			{
				I2B.Text = "25";
				I2A.Text = "25";
				I2.Visibility = Visibility.Hidden;
			}
			else
			{
				I2B.SelectedIndex = (int)schSettings[20];
				I2A.SelectedIndex = (int)schSettings[19];
				I2.Visibility = Visibility.Visible;
			}

			if (nInt < 1)
			{
				I1B.Text = "25";
				I1A.Text = "25";
				I1.Visibility = Visibility.Hidden;
			}
			else
			{
				I1B.SelectedIndex = (int)schSettings[18];
				I1A.SelectedIndex = (int)schSettings[17];
				I1.Visibility = Visibility.Visible;
			}
			listen = true;
		}

		private void I1Ach(object sender, RoutedEventArgs e)
		{
			if (!listen) return;
			int max = int.Parse(I1B.Text);
			int act = I1A.SelectedIndex;
			if (act >= max)
			{
				act = max - 1;
			}
			I1A.SelectedIndex = act;
		}

		private void I1Bch(object sender, RoutedEventArgs e)
		{
			if (!listen) return;

			int act = I1B.SelectedIndex;
			int max = int.Parse(I2A.Text);
			int min = int.Parse(I1A.Text);
			
			if (act >= max)
			{
				act = max - 1;
			}
			if (act <= min)
			{
				act = min + 1;
			}
			I1B.SelectedIndex = act;
		}

		private void I2Ach(object sender, RoutedEventArgs e)
		{
			if (!listen) return;

			int act = I2A.SelectedIndex;
			int max = int.Parse(I2B.Text);
			int min = int.Parse(I1B.Text);

			if (act >= max)
			{
				act = max - 1;
			}
			if (act <= min)
			{
				act = min + 1;
			}
			I2A.SelectedIndex = act;
		}

		private void I2Bch(object sender, RoutedEventArgs e)
		{
			if (!listen) return;

			int act = I2B.SelectedIndex;
			int max = int.Parse(I3A.Text);
			int min = int.Parse(I2A.Text);

			if (act >= max)
			{
				act = max - 1;
			}
			if (act <= min)
			{
				act = min + 1;
			}
			I2B.SelectedIndex = act;
		}

		private void I3Ach(object sender, RoutedEventArgs e)
		{
			if (!listen) return;

			int act = I3A.SelectedIndex;
			int max = int.Parse(I3B.Text);
			int min = int.Parse(I2B.Text);

			if (act >= max)
			{
				act = max - 1;
			}
			if (act <= min)
			{
				act = min + 1;
			}
			I3A.SelectedIndex = act;
		}

		private void I3Bch(object sender, RoutedEventArgs e)
		{
			if (!listen) return;

			int act = I3B.SelectedIndex;
			int min = int.Parse(I3A.Text);

			if (act <= min)
			{
				act = min + 1;
			}
			I3B.SelectedIndex = act;
		}

		public double[] export()
		{
			double[] setR = new double[7];
			setR[0] = numberOfIntervalsCB.SelectedIndex;
			setR[1] = double.Parse(I1A.Text);
			setR[2] = double.Parse(I1B.Text);
			setR[3] = double.Parse(I2A.Text);
			setR[4] = double.Parse(I2B.Text);
			setR[5] = double.Parse(I3A.Text);
			setR[6] = double.Parse(I3B.Text);

			return setR;
		}
	}
}
