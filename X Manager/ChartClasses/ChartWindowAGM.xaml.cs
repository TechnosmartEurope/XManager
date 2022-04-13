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
using System.Windows.Threading;
using System.IO.Ports;
using X_Manager;
using X_Manager.ChartClasses;
using System.Windows.Forms.DataVisualization;

namespace X_Manager
{

    public partial class ChartWindowAGM : ChartWindow
    {
        byte disableRendering = 0;
        bool accStat = true;
        bool gyroStat = true;
        bool compStat = true;
        bool chartStat = false;
        public bool stopp = false;
        public double offX = 0;
        public double offY = 0;
        public double offZ = 0;
        SerialPort sp;
        double compChartFullScale = 1000;
        byte compChartScaleIndex = 3;
        public bool skipCalFlag = false;
        UInt16 rawCoeff = (ushort)Math.Round(((double)32768 / 5000), 0);
        public ChartWindowAGM(string pName)
        {
            InitializeComponent();
            sp = new SerialPort();
            sp.BaudRate = 115200;
            sp.PortName = pName;
            this.Loaded += loaded;
            this.SizeChanged += resize;
            this.Closing += closing;
        }

        private void loaded(object sender, EventArgs e)
        {
            ((Image)(playGrid.Children[0])).Source = new BitmapImage(new Uri("pack://application:,,,/Resources/PLAY BUTTON.png"));
            ((System.Windows.Controls.Image)(accGrid.Children[0])).Source = new BitmapImage(new Uri("pack://application:,,,/Resources/ACC ON.png"));
            ((System.Windows.Controls.Image)(gyroGrid.Children[0])).Source = new BitmapImage(new Uri("pack://application:,,,/Resources/GYRO ON.png"));
            ((System.Windows.Controls.Image)(compGrid.Children[0])).Source = new BitmapImage(new Uri("pack://application:,,,/Resources/COMP ON.png"));
            chartHost.Width = ((Grid)(chartHost.Parent)).ActualWidth;
            //  - CType(chartHost.Parent, Grid).ActualWidth / 15
            chartHost.Height = ((Grid)(chartHost.Parent)).ActualHeight;
            //  - CType(chartHost.Parent, Grid).ActualHeight / 20
            mChart.Width = mChart.Parent.Width;
            mChart.Height = mChart.Parent.Height;
            mChart.BackColor = System.Drawing.Color.FromArgb(255, 0, 170, 222);

            //Aggiunge le chart areas

            //Accelerometro
            CustomChartArea accChartArea = new CustomChartArea();
            accChartArea.Name = "accChartArea";
            accChartArea.Position.X = 1;
            accChartArea.Position.Y = 1;
            accChartArea.Position.Width = 98;
            accChartArea.Position.Height = 60;

            //Giroscopio
            CustomChartArea gyroChartArea = new CustomChartArea();
            gyroChartArea.Name = "gyroChartArea";
            gyroChartArea.Position.X = 1;
            gyroChartArea.Position.Y = 62;
            gyroChartArea.Position.Width = 65;
            gyroChartArea.Position.Height = 37;
            gyroChartArea.AxisY.Interval = 1000;
            gyroChartArea.AxisY.MinorGrid.Interval = 200;
            gyroChartArea.AxisY.MinorGrid.Enabled = true;
            gyroChartArea.AxisY.IntervalOffset = 100;
            gyroChartArea.AxisY.Maximum = 2100;
            gyroChartArea.AxisY.Minimum = -2100;

            //Magnetometro
            CustomChartArea compChartArea = new CustomChartArea();
            compChartArea.Name = "compChartArea";
            compChartArea.Position.X = 67;
            compChartArea.Position.Y = 62;
            compChartArea.Position.Width = 32;
            compChartArea.Position.Height = 37;

            compChartArea.AxisX.Interval = 1;
            compChartArea.AxisX.MinorGrid.Enabled = false;
            compChartArea.AxisX.IntervalOffset = 0;
            compChartArea.AxisX.Maximum = 3;
            compChartArea.AxisX.Minimum = -2;
            compChartArea.AxisX.LabelStyle.ForeColor = System.Drawing.Color.White;
            compChartArea.AxisX.CustomLabels.Add((double)-2, (double)0, "X");
            compChartArea.AxisX.CustomLabels.Add((double)-1, (double)1, "Y");
            compChartArea.AxisX.CustomLabels.Add((double)0, (double)2, "Z");

            compChartArea.AxisY.Interval = (compChartFullScale / 4);
            compChartArea.AxisY.MinorGrid.Enabled = false;
            compChartArea.AxisY.IntervalOffset = 0;
            compChartArea.AxisY.Maximum = compChartFullScale;
            compChartArea.AxisY.Minimum = (compChartFullScale * -1);
            compChartArea.AxisY.LabelStyle.ForeColor = System.Drawing.Color.White;

            mChart.ChartAreas.Add(accChartArea);
            mChart.ChartAreas.Add(gyroChartArea);
            mChart.ChartAreas.Add(compChartArea);

            //Aggiunge legend e le serie all'area Accelerometro
            var accLegend = new System.Windows.Forms.DataVisualization.Charting.Legend();
            accLegend.Name = "accLegend";
            accLegend.Position.X = 80;
            accLegend.Position.Y = 3;
            mChart.Legends.Add(accLegend);

            var accX = new CustomSeries();
            accX.ChartArea = "accChartArea";
            accX.Color = System.Drawing.Color.White;
            accX.Legend = "accLegend";
            accX.Name = "accX";
            mChart.Series.Add(accX);

            var accY = new CustomSeries();
            accY.ChartArea = "accChartArea";
            accY.Color = System.Drawing.Color.Cyan;
            accY.Legend = "accLegend";
            accY.Name = "accY";
            mChart.Series.Add(accY);

            var accZ = new CustomSeries();
            accZ.ChartArea = "accChartArea";
            accZ.Color = System.Drawing.Color.Blue;
            accZ.Legend = "accLegend";
            accZ.Name = "accZ";
            mChart.Series.Add(accZ);

            //Aggiunge legend e le serie all'area Giroscopio
            var gyroLegend = new System.Windows.Forms.DataVisualization.Charting.Legend();
            gyroLegend.Name = "gyroLegend";
            gyroLegend.Position.X = 60;
            gyroLegend.Position.Y = 53;
            mChart.Legends.Add(gyroLegend);

            var gyroX = new CustomSeries();
            gyroX.ChartArea = "gyroChartArea";
            gyroX.Color = System.Drawing.Color.White;
            gyroX.Legend = "gyroLegend";
            gyroX.Name = "gyroX";
            mChart.Series.Add(gyroX);

            var gyroY = new CustomSeries();
            gyroY.ChartArea = "gyroChartArea";
            gyroY.Color = System.Drawing.Color.Cyan;
            gyroY.Legend = "gyroLegend";
            gyroY.Name = "gyroY";
            mChart.Series.Add(gyroY);

            var gyroZ = new CustomSeries();
            gyroZ.ChartArea = "gyroChartArea";
            gyroZ.Color = System.Drawing.Color.Blue;
            gyroZ.Legend = "gyroLegend";
            gyroZ.Name = "gyroZ";
            mChart.Series.Add(gyroZ);



            var compX = new CustomSeries();
            compX.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Column;
            compX.XValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.Single;
            compX.ChartArea = "compChartArea";
            compX.Color = System.Drawing.Color.White;
            compX.Name = "compX";
            mChart.Series.Add(compX);

            var compY = new CustomSeries();
            compY.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Column;
            compY.XValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.Single;
            compY.ChartArea = "compChartArea";
            compY.Color = System.Drawing.Color.Cyan;
            compY.Name = "compY";
            mChart.Series.Add(compY);

            var compZ = new CustomSeries();
            compZ.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Column;
            compZ.XValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.Single;
            compZ.ChartArea = "compChartArea";
            compZ.Color = System.Drawing.Color.Blue;
            compZ.Name = "compZ";
            mChart.Series.Add(compZ);

            var compMod = new CustomSeries();
            compMod.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Column;
            compMod.XValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.Single;
            compMod.ChartArea = "compChartArea";
            compMod.Color = System.Drawing.Color.Red;
            compMod.Name = "compMod";
            mChart.Series.Add(compMod);



            (mChart.Series)["accX"].Points.AddXY(0, 0);
            (mChart.Series)["gyroX"].Points.AddXY(0, 0);

            (mChart.Series)["compX"].Points.AddXY(-0.75, 1000);
            (mChart.Series)["compY"].Points.AddXY(0, 1000);
            (mChart.Series)["compZ"].Points.AddXY(0.75, 1000);
            (mChart.Series)["compMod"].Points.AddXY(1.5, 1000);

            sp.Open();

        }

