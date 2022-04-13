using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;
using System.Drawing;

namespace X_Manager.ChartClasses
{
    class CustomChartArea : ChartArea
    {
        public CustomChartArea() : base()
        {
            this.BackColor = System.Drawing.Color.Black;
            this.AxisX.LineColor = Color.WhiteSmoke;

            this.AxisY.MajorGrid.LineColor = Color.FromArgb(0xff, 0x40, 0x40, 0x60);
            this.AxisY.MinorGrid.LineColor = Color.FromArgb(0xff, 0x20, 0x20, 0x30);
            this.AxisY.LineColor = Color.WhiteSmoke;
            this.AxisY.Interval = 1;
            this.AxisY.MinorGrid.Interval = 0.2;
            this.AxisY.MinorGrid.Enabled = true;
            this.AxisY.IntervalOffset = 0.2;
            this.AxisY.Maximum = 4.2;
            this.AxisY.Minimum = -4.2;
            this.AxisY.LabelStyle.ForeColor = Color.White;
            this.AxisY.Enabled = AxisEnabled.True;

            this.AxisX.MajorGrid.LineColor = Color.FromArgb(0xff, 0x40, 0x40, 0x60);
            this.AxisX.MinorGrid.LineColor = Color.FromArgb(0xff, 0x20, 0x20, 0x30);
            this.AxisX.LineColor = Color.WhiteSmoke;
            this.AxisX.Interval = 10;
            this.AxisX.IntervalOffset = 0;
            this.AxisX.MinorGrid.Enabled = false;
            this.AxisX.Maximum = 31;
            this.AxisX.Minimum = 1;
            this.AxisX.LabelStyle.ForeColor = Color.FromArgb(255, 0, 0xaa, 0xde);
            this.AxisX.LabelStyle.Enabled = true;
            this.AxisX.Enabled = AxisEnabled.True;
            this.AxisX.IsMarginVisible = true;
            this.AxisX.IsStartedFromZero = true;
        }

    }
}
