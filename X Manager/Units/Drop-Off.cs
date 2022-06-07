using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace X_Manager.Units
{
	class Drop_Off : Unit
	{
		public Drop_Off(object p) : base(p)
		{
			base.positionCanSend = false;
			configurePositionButtonEnabled = false;
			modelCode = model_drop_off;
			modelName = "Drop-Off";
		}
		public override void abortConf()
		{

		}

		public override string askBattery()
		{
			string battery = "";
			double battLevel;
			ft.Write("TTTTGGAB");
			try
			{
				battLevel = ft.ReadByte(); battLevel *= 256;
				battLevel += ft.ReadByte();
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

		public override string askFirmware()
		{
			byte[] f = new byte[3];
			string firmware = "";

			ft.ReadExisting();
			ft.Write("TTTTTTTGGAF");
			ft.ReadTimeout = 400;
			int i = 0;
			try
			{
				for (i = 0; i < 3; i++) f[i] = (byte)ft.ReadByte();
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

		public override uint[] askMaxMemory()
		{
			return new uint[] { };
		}

		public override uint[] askMemory()
		{
			return new uint[] { };
		}

		public override string askName()
		{
			string unitNameBack;
			try
			{
				ft.Write("TTTTTTTGGAN");
				unitNameBack = ft.ReadLine();
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
			name = formatName(unitNameBack);
			return name;
		}

		public override void eraseMemory()
		{

		}

		public override void extractArds(string fileNameMdp, string fileName, bool fromDownload)
		{

		}

		public override byte[] getConf()
		{
			byte[] conf = new byte[2];

			ft.ReadExisting();
			ft.Write("TTTTTTTTTTTTGGAC");
			try
			{
				conf[0] = (byte)ft.ReadByte();
				conf[1] = (byte)ft.ReadByte();
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
			return conf;
		}

		public override void setConf(byte[] conf)
		{
			ft.ReadExisting();
			ft.Write("TTTTTTTTTTTTGGAc");
			try
			{
				ft.ReadByte();
				ft.Write(conf, 0, 2);
				ft.ReadByte();
			}
			catch
			{
				throw new Exception(unitNotReady);
			}
		}

		public override void disconnect()
		{
			base.disconnect();
			ft.Write("TTTTTTTTTGGAO");

		}
	}
}
