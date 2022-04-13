using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Windows;
using System.Windows.Threading;
#if X64
using FT_HANDLE = System.UInt64;
#else
using FT_HANDLE = System.UInt32;
#endif

namespace X_Manager.Units
{
    class Gipsy6 : Units.Unit
    {
        new byte[] firmwareArray = new byte[3];

        public Gipsy6(object p)
            : base(p)
        {
            configureMovementButtonEnabled = false;
            configurePositionButtonEnabled = true;
        }

        public override string askFirmware()
        {
            byte[] f = new byte[3];
            string firmware = "";

            sp.ReadExisting();
            sp.Write("TTTTTTTGGAF");
            sp.ReadTimeout = 400;
            int i = 0;
            try
            {
                for (i = 0; i < 3; i++) f[i] = (byte)sp.ReadByte();
            }
            catch
            {
                throw new Exception(unitNotReady);
            }
            firmTotA = 0;
            for (i = 0; i < 3; i++)
            {
                firmTotA *= 1000;
                firmTotA += f[i];
            }
            for (i = 0; i <= (f.Length - 2); i++)
            {
                firmware += f[i].ToString() + ".";
            }
            firmware += f[f.Length - 1].ToString();

            firmwareArray = f;

            return firmware;
        }

        public override string askName()
        {
            string unitNameBack;
            try
            {
                sp.ReadExisting();
                sp.Write("TTTTTTTGGAN");
                unitNameBack = sp.ReadLine();
            }
            catch
            {
                throw new Exception(unitNotReady);
            }
            name = formatName(unitNameBack);
            return name;
        }

        public override string askBattery()
        {
            string battery = "";
            double battLevel;
            sp.Write("TTTTGGAB");
            try
            {
                battLevel = sp.ReadByte(); battLevel *= 256;
                battLevel += sp.ReadByte();
                battLevel *= 6;
                battLevel /= 4096;
            }
            catch
            {
                throw new Exception(unitNotReady);
            }
            battery = Math.Round(battLevel, 2).ToString("0.00") + "V";
            return battery;
        }

        public override uint askMaxMemory()
        {
            UInt32 m;
            sp.Write("TTTTTTTGGAm");
            try
            {
                m = (UInt32)sp.ReadByte(); m *= 256;
                m += (UInt32)sp.ReadByte(); m *= 256;
                m += (UInt32)sp.ReadByte(); m *= 256;
                m += (UInt32)sp.ReadByte();
            }
            catch
            {
                throw new Exception(unitNotReady);
            }
            maxMemory = m;
            return maxMemory;
        }

        public override uint askMemory()
        {
            UInt32 m;
            sp.Write("TTTTTTTGGAM");
            try
            {
                m = (UInt32)sp.ReadByte(); m *= 256;
                m += (UInt32)sp.ReadByte(); m *= 256;
                m += (UInt32)sp.ReadByte(); m *= 256;
                m += (UInt32)sp.ReadByte();
            }
            catch
            {
                throw new Exception(unitNotReady);
            }
            memory = m;
            return memory;
        }

        public override void eraseMemory()
        {
            sp.Write("TTTTTTTTGGAE");
            try
            {
                sp.ReadByte();
            }
            catch
            {
                throw new Exception(unitNotReady);
            }
        }

        public override void setName(string newName)
        {
            if (newName.Length < 10)
            {
                for (int i = newName.Length; i <= 9; i++) newName += " ";
            }
            sp.Write("TTTTTTTGGAn");
            try
            {
                sp.ReadByte();
            }
            catch
            {
                throw new Exception(unitNotReady);
            }
            sp.WriteLine(newName);
        }

        public override bool isRemote()
        {
            remote = false;
            sp.Write("TTTTTTTTTTGGAl");
            try
            {
                if (sp.ReadByte() == 1) remote = true;
            }
            catch
            {
                throw new Exception(unitNotReady);
            }
            return remote;
        }