        private void resize(object sender, SizeChangedEventArgs e)
        {
            if (disableRendering < 2)
            {
                disableRendering++;
                return;
            }

            chartHost.Width = ((Grid)(chartHost.Parent)).ActualWidth;
            chartHost.Height = ((Grid)(chartHost.Parent)).ActualHeight;
            mChart.Width = mChart.Parent.Width;
            mChart.Height = mChart.Parent.Height;

            (mChart.ChartAreas)["accChartArea"].Position.X = 1;
            (mChart.ChartAreas)["accChartArea"].Position.Y = 1;
            (mChart.ChartAreas)["accChartArea"].Position.Width = 98;
            (mChart.ChartAreas)["accChartArea"].Position.Height = 60;

            (mChart.ChartAreas)["gyroChartArea"].Position.X = 1;
            (mChart.ChartAreas)["gyroChartArea"].Position.Y = 62;
            (mChart.ChartAreas)["gyroChartArea"].Position.Width = 65;
            (mChart.ChartAreas)["gyroChartArea"].Position.Height = 37;

            (mChart.ChartAreas)["compChartArea"].Position.X = 67;
            (mChart.ChartAreas)["compChartArea"].Position.Y = 62;
            (mChart.ChartAreas)["compChartArea"].Position.Width = 32;
            (mChart.ChartAreas)["compChartArea"].Position.Height = 37;

        }

