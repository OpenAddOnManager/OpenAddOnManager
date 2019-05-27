using System.Windows;
using ToastNotifications.Core;

namespace OpenAddOnManager.Windows
{
    public partial class AddOnDisplayPart : NotificationDisplayPart
    {
        public AddOnDisplayPart(AddOnMessage message, MessageOptions options)
        {
            InitializeComponent();
            Bind(message);
        }

        void CloseClickHandler(object sender, RoutedEventArgs e) => Notification.Close();
    }
}
