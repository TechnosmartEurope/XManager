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
	/// Interaction logic for AccDayIntervals.xaml
	/// </summary>
	public partial class AccDayIntervals : UserControl
	{
		public AccDayIntervals()
		{
			InitializeComponent();
		}

		public void enable12()
		{
			bit1CB.Items.Add("12");
			bit2CB.Items.Add("12");
			bit3CB.Items.Add("12");
			bit4CB.Items.Add("12");
			bit5CB.Items.Add("12");
		}

		private void uiKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Return)
			{
				e.Handled = true;
				System.Windows.Forms.SendKeys.SendWait("\t");
			}
		}

		private void newH(object sender, EventArgs e)
		{

			var box = (TextBox)sender;
			int n;
			switch (box.Name)
			{
				case "e1":
					try
					{
						n = int.Parse(box.Text);
						if (n < 1) n = 1;
						if (n > (int.Parse(e2.Text) - 1)) n = int.Parse(e2.Text) - 1;
					}
					catch
					{
						n = 1;
					}
					e1.Text = n.ToString();
					d2.Text = n.ToString();
					break;
				case "e2":
					try
					{
						n = int.Parse(box.Text);
						if (n < int.Parse(d2.Text) + 1) n = int.Parse(d2.Text) + 1;
						if (n > (int.Parse(e3.Text) - 1)) n = int.Parse(e3.Text) - 1;
					}
					catch
					{
						n = int.Parse(d2.Text) + 1;
					}
					e2.Text = n.ToString();
					d3.Text = n.ToString();
					break;
				case "e3":
					try
					{
						n = int.Parse(box.Text);
						if (n < int.Parse(d3.Text) + 1) n = int.Parse(d3.Text) + 1;
						if (n > (int.Parse(e4.Text) - 1)) n = int.Parse(e4.Text) - 1;
					}
					catch
					{
						n = int.Parse(d3.Text) + 1;
					}
					e3.Text = n.ToString();
					d4.Text = n.ToString();
					break;
				case "e4":
					try
					{
						n = int.Parse(box.Text);
						if (n < int.Parse(d4.Text) + 1) n = int.Parse(d4.Text) + 1;
						if (n > (int.Parse(e5.Text) - 1)) n = int.Parse(e5.Text) - 1;
					}
					catch
					{
						n = int.Parse(d4.Text) + 1;
					}
					e4.Text = n.ToString();
					d5.Text = n.ToString();
					break;
			}
			//a = int.Parse(d2.Text); b = int.Parse(e1.Text); c = int.Parse(e2.Text);
			//if (a > (c - 1)) a = c - 1;

		}

		private void minusB(object sender, RoutedEventArgs e)
		{
			int orario = 0;
			int intervallo = 0;
			while (orario < 24)
			{
				intervallo++;
				if (intervallo == 5) break;
				var v = (Grid)(mainGrid.Children[1 + intervallo]);
				orario = int.Parse(((TextBox)(v.Children[1])).Text);
			}
			if (intervallo == 1) return;
			intervallo--;
			int step = (int)Math.Round((double)(24 / intervallo), 0, MidpointRounding.AwayFromZero);

			byte[] schedule = exportSchedule();
			for (int i = 0; i < 5; i++)
			{
				int nuovaOra = step * (i + 1);
				if (nuovaOra > 24)
				{
					nuovaOra = 24;
				}
				nuovaOra = ((nuovaOra / 10) * 16) + (nuovaOra % 10);
				schedule[i * 6] = (byte)nuovaOra;
			}
			importSchedule(schedule);
		}

		private void plusB(object sender, RoutedEventArgs e)
		{

			int orario = 0;
			int intervallo = 0;
			while (orario < 24)
			{
				intervallo++;
				if (intervallo == 5) return;
				var v = (Grid)(mainGrid.Children[1 + intervallo]);
				orario = int.Parse(((TextBox)(v.Children[1])).Text);
			}
			intervallo++;
			int step = (int)Math.Round((double)(24 / intervallo), 0, MidpointRounding.AwayFromZero);
			if (intervallo == 5)
			{
				step = 5;
			}

			byte[] schedule = exportSchedule();
			for (int i = 0; i < 5; i++)
			{
				int nuovaOra = step * (i + 1);
				if (nuovaOra > 24)
				{
					nuovaOra = 24;
				}
				nuovaOra = ((nuovaOra / 10) * 16) + (nuovaOra % 10);
				schedule[i * 6] = (byte)nuovaOra;
			}
			importSchedule(schedule);


		}

		public void importSchedule(byte[] schedule)
		{
			bool hide = false;
			int startInt = 0;
			for (int i = 0; i < 5; i++)
			{
				var v = (Grid)(mainGrid.Children[i + 2]);
				if (hide)
				{
					((TextBox)(v.Children[0])).Text = startInt.ToString();
					((TextBox)(v.Children[1])).Text = 0x24.ToString();
					((ComboBox)(v.Children[2])).SelectedIndex = schedule[1 + (i * 6)];
					((ComboBox)(v.Children[3])).SelectedIndex = schedule[2 + (i * 6)];
					//Rimozione forzata dei 12bit in attesa del nuovo chip
					//if (schedule[3 + (i * 6)] == 2)
					//{
					//	schedule[3 + (i * 6)] = 1;
					//}
					///Fine rimozione forzata
					((ComboBox)(v.Children[4])).SelectedIndex = schedule[3 + (i * 6)];
					((ComboBox)(v.Children[5])).SelectedIndex = schedule[4 + (i * 6)];
					((ComboBox)(v.Children[6])).SelectedIndex = schedule[5 + (i * 6)];
					v.Visibility = Visibility.Hidden;
				}
				else
				{
					v.Visibility = Visibility.Visible;
					((TextBox)(v.Children[0])).Text = startInt.ToString();
					startInt = schedule[0 + (i * 6)];
					if (startInt == 0x24)
					{
						hide = true;
						((TextBox)(v.Children[1])).IsEnabled = false;
					}
					else
					{
						((TextBox)(v.Children[1])).IsEnabled = true;
					}
					startInt = ((startInt / 16) * 10) + (startInt % 16);
					((TextBox)(v.Children[1])).Text = startInt.ToString();
					((ComboBox)(v.Children[2])).SelectedIndex = schedule[1 + (i * 6)];
					((ComboBox)(v.Children[3])).SelectedIndex = schedule[2 + (i * 6)];
					//Rimozione forzata dei 12bit in attesa del nuovo chip
					//if (schedule[3 + (i * 6)] == 2)
					//{
					//	schedule[3 + (i * 6)] = 1;
					//}
					///Fine rimozione forzata
					((ComboBox)(v.Children[4])).SelectedIndex = schedule[3 + (i * 6)];
					((ComboBox)(v.Children[5])).SelectedIndex = schedule[4 + (i * 6)];
					((ComboBox)(v.Children[6])).SelectedIndex = schedule[5 + (i * 6)];
				}
			}


		}

		public byte[] exportSchedule()
		{
			byte[] schedule = new byte[30];

			for (int i = 0; i < 5; i++)
			{
				var v = (Grid)(mainGrid.Children[i + 2]);
				int b = byte.Parse(((TextBox)(v.Children[1])).Text);
				schedule[i * 6] = (byte)(((b / 10) * 16) + (b % 10));
				schedule[(i * 6) + 1] = ((byte)((ComboBox)(v.Children[2])).SelectedIndex);
				schedule[(i * 6) + 2] = ((byte)((ComboBox)(v.Children[3])).SelectedIndex);
				schedule[(i * 6) + 3] = ((byte)((ComboBox)(v.Children[4])).SelectedIndex);
				schedule[(i * 6) + 4] = ((byte)((ComboBox)(v.Children[5])).SelectedIndex);
				schedule[(i * 6) + 5] = ((byte)((ComboBox)(v.Children[6])).SelectedIndex);
			}
			return schedule;
		}

	}

}

