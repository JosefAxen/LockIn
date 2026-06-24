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
    public static string ActiveWorkout_FinishConfirm_Title           => Get(nameof(ActiveWorkout_FinishConfirm_Title));
    public static string ActiveWorkout_FinishConfirm_Body            => Get(nameof(ActiveWorkout_FinishConfirm_Body));
    public static string ActiveWorkout_FinishConfirm_Yes             => Get(nameof(ActiveWorkout_FinishConfirm_Yes));

    // ── Library ─────────────────────────────────────────────────────────
    public static string Library_Title                      => Get(nameof(Library_Title));
    public static string Library_AddButton                  => Get(nameof(Library_AddButton));
    public static string Library_Tab_Exercises              => Get(nameof(Library_Tab_Exercises));
    public static string Library_Tab_Templates              => Get(nameof(Library_Tab_Templates));
    public static string Library_Tab_Programs               => Get(nameof(Library_Tab_Programs));
    public static string Library_SearchPlaceholder          => Get(nameof(Library_SearchPlaceholder));
    public static string Library_NoTemplates_Title          => Get(nameof(Library_NoTemplates_Title));
    public static string Library_NoTemplates_Body           => Get(nameof(Library_NoTemplates_Body));
    public static string Library_Badge_Custom               => Get(nameof(Library_Badge_Custom));
    public static string Library_ViewProgram                => Get(nameof(Library_ViewProgram));
    public static string Library_DaysPerWeek_Format         => Get(nameof(Library_DaysPerWeek_Format));
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
}
