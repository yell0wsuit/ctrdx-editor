using System;

using Avalonia.Markup.Xaml;

namespace CtrDxEditor.Localization
{
    /// <summary>
    /// XAML markup extension: <c>{loc:Tr Some.Key}</c> resolves to the localized string at load time.
    /// </summary>
    public sealed class TrExtension : MarkupExtension
    {
        public TrExtension()
        {
        }

        public TrExtension(string key)
        {
            Key = key;
        }

        public string Key { get; set; } = "";

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Localizer.Get(Key);
        }
    }
}
