using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Threading;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Media;
using System.Net;
using Microsoft.VisualBasic;

namespace X_Manager.Remote
{
	/// <summary>
	/// Logica di interazione per BS_Main.xaml
	/// </summary>

	public class HTTP
	{
		public const string COMMAND_ERROR = "0xFFFFFFFF";
		public const string COMMAND_GET_VERSION = "getversion";
		public const string COMMAND_GET_USERSLIST = "getuserslist";
		public const string COMMAND_CREATE_NEW_USER = "createuser";
		public const string COMMAND_GET_USER_ID = "getuserid";
		public const string COMMAND_CREATE_BASESTATION = "createbasestation";
		public const string COMMAND_RENAME_BASESTATION = "renamebasestation";

		static string un = "bs001";
		static string pw = "pwd_BS001";
		static string url = "www.technosmart-tracking.com/BS_Scripts/ts_bsapi.php?OPER=";
		public static string cOut(string c)
		{
			string cComp = String.Format("https://{0}{1}", url, c);
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(cComp);
			request.Credentials = new NetworkCredential(un, pw);
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			Stream stream = response.GetResponseStream();
			StreamReader reader = new StreamReader(stream);
			return reader.ReadToEnd();
		}
	}

	public partial class BS_Main : Window
	{

		string currentDriveLetter = "";
		string currentDriveLabel = "";
		List<BS_listViewElement[]> historyList;
		TextBox newChannelTB;
		System.Timers.Timer saveAddressTimer;
		System.Timers.Timer saveScheduleTimer;
		System.Timers.Timer listDriveTimer;
		System.Timers.Timer validateDriveTimer;
		List<string> oldDriveLabels;
		DriveInfo currentSelectedDrive;
		//volatile bool saving = false;
		//volatile bool closingP = false;
		BS_listViewElement tempBsLvElement;
		int tempBsLvIndex;
		int bsAddress;
		List<CheckBox> scheduleCBArr;
		byte[] oldScheduleArr;
		DriveStatus oldDriveStatus;
		byte[] timestamp;
		byte[] oldConf;
		int oldAddress;

		private enum DriveStatus
		{
			NOT_SELECTED,
			NOT_VALID,
			TO_BE_FORMATTED,
			VALID
		}

		const string FILE_CONF = "BS_CONF.dat";
		const string FILE_NAME = "BS_NAME.txt";
		const string FILE_SCHEDULE = "BS_SCHEDULE.dat";
		const string FILE_UNITS = "BS_UNITS.dat";
		const string FOLDER_CONFIG = "CONFIG";

		bool save = false;
		bool somethingChanged = false;
		string intialName = "";
		readonly Control[] visArr;
		private readonly Object addressLock = new Object();
		private readonly Object scheduleLock = new Object();
		FTDI_Device ft;

		public BS_Main()
		{
			InitializeComponent();
			Point p = new Point(0, 0);

			ft = MainWindow.FTDI;

			//Crea un elenco di controlli da rendere visibili o invisibile a seconda che l'unità selezionata sia o meno una basestation
			visArr = new Control[] { channelListGB, scheduleGB, plusB, minusB, plusPlusB, undoB, allOnB, allOffB, confGB, saveB, bootloaderB, sendTimeB };

			Loaded += loaded;
			SizeChanged += sizeChanged;
			Closing += closing;
			historyList = new List<BS_listViewElement[]>();
			currentSelectedDrive = null;

			//Imposta il timer per il salvataggio della unit list a seguito di modifiche
			saveAddressTimer = new System.Timers.Timer();
			saveAddressTimer.Interval = 1000;
			saveAddressTimer.Elapsed += saveAddressTimerElapsed;

			//Imposta i timer per il salvataggio dello schedule a seguito di modifiche
			saveScheduleTimer = new System.Timers.Timer();
			saveScheduleTimer.Interval = 1100;
			saveScheduleTimer.Elapsed += saveScheduleTimerElapsed;

			//Imposta il timer per il refresh periodico della lista unità
			listDriveTimer = new System.Timers.Timer();
			listDriveTimer.Interval = 500;
			listDriveTimer.AutoReset = false;
			listDriveTimer.Elapsed += listDriveTimerElapsed;

			//Imposta il timer per il controllo periodico intergrità driver selezionato
			validateDriveTimer = new System.Timers.Timer();
			validateDriveTimer.Interval = 500;
			validateDriveTimer.AutoReset = false;
			validateDriveTimer.Elapsed += validateDriveTimerElapsed;

			//Aggiunge le 24 checkbox per lo schedule
			Thickness thickness = new Thickness(5, 20, 0, 0);
			scheduleCBArr = new List<CheckBox>();
			for (int i = 0; i < 24; i++)
			{
				if ((i == 8) || (i == 16))
				{
					thickness.Left += 55;
					thickness.Top = 20;
				}

				var chb = new CheckBox();
				chb.Name = "N" + i.ToString();
				chb.VerticalAlignment = VerticalAlignment.Top;
				chb.HorizontalAlignment = HorizontalAlignment.Left;
				chb.Margin = thickness;
				chb.Content = i.ToString();
				chb.HorizontalContentAlignment = HorizontalAlignment.Left;
				chb.Padding = new Thickness(0);
				chb.FontSize = 10;
				chb.Checked += scheduleCBchanged;
				chb.Unchecked += scheduleCBchanged;
				scheduleG.Children.Add(chb);
				scheduleCBArr.Add(chb);
				thickness.Top += 40;
			}

			//Invalida l'indirizzo di ricezzione
			bsAddress = -1;

		}

		#region INTERFACCIA

		private void loaded(Object sender, RoutedEventArgs e)
		{

			//Nasconde tutti i controlli relativi allo schedule perché in avvio non è stata selezionata alcuna basestation
			selectedDriveAspect(DriveStatus.NOT_SELECTED);

			int pos = listDrive();        //Elenca i drive connessi al pc nella listview dei drive
										  //Imposta le funzioni per gli eventi di finestra

			//Se almeno uno dei drive è una basestation la seleziona			
			if (pos != -1)
			{
				try
				{
					driveLV.SelectedIndex = pos;
					lock (addressLock)
					{
						lock (scheduleLock)
						{
							driveLvItemClicked();
						}
					}
				}
				catch { }
			}
			for (int i = 0; i < driveLV.Items.Count; i++)
			{
				if (validateDrive(((BS_listViewElement)driveLV.Items[i]).Drive))
				{
					driveLV.SelectedIndex = i;
					lock (addressLock)
					{
						lock (scheduleLock)
						{
							driveLvItemClicked();
						}
					}
					break;
				}
			}

			//Fa partire il timer per il controllo periodico delle unità connesse
			listDriveTimer.Start();

			//Fa partire il timer per il controllo periodico dell'integrità dell'unità selezionata
			validateDriveTimer.Start();

			string sopww = (HTTP.cOut(HTTP.COMMAND_GET_VERSION));
			//MessageBox.Show(sopww);
		}

		private void sizeChanged(object sender, SizeChangedEventArgs e)
		{
			//Evento per la gestione del ridimensionamento della finestra; gli elementi delle listview vengono riadattati alla nuova dimensione
			//delle listview
			resizeLVelements();
		}

		private void resizeLVelements()
		{
			var newWidth = driveLV.ActualWidth;
			foreach (BS_listViewElement element in driveLV.Items)
			{
				element.Width = newWidth - 15;
			}

			newWidth = channelLV.ActualWidth;
			foreach (BS_listViewElement element in channelLV.Items)
			{
				element.Width = newWidth - 15;
			}
			if (newChannelTB != null)
			{
				newChannelTB.Width = channelLV.Width;
			}
		}

