﻿using System;
using System.Windows;
using System.Windows.Controls;
using X_Manager.Units.AxyTreks;
using X_Manager.Themes;

namespace X_Manager.ConfigurationWindows
{
	/// <summary>
	/// Interaction logic for SettingsTab.xaml
	/// </summary>
	public partial class TrekSettingsTab : UserControl
	{
		object parent;
		public NumericUpDown batteryNud;
		Units.Unit unit;

		public TrekSettingsTab(object p)
		{
			InitializeComponent();

			parent = p;
			unit = ((TrekPositionConfigurationWindow)p).unit;

			adcRecording.IsChecked = true;

			dEnabled.IsChecked = false;
			dDisabled.IsChecked = true;

			RoutedEventArgs ev = new RoutedEventArgs();
			ev.RoutedEvent = System.Windows.Controls.Primitives.ToggleButton.CheckedEvent;

			((Grid)((GroupBox)((Grid)((Grid)Content).Children[2]).Children[0]).Content).RowDefinitions[1].Height = new GridLength(0);

			ADCValueUD.Value = 512;
			magMinB.Content = ">";

			SdDateTimePicker.SelectedDate = DateTime.Now;
			OffRB.IsChecked = true;
			ByTimeRB.IsChecked = false;
			ByDateRB.IsChecked = false;

			Loaded += loaded;

		}

		private void loaded(object sender, EventArgs e)
		{
			if (unit is AxyTrekR)
			{
				adcRadarGB.Header = "RADAR SETTINGS";
				adcRecording.Visibility = Visibility.Hidden;
				adcSP.Visibility = Visibility.Hidden;
				radarSP1.Visibility = Visibility.Visible;
				radarSP2.Visibility = Visibility.Visible;

			}
		}

		public double[] export()
		{
			double[] schSettings = new double[16];
			schSettings[0] = startDelayNud.Value;
			schSettings[1] = onTimeNud.Value;
			schSettings[2] = offTimeNud.Value;
			schSettings[3] = nSatNud.Value;
			schSettings[4] = acqSumNud.Value;
			schSettings[5] = acq1Nud.Value;
			schSettings[6] = acq2Nud.Value;
			schSettings[7] = 0;

			if ((bool)dEnabled.IsChecked) schSettings[7] = 1;

			if (unit is AxyTrekR)
			{
				schSettings[8] = radarOnTimeCB.SelectedIndex;
				schSettings[9] = radarPeriodCB.SelectedIndex;
				schSettings[10] = 0;
				schSettings[11] = 0;
			}
			else
			{
				schSettings[8] = 0;
				if ((bool)adcRecording.IsChecked) schSettings[8] = 1;

				schSettings[9] = 0;
				if ((bool)adcTrigger.IsChecked) schSettings[9] = 1;

				schSettings[10] = 0;
				if ((string)magMinB.Content == ">") schSettings[10] = 1;

				schSettings[11] = ADCValueUD.Value;
			}

			schSettings[12] = SdDateTimePicker.SelectedDate.Value.Day;
			schSettings[13] = SdDateTimePicker.SelectedDate.Value.Month;
			schSettings[14] = SdDateTimePicker.SelectedDate.Value.Year;

			schSettings[15] = 0;
			if ((bool)ByTimeRB.IsChecked) schSettings[15] = 1;
			if ((bool)ByDateRB.IsChecked) schSettings[15] = 2;

			return schSettings;
		}

		public void import(double[] schSettings)
		{
			startDelayNud.Value = schSettings[0];
			onTimeNud.Value = schSettings[1];
			offTimeNud.Value = schSettings[2];
			nSatNud.Value = schSettings[3];
			acqSumNud.Value = schSettings[4];
			acq1Nud.Value = schSettings[5];
			acq2Nud.Value = schSettings[6];
			dEnabled.IsChecked = false;
			dDisabled.IsChecked = true;

			if (schSettings[7] == 1)
			{
				dDisabled.IsChecked = false;
				dEnabled.IsChecked = true;
			}
			if (unit is AxyTrekR)
			{
				radarOnTimeCB.SelectedIndex = (int)schSettings[8];
				radarPeriodCB.SelectedIndex = (int)schSettings[9];
			}
			else
			{
				adcRecording.IsChecked = false;
				if (schSettings[8] == 1) adcRecording.IsChecked = true;
				adcTrigger.IsChecked = false;
				if (schSettings[9] == 1) adcTrigger.IsChecked = true;
				magMinB.Content = "<";
				if (schSettings[10] == 1) magMinB.Content = ">";
				ADCValueUD.Value = (UInt16)schSettings[11];
			}

			if (schSettings.Length > 12)
			{
				try
				{
					setSdDate(new double[] { schSettings[14], schSettings[13], schSettings[12] });
				}
				catch { }

				setSdRb(schSettings[15]);
			}
			else SdDateTimePicker.SelectedDate = DateTime.Now;
		}

		public void disableWaterControl()
		{
			((GroupBox)((Grid)((Grid)this.Content).Children[2]).Children[0]).Header = "WATER SWITCH";
			adcRecording.IsChecked = true;
			adcRecording.Content = "Handled by Accelerometer Configuration";
			adcRecording.FontSize = 11;
			adcRecording.IsEnabled = false;
		}

		public void switchToAdc()
		{
			((Grid)((GroupBox)((Grid)((Grid)this.Content).Children[2]).Children[0]).Content).RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
			((GroupBox)((Grid)((Grid)this.Content).Children[2]).Children[0]).Header = "ADC RECORDER / TRIGGER";
			adcRecording.Content = "Enable raw data recording";

		}

		private void magMinClick(object sender, RoutedEventArgs e)
		{
			if ((string)magMinB.Content == ">") magMinB.Content = "<";
			else magMinB.Content = ">";
		}

		public void setSdDate(double[] d)
		{
			DateTime dateD = new DateTime((int)d[0], (int)d[1], (int)d[2]);
			SdDateTimePicker.SelectedDate = dateD;
		}

		public void setSdRb(double d)
		{
			int n = (int)d;
			OffRB.IsChecked = false;
			ByTimeRB.IsChecked = false;
			ByDateRB.IsChecked = false;
			switch (n)
			{
				case 0:
					OffRB.IsChecked = true;
					break;
				case 1:
					ByTimeRB.IsChecked = true;
					break;
				case 2:
					ByDateRB.IsChecked = true;
					break;
			}
		}

		public double getSdRb()
		{
			if ((bool)ByTimeRB.IsChecked) return 1;
			else if ((bool)ByDateRB.IsChecked) return 2;
			else return 0;
		}

		private void offRbChecked(object sender, RoutedEventArgs e)
		{
			ByTimeRB.IsChecked = false;
			ByDateRB.IsChecked = false;

			if ((SdDateTimePicker.Visibility == Visibility.Hidden)) startDelayNud.Value = 0;
		}

		private void byTimeRbChecked(object sender, RoutedEventArgs e)
		{
			OffRB.IsChecked = false;
			ByDateRB.IsChecked = false;

		}

		private void byDateRbChecked(object sender, RoutedEventArgs e)
		{
			OffRB.IsChecked = false;
			ByTimeRB.IsChecked = false;

		}

	}
}
