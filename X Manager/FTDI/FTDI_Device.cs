using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.IO;
#if X64
using FT_HANDLE = System.UInt64;
#else
using FT_HANDLE = System.UInt32;
#endif



namespace X_Manager
{
	public class FTDI_Device
	{

		#region DichiarazioniFTDI

#if X64
		//public const string ftdiLibraryName = "ftd2xx64.dll";
		public const string ftdiLibraryName = "ftd2xx.dll";
#else
		public const string ftdiLibraryName = "FTD2XX.dll";
#endif


		//[DllImport("kernel32.dll")]
		//static extern bool AttachConsole(int dwProcessId);
		//private const int ATTACH_PARENT_PROCESS = -1;

		//[DllImport("kernel32.dll")]
		//static extern bool FreeConsole();

		public const int FT_LIST_NUMBER_ONLY = -2147483648;
		public const int FT_LIST_BY_INDEX = 0x40000000;
		public const int FT_LIST_ALL = 0x20000000;

		// FT_OpenEx Flags (See FT_OpenEx)
		public const int FT_OPEN_BY_SERIAL_NUMBER = 1;
		public const int FT_OPEN_BY_DESCRIPTION = 2;

		public enum FT_STATUS : int
		{
			FT_OK = 0,
			FT_INVALID_HANDLE,
			FT_DEVICE_NOT_FOUND,
			FT_DEVICE_NOT_OPENED,
			FT_IO_ERROR,
			FT_INSUFFICIENT_RESOURCES,
			FT_INVALID_PARAMETER,
			FT_INVALID_BAUD_RATE,
			FT_DEVICE_NOT_OPENED_FOR_ERASE,
			FT_DEVICE_NOT_OPENED_FOR_WRITE,
			FT_FAILED_TO_WRITE_DEVICE,
			FT_EEPROM_READ_FAILED,
			FT_EEPROM_WRITE_FAILED,
			FT_EEPROM_ERASE_FAILED,
			FT_EEPROM_NOT_PRESENT,
			FT_EEPROM_NOT_PROGRAMMED,
			FT_INVALID_ARGS,
			FT_OTHER_ERROR
		};
		// FT_ListDevices by number only
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_ListDevices(void* pvArg1, void* pvArg2, UInt32 dwFlags);

		//StringCode del dispositivo, passare il numero dispositivo, il puntatore al buffer di byte e (FT_LIST_BY_INDEX Or FT_OPEN_BY_SERIAL_NUMBER)
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_ListDevices(UInt32 pvArg1, void* pvArg2, UInt32 dwFlags);

		//Numero di device presenti, passare riferimento all'int numero di dispositivi, string null e FT_LIST_NUMBER_ONLY
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_ListDevices(ref int lngNumberOfDevices, string pvArg2, int lngFlags);

		//Apre il dispositivo identificato dal seriale
		[DllImport(ftdiLibraryName)]
		public static extern unsafe FT_STATUS FT_OpenEx(string stringCode, UInt32 dwFlags, ref FT_HANDLE ftHandle);


		[DllImport(ftdiLibraryName)]
		public static extern unsafe FT_STATUS FT_OpenEx(void* pvArg1, UInt32 dwFlags, ref FT_HANDLE ftHandle);

		//Restituisce il numero di porta com associato al dispositivo
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_GetComPortNumber(FT_HANDLE ftHandle, ref FT_HANDLE comNumber);


		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_Open(UInt32 uiPort, ref FT_HANDLE ftHandle);

