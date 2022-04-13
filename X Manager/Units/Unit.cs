using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO.Ports;
#if X64
using FT_HANDLE = System.UInt64;
#else
    using FT_HANDLE = System.UInt32;
#endif

namespace X_Manager.Units
{
    abstract public class Unit : IDisposable
    {
        bool disposed = false;
        public const int model_axyDepth_legacy = 255;
        public const int model_axy3_connecting = 253;
        public const int model_axy4_legacy = 252;
        public const int model_axyDepth = 127;
        public const int model_axy2 = 126;
        public const int model_axy3 = 125;
        public const int model_axy4 = 124;
        public const int model_Co2Logger = 123;
        public const int model_axy5 = 122;
        public const int model_AGM1_calib = 10;
        public const int model_Gipsy6 = 10;
        public const int model_AGM1 = 9;
        public const int model_axyTrekS = 7;
        public const int model_axyTrek = 6;
        
        public bool positionCanSend = false;
        public bool configurePositionButtonEnabled = false;
        public bool configureMovementButtonEnabled = true;
        protected bool realTimeSPVisibility = false;
        protected UInt32 maxMemory;
        protected UInt32 memory;
        protected string name;
        public byte modelCode = 0;
        protected string modelName;
        public UInt32 firmTotA;
        public UInt32 firmTotB;
        protected byte[] firmwareArray;
        public bool connected;
        public bool remote = false;
        public volatile bool convertStop;
        protected int nInputs;
        protected int nOutputs;
        protected double[] lastGroup = new double[1];

        protected MainWindow parent;
        protected SerialPort sp;

        protected string csvSeparator;
        protected string dateSeparator;

        protected const string unitNotReady = "Unit not ready";

#if X64
        [DllImport(@"resampleLib_x64.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport(@"resampleLib.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern int resample3(byte[] inputArray, int nInputSamples, double[] outArray, int nOutputSamples);

#if X64
        [DllImport(@"resampleLib_x64.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport(@"resampleLib.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern int resample4(byte[] inputArray, int nInputSamples, double[] outArray, int nOutputSamples);
        

        public Unit(object p)
        {
            parent = (MainWindow)p;
            sp = parent.sp;
            connected = false;
            csvSeparator = parent.csvSeparator;
        }

        public static string askModel(ref SerialPort sp)
        {
            sp.Write("TTTTTTTGGAf");
            string unitModelString;
            try
            {
                unitModelString = sp.ReadLine();
            }
            catch
            {
                throw new Exception("unit not ready");
            }
            //byte model=0;
            switch (unitModelString)
            {
                case "Axy-4":
                    //model = model_axy4;
                    break;
                case "Axy-Depth":
                    //model = model_axyDepth;
                    break;
                case "Axy-Trek":
                    //mode = mode_axyTrek;
                    break;
                case "Axy-Track":
                    //mode model_axyTrek;
                    break;
                case "Axy-Trek_D":
                    //mode model_axyTrek;
                    break;
                case "Axy-Trek_A":
                    //mode model_axyTrek;
                    break;
                case "AGM-1":
                case "AMG-1":
                    //mode model_AGM1;
                    break;
                case "Co2Logger":
                    //mode model_Co2Logger;
                    unitModelString = "CO2 Logger";
                    break;
                default:
                    //mode 0;
                    break;
            }
            return unitModelString;
        }

        public static string askLegacyModel(ref SerialPort sp)
        {
            string model = sp.ReadLine();
            model = model.Split('.')[1];
            switch (Int16.Parse(model))
            {
                case model_axy4_legacy:
                    model = "Axy-4";
                    break;
                case model_axy3_connecting:
                    model = "Axy-3";
                    break;
                case model_axyDepth_legacy:
                    model = "Axy-Depth";
                    break;
            }
            return model;
        }

        public abstract string askFirmware();

        public abstract string askBattery();

        public abstract string askName();

        protected string formatName(string nameIn)
        {
            int pointer = nameIn.Length;
            do
            {
                pointer--;
            } while ((nameIn[pointer] == 0x20) & pointer > 0);
            return nameIn.Substring(0, pointer + 1);

        }

        public abstract UInt32 askMaxMemory();

        public abstract UInt32 askMemory();

        public virtual int askRealTime()
        {
            return 0;
        }

        public virtual void getCoeffs()
        {

        }

        public abstract byte[] getConf();

        public abstract void setConf(byte[] conf);

        public abstract void abortConf();

        public virtual void setPcTime()
        {

        }

        public virtual byte[] getGpsSchedule()
        {
            return new byte[] { 0 };
        }

        public virtual void setGpsSchedule(byte[] schedule)
        {

        }

		public virtual byte[] getAccSchedule()
		{
			return new byte[] { 0 };
		}

		public virtual void setAccSchedule(byte[] schedule)
		{

		}

		public virtual void download(MainWindow parent, string fileName, UInt32 fromMemory, UInt32 toMemory, int baudrate)
        {

        }

        public virtual void downloadRemote(MainWindow parent, string fileName, UInt32 fromMemory, UInt32 toMemory, int baudrate)
        {

        }

        public abstract void extractArds(string fileNameMdp, string fileName, bool fromDownload);

        public virtual void convert(MainWindow parent, string fileName, string preferenceFile)
        {

        }

        public abstract void eraseMemory();

        public virtual void setName(string newName)
        {

        }

        public virtual void disconnect()
        {
            connected = false;
        }

        void setStateDisconnected()
        {
            connected = false;
        }

        public virtual bool isRemote()
        {
            return false;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    disposed = true;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

    }


}