        public override byte[] getGpsSchedule()
        {
            byte[] schedule = new byte[256];
            sp.ReadExisting();
            sp.Write("TTTTTTTTTTTTTGGAS");
            try
            {
                for (int i = 0; i < 256; i++)
                {
                    schedule[i] = (byte)sp.ReadByte();
                }
                //for (int i = 0; i <= 63; i++) { schedule[i] = (byte)sp.ReadByte(); }
                //if (remote) sp.Write(new byte[] { 2 }, 0, 1);

                //for (int i = 64; i <= 127; i++) { schedule[i] = (byte)sp.ReadByte(); }
                //if (remote) sp.Write(new byte[] { 2 }, 0, 1);

                //for (int i = 128; i <= 171; i++) { schedule[i] = (byte)sp.ReadByte(); }
            }
            catch
            {
                throw new Exception(unitNotReady);
            }
            return schedule;
        }

        public override void setGpsSchedule(byte[] schedule)
        {
            sp.Write("TTTTTTTTGGAs");
            try
            {
                sp.ReadByte();
                sp.Write(schedule, 0, 64);
                if (remote)
                {
                    sp.ReadByte();
                }
                sp.Write(schedule, 64, 64);
                if (remote)
                {
                    sp.ReadByte();
                }
                sp.Write(schedule, 128, 64);
                if (remote)
                {
                    sp.ReadByte();
                }
                sp.Write(schedule, 192, 64);
                if (remote)
                {
                    sp.ReadByte();
                }
                sp.ReadByte();
            }
            catch
            {
                throw new Exception(unitNotReady);
            }
        }

        public override void setPcTime()
        {
            sp.Write("TTTTTTTTGGAt");
            try
            {
                sp.ReadByte();
                byte[] dateAr = new byte[6];
                var dateToSend = DateTime.UtcNow;
                dateAr[0] = (byte)dateToSend.Second;
                dateAr[1] = (byte)dateToSend.Minute;
                dateAr[2] = (byte)dateToSend.Hour;
                dateAr[3] = (byte)dateToSend.Day;
                dateAr[4] = (byte)dateToSend.Month;
                dateAr[5] = (byte)(dateToSend.Year - 2000);
                sp.Write(dateAr, 0, 6);
                sp.ReadByte();
            }
            catch
            {
                throw new Exception(unitNotReady);
            }
        }

        public override void disconnect()
        {
            base.disconnect();
            sp.Write("TTTTTTTTTGGAO");
        }

