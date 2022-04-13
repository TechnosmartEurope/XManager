using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TestMain2CS
{   // ESEMPIO SENZA IL MARSHALING E SENZA UNSAFE (chiamata diretta a DLL unmanaged con gestione memoria da C#, managed)
    class TestMain
    {
        // Importiamo le funzioni resample3() e resample4() dalla DLL "resamplelib.dll" (che qui è nello stesso folder dell'.exe finale)
        [DllImport(@"resampleLib.dll", CallingConvention = CallingConvention.Cdecl)] // Usare il path appropriato
        public static extern int resample3(byte[] inputArray, int nInputSamples, double[] outArray, int nOutputSamples);
        [DllImport(@"resampleLib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int resample4(byte[] inputArray, int nInputSamples, double[] outArray, int nOutputSamples);

        static void Main(string[] args)
        {
            int resultCode = 0;
            int nInputs = 23;   // Numero campioni (triplette) di input: frequenza di campionamento
            int nOutputs = 25;  // Numero campioni (triplette) di output, pari alla frequenza desiderata (da ottenere tramite ricampionamento)
            double[] doubleOutArray = new double[3 * nOutputs];    // Array di double che ospiterà i dati di output nativi C#
            // Dati input di esempio (qui abbiamo simulato nInputs = 23):
            byte[] byteInputArray =
                                    {255, 251, 65,
                                    255, 251, 65,
                                    255, 251, 64,
                                    255, 251, 65,
                                    255, 251, 65,
                                    255, 251, 65,
                                    255, 251, 64,
                                    255, 251, 65,
                                    255, 251, 64,
                                    255, 251, 65,
                                    255, 251, 65,
                                    255, 251, 65,
                                    0, 251, 65,
                                    255, 251, 65,
                                    255, 251, 65,
                                    255, 251, 65,
                                    255, 251, 65,
                                    255, 251, 65,
                                    255, 251, 65,
                                    255, 251, 64,
                                    255, 251, 65,
                                    255, 251, 64,
                                    0, 251, 65};



            // Chiamata a funzione C
            resultCode = resample3(byteInputArray, nInputs, doubleOutArray, nOutputs);
            Console.Write("resample3() return code = ");
            Console.WriteLine(resultCode);  // 0 = OK

            if (resultCode == 0)    // Chiamata OK
            {
                Console.WriteLine("\nInput tuples:");
                for (int iRow = 0; iRow < nInputs; iRow++)
                {
                    Console.Write(byteInputArray[3 * iRow]);
                    Console.Write(" , ");
                    Console.Write(byteInputArray[3 * iRow + 1]);
                    Console.Write(" , ");
                    Console.WriteLine(byteInputArray[3 * iRow + 2]);
                }

                Console.WriteLine("\nInterpolated output tuples:");
                for (int iRow = 0; iRow < nOutputs; iRow++)
                {
                    Console.Write(doubleOutArray[3 * iRow]);
                    Console.Write(" , ");
                    Console.Write(doubleOutArray[3 * iRow + 1]);
                    Console.Write(" , ");
                    Console.WriteLine(doubleOutArray[3 * iRow + 2]);
                }
            }
            else
            {
                Console.WriteLine("Error from resample3() !");
            }
        }
    }
}
