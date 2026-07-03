using System;

using Avalonia.Markup.Xaml;

namespace CtrDxEditor.Localization
{
    /// <summary>
    /// XAML markup extension: <c>{loc:Tr Some.Key}</c> resolves to the localized string at load time.
    /// </summary>
    public sealed class TrExtension : MarkupExtension
    {
        /// <summary>Creates an empty translation extension; <see cref="Key"/> must be set separately.</summary>
        public TrExtension()
        {
        }

        /// <summary>Creates a translation extension for <paramref name="key"/>.</summary>
        public TrExtension(string key)
        {
            Key = key;
        }

        /// <summary>The localization key to resolve.</summary>
        public string Key { get; set; } = "";

        /// <inheritdoc />
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Localizer.Get(Key);
        }
    }
}