        private void startStop(object sender, RoutedEventArgs e)
        {
            if (!chartStat)
            {
                chartStat = true;
                stopp = false;
                ((System.Windows.Controls.Image)(playGrid.Children[0])).Source = new BitmapImage(new Uri("pack://application:,,,/Resources/STOP BUTTON.png"));
                System.Threading.Thread chartGo = new System.Threading.Thread(() => updateValues());
                try
                {
                    (mChart.Series)["accX"].Points.RemoveAt(0);
                    (mChart.Series)["accY"].Points.RemoveAt(0);
                    (mChart.Series)["accZ"].Points.RemoveAt(0);
                }
                catch { }
                try
                {
                    (mChart.Series)["gyroX"].Points.RemoveAt(0);
                    (mChart.Series)["gyroY"].Points.RemoveAt(0);
                    (mChart.Series)["gyroZ"].Points.RemoveAt(0);
                }
                catch { }
                chartGo.Start();
            }
            else
            {
                chartStat = false;
                ((System.Windows.Controls.Image)(playGrid.Children[0])).Source = new BitmapImage(new Uri("pack://application:,,,/Resources/PLAY BUTTON.png"));
                (mChart.Series)["accX"].Points.Clear();
                (mChart.Series)["accY"].Points.Clear();
                (mChart.Series)["accZ"].Points.Clear();
                (mChart.Series)["gyroX"].Points.Clear();
                (mChart.Series)["gyroY"].Points.Clear();
                (mChart.Series)["gyroZ"].Points.Clear();
                (mChart.Series)["compX"].Points.Clear();
            }
        }

        private void accClick(object sender, RoutedEventArgs e)
        {
            if (accStat)
            {
                accStat = false;
                ((System.Windows.Controls.Image)(playGrid.Children[0])).Source = new BitmapImage(new Uri("pack://application:,,,/Resources/ACC OFF.png"));
            }
            else
            {
                accStat = true;
                ((System.Windows.Controls.Image)(playGrid.Children[0])).Source = new BitmapImage(new Uri("pack://application:,,,/Resources/ACC ON.png"));
            }
        }

        private void gyroClick(object sender, RoutedEventArgs e)
        {
            if (gyroStat)
            {
                gyroStat = false;
                ((System.Windows.Controls.Image)(playGrid.Children[0])).Source = new BitmapImage(new Uri("pack://application:,,,/Resources/GYRO OFF.png"));
            }
            else
            {
                gyroStat = true;
                ((System.Windows.Controls.Image)(playGrid.Children[0])).Source = new BitmapImage(new Uri("pack://application:,,,/Resources/GYRO ON.png"));
            }
        }

        private void compClick(object sender, RoutedEventArgs e)
        {
            if (compStat)
            {
                compStat = false;
                ((System.Windows.Controls.Image)(playGrid.Children[0])).Source = new BitmapImage(new Uri("pack://application:,,,/Resources/COMP OFF.png"));
            }
            else
            {
                compStat = true;
                ((System.Windows.Controls.Image)(playGrid.Children[0])).Source = new BitmapImage(new Uri("pack://application:,,,/Resources/COMP ON.png"));
            }
        }

