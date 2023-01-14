using Messenger.Modules;
using System.Windows;
using System.Windows.Controls;

namespace Messenger
{
    public partial class PageClient : Page
    {
        public PageClient()
        {
            InitializeComponent();
            Loaded += _Loaded;
        }

        private void _Loaded(object sender, RoutedEventArgs e)
        {
            PageManager.SetProfilePage(this, uiListbox, ProfileModule.ClientList);
        }
    }
}
