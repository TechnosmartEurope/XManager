using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic.FileIO;
using System.Threading;
using X_Manager.Units;
using System.Windows.Controls;
using System.Windows;
using System.Text;
using System.Threading.Tasks;

namespace X_Manager
{
	class MainConsole : Parent
	{
		//X_Manager.App app = null;
		Unit cUnit;
		string fileName = "";
		string[] newPrefs;

		bool stop = false;

		//const int pref_pressMetri = 0;
		//const int pref_millibars = 1;
		//const int pref_dateFormat = 2;
		//const int pref_timeFormat = 3;
		//const int pref_fillEmpty = 4;
		//const int pref_sameColumn = 5;
		//const int pref_battery = 6;
		//const int pref_txt = 7;
		//const int pref_kml = 8;
		//const int pref_metadata = 16;
		//const int pref_leapSeconds = 17;
		//const int pref_removeNonGps = 18;

		public MainConsole()
		{
			csvSeparator = "\t";
			sp = null;
			var dt = DateTime.Now;
			newPrefs = new string[19];
			newPrefs[Unit.pref_pressMetri] = "millibars";
			newPrefs[Unit.pref_millibars] = "1016";
			newPrefs[Unit.pref_dateFormat] = "1";	//Date format
			newPrefs[Unit.pref_timeFormat] = "1";	//Time format
			newPrefs[Unit.pref_fillEmpty] = "False";
			newPrefs[Unit.pref_sameColumn] = "False";
			newPrefs[Unit.pref_battery] = "True";
			newPrefs[Unit.pref_txt] = "True";
			newPrefs[Unit.pref_kml] = "True";
			newPrefs[9] = dt.Hour.ToString() + "";
			newPrefs[10] = dt.Minute.ToString() + "";
			newPrefs[11] = dt.Second.ToString() + "";
			newPrefs[12] = dt.Date.Year.ToString() + "";
			newPrefs[13] = dt.Date.Month.ToString() + "";
			newPrefs[14] = dt.Date.Day.ToString() + "";
			newPrefs[Unit.pref_override_time] = "False";
			newPrefs[Unit.pref_metadata] = "True";
			newPrefs[Unit.pref_leapSeconds] = "2";
			newPrefs[Unit.pref_removeNonGps] = "False";

			//lastSettings = new string[11];
			//lastSettings[0] = "null";
			//lastSettings[1] = "null";
			//lastSettings[2] = "null";
			//lastSettings[3] = "";//Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + companyFolder + "\\Downloads";
			//lastSettings[4] = "";// Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + companyFolder + "\\Downloads";
			//lastSettings[5] = "depth";
			//lastSettings[6] = "false";
			//lastSettings[7] = "3";
			//lastSettings[8] = "\t";
			//lastSettings[9] = "A";
			//lastSettings[10] = "";// Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + companyFolder + "\\Axy5Schedule\r\n";

			statusLabel = new Label();
			statusLabel.Content = "Console";
			etaLabel = new Label();

			statusProgressBar = new ProgressBar();
		}

