using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace X_Manager
{
    public abstract class ConfigurationWindow : Window
    {
        //public object Owner;
        public bool? res = null;
        public bool mustWrite;
        public byte[] axyConfOut;
		public byte[] axyScheduleOut;

        protected string iniFile = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + MainWindow.companyFolder + MainWindow.appFolder + "\\settings.ini";
        protected string trekSchedFile = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + MainWindow.companyFolder + MainWindow.appFolder + "\\trekLastSchedule.ini";
        protected string gipsySchedFile = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + MainWindow.companyFolder + MainWindow.appFolder + "\\gipsyLastSchedule.ini";
        public ConfigurationWindow()
        {

        }

        public virtual new bool? ShowDialog()
        {
            //return base.ShowDialog();
            base.ShowDialog();
            return res;
        }

    }
}
