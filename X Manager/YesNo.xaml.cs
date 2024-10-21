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

namespace X_Manager
{
    public partial class YesNo : Window
    {

        int extraRes=0;
		public const int CANCELLED = 0;
		public const int YES = 1;
        public const int NO = 2;
        
        private bool choice = false;
        public YesNo(string question)
        {
            InitializeComponent();
            if (string.IsNullOrEmpty(question)) question = "";
            Loaded += loaded;
            labelMex.Content = question;
            Title = "";
            yesB.Content = "Yes";
            noB.Content = "No";
            choiceCB.Content = "";
        }

        public YesNo(string question, string title)
        {
            InitializeComponent();
            if (string.IsNullOrEmpty(question)) question = "";
            if (string.IsNullOrEmpty(title)) title = "";
            Loaded += loaded;
            labelMex.Content = question;
            Title = title;
            yesB.Content = "Yes";
            noB.Content = "No";
            choiceCB.Content = "";
        }

        public YesNo(string question, string title, string checkBoxContent)
        {
            InitializeComponent();
            if (string.IsNullOrEmpty(question)) question = "";
            if (string.IsNullOrEmpty(title)) title = "";
            if (string.IsNullOrEmpty(checkBoxContent)) checkBoxContent = "";
            Loaded += loaded;
            labelMex.Content = question;
            Title = title;
            yesB.Content = "Yes";
            noB.Content = "No";
            choiceCB.Content = checkBoxContent;           
        }

        public YesNo(string question, string title, string checkBoxContent, string yesButtonContent)
        {
            InitializeComponent();
            if (string.IsNullOrEmpty(question)) question = "";
            if (string.IsNullOrEmpty(title)) title = "";
            if (string.IsNullOrEmpty(checkBoxContent)) checkBoxContent = "";
            if (string.IsNullOrEmpty(yesButtonContent)) yesButtonContent = "";
            Loaded += loaded;
            labelMex.Content = question;
            Title = title;
            yesB.Content = yesButtonContent;
            noB.Content = "No";
            choiceCB.Content = checkBoxContent;
        }

        public YesNo(string question, string title, string checkBoxContent, string yesButtonContent, string noButtonContent)
        {
            InitializeComponent();
            if (string.IsNullOrEmpty(question)) question = "";
            if (string.IsNullOrEmpty(title)) title = "";
            if (string.IsNullOrEmpty(checkBoxContent)) checkBoxContent = "";
            if (string.IsNullOrEmpty(yesButtonContent)) yesButtonContent = "";
            if (string.IsNullOrEmpty(noButtonContent)) noButtonContent = "";
            Loaded += loaded;
            labelMex.Content = question;
            Title = title;
            yesB.Content = yesButtonContent;
            noB.Content = noButtonContent;
            choiceCB.Content = checkBoxContent;
        }

        private void loaded(object sender, RoutedEventArgs e)
        {
			if ((string)choiceCB.Content == "")
			{
				mainGrid.RowDefinitions[2].Height = new GridLength(1, GridUnitType.Pixel);
			}
			yesB.Focus();
        }

        public new int ShowDialog()
        {
            base.ShowDialog();
            return extraRes;
        }

        private void yesClick(object sender, RoutedEventArgs e)
        {
            extraRes = 1;
            if (choice) extraRes += 10;
            Close();
        }

        private void noClick(object sender, RoutedEventArgs e)
        {
            extraRes = 2;
            if (choice) extraRes += 10;
            Close();
        }

        private void choiceChecked(object sender, RoutedEventArgs e)
        {
            choice = true;
        }

        private void choiceUnchecked(object sender, RoutedEventArgs e)
        {
            choice = false;
        }

    }
}
