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
using System.Threading;
using System.Windows.Threading;

namespace X_Manager
{
    /// <summary>
    /// Interaction logic for Warning.xaml
    /// </summary>
    public partial class Warning : Window
    {
        [DllImport("user32.dll", EntryPoint = "SetCursorPos")]
        static extern unsafe void setCursorPos(int x, int y);

        public string picUri;
        string mexText;
        Point po;
        public Warning(string mText)
        {
            InitializeComponent();
            this.Loaded += warningLoaded;
            mexText = mText;
            okB.Focus();
        }

        public void updateText(string newText)
        {
            labelMex.Content = newText;
        }

        private void warningLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                BitmapImage bmp = new BitmapImage();
                bmp.BeginInit();
                if (picUri == null)
                {
                    picUri = "pack://application:,,,/Resources/ok.png";
                }
                bmp.UriSource = new Uri(picUri, UriKind.Absolute);
                bmp.EndInit();
                PictureBox.Source=bmp;
            }catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            labelMex.Content=mexText;
            Thread mtt = new Thread(new ThreadStart(mouseT));
            mtt.Start();
        }

        private void okB_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void setMouse()
        {
            try
            {
                Point p1 = new Point(this.Left, this.Top);
                Point p2 = okB.TransformToAncestor(this).Transform(new Point(35, 35));
                setCursorPos((int)(p1.X + p2.X), (int)(p1.Y + p2.Y));
            }
            catch
            {
                setCursorPos((int)po.X + 105, (int)po.Y + 130);
            }
        }
        private void mouseT()
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => this.setMouse()));
        }

        private void keyDownManager(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                closee();
            }
        }

        private void onKey(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                closee();
            }
        }

        private void closee()
        {
            //MessageBox.Show("Si");
            this.Close();
        }



    }
}
