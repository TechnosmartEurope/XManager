﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using System.IO;
using System.Runtime.InteropServices;
#if X64
using FT_HANDLE = System.UInt64;
#else
    using FT_HANDLE = System.UInt32;
#endif

namespace X_Manager.Units
{
    class CO2_Logger : Units.Unit
    {

        struct timeStamp
        {
            public byte tsType;
            public byte tsTypeExt1;
            public byte tsTypeExt2;
            public bool co2NewValues;
            public DateTime orario;
            public float batteryLevel;
            public byte stopEvent;
            //public byte timeStampLength;
            public float temperature;
            public float temperatureMS;
            public float humidity;
            public byte co2Current;
            public float co2Voltage;
            public ushort co2Baseline;
            public ushort co2Raw;
            public byte co2Error;
        }

        byte dateFormat;
        byte timeFormat;
        bool sameColumn = false;
        bool prefBattery = false;
        bool tempMS = false;
        bool repeatEmptyValues = true;
        bool angloTime = false;
        ushort rate;
        string dateFormatParameter;
        bool metadata;
        CultureInfo dateCi;
        double[] convCoeffs = new double[7];

        public CO2_Logger(object p)
            : base(p)
        {
            base.positionCanSend = true;
            configurePositionButtonEnabled = false;
            modelCode = model_Co2Logger;
            modelName = "CO2 Logger";
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
                battLevel *= 6.6;
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

        public override uint[] askMemory()
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
			return new uint[] { m };
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
                conf[22] = (byte)sp.ReadByte();
                conf[24] = (byte)sp.ReadByte();
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
                sp.Write(conf, 22, 1);
                sp.Write(conf, 24, 1);
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
            try
            {
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
            fo.Write(new byte[] { model_Co2Logger, (byte)254 }, 0, 2);

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

        public override void disconnect()
        {
            base.disconnect();
            sp.Write("TTTTTTTTTGGAO");
        }

        public override void convert(MainWindow parent, string fileName, string preferenceFile)
        {
            const int pref_dateFormat = 2;
            const int pref_timeFormat = 3;
            const int pref_fillEmpty = 4;
            const int pref_sameColumn = 5;
            const int pref_battery = 6;
            const int pref_metadata = 16;

            timeStamp timeStampO = new timeStamp();
            string barStatus = "";
            string[] prefs = System.IO.File.ReadAllLines(MainWindow.prefFile);

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

            byte rrb = ard.ReadByte();
            rate = findSamplingRate(rrb);

            if (firmTotA > 1001)
            {
                convCoeffs = new double[] { 0, 0, 0, 0, 0, 0 };
                for (int i = 0; i < 6; i++)
                {
                    convCoeffs[i] = ard.ReadByte() * 256 + ard.ReadByte();
                    if (convCoeffs[i] != 0)
                    {
                        tempMS = true;
                    }
                }
            }

            timeStampO.orario = findStartTime(ref prefs);

            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => barStatus = (string)parent.statusLabel.Content));
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.IsIndeterminate = false));
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
                new Action(() => parent.statusLabel.Content = barStatus + " - Converting"));

            shortFileName = Path.GetFileNameWithoutExtension(fileName);

            convertStop = false;

            csvPlaceHeader(ref csv);

            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Maximum = ard.BaseStream.Length - 1));

            while ((!convertStop) & (timeStampO.stopEvent == 0))
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Value = ard.BaseStream.Position));

                try
                {
                    while (ard.ReadByte() != 0xab) ;
                }
                catch
                {
                    if (detectEof(ref ard)) break;
                }

                decodeTimeStamp(ref ard, ref timeStampO);

                try
                {
                    csv.Write(System.Text.Encoding.ASCII.GetBytes(groupConverter(ref timeStampO, shortFileName)));
                }
                catch { }

            }

            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.statusProgressBar.Value = ard.BaseStream.Position));

            csv.Close();
            ard.Close();

            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => parent.nextFile()));
        }

        private string groupConverter(ref timeStamp tsLoc, string unitName)
        {
            string ampm = "";
            string textOut, dateTimeS, additionalInfo;
            string dateS = "";
            NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;
            ushort contoTab = 0;

            dateS = tsLoc.orario.ToString(dateFormatParameter);

            dateTimeS = dateS + dateSeparator + tsLoc.orario.ToString("T", dateCi);
            if (angloTime)
            {
                ampm = dateTimeS.Split(' ')[dateTimeS.Split(' ').Length - 1];
                dateTimeS = dateTimeS.Remove(dateTimeS.Length - 3, 3);
            }

            textOut = unitName + csvSeparator + dateTimeS;
            if (angloTime) textOut += " " + ampm;

            additionalInfo = "";

            contoTab += 8;
            additionalInfo += csvSeparator + tsLoc.temperature.ToString(nfi);
            if (tempMS)
            {
                additionalInfo += csvSeparator + tsLoc.temperatureMS.ToString(nfi);
            }
            additionalInfo += csvSeparator + tsLoc.humidity.ToString(nfi);
            additionalInfo += csvSeparator + tsLoc.co2Current.ToString(nfi) + csvSeparator + tsLoc.co2Voltage.ToString(nfi);
            additionalInfo += csvSeparator + tsLoc.co2NewValues.ToString() + csvSeparator + tsLoc.co2Raw.ToString(nfi);
            additionalInfo += csvSeparator + tsLoc.co2Baseline.ToString(nfi) + csvSeparator + tsLoc.co2Error.ToString(nfi);

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

            return textOut;
        }

        private ushort findSamplingRate(byte rateIn)
        {
            byte rateOut = 0;

            switch (rateIn)
            {
                case 1:
                    rateOut = 1;
                    break;
                case 2:
                    rateOut = 10;
                    break;
                case 3:
                    rateOut = 60;
                    break;
            }
            return rateOut;
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

            csvHeader += csvSeparator + "ENS Temp. (°C)";

            if (tempMS)
            {
                csvHeader += csvSeparator + "MS Temp. (°C)";
            }

            csvHeader += csvSeparator + "Humidity (%)" + csvSeparator + "Current (uA)" + csvSeparator + "Voltage (V)"
                + csvSeparator + "New Sample" + csvSeparator + "RawValues" + csvSeparator + "Baseline" + csvSeparator + "ErrorLog";



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
                    if ((tsc.tsTypeExt1 & 1) == 1)
                    {
                        tsc.tsTypeExt2 = ard.ReadByte();
                    }
                }
                catch
                {
                    return;
                }
            }

            if ((tsc.tsType & 2) == 2)
            {
                if (pressureDepth5837(ref ard, ref tsc)) return;
            }


            if ((tsc.tsType & 8) == 8)
            {
                tsc.batteryLevel = (float)Math.Round(((((float)((ard.ReadByte() * 256) + ard.ReadByte())) * 6) / 4096), 2);
            }

            tsc.orario = tsc.orario.AddSeconds(1);

            if ((tsc.tsType & 1) == 1)
            {
                try
                {
                    if ((tsc.tsTypeExt1 & 4) == 4)
                    {
                        try
                        {
                            tsc.temperature = ard.ReadByte() * 256 + ard.ReadByte();
                        }
                        catch
                        {
                            return;
                        }
                        tsc.temperature /= 64;
                        tsc.temperature -= (float)273.15;
                    }

                    if ((tsc.tsTypeExt1 & 8) == 8)
                    {
                        tsc.co2Error = ard.ReadByte();
                    }

                    if ((tsc.tsTypeExt1 & 16) == 16)
                    {
                        tsc.co2Baseline = (ushort)(ard.ReadByte() * 256 + ard.ReadByte());
                    }

                    if ((tsc.tsTypeExt1 & 32) == 32)
                    {
                        tsc.co2NewValues = true;
                    }

                    if ((tsc.tsTypeExt1 & 64) == 64)
                    {
                        tsc.co2Current = ard.ReadByte();
                        tsc.co2Voltage = ard.ReadByte();
                        tsc.co2Raw = (ushort)(tsc.co2Current * 256 + tsc.co2Voltage);
                        tsc.co2Voltage += ((tsc.co2Current & 3) * 256);
                        tsc.co2Current = (byte)(tsc.co2Current >> 2);
                        tsc.co2Voltage /= 620;
                    }
                    if ((tsc.tsTypeExt1 & 128) == 128)
                    {
                        tsc.humidity = ard.ReadByte() * 256 + ard.ReadByte();
                        tsc.humidity /= 512;
                    }
                }
                catch { }
            }
        }

        private bool pressureDepth5837(ref BinaryReader ard, ref timeStamp tsc)
        {
            double dT;
            double off;
            double sens;
            double d2;

            try
            {
                d2 = (ard.ReadByte() * 65536 + ard.ReadByte() * 256 + ard.ReadByte());
            }
            catch
            {
                return true;
            }

            dT = d2 - convCoeffs[4] * 256;
            tsc.temperatureMS = (float)(2000 + (dT * convCoeffs[5]) / 8388608);
            off = convCoeffs[1] * 65536 + (convCoeffs[3] * dT) / 128;
            sens = convCoeffs[0] * 32768 + (convCoeffs[2] * dT) / 256;
            if (tsc.temperatureMS > 2000)
            {
                tsc.temperatureMS -= (float)((2 * Math.Pow(dT, 2)) / 137438953472);
                off -= ((Math.Pow((tsc.temperatureMS - 2000), 2)) / 16);
            }
            else
            {
                off -= 3 * ((Math.Pow((tsc.temperatureMS - 2000), 2)) / 2);
                sens -= 5 * ((Math.Pow((tsc.temperatureMS - 2000), 2)) / 8);
                if (tsc.temperatureMS < -1500)
                {
                    off -= 7 * Math.Pow((tsc.temperatureMS + 1500), 2);
                    sens -= 4 * Math.Pow((tsc.temperatureMS + 1500), 2);
                }
                tsc.temperatureMS -= (float)(3 * (Math.Pow(dT, 2))) / 8589934592;
            }
            tsc.temperatureMS = (float)Math.Round((tsc.temperatureMS / 100), 1);

            return false;
        }

        private bool detectEof(ref BinaryReader ard)
        {
            if (ard.BaseStream.Position >= ard.BaseStream.Length)
            {
                return true;
            }
            return false;
        }
    }
}
