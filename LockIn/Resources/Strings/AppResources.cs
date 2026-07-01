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

    internal static string Get(string key) =>
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

    // ── Cardio ─────────────────────────────────────────────────────────
    public static string Cardio_Title                        => Get(nameof(Cardio_Title));
    public static string Cardio_ActivityType_Label           => Get(nameof(Cardio_ActivityType_Label));
    public static string Cardio_Duration_Label               => Get(nameof(Cardio_Duration_Label));
    public static string Cardio_Distance_Label               => Get(nameof(Cardio_Distance_Label));
    public static string Cardio_HeartRate_Label              => Get(nameof(Cardio_HeartRate_Label));
    public static string Cardio_Calories_Label               => Get(nameof(Cardio_Calories_Label));
    public static string Cardio_Notes_Label                  => Get(nameof(Cardio_Notes_Label));
    public static string Cardio_CustomName_Label             => Get(nameof(Cardio_CustomName_Label));
    public static string Cardio_Save_Button                  => Get(nameof(Cardio_Save_Button));
    public static string Cardio_Delete_Confirm               => Get(nameof(Cardio_Delete_Confirm));
    public static string Cardio_Delete_Yes                   => Get(nameof(Cardio_Delete_Yes));
    public static string Cardio_Delete_No                    => Get(nameof(Cardio_Delete_No));
    public static string History_Cardio_Section              => Get(nameof(History_Cardio_Section));
    public static string History_Cardio_Minutes              => Get(nameof(History_Cardio_Minutes));
    public static string History_Cardio_Km                   => Get(nameof(History_Cardio_Km));
    public static string TrainPage_Cardio_Button             => Get(nameof(TrainPage_Cardio_Button));
    public static string Cardio_Activity_Running             => Get(nameof(Cardio_Activity_Running));
    public static string Cardio_Activity_OutdoorCycling      => Get(nameof(Cardio_Activity_OutdoorCycling));
    public static string Cardio_Activity_IndoorCycling       => Get(nameof(Cardio_Activity_IndoorCycling));
    public static string Cardio_Activity_Rowing              => Get(nameof(Cardio_Activity_Rowing));
    public static string Cardio_Activity_Stairmaster         => Get(nameof(Cardio_Activity_Stairmaster));
    public static string Cardio_Activity_Elliptical          => Get(nameof(Cardio_Activity_Elliptical));
    public static string Cardio_Activity_Walking             => Get(nameof(Cardio_Activity_Walking));
    public static string Cardio_Activity_Swimming            => Get(nameof(Cardio_Activity_Swimming));
    public static string Cardio_Activity_JumpRope            => Get(nameof(Cardio_Activity_JumpRope));
    public static string Cardio_Activity_Hiit                => Get(nameof(Cardio_Activity_Hiit));
    public static string Cardio_Activity_Boxing              => Get(nameof(Cardio_Activity_Boxing));
    public static string Cardio_Activity_Padel               => Get(nameof(Cardio_Activity_Padel));
    public static string Cardio_Activity_Dancing             => Get(nameof(Cardio_Activity_Dancing));
    public static string Cardio_Activity_Yoga                => Get(nameof(Cardio_Activity_Yoga));
    public static string Cardio_Activity_CrossCountrySkiing  => Get(nameof(Cardio_Activity_CrossCountrySkiing));
    public static string Cardio_Activity_Other               => Get(nameof(Cardio_Activity_Other));
    public static string Cardio_Activity_Custom              => Get(nameof(Cardio_Activity_Custom));

    // ── Notifications ──────────────────────────────────────────────────
    public static string Notification_Reminder_Body         => Get(nameof(Notification_Reminder_Body));
    public static string Notification_Reminder_Title        => Get(nameof(Notification_Reminder_Title));
    public static string Notification_Reminder_Body_Label_Format => Get(nameof(Notification_Reminder_Body_Label_Format));
    public static string Notification_RestTimer_Title       => Get(nameof(Notification_RestTimer_Title));
    public static string Notification_RestTimer_Body_Format => Get(nameof(Notification_RestTimer_Body_Format));

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

    // ── History ──────────────────────────────────────────────────────
    public static string History_Title                    => Get(nameof(History_Title));
    public static string History_Achievements             => Get(nameof(History_Achievements));
    public static string History_Period_All               => Get(nameof(History_Period_All));
    public static string History_Period_Week              => Get(nameof(History_Period_Week));
    public static string History_Period_Month             => Get(nameof(History_Period_Month));
    public static string History_Sort_Date                => Get(nameof(History_Sort_Date));
    public static string History_Sort_Volume              => Get(nameof(History_Sort_Volume));
    public static string History_Card_KgVolume            => Get(nameof(History_Card_KgVolume));
    public static string History_Card_Sets                => Get(nameof(History_Card_Sets));
    public static string History_Cal_Mon                  => Get(nameof(History_Cal_Mon));
    public static string History_Cal_Tue                  => Get(nameof(History_Cal_Tue));
    public static string History_Cal_Wed                  => Get(nameof(History_Cal_Wed));
    public static string History_Cal_Thu                  => Get(nameof(History_Cal_Thu));
    public static string History_Cal_Fri                  => Get(nameof(History_Cal_Fri));
    public static string History_Cal_Sat                  => Get(nameof(History_Cal_Sat));
    public static string History_Cal_Sun                  => Get(nameof(History_Cal_Sun));
    public static string History_Empty_Title_NoSessions   => Get(nameof(History_Empty_Title_NoSessions));
    public static string History_Empty_Title_NoResults    => Get(nameof(History_Empty_Title_NoResults));
    public static string History_Empty_Body_NoSessions    => Get(nameof(History_Empty_Body_NoSessions));
    public static string History_Empty_Body_NoResults     => Get(nameof(History_Empty_Body_NoResults));

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
    public static string Train_Muscle_Forearms            => Get(nameof(Train_Muscle_Forearms));

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
    public static string Hem_VO2Max_Label               => Get(nameof(Hem_VO2Max_Label));
    public static string Hem_VO2Max_Sub                 => Get(nameof(Hem_VO2Max_Sub));
    public static string Hem_Muscles_Title              => Get(nameof(Hem_Muscles_Title));
    public static string Hem_MuscleTrend_Title          => Get(nameof(Hem_MuscleTrend_Title));
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

    // Hem — Coach chips
    public static string Hem_Chip_PRProximity_Text    => Get(nameof(Hem_Chip_PRProximity_Text));
    public static string Hem_Chip_MuscleGap_Text      => Get(nameof(Hem_Chip_MuscleGap_Text));
    public static string Hem_Chip_VolumeTrendUp_Text  => Get(nameof(Hem_Chip_VolumeTrendUp_Text));
    public static string Hem_Chip_VolumeTrendDown_Text => Get(nameof(Hem_Chip_VolumeTrendDown_Text));
    public static string Hem_Chip_WeekSummary_Text    => Get(nameof(Hem_Chip_WeekSummary_Text));
    public static string Hem_Chip_StreakWeeks_Text     => Get(nameof(Hem_Chip_StreakWeeks_Text));
    public static string Hem_Chip_PRProximity_Header  => Get(nameof(Hem_Chip_PRProximity_Header));
    public static string Hem_Chip_PRProximity_Body    => Get(nameof(Hem_Chip_PRProximity_Body));
    public static string Hem_Chip_MuscleGap_Header    => Get(nameof(Hem_Chip_MuscleGap_Header));
    public static string Hem_Chip_MuscleGap_Body      => Get(nameof(Hem_Chip_MuscleGap_Body));
    public static string Hem_Chip_VolumeTrend_Header  => Get(nameof(Hem_Chip_VolumeTrend_Header));
    public static string Hem_Chip_VolumeTrend_Body    => Get(nameof(Hem_Chip_VolumeTrend_Body));
    public static string Hem_Chip_WeekSummary_Header  => Get(nameof(Hem_Chip_WeekSummary_Header));
    public static string Hem_Chip_WeekSummary_Body    => Get(nameof(Hem_Chip_WeekSummary_Body));
    public static string Hem_Chip_StreakWeeks_Header   => Get(nameof(Hem_Chip_StreakWeeks_Header));
    public static string Hem_Chip_StreakWeeks_Body     => Get(nameof(Hem_Chip_StreakWeeks_Body));

    public static string Hem_Sparkline_Steps            => Get(nameof(Hem_Sparkline_Steps));
    public static string Hem_Sparkline_Calories         => Get(nameof(Hem_Sparkline_Calories));
    public static string Hem_Sparkline_ActiveTime       => Get(nameof(Hem_Sparkline_ActiveTime));
    public static string Hem_Sparkline_HeartRate        => Get(nameof(Hem_Sparkline_HeartRate));

    // ── Kropp ───────────────────────────────────────────────────────────
    public static string Kropp_Title                        => Get(nameof(Kropp_Title));
    public static string Kropp_Tab_Weight                   => Get(nameof(Kropp_Tab_Weight));
    public static string Kropp_Tab_Body                     => Get(nameof(Kropp_Tab_Body));
    public static string Kropp_Tab_Heatmap                  => Get(nameof(Kropp_Tab_Heatmap));
    public static string Kropp_NoWeightData                 => Get(nameof(Kropp_NoWeightData));
    public static string Kropp_WeightProgress               => Get(nameof(Kropp_WeightProgress));
    public static string Kropp_RecentLogs                   => Get(nameof(Kropp_RecentLogs));
    public static string Kropp_LogWeight                    => Get(nameof(Kropp_LogWeight));
    public static string Kropp_LatestMeasurement            => Get(nameof(Kropp_LatestMeasurement));
    public static string Kropp_Measurement_Waist            => Get(nameof(Kropp_Measurement_Waist));
    public static string Kropp_Measurement_Chest            => Get(nameof(Kropp_Measurement_Chest));
    public static string Kropp_Measurement_Hips             => Get(nameof(Kropp_Measurement_Hips));
    public static string Kropp_Measurement_Arms             => Get(nameof(Kropp_Measurement_Arms));
    public static string Kropp_Measurement_Thighs           => Get(nameof(Kropp_Measurement_Thighs));
    public static string Kropp_NoMeasurementData            => Get(nameof(Kropp_NoMeasurementData));
    public static string Kropp_MeasurementHistory           => Get(nameof(Kropp_MeasurementHistory));
    public static string Kropp_LogMeasurement               => Get(nameof(Kropp_LogMeasurement));
    public static string Kropp_MuscleGroups_Title           => Get(nameof(Kropp_MuscleGroups_Title));
    public static string Kropp_LogWeight_Title              => Get(nameof(Kropp_LogWeight_Title));
    public static string Kropp_LogWeight_Body               => Get(nameof(Kropp_LogWeight_Body));
    public static string Kropp_LogWeight_Placeholder        => Get(nameof(Kropp_LogWeight_Placeholder));
    public static string Kropp_DeleteWeight_Body_Format     => Get(nameof(Kropp_DeleteWeight_Body_Format));
    public static string Kropp_Prompt_Waist_Body            => Get(nameof(Kropp_Prompt_Waist_Body));
    public static string Kropp_Prompt_Chest_Body            => Get(nameof(Kropp_Prompt_Chest_Body));
    public static string Kropp_Prompt_Hips_Body             => Get(nameof(Kropp_Prompt_Hips_Body));
    public static string Kropp_Prompt_Arms_Body             => Get(nameof(Kropp_Prompt_Arms_Body));
    public static string Kropp_Prompt_Thighs_Body           => Get(nameof(Kropp_Prompt_Thighs_Body));
    public static string Kropp_DeleteMeasurement_Body_Format => Get(nameof(Kropp_DeleteMeasurement_Body_Format));

    // ── SessionDetail ────────────────────────────────────────────────────
    public static string SessionDetail_Col_Volume        => Get(nameof(SessionDetail_Col_Volume));
    public static string SessionDetail_Col_Duration      => Get(nameof(SessionDetail_Col_Duration));
    public static string SessionDetail_Col_PRs           => Get(nameof(SessionDetail_Col_PRs));
    public static string SessionDetail_Notes             => Get(nameof(SessionDetail_Notes));
    public static string SessionDetail_Photos            => Get(nameof(SessionDetail_Photos));
    public static string SessionDetail_AddPhoto          => Get(nameof(SessionDetail_AddPhoto));
    public static string SessionDetail_AddPhoto_Title    => Get(nameof(SessionDetail_AddPhoto_Title));
    public static string SessionDetail_Photo_TakePhoto   => Get(nameof(SessionDetail_Photo_TakePhoto));
    public static string SessionDetail_Photo_PickLibrary => Get(nameof(SessionDetail_Photo_PickLibrary));
    public static string SessionDetail_DeletePhoto_Title => Get(nameof(SessionDetail_DeletePhoto_Title));
    public static string SessionDetail_DeletePhoto_Body  => Get(nameof(SessionDetail_DeletePhoto_Body));

    // ── ActiveWorkout ────────────────────────────────────────────────────
    public static string ActiveWorkout_FreeWorkout                   => Get(nameof(ActiveWorkout_FreeWorkout));
    public static string ActiveWorkout_Superset                      => Get(nameof(ActiveWorkout_Superset));
    public static string ActiveWorkout_Col_Set                       => Get(nameof(ActiveWorkout_Col_Set));
    public static string ActiveWorkout_Col_Weight                    => Get(nameof(ActiveWorkout_Col_Weight));
    public static string ActiveWorkout_Col_Reps                      => Get(nameof(ActiveWorkout_Col_Reps));
    public static string ActiveWorkout_Col_Rir                       => Get(nameof(ActiveWorkout_Col_Rir));
    public static string ActiveWorkout_AddSet                        => Get(nameof(ActiveWorkout_AddSet));
    public static string ActiveWorkout_RemoveSet                     => Get(nameof(ActiveWorkout_RemoveSet));
    public static string ActiveWorkout_AddExercise                   => Get(nameof(ActiveWorkout_AddExercise));
    public static string ActiveWorkout_FinishWorkout                 => Get(nameof(ActiveWorkout_FinishWorkout));
    public static string ActiveWorkout_RemoveExercise_Title          => Get(nameof(ActiveWorkout_RemoveExercise_Title));
    public static string ActiveWorkout_RemoveExercise_Body_Logs      => Get(nameof(ActiveWorkout_RemoveExercise_Body_Logs));
    public static string ActiveWorkout_RemoveExercise_Body_Empty     => Get(nameof(ActiveWorkout_RemoveExercise_Body_Empty));
    public static string ActiveWorkout_RestTime_Title                => Get(nameof(ActiveWorkout_RestTime_Title));
    public static string ActiveWorkout_RestTime_Body_Format          => Get(nameof(ActiveWorkout_RestTime_Body_Format));
    public static string ActiveWorkout_Toast_EnterDuration           => Get(nameof(ActiveWorkout_Toast_EnterDuration));
    public static string ActiveWorkout_Toast_EnterReps               => Get(nameof(ActiveWorkout_Toast_EnterReps));
    public static string ActiveWorkout_PR_Message_Format             => Get(nameof(ActiveWorkout_PR_Message_Format));
    public static string ActiveWorkout_AutoProgress_WeightUp_Format  => Get(nameof(ActiveWorkout_AutoProgress_WeightUp_Format));
    public static string ActiveWorkout_AutoProgress_RepsUp_Format    => Get(nameof(ActiveWorkout_AutoProgress_RepsUp_Format));

    // ── RP RIR-hint ─────────────────────────────────────────────────────
    public static string Rir_Hint_Text                               => Get(nameof(Rir_Hint_Text));
    public static string Rir_Hint_Dismiss                            => Get(nameof(Rir_Hint_Dismiss));

    // ── RP vikt-progression ─────────────────────────────────────────────
    public static string WeightSuggestion_Reason_HitAllReps          => Get(nameof(WeightSuggestion_Reason_HitAllReps));
    public static string WeightSuggestion_Reason_MaintainWeight      => Get(nameof(WeightSuggestion_Reason_MaintainWeight));
    public static string WeightSuggestion_Reason_MissedReps          => Get(nameof(WeightSuggestion_Reason_MissedReps));

    // ── PostWorkout RP recovery-feedback ─────────────────────────────────
    public static string PostWorkout_Feedback_Header                 => Get(nameof(PostWorkout_Feedback_Header));
    public static string PostWorkout_Pump_Question                   => Get(nameof(PostWorkout_Pump_Question));
    public static string PostWorkout_Soreness_Question               => Get(nameof(PostWorkout_Soreness_Question));
    public static string PostWorkout_Performance_Question            => Get(nameof(PostWorkout_Performance_Question));
    public static string PostWorkout_Scale_Low                       => Get(nameof(PostWorkout_Scale_Low));
    public static string PostWorkout_Scale_High                      => Get(nameof(PostWorkout_Scale_High));

    // ── RP coach-chips ─────────────────────────────────────────────────
    public static string CoachChip_Deload_Text                       => Get(nameof(CoachChip_Deload_Text));
    public static string CoachChip_Deload_Header                     => Get(nameof(CoachChip_Deload_Header));
    public static string CoachChip_Deload_Body                       => Get(nameof(CoachChip_Deload_Body));
    public static string CoachChip_VolumeUp_Text_Format              => Get(nameof(CoachChip_VolumeUp_Text_Format));
    public static string CoachChip_VolumeUp_Header_Format            => Get(nameof(CoachChip_VolumeUp_Header_Format));
    public static string CoachChip_VolumeUp_Body_Format              => Get(nameof(CoachChip_VolumeUp_Body_Format));
    public static string CoachChip_VolumeDown_Text_Format            => Get(nameof(CoachChip_VolumeDown_Text_Format));
    public static string CoachChip_VolumeDown_Header_Format          => Get(nameof(CoachChip_VolumeDown_Header_Format));
    public static string CoachChip_VolumeDown_Body_Format            => Get(nameof(CoachChip_VolumeDown_Body_Format));
    public static string ActiveWorkout_FinishConfirm_Title           => Get(nameof(ActiveWorkout_FinishConfirm_Title));
    public static string ActiveWorkout_FinishConfirm_Body            => Get(nameof(ActiveWorkout_FinishConfirm_Body));
    public static string ActiveWorkout_FinishConfirm_Yes             => Get(nameof(ActiveWorkout_FinishConfirm_Yes));

    // ── PostWorkout ──────────────────────────────────────────────────────
    public static string PostWorkout_GreatJob                 => Get(nameof(PostWorkout_GreatJob));
    public static string PostWorkout_Stats_Volume             => Get(nameof(PostWorkout_Stats_Volume));
    public static string PostWorkout_Stats_Sets               => Get(nameof(PostWorkout_Stats_Sets));
    public static string PostWorkout_Stats_PR                 => Get(nameof(PostWorkout_Stats_PR));
    public static string PostWorkout_MuscleGroups             => Get(nameof(PostWorkout_MuscleGroups));
    public static string PostWorkout_Notes                    => Get(nameof(PostWorkout_Notes));
    public static string PostWorkout_Notes_Placeholder        => Get(nameof(PostWorkout_Notes_Placeholder));
    public static string PostWorkout_NewAchievements          => Get(nameof(PostWorkout_NewAchievements));
    public static string PostWorkout_Achievement_Unlocked     => Get(nameof(PostWorkout_Achievement_Unlocked));
    public static string PostWorkout_Photos                   => Get(nameof(PostWorkout_Photos));
    public static string PostWorkout_AddPhoto                 => Get(nameof(PostWorkout_AddPhoto));
    public static string PostWorkout_PersonalRecords          => Get(nameof(PostWorkout_PersonalRecords));
    public static string PostWorkout_AddPhoto_Title           => Get(nameof(PostWorkout_AddPhoto_Title));
    public static string PostWorkout_Photo_TakePhoto          => Get(nameof(PostWorkout_Photo_TakePhoto));
    public static string PostWorkout_Photo_PickLibrary        => Get(nameof(PostWorkout_Photo_PickLibrary));
    public static string PostWorkout_DeletePhoto_Title        => Get(nameof(PostWorkout_DeletePhoto_Title));
    public static string PostWorkout_DeletePhoto_Body         => Get(nameof(PostWorkout_DeletePhoto_Body));
    public static string PostWorkout_VolumeDisplay_Format     => Get(nameof(PostWorkout_VolumeDisplay_Format));
    public static string PostWorkout_SetsDisplay_Format       => Get(nameof(PostWorkout_SetsDisplay_Format));
    public static string PostWorkout_Epley1RM_Format          => Get(nameof(PostWorkout_Epley1RM_Format));
    public static string PostWorkout_UndoFinish              => Get(nameof(PostWorkout_UndoFinish));
    public static string PostWorkout_Share                  => Get(nameof(PostWorkout_Share));
    public static string PostWorkout_Share_Error            => Get(nameof(PostWorkout_Share_Error));
    public static string PostWorkout_Share_ImageTitle       => Get(nameof(PostWorkout_Share_ImageTitle));
    public static string ExerciseProgress_Share_Error      => Get(nameof(ExerciseProgress_Share_Error));
    public static string ExerciseProgress_Share_ImageTitle => Get(nameof(ExerciseProgress_Share_ImageTitle));
    public static string Share_Footer_Tagline               => Get(nameof(Share_Footer_Tagline));

    // ── TemplateEdit ─────────────────────────────────────────────────────
    public static string TemplateEdit_PageTitle           => Get(nameof(TemplateEdit_PageTitle));
    public static string TemplateEdit_SectionName         => Get(nameof(TemplateEdit_SectionName));
    public static string TemplateEdit_NamePlaceholder     => Get(nameof(TemplateEdit_NamePlaceholder));
    public static string TemplateEdit_SectionExercises    => Get(nameof(TemplateEdit_SectionExercises));
    public static string TemplateEdit_AutoProgression     => Get(nameof(TemplateEdit_AutoProgression));
    public static string TemplateEdit_MinReps             => Get(nameof(TemplateEdit_MinReps));
    public static string TemplateEdit_MaxReps             => Get(nameof(TemplateEdit_MaxReps));
    public static string TemplateEdit_WeightIncrement     => Get(nameof(TemplateEdit_WeightIncrement));
    public static string TemplateEdit_ColRest             => Get(nameof(TemplateEdit_ColRest));
    public static string TemplateEdit_AddExercise         => Get(nameof(TemplateEdit_AddExercise));
    public static string TemplateEdit_UnknownExercise     => Get(nameof(TemplateEdit_UnknownExercise));
    public static string TemplateEdit_RestTime_Title      => Get(nameof(TemplateEdit_RestTime_Title));
    public static string TemplateEdit_RestTime_Body_Format => Get(nameof(TemplateEdit_RestTime_Body_Format));
    public static string TemplateEdit_SupersetToast       => Get(nameof(TemplateEdit_SupersetToast));
    public static string TemplateEdit_Toast_EnterName     => Get(nameof(TemplateEdit_Toast_EnterName));
    public static string TemplateEdit_SupersetAdd         => Get(nameof(TemplateEdit_SupersetAdd));
    public static string TemplateEdit_SupersetRemove      => Get(nameof(TemplateEdit_SupersetRemove));
    public static string TemplateEdit_ClearExercise       => Get(nameof(TemplateEdit_ClearExercise));

    // ── ExerciseProgress ─────────────────────────────────────────────────
    public static string ExerciseProgress_Chart_1RM            => Get(nameof(ExerciseProgress_Chart_1RM));
    public static string ExerciseProgress_BestLift             => Get(nameof(ExerciseProgress_BestLift));
    public static string ExerciseProgress_Est1RM_Prefix        => Get(nameof(ExerciseProgress_Est1RM_Prefix));
    public static string ExerciseProgress_TotalVolume          => Get(nameof(ExerciseProgress_TotalVolume));
    public static string ExerciseProgress_LoggedSessions       => Get(nameof(ExerciseProgress_LoggedSessions));
    public static string ExerciseProgress_Empty_Title          => Get(nameof(ExerciseProgress_Empty_Title));
    public static string ExerciseProgress_Empty_Body           => Get(nameof(ExerciseProgress_Empty_Body));
    public static string ExerciseProgress_ExerciseInfo         => Get(nameof(ExerciseProgress_ExerciseInfo));
    public static string ExerciseProgress_Equipment            => Get(nameof(ExerciseProgress_Equipment));
    public static string ExerciseProgress_Level                => Get(nameof(ExerciseProgress_Level));
    public static string ExerciseProgress_Type                 => Get(nameof(ExerciseProgress_Type));
    public static string ExerciseProgress_Force                => Get(nameof(ExerciseProgress_Force));
    public static string ExerciseProgress_SecondaryMuscles     => Get(nameof(ExerciseProgress_SecondaryMuscles));
    public static string ExerciseProgress_Instructions         => Get(nameof(ExerciseProgress_Instructions));
    public static string ExerciseProgress_Notes                => Get(nameof(ExerciseProgress_Notes));
    public static string ExerciseProgress_Notes_Placeholder    => Get(nameof(ExerciseProgress_Notes_Placeholder));
    public static string ExerciseProgress_Sessions_Format      => Get(nameof(ExerciseProgress_Sessions_Format));
    public static string ExerciseProgress_Level_Beginner       => Get(nameof(ExerciseProgress_Level_Beginner));
    public static string ExerciseProgress_Level_Intermediate   => Get(nameof(ExerciseProgress_Level_Intermediate));
    public static string ExerciseProgress_Level_Expert         => Get(nameof(ExerciseProgress_Level_Expert));
    public static string ExerciseProgress_Mechanic_Compound    => Get(nameof(ExerciseProgress_Mechanic_Compound));
    public static string ExerciseProgress_Mechanic_Isolation   => Get(nameof(ExerciseProgress_Mechanic_Isolation));
    public static string ExerciseProgress_ForceType_Push       => Get(nameof(ExerciseProgress_ForceType_Push));
    public static string ExerciseProgress_ForceType_Pull       => Get(nameof(ExerciseProgress_ForceType_Pull));
    public static string ExerciseProgress_ForceType_Static     => Get(nameof(ExerciseProgress_ForceType_Static));

    // ── ExercisePicker ───────────────────────────────────────────────────
    public static string ExercisePicker_Title             => Get(nameof(ExercisePicker_Title));
    public static string ExercisePicker_SearchPlaceholder => Get(nameof(ExercisePicker_SearchPlaceholder));
    public static string ExercisePicker_CreateCustom      => Get(nameof(ExercisePicker_CreateCustom));

    // ── Library ─────────────────────────────────────────────────────────
    public static string Library_Title                      => Get(nameof(Library_Title));
    public static string Library_AddButton                  => Get(nameof(Library_AddButton));
    public static string Library_Tab_Exercises              => Get(nameof(Library_Tab_Exercises));
    public static string Library_Tab_Templates              => Get(nameof(Library_Tab_Templates));
    public static string Library_Tab_Programs               => Get(nameof(Library_Tab_Programs));
    public static string Library_Tab_Cycles                 => Get(nameof(Library_Tab_Cycles));
    public static string Library_SearchPlaceholder          => Get(nameof(Library_SearchPlaceholder));
    public static string Library_NoTemplates_Title          => Get(nameof(Library_NoTemplates_Title));
    public static string Library_NoTemplates_Body           => Get(nameof(Library_NoTemplates_Body));
    public static string Library_Badge_Custom               => Get(nameof(Library_Badge_Custom));
    public static string Library_ViewProgram                => Get(nameof(Library_ViewProgram));
    public static string Library_DaysPerWeek_Format         => Get(nameof(Library_DaysPerWeek_Format));
    public static string Library_DuplicateTemplate          => Get(nameof(Library_DuplicateTemplate));
    public static string Library_DuplicateTemplate_NamePrefix => Get(nameof(Library_DuplicateTemplate_NamePrefix));
    public static string Library_Chip_All                   => Get(nameof(Library_Chip_All));
    public static string Library_DeleteTemplate_Title       => Get(nameof(Library_DeleteTemplate_Title));
    public static string Library_DeleteTemplate_Body_Format => Get(nameof(Library_DeleteTemplate_Body_Format));
    public static string Library_Muscle_Chest               => Get(nameof(Library_Muscle_Chest));
    public static string Library_Muscle_Back                => Get(nameof(Library_Muscle_Back));
    public static string Library_Muscle_Shoulders           => Get(nameof(Library_Muscle_Shoulders));
    public static string Library_Muscle_Biceps              => Get(nameof(Library_Muscle_Biceps));
    public static string Library_Muscle_Triceps             => Get(nameof(Library_Muscle_Triceps));
    public static string Library_Muscle_Forearms            => Get(nameof(Library_Muscle_Forearms));
    public static string Library_Muscle_Legs                => Get(nameof(Library_Muscle_Legs));
    public static string Library_Muscle_Core                => Get(nameof(Library_Muscle_Core));
    public static string Library_Muscle_FullBody            => Get(nameof(Library_Muscle_FullBody));
    public static string Library_Muscle_Other               => Get(nameof(Library_Muscle_Other));
    public static string Library_Equipment_Barbell          => Get(nameof(Library_Equipment_Barbell));
    public static string Library_Equipment_Dumbbell         => Get(nameof(Library_Equipment_Dumbbell));
    public static string Library_Equipment_Cable            => Get(nameof(Library_Equipment_Cable));
    public static string Library_Equipment_Machine          => Get(nameof(Library_Equipment_Machine));
    public static string Library_Equipment_Bodyweight       => Get(nameof(Library_Equipment_Bodyweight));
    public static string Library_Equipment_EZBar            => Get(nameof(Library_Equipment_EZBar));
    public static string Library_Equipment_Kettlebell       => Get(nameof(Library_Equipment_Kettlebell));
    public static string Library_Equipment_Bands            => Get(nameof(Library_Equipment_Bands));
    public static string Library_Equipment_FoamRoll         => Get(nameof(Library_Equipment_FoamRoll));
    public static string Library_Equipment_MedicineBall     => Get(nameof(Library_Equipment_MedicineBall));
    public static string Library_Equipment_Other            => Get(nameof(Library_Equipment_Other));

    // ── Periodization ───────────────────────────────────────────────────
    public static string Periodization_Title                => Get(nameof(Periodization_Title));
    public static string Periodization_NoCycles_Title       => Get(nameof(Periodization_NoCycles_Title));
    public static string Periodization_NoCycles_Body        => Get(nameof(Periodization_NoCycles_Body));
    public static string Periodization_NewButton            => Get(nameof(Periodization_NewButton));
    public static string Periodization_Active_Badge         => Get(nameof(Periodization_Active_Badge));
    public static string Periodization_Weeks_Format         => Get(nameof(Periodization_Weeks_Format));
    public static string Periodization_Week_Label_Format    => Get(nameof(Periodization_Week_Label_Format));

    // ── CycleDetail ──────────────────────────────────────────────────────
    public static string CycleDetail_Title_New              => Get(nameof(CycleDetail_Title_New));
    public static string CycleDetail_Title_Edit             => Get(nameof(CycleDetail_Title_Edit));
    public static string CycleDetail_Name_Placeholder       => Get(nameof(CycleDetail_Name_Placeholder));
    public static string CycleDetail_WeekCount_Label        => Get(nameof(CycleDetail_WeekCount_Label));
    public static string CycleDetail_StartDate_Label        => Get(nameof(CycleDetail_StartDate_Label));
    public static string CycleDetail_Label_Placeholder      => Get(nameof(CycleDetail_Label_Placeholder));
    public static string CycleDetail_Intensity_Label_Format => Get(nameof(CycleDetail_Intensity_Label_Format));
    public static string CycleDetail_Save_Button            => Get(nameof(CycleDetail_Save_Button));
    public static string CycleDetail_PickTemplate_Title     => Get(nameof(CycleDetail_PickTemplate_Title));
    public static string CycleDetail_PickTemplate_Cancel    => Get(nameof(CycleDetail_PickTemplate_Cancel));
    public static string CycleDetail_NoTemplate             => Get(nameof(CycleDetail_NoTemplate));
    public static string CycleDetail_Delete_Title           => Get(nameof(CycleDetail_Delete_Title));
    public static string CycleDetail_Delete_Body_Format     => Get(nameof(CycleDetail_Delete_Body_Format));

    // ── Weekdays ─────────────────────────────────────────────────────────
    public static string Day_0                              => Get(nameof(Day_0));
    public static string Day_1                              => Get(nameof(Day_1));
    public static string Day_2                              => Get(nameof(Day_2));
    public static string Day_3                              => Get(nameof(Day_3));
    public static string Day_4                              => Get(nameof(Day_4));
    public static string Day_5                              => Get(nameof(Day_5));
    public static string Day_6                              => Get(nameof(Day_6));

    // ── ProgramDetail ────────────────────────────────────────────────────
    public static string ProgramDetail_SectionDays          => Get(nameof(ProgramDetail_SectionDays));
    public static string ProgramDetail_ActivateButton       => Get(nameof(ProgramDetail_ActivateButton));
    public static string ProgramDetail_DaysLabel_Format     => Get(nameof(ProgramDetail_DaysLabel_Format));
    public static string ProgramDetail_RestSeconds_Format   => Get(nameof(ProgramDetail_RestSeconds_Format));
    public static string ProgramDetail_Activate_Title       => Get(nameof(ProgramDetail_Activate_Title));
    public static string ProgramDetail_Activate_Body_Format => Get(nameof(ProgramDetail_Activate_Body_Format));
    public static string ProgramDetail_Activate_Confirm     => Get(nameof(ProgramDetail_Activate_Confirm));
    public static string ProgramDetail_Toast_Created_Format => Get(nameof(ProgramDetail_Toast_Created_Format));

    // ── Achievements ────────────────────────────────────────────────────
    public static string Achievements_Title                 => Get(nameof(Achievements_Title));
    public static string Achievements_Unlocked              => Get(nameof(Achievements_Unlocked));

    // ── Settings ─────────────────────────────────────────────────────────
    public static string Settings_PageTitle             => Get(nameof(Settings_PageTitle));
    public static string Settings_Section_Profile       => Get(nameof(Settings_Section_Profile));
    public static string Settings_Profile_Name          => Get(nameof(Settings_Profile_Name));
    public static string Settings_Section_Training      => Get(nameof(Settings_Section_Training));
    public static string Settings_WeeklyGoal_Title      => Get(nameof(Settings_WeeklyGoal_Title));
    public static string Settings_Section_Units         => Get(nameof(Settings_Section_Units));
    public static string Settings_WeightUnit_Title      => Get(nameof(Settings_WeightUnit_Title));
    public static string Settings_Section_RestTimer     => Get(nameof(Settings_Section_RestTimer));
    public static string Settings_Section_Reminders     => Get(nameof(Settings_Section_Reminders));
    public static string Settings_Vibration_Title       => Get(nameof(Settings_Vibration_Title));
    public static string Settings_Vibration_Description => Get(nameof(Settings_Vibration_Description));
    public static string Settings_Sound_Title           => Get(nameof(Settings_Sound_Title));
    public static string Settings_Sound_Description     => Get(nameof(Settings_Sound_Description));
    public static string Settings_Height_Title          => Get(nameof(Settings_Height_Title));
    public static string Settings_Section_Health        => Get(nameof(Settings_Section_Health));
    public static string Settings_HealthSync_Title      => Get(nameof(Settings_HealthSync_Title));
    public static string Settings_HealthSync_Description => Get(nameof(Settings_HealthSync_Description));
    public static string Settings_BodyWeight_Title      => Get(nameof(Settings_BodyWeight_Title));
    public static string Settings_BodyWeight_Description => Get(nameof(Settings_BodyWeight_Description));
    public static string Settings_ProgressPhotos_Title  => Get(nameof(Settings_ProgressPhotos_Title));
    public static string Settings_ProgressPhotos_Description => Get(nameof(Settings_ProgressPhotos_Description));
    public static string Settings_Section_Data          => Get(nameof(Settings_Section_Data));
    public static string Settings_ClearData_Button      => Get(nameof(Settings_ClearData_Button));
    public static string Settings_EditName_Title        => Get(nameof(Settings_EditName_Title));
    public static string Settings_EditName_Body         => Get(nameof(Settings_EditName_Body));
    public static string Settings_EditWeeklyGoal_Title  => Get(nameof(Settings_EditWeeklyGoal_Title));
    public static string Settings_EditWeeklyGoal_Body   => Get(nameof(Settings_EditWeeklyGoal_Body));
    public static string Settings_Height_Format         => Get(nameof(Settings_Height_Format));
    public static string Settings_Height_Prompt_Body    => Get(nameof(Settings_Height_Prompt_Body));
    public static string Settings_Height_Prompt_Title   => Get(nameof(Settings_Height_Prompt_Title));
    public static string Settings_ClearData_Body        => Get(nameof(Settings_ClearData_Body));
    public static string Settings_ClearData_Confirm     => Get(nameof(Settings_ClearData_Confirm));
    public static string Settings_ClearData_Toast       => Get(nameof(Settings_ClearData_Toast));
    public static string Settings_Reminders_Day_0       => Get(nameof(Settings_Reminders_Day_0));
    public static string Settings_Reminders_Day_1       => Get(nameof(Settings_Reminders_Day_1));
    public static string Settings_Reminders_Day_2       => Get(nameof(Settings_Reminders_Day_2));
    public static string Settings_Reminders_Day_3       => Get(nameof(Settings_Reminders_Day_3));
    public static string Settings_Reminders_Day_4       => Get(nameof(Settings_Reminders_Day_4));
    public static string Settings_Reminders_Day_5       => Get(nameof(Settings_Reminders_Day_5));
    public static string Settings_Reminders_Day_6       => Get(nameof(Settings_Reminders_Day_6));
    public static string Settings_Reminders_Off         => Get(nameof(Settings_Reminders_Off));
    public static string Settings_Reminders_Time_Label        => Get(nameof(Settings_Reminders_Time_Label));
    public static string Settings_Reminders_TimeInvalid       => Get(nameof(Settings_Reminders_TimeInvalid));
    public static string Settings_Reminders_TimePrompt_Body   => Get(nameof(Settings_Reminders_TimePrompt_Body));
    public static string Settings_Reminders_TimePrompt_Title  => Get(nameof(Settings_Reminders_TimePrompt_Title));
    public static string Settings_Reminders_Title             => Get(nameof(Settings_Reminders_Title));
    public static string Settings_Reminders_Label_Prompt_Title => Get(nameof(Settings_Reminders_Label_Prompt_Title));
    public static string Settings_Reminders_Label_Prompt_Body  => Get(nameof(Settings_Reminders_Label_Prompt_Body));
    public static string Settings_ExportData_Title    => Get(nameof(Settings_ExportData_Title));
    public static string Settings_ExportData_Subtitle => Get(nameof(Settings_ExportData_Subtitle));
    public static string Settings_ExportData_Error    => Get(nameof(Settings_ExportData_Error));
    public static string Settings_WeeklyGoal_Format     => Get(nameof(Settings_WeeklyGoal_Format));

    // ── PlateCalculator ──────────────────────────────────────────────────
    public static string PlateCalculator_Title                  => Get(nameof(PlateCalculator_Title));
    public static string PlateCalculator_TargetWeight           => Get(nameof(PlateCalculator_TargetWeight));
    public static string PlateCalculator_BarWeight              => Get(nameof(PlateCalculator_BarWeight));
    public static string PlateCalculator_Calculate              => Get(nameof(PlateCalculator_Calculate));
    public static string PlateCalculator_PerSide                => Get(nameof(PlateCalculator_PerSide));
    public static string PlateCalculator_AvailablePlates        => Get(nameof(PlateCalculator_AvailablePlates));
    public static string PlateCalculator_InvalidTarget          => Get(nameof(PlateCalculator_InvalidTarget));
    public static string PlateCalculator_TargetBelowBar_Format  => Get(nameof(PlateCalculator_TargetBelowBar_Format));
    public static string PlateCalculator_NotExact_Format        => Get(nameof(PlateCalculator_NotExact_Format));
    public static string PlateCalculator_BarOnly_Format         => Get(nameof(PlateCalculator_BarOnly_Format));
    public static string PlateCalculator_PerSideSuffix          => Get(nameof(PlateCalculator_PerSideSuffix));

    // ── BodyWeight ──────────────────────────────────────────────────────
    public static string BodyWeight_Title              => Get(nameof(BodyWeight_Title));
    public static string BodyWeight_NoData             => Get(nameof(BodyWeight_NoData));
    public static string BodyWeight_ChartTitle         => Get(nameof(BodyWeight_ChartTitle));
    public static string BodyWeight_Bmi                => Get(nameof(BodyWeight_Bmi));
    public static string BodyWeight_BmiCategory_Normal => Get(nameof(BodyWeight_BmiCategory_Normal));
    public static string BodyWeight_BmiCategory_Obese => Get(nameof(BodyWeight_BmiCategory_Obese));
    public static string BodyWeight_BmiCategory_Overweight => Get(nameof(BodyWeight_BmiCategory_Overweight));
    public static string BodyWeight_BmiCategory_Underweight => Get(nameof(BodyWeight_BmiCategory_Underweight));
    public static string BodyWeight_LogButton          => Get(nameof(BodyWeight_LogButton));
    public static string BodyWeight_RecentLogs         => Get(nameof(BodyWeight_RecentLogs));
    public static string BodyWeight_ShowMore           => Get(nameof(BodyWeight_ShowMore));
    public static string BodyWeight_Trend_Down         => Get(nameof(BodyWeight_Trend_Down));
    public static string BodyWeight_Trend_Stable       => Get(nameof(BodyWeight_Trend_Stable));
    public static string BodyWeight_Trend_Up           => Get(nameof(BodyWeight_Trend_Up));
    public static string BodyWeight_Prompt_Title       => Get(nameof(BodyWeight_Prompt_Title));
    public static string BodyWeight_Prompt_Body        => Get(nameof(BodyWeight_Prompt_Body));
    public static string BodyWeight_EntryPlaceholder   => Get(nameof(BodyWeight_EntryPlaceholder));
    public static string BodyWeight_DeleteBody_Format  => Get(nameof(BodyWeight_DeleteBody_Format));

    // ── CreateExercise ──────────────────────────────────────────────────
    public static string CreateExercise_Title              => Get(nameof(CreateExercise_Title));
    public static string CreateExercise_NameLabel          => Get(nameof(CreateExercise_NameLabel));
    public static string CreateExercise_NamePlaceholder    => Get(nameof(CreateExercise_NamePlaceholder));
    public static string CreateExercise_MuscleGroupLabel   => Get(nameof(CreateExercise_MuscleGroupLabel));
    public static string CreateExercise_EquipmentLabel     => Get(nameof(CreateExercise_EquipmentLabel));
    public static string CreateExercise_RestLabel          => Get(nameof(CreateExercise_RestLabel));
    public static string CreateExercise_RestUnit           => Get(nameof(CreateExercise_RestUnit));
    public static string CreateExercise_NotesLabel         => Get(nameof(CreateExercise_NotesLabel));
    public static string CreateExercise_NotesPlaceholder   => Get(nameof(CreateExercise_NotesPlaceholder));

    // ── ProgressPhotos ──────────────────────────────────────────────────
    public static string ProgressPhotos_Title              => Get(nameof(ProgressPhotos_Title));
    public static string ProgressPhotos_Empty_Title        => Get(nameof(ProgressPhotos_Empty_Title));
    public static string ProgressPhotos_Empty_Body         => Get(nameof(ProgressPhotos_Empty_Body));
    public static string ProgressPhotos_DeletePhotoTitle   => Get(nameof(ProgressPhotos_DeletePhotoTitle));
    public static string ProgressPhotos_DeletePhotoBody    => Get(nameof(ProgressPhotos_DeletePhotoBody));

    // ── Hem — Activity card labels ──────────────────────────────────────
    public static string Hem_Steps_Label       => Get(nameof(Hem_Steps_Label));
    public static string Hem_Calories_Label    => Get(nameof(Hem_Calories_Label));
    public static string Hem_ActiveTime_Label  => Get(nameof(Hem_ActiveTime_Label));
    public static string Hem_HeartRate_Label   => Get(nameof(Hem_HeartRate_Label));

    // ── Hem — Sleep stage labels ────────────────────────────────────────
    public static string Hem_Sleep_Core        => Get(nameof(Hem_Sleep_Core));
    public static string Hem_Sleep_Deep        => Get(nameof(Hem_Sleep_Deep));
    public static string Hem_Sleep_Rem         => Get(nameof(Hem_Sleep_Rem));

    // ── History ─────────────────────────────────────────────────────────
    public static string History_Card_PRs      => Get(nameof(History_Card_PRs));

    // ── Hem — weekday abbreviations ─────────────────────────────────────
    public static string Hem_Weekday_Mon       => Get(nameof(Hem_Weekday_Mon));
    public static string Hem_Weekday_Tue       => Get(nameof(Hem_Weekday_Tue));
    public static string Hem_Weekday_Wed       => Get(nameof(Hem_Weekday_Wed));
    public static string Hem_Weekday_Thu       => Get(nameof(Hem_Weekday_Thu));
    public static string Hem_Weekday_Fri       => Get(nameof(Hem_Weekday_Fri));
    public static string Hem_Weekday_Sat       => Get(nameof(Hem_Weekday_Sat));
    public static string Hem_Weekday_Sun       => Get(nameof(Hem_Weekday_Sun));

    // ── Library ─────────────────────────────────────────────────────────
    public static string Library_LoadingIn     => Get(nameof(Library_LoadingIn));

    // ── Navigation page titles ──────────────────────────────────────────
    public static string CreateExercise_PageTitle  => Get(nameof(CreateExercise_PageTitle));
    public static string ExercisePicker_PageTitle  => Get(nameof(ExercisePicker_PageTitle));

    // ── Achievements ──────────────────────────────────────────────────────
    public static string Achievement_FirstWorkout_Title            => Get(nameof(Achievement_FirstWorkout_Title));
    public static string Achievement_FirstWorkout_Description      => Get(nameof(Achievement_FirstWorkout_Description));
    public static string Achievement_Sessions5_Title               => Get(nameof(Achievement_Sessions5_Title));
    public static string Achievement_Sessions5_Description         => Get(nameof(Achievement_Sessions5_Description));
    public static string Achievement_Sessions10_Title              => Get(nameof(Achievement_Sessions10_Title));
    public static string Achievement_Sessions10_Description        => Get(nameof(Achievement_Sessions10_Description));
    public static string Achievement_Sessions25_Title              => Get(nameof(Achievement_Sessions25_Title));
    public static string Achievement_Sessions25_Description        => Get(nameof(Achievement_Sessions25_Description));
    public static string Achievement_Sessions50_Title              => Get(nameof(Achievement_Sessions50_Title));
    public static string Achievement_Sessions50_Description        => Get(nameof(Achievement_Sessions50_Description));
    public static string Achievement_Sessions100_Title             => Get(nameof(Achievement_Sessions100_Title));
    public static string Achievement_Sessions100_Description       => Get(nameof(Achievement_Sessions100_Description));
    public static string Achievement_WeekStreak1_Title             => Get(nameof(Achievement_WeekStreak1_Title));
    public static string Achievement_WeekStreak1_Description       => Get(nameof(Achievement_WeekStreak1_Description));
    public static string Achievement_WeekStreak4_Title             => Get(nameof(Achievement_WeekStreak4_Title));
    public static string Achievement_WeekStreak4_Description       => Get(nameof(Achievement_WeekStreak4_Description));
    public static string Achievement_WeekStreak12_Title            => Get(nameof(Achievement_WeekStreak12_Title));
    public static string Achievement_WeekStreak12_Description      => Get(nameof(Achievement_WeekStreak12_Description));
    public static string Achievement_FirstPR_Title                 => Get(nameof(Achievement_FirstPR_Title));
    public static string Achievement_FirstPR_Description           => Get(nameof(Achievement_FirstPR_Description));
    public static string Achievement_PR10_Title                    => Get(nameof(Achievement_PR10_Title));
    public static string Achievement_PR10_Description              => Get(nameof(Achievement_PR10_Description));
    public static string Achievement_PR50_Title                    => Get(nameof(Achievement_PR50_Title));
    public static string Achievement_PR50_Description              => Get(nameof(Achievement_PR50_Description));
    public static string Achievement_TotalVolume100k_Title         => Get(nameof(Achievement_TotalVolume100k_Title));
    public static string Achievement_TotalVolume100k_Description   => Get(nameof(Achievement_TotalVolume100k_Description));
    public static string Achievement_TotalVolume500k_Title         => Get(nameof(Achievement_TotalVolume500k_Title));
    public static string Achievement_TotalVolume500k_Description   => Get(nameof(Achievement_TotalVolume500k_Description));
    public static string Achievement_TotalVolume1M_Title           => Get(nameof(Achievement_TotalVolume1M_Title));
    public static string Achievement_TotalVolume1M_Description     => Get(nameof(Achievement_TotalVolume1M_Description));
    public static string Achievement_AllMuscleGroups_Title         => Get(nameof(Achievement_AllMuscleGroups_Title));
    public static string Achievement_AllMuscleGroups_Description   => Get(nameof(Achievement_AllMuscleGroups_Description));
    public static string Achievement_LongSession_Title             => Get(nameof(Achievement_LongSession_Title));
    public static string Achievement_LongSession_Description       => Get(nameof(Achievement_LongSession_Description));
    public static string Achievement_EarlyBird_Title               => Get(nameof(Achievement_EarlyBird_Title));
    public static string Achievement_EarlyBird_Description         => Get(nameof(Achievement_EarlyBird_Description));
    public static string Achievement_NightOwl_Title                => Get(nameof(Achievement_NightOwl_Title));
    public static string Achievement_NightOwl_Description          => Get(nameof(Achievement_NightOwl_Description));
    public static string Achievement_FirstCustomExercise_Title     => Get(nameof(Achievement_FirstCustomExercise_Title));
    public static string Achievement_FirstCustomExercise_Description => Get(nameof(Achievement_FirstCustomExercise_Description));

    // ── Programs ──────────────────────────────────────────────────────────
    public static string Program_ppl_Description            => Get(nameof(Program_ppl_Description));
    public static string Program_ppl_Day1_Label             => Get(nameof(Program_ppl_Day1_Label));
    public static string Program_ppl_Day2_Label             => Get(nameof(Program_ppl_Day2_Label));
    public static string Program_ppl_Day3_Label             => Get(nameof(Program_ppl_Day3_Label));
    public static string Program_ppl_Day4_Label             => Get(nameof(Program_ppl_Day4_Label));
    public static string Program_ppl_Day5_Label             => Get(nameof(Program_ppl_Day5_Label));
    public static string Program_ppl_Day6_Label             => Get(nameof(Program_ppl_Day6_Label));
    public static string Program_upperlower_Description     => Get(nameof(Program_upperlower_Description));
    public static string Program_upperlower_Day1_Label      => Get(nameof(Program_upperlower_Day1_Label));
    public static string Program_upperlower_Day2_Label      => Get(nameof(Program_upperlower_Day2_Label));
    public static string Program_upperlower_Day3_Label      => Get(nameof(Program_upperlower_Day3_Label));
    public static string Program_upperlower_Day4_Label      => Get(nameof(Program_upperlower_Day4_Label));
    public static string Program_fullbody_Description       => Get(nameof(Program_fullbody_Description));
    public static string Program_fullbody_Day1_Label        => Get(nameof(Program_fullbody_Day1_Label));
    public static string Program_fullbody_Day2_Label        => Get(nameof(Program_fullbody_Day2_Label));
    public static string Program_fullbody_Day3_Label        => Get(nameof(Program_fullbody_Day3_Label));
    public static string Program_startingstrength_Description => Get(nameof(Program_startingstrength_Description));
    public static string Program_startingstrength_Day1_Label  => Get(nameof(Program_startingstrength_Day1_Label));
    public static string Program_startingstrength_Day2_Label  => Get(nameof(Program_startingstrength_Day2_Label));
    public static string Program_texasmethod_Description    => Get(nameof(Program_texasmethod_Description));
    public static string Program_texasmethod_Day1_Label     => Get(nameof(Program_texasmethod_Day1_Label));
    public static string Program_texasmethod_Day2_Label     => Get(nameof(Program_texasmethod_Day2_Label));
    public static string Program_texasmethod_Day3_Label     => Get(nameof(Program_texasmethod_Day3_Label));
    public static string Program_531bbb_Description         => Get(nameof(Program_531bbb_Description));
    public static string Program_531bbb_Day1_Label          => Get(nameof(Program_531bbb_Day1_Label));
    public static string Program_531bbb_Day2_Label          => Get(nameof(Program_531bbb_Day2_Label));
    public static string Program_531bbb_Day3_Label          => Get(nameof(Program_531bbb_Day3_Label));
}
