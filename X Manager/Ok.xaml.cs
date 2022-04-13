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
using System.Threading;
using System.Windows.Threading;

namespace X_Manager
{

    public partial class Ok : Window
    {
        [DllImport("user32.dll", EntryPoint = "SetCursorPos")]
        static extern unsafe void SetCursorPos(int x, int y);

        public Point okPoint;
        public string okPicUri;
        public Ok(string mText)
        {
            InitializeComponent();
            labelMex.Content = mText;
            okB.Focus();
            this.Loaded += loaded;
        }

        private void loaded(object sender, EventArgs e)
        {
            var mtt = new Thread(() => mouseT());
            mtt.Start();
        }

        private void okClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void setMouse()
        {
            try
            {
                Point p1 = new Point(this.Left, this.Top);
                Point p2 = okB.TransformToAncestor(this).Transform(new Point(35, 35));
                SetCursorPos((int)(p1.X + p2.X), (int)(p1.Y + p2.Y));
            }
            catch
            {
                SetCursorPos((int)(okPoint.X + 105), (int)(okPoint.Y + 130));
            }
        }

        private void mouseT()
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => setMouse()));
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
            this.closee();
        }
    }
}
