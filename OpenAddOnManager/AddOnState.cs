using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace OpenAddOnManager
{
    public class AddOnState
    {
        public Uri AddOnPageUrl { get; set; }

        public string AuthorEmail { get; set; }

        public string AuthorName { get; set; }

        public Uri AuthorPageUrl { get; set; }

        public string Description { get; set; }

        public Uri DonationsUrl { get; set; }

        public Uri IconUrl { get; set; }

        public List<string> InstalledFiles { get; set; }

        public string InstalledSha { get; set; }

        public bool IsLicenseAgreed { get; set; }

        public bool IsPrereleaseVersion { get; set; }

        public string License { get; set; }

        public string Name { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Flavor Flavor { get; set; }

        public List<string> SavedVariablesAddOnNames { get; set; }

        public List<string> SavedVariablesPerCharacterAddOnNames { get; set; }

        public string SourceBranch { get; set; }

        public Uri SourceUrl { get; set; }

        public Uri SupportUrl { get; set; }
    }
}