		private bool parseCommands()
		{
			bool res = true;

			List<string> args = Environment.GetCommandLineArgs().ToList();
			args.RemoveAt(0);
			while ((args.Count > 0) & res)
			{
				//string arg = args[0];
				//args.RemoveAt(0);

				string arg = lRead(args);
				bool pref;
				int val;
				switch (arg)
				{
					case "-h":
						res = false;
						break;
					case "-c":
						if ((args.Count == 0) || args[0].StartsWith("-"))
						{
							Console.WriteLine("Bad file name. Exiting...");
							res = false;
						}
						else
						{
							fileName = lRead(args);
						}
						break;

					case "-txt":
						pref = false;
						if (args.Count == 0 || (!bool.TryParse(lRead(args), out pref)) )
						{
							Console.WriteLine("TXT parameter missing. Exiting...");
							res = false;
							break;
						}
						newPrefs[Unit.pref_txt] = pref.ToString();
						break;
					case "-kml":
						pref = false;
						if (args.Count == 0 || (!bool.TryParse(lRead(args), out pref)))
						{
							Console.WriteLine("KML parameter missing. Exiting...");
							res = false;
							break;
						}
						newPrefs[Unit.pref_kml] = pref.ToString();
						break;
					case "-pressUnit":
						if (args.Count == 0)
						{
							Console.WriteLine("Pressure/Height parameter missing. Exiting...");
							res = false;
							break;
						}
						arg = lRead(args);
						if (arg.Contains("meter"))
						{
							newPrefs[Unit.pref_pressMetri] = "meters";
						}else if (arg.Contains("bar"))
						{
							newPrefs[Unit.pref_pressMetri] = "millibars";
						}
						else
						{
							Console.WriteLine("Pressure/Height bad parameter. Exiting...");
							res = false;
							break;
						}
						break;
					case "-pressOffset":
						if (args.Count == 0)
						{
							Console.WriteLine("Pressure offset value missing. Exiting...");
							res = false;
							break;
						}
						arg = lRead(args);
						if(!int.TryParse(arg,out val))
						{
							Console.WriteLine("Pressure offset bad value. Exiting...");
							res = false;
							break;
						}
						else
						{
							newPrefs[Unit.pref_millibars] = arg;
						}
						break;
					case "-dateFormat":
						if (args.Count == 0)
						{
							Console.WriteLine("Date format value missing. Exiting...");
							res = false;
							break;
						}
						arg = lRead(args);
						switch (arg)
						{
							case "dd/MM/yyyy":
								arg = "1";
								break;
							case "MM/dd/yyyy":
								arg = "2";
								break;
							case "yyyy/MM/dd":
								arg = "3";
								break;
							case "yyyy/dd/MM":
								arg = "4";
								break;
							default:
								Console.WriteLine("Bad date format. Exiting...");
								res = false;
								break;
						}
						newPrefs[Unit.pref_dateFormat] = arg;
						break;
					case "-timeFormat":
						if (args.Count == 0)
						{
							Console.WriteLine("Time format value missing. Exiting...");
							res = false;
							break;
						}
						arg = lRead(args);
						switch (arg)
						{
							case "ampm":
							case "12":
								newPrefs[Unit.pref_timeFormat] = "2";
								break;
							case "24":
								newPrefs[Unit.pref_timeFormat] = "1";
								break;
							default:
								Console.WriteLine("Bad Time format value. Exiting...");
								res = false;
								break;
						}
						break;
					case "-fillEmpty":
						if (args.Count == 0)
						{
							Console.WriteLine("\"fill empty fields\" preference missing. Exiting...");
							res = false;
							break;
						}
						arg = lRead(args);
						if (!bool.TryParse(arg, out pref))
						{
							Console.WriteLine("Bad \"fill empty fields\" preference value. Exiting...");
							res = false;
							break;
						}
						newPrefs[Unit.pref_fillEmpty] = pref.ToString();
						break;
					case "-sameColumn":
						if (args.Count == 0)
						{
							Console.WriteLine("Time and Date column preference missing. Exiting...");
							res = false;
							break;
						}
						arg = lRead(args);
						if (!bool.TryParse(arg, out pref))
						{
							Console.WriteLine("Bad Time and Date column preference. Exiting...");
							res = false;
							break;
						}
						newPrefs[Unit.pref_sameColumn] = pref.ToString();
						break;
					case "-battery":
						if (args.Count == 0)
						{
							Console.WriteLine("Battery preference missing. Exiting...");
							res = false;
							break;
						}
						arg = lRead(args);
						if (!bool.TryParse(arg, out pref))
						{
							Console.WriteLine("Bad Battery preference. Exiting...");
							res = false;
							break;
						}
						newPrefs[Unit.pref_battery] = pref.ToString();
						break;
					case "-newTime":
						if (args.Count == 0)
						{
							Console.WriteLine("New Date and Time missing. Exiting...");
							res = false;
							break;
						}
						arg = lRead(args);
						var dt = new DateTime();
						DateTime.TryParse(arg, out dt);
						newPrefs[9] = dt.Hour.ToString() + "";
						newPrefs[10] = dt.Minute.ToString() + "";
						newPrefs[11] = dt.Second.ToString() + "";
						newPrefs[12] = dt.Date.Year.ToString() + "";
						newPrefs[13] = dt.Date.Month.ToString() + "";
						newPrefs[14] = dt.Date.Day.ToString() + "";
						break;
					case "-overrideTime":
						if (args.Count == 0)
						{
							Console.WriteLine("Override Time preference missing. Exiting...");
							res = false;
							break;
						}
						arg = lRead(args);
						if (!bool.TryParse(arg, out pref))
						{
							Console.WriteLine("Bad Override Time preference. Exiting...");
							res = false;
							break;
						}
						newPrefs[Unit.pref_override_time] = pref.ToString();
						break;
					case "-metadata":
						if (args.Count == 0)
						{
							Console.WriteLine("Metadata preference missing. Exiting...");
							res = false;
							break;
						}
						arg = lRead(args);
						if (!bool.TryParse(arg, out pref))
						{
							Console.WriteLine("Bad Metadata preference. Exiting...");
							res = false;
							break;
						}
						newPrefs[Unit.pref_metadata] = pref.ToString();
						break;
					case "-removeNonGPS":
						if (args.Count == 0)
						{
							Console.WriteLine("Remove non-GPS session preference missing. Exiting...");
							res = false;
							break;
						}
						arg = lRead(args);
						if (!bool.TryParse(arg, out pref))
						{
							Console.WriteLine("Bad Remove non-GPS session preference. Exiting...");
							res = false;
							break;
						}
						newPrefs[Unit.pref_removeNonGps] = pref.ToString();
						break;
					case "-leapSeconds":
						if (args.Count == 0)
						{
							Console.WriteLine("Leap seconds value missing. Exiting...");
							res = false;
							break;
						}
						arg = lRead(args);
						if (!int.TryParse(arg,out val))
						{
							Console.WriteLine("Bad Leap seconds value. Exiting...");
							res = false;
							break;
						}
						newPrefs[Unit.pref_leapSeconds] = val.ToString();
						break;

				}
			}

			return res;

		}

