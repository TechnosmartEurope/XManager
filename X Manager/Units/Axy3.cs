using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.IO;
using System.Windows;
using System.Globalization;
#if X64
using FT_HANDLE = System.UInt64;
#else
    using FT_HANDLE = System.UInt32;
#endif

namespace X_Manager.Units
{
    class Axy3 : Unit
    {

        struct timeStamp
        {
            public byte tsType;
            //public byte tsTypeExt1;
            //public byte tsTypeExt2;
            public float batteryLevel;
            public float temp;
            public DateTime orario;
            public byte stopEvent;
            public byte timeStampLength;
        }

        byte dateFormat;
        byte timeFormat;
        bool sameColumn = false;
        bool prefBattery = false;
        bool repeatEmptyValues = true;
        bool bits;
        byte bitsDiv;
        bool angloTime = false;
        ushort rate;
        ushort rateComp;
        byte range;
        double gCoeff;
        string dateFormatParameter;
        ushort addMilli;
        bool metadata;
        CultureInfo dateCi;
        byte cifreDec;
        string cifreDecString;


        public Axy3(object p)
            : base(p)
        {
            base.positionCanSend = false;
            configurePositionButtonEnabled = false;
            modelCode = model_axy3;
            modelName = "Axy-3";
            defaultArdExtension = "ard1";
        }

        public override void abortConf()
        {

        }

        public override string askBattery()
        {
            string battery = "";
            double battLevel;
            sp.Write("b");
            try
            {
                battLevel = sp.ReadByte(); battLevel *= 256;
                battLevel += sp.ReadByte();
                battLevel *= 6;
                battLevel /= 1024;
            }
            catch
            {
                throw new Exception(unitNotReady);
            }
            battery = Math.Round(battLevel, 2).ToString("0.00") + "V";
            return battery;
        }

        public override string askFirmware()
        {
            byte[] f = new byte[2];
            string firmware;
            try
            {
                firmware = sp.ReadLine();
            }
            catch
            {
                throw new Exception(unitNotReady);
            }
            f[0] = byte.Parse(firmware.Split('.')[0]);
            f[1] = byte.Parse(firmware.Split('.')[1]);

            firmTotA = 0;
            for (int i = 0; i < 3; i++)
            {
                firmTotA *= 1000;
                firmTotA += f[i];
            }

            firmwareArray = f;

            return firmware;
        }

        public override uint askMaxMemory()
        {
            uint m;
            sp.Write("m");
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

        public override uint[] askMemory()
        {
            uint m;
            sp.Write("M");
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
			return new uint[] { m };
		}

        public override string askName()
        {
            string unitNameBack = sp.ReadExisting();

            name = formatName(unitNameBack);
            return name;
        }

        public override void setName(string newName)
        {
            if (newName.Length < 10)
            {
                for (int i = newName.Length; i <= 9; i++) newName += " ";
            }
            sp.Write("N");
            try
            {
                sp.ReadByte();
                sp.Write(newName);
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
            sp.Write("B");
        }

        public unsafe override void download(string fileName, uint fromMemory, uint toMemory, int baudrate)
        {
            convertStop = false;
            uint actMemory = fromMemory;
            System.IO.FileMode fm = System.IO.FileMode.Create;
            if (fromMemory != 0) fm = System.IO.FileMode.Append;

            string fileNameMdp = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + ".mdp";
            var fo = new System.IO.BinaryWriter(File.Open(fileNameMdp, fm));

            sp.Write("d");
            try
            {
                sp.ReadByte();
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

            sp.BaudRate = 3000000;

            Thread.Sleep(200);

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
                    bytesToWrite = 2;
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
                    fo.Write(inBuffer, 0, 4096);
                }

                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Value = actMemory));

                if (convertStop) actMemory = toMemory;
            }

