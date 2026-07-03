using iNKORE.UI.WPF.Modern.Controls;
using iNKORE.UI.WPF.Modern.Media.Animation;
using System.Windows;
using Page = iNKORE.UI.WPF.Modern.Controls.Page;

namespace ExamSettings
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void NavigationView_Root_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            var item = sender.SelectedItem;
            Type? pageType = null;

            if (item == NavigationViewItem_Home)
                pageType = typeof(Pages.Home);
            else if (item == NavigationViewItem_ExamEditer)
                pageType = typeof(Pages.ExamEditer);
            else if (item == NavigationViewItem_Settings)
                pageType = typeof(Pages.Settings);
            else if (item == NavigationViewItem_About)
                pageType = typeof(Pages.About);

            if (pageType != null)
            {
                NavigationView_Root.Header = (item as NavigationViewItem)?.Content;

                if (Frame_Main.Content?.GetType() != pageType)
                {
                    Frame_Main.Navigate(pageType, null, new SlideNavigationTransitionInfo());
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            NavigationView_Root.SelectedItem = NavigationViewItem_Home;
        }
    }
}