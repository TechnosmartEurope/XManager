using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace X_Manager.Themes
{

	public partial class CustomResources : ResourceDictionary
	{
		public void ComboBoxTextPreviewMouseDown(object sender, EventArgs e)
		{
			var cp = sender as ContentPresenter;
			Grid c1 = null;
			if (sender is Grid)
			{
				c1 = (Grid)sender;
			}
			else if (sender is ContentPresenter)
			{
				c1 = (Grid)(sender as ContentPresenter).Parent;
			}
			ToggleButton tb = (ToggleButton)(c1.Children[2]);
			tb.IsChecked = !tb.IsChecked;
		}

		public void SecondPreview(object sender, RoutedEventArgs e)
		{
			Grid c1 = (Grid)sender;
			ContentPresenter cp = (ContentPresenter)c1.Children[1];
			if ((String)cp.Content == "")
			{
				ToggleButton tb = (ToggleButton)c1.Children[2];
				if (tb.IsChecked == false)
				{
					tb.IsChecked = true;
					tb.IsChecked = true; tb.IsChecked = true; tb.IsChecked = true;
					e.Handled = true;
				}				
			}
		}

		internal class XManager_Theme
		{
		}
	}
}
