using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;

using Avalonia.Platform;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Descriptors;

namespace CtrDxEditor.Localization
{
    /// <summary>
    /// Loads UI strings from JSON once at startup, following the OS UI culture with an English
    /// fallback. <c>en.json</c> is the base; a matching culture file overlays it, so any key the
    /// translation omits falls back to English. Language is fixed for the session (no live switch).
    /// </summary>
    public static class Localizer
    {
        private static readonly Dictionary<string, string> Strings = Load();

        private static Dictionary<string, string> Load()
        {
            Dictionary<string, string> result = ReadResource("en");

            CultureInfo culture = CultureInfo.CurrentUICulture;
            // Most specific first (e.g. "pt-BR"), then the bare language ("pt"); English is the base.
            foreach (string name in new[] { culture.Name, culture.TwoLetterISOLanguageName })
            {
                if (string.IsNullOrEmpty(name) || name.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Dictionary<string, string> localized = ReadResource(name);
                if (localized.Count > 0)
                {
                    foreach ((string key, string value) in localized)
                    {
                        result[key] = value;
                    }
                    break;
                }
            }

            return result;
        }

        private static Dictionary<string, string> ReadResource(string lang)
        {
            Uri uri = new($"avares://CtrDxEditor.Shared/Localization/{lang}.json");
            try
            {
                if (!AssetLoader.Exists(uri))
                {
                    return [];
                }

                using Stream stream = AssetLoader.Open(uri);
                return JsonSerializer.Deserialize(stream, AppJsonContext.Default.DictionaryStringString) ?? [];
            }
            catch (InvalidOperationException)
            {
                return [];
            }
        }

        /// <summary>UI string for a key; returns the key itself when missing so gaps are visible.</summary>
        public static string Get(string key)
        {
            return Strings.TryGetValue(key, out string? value) ? value : key;
        }

        /// <summary>Display name for a level element, falling back to its descriptor then its raw id.</summary>
        public static string ObjectName(string elementName)
        {
            return Strings.TryGetValue("Object." + elementName, out string? value)
                ? value
                : DescriptorTable.Default.For(elementName)?.DisplayName ?? elementName;
        }

        /// <summary>Display label for an object attribute; the raw name stays the XML key and never changes.</summary>
        public static string AttributeName(string name)
        {
            return Strings.TryGetValue("Attr." + name, out string? value) ? value : name;
        }
    }
}
