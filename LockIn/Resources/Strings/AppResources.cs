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
    public static string Common_Cancel   => Get(nameof(Common_Cancel));
    public static string Common_Skip     => Get(nameof(Common_Skip));
    public static string Common_OK       => Get(nameof(Common_OK));
    public static string Common_Save     => Get(nameof(Common_Save));
    public static string Common_Delete   => Get(nameof(Common_Delete));
    public static string Common_Add      => Get(nameof(Common_Add));
    public static string Common_Edit     => Get(nameof(Common_Edit));
    public static string Common_Done     => Get(nameof(Common_Done));
    public static string Common_Close    => Get(nameof(Common_Close));
    public static string Common_Back     => Get(nameof(Common_Back));
    public static string Common_Yes      => Get(nameof(Common_Yes));
    public static string Common_No       => Get(nameof(Common_No));
    public static string Common_Continue => Get(nameof(Common_Continue));
    public static string Common_Undo     => Get(nameof(Common_Undo));
    public static string Common_Loading  => Get(nameof(Common_Loading));
    public static string Common_Error    => Get(nameof(Common_Error));
    public static string Common_Confirm  => Get(nameof(Common_Confirm));

    // ── Tab titles ─────────────────────────────────────────────────────
    public static string Tab_Home    => Get(nameof(Tab_Home));
    public static string Tab_Train   => Get(nameof(Tab_Train));
    public static string Tab_History => Get(nameof(Tab_History));
    public static string Tab_Library => Get(nameof(Tab_Library));
    public static string Tab_Body    => Get(nameof(Tab_Body));

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

    // ── Train ────────────────────────────────────────────────────────
    public static string Train_PageTitle                  => Get(nameof(Train_PageTitle));
    public static string Train_ThisWeek                   => Get(nameof(Train_ThisWeek));
    public static string Train_DaysStreak                 => Get(nameof(Train_DaysStreak));
    public static string Train_TotalSessions              => Get(nameof(Train_TotalSessions));
    public static string Train_Deload_Title               => Get(nameof(Train_Deload_Title));
    public static string Train_Deload_Body                => Get(nameof(Train_Deload_Body));
    public static string Train_Volume_Title               => Get(nameof(Train_Volume_Title));
    public static string Train_Volume_Max                 => Get(nameof(Train_Volume_Max));
    public static string Train_NoTemplates_Title          => Get(nameof(Train_NoTemplates_Title));
    public static string Train_NoTemplates_Body           => Get(nameof(Train_NoTemplates_Body));
    public static string Train_Programs_Section           => Get(nameof(Train_Programs_Section));
    public static string Train_SessionCount_Format        => Get(nameof(Train_SessionCount_Format));
    public static string Train_MyTemplates                => Get(nameof(Train_MyTemplates));
    public static string Train_FreeWorkout                => Get(nameof(Train_FreeWorkout));
    public static string Train_RestReminder_Title         => Get(nameof(Train_RestReminder_Title));
    public static string Train_RestReminder_Body_Format   => Get(nameof(Train_RestReminder_Body_Format));
    public static string Train_RestReminder_OK            => Get(nameof(Train_RestReminder_OK));
    public static string Train_ActiveSession_Title        => Get(nameof(Train_ActiveSession_Title));
    public static string Train_ActiveSession_Body         => Get(nameof(Train_ActiveSession_Body));
    public static string Train_ActiveSession_GoTo         => Get(nameof(Train_ActiveSession_GoTo));
    public static string Train_DeleteTemplate_Title       => Get(nameof(Train_DeleteTemplate_Title));
    public static string Train_DeleteTemplate_Body_Format => Get(nameof(Train_DeleteTemplate_Body_Format));
    public static string Train_Muscle_Chest               => Get(nameof(Train_Muscle_Chest));
    public static string Train_Muscle_Back                => Get(nameof(Train_Muscle_Back));
    public static string Train_Muscle_Shoulders           => Get(nameof(Train_Muscle_Shoulders));
    public static string Train_Muscle_Biceps              => Get(nameof(Train_Muscle_Biceps));
    public static string Train_Muscle_Triceps             => Get(nameof(Train_Muscle_Triceps));
    public static string Train_Muscle_Legs                => Get(nameof(Train_Muscle_Legs));
    public static string Train_Muscle_Core                => Get(nameof(Train_Muscle_Core));

    // ── Hem ─────────────────────────────────────────────────────────
    public static string Hem_ScoreCard_Title            => Get(nameof(Hem_ScoreCard_Title));
    public static string Hem_Recommendation_Label       => Get(nameof(Hem_Recommendation_Label));
    public static string Hem_Strain_Label               => Get(nameof(Hem_Strain_Label));
    public static string Hem_Recovery_Label             => Get(nameof(Hem_Recovery_Label));
    public static string Hem_Sleep_Label                => Get(nameof(Hem_Sleep_Label));
    public static string Hem_SleepStage_Awake           => Get(nameof(Hem_SleepStage_Awake));
    public static string Hem_Streak_Label               => Get(nameof(Hem_Streak_Label));
    public static string Hem_SeeAll                     => Get(nameof(Hem_SeeAll));
    public static string Hem_Activity_Label             => Get(nameof(Hem_Activity_Label));
    public static string Hem_Steps_Sub                  => Get(nameof(Hem_Steps_Sub));
    public static string Hem_Calories_Sub               => Get(nameof(Hem_Calories_Sub));
    public static string Hem_ActiveTime_Sub             => Get(nameof(Hem_ActiveTime_Sub));
    public static string Hem_HeartRate_Sub              => Get(nameof(Hem_HeartRate_Sub));
    public static string Hem_Muscles_Title              => Get(nameof(Hem_Muscles_Title));
    public static string Hem_WorkoutBanner_Label        => Get(nameof(Hem_WorkoutBanner_Label));
    public static string Hem_Greeting_Morning           => Get(nameof(Hem_Greeting_Morning));
    public static string Hem_Greeting_Forenoon          => Get(nameof(Hem_Greeting_Forenoon));
    public static string Hem_Greeting_Afternoon         => Get(nameof(Hem_Greeting_Afternoon));
    public static string Hem_Greeting_Evening           => Get(nameof(Hem_Greeting_Evening));
    public static string Hem_StreakDays_One             => Get(nameof(Hem_StreakDays_One));
    public static string Hem_StreakDays_Many            => Get(nameof(Hem_StreakDays_Many));
    public static string Hem_NoStreak                   => Get(nameof(Hem_NoStreak));
    public static string Hem_Motivation_Complete        => Get(nameof(Hem_Motivation_Complete));
    public static string Hem_Motivation_Strong          => Get(nameof(Hem_Motivation_Strong));
    public static string Hem_Motivation_Halfway         => Get(nameof(Hem_Motivation_Halfway));
    public static string Hem_Motivation_Going           => Get(nameof(Hem_Motivation_Going));
    public static string Hem_Motivation_Started         => Get(nameof(Hem_Motivation_Started));
    public static string Hem_Motivation_New             => Get(nameof(Hem_Motivation_New));
    public static string Hem_Recovery_Optimal          => Get(nameof(Hem_Recovery_Optimal));
    public static string Hem_Recovery_Good             => Get(nameof(Hem_Recovery_Good));
    public static string Hem_Recovery_Medium           => Get(nameof(Hem_Recovery_Medium));
    public static string Hem_Recovery_Low              => Get(nameof(Hem_Recovery_Low));
    public static string Hem_Sleep_Sufficient          => Get(nameof(Hem_Sleep_Sufficient));
    public static string Hem_Sleep_OK                  => Get(nameof(Hem_Sleep_OK));
    public static string Hem_Sleep_TooLittle           => Get(nameof(Hem_Sleep_TooLittle));
    public static string Hem_Loading_Data              => Get(nameof(Hem_Loading_Data));
    public static string Hem_Recovery_NoWatch          => Get(nameof(Hem_Recovery_NoWatch));
    public static string Hem_Recovery_Components       => Get(nameof(Hem_Recovery_Components));
    public static string Hem_StrainTarget_Format       => Get(nameof(Hem_StrainTarget_Format));
    public static string Hem_Rec_JustTrained_Head      => Get(nameof(Hem_Rec_JustTrained_Head));
    public static string Hem_Rec_JustTrained_Body      => Get(nameof(Hem_Rec_JustTrained_Body));
    public static string Hem_Rec_LongBreak_Head        => Get(nameof(Hem_Rec_LongBreak_Head));
    public static string Hem_Rec_LongBreak_Body        => Get(nameof(Hem_Rec_LongBreak_Body));
    public static string Hem_Rec_PlanRest_Head         => Get(nameof(Hem_Rec_PlanRest_Head));
    public static string Hem_Rec_PlanRest_Body         => Get(nameof(Hem_Rec_PlanRest_Body));
    public static string Hem_Rec_Head_RestPriority     => Get(nameof(Hem_Rec_Head_RestPriority));
    public static string Hem_Rec_Head_LightMove        => Get(nameof(Hem_Rec_Head_LightMove));
    public static string Hem_Rec_Head_NormalSession    => Get(nameof(Hem_Rec_Head_NormalSession));
    public static string Hem_Rec_Head_GoHard           => Get(nameof(Hem_Rec_Head_GoHard));
    public static string Hem_Rec_Head_PeakForm         => Get(nameof(Hem_Rec_Head_PeakForm));
    public static string Hem_Rec_Body_RestPriority     => Get(nameof(Hem_Rec_Body_RestPriority));
    public static string Hem_Rec_Body_LightMove        => Get(nameof(Hem_Rec_Body_LightMove));
    public static string Hem_Rec_Body_NormalSession    => Get(nameof(Hem_Rec_Body_NormalSession));
    public static string Hem_Rec_Body_GoHard           => Get(nameof(Hem_Rec_Body_GoHard));
    public static string Hem_Rec_Body_PeakForm         => Get(nameof(Hem_Rec_Body_PeakForm));
    public static string Hem_Rec_DifferentMuscle       => Get(nameof(Hem_Rec_DifferentMuscle));
    public static string Hem_Accessibility_GaugeLoading    => Get(nameof(Hem_Accessibility_GaugeLoading));
    public static string Hem_Accessibility_StrainLoading   => Get(nameof(Hem_Accessibility_StrainLoading));
    public static string Hem_Accessibility_RecoveryLoading => Get(nameof(Hem_Accessibility_RecoveryLoading));
    public static string Hem_Accessibility_SleepLoading    => Get(nameof(Hem_Accessibility_SleepLoading));
    public static string Hem_Accessibility_Gauge        => Get(nameof(Hem_Accessibility_Gauge));
    public static string Hem_Accessibility_Strain       => Get(nameof(Hem_Accessibility_Strain));
    public static string Hem_Accessibility_StrainNoData => Get(nameof(Hem_Accessibility_StrainNoData));
    public static string Hem_Accessibility_Recovery     => Get(nameof(Hem_Accessibility_Recovery));
    public static string Hem_Accessibility_Sleep        => Get(nameof(Hem_Accessibility_Sleep));
    public static string Hem_Accessibility_SleepNoData  => Get(nameof(Hem_Accessibility_SleepNoData));
    public static string Hem_Coach_WeeklySummary        => Get(nameof(Hem_Coach_WeeklySummary));
    public static string Hem_Coach_RecoveryTips         => Get(nameof(Hem_Coach_RecoveryTips));
    public static string Hem_Coach_NextSession          => Get(nameof(Hem_Coach_NextSession));
    public static string Hem_Sparkline_Steps            => Get(nameof(Hem_Sparkline_Steps));
    public static string Hem_Sparkline_Calories         => Get(nameof(Hem_Sparkline_Calories));
    public static string Hem_Sparkline_ActiveTime       => Get(nameof(Hem_Sparkline_ActiveTime));
    public static string Hem_Sparkline_HeartRate        => Get(nameof(Hem_Sparkline_HeartRate));
}
