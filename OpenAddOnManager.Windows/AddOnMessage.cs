using System.Windows;
using ToastNotifications.Core;
using ToastNotifications.Messages.Core;

namespace OpenAddOnManager.Windows
{
    public class AddOnMessage : MessageBase<AddOnDisplayPart>
    {
        public AddOnMessage(string message) : this(message, new MessageOptions())
        {
        }

        public AddOnMessage(string message, MessageOptions options) : base(message, options)
        {
        }

        public AddOn AddOn { get; set; }

        public AddOnMessageType MessageType { get; set; } = AddOnMessageType.UpdateAvailable;

        protected override AddOnDisplayPart CreateDisplayPart() => new AddOnDisplayPart(this, Options);

        protected override void UpdateDisplayOptions(AddOnDisplayPart displayPart, MessageOptions options)
        {
            //if (options.FontSize != null)
            //    displayPart.Text.FontSize = options.FontSize.Value;

            displayPart.closeButton.Visibility = options.ShowCloseButton ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
