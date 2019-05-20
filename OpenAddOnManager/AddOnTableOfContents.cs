using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OpenAddOnManager
{
    public class AddOnTableOfContents
    {
        static readonly Regex commentPattern = new Regex("^#(?<text>[^#].*)$", RegexOptions.Compiled);
        static readonly Regex tagPattern = new Regex("^##\\s*(?<name>.*)\\s*:\\s*(?<value>.*)\\s*$", RegexOptions.Compiled);

        public static async Task<AddOnTableOfContents> LoadFromAsync(FileInfo file)
        {
            var tags = new Dictionary<string, string>();
            var loadPaths = new List<string>();
            var comments = new List<string>();
            using (var fileStream = file.OpenRead())
            using (var streamReader = new StreamReader(fileStream))
                while (!streamReader.EndOfStream)
                {
                    var line = await streamReader.ReadLineAsync().ConfigureAwait(false);
                    var tagMatch = tagPattern.Match(line);
                    if (tagMatch.Success)
                        tags.Add(tagMatch.Groups["name"].Value, tagMatch.Groups["value"].Value);
                    else
                    {
                        var commentMatch = commentPattern.Match(line);
                        if (commentMatch.Success)
                            comments.Add(commentMatch.Groups["text"].Value);
                        else if (!string.IsNullOrWhiteSpace(line))
                            loadPaths.Add(line);
                    }
                }
            return new AddOnTableOfContents(tags.ToImmutableDictionary(), loadPaths.ToImmutableArray(), comments.ToImmutableArray());
        }

        AddOnTableOfContents(IReadOnlyDictionary<string, string> tags, IReadOnlyList<string> loadPaths, IReadOnlyList<string> comments)
        {
            Tags = tags;
            LoadPaths = loadPaths;
            Comments = comments;

            LocalizedNotes = CollectLocalizedTags("Notes");
            LocalizedTitles = CollectLocalizedTags("Title");
        }

        IReadOnlyList<string> GetListTag(string tagName) => GetScalarTag(tagName)?.Split(',').Select(listItem => listItem.Trim()).ToImmutableList();

        string GetScalarTag(string tagName) => Tags.TryGetValue(tagName, out var value) ? value : null;

        IReadOnlyDictionary<CultureInfo, string> CollectLocalizedTags(string namePrefix)
        {
            namePrefix = $"{namePrefix}-";
            var localizedTags = new Dictionary<CultureInfo, string>();
            foreach (var localizedTag in Tags.Where(kv => kv.Key.StartsWith(namePrefix)))
            {
                try
                {
                    localizedTags.Add(new CultureInfo($"{localizedTag.Key.Substring(namePrefix.Length, 2)}-{localizedTag.Key.Substring(namePrefix.Length + 2)}"), localizedTag.Value);
                }
                catch (CultureNotFoundException)
                {
                    // Whoops!
                }
            }
            return localizedTags.ToImmutableDictionary();
        }

        public string Author => GetScalarTag("Author");

        public IReadOnlyList<string> Comments { get; }

        public bool EnabledByDefault => GetScalarTag("DefaultState") != "disabled";

        public IReadOnlyList<string> LoadManagers => GetListTag("LoadManagers");

        public bool LoadOnDemand => GetScalarTag("LoadOnDemand") == "1";

        public IReadOnlyList<string> LoadPaths { get; }

        public IReadOnlyList<string> LoadWith => GetListTag("LoadWith");

        public IReadOnlyDictionary<CultureInfo, string> LocalizedNotes { get; }

        public IReadOnlyDictionary<CultureInfo, string> LocalizedTitles { get; }

        public string Notes => GetScalarTag("Notes");

        public IReadOnlyList<string> OptionalDependencies => GetListTag("OptionalDeps");

        public IReadOnlyList<string> RequiredDependencies => GetListTag("RequiredDeps") ?? GetListTag("Dependencies") ?? GetListTag(Tags.Keys.FirstOrDefault(key => key.StartsWith("Dep")) ?? string.Empty);

        public IReadOnlyList<string> SavedVariables => GetListTag("SavedVariables");

        public IReadOnlyList<string> SavedVariablesPerCharacter => GetListTag("SavedVariablesPerCharacter");

        public bool Secure => GetScalarTag("Secure") == "1";

        public IReadOnlyDictionary<string, string> Tags { get; }

        public string Title => GetScalarTag("Title");

        public string Version => GetScalarTag("Version");
    }
}
