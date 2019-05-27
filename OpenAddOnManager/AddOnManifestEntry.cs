using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace OpenAddOnManager
{
    public class AddOnManifestEntry
    {
        public AddOnManifestEntry() => Flavor = Flavor.wow;

        public Uri AddOnPageUrl { get; set; }

        public string AuthorEmail { get; set; }

        public string AuthorName { get; set; }

        public Uri AuthorPageUrl { get; set; }

        public string Description { get; set; }

        public Uri DonationsUrl { get; set; }

        public Uri IconUrl { get; set; }

        public bool IsPrereleaseVersion { get; set; }

        public string Name { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Flavor Flavor { get; set; }

        public string SourceBranch { get; set; }

        public Uri SourceUrl { get; set; }

        public Uri SupportUrl { get; set; }
    }
}
