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
	public partial class ScheduleConfiguration : PageCopy
	{
		byte[] conf;
		//TextBlockQ[] tbArr = new TextBlockQ[24];
		TimePanel[] timePanelArAB = new TimePanel[24];
		TimePanel[] timePanelArCD = new TimePanel[24];
		uint[] sch = new uint[4];
		TextBox[] tbArr;
		ComboBox[] cbArr;
		CheckBox[] ckArr;
		int[] oldCbVal = new int[4];
		public ScheduleConfiguration(byte[] conf)
		{
			InitializeComponent();
			this.conf = conf;
			int riga = 0;
			int colonna = 0;
			tbArr = new TextBox[] { aValTB, bValTB, cValTB, dValTB };
			cbArr = new ComboBox[4] { aTimeUnitCB, bTimeUnitCB, cTimeUnitCB, dTimeUnitCB };
			ckArr = new CheckBox[12] { m1, m2, m3, m4, m5, m6, m7, m8, m9, m10, m11, m12 };

			for (int i = 0; i < 24; i++)
			{
				//StackPanelQ sp = new StackPanelQ();
				TimePanel timePanelAB = new TimePanel("A", "B");
				timePanelAB.time = i;
				Grid.SetColumn(timePanelAB, colonna);
				Grid.SetRow(timePanelAB, riga);

				TimePanel timePanelCD = new TimePanel("C", "D");
				timePanelCD.time = i;
				Grid.SetColumn(timePanelCD, colonna);
				Grid.SetRow(timePanelCD, riga);

				primaryScheduleGrid.Children.Add(timePanelAB);
				timePanelArAB[i] = timePanelAB;
				secondaryScheduleGrid.Children.Add(timePanelCD);
				timePanelArCD[i] = timePanelCD;

				riga++;
				if (riga == 12)
				{
					riga = 0;
					colonna++;
				}

			}

			sch[0] = BitConverter.ToUInt32(conf, 52);	//52-55
			sch[1] = BitConverter.ToUInt32(conf, 56);	//56-59
			sch[2] = BitConverter.ToUInt32(conf, 84);	//84-87
			sch[3] = BitConverter.ToUInt32(conf, 88);	//88-91
		}

		private void loaded(object sender, RoutedEventArgs e)
		{
			bool bChecked = false;
			bool dChecked = false;

			//Caselle orari A/B e C/D
			for (int i = 0; i < 24; i++)
			{
				if (conf[60 + i] == 0)
				{
					timePanelArAB[i].isChecked = false;
				}
				else if (conf[60 + i] == 2)
				{
					bChecked = true;
					timePanelArAB[i].setAB();
					timePanelArAB[i].sel = 1;
				}
				if (conf[92 + i] == 0)
				{
					timePanelArCD[i].isChecked = false;
				}
				else if (conf[92 + i] == 2)
				{
					dChecked = true;
					timePanelArCD[i].setAB();
					timePanelArCD[i].sel = 1;
				}
			}
			if (bChecked)
			{
				scheduleBCB.IsChecked = true;
				for (int i = 0; i < 24; i++)
				{
					timePanelArAB[i].setAB();
				}
			}
			if (dChecked)
			{
				scheduleDCB.IsChecked = true;
				for (int i = 0; i < 24; i++)
				{
					timePanelArCD[i].setAB();
				}
			}

			//Secondi Schedule ABCD
			for (int i = 0; i < 4; i++)
			{
				if ((sch[i] % 3600) == 0)
				{
					sch[i] /= 3600;
					cbArr[i].SelectedIndex = 2;
				}
				else if ((sch[i] % 60) == 0)
				{
					sch[i] /= 60;
					cbArr[i].SelectedIndex = 1;
				}
				else
				{
					cbArr[i].SelectedIndex = 0;
				}
				oldCbVal[i] = cbArr[i].SelectedIndex;
				tbArr[i].Text = sch[i].ToString();
				tbArr[i].LostFocus += validate;
				tbArr[i].KeyDown += validate;
				cbArr[i].SelectionChanged += cbSelChanged;
			}

			//Mesi C/D
			for (int i = 0; i < 12; i++)
			{
				if (conf[i + 116] == 1)
				{
					ckArr[i].IsChecked = true;
				}
			}
		}

		public override void copyValues()
		{
			for (int i = 0; i < 4; i++)
			{
				sch[i] *= (uint)Math.Pow(60, cbArr[i].SelectedIndex);
			}
			conf[55] = (byte)(sch[0] >> 24);
			conf[54] = (byte)(sch[0] >> 16);
			conf[53] = (byte)(sch[0] >> 8);
			conf[52] = (byte)(sch[0] & 0xff);

			conf[59] = (byte)(sch[1] >> 24);
			conf[58] = (byte)(sch[1] >> 16);
			conf[57] = (byte)(sch[1] >> 8);
			conf[56] = (byte)(sch[1] & 0xff);

			conf[87] = (byte)(sch[2] >> 24);
			conf[86] = (byte)(sch[2] >> 16);
			conf[85] = (byte)(sch[2] >> 8);
			conf[84] = (byte)(sch[2] & 0xff);

			conf[91] = (byte)(sch[3] >> 24);
			conf[90] = (byte)(sch[3] >> 16);
			conf[89] = (byte)(sch[3] >> 8);
			conf[88] = (byte)(sch[3] & 0xff);

			for (int i = 0; i < 24; i++)
			{
				conf[i + 60] = 0;
				if (timePanelArAB[i].isChecked)
				{
					conf[i + 60] = (byte)(timePanelArAB[i].sel + 1);
				}

				conf[i + 92] = 0;
				if (timePanelArCD[i].isChecked)
				{
					conf[i + 92] = (byte)(timePanelArCD[i].sel + 1);
				}
			}

			for (int i = 0; i < 12; i++)
			{
				conf[i + 116] = 0;
				if (ckArr[i].IsChecked == true)
				{
					conf[i + 116] = 1;
				}
			}

		}

		private void scheduleBCB_Checked(object sender, RoutedEventArgs e)
		{
			if (((CheckBox)sender).IsChecked == true)
			{
				for (int i = 0; i < 24; i++)
				{
					timePanelArAB[i].setAB();
				}
			}
			else
			{
				for (int i = 0; i < 24; i++)
				{
					timePanelArAB[i].setA();
				}
			}

		}

		private void scheduleDCB_Checked(object sender, RoutedEventArgs e)
		{
			if (((CheckBox)sender).IsChecked == true)
			{
				for (int i = 0; i < 24; i++)
				{
					timePanelArCD[i].setAB();
				}
			}
			else
			{
				for (int i = 0; i < 24; i++)
				{
					timePanelArCD[i].setA();
				}
			}
		}

		private void validate(object sender, RoutedEventArgs e)
		{
			TextBox tb = (TextBox)sender;
			validate(ref tb);
		}

		private void validate(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Return)
			{
				TextBox tb = (TextBox)sender;
				validate(ref tb);

				return;
			}
			int ascii = KeyInterop.VirtualKeyFromKey(e.Key);
			if (((ascii < 48) | (ascii > 57)) && ((ascii < 96) | (ascii > 105)))
			{
				if (ascii != 9)
				{
					e.Handled = true;
				}
			}
		}

		private void validate(ref TextBox tb)
		{
			int ind;
			for (ind = 0; ind < 4; ind++)
			{
				if (tb == tbArr[ind]) break;
			}

			cbArr[ind].SelectionChanged -= cbSelChanged;

			uint newVal = 0;
			uint.TryParse(tbArr[ind].Text, out newVal);
			try
			{
				if (cbArr[ind].SelectedIndex == 2)
				{
					newVal *= 3600;
				}
				else if (cbArr[ind].SelectedIndex == 1)
				{
					newVal *= 60;
				}
			}
			catch
			{
				newVal = 0;
			}
			if (newVal == 0)
			{
				tbArr[ind].Text = sch[ind].ToString();
				return;
			}
			else
			{
				if ((newVal % 3600) == 0)
				{
					cbArr[ind].SelectedIndex = 2;
					newVal /= 3600;
				}
				else if ((newVal % 60) == 0)
				{
					cbArr[ind].SelectedIndex = 1;
					newVal /= 60;
				}
				else
				{
					cbArr[ind].SelectedIndex = 0;
				}
			}

			sch[ind] = newVal;
			tbArr[ind].Text = sch[ind].ToString();
			oldCbVal[ind] = cbArr[ind].SelectedIndex;
			cbArr[ind].SelectionChanged += cbSelChanged;
		}

		private void cbSelChanged(object sender, SelectionChangedEventArgs e)
		{
			ComboBox cb = (ComboBox)sender;

			int ind;
			for (ind = 0; ind < 4; ind++)
			{
				if (cb == cbArr[ind]) break;
			}

			if (cbArr[ind].SelectedIndex == oldCbVal[ind]) return;

			if (cbArr[ind].SelectedIndex == (oldCbVal[ind] - 2))
			{
				sch[ind] *= 3600;

			}
			else if (cbArr[ind].SelectedIndex == (oldCbVal[ind] - 1))
			{
				sch[ind] *= 60;
			}
			else if (cbArr[ind].SelectedIndex == (oldCbVal[ind] + 1))
			{
				sch[ind] /= 60;
			}
			else if (cbArr[ind].SelectedIndex == (oldCbVal[ind] + 2))
			{
				sch[ind] /= 3600;
			}

			if (sch[ind] == 0) sch[ind] = 1;

			oldCbVal[ind] = cbArr[ind].SelectedIndex;
			tbArr[ind].Text = sch[ind].ToString();
		}

	}
}
