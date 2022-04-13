﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using System.Globalization;
#if X64
using FT_HANDLE = System.UInt64;
#else
    using FT_HANDLE = System.UInt32;
#endif

namespace X_Manager.Units
{
    class AxyDepth_2 : Units.Unit
    {
        ushort[] coeffs = new ushort[7];
        double[] convCoeffs = new double[7];
        //bool evitaSoglie = false;
        bool disposed = false;
        new byte[] firmwareArray = new byte[6];
        struct timeStamp
        {
            public byte tsType, tsTypeExt1, tsTypeExt2;
            public float batteryLevel;
            public float temp, press, pressOffset;
            public DateTime orario;
            public byte stopEvent;
            public byte timeStampLength;
        }

        byte dateFormat;
        byte timeFormat;
        bool sameColumn = false;
        bool prefBattery = false;
        bool repeatEmptyValues = true;
        bool isDepth = true;
        bool bits;
        byte bitsDiv;
        bool inMeters = false;
        bool angloTime = false;
        ushort rate;
        ushort rateComp;
        byte range;
        double gCoeff;
        string dateFormatParameter;
        ushort addMilli;
        bool metadata;
        byte cifreDec;
        string cifreDecString;
        CultureInfo dateCi;
        bool overrideTime;

        public AxyDepth_2(object p)
            : base(p)
        {
            base.positionCanSend = false;
            configurePositionButtonEnabled = false;
            modelCode = model_axyDepth;
            modelName = "Axy-Depth";
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
                if (i > 0)
                {
                    Array.Resize(ref f, 2);
                }
                else
                {
                    throw new Exception(unitNotReady);
                }
            }

            firmTotA = 0;
            for (i = 0; i <= (f.Length - 1); i++)
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