        private void updateValues()
        {
            //UInt16 byteCount = 0;
            byte[] buff = new byte[12];
            Int16 accTemp;
            Int16 gyroTemp;
            Int16 compTemp;
            float[] accBuff = new float[3];
            float[] gyroBuff = new float[3];
            double[] compBuff = new double[4] { 0, 0, 0, 0 };
            double[] compCoeffs = new double[3];

            //bool cableFailure = false;

            sp.ReadTimeout = 1000;

            sp.Write("TTTTTTTTTTTTTTTTTTTTGGAr");

            //richiesta coefficienti guadagno
            sp.Write("C");
            compCoeffs[0] = ((((double)sp.ReadByte()) - 128) * 0.5 / 128) + 1;
            compCoeffs[1] = ((((double)sp.ReadByte()) - 128) * 0.5 / 128) + 1;
            compCoeffs[2] = ((((double)sp.ReadByte()) - 128) * 0.5 / 128) + 1;

            //richiesta coefficienti offset
            sp.Write("h");

            offX = (double)(((sp.ReadByte() + (sp.ReadByte() << 8)) ^ 0x8000) - 32768);
            compTemp = (short)((((sp.ReadByte() << 8) + sp.ReadByte()) ^ 0x8000) - 32768);
            offX += compTemp;
            offX /= 2;
            offY = (double)(((sp.ReadByte() + (sp.ReadByte() << 8)) ^ 0x8000) - 32768);
            compTemp = (short)((((sp.ReadByte() << 8) + sp.ReadByte()) ^ 0x8000) - 32768);
            offY += compTemp;
            offY /= 2;
            offZ = (double)(((sp.ReadByte() + (sp.ReadByte() << 8)) ^ 0x8000) - 32768);
            compTemp = (short)((((sp.ReadByte() << 8) + sp.ReadByte()) ^ 0x8000) - 32768);
            offZ += compTemp;
            offZ /= 2;

            while (sp.BytesToRead != 0)
            {
                sp.ReadByte();
            }

            while (!stopp)
            {
                if (accStat)
                {
                    sp.Write("a");
                    accTemp = (short)((((sp.ReadByte() << 8) + sp.ReadByte()) ^ 0x8000) - 32768);
                    accBuff[0] = (float)accTemp * 4 / 32768;
                    accTemp = (short)((((sp.ReadByte() << 8) + sp.ReadByte()) ^ 0x8000) - 32768);
                    accBuff[1] = (float)accTemp * 4 / 32768;
                    accTemp = (short)((((sp.ReadByte() << 8) + sp.ReadByte()) ^ 0x8000) - 32768);
                    accBuff[2] = (float)accTemp * 4 / 32768;

                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => seriesUpdate(accBuff, "acc")));
                }

