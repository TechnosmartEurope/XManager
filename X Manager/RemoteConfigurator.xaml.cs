﻿using System;
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
using System.IO.Ports;
using X_Manager.Units;

namespace X_Manager
{
	/// <summary>
	/// Logica di interazione per RemoteConfigurator.xaml
	/// </summary>
	public partial class RemoteConfigurator : UserControl
	{
		RemoteManagement rm;
		SerialPort sp;

		int physycalAddress;
		int logicalAddress;
		Unit u;

		public RemoteConfigurator(ref SerialPort sp, object rm)
		{
			InitializeComponent();
			this.sp = sp;
			this.rm = (RemoteManagement)rm;
			physicalTB.IsEnabled = false;
			logicalTB.IsEnabled = false;
			//physicalTB.Text = logicalTB.Text = "Not connected.";

		}

		private bool checkConnectedUnit()
		{
			u = rm.getUnit();
			bool res = false;
			if (u == null)
			{
				physicalTB.IsEnabled = false;
				physicalTB.TextChanged -= PhysicalTB_TextChanged;
				logicalTB.IsEnabled = false;
				logicalTB.TextChanged -= LogicalTB_TextChanged;
				rm.UI_disconnected(1);
				logicalTB.Text = "Not connected.";
				physicalTB.Text = "Not connected.";
			}
			else
			{
				physicalTB.IsEnabled = true;
				physicalTB.TextChanged -= PhysicalTB_TextChanged;
				physicalTB.TextChanged += PhysicalTB_TextChanged;
				logicalTB.IsEnabled = true;
				logicalTB.TextChanged -= LogicalTB_TextChanged;
				logicalTB.TextChanged += LogicalTB_TextChanged;
				res = true;
			}
			return res;
		}

		public void read()
		{
			if (!checkConnectedUnit()) return;
			if (!sp.IsOpen) sp.Open();
			sp.Write("TTTTTTTTGGAA");
			sp.ReadTimeout = 1000;
			try
			{
				sp.ReadLine();
				physycalAddress = sp.ReadByte();
				logicalAddress = (sp.ReadByte() * 65536) + (sp.ReadByte() * 256) + sp.ReadByte();
				string phMod = null;
				string loMod = null;
				if ((bool)phCB.IsChecked) phMod = "x";
				if ((bool)loCB.IsChecked) loMod = "x";
				physicalTB.Text = physycalAddress.ToString(phMod);
				logicalTB.Text = logicalAddress.ToString(loMod);
			}
			catch
			{
				rm.connect();   //Disconnette l'unità
				read();
			}
		}

		private void write()
		{
			if (!checkConnectedUnit()) return;
			if (!sp.IsOpen) sp.Open();
			sp.Write("TTTTTTTTGGAa");
			sp.ReadTimeout = 1000;
			try
			{
				sp.ReadLine();
				sp.Write(new byte[] { (byte)physycalAddress }, 0, 1);
				sp.Write(new byte[] { (byte)((logicalAddress >> 16) & 0xff) }, 0, 1);
				sp.Write(new byte[] { (byte)((logicalAddress >> 8) & 0xff) }, 0, 1);
				sp.Write(new byte[] { (byte)(logicalAddress & 0xff) }, 0, 1);
				sp.ReadLine();
			}
			catch
			{
				rm.connect();   //Disconnette l'unità
				read();
			}
		}

		private void PhysicalTB_TextChanged(object sender, TextChangedEventArgs e)
		{
			int newAddress;
			physicalTB.Text = physicalTB.Text.ToLower();
			if (!(bool)phCB.IsChecked)
			{
				if (int.TryParse(physicalTB.Text, out newAddress))
				{
					if ((newAddress > 255) | (newAddress < 1))
					{
						physicalTB.Text = physycalAddress.ToString();
						return;
					}
					physycalAddress = newAddress;
				}
				else
				{
					physicalTB.Text = physycalAddress.ToString();
				}
			}
			else
			{
				if (int.TryParse(physicalTB.Text, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out newAddress))
				{
					if ((newAddress > 255) | (newAddress < 1))
					{
						physicalTB.Text = physycalAddress.ToString("x");
						return;
					}
					physycalAddress = newAddress;
				}
				else
				{
					physicalTB.Text = physycalAddress.ToString("x");
				}
			}
		}

		private void LogicalTB_TextChanged(object sender, TextChangedEventArgs e)
		{
			int newAddress;
			logicalTB.Text = logicalTB.Text.ToLower();
			if (!(bool)loCB.IsChecked)
			{
				if (int.TryParse(logicalTB.Text, out newAddress))
				{
					if ((newAddress < 1) | (newAddress > 16700000))
					{
						logicalTB.Text = logicalAddress.ToString();
						return;
					}
					logicalAddress = newAddress;
					int suggestedPhAddress = 0xff;
					if (newAddress < 0xffffff)
					{
						suggestedPhAddress = 2;
					}
					if ((bool)phCB.IsChecked)
					{
						physicalTB.Text = suggestedPhAddress.ToString("x");
					}
					else
					{
						physicalTB.Text = suggestedPhAddress.ToString();
					}
				}
				else
				{
					logicalTB.Text = logicalAddress.ToString();
				}
			}
			else
			{
				if (int.TryParse(logicalTB.Text, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out newAddress))
				{
					if ((newAddress < 1) | (newAddress > 0xffffff))
					{
						logicalTB.Text = logicalAddress.ToString("x");
						return;
					}
					logicalAddress = newAddress;
					int suggestedPhAddress = 0xff;
					if (newAddress < 0xffffff)
					{
						suggestedPhAddress = 2;
					}
					if ((bool)phCB.IsChecked)
					{
						physicalTB.Text = suggestedPhAddress.ToString("x");
					}
					else
					{
						physicalTB.Text = suggestedPhAddress.ToString();
					}
				}
				else
				{
					logicalTB.Text = logicalAddress.ToString("x");
				}

			}
		}

		private void ReadB_Click(object sender, RoutedEventArgs e)
		{
			read();
		}

		private void SendB_Click(object sender, RoutedEventArgs e)
		{
			write();
		}

		private void PhCB_Checked(object sender, RoutedEventArgs e)
		{
			int newAddress = 0;
			if ((bool)phCB.IsChecked)
			{
				if (!int.TryParse(physicalTB.Text, out newAddress)) return;
				physicalTB.Text = physycalAddress.ToString("x");
			}
			else
			{
				if (!int.TryParse(physicalTB.Text, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out newAddress)) return;
				physicalTB.Text = physycalAddress.ToString();
			}
		}

		private void LoCB_Checked(object sender, RoutedEventArgs e)
		{
			int newAddress = 0;
			if ((bool)loCB.IsChecked)
			{
				if (!int.TryParse(logicalTB.Text, out newAddress)) return;
				logicalTB.Text = logicalAddress.ToString("x");
			}
			else
			{
				if (!int.TryParse(physicalTB.Text, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out newAddress)) return;
				logicalTB.Text = logicalAddress.ToString();
			}
		}
	}
}
