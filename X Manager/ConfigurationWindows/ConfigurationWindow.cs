﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace X_Manager.ConfigurationWindows
{
	public class ConfigurationWindow : Window
	{
		public bool? res = null;
		public bool mustWrite = false;
		public byte[] axyConfOut;
		public byte[] axyScheduleOut;
		public string lastForthContent = "SEND";

		protected string iniFile = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + MainWindow.companyFolder + MainWindow.appFolder + "\\settings.ini";
		protected string trekSchedFile = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + MainWindow.companyFolder + MainWindow.appFolder + "\\trekLastSchedule.ini";
		protected string gipsySchedFile = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + MainWindow.companyFolder + MainWindow.appFolder + "\\gipsyLastSchedule.ini";


		public ConfigurationWindow()
		{

		}

		public virtual new bool? ShowDialog()
		{
			//return base.ShowDialog();
			base.ShowDialog();
			return res;
		}
	}
}