		private void disableControls(FrameworkElement c)
		{
			//Vengono disabilitati tutti i controllo nella finestra
			if (c is Panel)
			{
				foreach (FrameworkElement cIn in ((Panel)c).Children)
				{
					disableControls(cIn);
				}
			}
			else
			{
				if (c is Button)
				{
					c.IsEnabled = false;
				}
			}
		}

		private void enableControls(FrameworkElement c)
		{
			////Vengono abilitati tutti i controllo nella finestra
			if (c is Panel)
			{
				foreach (FrameworkElement cIn in ((Panel)c).Children)
				{
					enableControls(cIn);
				}
			}
			else
			{
				if (c is Button)
				{
					c.IsEnabled = true;
				}
			}
		}

		private void selectedDriveAspect(DriveStatus s)
		{
			oldDriveStatus = s;
			Color col = new Color();
			formatB.Visibility = Visibility.Hidden;
			switch (s)
			{
				case DriveStatus.NOT_SELECTED:

					foreach (Control c in visArr)
					{
						c.Visibility = Visibility.Hidden;
					}
					notValidL.Visibility = Visibility.Visible;
					notValidL.Content = "Please, select a drive\r\nfrom the list.";
					col = Color.FromArgb(255, 0xa0, 0xa0, 0xa0);
					break;
				case DriveStatus.NOT_VALID:
					foreach (Control c in visArr)
					{
						c.Visibility = Visibility.Hidden;
					}
					notValidL.Visibility = Visibility.Visible;
					notValidL.Content = "Not a valid BaseStation drive.";
					col = Color.FromArgb(255, 0xba, 0xba, 0xba);
					break;
				case DriveStatus.TO_BE_FORMATTED:
					foreach (Control c in visArr)
					{
						c.Visibility = Visibility.Hidden;
					}
					notValidL.Visibility = Visibility.Visible;
					notValidL.Content = "This drive needs to be\r\nformatted in order\r\nto work as a valid\r\nBaseStation";
					formatB.Visibility = Visibility.Visible;
					col = Color.FromArgb(255, 220, 180, 0);
					break;
				case DriveStatus.VALID:
					foreach (Control c in visArr)
					{
						c.Visibility = Visibility.Visible;
					}
					notValidL.Visibility = Visibility.Hidden;
					col = Color.FromArgb(255, 0, 220, 0);
					break;
			}
			try
			{
				(driveLV.SelectedItem as BS_listViewElement).Foreground = new SolidColorBrush(col);
			}
			catch { }
		}

		private void bootloader_Click(object sender, RoutedEventArgs e)
		{
			listDriveTimer.Stop();
			saveAddressTimer.Stop();
			saveScheduleTimer.Stop();
			validateDriveTimer.Stop();
			if (ft is null)
			{
				MessageBox.Show("No serial data cable detected.");
				listDriveTimer.Start();
				validateDriveTimer.Start();
				return;
			}
			var bl = new Bootloader.Bootloader_Gipsy6(true, "BaseStation");

			bl.ShowDialog();
			listDriveTimer.Start();
			validateDriveTimer.Start();
		}

		private void sendTimestamp_click(object sender, RoutedEventArgs e)
		{
			//var drive = (driveLV.SelectedItem as BS_listViewElement).Drive;
			//byte[] conf = File.ReadAllBytes(drive.Name + FILE_CONF);
			//conf[0x12] = 1;
			timestamp = new byte[8];
			DateTime dt = DateTime.UtcNow;

			timestamp[0] = 1;
			timestamp[1] = reverseByte(dec2BCD((byte)(dt.Year - 2000)));
			timestamp[2] = reverseByte(dec2BCD((byte)dt.Month));
			timestamp[3] = reverseByte(dec2BCD((byte)dt.Day));
			timestamp[4] = reverseByte(dec2BCD((byte)dt.DayOfWeek));
			timestamp[5] = reverseByte(dec2BCD((byte)dt.Hour));
			timestamp[6] = reverseByte(dec2BCD((byte)dt.Minute));
			timestamp[7] = reverseByte(dec2BCD((byte)dt.Second));

			//File.WriteAllBytes(drive.Name + FILE_CONF, conf);
			MessageBox.Show("Date and time set.");
		}

		private void saveB_Click(object sender, RoutedEventArgs e)
		{
			string num = bsAddressTB.Text;
			var ns = System.Globalization.NumberStyles.Integer;
			if (num.IndexOf("0x", StringComparison.InvariantCultureIgnoreCase) != -1)
			{
				ns = System.Globalization.NumberStyles.HexNumber;
				num = num.Remove(0, 2);
				num = num.ToLower();
			}
			int.TryParse(num, ns, System.Globalization.CultureInfo.InvariantCulture, out bsAddress);
			if (bsAddress == 0 || bsAddress == 555 || bsAddress >= 0xffff00)
			{
				bsAddress = -1;
			}
			if (bsAddress == -1)
			{
				MessageBox.Show("Invalid Receiving Address!");
				return;
			}
			save = true;
			Close();
		}

		private void closing(object sender, System.ComponentModel.CancelEventArgs e)
		{

			saveAddressTimer.Stop();
			saveScheduleTimer.Stop();
			listDriveTimer.Stop();
			validateDriveTimer.Stop();
			if (!bsNameTB.Text.Equals(intialName))
			{
				somethingChanged = true;
			}
			if (somethingChanged && !save)
			{
				var tg = new YesNo("WARNING: Do you want to save changes before closing?", "Unsaved Changes", "", "Yes", "No");
				if (tg.ShowDialog() == 1) save = true;
			}
			if (save)
			{
				lock (addressLock)
				{
					lock (scheduleLock)
					{
						disableControls(MainGrid);
						try
						{
							saveNameConfigurationDate();
							saveSchedule();
							saveUnitList();
						}
						catch (Exception ex)
						{
							MessageBox.Show(ex.Message);
						}
					}
				}
			}
			saveAddressTimer = null;
			saveScheduleTimer = null;
			listDriveTimer = null;
			validateDriveTimer = null;
		}

		#endregion

