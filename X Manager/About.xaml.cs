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
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Drawing;

namespace X_Manager
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : Window
    {

        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);
        public About()
        {
            InitializeComponent();
            this.Loaded += loaded;
        }

        private void loaded(object sender, EventArgs e)
        {

			string fd = RuntimeInformation.FrameworkDescription;

			banner.Source = ImageSourceForBitmap(Properties.Resources.technosmartLogoBlurred);

            var ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            versionLabel.Content = "Version: " + ver.ToString();
			netVersionLabel.Content = "Installed " + fd;

#if X86
            versionLabel.Content += " (32 bit)";
#else
            versionLabel.Content += " (64 bit)";
#endif

            infoLabel.Content += "TechnoSmArt Europe S.r.l.\r\nvia di Novella, 1\r\n00199 Roma (RM)\r\nItaly";
        }

        private ImageSource ImageSourceForBitmap(Bitmap bmp)
        {
            var handle = bmp.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally { DeleteObject(handle); }
        }
    }
}


//public class bmpExt : System.Drawing.Bitmap
//{
//    [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
//    [return: MarshalAs(UnmanagedType.Bool)]
//    public static extern bool DeleteObject([In] IntPtr hObject);
//}