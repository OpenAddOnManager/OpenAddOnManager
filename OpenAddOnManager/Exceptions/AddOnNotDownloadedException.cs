using System;

namespace OpenAddOnManager.Exceptions
{
    public class AddOnNotDownloadedException : Exception
    {
        public override string Message => "The add-on has not been downloaded";
    }
}
