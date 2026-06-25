using System.Globalization;

namespace LockIn.Resources.Strings;

/// <summary>
/// XAML markup extension: <c>{loc:Localize Onboarding_Step0_Welcome}</c>
/// Saknad nyckel returnerar nyckelnamnet bokstavligt så missar syns utan att krascha.
/// </summary>
[ContentProperty(nameof(Key))]
[AcceptEmptyServiceProvider]
public class LocalizeExtension : IMarkupExtension<string>
{
    public string Key { get; set; } = "";

    public string ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrEmpty(Key)) return string.Empty;
        var culture = AppResources.Culture ?? CultureInfo.CurrentUICulture;
        return AppResources.ResourceManager.GetString(Key, culture) ?? Key;
    }

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
        => ProvideValue(serviceProvider);
}
