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
using System.Security.Cryptography;
using System.Windows.Media.Imaging;



namespace X_Manager.Remote
{
	/// <summary>
	/// Logica di interazione per RemoteUeserSelection.xaml
	/// </summary>
	/// 
	public partial class RemoteUeserSelection : Window
	{

		public string basetstationID = "";
		public string userID = "";
		string[] users;
		public RemoteUeserSelection()
		{
			InitializeComponent();
			users = HTTP.cOut(HTTP.COMMAND_GET_USERSLIST).Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string user in users)
			{
				usersLV.Items.Add(user);
			}
		}

		private void filterTB_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				e.Handled = true;
				return;
			}
		}

		private void filterTB_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (filterTB.Text.Length > 1)
			{
				string filter = filterTB.Text;
				usersLV.Items.Clear();
				foreach (string user in users)
				{
					if (user.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
					{
						usersLV.Items.Add(user);
					}
				}
			}
			else if (filterTB.Text.Length == 0)
			{
				usersLV.Items.Clear();
				foreach (string user in users)
				{
					usersLV.Items.Add(user);
				}
			}
		}

		private void generateB_Click(object sender, RoutedEventArgs e)
		{
			const string valid0 = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
			const string valid1 = "1234567890";
			const string valid2 = "\\|!\"£$%&/()=?^'[]+*ç°§@#,.-;:_";
			StringBuilder password = new StringBuilder(); ;

			do
			{
				password.Append(valid0[MainWindow.random.Next(valid0.Length)]);
			} while (password.Length < 8);

			int pos = MainWindow.random.Next(password.Length);
			password = password.Insert(pos, valid1[MainWindow.random.Next(valid1.Length)]);
			password = password.Insert(pos, valid1[MainWindow.random.Next(valid1.Length)]);
			pos = MainWindow.random.Next(password.Length);
			password = password.Insert(pos, valid2[MainWindow.random.Next(valid2.Length)]);
			passwordTB.Text = password.ToString();


		}

		private bool validatePassword()
		{
			bool passwordValid = false;
			int pwSpecial = 0;
			const string valid0 = "\\|!\"£$%&/()=?^'[]+*ç°§@#,.-;:_";
			const string valid1 = "1234567890";
			if (passwordTB.Text.Length >= 8)
			{
				passwordValid = true;
			}
			for (int i = 0; i < passwordTB.Text.Length; i++)
			{
				if (valid0.IndexOf(passwordTB.Text.Substring(i, 1), StringComparison.OrdinalIgnoreCase) != -1)
				{
					pwSpecial++;
					break;
				}
			}
			for (int i = 0; i < passwordTB.Text.Length; i++)
			{
				if (valid1.IndexOf(passwordTB.Text.Substring(i, 1), StringComparison.OrdinalIgnoreCase) != -1)
				{
					pwSpecial++;
					break;
				}
			}
			return passwordValid && (pwSpecial == 2);
		}

		private void addUserB_Click(object sender, RoutedEventArgs e)
		{
			if (userNameTB.Text.Length < 1)
			{
				MessageBox.Show("User name field can't be left empty.");
				return;
			}
			if (!validatePassword())
			{
				MessageBox.Show("Password not valid.\r\nNew passwords must contain at least 1 number, 1 special character\r\nand must be at least 8 characters long.");
				return;
			}
			foreach (string user in users)
			{
				if (user.Equals(userNameTB.Text, StringComparison.OrdinalIgnoreCase))
				{
					MessageBox.Show("User already Existing.");
					return;
				}
			}
			string userId = HTTP.cOut(String.Format("{0}&username={1}&password={2}", HTTP.COMMAND_CREATE_NEW_USER, userNameTB.Text, passwordTB.Text));
			if (userId.Equals(HTTP.COMMAND_ERROR))
			{
				MessageBox.Show("Error: can't create a new user.");
				return;
			}
			usersLV.Items.Clear();
			users = HTTP.cOut(HTTP.COMMAND_GET_USERSLIST).Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string user in users)
			{
				usersLV.Items.Add(user);
			}
		}

		private void selectB_Click(object sender, RoutedEventArgs e)
		{
			basetstationID = "";
			if (usersLV.SelectedIndex == -1)
			{
				MessageBox.Show("Please, select a user from the list.");
				return;
			}
			userID = HTTP.cOut(String.Format("{0}&username={1}", HTTP.COMMAND_GET_USER_ID, (String)usersLV.SelectedItem));
			if (userID.Equals(HTTP.COMMAND_ERROR))
			{
				MessageBox.Show("ERROR: Can't get user id.");
				return;
			}
			basetstationID = HTTP.cOut(String.Format("{0}&userid={1}&stationname=", HTTP.COMMAND_CREATE_BASESTATION, userID));
			if (basetstationID.Equals(HTTP.COMMAND_ERROR))
			{
				MessageBox.Show("ERROR: Can't create basestation.");
				basetstationID = "";
				return;
			}

			Close();
		}
	}
}
