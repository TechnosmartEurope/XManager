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

		CheckBox[] mAr_sx;
		CheckBox[] mAr_dx;
		//int[] oldCbVal = new int[4];
		public ScheduleConfiguration(byte[] conf)
		{
			InitializeComponent();
			this.conf = conf;
			int riga = 0;
			int colonna = 0;
			quantityArr = new ComboBox[] { aValCB, bValCB, cValCB, dValCB };
			unitArr = new ComboBox[4] { aTimeUnitCB, bTimeUnitCB, cTimeUnitCB, dTimeUnitCB };
			mAr_sx = new CheckBox[12] { m1_sx, m2_sx, m3_sx, m4_sx, m5_sx, m6_sx, m7_sx, m8_sx, m9_sx, m10_sx, m11_sx, m12_sx };
			mAr_dx = new CheckBox[12] { m1_dx, m2_dx, m3_dx, m4_dx, m5_dx, m6_dx, m7_dx, m8_dx, m9_dx, m10_dx, m11_dx, m12_dx };

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
				timePanelAB.checkedChanged += leftCheckedManager;
				secondaryScheduleGrid.Children.Add(timePanelCD);
				timePanelArCD[i] = timePanelCD;
				timePanelCD.checkedChanged += rightCheckedManager;

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
			leftCheckedManager(new object(), new EventArgs());
			rightCheckedManager(new object(), new EventArgs());

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
				mAr_sx[i].IsChecked = conf[i + 116] == 0 ? true : false;
				mAr_dx[i].IsChecked = conf[i + 116] == 1 ? true : false;
				mAr_sx[i].Checked += mSx_Checked;
				mAr_sx[i].Unchecked += mSx_Checked;
				mAr_dx[i].Checked += mDx_Checked;
				mAr_dx[i].Unchecked += mDx_Checked;

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

			//Mesi validità schedule C/D
			for (int i = 0; i < 12; i++)
			{
				conf[i + 116] = 0;
				if (mAr_dx[i].IsChecked == true)
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

		private void mDx_Checked(object sender, RoutedEventArgs e)
		{
			int index = 0;
			for (int i = 0; i < 12; i++)
			{
				if ((CheckBox)sender == mAr_dx[i])
				{
					index = i;
					break;
				}
			}
			mAr_sx[index].Checked -= mSx_Checked;
			mAr_sx[index].Unchecked -= mSx_Checked;
			mAr_sx[index].IsChecked = !mAr_dx[index].IsChecked;
			mAr_sx[index].Checked += mSx_Checked;
			mAr_sx[index].Unchecked += mSx_Checked;
		}

		private void mSx_Checked(object sender, RoutedEventArgs e)
		{
			int index = 0;
			for (int i = 0; i < 12; i++)
			{
				if ((CheckBox)sender == mAr_sx[i])
				{
					index = i;
					break;
				}
			}
			mAr_dx[index].Checked -= mDx_Checked;
			mAr_dx[index].Unchecked -= mDx_Checked;
			mAr_dx[index].IsChecked = !mAr_sx[index].IsChecked;
			mAr_dx[index].Checked += mDx_Checked;
			mAr_dx[index].Unchecked += mDx_Checked;
		}

		private void leftSelectAll_Checked(object sender, RoutedEventArgs e)
		{
			for (int i = 0;i < 24;i++){
				timePanelArAB[i].checkedChanged -= leftCheckedManager;
				timePanelArAB[i].isChecked = (bool)leftSelectAll.IsChecked;
				timePanelArAB[i].checkedChanged += leftCheckedManager;
			}
			leftSelectAll.Checked -= leftSelectAll_Checked;
			leftSelectAll.Unchecked -= leftSelectAll_Checked;
			leftSelectAll.IsChecked = leftSelectAll.IsChecked;
			leftSelectAll.Checked += leftSelectAll_Checked;
			leftSelectAll.Unchecked += leftSelectAll_Checked;
		}

		private void rightSelectAll_Checked(object sender, RoutedEventArgs e)
		{
			for (int i = 0; i < 24; i++)
			{
				timePanelArCD[i].checkedChanged -= rightCheckedManager;
				timePanelArCD[i].isChecked = (bool)rightSelectAll.IsChecked;
				timePanelArCD[i].checkedChanged += rightCheckedManager;
			}
			rightSelectAll.Checked -= rightSelectAll_Checked;
			rightSelectAll.Unchecked -= rightSelectAll_Checked;
			rightSelectAll.IsChecked = rightSelectAll.IsChecked;
			rightSelectAll.Checked += rightSelectAll_Checked;
			rightSelectAll.Unchecked += rightSelectAll_Checked;
		}

		private void leftCheckedManager(object sender, EventArgs e)
		{
			int lc = 0;
			for (int i = 0; i < 24; i++)
			{
				if (timePanelArAB[i].isChecked == true)
				{
					lc++;
				}
			}
			leftSelectAll.Checked -= leftSelectAll_Checked;
			leftSelectAll.Unchecked -= leftSelectAll_Checked;
			if (lc == 24)
			{
				leftSelectAll.IsChecked = true;
			}
			else
			{
				leftSelectAll.IsChecked = false;
			}
			leftSelectAll.Checked += leftSelectAll_Checked;
			leftSelectAll.Unchecked += leftSelectAll_Checked;
		}

		private void rightCheckedManager(object sender, EventArgs e)
		{
			int rc = 0;
			for (int i = 0; i < 24; i++)
			{
				if (timePanelArCD[i].isChecked == true)
				{
					rc++;
				}
			}
			rightSelectAll.Checked -= rightSelectAll_Checked;
			rightSelectAll.Unchecked -= rightSelectAll_Checked;
			if (rc == 24)
			{
				rightSelectAll.IsChecked = true;
			}
			else
			{
				rightSelectAll.IsChecked = false;
			}
			rightSelectAll.Checked += rightSelectAll_Checked;
			rightSelectAll.Unchecked += rightSelectAll_Checked;
		}

	}
}
