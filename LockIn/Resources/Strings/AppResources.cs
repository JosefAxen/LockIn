using System.Globalization;
using System.Resources;

namespace LockIn.Resources.Strings;

/// <summary>
/// Hand-written wrapper kring AppResources.resx satellitassemblier.
/// Lägg till nya properties här när nycklar läggs till i .resx-filerna.
/// </summary>
public static class AppResources
{
    public static ResourceManager ResourceManager { get; } = new(
        "LockIn.Resources.Strings.AppResources",
        typeof(AppResources).Assembly);

    /// <summary>
    /// Override för tester / framtida språkval. När null används CurrentUICulture.
    /// </summary>
    public static CultureInfo? Culture { get; set; }

    private static string Get(string key) =>
        ResourceManager.GetString(key, Culture) ?? key;

    // ── Common ─────────────────────────────────────────────────────────
    public static string Common_Cancel => Get(nameof(Common_Cancel));
    public static string Common_Skip   => Get(nameof(Common_Skip));

    // ── Onboarding ─────────────────────────────────────────────────────
    public static string Onboarding_SkipLink           => Get(nameof(Onboarding_SkipLink));
    public static string Onboarding_SkipDialogTitle    => Get(nameof(Onboarding_SkipDialogTitle));
    public static string Onboarding_SkipDialogBody     => Get(nameof(Onboarding_SkipDialogBody));

    public static string Onboarding_Step0_Welcome      => Get(nameof(Onboarding_Step0_Welcome));
    public static string Onboarding_Step0_Question     => Get(nameof(Onboarding_Step0_Question));
    public static string Onboarding_Step0_Privacy      => Get(nameof(Onboarding_Step0_Privacy));
    public static string Onboarding_Step0_Placeholder  => Get(nameof(Onboarding_Step0_Placeholder));

    public static string Onboarding_Step1_Title        => Get(nameof(Onboarding_Step1_Title));
    public static string Onboarding_Step1_Question     => Get(nameof(Onboarding_Step1_Question));
    public static string Onboarding_Step1_Wk2_Title    => Get(nameof(Onboarding_Step1_Wk2_Title));
    public static string Onboarding_Step1_Wk2_Sub      => Get(nameof(Onboarding_Step1_Wk2_Sub));
    public static string Onboarding_Step1_Wk3_Title    => Get(nameof(Onboarding_Step1_Wk3_Title));
    public static string Onboarding_Step1_Wk3_Sub      => Get(nameof(Onboarding_Step1_Wk3_Sub));
    public static string Onboarding_Step1_Wk4_Title    => Get(nameof(Onboarding_Step1_Wk4_Title));
    public static string Onboarding_Step1_Wk4_Sub      => Get(nameof(Onboarding_Step1_Wk4_Sub));
    public static string Onboarding_Step1_Wk5_Title    => Get(nameof(Onboarding_Step1_Wk5_Title));
    public static string Onboarding_Step1_Wk5_Sub      => Get(nameof(Onboarding_Step1_Wk5_Sub));
    public static string Onboarding_Step1_Wk6_Title    => Get(nameof(Onboarding_Step1_Wk6_Title));
    public static string Onboarding_Step1_Wk6_Sub      => Get(nameof(Onboarding_Step1_Wk6_Sub));

    public static string Onboarding_Step2_Title        => Get(nameof(Onboarding_Step2_Title));
    public static string Onboarding_Step2_Question     => Get(nameof(Onboarding_Step2_Question));
    public static string Onboarding_Step2_Exp0_Title   => Get(nameof(Onboarding_Step2_Exp0_Title));
    public static string Onboarding_Step2_Exp0_Sub     => Get(nameof(Onboarding_Step2_Exp0_Sub));
    public static string Onboarding_Step2_Exp1_Title   => Get(nameof(Onboarding_Step2_Exp1_Title));
    public static string Onboarding_Step2_Exp1_Sub     => Get(nameof(Onboarding_Step2_Exp1_Sub));
    public static string Onboarding_Step2_Exp2_Title   => Get(nameof(Onboarding_Step2_Exp2_Title));
    public static string Onboarding_Step2_Exp2_Sub     => Get(nameof(Onboarding_Step2_Exp2_Sub));

    public static string Onboarding_Step3_Title        => Get(nameof(Onboarding_Step3_Title));
    public static string Onboarding_Step3_Question     => Get(nameof(Onboarding_Step3_Question));
    public static string Onboarding_Step3_Strength_Title => Get(nameof(Onboarding_Step3_Strength_Title));
    public static string Onboarding_Step3_Strength_Sub   => Get(nameof(Onboarding_Step3_Strength_Sub));
    public static string Onboarding_Step3_Hyper_Title    => Get(nameof(Onboarding_Step3_Hyper_Title));
    public static string Onboarding_Step3_Hyper_Sub      => Get(nameof(Onboarding_Step3_Hyper_Sub));

    public static string Onboarding_Step4_Title         => Get(nameof(Onboarding_Step4_Title));
    public static string Onboarding_Step4_Subtitle      => Get(nameof(Onboarding_Step4_Subtitle));
    public static string Onboarding_Step4_Activate      => Get(nameof(Onboarding_Step4_Activate));
    public static string Onboarding_Step4_SkipProgram   => Get(nameof(Onboarding_Step4_SkipProgram));
    public static string Onboarding_RecommendedFormat   => Get(nameof(Onboarding_RecommendedFormat));

    public static string Onboarding_Continue            => Get(nameof(Onboarding_Continue));
}
