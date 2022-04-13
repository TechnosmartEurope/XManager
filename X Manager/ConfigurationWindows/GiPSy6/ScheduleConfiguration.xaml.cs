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
		ComboBox[] quantityArr;
		ComboBox[] unitArr;

		CheckBox[] mAr;
		CheckBox[] mmAr;
		//int[] oldCbVal = new int[4];
		public ScheduleConfiguration(byte[] conf)
		{
			InitializeComponent();
			this.conf = conf;
			int riga = 0;
			int colonna = 0;
			quantityArr = new ComboBox[] { aValCB, bValCB, cValCB, dValCB };
			unitArr = new ComboBox[4] { aTimeUnitCB, bTimeUnitCB, cTimeUnitCB, dTimeUnitCB };
			mAr = new CheckBox[12] { m1, m2, m3, m4, m5, m6, m7, m8, m9, m10, m11, m12 };
			mmAr = new CheckBox[12] { mm1, mm2, mm3, mm4, mm5, mm6, mm7, mm8, mm9, mm10, mm11, mm12 };

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

			sch[0] = BitConverter.ToUInt16(conf, 52);   //52-53
			sch[1] = BitConverter.ToUInt16(conf, 54);   //54-55
			sch[2] = BitConverter.ToUInt16(conf, 84);   //84-85
			sch[3] = BitConverter.ToUInt16(conf, 86);   //86-87
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
				unitArr[i].SelectedIndex = (int)sch[i] >> 8;
				quantityArr[i].Text = (sch[i] & 0xff).ToString();
				if (unitArr[i].SelectedIndex == 2)
				{
					quantityArr[i].Items.Clear();
					quantityArr[i].Items.Add("1");
					quantityArr[i].SelectedIndex = 0;
				}
				unitArr[i].SelectionChanged += cbSelChanged;
			}

			//Mesi C/D
			for (int i = 0; i < 12; i++)
			{
				mmAr[i].IsChecked = conf[i + 116] == 0 ? true : false;
				mAr[i].IsChecked = conf[i + 116] == 1 ? true : false;
				mAr[i].Checked += m_Checked;
				mAr[i].Unchecked += m_Checked;
				mmAr[i].Checked += mm_Checked;
				mmAr[i].Unchecked += mm_Checked;
				
			}
		}

		public override void copyValues()
		{

			for (int i = 0; i < 4; i++)
			{
				sch[i] = uint.Parse(quantityArr[i].Text);
				sch[i] += (uint)(unitArr[i].SelectedIndex) << 8;
				//sch[i] *= (uint)Math.Pow(60, unitArr[i].SelectedIndex);
			}

			conf[52] = (byte)(sch[0] & 0xff);
			conf[53] = (byte)(sch[0] >> 8);

			conf[54] = (byte)(sch[1] & 0xff);
			conf[55] = (byte)(sch[1] >> 8);

			conf[84] = (byte)(sch[2] & 0xff);
			conf[85] = (byte)(sch[2] >> 8);

			conf[86] = (byte)(sch[3] & 0xff);
			conf[87] = (byte)(sch[3] >> 8);

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
				if (mmAr[i].IsChecked == true)
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

		private void cbSelChanged(object sender, SelectionChangedEventArgs e)
		{
			ComboBox cb = (ComboBox)sender;

			int ind;
			for (ind = 0; ind < 4; ind++)
			{
				if (cb == unitArr[ind]) break;
			}
			string oldVal = quantityArr[ind].Text;
			if (cb.SelectedIndex == 2)
			{
				quantityArr[ind].Items.Clear();
				quantityArr[ind].Items.Add("1");
				quantityArr[ind].SelectedIndex = 0;
			}
			else
			{
				if (quantityArr[ind].Items.Count == 1)
				{
					quantityArr[ind].Items.Clear();
					string[] sIt = new string[] { "1", "2", "3", "4", "5", "6", "10", "12", "15", "20", "30" };
					foreach (string s in sIt)
					{
						quantityArr[ind].Items.Add(s);
					}
				}
				quantityArr[ind].Text = oldVal;
			}
		}

		private void mm_Checked(object sender, RoutedEventArgs e)
		{
			int index = 0;
			for (int i = 0; i < 12; i++)
			{
				if ((CheckBox)sender == mmAr[i])
				{
					index = i;
					break;
				}
			}
			mAr[index].Checked -= m_Checked;
			mAr[index].Unchecked -= m_Checked;
			mAr[index].IsChecked = !mmAr[index].IsChecked;
			mAr[index].Checked += m_Checked;
			mAr[index].Unchecked += m_Checked;
		}

		private void m_Checked(object sender, RoutedEventArgs e)
		{
			int index = 0;
			for (int i = 0; i < 12; i++)
			{
				if ((CheckBox)sender == mAr[i])
				{
					index = i;
					break;
				}
			}
			mmAr[index].Checked -= mm_Checked;
			mmAr[index].Unchecked -= mm_Checked;
			mmAr[index].IsChecked = !mAr[index].IsChecked;
			mmAr[index].Checked += mm_Checked;
			mmAr[index].Unchecked += mm_Checked;
		}
	}
}