		public override void nextFile()
		{
			if (stop)
			{
				Console.WriteLine("\r\nConversion complete.");
				Environment.Exit(-1);
			}
			stop = true;

			const int type_ard = 1;
			const int type_rem = 2;
			const int type_mdp = 3;

			int fileType = type_ard;
			//string fileHeader = "ARD ";
			string fileNameCsv;
			string fileNametxt;
			string fileNameKml;
			string fileNamePlaceMark;
			string[] nomiFile = new string[] { "" };
			string addOn = "";

			Console.Write("\r\n X MANAGER - Console Mode.\r\n\r\n");

			if (!parseCommands())
			{
				System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
				Stream stream = asm.GetManifestResourceStream("X_Manager.Resources.console-help.txt");
				StreamReader source = new StreamReader(stream);
				string pub = source.ReadToEnd();
				source.Dispose();
				stream.Dispose();
				Console.Write(pub+"\r\n");
				exit();
			}

			string exten = System.IO.Path.GetExtension(fileName);
			if ((exten.Length > 4))
			{
				addOn = ("_S" + exten.Remove(0, 4));
			}
			fileNameCsv = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + addOn + ".csv";
			fileNametxt = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + addOn + ".txt";
			fileNameKml = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + addOn + "_temp" + ".kml";
			fileNamePlaceMark = Path.GetDirectoryName(fileName) + "\\" + System.IO.Path.GetFileNameWithoutExtension(fileName) + addOn + ".kml";
			nomiFile = new string[] { fileNameCsv, fileNametxt, fileNamePlaceMark };

			if ((Path.GetExtension(fileName).Contains("Dump") || (System.IO.Path.GetExtension(fileName).Contains("dump") || System.IO.Path.GetExtension(fileName).Contains("mdp"))))
			{
				//fileHeader = "MEMDUMP ";
				fileType = type_mdp;
			}
			else if (Path.GetExtension(fileName).Contains("rem"))
			{
				//fileHeader = "REM ";
				fileType = type_rem;
			}

			foreach (string nomefile in nomiFile)
			{
				try
				{
					FileSystem.DeleteFile(nomefile);

				}
				catch { }
			}

			Console.WriteLine("Converting file: " + fileName);

			FileStream fs = File.OpenRead(fileName);
			byte model;
			byte fw = 0;
			if (fileType == type_ard)
			{
				model = (byte)fs.ReadByte();
			}
			else
			{
				try
				{
					model = findMdpModel(ref fs)[0];
					fw = findMdpModel(ref fs)[1];
				}
				catch (Exception ex)
				{
					Console.Write(ex.Message);
					exit();
					return;
				}
			}

			switch (model)
			{
				case Unit.model_axy3:
					cUnit = new Axy3(this);
					break;
				case Unit.model_axy4:
					if (fileType == type_ard) fw = (byte)fs.ReadByte();
					if ((fw < 2)) cUnit = new Axy4_1(this);
					else cUnit = new Axy4_2(this);
					break;
				case Unit.model_axy5:
					cUnit = new Axy5(this);
					break;
				case Unit.model_axyDepth:
					if (fileType == type_ard) fw = (byte)fs.ReadByte();
					if (fw < 2)
					{
						cUnit = new AxyDepth_1(this);
					}
					else
					{
						cUnit = new AxyDepth_2(this);
					}
					break;
				case Unit.model_axyTrek:
					cUnit = new AxyTrek(this);
					break;
				case Unit.model_AGM1:
					cUnit = new AGM(this);
					break;
				case Unit.model_Co2Logger:
					cUnit = new CO2_Logger(this);
					break;
				case Unit.model_Gipsy6:
					cUnit = new Gipsy6(this);
					break;
				default:
					cUnit = new AxyTrek(this);
					break;
			}

			UInt32 FileLength = (UInt32)fs.Length;
			fs.Close();
			Thread conversionThread;
			if ((fileType == type_ard) | (fileType == type_rem))
			{
				conversionThread = new Thread(() => cUnit.convert(fileName, newPrefs));
			}
			else
			{
				conversionThread = new Thread(() => cUnit.extractArds(fileName, fileName, false));
			}

			conversionThread.SetApartmentState(ApartmentState.STA);
			conversionThread.Start();

			System.Windows.Threading.Dispatcher.Run();
		}

		private byte[] findMdpModel(ref FileStream iFile)
		{
			long pos = iFile.Position;
			byte[] outt = new byte[2];
			iFile.Position = iFile.Length - 1;
			if (iFile.ReadByte() == 254)
			{
				iFile.Position = iFile.Length - 2;
				outt[0] = (byte)iFile.ReadByte();
				iFile.Position = iFile.Length - 5;
				outt[1] = (byte)iFile.ReadByte();
			}
			else
			{
				outt[0] = 0xff;
				outt[1] = 0xff;
				iFile.Close();
				throw new Exception("Unknown mdp model");
			}
			iFile.Position = pos;
			return outt;
		}

		public override void downloadFinished()
		{

		}

		public override void downloadFailed()
		{

		}
		private string lRead(List<string> li)
		{
			string lo = li[0];
			li.RemoveAt(0);
			return lo;
		}

		private void exit()
		{
			Environment.Exit(-1);
		}

	}
}
