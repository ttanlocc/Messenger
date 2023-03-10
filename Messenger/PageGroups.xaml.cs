using Messenger.Modules;
using Mikodev.Network;
using System.Windows;
using System.Windows.Controls;

namespace Messenger
{
    public partial class PageGroups : Page
    {
        public PageGroups()
        {
            InitializeComponent();
            Loaded += _Loaded;
        }

        private void _Loaded(object sender, RoutedEventArgs e)
        {
            PageManager.SetProfilePage(this, uiListbox, ProfileModule.GroupList);
        }

        private void _Click(object sender, RoutedEventArgs e)
        {
            var tag = (e.OriginalSource as Button)?.Tag as string;
            if (tag == null)
                return;
            if (tag == "edit")
            {
                var vis = uiEditGrid.Visibility;
                uiEditGrid.Visibility = (vis == Visibility.Visible) ? Visibility.Collapsed : Visibility.Visible;
            }
            else if (tag == "apply")
            {
                if (string.Equals(uiEditBox.Text, ProfileModule.GroupLabels) == false && ProfileModule.SetGroupLabels(uiEditBox.Text) == false)
                    Entrance.ShowError($"Maximum allowed {Links.GroupLabelLimit} group tags", null);
                else
                    uiEditGrid.Visibility = Visibility.Collapsed;
            }
        }
    }
}