            fo.Write(firmwareArray, 0, firmwareArray.Length);
            for (int i = firmwareArray.Length; i <= 2; i++)
            {
                fo.Write(new byte[] { 0xff }, 0, 1);
            }
            fo.Write(new byte[] { model_axy3, (byte)254 }, 0, 2);

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
                if (Parent.lastSettings[6].Equals("false"))
                {
                    try
                    {
                        //System.IO.File.Delete(fileNameMdp);
                        fDel(fileNameMdp);
                    }
                    catch { }
                }
            }

            Thread.Sleep(300);
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.downloadFinished()));
        }

        public override void eraseMemory()
        {
            sp.Write("E");
            try
            {
                sp.ReadByte();
            }
            catch
            {
                throw new Exception(unitNotReady);
            }
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
                            //System.IO.File.Delete(fileNameArd);
                            fDel(fileNameArd);
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
                if ((Parent.lastSettings[6].Equals("false")) | (!connected)) fDel(fileNameMdp); //System.IO.File.Delete(fileNameMdp);
                else
                {
                    if (!Path.GetExtension(fileNameMdp).Contains("Dump"))
                    {
                        string newFileNameMdp = Path.GetDirectoryName(fileNameMdp) + "\\" + Path.GetFileNameWithoutExtension(fileNameMdp) + ".memDump";
                        if (System.IO.File.Exists(newFileNameMdp)) fDel(newFileNameMdp); //System.IO.File.Delete(newFileNameMdp);
                        //string newFileNameMdp = Path.GetFileNameWithoutExtension(fileNameMdp) + ".memDump";
                        System.IO.File.Move(fileNameMdp, newFileNameMdp);
                    }
                }
            }
            catch { }
            if (!fromDownload) Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.nextFile()));
        }

        public override byte[] getConf()
        {
            byte[] conf = new byte[30];
            for (byte i = 0; i < 30; i++) conf[i] = 0xff;

            conf[22] = 0;
            conf[25] = modelCode;

            sp.ReadExisting();
            sp.Write("C");
            try
            {
                sp.ReadByte();
                for (int i = 15; i <= 17; i++) { conf[i] = (byte)sp.ReadByte(); }
            }
            catch
            {
                throw new Exception(unitNotReady);
            }
            return conf;
        }

        public override void setConf(byte[] conf)
        {
            sp.Write(conf, 15, 3);
            try
            {
                sp.ReadLine();
            }
            catch
            {
                throw new Exception(unitNotReady);
            }
        }

        public override void convert(string fileName, string[] prefs)
        {
            timeStamp timeStampO = new timeStamp();
            string barStatus = "";

            string shortFileName;
            string addOn = "";
            string exten = Path.GetExtension(fileName);
            if ((exten.Length > 4)) addOn = ("_S" + exten.Remove(0, 4));
            string fileNameCsv = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + addOn + ".csv";
            BinaryReader ard = new System.IO.BinaryReader(System.IO.File.Open(fileName, FileMode.Open));
            BinaryWriter csv = new System.IO.BinaryWriter(System.IO.File.OpenWrite(fileNameCsv));

            ard.BaseStream.Position = 1;
            firmTotA = (uint)(ard.ReadByte() * 1000 + ard.ReadByte());
            if (firmTotA > 2004)
            {
                firmTotA *= 1000; firmTotA += ard.ReadByte();
            }

            //Imposta le preferenze di conversione

            if ((prefs[pref_fillEmpty] == "False")) repeatEmptyValues = false;

            dateSeparator = csvSeparator;
            if ((prefs[pref_sameColumn] == "True"))
            {
                sameColumn = true;
                dateSeparator = " ";
            }

            if (prefs[pref_battery] == "True") prefBattery = true;

            dateCi = new CultureInfo("it-IT");
            angloTime = false;
            if (prefs[pref_timeFormat] == "2")
            {
                angloTime = true;
                dateCi = new CultureInfo("en-US");
            }

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

            metadata = false;
            if (prefs[pref_metadata] == "True") metadata = true;

            //Legge i parametri di logging
            ard.ReadByte();

            byte rrb = ard.ReadByte();
            rate = findSamplingRate(rrb);
            rateComp = rate;
            if (rate == 1) rateComp = 10;
            range = findRange(rrb);
            bits = findBits(rrb);
            bitsDiv = findBytesPerSample();

            //Array.Resize(ref lastGroup, ((rate * 3)));
            lastGroup = new double[(rate * 3)];

            nOutputs = rateComp;

            cifreDec = 3; cifreDecString = "0.000";
            if (bits)
            {
                cifreDec = 4; cifreDecString = "0.0000";
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

            //Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
            //    new Action(() => parent.statusProgressBar.Maximum = ard.BaseStream.Length - 1));
            progMax = ard.BaseStream.Length - 1;
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
                new Action(() => parent.statusProgressBar.Maximum = ard.BaseStream.Length - 1));
            progressWorker.RunWorkerAsync();

            decodeFirstTimeStamp(ref ard, ref timeStampO);

            if (timeStampO.stopEvent > 0)
            {
                csv.Write(System.Text.Encoding.ASCII.GetBytes(groupConverter(ref timeStampO, lastGroup, shortFileName)));

                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
                            new Action(() => parent.statusProgressBar.Value = ard.BaseStream.Position));

                csv.Close();
                ard.Close();

                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.nextFile()));
                return;
            }

            try
            {
                csv.Write(System.Text.Encoding.ASCII.GetBytes(groupConverter(ref timeStampO, extractGroup(ref ard, ref timeStampO), shortFileName)));
            }
            catch
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.nextFile()));
                return;
            }

            while (!convertStop)
            {

                while (Interlocked.Exchange(ref progLock, 2) > 0) { }

                progVal = ard.BaseStream.Position;
                Interlocked.Exchange(ref progLock, 0);
                //Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
                //            new Action(() => parent.statusProgressBar.Value = ard.BaseStream.Position));
                if (detectEof(ref ard)) break;

                decodeTimeStamp(ref ard, ref timeStampO);

                if (timeStampO.stopEvent > 0)
                {
                    csv.Write(System.Text.Encoding.ASCII.GetBytes(groupConverter(ref timeStampO, lastGroup, shortFileName)));
                    break;
                }

                try
                {
                    csv.Write(System.Text.Encoding.ASCII.GetBytes(
                        groupConverter(ref timeStampO, extractGroup(ref ard, ref timeStampO), shortFileName)));
                }
                catch { }

            }
            while (Interlocked.Exchange(ref progLock, 2) > 0) { }
            progVal = ard.BaseStream.Position;
            Thread.Sleep(300);
            progVal = -1;
            Interlocked.Exchange(ref progLock, 0);
            //Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Value = ard.BaseStream.Position));

            csv.Close();
            ard.Close();

            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.nextFile()));
        }

        private string groupConverter(ref timeStamp tsLoc, double[] group, string unitName)
        {
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

            textOut = "";

            x = group[0]; y = group[1]; z = group[2];

            x *= gCoeff; x = Math.Round(x, cifreDec);
            y *= gCoeff; y = Math.Round(y, cifreDec);
            z *= gCoeff; z = Math.Round(z, cifreDec);

            textOut = unitName + csvSeparator + dateTimeS + ".000";
            if (angloTime) textOut += " " + ampm;
            textOut += csvSeparator + x.ToString(cifreDecString, nfi) + csvSeparator + y.ToString(cifreDecString, nfi) + csvSeparator + z.ToString(cifreDecString, nfi);

            additionalInfo = "";

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
                        case 4:
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

            else addMilli = (ushort)((1.0 / rateOut) * 1000);

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
			if (bitsIn < 8)
			{
				switch (range)
				{
					case 2:
						gCoeff = 15.63;
						break;
					case 4:
						gCoeff = 31.26;
						break;
					case 8:
						gCoeff = 62.52;
						break;
					case 16:
						gCoeff = 187.58;
						break;
				}
				return false;
			}
			else
			{
				switch (range)
				{
					case 2:
						gCoeff = 3.9;
						break;
					case 4:
						gCoeff = 7.82;
						break;
					case 8:
						gCoeff = 15.63;
						break;
					case 16:
						gCoeff = 46.9;
						break;
				}
				return true;
			}
		}

        private byte findBytesPerSample()
        {
            byte bitsDiv = 3;
            if (bits) bitsDiv = 4;
            return bitsDiv;
        }

        private void decodeFirstTimeStamp(ref BinaryReader ard, ref timeStamp tsc)
        {
            tsc.stopEvent = 0;

            byte newType = 0;

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


            if ((tsc.tsType % 10) > 0)
            {
                try
                {
                    tsc.temp = (float)(ard.ReadByte() * 256 + ard.ReadByte());
                }
                catch
                {
                    return;
                }
                if (tsc.temp > 511) tsc.temp -= 1024;
                tsc.temp = (float)Math.Round(((tsc.temp * 0.1221) + 22.5), 2);
                newType += 2;
            }

            if (tsc.tsType > 9)
            {
                try
                {
                    tsc.batteryLevel = (float)(((ard.ReadByte() * 256.0 + ard.ReadByte()) * 6) / 1024);
                    if (tsc.batteryLevel > 5) tsc.batteryLevel /= 4;
                    tsc.batteryLevel = (float)Math.Round(tsc.batteryLevel, 2);
                }
                catch
                {
                    return;
                }
                newType += 8;
            }

            tsc.tsType = newType;

            tsc.orario = tsc.orario.AddSeconds(1);
        }

        private void decodeTimeStamp(ref BinaryReader ard, ref timeStamp tsc)
        {
            tsc.stopEvent = 0;

            byte newType = 0;

            tsc.tsType = ard.ReadByte();

            if (tsc.tsType == 0xfF)
            {
                tsc.stopEvent = 4;
                return;
            }


            if ((tsc.tsType % 10) > 0)
            {
                try
                {
                    tsc.temp = (float)(ard.ReadByte() * 256 + ard.ReadByte());
                }
                catch
                {
                    return;
                }
                if (tsc.temp > 511) tsc.temp -= 1024;
                tsc.temp = (float)Math.Round(((tsc.temp * 0.1221) + 22.5), 2);
                newType += 2;
            }

            if (tsc.tsType > 9)
            {
                try
                {
                    tsc.batteryLevel = (float)(((ard.ReadByte() * 256.0 + ard.ReadByte()) * 6) / 1024);
                    if (tsc.batteryLevel > 5) tsc.batteryLevel /= 4;
                    tsc.batteryLevel = (float)Math.Round(tsc.batteryLevel, 2);
                }
                catch
                {
                    return;
                }
                newType += 8;
            }
            tsc.tsType = newType;
            tsc.orario = tsc.orario.AddSeconds(1);
        }

        private void csvPlaceHeader(ref BinaryWriter csv)
        {
            string csvHeader = "TagID";
            if (sameColumn)
            {
                csvHeader += csvSeparator + "Timestamp";
            }
            else
            {
                csvHeader += csvSeparator + "Date" + csvSeparator + "Time";
            }

            csvHeader += csvSeparator + "X" + csvSeparator + "Y" + csvSeparator + "Z";
            csvHeader += csvSeparator + "Temp. (°C)";
            if (prefBattery)
            {
                csvHeader += csvSeparator + "Battery Voltage (V)";
            }

            if (metadata)
            {
                csvHeader += csvSeparator + "Metadata";
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
            double[] doubleResult=new double[nOutputs*3];
            if (bits)
            {
                resultCode = resample4(group, (int)tsc.timeStampLength, doubleResult, nOutputs);
            }
            else
            {
                resultCode = resample3(group, (int)tsc.timeStampLength, doubleResult, nOutputs);
            }
            //doubleResult = new double[(nOutputs * 3)];
            //Marshal.Copy(doubleResultArray, doubleResult, 0, nOutputs * 3);
            //Marshal.FreeCoTaskMem(doubleResultArray);
            return doubleResult;
        }

    }
}
