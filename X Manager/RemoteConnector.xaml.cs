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
using System.Windows.Shapes;
using System.IO.Ports;
using System.Threading;
using System.Windows.Threading;
using System.Diagnostics;

namespace X_Manager
{
    /// <summary>
    /// Interaction logic for RemoteConnector.xaml
    /// </summary>

    public partial class RemoteConnector : Window
    {
        private int connectionResult = 0;
        private SerialPort sp;
        volatile int stop = 0;
        
        SolidColorBrush grigio = new SolidColorBrush();
        SolidColorBrush blu = new SolidColorBrush();
        SolidColorBrush celeste = new SolidColorBrush();
        SolidColorBrush bianco = new SolidColorBrush();
        SolidColorBrush verde = new SolidColorBrush();
        SolidColorBrush rosso = new SolidColorBrush();

        public RemoteConnector(ref System.IO.Ports.SerialPort serialPort)
        {
            InitializeComponent();

            grigio.Color = Color.FromArgb(0xff, 0x42, 0x42, 0x42);
            blu.Color = Color.FromArgb(0xff, 0x00, 0xaa, 0xde);
            celeste.Color = Color.FromArgb(0xff, 0x10, 0xc9, 0xff);
            bianco.Color = Color.FromArgb(0xff, 0xff, 0xff, 0xff);
            rosso.Color = Color.FromArgb(0xff, 0xff, 0x00, 0x00);
            verde.Color = Color.FromArgb(0xff, 0x16, 0xf5, 0x00);

            channelListCB.Items.Clear();
            for (int i = 2; i < 254; i++)
            {
                channelListCB.Items.Add(i.ToString());
            }
            channelListCB.SelectedIndex = 0;
            stopB.IsEnabled = false;
            sp = serialPort;
            sp.Open();
            channelListCB.SelectionChanged += channelListSelectionChangedEvent;
        }

        public new int ShowDialog()
        {
            base.ShowDialog();
            return connectionResult;
        }

        private void wakeClick(object sender, RoutedEventArgs e)
        {
            stop = 0;
            sp.Write(new byte[] { 52 }, 0, 1);
            Thread.Sleep(250);
            sp.Write(new byte[] { 43, 43, 43 }, 0, 3);
            Thread.Sleep(250);
            byte address = byte.Parse(channelListCB.SelectedItem.ToString());
            channelListCB.IsEnabled = false;
            sp.Write(new byte[] { 65, 84, 65, 87, address }, 0, 5);
            Thread antennaThread = new Thread(() => animateAntenna());
            antennaThread.Start();
            wakeB.IsEnabled = false;
            Thread commThread = new Thread(() => communicationAttempt(address));
            commThread.SetApartmentState(System.Threading.ApartmentState.STA);
            commThread.Start();
            stopB.IsEnabled = true;
        }