                if (gyroStat)
                {
                    sp.Write("g");
                    gyroTemp = (short)((((sp.ReadByte() << 8) + sp.ReadByte()) ^ 0x8000) - 32768);
                    gyroBuff[0] = (float)gyroTemp * 4 / 32768;
                    gyroTemp = (short)((((sp.ReadByte() << 8) + sp.ReadByte()) ^ 0x8000) - 32768);
                    gyroBuff[1] = (float)gyroTemp * 4 / 32768;
                    gyroTemp = (short)((((sp.ReadByte() << 8) + sp.ReadByte()) ^ 0x8000) - 32768);
                    gyroBuff[2] = (float)gyroTemp * 4 / 32768;

                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => seriesUpdate(gyroBuff, "gyro")));
                }

                if (compStat)
                {
                    sp.Write("c");
                    compBuff[0] = (double)(((sp.ReadByte() + (sp.ReadByte() << 8)) ^ 0x8000) - 32768);
                    compBuff[1] = (double)(((sp.ReadByte() + (sp.ReadByte() << 8)) ^ 0x8000) - 32768);
                    compBuff[2] = (double)(((sp.ReadByte() + (sp.ReadByte() << 8)) ^ 0x8000) - 32768);
                    if (!skipCalFlag)
                    {
                        compBuff[0] -= offX;
                        compBuff[1] -= offY;
                        compBuff[2] -= offZ;
                    }
                    compBuff[0] *= compCoeffs[0];
                    compBuff[1] *= compCoeffs[1];
                    compBuff[2] *= compCoeffs[2];

                    compBuff[3] = System.Math.Sqrt(Math.Pow(compBuff[0], 2) + Math.Pow(compBuff[1], 2) + Math.Pow(compBuff[2], 2));

                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => compSeriesUpdate(compBuff)));
                }
            }
            sp.Write("x");
            sp.ReadExisting();
        }

        private void seriesUpdate(float[] buff, string series)
        {
            if ((mChart.Series)[series + "X"].Points.Count > 30)
            {
                // Toglie il primo punto dal grafico
                (mChart.Series)[series + "X"].Points.RemoveAt(0);
                (mChart.Series)[series + "Y"].Points.RemoveAt(0);
                (mChart.Series)[series + "Z"].Points.RemoveAt(0);
            }

            (mChart.Series)[series + "X"].Points.AddY(buff[0]);
            (mChart.Series)[series + "Y"].Points.AddY(buff[1]);
            (mChart.Series)[series + "Z"].Points.AddY(buff[2]);
        }

        private void compSeriesUpdate(double[] compBuff)
        {
            try
            {
                (mChart.Series)["compX"].Points.RemoveAt(0);
                (mChart.Series)["compY"].Points.RemoveAt(0);
                (mChart.Series)["compZ"].Points.RemoveAt(0);
                (mChart.Series)["compMod"].Points.RemoveAt(0);

                (mChart.Series)["compX"].Points.AddXY(-0.75, compBuff[0]);
                (mChart.Series)["compY"].Points.AddXY(0, compBuff[1]);
                (mChart.Series)["compZ"].Points.AddXY(0.75, compBuff[2]);
                (mChart.Series)["compMod"].Points.AddXY(1.5, compBuff[3]);
            }
            catch { }
        }

        private void ingrandisci(object sender, RoutedEventArgs e)
        {
            if ((compChartScaleIndex == 0)) return;

            compChartScaleIndex--;
            switch (compChartScaleIndex)
            {
                case 0:
                    compChartFullScale = (50 * rawCoeff);
                    break;
                case 1:
                    compChartFullScale = (100 * rawCoeff);
                    break;
                case 2:
                    compChartFullScale = (200 * rawCoeff);
                    break;
                case 3:
                    compChartFullScale = (500 * rawCoeff);
                    break;
                case 4:
                    compChartFullScale = (1000 * rawCoeff);
                    break;
                case 5:
                    compChartFullScale = (2000 * rawCoeff);
                    break;
                case 6:
                    compChartFullScale = (5000 * rawCoeff);
                    break;
            }

            mChart.ChartAreas[2].AxisY.Interval = compChartFullScale / 4;
            mChart.ChartAreas[2].AxisY.Maximum = compChartFullScale;
            mChart.ChartAreas[2].AxisY.Minimum = -compChartFullScale;
        }

        private void riduci(object sender, RoutedEventArgs e)
        {
            if ((compChartScaleIndex == 6)) return;

            compChartScaleIndex++;
            switch (compChartScaleIndex)
            {
                case 0:
                    compChartFullScale = (50 * rawCoeff);
                    break;
                case 1:
                    compChartFullScale = (100 * rawCoeff);
                    break;
                case 2:
                    compChartFullScale = (200 * rawCoeff);
                    break;
                case 3:
                    compChartFullScale = (500 * rawCoeff);
                    break;
                case 4:
                    compChartFullScale = (1000 * rawCoeff);
                    break;
                case 5:
                    compChartFullScale = (2000 * rawCoeff);
                    break;
                case 6:
                    compChartFullScale = (5000 * rawCoeff);
                    break;
            }

            mChart.ChartAreas[2].AxisY.Interval = compChartFullScale / 4;
            mChart.ChartAreas[2].AxisY.Maximum = compChartFullScale;
            mChart.ChartAreas[2].AxisY.Minimum = -compChartFullScale;
        }

        private void skipCal(object sender, RoutedEventArgs e)
        {
            if (!skipCalFlag)
            {
                skipCalFlag = true;
                skiptext.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xff, 0xba, 0xba, 0xba));
            }
            else
            {
                skipCalFlag = false;
                skiptext.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xff, 0x5a, 0x5a, 0x5a));
            }
        }

        private void closing(object sender, EventArgs e)
        {
            sp.Close();
        }

    }
}