		#region DRIVE
		private int listDrive()
		{
			int res = -1;
			int sum = 0;
			undoB.Content = "<-";
			var dl = DriveInfo.GetDrives();             // Ottiene una lista dei drive connessi al pc
			oldDriveLabels = new List<String>();         /* Istanzia una lista per memorizzare le label dei drive
														  per il successivo confronto temporizzato */
			for (int i = 0; i < dl.Count(); i++)
			{
				{
					try
					{
						/*Tenta di creare una listviewElement per ogni drive connesso. Se il drive non è disponibile,
						non viene aggiunto e si passa al successivo */
						var lve = new BS_listViewElement(dl[i]);
						oldDriveLabels.Add(dl[i].VolumeLabel);
						if (dl[i].VolumeLabel.IndexOf("basestation", StringComparison.OrdinalIgnoreCase) >= 0)
						//if (dl[i].VolumeLabel.Contains("BaseStation")   )
						{
							if (validateDrive(dl[i]))
							{
								if (res == -1) res = i;
								lve.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 220, 0));
							}
							else
							{
								if (dl[i].DriveFormat == "FAT32")
								{
									lve.Foreground = new SolidColorBrush(Color.FromArgb(255, 220, 180, 0));
								}
							}
						}
						driveLV.Items.Add(lve);
					}
					catch
					{
						oldDriveLabels.Add("");
						if (res == -1)
						{
							sum++;
						}
					}
				}
			}
			return res - sum;
		}

		private void listDriveTimerElapsed(Object source, ElapsedEventArgs e)
		{
			listDriveTimer.Stop();      //Ferma il timer

			lock (addressLock)
			{
				lock (scheduleLock)
				{
					//Controllo se i drive connessi al pc sono cambiati rispetto alla lista storica
					DriveInfo[] newDriveList = DriveInfo.GetDrives();
					List<string> newDriveLabels = new List<string>();
					for (int i = 0; i < newDriveList.Length; i++)
					{
						try
						{
							newDriveLabels.Add(newDriveList[i].VolumeLabel);
						}
						catch
						{
							newDriveLabels.Add("");
						}
					}

					bool changed = true;
					if (newDriveLabels.Count == oldDriveLabels.Count)
					{
						changed = false;
						for (int i = 0; i < newDriveLabels.Count; i++)
						{
							try
							{
								if (!oldDriveLabels[i].Equals(newDriveLabels[i]))
								{
									changed = true;
									break;
								}
							}
							catch { }
						}
					}

					if (changed)        //Se la lista è cambiata:
					{
						validateDriveTimer.Stop();

						//Acquisisce il blocco per evitare l'esecuzione di altri comandi			

						//Salva la nuova lista nella lista storica

						string[] temp = new string[newDriveLabels.Count];
						newDriveLabels.CopyTo(temp);
						oldDriveLabels = temp.ToList();
						//Pulisce la listview dei drive e la riempie con la nuova lista
						int newLength = 0;
						Application.Current.Dispatcher.Invoke(() => driveLV.Items.Clear());
						Application.Current.Dispatcher.Invoke(() => listDrive());
						Application.Current.Dispatcher.Invoke(() => newLength = driveLV.Items.Count);

						//Nel caso prima del refresh fosse selezionato un drive (memorizzato in currentSelectedDrive), prova a riselezionarlo
						try
						{
							if (currentSelectedDrive != null)
							{
								string driveName = "";
								string compDriveName = "!";
								Application.Current.Dispatcher.Invoke(() => compDriveName = currentSelectedDrive.Name);
								bool click = false;
								//Confronta i nomi della nuova lista di drive con quello memorizzato come precedentemente selezionato
								for (int i = 0; i < newLength; i++)
								{
									try
									{
										Application.Current.Dispatcher.Invoke(() => driveName = ((BS_listViewElement)driveLV.Items[i]).Drive.Name);
										//Se trova un riscontro positivo nella listview, imposta l'elemento della listview come selezionato
										if (driveName.Equals(compDriveName))
										{
											click = true;
											Application.Current.Dispatcher.Invoke(() => driveLV.SelectedIndex = i);
											break;
										}
									}
									catch { }
								}
								//Se un elemento è stato selezionato, viene invocata la funzione che analizza il drive selezionato
								if (click)
								{
									Application.Current.Dispatcher.Invoke(() => driveLvItemClicked());
								}
								else
								{
									Application.Current.Dispatcher.Invoke(() => driveLV.SelectedIndex = -1);
									Application.Current.Dispatcher.Invoke(() => selectedDriveAspect(DriveStatus.NOT_SELECTED));

								}
							}
						}
						catch
						{
							//Se la riselezione non va a buon fine, vene selezionato e analizzato il primo elemento della lsitview (se non vuota)
							if (driveLV.Items.Count > 0)
							{
								Application.Current.Dispatcher.Invoke(() => driveLV.SelectedIndex = 0);
								Application.Current.Dispatcher.Invoke(() => driveLvItemClicked());
							}

						}
						if (!(listDriveTimer is null))
						{
							validateDriveTimer.Start();
						}
					}
				}
			}
			//Fa ripartire il timer per il controllo periodico dei drive
			if (!(listDriveTimer is null))
			{
				listDriveTimer.Start();
			}
		}

		private void validateDriveTimerElapsed(Object source, ElapsedEventArgs e)
		{

			validateDriveTimer.Stop();
			lock (addressLock)
			{
				lock (scheduleLock)
				{
					Application.Current.Dispatcher.Invoke(() => validateDriveTimerElapsedDelegate());
				}
			}
			if (!(validateDriveTimer is null))
			{
				validateDriveTimer.Start();
			}
		}

		private void validateDriveTimerElapsedDelegate()
		{
			if (driveLV.SelectedIndex < 0)
			{
				return;
			}

			DriveStatus d = DriveStatus.VALID;

			if (validateDrive((driveLV.SelectedItem as BS_listViewElement).Drive))
			{
				d = DriveStatus.VALID;
			}
			else
			{
				if ((driveLV.SelectedItem as BS_listViewElement).Drive.VolumeLabel.IndexOf("basestation", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					if ((driveLV.SelectedItem as BS_listViewElement).Drive.DriveFormat == "FAT32")
					{
						d = DriveStatus.TO_BE_FORMATTED;
					}
				}
				else
				{
					d = DriveStatus.NOT_VALID;
				}
			}
			if (d != oldDriveStatus)
			{
				if (d == DriveStatus.VALID)
				{
					fillChannelList((driveLV.SelectedItem as BS_listViewElement).Drive);
					fillScheduleList((driveLV.SelectedItem as BS_listViewElement).Drive);
					fillConf((driveLV.SelectedItem as BS_listViewElement).Drive);
				}
				selectedDriveAspect(d);
			}
		}

		private void driveLV_MouseUp(object sender, MouseButtonEventArgs e)
		{
			//Click sulla listview dei drive

			if (driveLV.SelectedItem == null) return;   //Se è stata cliccata una parte vuota della listview, esce subito

			//Altrimenti acquisisce il blocco dei comandi e analizza il drive selezionato
			lock (addressLock)
			{
				lock (scheduleLock)
				{
					driveLvItemClicked();
				}
			}
		}

		private void driveLvItemClicked()
		{

			var drive = ((BS_listViewElement)driveLV.SelectedItem).Drive;
			currentSelectedDrive = ((BS_listViewElement)driveLV.SelectedItem).Drive;    //Crea una copia del riferimento al drive selezionato
			if (drive == null) return;
			/*Confronta il nome del drive cliccato con quello precedentemente selezionato,
			 * *se è lo stesso esce subito */
			if (currentDriveLetter == drive.Name)
			{
				if (currentDriveLabel == drive.VolumeLabel)
				{
					return;
				}
			}

			currentDriveLetter = drive.Name;                //Se è diverso, aggiorna il nome
			currentDriveLabel = drive.VolumeLabel;

			if (validateDrive(drive))
			{
				//Se il drive selezionato è una BaseStation, rende visibili i controlli di lavoro
				selectedDriveAspect(DriveStatus.VALID);

				//Resetta il flag di cambiamenti effettuati
				somethingChanged = false;

				fillChannelList(drive);         //Riempie la lista unità
				fillScheduleList(drive);        //Imposta gli orari dello schedule
				fillConf(drive);                //Riempe i campi della configurazione
				resizeLVelements();
			}
			//else if (drive.VolumeLabel.Contains("BaseStation") || drive.VolumeLabel.Contains("BASESTATION"))
			else if (drive.VolumeLabel.IndexOf("basestation", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				if (drive.DriveFormat == "FAT32")
				{
					selectedDriveAspect(DriveStatus.TO_BE_FORMATTED);
				}
			}
			else
			{
				//Altrimenti rende invisibili i controlli di lavoro
				selectedDriveAspect(DriveStatus.NOT_VALID);
			}
			if (!(validateDriveTimer is null))
			{
				validateDriveTimer.Start();
			}
		}

		private bool validateDrive(DriveInfo drive)
		{
			/*Controlla che il drive selezionato soddisfi tutti i requisiti della BaseStation (Formattazione, Etichetta di volume e presenza
			 * di file caratteristici)
			 * Restituisce true se il drive è una BaseStation, altrimenti false			 */
			try
			{
				if (drive.DriveFormat != "FAT32") return false;
				if (drive.VolumeLabel.IndexOf("basestation", StringComparison.OrdinalIgnoreCase) < 0) return false;
				if (!File.Exists(drive.Name + FILE_CONF)) return false;
				if (!File.Exists(drive.Name + FILE_NAME)) return false;
				if (!File.Exists(drive.Name + FILE_SCHEDULE)) return false;
				if (!File.Exists(drive.Name + FILE_UNITS)) return false;
				if (!Directory.Exists(drive.Name + FOLDER_CONFIG)) return false;
			}
			catch
			{
				return false;
			}
			return true;
		}

		#endregion

		#region INDIRIZZI

		private void fillChannelList(DriveInfo d)
		{
			channelLV.Items.Clear();
			byte[] buff = File.ReadAllBytes(d.Name + FILE_UNITS);   //Carica in memoria il contenuto del file unità
			if (buff.Length == 0)                                   //Se non ci sono unità esce subito
			{
				return;
			}
			var s = buff.Skip(8).Take(8).ToArray();                 //Carica il numero delle unità
			Array.Reverse(s);
			UInt64 units = BitConverter.ToUInt64(s, 0);
			int counter = 0x10;
			var addsIn = new List<Tuple<int, bool>>();      //Imposta una lista di tuple che rappresentano le unità da scaricare
			int add;
			//Legge gruppi da 16 byte (un gruppo per ogni unità)
			for (uint i = 0; i < units; i++)
			{
				add = buff[counter] * 65536 + buff[counter + 1] * 256 + buff[counter + 2];  //Carica l'indirizzo dell'unità
				bool green = buff[counter + 4] == 1 ? true : false;     //Carica il colore dell'unità
				int pos = 0;
				for (int j = 0; j < addsIn.Count; j++)  //Calcola la posizione in cui inserire la nuova unità nella lista
				{
					if (add < addsIn[j].Item1)
					{
						break;
					}
					pos++;
				}
				addsIn.Insert(pos, new Tuple<int, bool>(add, green));   /*inserisce la nuova unità nella posizione calcolata
				                                                     	 * in modo che la lista sia sempre in ordine crescente */
				counter += 0x10;
			}

			//Inserisce tutte le unità della lista nella listview degli indirizzi
			for (int i = 0; i < (int)units; i++)
			{
				var lve = new BS_listViewElement(addsIn[i].Item1, addsIn[i].Item2);
				//Se per l'unità corrente esiste un file di configurazione:
				if (File.Exists(d.Name + "\\CONFIG\\" + lve.Address.ToString("00000000") + ".cfg"))
				{
					if (buff[((i + 1) * 0x10) + 5] == 1)    //Se è anche presente il flag carica la configurazione e
					{
						byte[] conf = File.ReadAllBytes(d.Name + "\\CONFIG\\" + lve.Address.ToString("00000000") + ".cfg");
						lve.NewConf = conf;
					}
					else                                    //Altrimenti cancella il file
					{
						File.Delete(d.Name + "\\CONFIG\\" + lve.Address.ToString("00000000") + ".cfg");
					}
				}
				else //Se il file non esiste ma è presente la spunta, la toglie perché è un errore
				{
					if (buff[((i + 1) * 0x10) + 5] == 1)
					{
						buff[((i + 1) * 0x10) + 5] = 0;
					}
				}

				//Se per l'unità corrente esiste un file di nuovo nome:
				if (File.Exists(d.Name + "\\CONFIG\\" + lve.Address.ToString("00000000") + ".nnm"))
				{
					if (buff[((i + 1) * 0x10) + 6] == 1)    //Se è anche presente il flag carica la configurazione e
					{
						string name = File.ReadAllText(d.Name + "\\CONFIG\\" + lve.Address.ToString("00000000") + ".nnm");
						lve.NewName = name;
					}
					else                                    //Altrimenti cancella il file
					{
						File.Delete(d.Name + "\\CONFIG\\" + lve.Address.ToString("00000000") + ".nnm");
					}
				}
				else //Se il file non esiste ma è presente la spunta, la toglie perché è un errore
				{
					if (buff[((i + 1) * 0x10) + 6] == 1)
					{
						buff[((i + 1) * 0x10) + 6] = 0;
					}
				}

				channelLV.Items.Add(lve);

				counter += 0x10;
			}

			//Aggiorna il file con la lista unità (nel caso fossero state tolte spunte)
			File.WriteAllBytes(d.Name + FILE_UNITS, buff);

			//Reinizializza la lista degli undo
			historyList.Clear();

		}

		private void plusB_Click(object sender, RoutedEventArgs e)
		{
			//Fa comparire una textBox di input a fondo listview per inserire un nuovo indirizzo
			plusB.Click -= plusB_Click;
			var tb = new TextBox();
			Grid.SetColumn(tb, 1);
			tb.Width = channelListGB.ActualWidth;
			tb.VerticalAlignment = VerticalAlignment.Bottom;
			tb.Background = new SolidColorBrush(Colors.White);
			tb.Margin = new Thickness(5, 0, 5, 35);
			tb.Name = "plusTB";
			tb.PreviewKeyDown += plusTb_KeyDown;  //Aggiunge l'evento di pressione del tasto per intercettare ESC o INVIO (vedi funzione seguente)
			newChannelTB = tb;
			MainGrid.Children.Add(tb);
			tb.Focus();
		}

		private void plusTb_KeyDown(object sender, KeyEventArgs e)
		{
			var tb = (TextBox)sender;
			//E' stato premuto INVIO:
			if (e.Key == Key.Enter)
			{
				int newCh = -1;
				if (int.TryParse(tb.Text, out newCh))   //Se è stato inserito del testo numerico valido, newCh assume il valore numerico inserito, altrimenti rimane a -1
				{
					if (newCh >= 0)     //Se il testo inserito è un numero:
					{
						bool add = true;
						int pos = 0;
						//Cicla tra gli indirizzi già esistenti; , altrimenti lo inserisce in ordine crescente
						for (int i = 0; i < channelLV.Items.Count; i++)
						{
							BS_listViewElement lv = (BS_listViewElement)channelLV.Items[i];
							if (int.Parse(lv.Text) == newCh)            //se il nuovo indirizzo esiste esce senza inserirlo
							{
								add = false;
								break;
							}
							else if (newCh < int.Parse(lv.Text))        //altrimenti calcola la posizione in cui inserirlo nella lista, per rispettare l'ordine crescente
							{
								add = true;
								break;
							}
							pos++;
						}
						if (add)                //Inserisce il nuovo indirizzo nella posizione calcolata
						{
							somethingChanged = true;
							undo_addItem();  //Viene aggiunto un livello di undo
							var lvv = new BS_listViewElement(newCh, false);
							lvv.Width = channelLV.ActualWidth - 15;
							channelLV.Items.Insert(pos, lvv);
							channelLV.ScrollIntoView(lvv);      //Fa scorrere la listview per inquadrare il nuovo indirizzo
																//lock (addressLock)
																//{
																//	saveUnitList();
																//}
						}
					}
				}
			}
			if (e.Key == Key.Enter || e.Key == Key.Escape)      //Se sono stati premuti INVIO o ESC, chiude la inputBox creata per inserire il nuovo indirizzo
			{
				tb.PreviewKeyDown -= plusTb_KeyDown;
				MainGrid.Children.Remove(tb);
				e.Handled = true;
				plusB.Click -= plusB_Click;
				plusB.Click += plusB_Click;
				newChannelTB = null;
				tb = null;          //La inputBox viene dereferenziata per essere pulita dal garbage collector
			}
		}

		private void plusPlusB_Click(object sender, RoutedEventArgs e)
		{
			undo_addItem();
			if (channelLV.Items.Count == 0)
			{
				channelLV.Items.Insert(0, new BS_listViewElement(1, false));
				channelLV.SelectedIndex = 0;
				return;
			}
			if (channelLV.SelectedItem == null)
			{
				//Se non è stato selezionato alcun indirizzo, forza la selezione all'ultimo della lista
				channelLV.SelectedItem = channelLV.Items[channelLV.Items.Count - 1];
			}
			int pos = channelLV.SelectedIndex;
			int val = int.Parse(((BS_listViewElement)channelLV.SelectedItem).Text) + 1; //Aggiunge 1 all'indirizzo selezionato
			{
				while (pos < channelLV.Items.Count - 1)
				{
					//Cicla tra tutti gli indirizzi successivi a quello selezionato, incrementanto di volta in volta il nuovop indirizzo
					int compVal = int.Parse(((BS_listViewElement)channelLV.Items[pos + 1]).Text);
					if (val == compVal)
					{
						val++;
						pos++;
					}
					else
					{
						//Si ferma quando il nuovo indirizzo calcolato non viene trovato nella lista
						break;
					}
				}
				somethingChanged = true;
				pos++;
				//Inserisce il nuovo indirizzo nella posizione calcolata
				channelLV.Items.Insert(pos, new BS_listViewElement(val, false));
				channelLV.SelectedIndex = pos;
				//Fa partire il timer per il salvataggio automatico
				saveAddressTimer.Stop();
				//saveAddressTimer.Start();
			}
		}

		private void minusB_Click(object sender, RoutedEventArgs e)
		{
			undo_addItem();      //Viene salvato un undo

			//Memorizza la posizione dell'indirizzo precedente a quello attualmente selezionato
			int oldPos = channelLV.SelectedIndex;
			oldPos--;
			if (oldPos == -1) oldPos = 0;   //Eventualmente satura la nuova posizione a zero

			//Cicla tra tutti gli elementi selezionati per riimuoverli
			while (channelLV.SelectedItems.Count > 0)
			{
				channelLV.Items.Remove(channelLV.SelectedItems[0]);
				somethingChanged = true;
			}
			//Prova a selezionare l'elemento presente alla posizione salvata prima
			try
			{
				channelLV.SelectedIndex = oldPos;
			}
			catch { }
			//Fa partire il timer per il salvataggio automatico
			saveAddressTimer.Stop();
			//saveAddressTimer.Start();
		}

		private void channelLV_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (channelLV.SelectedIndex < 0) return;
			undo_addItem();      //Si salva un undo

			channelLV.MouseDoubleClick -= channelLV_MouseDoubleClick;       //Viene rimosso l'interrupt, sarà reinserito a fine funzione
			channelLV.MouseRightButtonUp -= channelLV_MouseRightClick;       //Viene rimosso l'interrupt, sarà reinserito a fine funzione
			tempBsLvElement = (BS_listViewElement)channelLV.SelectedItem;   //Vengono salvati riferimento e posizione dell'elemento da modificare
			tempBsLvIndex = channelLV.SelectedIndex;
			var index = channelLV.SelectedIndex;
			var tb = new TextBox();                                         //Viene creata una nuova textbox 
			tb.Background = new SolidColorBrush(Colors.White);
			tb.Width = ((BS_listViewElement)channelLV.SelectedItem).ActualWidth;
			tb.Height = ((BS_listViewElement)channelLV.SelectedItem).ActualHeight;
			tb.Text = tempBsLvElement.Address.ToString();                   //Si inserisce il testo originale dell'elemento da modificare
			oldAddress = tempBsLvElement.Address;							//Salva l'indirizzo che sarà modificato
			oldConf = null;
			if (tempBsLvElement.NewConf != null)                            //Salva l'eventuale nuovo schedule per associarlo al nuovo indirizzo
			{
				oldConf = new byte[tempBsLvElement.NewConf.Length];
				Array.Copy(tempBsLvElement.NewConf, 0, oldConf, 0, oldConf.Length);
			}
			tb.PreviewKeyDown += doubleClick_TextBoxKD;                     //Si aggiunge l'evento di keydown per intercettare INVIO
			channelLV.Items.RemoveAt(index);                                //Viene rimosso l'elemento da modificare
			disableControls(MainGrid);                                      //Si disattivano tutto gli altri controlli
			channelLV.Items.Insert(index, tb);                              //Nella stessa posizione viene aggiunta la casella di testo
			tb.Loaded += tbAddLoaded;
		}

		private void tbAddLoaded(object sender, RoutedEventArgs e)
		{
			Keyboard.Focus(sender as TextBox);
			(sender as TextBox).SelectAll();
		}

		private void doubleClick_TextBoxKD(object sender, KeyEventArgs e)
		{
			var tb = (TextBox)sender;
			bool add = false;
			//E' stato premuto INVIO:
			if (e.Key == Key.Enter)
			{
				//e.Handled = true;
				channelLV.Items.RemoveAt(tempBsLvIndex);    //La casella di input viene rimossa
				int newCh = -1;
				if (int.TryParse(tb.Text, out newCh))       //Se il valore inserito è valido e maggiore o uguale a zero si prosegue
				{
					if (newCh >= 0)
					{
						add = true;
						int pos = 0;
						var forEnd = channelLV.Items.Count;
						if (newCh == tempBsLvElement.Address)
						{
							//channelLV.Items.Insert(tempBsLvIndex, tempBsLvElement);
							add = false;
							forEnd = 0;
						}
						for (int i = 0; i < forEnd; i++) //Si cicla per tutti gli elementi esistenti nella listview
						{
							BS_listViewElement lv = (BS_listViewElement)channelLV.Items[i];
							if (int.Parse(lv.Text) == newCh)        //Se il valore inserito era già esistente, si esce senza aggiungerelo
							{
								//channelLV.Items.Insert(tempBsLvIndex, tempBsLvElement);
								add = false;
								break;
							}
							else if (newCh < int.Parse(lv.Text))    //Altrimenti, viene calcolata la posizione in cui inserire il nuovo elemento
							{
								add = true;
								break;
							}
							pos++;
							if (i == channelLV.Items.Count - 1)     /*Se si è raggiunta la fine della lista, la posizione in cui inserire è in fondo alla
							                                   		 * lista	*/
							{
								add = true;
							}
						}
						if (add)
						{
							somethingChanged = true;
							//Si crea il nuovo elemento e si inserisce nella posizione calcolata in precedenza
							tempBsLvElement = new BS_listViewElement(newCh, false);
							//Si controlla se al vecchio indirizzo era associato uno schedule da inviare
							if (oldConf != null)
							{
								//Se il file con lo schedule era presente lo cancella
								var dr = ((BS_listViewElement)driveLV.SelectedItem).Drive;
								if (File.Exists(dr.Name + FOLDER_CONFIG + "\\" + oldAddress.ToString("00000000") + ".cfg"))
								{
									File.Delete(dr.Name + FOLDER_CONFIG + "\\" + oldAddress.ToString("00000000") + ".cfg");
								}
								oldConf[541] = (byte)(newCh >> 16);
								oldConf[542] = (byte)(newCh >> 8);
								oldConf[543] = (byte)(newCh & 0xff);
								tempBsLvElement.NewConf = oldConf;
								oldConf = null;
							}
							tempBsLvElement.Width = channelLV.ActualWidth - 15;
							channelLV.Items.Insert(pos, tempBsLvElement);
							channelLV.ScrollIntoView(tempBsLvElement);
							//Si salva su basestation la nuova lista
							//lock (addressLock)
							//{
							//	saveUnitList();
							//}
						}
					}
				}
			}
			if (e.Key == Key.Escape)
			{
				channelLV.Items.RemoveAt(tempBsLvIndex);    //La casella di input viene rimossa
			}
			if (e.Key == Key.Enter || e.Key == Key.Escape)
			{
				e.Handled = true;
				if (!add)
				{
					/* Se è stato premuto INVIO e il valore non è valido o preesistente, o se è stato premuto ESC,
					* si reinserisce il precedente valore */
					//channelLV.Items.RemoveAt(tempBsLvIndex);
					channelLV.Items.Insert(tempBsLvIndex, tempBsLvElement);
					historyList.RemoveAt(historyList.Count - 1);
				}
				tempBsLvElement = null;                                     //Viene dereferenziato il vechcio controllo
				enableControls(MainGrid);                                   //Si riattivano tutti i controlli
				channelLV.MouseDoubleClick -= channelLV_MouseDoubleClick;   //Si riaggiunge l'evento di double click
				channelLV.MouseDoubleClick += channelLV_MouseDoubleClick;
				channelLV.MouseRightButtonUp -= channelLV_MouseRightClick;
				channelLV.MouseRightButtonUp += channelLV_MouseRightClick;
			}

		}

		private void channelLV_MouseRightClick(object sender, MouseButtonEventArgs e)
		{
			if (channelLV.SelectedIndex < 0) return;
			tempBsLvElement = (BS_listViewElement)channelLV.SelectedItem;   //Vengono salvati riferimento e posizione dell'elemento da gestire
			tempBsLvIndex = channelLV.SelectedIndex;

			ContextMenu cm = new ContextMenu();
			cm.Name = "AddressContextMenu";
			MenuItem addressMI = new MenuItem();
			addressMI.Header = "Modify Address";
			addressMI.Click += addressMi_Clicked;
			cm.Items.Add(addressMI);
			cm.Items.Add(new Separator());
			MenuItem scheduleMI = new MenuItem();
			scheduleMI.Header = "Change Unit Schedule";
			scheduleMI.Click += scheduleMi_Clicked;
			cm.Items.Add(scheduleMI);
			MenuItem unitnameMI = new MenuItem();
			unitnameMI.Header = "Change Unit Name";
			unitnameMI.Click += unitnameMi_Clicked;
			cm.Items.Add(unitnameMI);
			cm.Items.Add(new Separator());
			MenuItem resetUnitconfMI = new MenuItem();
			resetUnitconfMI.Header = "Cancel New Unit Schedule";
			resetUnitconfMI.Click += resetscheduleMi_Clicked;
			cm.Items.Add(resetUnitconfMI);
			MenuItem resetUnitnameMI = new MenuItem();
			resetUnitnameMI.Header = "Cancel New Unit Name";
			resetUnitnameMI.Click += resetunitnameMi_Clicked;
			cm.Items.Add(resetUnitnameMI);
			tempBsLvElement.ContextMenu = cm;
			cm.IsOpen = true;
		}

		private void addressMi_Clicked(object sender, RoutedEventArgs e)
		{
			channelLV.MouseRightButtonUp -= channelLV_MouseRightClick;       //Viene rimosso l'interrupt, sarà reinserito a fine funzione
			var lve = (BS_listViewElement)channelLV.SelectedItem;
			lve.ContextMenu = null;
			channelLV_MouseDoubleClick(channelLV, new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left));
			channelLV.MouseRightButtonUp += channelLV_MouseRightClick;
		}

		private void unitnameMi_Clicked(object sender, RoutedEventArgs e)
		{
			channelLV.MouseRightButtonUp -= channelLV_MouseRightClick;       //Viene rimosso l'interrupt, sarà reinserito a fine funzione
			var nn = new BS_NewUnitName((channelLV.SelectedItem as BS_listViewElement).Address);
			nn.ShowDialog();
			if (!nn.newName.Equals(""))
			{
				(channelLV.SelectedItem as BS_listViewElement).NewName = nn.newName;
			}
			channelLV.MouseRightButtonUp += channelLV_MouseRightClick;
		}

		private void resetunitnameMi_Clicked(object sender, RoutedEventArgs e)
		{
			var dr = ((BS_listViewElement)driveLV.SelectedItem).Drive;
			var item = channelLV.SelectedItem as BS_listViewElement;
			item.NewName = "";
			if (File.Exists(dr.Name + FOLDER_CONFIG + "\\" + item.Address.ToString("00000000") + ".nnm"))
			{
				File.Delete(dr.Name + FOLDER_CONFIG + "\\" + item.Address.ToString("00000000") + ".nnm");
			}
		}

		private void scheduleMi_Clicked(object sender, RoutedEventArgs e)
		{
			channelLV.MouseRightButtonUp -= channelLV_MouseRightClick;       //Viene rimosso l'interrupt, sarà reinserito a fine funzione
			(channelLV.SelectedItem as BS_listViewElement).ContextMenu = null;
			string configPath = (driveLV.SelectedItem as BS_listViewElement).Drive.Name + "CONFIG\\" +
							(channelLV.SelectedItem as BS_listViewElement).Address.ToString("00000000") + ".cfg";
			byte[] conf = new byte[600];
			if (File.Exists(configPath))
			{
				conf = File.ReadAllBytes(configPath);
			}
			else
			{
				Array.Copy(Units.Gipsy6.Gipsy6N.defConf, conf, 600);
			}
			conf[541] = (byte)((channelLV.SelectedItem as BS_listViewElement).Address >> 16);
			conf[542] = (byte)((channelLV.SelectedItem as BS_listViewElement).Address >> 8);
			conf[543] = (byte)(channelLV.SelectedItem as BS_listViewElement).Address;
			//var g6 = new Units.Gipsy6(this);
			//g6.firmTotA = 999999999;
			var cf = new ConfigurationWindows.GiPSy6ConfigurationMain(conf, null);
			cf.lockRfAddress = true;
			cf.ShowDialog();
			for (int i = 0; i < 32; i++)        //Questi byte vengono messi tutti a 0xff perché per cambiare il nome unità c'è il file .nnm
			{
				cf.axyConfOut[i] = 0xff;
			}
			if (cf.mustWrite)
			{
				(channelLV.SelectedItem as BS_listViewElement).NewConf = cf.axyConfOut;
			}
			channelLV.MouseRightButtonUp += channelLV_MouseRightClick;
		}

		private void resetscheduleMi_Clicked(object sender, RoutedEventArgs e)
		{
			var dr = ((BS_listViewElement)driveLV.SelectedItem).Drive;
			var item = channelLV.SelectedItem as BS_listViewElement;
			item.NewConf = null;
			if (File.Exists(dr.Name + FOLDER_CONFIG + "\\" + item.Address.ToString("00000000") + ".cfg"))
			{
				File.Delete(dr.Name + FOLDER_CONFIG + "\\" + item.Address.ToString("00000000") + ".cfg");
			}
		}

		private void undoB_Click(object sender, RoutedEventArgs e)
		{
			//Ripristina l'ultima configurazione salvata nell'undo
			undo_restoreItem();
			//Fa partire il timer per il salvataggio automatico
			saveAddressTimer.Stop();
			//saveAddressTimer.Start();
		}

		private void undo_addItem()
		{
			BS_listViewElement[] newHistoryItem = new BS_listViewElement[channelLV.Items.Count];    //Crea un array di listview
			channelLV.Items.CopyTo(newHistoryItem, 0);      //Copia le listview nella lista indirizzi nell'array creato
			historyList.Add(newHistoryItem);        //Aggiunge l'array alla lista degli array dell'undo
		}

		private void undo_restoreItem()
		{
			if (historyList.Count == 0) return;     //Se non ci sono più undo esce subito

			channelLV.Items.Clear();                //Pulisce la lista degli indirizzi
			foreach (var item in historyList[historyList.Count - 1])    //Inserisce le listvie presenti nell'ultimo array dell'undo nella lista indirizzi
			{
				channelLV.Items.Add(item);
			}
			historyList.RemoveAt(historyList.Count - 1);    //Rimuove l'ultimo array dalla lista degli array
		}

		private void saveAddressTimerElapsed(Object source, ElapsedEventArgs e)
		{
			//Allo scadere del timer viene salvata su file la lista indirizzi
			saveAddressTimer.Stop();
			//lock (addressLock)
			//{
			//	Application.Current.Dispatcher.Invoke(() => saveUnitList());
			//}
		}

		private void saveUnitList()
		{
			DriveInfo dr;
			byte[] buff;
			try
			{
				dr = ((BS_listViewElement)driveLV.SelectedItem).Drive;
				if (!validateDrive(dr)) return;
				buff = new byte[(channelLV.Items.Count * 16) + 16];
				//Scrive il numero di unità nella prima riga (0x00 - 0x0f)
				byte[] unitNum = BitConverter.GetBytes((UInt64)channelLV.Items.Count);
				Array.Reverse(unitNum);
				Array.Copy(unitNum, 0, buff, 8, 8);
				//Scrive le righe per ogni unità presente (0x10 in poi)
				for (int i = 0; i < channelLV.Items.Count; i++)
				{
					var item = (BS_listViewElement)channelLV.Items[i];
					var add = BitConverter.GetBytes((UInt32)item.Address);
					Array.Reverse(add);
					Array.Copy(add, 1, buff, (i + 1) * 0x10, 3);
					if (item.NewConf != null)
					{
						File.WriteAllBytes(dr.Name + FOLDER_CONFIG + "\\" + item.Address.ToString("00000000") + ".cfg", item.NewConf);
						buff[((i + 1) * 0x10) + 5] = 1;
					}
					if (item.NewName != "")
					{
						while (item.NewName.Length < 28)
						{
							item.NewName = item.NewName + " ";
						}
						File.WriteAllText(dr.Name + FOLDER_CONFIG + "\\" + item.Address.ToString("00000000") + ".nnm", item.NewName);
						buff[((i + 1) * 0x10) + 6] = 1;
					}
				}
				File.WriteAllBytes(dr.Name + FILE_UNITS, buff);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
				return;
			}

		}

		#endregion

		#region SCHEDULE

		private void fillScheduleList(DriveInfo d)
		{
			//Legge dal file degli schedule i caratteri '1' o '0' relativi agli orari e imposta di conseguenza le checkbox
			//string[] schedule = File.ReadAllLines(d.Name + FILE_SCHEDULE);
			byte[] schedule = File.ReadAllBytes(d.Name + FILE_SCHEDULE);
			oldScheduleArr = new byte[24];
			for (int i = 0; i < 24; i++)
			{
				scheduleCBArr[i].Checked -= scheduleCBchanged;
				scheduleCBArr[i].Unchecked -= scheduleCBchanged;
				if (schedule[i] == 1)
				{
					scheduleCBArr[i].IsChecked = true;
					oldScheduleArr[i] = 1;
				}
				else
				{
					scheduleCBArr[i].IsChecked = false;
					oldScheduleArr[i] = 0;
				}
				scheduleCBArr[i].Checked += scheduleCBchanged;
				scheduleCBArr[i].Unchecked += scheduleCBchanged;
			}
		}

		private void scheduleCBchanged(object sender, RoutedEventArgs e)
		{
			somethingChanged = true;
			saveScheduleTimer.Stop();
			//saveScheduleTimer.Start();
		}

		private void allOnB_Click(object sender, RoutedEventArgs e)
		{
			somethingChanged = true;
			for (int i = 0; i < 24; i++)
			{
				scheduleCBArr[i].Checked -= scheduleCBchanged;
				scheduleCBArr[i].Unchecked -= scheduleCBchanged;
				scheduleCBArr[i].IsChecked = true;
				scheduleCBArr[i].Checked += scheduleCBchanged;
				scheduleCBArr[i].Unchecked += scheduleCBchanged;
			}
			//lock (scheduleLock)
			//{
			//	saveSchedule();
			//}
		}

		private void allOffB_Click(object sender, RoutedEventArgs e)
		{
			somethingChanged = true;
			for (int i = 0; i < 24; i++)
			{
				scheduleCBArr[i].Checked -= scheduleCBchanged;
				scheduleCBArr[i].Unchecked -= scheduleCBchanged;
				scheduleCBArr[i].IsChecked = false;
				scheduleCBArr[i].Checked += scheduleCBchanged;
				scheduleCBArr[i].Unchecked += scheduleCBchanged;
			}
			//lock (scheduleLock)
			//{
			//	saveSchedule();
			//}
		}

		private void saveScheduleTimerElapsed(object sender, ElapsedEventArgs e)
		{
			saveScheduleTimer.Stop();
			byte[] newScheduleArr = new byte[24];
			for (int i = 0; i < 24; i++)
			{
				Application.Current.Dispatcher.Invoke(() => newScheduleArr[i] = (bool)scheduleCBArr[i].IsChecked ? (byte)1 : (byte)0);
			}
			if (!newScheduleArr.SequenceEqual(oldScheduleArr))
			{
				Array.Copy(newScheduleArr, oldScheduleArr, newScheduleArr.Length);
				//lock (scheduleLock)
				//{
				//	Application.Current.Dispatcher.Invoke(() => saveSchedule());
				//}
			}

		}

		private void saveSchedule()
		{
			DriveInfo dr;
			try
			{
				dr = ((BS_listViewElement)driveLV.SelectedItem).Drive;
				if (!validateDrive(dr)) return;
				byte[] sch = new byte[48];

				for (int i = 0; i < 24; i++)
				{
					if (scheduleCBArr[i].IsChecked == true)
					{
						sch[i] = 1;
					}
					else
					{
						sch[i] = 0;
					}
				}
				for (int i = 24; i < 48; i++)   //orari 4G: per ora tutti a zero
				{
					sch[i] = 0;
				}
				File.WriteAllBytes(dr.Name + FILE_SCHEDULE, sch);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
				return;
			}
		}

		#endregion

		#region CONFIGURAZIONE

		private void fillConf(DriveInfo d)
		{
			var basestationID = new byte[4];
			var userID = new byte[4];
			var address = new byte[4];
			var fs = File.OpenRead(d.Name + FILE_CONF);
			fs.Position = 6;
			fs.Read(basestationID, 0, 4);
			fs.Read(address, 0, 4);
			fs.Read(userID, 0, 4);
			fs.Close();
			Array.Reverse(basestationID);
			Array.Reverse(address);
			Array.Reverse(userID);
			bsIDL.Content = BitConverter.ToInt32(basestationID, 0).ToString();
			bsAddressTB.Text = BitConverter.ToInt32(address, 0).ToString();
			bsAddress = BitConverter.ToInt32(address, 0);
			userIDL.Content = BitConverter.ToInt32(userID, 0).ToString();
			bsNameTB.Text = File.ReadAllLines(d.Name + FILE_NAME)[0];
			intialName = bsNameTB.Text;
		}

		private void saveNameConfigurationDate()
		{
			DriveInfo dr = null;

			//Salva l'indirizzo di ricezione e, se impostate, data e ora
			try
			{
				byte[] conf = File.ReadAllBytes(dr.Name + FILE_CONF);
				byte[] add = BitConverter.GetBytes(bsAddress);
				Array.Reverse(add);
				Array.Copy(add, 0, conf, 0x0a, 4);
				if (!(timestamp is null))
				{
					Array.Copy(timestamp, 0, conf, 0x12, 8);
				}
				File.WriteAllBytes(dr.Name + FILE_CONF, conf);
			}
			catch { }

			//Salva il nome della basestation
			try
			{
				dr = ((BS_listViewElement)driveLV.SelectedItem).Drive;
				File.WriteAllText(dr.Name + FILE_NAME, bsNameTB.Text);
			}
			catch { }
		}

		private void formatB_Click(object sender, RoutedEventArgs e)
		{
			listDriveTimer.Stop();
			validateDriveTimer.Stop();
			byte[] bsId = { 0, 0, 0, 1 };
			byte[] userId = { 0, 0, 0, 0 };
			bool goOn = false;
			lock (addressLock)
			{
				lock (scheduleLock) { }
			}
			if (File.Exists((driveLV.SelectedItem as BS_listViewElement).Drive.Name + FILE_CONF))
			{
				var yn = new YesNo("The selected device already contains a valid ID. Do you want to keep it?", "BASESTATION ID", "", "KEEP", "DISCARD");
				if (yn.ShowDialog() == YesNo.YES)//Se la basestation era già formattata, la riformatta usando il basestation id esistente. Non c'è bisogno di loggarsi come admin
				{
					Array.Copy(File.ReadAllBytes((driveLV.SelectedItem as BS_listViewElement).Drive.Name + FILE_CONF).Skip(6).Take(4).ToArray(), bsId, 4);
					Array.Copy(File.ReadAllBytes((driveLV.SelectedItem as BS_listViewElement).Drive.Name + FILE_CONF).Skip(14).Take(4).ToArray(), userId, 4);
					formatProgress(0);
					var t = new Thread(() => formatTask(bsId, userId));
					t.SetApartmentState(ApartmentState.STA);
					t.Start();
					return;
				}
			}
			if (!MainWindow.adminUser)      //Se la basestation è vergine, si vuole cambiare cliente o id, c'è bisogno di essere admin
			{
				string res = Interaction.InputBox("Insert password: ", "Password");
				if ((res != "cetriolo") && (res != "saji"))
				{
					MessageBox.Show("Wrong password.");
				}
				else
				{
					goOn = true;
					MainWindow.adminUser = true;
				}
			}
			else
			{
				goOn = true;
			}
			if (goOn)
			{
				RemoteUeserSelection rus = new RemoteUeserSelection();
				rus.Owner = this;
				rus.ShowDialog();
				if (rus.basetstationID == "") return;
				uint bsIdn = uint.Parse(rus.basetstationID);
				uint userIdn = uint.Parse(rus.userID);
				bsId[0] = (byte)(bsIdn >> 24);
				bsId[1] = (byte)(bsIdn >> 16);
				bsId[2] = (byte)(bsIdn >> 8);
				bsId[3] = (byte)bsIdn;
				userId[0] = (byte)(userIdn >> 24);
				userId[1] = (byte)(userIdn >> 16);
				userId[2] = (byte)(userIdn >> 8);
				userId[3] = (byte)userIdn;
				rus = null;
				formatProgress(0);
				var t = new Thread(() => formatTask(bsId, userId));
				t.SetApartmentState(ApartmentState.STA);
				t.Start();
			}
		}

		private void formatProgress(int status)
		{
			switch (status)
			{
				case 0:
					notValidL.Content = "Formatting unit. Please wait...";
					break;
				case 1:
					notValidL.Content = "Configuring unit. Please wait...";
					break;
				case 100:
					int pos = driveLV.SelectedIndex;
					driveLV.Items.Clear();
					listDrive();
					driveLV.SelectedIndex = pos;
					currentDriveLabel = "";
					currentDriveLetter = "";
					driveLvItemClicked();
					listDriveTimer.Start();
					break;
				case 200:
					notValidL.Content = "Formatting Failed!";
					listDriveTimer.Start();
					break;
			}
		}

		private void formatTask(byte[] bsId, byte[] userId)
		{

			DriveInfo di = null;
			//try
			//{
			Application.Current.Dispatcher.Invoke(() => di = ((BS_listViewElement)driveLV.SelectedItem).Drive);

			//	string drive = di.Name.Substring(0, 1) + ":";

			//	var psi = new ProcessStartInfo();
			//	psi.FileName = "format.com";
			//	psi.CreateNoWindow = true; //if you want to hide the window
			//	psi.WorkingDirectory = Environment.SystemDirectory;
			//	psi.Arguments = "/FS:FAT32" + " /Y" + " /V:BaseStation /Q /A:2048 " + drive;
			//	psi.UseShellExecute = false;
			//	psi.CreateNoWindow = true;
			//	psi.RedirectStandardOutput = true;
			//	psi.RedirectStandardInput = true;
			//	var formatProcess = Process.Start(psi);
			//	var swStandardInput = formatProcess.StandardInput;
			//	swStandardInput.WriteLine();
			//	formatProcess.WaitForExit();
			//}
			//catch (Exception ex)
			//{
			//	MessageBox.Show(ex.Message);
			//	Application.Current.Dispatcher.Invoke(() => formatProgress(200));
			//	return;
			//}

			DirectoryInfo Di = new DirectoryInfo(di.Name);
			foreach (FileInfo f in Di.GetFiles())
			{
				try
				{
					f.Delete();
				}
				catch { }
			}
			foreach (DirectoryInfo d in Di.GetDirectories())
			{
				try
				{
					d.Delete(true);
				}
				catch { }
			}

			byte[] conf = { 0x74, 0x73, 0x6D, 0x65, 0x42, 0x53,
							bsId[0], bsId[1], bsId[2], bsId[3],
							0x00, 0x00, 0x00, 0x01,
							userId[0],userId[1],userId[2],userId[3],
							0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
			Application.Current.Dispatcher.Invoke(() => formatProgress(1));
			Directory.CreateDirectory(di.Name + "CONFIG");
			Directory.CreateDirectory(di.Name + "DATA");
			File.WriteAllBytes(di.Name + FILE_CONF, conf);
			File.WriteAllText(di.Name + FILE_NAME, "No Name");
			File.WriteAllBytes(di.Name + FILE_SCHEDULE, new byte[] {    0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
																		0x01,0x01,0x01,0x01,0x01,0x01,0x01,0x01,0x01,0x01,0x01,0x01,});
			File.WriteAllBytes(di.Name + FILE_UNITS, new byte[] {       0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
																		0x00, 0x00, 0x00, 0x00, 0x00,0x00});

			Thread.Sleep(200);
			Application.Current.Dispatcher.Invoke(() => formatProgress(100));


		}

		private void bsNameTB_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				e.Handled = true;
				if (bsNameTB.Text == "")
				{
					bsNameTB.Text = "No Name";
				}
				string res = HTTP.cOut(string.Format("{0}&userid={1}&stationid={2}&stationname={3}", HTTP.COMMAND_RENAME_BASESTATION, userIDL.Content, bsIDL.Content, bsNameTB.Text));
				if (res == HTTP.COMMAND_ERROR)
				{
					MessageBox.Show("Error renaming basestation.");
				}
				else
				{
					MessageBox.Show("Basestation succesfully renamed.");
				}
			}
		}


		#endregion


		private byte reverseByte(byte inb)
		{
			byte outb = 0;

			outb = (byte)((inb << 7) & 0b1000_0000);
			outb += (byte)((inb << 5) & 0b0100_0000);
			outb += (byte)((inb << 3) & 0b0010_0000);
			outb += (byte)((inb << 1) & 0b0001_0000);
			outb += (byte)((inb >> 1) & 0b0000_1000);
			outb += (byte)((inb >> 3) & 0b0000_0100);
			outb += (byte)((inb >> 5) & 0b0000_0010);
			outb += (byte)((inb >> 7) & 0b0000_0001);

			return outb;
		}

		private byte dec2BCD(byte inb)
		{
			byte outb = 0;

			outb = (byte)(inb / 10);
			outb *= 16;
			outb += (byte)(inb % 10);

			return outb;
		}

	}
}

