using System;

namespace OpenAddOnManager.Exceptions
{
    public class UserHasNotAgreedToLicenseException : Exception
    {
        public override string Message => "The user has not agreed to the license";
    }
}
