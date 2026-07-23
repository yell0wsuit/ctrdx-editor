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

        /// <summary>Whether to strip access-key underscores from the resolved string.</summary>
        /// <remarks>
        /// <c>AccessText</c> consumes the <c>_</c> marker; tooltips and plain <c>TextBlock</c>s render it
        /// literally, so touch surfaces reusing a desktop menu key need it removed. Kept here rather than
        /// in a converter because the extension returns a string, not a binding - reusing the same
        /// resource keys is what stops the drawer and the menu drifting apart in translation.
        /// </remarks>
        public bool Plain { get; set; }

        /// <inheritdoc />
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            string value = Localizer.Get(Key);
            return Plain ? value.Replace("_", string.Empty, StringComparison.Ordinal) : value;
        }
    }
}
