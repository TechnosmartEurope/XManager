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

namespace X_Manager.ConfigurationWindows.GiPSy6
{
	/// <summary>
	/// Logica di interazione per HourBar.xaml
	/// </summary>
	public partial class HourBar : UserControl
	{

		Rectangle[] rectAr;
		byte[] rStat;
		public bool oneAtLeast = false;

		public enum MODE
		{
			MODE_OLD = 0,
			MODE_NEW
		}
		public HourBar()
		{
			InitializeComponent();

			rectAr = new Rectangle[24] { r00, r01, r02, r03, r04, r05, r06, r07, r08, r09, r10, r11, r12, r13, r14, r15, r16, r17, r18, r19, r20,
							r21, r22, r23 };

			rStat = new byte[24];

			foreach (Rectangle r in rectAr)
			{
				r.MouseLeftButtonDown += R_Click;
				r.MouseEnter += R_MouseEnter;
			}
		}

		private void R_MouseEnter(object sender, MouseEventArgs e)
		{
			if (Mouse.LeftButton == MouseButtonState.Pressed)
			{
				string name = (sender as Rectangle).Name;
				name = name.Substring(1, 2);
				int pos = int.Parse(name);
				if (rStat[pos] == 0)
				{
					rStat[pos] = 1;
					rectAr[pos].Fill = new SolidColorBrush(Color.FromArgb(0xff, 0x00, 0xaa, 0xde));
				}
				else
				{
					rStat[pos] = 0;
					rectAr[pos].Fill = new SolidColorBrush(Color.FromArgb(0xff, 0x20, 0x20, 0x20));
					if (oneAtLeast)
					{
						checkOneAtLeast();
					}
				}
			}
		}

		private void R_Click(object sender, MouseButtonEventArgs e)
		{
			string name = (sender as Rectangle).Name;
			name = name.Substring(1, 2);
			int pos = int.Parse(name);
			if (rStat[pos] == 0)
			{
				rStat[pos] = 1;
				rectAr[pos].Fill = new SolidColorBrush(Color.FromArgb(0xff, 0x00, 0xaa, 0xde));
			}
			else
			{
				rStat[pos] = 0;
				rectAr[pos].Fill = new SolidColorBrush(Color.FromArgb(0xff, 0x20, 0x20, 0x20));
				if (oneAtLeast)
				{
					checkOneAtLeast();
				}
			}

		}

		private void checkOneAtLeast()
		{
			int check = 0;
			for (int i = 0; i < 24; i++)
			{
				check += rStat[i];
			}
			if (check == 0)
			{
				rStat[0] = 1;
				rectAr[0].Fill = new SolidColorBrush(Color.FromArgb(0xff, 0x00, 0xaa, 0xde));
			}
		}

		public void setHour(int hour)
		{
			rStat[hour] = 1;
			rectAr[hour].Fill = new SolidColorBrush(Color.FromArgb(0xff, 0x00, 0xaa, 0xde));
		}

		public byte[] getStatus(MODE m)
		{
			if (m == MODE.MODE_OLD)
			{
				return rStat;
			}
			else
			{
				byte[] retAr = new byte[3];
				for (int j = 0; j < 3; j++)
				{
					for (int i = 23 - (j * 8); i > 15 - (j * 8); i--)
					{
						retAr[j] <<= 1;
						retAr[j] += rStat[i];
					}
				}
				return retAr;
			}
		}

		public void setStatus(byte[] arIn)
		{
			int check = 0;
			if (arIn.Length == 24)
			{
				for (int i = 0; i < 24; i++)
				{
					rStat[i] = arIn[i];
					if (rStat[i] == 1)
					{
						rectAr[i].Fill = new SolidColorBrush(Color.FromArgb(0xff, 0x00, 0xaa, 0xde));
						check++;
					}
					else
					{
						rectAr[i].Fill = new SolidColorBrush(Color.FromArgb(0xff, 0x20, 0x20, 0x20));
					}
				}
			}
			else
			{
				for (int j = 0; j < 3; j++)
				{
					for (int i = 0; i < 8; i++)
					{

						rStat[i + ((2 - j) * 8)] = (byte)((arIn[j] >> i) & 1);
						if (((arIn[j] >> i) & 1) == 1)
						{
							rectAr[i + ((2 - j) * 8)].Fill = new SolidColorBrush(Color.FromArgb(0xff, 0x00, 0xaa, 0xde));
							check++;
						}
						else
						{
							rectAr[i + ((2 - j) * 8)].Fill = new SolidColorBrush(Color.FromArgb(0xff, 0x20, 0x20, 0x20));
						}
					}
				}
			}
			if (check == 0 && oneAtLeast)
			{
				rStat[0] = 1;
				rectAr[0].Fill = new SolidColorBrush(Color.FromArgb(0xff, 0x00, 0xaa, 0xde));
			}
		}

		public void allOn()
		{
			for (int i = 0; i < 24; i++)
			{
				rStat[i] = 1;
				rectAr[i].Fill = new SolidColorBrush(Color.FromArgb(0xff, 0x00, 0xaa, 0xde));
			}
		}

		public void allOff()
		{
			for (int i = 0; i < 24; i++)
			{
				rStat[i] = 0;
				rectAr[i].Fill = new SolidColorBrush(Color.FromArgb(0xff, 0x20, 0x20, 0x20));
			}
			if (oneAtLeast)
			{
				rStat[0] = 1;
				rectAr[0].Fill = new SolidColorBrush(Color.FromArgb(0xff, 0x00, 0xaa, 0xde));
			}
		}
	}
}