        public unsafe override void download(MainWindow parent, string fileName, uint fromMemory, uint toMemory, int baudrate)
        {
            convertStop = false;
            uint actMemory = fromMemory;
            System.IO.FileMode fm = System.IO.FileMode.Create;
            if (fromMemory != 0) fm = System.IO.FileMode.Append;
            string fileNameMdp = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + ".mdp";
            var fo = new BinaryWriter(File.Open(fileNameMdp, fm));

            sp.Write("TTTTTTTTTTTTGGAD");
            try
            {
                sp.ReadByte();
            }
            catch
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFailed()));
                try
                {
                    fo.Close();
                }
                catch { }
                return;
            }

            Thread.Sleep(50);
            byte b = (byte)(baudrate / 1000000);
            sp.Write(new byte[] { b }, 0, 1);
            Thread.Sleep(100);
            sp.BaudRate = baudrate;
            Thread.Sleep(100);

            Thread.Sleep(200);
            sp.Write("S");
            Thread.Sleep(1000);
            if (sp.ReadByte() != (byte)0x53)
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFailed()));
                try
                {
                    fo.Close();
                }
                catch { }
                return;
            }

            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.progressBarStopButton.IsEnabled = true));
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.progressBarStopButtonColumn.Width = new GridLength(80)));

            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.IsIndeterminate = false));
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Minimum = 0));
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Maximum = toMemory));
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Value = fromMemory));

            //Passa alla gestione FTDI D2XX
            sp.Close();

            MainWindow.FT_STATUS FT_Status;
            FT_HANDLE FT_Handle = 0;
            byte[] outBuffer = new byte[50];
            byte[] inBuffer = new byte[4096];
            byte[] tempBuffer = new byte[2048];
            byte[] address = new byte[8];

            uint bytesToWrite = 0, bytesWritten = 0, bytesReturned = 0;

            FT_Status = MainWindow.FT_OpenEx(parent.ftdiSerialNumber, (UInt32)1, ref FT_Handle);
            if (FT_Status != MainWindow.FT_STATUS.FT_OK)
            {
                MainWindow.FT_Close(FT_Handle);
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFailed()));
                try
                {
                    fo.Close();
                }
                catch { }
                return;
            }

            MainWindow.FT_SetLatencyTimer(FT_Handle, (byte)1);
            MainWindow.FT_SetTimeouts(FT_Handle, (uint)1000, (uint)1000);

            bool firstLoop = true;

            while (actMemory < toMemory)
            {
                if (((actMemory % 0x2000000) == 0) | (firstLoop))
                {
                    address = BitConverter.GetBytes(actMemory);
                    Array.Reverse(address);
                    Array.Copy(address, 0, outBuffer, 1, 3);
                    outBuffer[0] = 65;
                    bytesToWrite = 4;
                    firstLoop = false;
                }
                else
                {
                    outBuffer[0] = 79;
                    bytesToWrite = 1;
                }
                fixed (byte* outP = outBuffer, inP = inBuffer)
                {
                    FT_Status = MainWindow.FT_Write(FT_Handle, outP, bytesToWrite, ref bytesWritten);
                    FT_Status = MainWindow.FT_Read(FT_Handle, inP, (uint)4096, ref bytesReturned);
                }

                if (FT_Status != MainWindow.FT_STATUS.FT_OK)
                {
                    outBuffer[0] = 88;
                    fixed (byte* outP = outBuffer) { FT_Status = MainWindow.FT_Write(FT_Handle, outP, bytesToWrite, ref bytesWritten); }
                    MainWindow.FT_Close(FT_Handle);
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFailed()));
                    fo.Write(inBuffer);
                    fo.Close();
                    return;
                }
                else if (bytesReturned != 4096)
                {
                    firstLoop = true;
                }
                else
                {
                    actMemory += 4096;
                    if ((actMemory % 0x20000) == 0)
                    {
                        actMemory -= 4096;
                        for (int i = 0; i < 2; i++)
                        {
                            address = BitConverter.GetBytes(actMemory);
                            Array.Reverse(address);
                            Array.Copy(address, 0, outBuffer, 1, 3);
                            outBuffer[0] = 97;
                            bytesToWrite = 4;
                            fixed (byte* outP = outBuffer, inP = inBuffer)
                            {
                                FT_Status = MainWindow.FT_Write(FT_Handle, outP, bytesToWrite, ref bytesWritten);
                                FT_Status = MainWindow.FT_Read(FT_Handle, inP, (uint)2048, ref bytesReturned);
                            }
                            fo.Write(inBuffer, 0, 2048);
                            actMemory += 2048;
                        }
                        firstLoop = true;
                    }
                    else
                    {
                        fo.Write(inBuffer, 0, 4096);
                    }
                }

                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Value = actMemory));

                if (convertStop) actMemory = toMemory;
            }

            fo.Write(firmwareArray, 0, 6);
            fo.Write(new byte[] { model_axyTrek, (byte)254 }, 0, 2);

            fo.Close();
            outBuffer[0] = 88;
            bytesToWrite = 1;
            fixed (byte* outP = outBuffer)
            {
                FT_Status = MainWindow.FT_Write(FT_Handle, outP, bytesToWrite, ref bytesWritten);
            }
            MainWindow.FT_Close(FT_Handle);
            sp.BaudRate = 115200;
            sp.Open();
            if (!convertStop) extractArds(fileNameMdp, fileName, true);
            else
            {
                if (MainWindow.lastSettings[6].Equals("false"))
                {
                    try
                    {
                        System.IO.File.Delete(fileNameMdp);
                    }
                    catch { }
                }
            }
            Thread.Sleep(300);
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFinished()));
        }




        public override void abortConf() { }

        public override void setConf(byte[] conf) { }

        public override byte[] getConf() { return new byte[] { 0 }; }

        public override void extractArds(string fileNameMdp, string fileName, bool fromDownload) { }

    }
}