        public override string askName()
        {
            string unitNameBack;
            try
            {
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
            sp.Write("TTTTTTTTTGGAE");
            try
            {
                sp.ReadByte();
            }
            catch
            {
                throw new Exception(unitNotReady);
            }
        }

        public override void getCoeffs()
        {
            coeffs[0] = 0;
            sp.Write("TTTTTTTTTTTGGAg");
            try
            {
                for (int i = 1; i <= 6; i++)
                {
                    coeffs[i] = (ushort)(sp.ReadByte() * 256 + sp.ReadByte());
                }
            }
            catch
            {
                throw new Exception(unitNotReady);
            }
            uint sommaSoglie = 0;
            try
            {
                sommaSoglie = (uint)(coeffs[1] + coeffs[2] + coeffs[3]);
            }
            catch
            {
                sommaSoglie = 1;
            }
            if ((sommaSoglie == 0) | (sommaSoglie == 0x2fd))
            {
                //evitaSoglie = true;
            }
        }

        public override void setPcTime()
        {
            if (firmTotA < 2006000) return;

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

        public override byte[] getConf()
        {
            byte[] conf = new byte[30];
            for (byte i = 0; i < 30; i++) conf[i] = 0xff;

            conf[22] = 0;
            conf[25] = modelCode;

            sp.ReadExisting();
            sp.Write("TTTTTTTTTTTTGGAC");
            try
            {
                for (int i = 15; i <= 19; i++) { conf[i] = (byte)sp.ReadByte(); }
            }
            catch
            {
                throw new Exception(unitNotReady);
            }
            return conf;
        }

        public override void setConf(byte[] conf)
        {
            sp.ReadExisting();
            sp.Write("TTTTTTTTTTTTGGAc");
            try
            {
                sp.ReadByte();
                sp.Write(conf, 15, 5);
                sp.ReadByte();
            }
            catch
            {
                throw new Exception(unitNotReady);
            }
        }

        public override void abortConf()
        {

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

        public override void disconnect()
        {
            base.disconnect();
            sp.Write("TTTTTTTTTGGAO");
        }

        private uint[] calcolaSoglieDepth()
        {
            uint[] soglie = new uint[18];
            double[] temps = new double[] { -15, -10, 0, 10, 20, 30, 40, 50 };
            double[] tempsInt = new double[] { -18, -12, -5, 5, 15, 25, 35, 45, 55 };

            for (int i = 0; i <= 7; i++) soglie[i] = (uint)guessD2(temps[i]);

            for (int i = 8; i <= 16; i++) soglie[i] = guessD1(guessD2(tempsInt[i - 8]), 1520);

            soglie[17] = 1;
            return soglie;
        }

        private double guessD2(double t)
        {
            double max = 16777215;
            double min = 0;
            double d2 = (max - min) / 2;
            double temp;

            while (Math.Abs(max - min) > 4)
            {
                temp = pTemp(d2);
                if (temp > t) max = d2;
                else min = d2;
                d2 = ((max - min) / 2) + min;
            }
            return d2;
        }

        private uint guessD1(double d2, double p)
        {
            double max = 16777215;
            double min = 0;
            double d1 = (max - min) / 2;
            double press;

            while (Math.Abs(max - min) > 4)
            {
                press = pDepth(d1, d2);
                if (press > p) max = d1;
                else min = d1;
                d1 = ((max - min) / 2) + min;
            }
            return (uint)d1;
        }

        private double pDepth(double d1, double d2)
        {
            double dT;
            double off;
            double sens;
            double temp;
            double[] c = new double[7];

            for (int count = 1; count <= 6; count++)
            {
                c[count - 1] = (double)coeffs[count];
            }
            dT = d2 - c[4] * 256;
            temp = 2000 + (dT * c[5]) / 8388608;
            off = c[1] * 65536 + (c[3] * dT) / 128;
            sens = c[0] * 32768 + (c[2] * dT) / 256;
            if (temp > 2000)
            {
                temp -= ((2 * Math.Pow(dT, 2)) / 137438953472);
                off -= ((Math.Pow((temp - 2000), 2)) / 16);
            }
            else
            {
                off -= 3 * ((Math.Pow((temp - 2000), 2)) / 2);
                sens -= 5 * ((Math.Pow((temp - 2000), 2)) / 8);
                if (temp < -1500)
                {
                    off -= 7 * Math.Pow((temp + 1500), 2);
                    sens -= 4 * Math.Pow((temp + 1500), 2);
                }
                temp -= (3 * (Math.Pow(dT, 2))) / 8589934592;
            }
            sens = d1 * sens / 2097152;
            sens -= off;
            sens /= 81920;
            return sens;
        }

        private double pTemp(double d2)
        {
            double ti;
            double dt;
            double temp;
            double[] c = new double[7];

            for (int count = 1; count <= 6; count++)
            {
                c[count] = (double)coeffs[count];
            }
            dt = d2 - c[5] * 256;
            temp = 2000 + ((dt * c[6]) / 8388608);
            if ((temp / 100) < 20) ti = 3 * (Math.Pow(dt, 2)) / 8388608;
            else ti = 2 * (Math.Pow(dt, 2)) / 137438953472;
            temp = (temp - ti) / 100;
            return temp;
        }

        public unsafe override void download(MainWindow parent, string fileName, uint fromMemory, uint toMemory, int baudrate)
        {
            convertStop = false;
            uint actMemory = fromMemory;
            System.IO.FileMode fm = System.IO.FileMode.Create;
            if (fromMemory != 0) fm = System.IO.FileMode.Append;
            string fileNameMdp = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + ".mdp";
            var fo = new System.IO.BinaryWriter(File.Open(fileNameMdp, fm));

            sp.Write("TTTTTTTTTTTTGGAe");
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

            Thread.Sleep(70);
            byte b = (byte)(baudrate / 1000000);
            sp.Write(new byte[] { b }, 0, 1);
            sp.BaudRate = baudrate;

            Thread.Sleep(200);
            sp.Write("S");
            Thread.Sleep(50);

            if (sp.ReadByte() != 0x53)
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

            fo.Write(firmwareArray, 0, firmwareArray.Length);
            for (int i = firmwareArray.Length; i <= 2; i++)
            {
                fo.Write(new byte[] { 0xff }, 0, 1);
            }
            fo.Write(new byte[] { model_axyDepth, (byte)254 }, 0, 2);

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

        public override void extractArds(string fileNameMdp, string fileName, bool fromDownload)
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusLabel.Content = "Creating Ard file(s)..."));
            var mdp = new BinaryReader(System.IO.File.Open(fileNameMdp, FileMode.Open));

            BinaryWriter ard = BinaryWriter.Null;
            //ushort packLength = 255;
            //ushort firstPackLength = 254;
            string fileNameArd = "";
            byte testByte, testByte2;
            const int yes = 1;
            const int no = 2;
            const int yes_alaways = 11;
            int resp = no;
            ushort counter = 0;

            while (mdp.BaseStream.Position < mdp.BaseStream.Length)
            {
                testByte = mdp.ReadByte();
                if (testByte == 0xcf)
                {
                    testByte2 = mdp.ReadByte();
                    if (ard != BinaryWriter.Null)
                    {
                        ard.Close();
                    }
                    counter++;
                    fileNameArd = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + "_S" + counter.ToString() + ".ard";
                    if (System.IO.File.Exists(fileNameArd))
                    {
                        if (resp < 11)
                        {
                            var yn = new YesNo(fileNameArd + " already exists. Do you want to overwrite it?", "FILE EXISTING", "Remeber my choice");
                            resp = yn.ShowDialog();
                        }
                        if ((resp == yes) | (resp == yes_alaways))
                        {
                            System.IO.File.Delete(fileNameArd);
                        }
                        else
                        {
                            do
                            {
                                fileNameArd = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileNameArd) + " (1)" + ".ard";
                            } while (System.IO.File.Exists(fileNameArd));
                        }
                    }
                    ard = new System.IO.BinaryWriter(System.IO.File.Open(fileNameArd, FileMode.Create));
                    ard.Write(new byte[] { modelCode }, 0, 1);
                    if (!connected)
                    {
                        var oldPosition = mdp.BaseStream.Position;
                        mdp.BaseStream.Position = mdp.BaseStream.Length - 1;
                        if (mdp.ReadByte() == 254)
                        {
                            mdp.BaseStream.Position -= 5;
                            firmwareArray = mdp.ReadBytes(3);
                        }
                        if (firmwareArray[2] == 0xff) Array.Resize(ref firmwareArray, 2);
                        mdp.BaseStream.Position = oldPosition;
                    }
                    ard.Write(firmwareArray, 0, firmwareArray.Length);
                    ard.Write(mdp.ReadBytes(254));
                }
                else if (testByte == 0xff)
                {
                    if (counter != 0)
                    {
                        break;
                    }
                }
                else
                {
                    ard.Write(mdp.ReadBytes(255));
                }
            }
            try
            {
                mdp.Close();
                ard.Close();
            }
            catch { }

            try
            {
                if ((MainWindow.lastSettings[6].Equals("false")) | (!connected)) System.IO.File.Delete(fileNameMdp);
                else
                {
                    if (!Path.GetExtension(fileNameMdp).Contains("Dump"))
                    {
                        string newFileNameMdp = Path.GetDirectoryName(fileNameMdp) + "\\" + Path.GetFileNameWithoutExtension(fileNameMdp) + ".memDump";
                        if (System.IO.File.Exists(newFileNameMdp)) System.IO.File.Delete(newFileNameMdp);
                        //string newFileNameMdp = Path.GetFileNameWithoutExtension(fileNameMdp) + ".memDump";
                        System.IO.File.Move(fileNameMdp, newFileNameMdp);
                    }
                }
            }
            catch { }
            if (!fromDownload) Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.nextFile()));
        }

        public override void convert(MainWindow parent, string fileName, string preferenceFile)
        {
            const int pref_pressMetri = 0;
            const int pref_millibars = 1;
            const int pref_dateFormat = 2;
            const int pref_timeFormat = 3;
            const int pref_fillEmpty = 4;
            const int pref_sameColumn = 5;
            const int pref_battery = 6;
            const int pref_override_time = 15;
            const int pref_metadata = 16;

            timeStamp timeStampO = new timeStamp();
            string barStatus = "";
            string[] prefs = System.IO.File.ReadAllLines(parent.prefFile);

            string shortFileName;
            string addOn = "";
            string exten = Path.GetExtension(fileName);
            if (exten.Length > 4)
            {
                addOn = ("_S" + exten.Remove(0, 4));
            }
            string fileNameCsv = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + addOn + ".csv";
            BinaryReader ard = new System.IO.BinaryReader(System.IO.File.Open(fileName, FileMode.Open));
            BinaryWriter csv = new System.IO.BinaryWriter(System.IO.File.OpenWrite(fileNameCsv));

            ard.BaseStream.Position = 1;
            firmTotA = (uint)(ard.ReadByte() * 1000 + ard.ReadByte());
            if (firmTotA > 2004)
            {
                firmTotA *= 1000;
                firmTotA += ard.ReadByte();
            }

            //Legge i coefficienti sensore di profondità
            convCoeffs = new double[] { 0, 0, 0, 0, 0, 0 };
            for (int i = 0; i < 6; i++)
            {
                convCoeffs[i] = ard.ReadByte() * 256 + ard.ReadByte();
            }

            //Imposta le preferenze di conversione

            if (prefs[pref_fillEmpty] == "False")
            {
                repeatEmptyValues = false;
            }

            dateSeparator = csvSeparator;
            if (prefs[pref_sameColumn] == "True")
            {
                sameColumn = true;
                dateSeparator = " ";
            }

            if (prefs[pref_battery] == "True")
            {
                prefBattery = true;
            }

            dateCi = new CultureInfo("it-IT");
            if (prefs[pref_timeFormat] == "2")
            {
                dateCi = new CultureInfo("en-US");
                angloTime = true;
            }

            if (MainWindow.lastSettings[5] == "air")
            {
                isDepth = false;
            }

            if (prefs[pref_pressMetri] == "meters")
            {
                inMeters = true;
            }

            timeStampO.pressOffset = float.Parse(prefs[pref_millibars]);
            dateFormat = byte.Parse(prefs[pref_dateFormat]);
            timeFormat = byte.Parse(prefs[pref_timeFormat]);
            switch (dateFormat)
            {
                case 1:
                    dateFormatParameter = "dd/MM/yyyy";
                    break;
                case 2:
                    dateFormatParameter = "MM/dd/yyyy";
                    break;
                case 3:
                    dateFormatParameter = "yyyy/MM/dd";
                    break;
                case 4:
                    dateFormatParameter = "yyyy/dd/MM";
                    break;
            }

            overrideTime = false;
            if (prefs[pref_override_time] == "True") overrideTime = true;

            metadata = false;
            if (prefs[pref_metadata] == "True") metadata = true;

            //Legge i parametri di logging
            ard.ReadByte();

            byte rrb = ard.ReadByte();
            rate = findSamplingRate(rrb);
            rateComp = rate;
            if (rate == 1)
            {
                rateComp = 5;
            }
            range = findRange(rrb);
            bits = findBits(rrb);
            bitsDiv = findBytesPerSample();

            Array.Resize(ref lastGroup, (rate * 3));
            nOutputs = rateComp;

            cifreDec = 3;
            cifreDecString = "0.000";
            if (bits)
            {
                cifreDec = 4;
                cifreDecString = "0.0000";
            }

            timeStampO.orario = findStartTime(ref prefs);

            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => barStatus = (string)parent.statusLabel.Content));
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.IsIndeterminate = false));
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
                new Action(() => parent.statusLabel.Content = barStatus + " - Converting"));

            shortFileName = Path.GetFileNameWithoutExtension(fileName);

            convertStop = false;

            ard.ReadByte();

            csvPlaceHeader(ref csv);

            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Maximum = ard.BaseStream.Length - 1));

            while (!convertStop)
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Value = ard.BaseStream.Position));
                if (detectEof(ref ard)) break;

                try
                {
                    decodeTimeStamp(ref ard, ref timeStampO);
                }
                catch
                {
                    MessageBox.Show("Timestamp: " + ard.BaseStream.Position.ToString("X8"));
                }


                if (timeStampO.stopEvent > 0)
                {
                    csv.Write(System.Text.Encoding.ASCII.GetBytes(groupConverter(ref timeStampO, lastGroup, shortFileName)));
                    break;
                }

                try
                {
                    csv.Write(System.Text.Encoding.ASCII.GetBytes(groupConverter(ref timeStampO, extractGroup(ref ard, ref timeStampO), shortFileName)));
                }
                catch { }

            }

            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Value = ard.BaseStream.Position));

            csv.Close();
            ard.Close();

            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.nextFile()));
        }

        private double[] extractGroup(ref BinaryReader ard, ref timeStamp tsc)
        {
            byte[] group = new byte[2000];
            bool badGroup = false;
            long position = 0;
            byte dummy, dummyExt;
            ushort badPosition = 600;

            if (ard.BaseStream.Position == ard.BaseStream.Length) return lastGroup;

            do
            {
                dummy = ard.ReadByte();
                if (dummy == 0xab)
                {
                    if (ard.BaseStream.Position < ard.BaseStream.Length) dummyExt = ard.ReadByte();
                    else return lastGroup;

                    if (dummyExt == 0xab)
                    {
                        group[position] = (byte)0xab;
                        position += 1;
                        dummy = 0;
                    }
                    else
                    {
                        ard.BaseStream.Position -= 1;
                        if (badGroup)
                        {
                            System.IO.File.AppendAllText(((FileStream)ard.BaseStream).Name + "errorList.txt", "-> " + ard.BaseStream.Position.ToString("X8") + "\r\n");
                        }
                    }
                }
                else
                {
                    if (position < badPosition)
                    {
                        group[position] = dummy;
                        position++;
                    }
                    else if ((position == badPosition) && (!badGroup))
                    {
                        badGroup = true;
                        System.IO.File.AppendAllText(((FileStream)ard.BaseStream).Name + "errorList.txt", "-> " + ard.BaseStream.Position.ToString("X8") + "\r\n");
                    }
                }
            } while ((dummy != (byte)0xab) && (ard.BaseStream.Position < ard.BaseStream.Length));

            //Array.Resize(ref group, (int)position);

            tsc.timeStampLength = (byte)(position / bitsDiv);
            //IntPtr doubleResultArray = Marshal.AllocCoTaskMem(sizeof(double) * nOutputs * 3);
            int resultCode = 0;
            double[] doubleResultArray = new double[nOutputs * 3];
            if (bits)
            {
                resultCode = resample4(group, (int)tsc.timeStampLength, doubleResultArray, nOutputs);
            }
            else
            {
                resultCode = resample3(group, (int)tsc.timeStampLength, doubleResultArray, nOutputs);
            }
            //doubleResult = new double[(nOutputs * 3)];
            //Marshal.Copy(doubleResultArray, doubleResult, 0, nOutputs * 3);
            //Marshal.FreeCoTaskMem(doubleResultArray);
            return doubleResultArray;
        }

        private string groupConverter(ref timeStamp tsLoc, double[] group, string unitName)
        {
            //if (group.Length == 0) return ("");

            double x, y, z;
            string ampm = "";
            string textOut, dateTimeS, additionalInfo;
            string dateS = "";
            ushort milli;
            NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;
            ushort contoTab = 0;

            dateS = tsLoc.orario.ToString(dateFormatParameter);

            dateTimeS = dateS + dateSeparator + tsLoc.orario.ToString("T", dateCi);
            if (angloTime)
            {
                ampm = dateTimeS.Split(' ')[dateTimeS.Split(' ').Length - 1];
                dateTimeS = dateTimeS.Remove(dateTimeS.Length - 3, 3);
            }

            milli = 0;

            //textOut = "";

            x = group[0]; y = group[1]; z = group[2];

            x *= gCoeff; x = Math.Round(x, cifreDec);
            y *= gCoeff; y = Math.Round(y, cifreDec);
            z *= gCoeff; z = Math.Round(z, cifreDec);

            textOut = unitName + csvSeparator + dateTimeS + ".000";
            if (angloTime) textOut += " " + ampm;
            textOut += csvSeparator + x.ToString(cifreDecString, nfi) + csvSeparator + y.ToString(cifreDecString, nfi) + csvSeparator + z.ToString(cifreDecString, nfi);

            additionalInfo = "";

            //Inserisce la pressione e la temperatura
            contoTab = 1;
            additionalInfo += csvSeparator;
            if (((tsLoc.tsType & 4) == 4) | repeatEmptyValues) additionalInfo += tsLoc.press.ToString(nfi);

            contoTab = 1;
            additionalInfo += csvSeparator;
            if (((tsLoc.tsType & 2) == 2) | repeatEmptyValues) additionalInfo += tsLoc.temp.ToString(nfi);

            //Inserisce la batteria
            if (prefBattery)
            {
                contoTab += 1;
                additionalInfo += csvSeparator;
                if (((tsLoc.tsType & 8) == 8) | repeatEmptyValues) additionalInfo += tsLoc.batteryLevel.ToString(nfi);
            }

            //Inserisce i metadati
            if (metadata)
            {
                contoTab += 1;
                additionalInfo += csvSeparator;
                if (tsLoc.stopEvent > 0)
                {
                    switch (tsLoc.stopEvent)
                    {
                        case 1:
                            additionalInfo += "Low battery.";
                            break;
                        case 2:
                            additionalInfo += "Power off command received.";
                            break;
                        case 3:
                            additionalInfo += "Memory full.";
                            break;
                    }
                    textOut += additionalInfo + "\r\n";
                    return textOut;
                }
            }

            textOut += additionalInfo + "\r\n";

            if (tsLoc.stopEvent > 0) return textOut;

            if (!repeatEmptyValues)
            {
                additionalInfo = "";
                for (ushort ui = 0; ui < contoTab; ui++) additionalInfo += csvSeparator;
            }

            milli += addMilli;
            dateTimeS += ".";
            if (tsLoc.stopEvent > 0) bitsDiv = 1;

            var iend = (short)((rate * 3));

            for (short i = 3; i < iend; i += 3)
            {
                x = group[i];
                y = group[i + 1];
                z = group[i + 2];

                x *= gCoeff; x = Math.Round(x, cifreDec);
                y *= gCoeff; y = Math.Round(y, cifreDec);
                z *= gCoeff; z = Math.Round(z, cifreDec);

                if (rate == 1)
                {
                    tsLoc.orario = tsLoc.orario.AddSeconds(1);
                    dateTimeS = dateS + csvSeparator + tsLoc.orario.ToString("T", dateCi) + ".";
                }
                textOut += unitName + csvSeparator + dateTimeS + milli.ToString("D3");

                if (angloTime) textOut += " " + ampm;
                textOut += csvSeparator + x.ToString(cifreDecString, nfi) + csvSeparator + y.ToString(cifreDecString, nfi) + csvSeparator + z.ToString(cifreDecString, nfi);

                textOut += additionalInfo + "\r\n";
                milli += addMilli;
            }

            return textOut;
        }

        private DateTime findStartTime(ref string[] prefs)
        {
            const int pref_h = 9;
            const int pref_m = 10;
            const int pref_s = 11;
            const int pref_date_year = 12;
            const int pref_date_month = 13;
            const int pref_date_day = 14;

            DateTime dt = new DateTime(int.Parse(prefs[pref_date_year]), int.Parse(prefs[pref_date_month]), int.Parse(prefs[pref_date_day]),
                int.Parse(prefs[pref_h]), int.Parse(prefs[pref_m]), int.Parse(prefs[pref_s]));

            dt.AddSeconds(29);

            return dt;
        }

        private ushort findSamplingRate(byte rateIn)
        {
            byte rateOut;
            rateIn = (byte)(rateIn >> 4);
            if (rateIn > 7) rateIn -= 8;

            switch (rateIn)
            {
                case 0:
                    rateOut = 50;
                    break;
                case 1:
                    rateOut = 25;
                    break;
                case 2:
                    rateOut = 100;
                    break;
                case 3:
                    rateOut = 10;
                    break;
                case 4:
                    rateOut = 1;
                    break;
                default:
                    rateOut = 50;
                    break;
            }

            if (rateOut == 1) addMilli = 0;

            else addMilli = (ushort)((1 / (double)rateOut) * 1000);

            return rateOut;


        }

        private byte findRange(byte rangeIn)
        {
            rangeIn &= 15;
            if (rangeIn > 7) rangeIn -= 8;

            byte rangeOut = (byte)(Math.Pow(2, (rangeIn + 1)));

            return rangeOut;
        }

        private bool findBits(byte bitsIn)
        {
            bitsIn &= 15;
            //sogliaNeg = 127;
            //rendiNeg = 256;
            gCoeff = range * 2.0 / 256;
            if (bitsIn > 7)
            {
                gCoeff /= 4;
                return true;
            }
            return false;
        }

        private byte findBytesPerSample()
        {
            byte bitsDiv = 3;
            if (bits) bitsDiv = 4;
            return bitsDiv;
        }

        private void decodeTimeStamp(ref BinaryReader ard, ref timeStamp tsc)
        {
            tsc.stopEvent = 0;

            tsc.tsType = ard.ReadByte();

            if (tsc.tsType >= 0xfd)
            {
                switch (tsc.tsType)
                {
                    case 0xfd:
                        tsc.stopEvent = 3;
                        break;
                    case 0xfe:
                        tsc.stopEvent = 1;
                        break;
                    case 0xff:
                        tsc.stopEvent = 2;
                        break;
                }
                return;
            }


            if ((tsc.tsType & 1) == 1)
            {
                try
                {
                    tsc.tsTypeExt1 = ard.ReadByte();
                    if ((tsc.tsTypeExt1 & 1) == 1) tsc.tsTypeExt2 = ard.ReadByte();
                }
                catch
                {
                    return;
                }
            }

            if (((tsc.tsType & 2) == 2) | ((tsc.tsType & 4) == 4))
            {
                if (isDepth)
                {
                    if (pressureDepth5837(ref ard, ref tsc)) return;
                }
                else
                {
                    if (pressureAir(ref ard, ref tsc)) return;
                }
            }


            if ((tsc.tsType & 8) == 8)
            {
                tsc.batteryLevel = (float)Math.Round(((((float)((ard.ReadByte() * 256) + ard.ReadByte())) * 6) / 4096), 2);
            }

            tsc.orario = tsc.orario.AddSeconds(1);

            if ((tsc.tsType & 1) == 1)
            {
                if (((tsc.tsTypeExt1 & 32) == 32) && (!overrideTime))
                {
                    int anno = ard.ReadByte();
                    anno = ((anno >> 4) * 10) + (anno & 15) + 2000;
                    ard.ReadByte();

                    int giorno = ard.ReadByte();
                    giorno = ((giorno >> 4) * 10) + (giorno & 15);

                    int mese = ard.ReadByte();
                    mese = ((mese >> 4) * 10) + (mese & 15);

                    int ore = ard.ReadByte();
                    ore = ((ore >> 4) * 10) + (ore & 15);
                    ard.ReadByte();

                    int secondi = ard.ReadByte();
                    secondi = ((secondi >> 4) * 10) + (secondi & 15);

                    int minuti = ard.ReadByte();
                    minuti = ((minuti >> 4) * 10) + (minuti & 15);

                    tsc.orario = new DateTime(anno, mese, giorno, ore, minuti, secondi);
                }
            }
        }

        private bool pressureAir(ref BinaryReader ard, ref timeStamp tsc)
        {
            double dT, off, sens, t2, off2, sens2;
            double d1, d2;

            try
            {
                d2 = (uint)(ard.ReadByte() * 65536 + ard.ReadByte() * 256 + ard.ReadByte());
            }
            catch
            {
                return true;
            }

            dT = d2 - convCoeffs[4] * 256;
            tsc.temp = (float)(2000 + (dT * convCoeffs[5]) / 8388608);
            off = convCoeffs[1] * 131072 + (convCoeffs[3] * dT) / 64;
            sens = convCoeffs[0] * 65536 + (convCoeffs[2] * dT) / 128;
            if (tsc.temp > 2000)
            {
                t2 = 0;
                off2 = 0;
                sens2 = 0;
            }
            else
            {
                t2 = Math.Pow(dT, 2) / 2147483648;
                off2 = 61 * Math.Pow((tsc.temp - 2000), 2) / 16;
                sens2 = 2 * Math.Pow((tsc.temp - 2000), 2);
                if (tsc.temp < -1500)
                {
                    off2 += 20 * Math.Pow((tsc.temp + 1500), 2);
                    sens2 += 12 * Math.Pow((tsc.temp + 1500), 2);
                }
            }
            tsc.temp -= (float)t2;
            off -= off2;
            sens -= sens2;
            tsc.temp /= 100;
            tsc.temp = (float)Math.Round(tsc.temp, 1);

            if ((tsc.tsType & 4) == 4)
            {
                try
                {
                    d1 = (uint)(ard.ReadByte() * 65536 + ard.ReadByte() * 256 + ard.ReadByte());
                }
                catch
                {
                    return true;
                }
                tsc.press = (float)(((d1 * sens / 2097152) - off) / 32768);
                tsc.press /= 100;
                tsc.press = (float)Math.Round(tsc.press, 2);
            }
            return false;
        }

        private bool pressureDepth5837(ref BinaryReader ard, ref timeStamp tsc)
        {
            double dT;
            double off;
            double sens;
            double d1, d2;

            try
            {
                d2 = (ard.ReadByte() * 65536 + ard.ReadByte() * 256 + ard.ReadByte());
            }
            catch
            {
                return true;
            }

            dT = d2 - convCoeffs[4] * 256;
            tsc.temp = (float)(2000 + (dT * convCoeffs[5]) / 8388608);
            off = convCoeffs[1] * 65536 + (convCoeffs[3] * dT) / 128;
            sens = convCoeffs[0] * 32768 + (convCoeffs[2] * dT) / 256;
            if (tsc.temp > 2000)
            {
                tsc.temp -= (float)((2 * Math.Pow(dT, 2)) / 137438953472);
                off -= ((Math.Pow((tsc.temp - 2000), 2)) / 16);
            }
            else
            {
                off -= 3 * ((Math.Pow((tsc.temp - 2000), 2)) / 2);
                sens -= 5 * ((Math.Pow((tsc.temp - 2000), 2)) / 8);
                if (tsc.temp < -1500)
                {
                    off -= 7 * Math.Pow((tsc.temp + 1500), 2);
                    sens -= 4 * Math.Pow((tsc.temp + 1500), 2);
                }
                tsc.temp -= (float)(3 * (Math.Pow(dT, 2))) / 8589934592;
            }
            tsc.temp = (float)Math.Round((tsc.temp / 100), 1);
            if ((tsc.tsType & 4) == 4)
            {
                try
                {
                    d1 = (uint)(ard.ReadByte() * 65536 + ard.ReadByte() * 256 + ard.ReadByte());
                }
                catch
                {
                    return true;
                }
                tsc.press = (float)Math.Round((((d1 * sens / 2097152) - off) / 81920), 1);
                if (inMeters)
                {
                    tsc.press -= tsc.pressOffset;
                    if (tsc.press <= 0) tsc.press = 0;
                    else
                    {
                        tsc.press = (float)(tsc.press / 98.1);
                        tsc.press = (float)Math.Round(tsc.press, 2);
                    }
                }
            }
            return false;
        }

        private void csvPlaceHeader(ref BinaryWriter csv)
        {
            string csvHeader = "TagID";
            if (sameColumn)
            {
                csvHeader = csvHeader + csvSeparator + "Timestamp";
            }
            else
            {
                csvHeader = csvHeader + csvSeparator + "Date" + csvSeparator + "Time";
            }

            csvHeader = csvHeader + csvSeparator + "X" + csvSeparator + "Y" + csvSeparator + "Z";

            if (!inMeters)
            {
                csvHeader += csvSeparator + "Pressure";
            }
            else
            {
                if (isDepth)
                {
                    csvHeader += csvSeparator + "Depth";
                }
                else
                {
                    csvHeader += csvSeparator + "Altitude";
                }
            }

            csvHeader += csvSeparator + "Temp. (°C)";

            if (prefBattery)
            {
                csvHeader = csvHeader + csvSeparator + "Battery Voltage (V)";
            }

            if (metadata)
            {
                csvHeader = csvHeader + csvSeparator + "Metadata";
            }

            csvHeader += "\r\n";
            csv.Write(System.Text.Encoding.ASCII.GetBytes(csvHeader));
        }

        private bool detectEof(ref BinaryReader ard)
        {
            if (ard.BaseStream.Position >= ard.BaseStream.Length)
            {
                return true;
            }
            return false;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    //Release managed resources.
                }
                // Release unmanaged resources.
                // Set large fields to null.
                disposed = true;
            }
            base.Dispose(disposing);
        }
    }
}