		[DllImport(ftdiLibraryName)]
		public static extern unsafe FT_STATUS FT_Close(FT_HANDLE ftHandle);
		[DllImport(ftdiLibraryName)]
		public static extern unsafe FT_STATUS FT_Read(FT_HANDLE ftHandle, void* lpBuffer, UInt32 dwBytesToRead, ref UInt32 lpdwBytesReturned);
		[DllImport(ftdiLibraryName)]
		public static extern unsafe FT_STATUS FT_Write(FT_HANDLE ftHandle, void* lpBuffer, UInt32 dwBytesToRead, ref UInt32 lpdwBytesWritten);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_SetBaudRate(FT_HANDLE ftHandle, UInt32 dwBaudRate);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_SetDataCharacteristics(FT_HANDLE ftHandle, byte uWordLength, byte uStopBits, byte uParity);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_SetFlowControl(FT_HANDLE ftHandle, char usFlowControl, byte uXon, byte uXoff);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_SetDtr(FT_HANDLE ftHandle);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_ClrDtr(FT_HANDLE ftHandle);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_SetRts(FT_HANDLE ftHandle);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_ClrRts(FT_HANDLE ftHandle);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_GetModemStatus(FT_HANDLE ftHandle, ref UInt32 lpdwModemStatus);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_SetChars(FT_HANDLE ftHandle, byte uEventCh, byte uEventChEn, byte uErrorCh, byte uErrorChEn);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_Purge(FT_HANDLE ftHandle, UInt32 dwMask);
		[DllImport(ftdiLibraryName)]
		public static extern unsafe FT_STATUS FT_SetTimeouts(FT_HANDLE ftHandle, UInt32 dwReadTimeout, UInt32 dwWriteTimeout);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_GetQueueStatus(FT_HANDLE ftHandle, ref UInt32 lpdwAmountInRxQueue);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_SetBreakOn(FT_HANDLE ftHandle);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_SetBreakOff(FT_HANDLE ftHandle);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_GetStatus(FT_HANDLE ftHandle, ref UInt32 lpdwAmountInRxQueue, ref UInt32 lpdwAmountInTxQueue, ref UInt32 lpdwEventStatus);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_SetEventNotification(FT_HANDLE ftHandle, UInt32 dwEventMask, void* pvArg);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_ResetDevice(FT_HANDLE ftHandle);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_SetDivisor(FT_HANDLE ftHandle, char usDivisor);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_GetLatencyTimer(FT_HANDLE ftHandle, ref byte pucTimer);
		[DllImport(ftdiLibraryName)]
		public static extern unsafe FT_STATUS FT_SetLatencyTimer(FT_HANDLE ftHandle, byte ucTimer);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_GetBitMode(FT_HANDLE ftHandle, ref byte pucMode);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_SetBitMode(FT_HANDLE ftHandle, byte ucMask, byte ucEnable);
		[DllImport(ftdiLibraryName)]
		static extern unsafe FT_STATUS FT_SetUSBParameters(FT_HANDLE ftHandle, UInt32 dwInTransferSize, UInt32 dwOutTransferSize);
		#endregion

		#region variabili
		FT_HANDLE ftHandle = 0;
		FT_HANDLE comNumber = 0;
		public string stringCode = "";
		bool _isOpen = false;
		uint _readTimeout = 0;
		uint _baudrate = 115200;
		#endregion

		public bool IsOpen
		{
			get
			{
				return _isOpen;
			}
		}

		public uint ReadTimeout
		{
			get
			{
				return _readTimeout;
			}
			set
			{
				if (_isOpen)
				{
					_readTimeout = value;
					setTimeout(value);
				}
			}
		}

		public uint BaudRate
		{
			get
			{
				return _baudrate;
			}
			set
			{
				while (BytesToWrite() > 0) { };
				Thread.Sleep(5);
				setBaudrate(value);
				_baudrate = value;
			}
		}

		public FTDI_Device(string comPortName)
		{
			comPortName = comPortName.Substring(3);
			UInt64.TryParse(comPortName, out comNumber);
			stringCode = GetSerialCodeByComNumber(comNumber);
		}

		public unsafe string GetSerialCodeByComNumber(FT_HANDLE targetComPort)
		{
			int deviceCount = 0;
			byte[] stringCodeBuffer = new byte[64];
			string returnStringCode = "";

			if (FT_ListDevices(ref deviceCount, null, FT_LIST_NUMBER_ONLY) != FT_STATUS.FT_OK) return returnStringCode;

			for (int i = 0; i <= deviceCount; i++)
			{
				string stringCode;
				//Ottiene il serial number
				fixed (byte* pBuf = stringCodeBuffer)
				{
					if (FT_ListDevices((UInt32)i, pBuf, (FT_LIST_BY_INDEX | FT_OPEN_BY_SERIAL_NUMBER)) != FT_STATUS.FT_OK) continue;
					StringBuilder sb = new StringBuilder();
					for (int j = 0; stringCodeBuffer[j] != 0; j++)
					{
						sb.Append(Convert.ToChar(stringCodeBuffer[j]));
					}
					stringCode = sb.ToString();
				}

				//Apre il dispositivo identificato dallo StringCode
				if (FT_OpenEx(stringCode, (UInt32)1, ref ftHandle) != FT_STATUS.FT_OK) continue;

				//Chiede il numero di porta COM associata al dispositivo
				if (FT_GetComPortNumber(ftHandle, ref comNumber) != FT_STATUS.FT_OK) continue;
				//comPortName = "COM" + comNumber.ToString();
				FT_Close(ftHandle);

				//Se la porta è quella desiderata, imposta il tempo di latenza del buffer a 1ms
				if (comNumber == targetComPort)
				{
					returnStringCode = stringCode;
					break;
				}
			}
			return returnStringCode;
		}

