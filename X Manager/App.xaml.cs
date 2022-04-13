using System;
using System.IO;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;

namespace X_Manager
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	/// 

	public partial class App : System.Windows.Application
	{
		[DllImport("kernel32.dll")]
		static extern IntPtr GetConsoleWindow();

		[DllImport("user32.dll")]
		static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		const int SW_HIDE = 0;
		const int SW_SHOW = 5;

		[System.STAThreadAttribute()]
		[System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
		public static void Main()
		{
			X_Manager.App app = new X_Manager.App();
			//System.Windows.Forms.Application.EnableVisualStyles();
			//System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
			//AttachConsole(ATTACH_PARENT_PROCESS);

			string[] args = Environment.GetCommandLineArgs();
			bool form = true;
			foreach (string arg in args)
			{
				if (arg.Contains("-c") | arg.Contains("-h"))
				{
					form = false;
					break;
				}
			}
			if (form)
			{
				var handle = GetConsoleWindow();
				ShowWindow(handle, SW_HIDE);
				app.InitializeComponent();
				app.Run();
			}
			else
			{
				MainConsole mc = new MainConsole();
				mc.nextFile();
				//MainConsole mc = app.InitializeConsole(ref app);
			}

		}

	}


}
