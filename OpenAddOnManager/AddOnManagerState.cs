using System;
using System.Collections.Generic;

namespace OpenAddOnManager
{
    public class AddOnManagerState
    {
        public DateTimeOffset LastManifestsCheck { get; set; }

        public TimeSpan ManifestsCheckFrequency { get; set; }

        public List<Uri> ManifestUrls { get; set; }
    }
}
