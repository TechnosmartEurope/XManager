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

namespace X_Manager
{
	/// <summary>
	/// Logica di interazione per DownloadRangePrompt.xaml
	/// </summary>
	public partial class DownloadRangeInput : Window
	{
		uint startAddressProperty = 0;
		uint finalAddressProperty = 0;

		public uint startAddress
		{
			get
			{
				return startAddressProperty;
			}
			set
			{
				startAddressProperty = value;
				string format = "";
				if ((bool)startCB.IsChecked)
				{
					format = "X8";
				}
				startTB.Text = startAddressProperty.ToString(format);
			}
		}
		public uint finalAddress
		{
			get
			{
				return finalAddressProperty;
			}
			set
			{
				finalAddressProperty = value;
				string format = "";
				if ((bool)startCB.IsChecked)
				{
					format = "X8";
				}
				finalTB.Text = finalAddressProperty.ToString(format);
			}
		}

		public DownloadRangeInput()
		{
			InitializeComponent();
			boundaryRB.IsChecked = true;
			lastRB.IsChecked = false;
			startCB.IsChecked = true;
			finalCB.IsChecked = true;
			startCB.Checked += startCB_Checked;
			startCB.Unchecked += startCB_Checked;
			finalCB.Checked += finalCB_Checked;
			finalCB.Unchecked += finalCB_Checked;
			startAddress = 0;
			finalAddress = 0;
		}

		private void okB_Click(object sender, RoutedEventArgs e)
		{
			if (!(bool)boundaryRB.IsChecked)
			{
				finalAddressProperty += 0x1000;
			}
			if (startAddressProperty >= finalAddressProperty)
			{
				MessageBox.Show("Error: final address less or equal to start address.");
				return;
			}
			this.DialogResult = true;
			this.Close();
		}

		private void startTb_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				startTBValidate();
			}
		}

		private void FinalTB_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				finalTBValidate();
			}
		}

		private void StartTB_LostFocus(object sender, RoutedEventArgs e)
		{
			startTBValidate();
		}

		private void FinalTB_LostFocus(object sender, RoutedEventArgs e)
		{
			finalTBValidate();
		}

		private void startTBValidate()
		{
			var ns = new System.Globalization.NumberStyles();
			ns = System.Globalization.NumberStyles.Number;
			if ((bool)startCB.IsChecked)
			{
				ns = System.Globalization.NumberStyles.HexNumber;
			}

			uint newstartAddressProperty = startAddressProperty;
			if (uint.TryParse(startTB.Text, ns, System.Globalization.CultureInfo.InvariantCulture, out newstartAddressProperty))
			{
				if (newstartAddressProperty < 0)
				{
					MessageBox.Show("Please enter a positive value");
				}
				else
				{
					newstartAddressProperty = (newstartAddressProperty / 4096) * 4096;
					startAddressProperty = newstartAddressProperty;
				}
			}
			string format = "";
			if ((bool)startCB.IsChecked) format = "X8";
			startTB.Text = startAddressProperty.ToString(format);
		}

		private void finalTBValidate()
		{
			//(int.TryParse(logicalTB.Text, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out newAddress)
			var ns = new System.Globalization.NumberStyles();
			ns = System.Globalization.NumberStyles.Number;
			if ((bool)finalCB.IsChecked)
			{
				ns = System.Globalization.NumberStyles.HexNumber;
			}

			uint newfinalAddressProperty = finalAddressProperty;
			if (uint.TryParse(finalTB.Text, ns, System.Globalization.CultureInfo.InvariantCulture, out newfinalAddressProperty))
			{
				if (newfinalAddressProperty < 0)
				{
					MessageBox.Show("Please enter a positive value");
				}
				else
				{
					newfinalAddressProperty = (newfinalAddressProperty / 4096) * 4096;
					finalAddressProperty = newfinalAddressProperty;
				}
			}
			string format = "";
			if ((bool)startCB.IsChecked) format = "X8";
			finalTB.Text = finalAddressProperty.ToString(format);
		}

		private void startCB_Checked(object sender, RoutedEventArgs e)
		{
			if ((bool)startCB.IsChecked)
			{
				startTB.Text = (int.Parse(startTB.Text).ToString("X8"));
			}
			else
			{
				startTB.Text = (int.Parse(startTB.Text, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture).ToString());
			}
		}

		private void finalCB_Checked(object sender, RoutedEventArgs e)
		{
			if ((bool)finalCB.IsChecked)
			{
				finalTB.Text = (int.Parse(finalTB.Text).ToString("X8"));
			}
			else
			{
				finalTB.Text = (int.Parse(finalTB.Text, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture).ToString());
			}
		}
	}
}
