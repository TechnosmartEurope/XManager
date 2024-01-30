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

namespace X_Manager.Themes
{
	/// <summary>
	/// Logica di interazione per NumericUpDown.xaml
	/// </summary>
	public partial class NumericUpDown : UserControl
	{

		public event EventHandler ValueChanged;

		int _maxValue = Int32.MaxValue;
		int _minValue = Int32.MinValue;
		int _value = 0;
		public int MaxValue
		{
			get
			{
				return _maxValue;
			}
			set
			{
				if (value == Int32.MinValue) _minValue = Int32.MinValue;
				else if (value < _minValue) _minValue = value - 1;
				_maxValue = value;
				Value = _value;
			}
		}
		public int MinValue
		{
			get
			{
				return _minValue;
			}
			set
			{
				if (value == Int32.MaxValue) _maxValue = Int32.MaxValue;
				else if (value > _maxValue) _maxValue = value + 1;
				_minValue = value;
				Value = _value;
			}
		}
		public int Value
		{
			get
			{
				return _value;
			}
			set
			{
				int oldValue = _value;
				if (value < _minValue) _value = _minValue;
				else if (value > _maxValue) _value = _maxValue;
				else _value = value;
				valueTB.Text = _value.ToString();
				if (oldValue != _value)
				{
					if (ValueChanged != null)
					{
						ValueChanged(this, EventArgs.Empty);
					}
				}
			}
		}

		public NumericUpDown()
		{
			InitializeComponent();
			Loaded += NumericUpDown_Loaded;
			valueTB.Text = _value.ToString();
		}

		private void NumericUpDown_Loaded(object sender, RoutedEventArgs e)
		{

		}

		private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			var width = ((NumericUpDown)sender).ActualWidth;
			if ((width / 5) < 20) width = 20;
			else if ((width / 5) > 40) width = 40;
			else width /= 5;
			plusColumn.Width = new GridLength(width, GridUnitType.Pixel);
			minusColumn.Width = new GridLength(width, GridUnitType.Pixel);
			//test.Content = minusColumn.Width.ToString();

		}

		private void plusB_Click(object sender, RoutedEventArgs e)
		{
			Value++;
		}

		private void minusB_Click(object sender, RoutedEventArgs e)
		{
			Value--;
		}

		private void valueTB_KeyDown(object sender, KeyEventArgs e)
		{
			validateInput();
		}

		private void valueTB_LostFocus(object sender, RoutedEventArgs e)
		{
			validateInput();
		}

		private void validateInput()
		{
			int val = _value;
			Int32.TryParse(valueTB.Text, out val);
			Value = val;
		}

		private void UserControl_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			valueTB.IsEnabled = IsEnabled;
			plusB.IsEnabled = IsEnabled;
			minusB.IsEnabled = IsEnabled;

		}
	}
}