        private void communicationAttempt(byte address)
        {
			//Stopwatch sw = new Stopwatch();
            sp.ReadTimeout = 3000;
            byte status = 0;
            byte connCount = 0;
            while (stop == 0)
            {
                try
                {
					status = (byte)sp.ReadByte();
					//sw.Start();
					if (status < 3)
                    {
                        connCount++;
                    }
                    if (connCount == 15)
                    {
                        status = 2;
                    }
                }
                catch
                {
                    stop = 102;
                    var w = new Warning("Base Station not ready.");
                    w.picUri = "pack://application:,,,/Resources/alert2.png";
                    w.ShowDialog();
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => this.channelListCB.IsEnabled = true));
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => this.wakeB.IsEnabled = true));
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => this.finalize(0)));
                    status = 255;
                    return;
                }
                if (status == 0)
                {
					if (stop == 0)
					{
						//Thread.Sleep(800);
						//sw.Stop();
						//Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => this.sviluppoTB.Text = sw.Elapsed.Milliseconds.ToString()));
						//sw.Reset();
						sp.Write(new byte[] { 65, 84, 65, 87, address }, 0, 5);
					}
					else
					{
						sp.Write("ATX");
					}					
                }
                else
                {
					if (status == 1)    //L'unità ha risposto
					{
						stop = 1;
						Thread.Sleep(100);
						sp.Write("ATX");
						Thread.Sleep(500);
						Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => this.channelListCB.IsEnabled = true));
						Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => this.wakeB.IsEnabled = true));
						Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => this.finalize(1)));
					}
					else if (status == 2)   //Raggiunto massimo numero di tentativi
					{
						stop = 2;
						Thread.Sleep(100);
						sp.Write("ATX");
						Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => this.channelListCB.IsEnabled = true));
						Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => this.wakeB.IsEnabled = true));
					}
				}
            }
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => this.channelListCB.IsEnabled = true));
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => this.wakeB.IsEnabled = true));
        }

        private void stopClick(object sender, RoutedEventArgs e)
        {
            stop = 100;
            stopB.IsEnabled = false;
        }

        private void finalize(int result)
        {
            connectionResult = result;
            sp.Close();
            this.Close();
        }

        private void colorStep(int counter)
        {
            switch (counter)
            {
                case 0:
                    r1.Fill = blu;
                    r2.Fill = grigio;
                    r3.Fill = grigio;
                    r4.Fill = grigio;
                    ra.Fill = grigio;
                    rb.Fill = grigio;
                    break;
                case 1:
                    r1.Fill = grigio;
                    r2.Fill = blu;
                    break;
                case 2:
                    r2.Fill = grigio;
                    r3.Fill = blu;
                    break;
                case 3:
                    r3.Fill = grigio;
                    r4.Fill = blu;
                    break;
                case 4:
                    r1.Fill = celeste;
                    r4.Fill = blu;
                    break;
                case 5:
                    r1.Fill = grigio;
                    r2.Fill = celeste;
                    break;
                case 6:
                    r2.Fill = grigio;
                    r3.Fill = celeste;
                    break;
                case 7:
                    r3.Fill = grigio;
                    r4.Fill = celeste;
                    ra.Fill = blu;
                    rb.Fill = blu;
                    break;
                case 8:
                    r4.Fill = grigio;
                    ra.Fill = bianco;
                    rb.Fill = bianco;
                    break;
                case 100:               //E' stato premuto stop
                    r1.Fill = grigio;
                    r2.Fill = grigio;
                    r3.Fill = grigio;
                    r4.Fill = grigio;
                    ra.Fill = grigio;
                    rb.Fill = grigio;
                    break;
                case 101:               //L'unità ha risposto
                    r1.Fill = verde;
                    r2.Fill = verde;
                    r3.Fill = verde;
                    r4.Fill = verde;
                    ra.Fill = verde;
                    rb.Fill = verde;
                    break;
                case 102:               //L'unità non ha risposto
                    r1.Fill = rosso;
                    r2.Fill = rosso;
                    r3.Fill = rosso;
                    r4.Fill = rosso;
                    ra.Fill = rosso;
                    rb.Fill = rosso;
                    break;
            }
        }

        private void animateAntenna()
        {
            int counter = 0;
            while (stop == 0)
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => this.colorStep(counter)));
                switch (counter)
                {
                    case 0:
                        Thread.Sleep(200);
                        break;
                    case 1:
                        Thread.Sleep(200);
                        break;
                    case 2:
                        Thread.Sleep(200);
                        break;
                    case 3:
                        Thread.Sleep(50);
                        break;
                    case 4:
                        Thread.Sleep(50);
                        break;
                    case 5:
                        Thread.Sleep(50);
                        break;
                    case 6:
                        Thread.Sleep(50);
                        break;
                    case 7:
                        Thread.Sleep(50);
                        break;
                    case 8:
                        Thread.Sleep(150);
                        break;
                }
                counter++;
                if (counter == 9)
                {
                    counter = 0;
                }
            }
            int resp = stop;
            if (resp < 100) resp += 100;
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => this.colorStep(resp)));

        }

        private void channelListSelectionChangedEvent(object sender, SelectionChangedEventArgs e)
        {
            colorStep(100);
        }
    }
}
