using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;
using System.Drawing;

namespace X_Manager.ChartClasses
{
    class CustomSeries : Series
    {
        public CustomSeries()
            : base()
        {
            this.BackSecondaryColor = System.Drawing.Color.Transparent;
            this.ChartType = SeriesChartType.Spline;
            this.XValueType = ChartValueType.Int32;
            this.YValueType = ChartValueType.Single;
            this.BorderWidth = 2;
        }

    }
}
