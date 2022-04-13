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
        public const int yes = 1;
        public const int no = 2;
        private bool choice = false;
        public YesNo(string question)
        {
            InitializeComponent();
            if (string.IsNullOrEmpty(question)) question = "";
            this.Loaded += loaded;
            labelMex.Content = question;
            this.Title = "";
            this.yesB.Content = "Yes";
            this.noB.Content = "No";
            this.choiceCB.Content = "";
            mainGrid.RowDefinitions[2].Height = new GridLength(1, GridUnitType.Pixel);
            
        }

        public YesNo(string question, string title)
        {
            InitializeComponent();
            if (string.IsNullOrEmpty(question)) question = "";
            if (string.IsNullOrEmpty(title)) title = "";
            this.Loaded += loaded;
            labelMex.Content = question;
            this.Title = title;
            this.yesB.Content = "Yes";
            this.noB.Content = "No";
            this.choiceCB.Content = "";
            mainGrid.RowDefinitions[2].Height = new GridLength(1, GridUnitType.Pixel);
        }

        public YesNo(string question, string title, string checkBoxContent)
        {
            InitializeComponent();
            if (string.IsNullOrEmpty(question)) question = "";
            if (string.IsNullOrEmpty(title)) title = "";
            if (string.IsNullOrEmpty(checkBoxContent)) checkBoxContent = "";
            this.Loaded += loaded;
            labelMex.Content = question;
            this.Title = title;
            this.yesB.Content = "Yes";
            this.noB.Content = "No";
            this.choiceCB.Content = checkBoxContent;
            if (checkBoxContent == "")
            {
                mainGrid.RowDefinitions[2].Height = new GridLength(1, GridUnitType.Pixel);
            }
            
        }

        public YesNo(string question, string title, string checkBoxContent, string yesButtonContent)
        {
            InitializeComponent();
            if (string.IsNullOrEmpty(question)) question = "";
            if (string.IsNullOrEmpty(title)) title = "";
            if (string.IsNullOrEmpty(checkBoxContent)) checkBoxContent = "";
            if (string.IsNullOrEmpty(yesButtonContent)) yesButtonContent = "";
            this.Loaded += loaded;
            labelMex.Content = question;
            this.Title = title;
            this.yesB.Content = yesButtonContent;
            this.noB.Content = "No";
            this.choiceCB.Content = checkBoxContent;
            if (checkBoxContent == "")
            {
                mainGrid.RowDefinitions[2].Height = new GridLength(1, GridUnitType.Pixel);
            }

        }

        public YesNo(string question, string title, string checkBoxContent, string yesButtonContent, string noButtonContent)
        {
            InitializeComponent();
            if (string.IsNullOrEmpty(question)) question = "";
            if (string.IsNullOrEmpty(title)) title = "";
            if (string.IsNullOrEmpty(checkBoxContent)) checkBoxContent = "";
            if (string.IsNullOrEmpty(yesButtonContent)) yesButtonContent = "";
            if (string.IsNullOrEmpty(noButtonContent)) noButtonContent = "";
            this.Loaded += loaded;
            labelMex.Content = question;
            this.Title = title;
            this.yesB.Content = yesButtonContent;
            this.noB.Content = noButtonContent;
            this.choiceCB.Content = checkBoxContent;
            if (checkBoxContent == "")
            {
                mainGrid.RowDefinitions[2].Height = new GridLength(1, GridUnitType.Pixel);
            }

        }

        private void loaded(object sender, RoutedEventArgs e)
        {
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
            this.Close();
        }

        private void noClick(object sender, RoutedEventArgs e)
        {
            extraRes = 2;
            if (choice) extraRes += 10;
            this.Close();
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
