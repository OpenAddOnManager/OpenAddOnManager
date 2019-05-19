using System;

namespace OpenAddOnManager
{
    public class AddOnManifestEntry
    {
        public AddOnManifestEntry() => ReleaseChannelId = "_retail_";

        public Uri AddOnPageUrl { get; set; }

        public string AuthorEmail { get; set; }

        public string AuthorName { get; set; }

        public Uri AuthorPageUrl { get; set; }

        public string Description { get; set; }

        public Uri DonationsUrl { get; set; }

        public Uri IconUrl { get; set; }

        public string Name { get; set; }

        public string ReleaseChannelId { get; set; }

        public string SourceBranch { get; set; }

        public Uri SourceUrl { get; set; }

        public Uri SupportUrl { get; set; }
    }
}
