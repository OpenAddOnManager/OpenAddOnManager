using System;

namespace OpenAddOnManager
{
    public class AddOnEventArgs : EventArgs
    {
        public AddOnEventArgs(AddOn addOn) => AddOn = addOn;

        public AddOn AddOn { get; }
    }
}