		public bool Open()
		{
			//if (IsOpen) return true;
			//if (FT_OpenEx(stringCode, 1, ref ftHandle) != FT_STATUS.FT_OK) return false;
			//BaudRate = 115200;
			//setLatency();

			//_isOpen = true;
			//return true;
			return Open(115200);
		}

		public bool Open(uint newBaudrate)
		{
			if (IsOpen) return true;
			if (FT_OpenEx(stringCode, 1, ref ftHandle) != FT_STATUS.FT_OK) return false;
			var fff = FT_SetDataCharacteristics(ftHandle, 8, 0, 0);
			BaudRate = newBaudrate;
			setLatency();
			_isOpen = true;
			ReadTimeout = 1000;
			return true;
		}

		public bool OpenSimple()
		{
			if (IsOpen) return true;
			if (FT_OpenEx(stringCode, 1, ref ftHandle) != FT_STATUS.FT_OK) return false;
			setLatency();
			_isOpen = true;
			return true;
		}

		public void Close()
		{
			if (_isOpen)
			{
				_isOpen = false;
				FT_Close(ftHandle);
				//ftHandle = 0;
				//comNumber = 0;
				//stringCode = "";
			}
		}

		public void destroy()
		{
			if (_isOpen)
			{
				Close();
				ftHandle = 0;
				comNumber = 0;
				stringCode = "";
			}
		}

		private unsafe byte setLatency()
		{
			//NB: Il dispositivo deve essere già aperto
			byte pucTimer = 0;
			if (ftHandle == 0) return 0;
			//if (FT_OpenEx(stringCode, (UInt32)1, ref ftHandle) != FT_STATUS.FT_OK) return 0;
			if (FT_SetLatencyTimer(ftHandle, 1) != FT_STATUS.FT_OK) return 0;
			if (FT_GetLatencyTimer(ftHandle, ref pucTimer) != FT_STATUS.FT_OK) return 0;
			//NB: Il dispositivo non viene chiuso, altrimenti la latenza si resetta a quella di default
			return pucTimer;
		}

		private void setTimeout(uint timeout)
		{
			FT_SetTimeouts(ftHandle, timeout, timeout);
		}

		private void setBaudrate(uint newBaudrate)
		{
			FT_STATUS fts = FT_SetBaudRate(ftHandle, newBaudrate);
		}

		public unsafe int Read(byte[] buffIn, uint bytesToRead)
		{
			uint bytesRead = 0;
			int bytesReadOut = 0;
			FT_STATUS ftS;
			fixed (byte* buff = buffIn)
			{
				ftS = FT_Read(ftHandle, buff, bytesToRead, ref bytesRead);
			}
			if (ftS != FT_STATUS.FT_OK)
			{
				bytesReadOut = -1;
			}
			else
			{
				bytesReadOut = (int)bytesRead;
			}
			return bytesReadOut;
		}

		public unsafe int Read(byte[] buffIn, uint startPosition, uint bytesToRead)
		{
			uint bytesRead = 0;
			int bytesReadOut = 0;
			if (bytesToRead < 1)
			{
				return -1;
			}
			FT_STATUS ftS;
			fixed (byte* buff = &buffIn[startPosition])
			{
				ftS = FT_Read(ftHandle, buff, bytesToRead, ref bytesRead);
			}
			if (ftS != FT_STATUS.FT_OK)
			{
				bytesReadOut = -1;
			}
			else
			{
				bytesReadOut = (int)bytesRead;
			}
			return bytesReadOut;
		}

		public unsafe byte ReadByte()
		{
			byte res = 0;
			uint bytesRead = 0;
			FT_STATUS ftS;
			byte* buff = &res;
			ftS = FT_Read(ftHandle, &res, 1, ref bytesRead);
			if (ftS != FT_STATUS.FT_OK || bytesRead == 0)
			{
				throw new Exception("Reading timeout expired.");
			}
			return res;
		}

		public unsafe string ReadLine()
		{
			byte[] buff = new byte[4096];
			int counter = 0;
			while (counter < 4096)
			{
				try
				{
					buff[counter] = ReadByte();
					if ((buff[counter] == 13) || (buff[counter] == 10))
					{
						Thread.Sleep(1);
						ReadExisting();
						break;
					}
					counter++;
				}
				catch
				{
					throw new Exception("Reading timeout expired.");
				}
			}
			return ASCIIEncoding.ASCII.GetString(buff, 0, counter);
		}

		public unsafe int Write(byte[] buffOut, uint bytesToWrite)
		{
			uint bytesWritten = 0;
			int bytesWrittenOut = 0;
			FT_STATUS ftS;
			fixed (byte* buff = buffOut)
			{
				ftS = FT_Write(ftHandle, buff, bytesToWrite, ref bytesWritten);
			}
			if (ftS != FT_STATUS.FT_OK)
			{
				bytesWrittenOut = -1;
			}
			else
			{
				bytesWrittenOut = (int)bytesWritten;
			}
			return bytesWrittenOut;
		}

		public unsafe int Write(byte[] buffOut, uint startPosition, uint bytesToWrite)
		{
			uint bytesWritten = 0;
			int bytesWrittenOut = 0;
			FT_STATUS ftS;
			fixed (byte* buff = &buffOut[startPosition])
			{
				ftS = FT_Write(ftHandle, buff, bytesToWrite, ref bytesWritten);
			}
			if (ftS != FT_STATUS.FT_OK)
			{
				bytesWrittenOut = -1;
			}
			else
			{
				bytesWrittenOut = (int)bytesWritten;
			}
			return bytesWrittenOut;
		}

		public unsafe int Write(string sIn)
		{
			int bytesWrittenOut;
			uint bytesWritten = 0;
			uint bytesToWrite = (uint)sIn.Length;
			byte[] buffOut = Encoding.ASCII.GetBytes(sIn);
			FT_STATUS ftS;
			fixed (byte* buff = buffOut)
			{
				ftS = FT_Write(ftHandle, buff, bytesToWrite, ref bytesWritten);
			}
			if (ftS != FT_STATUS.FT_OK)
			{
				bytesWrittenOut = -1;
			}
			else
			{
				bytesWrittenOut = (int)bytesWritten;
			}
			return bytesWrittenOut;
		}

		public unsafe int WriteLine(string sIn)
		{
			int bytesWrittenOut;
			uint bytesWritten = 0;
			uint bytesToWrite = (uint)sIn.Length + 2;
			byte[] buffOut = Encoding.ASCII.GetBytes(sIn + "\r\n");
			FT_STATUS ftS;
			fixed (byte* buff = buffOut)
			{
				ftS = FT_Write(ftHandle, buff, bytesToWrite, ref bytesWritten);
			}
			if (ftS != FT_STATUS.FT_OK)
			{
				bytesWrittenOut = -1;
			}
			else
			{
				bytesWrittenOut = (int)bytesWritten;
			}
			return bytesWrittenOut;
		}

		public unsafe int ReadExisting()
		{
			uint oldTimeout = _readTimeout;
			setTimeout(10);
			byte[] buff = new byte[8192];
			int res = Read(buff, 8192);
			setTimeout(oldTimeout);
			return res;
		}

		public unsafe string ReadExistingOut()
		{
			uint oldTimeout = _readTimeout;
			setTimeout(10);
			byte[] buff = new byte[8192];
			int res = Read(buff, 8192);
			setTimeout(oldTimeout);
			return Encoding.ASCII.GetString(buff, 0, res);
		}

		//public uint BytesToRead()
		//{
		//	uint bytesToRead = 0;
		//	uint bytesToWrite = 0;
		//	uint eventStatus = 0;
		//	FT_GetStatus(ftHandle, ref bytesToRead, ref bytesToWrite, ref eventStatus);
		//	return bytesToRead;
		//}

		public uint BytesToRead()
		{
			uint res = 0;
			FT_GetQueueStatus(ftHandle, ref res);
			return res;
		}

		public uint BytesToWrite()
		{
			uint rxQ = 0;
			uint txQ = 0;
			uint evS = 0;
			FT_GetStatus(ftHandle, ref rxQ, ref txQ, ref evS);
			return txQ;
		}

	}
}
