# Bevel-stil för Bibliotek + ExercisePicker

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Få alla tre tabbar i Bibliotek (ÖVNINGAR, MALLAR, PROGRAM) och övningslistan i ExercisePicker att se mjuk och Bevel-lik ut: pill-formad sökruta, kort utan hård vänsterkantsbar och utan 1px-border.

**Architecture:** Två XAML-sidor får flera lokala byten: (1) MAUI default `SearchBar` (kantig) ersätts av en `Border`-wrappad `Entry` i pill-form, (2) exercise-cards går från `CardFrame` + 4dp vänsterbar till en mjuk `Border` med rundade hörn 18, ingen synlig kantlinje, och en liten färgad cirkel (10x10) som muskelindikator istället för bar, (3) muskelchip-raden får mjukare radie, (4) MALLAR-korten får samma mjuka behandling utan vänsterbar (play-knappens orange färg + delete-knapp behålls), (5) PROGRAM-korten får mjukare Border (har redan ingen vänsterbar). Ingen ny C#-kod — bara XAML.

**Tech Stack:** .NET MAUI 10, XAML, befintliga Forge-tokens (`ForgeSurface`, `ForgeSurface2`, `ForgeText`, `ForgeMuted`, `ForgeAccentOrange`, `ForgeAccentOrangeDim`, `ForgeAccentAmber`, `ForgeAccentAmberDim`).

---

## Filöversikt

**Modifieras:**
- `LockIn/Views/LibraryPage.xaml` — sökruta + muskelchip + övningskort + mallkort + programkort
- `LockIn/Views/ExercisePickerPage.xaml` — sökruta + muskelchip + custom-rad + övningskort
- `LockIn/LockIn.csproj` — ApplicationVersion 5 → 6

**Skapas:** inga nya filer

**Verifiering:** ingen test-suite finns i projektet (MAUI XAML). Verifiering = `dotnet build` + visuell inspektion i iOS-simulator.

---

## Task 1: LibraryPage — Pill-formad sökruta

**Files:**
- Modify: `LockIn/Views/LibraryPage.xaml:129-133`

- [ ] **Step 1: Ersätt SearchBar med Border + Entry**

Hitta blocket på rad 129–133:
```xml
<!-- Search bar (exercises only) -->
<SearchBar Grid.Row="2"
           IsVisible="{Binding ShowExercises}"
           Text="{Binding SearchText}"
           Placeholder="Sök övning..."
           Margin="16,0,16,8"/>
```

Ersätt med:
```xml
<!-- Search bar (exercises only) — pill, Bevel-stil -->
<Border Grid.Row="2"
        IsVisible="{Binding ShowExercises}"
        BackgroundColor="{AppThemeBinding Light={StaticResource LightSurface}, Dark={StaticResource ForgeSurface2}}"
        StrokeShape="RoundRectangle 26"
        StrokeThickness="0"
        Padding="18,0"
        HeightRequest="46"
        Margin="16,0,16,10">
    <Grid ColumnDefinitions="Auto,*" ColumnSpacing="10">
        <Label Grid.Column="0"
               Text="⌕"
               FontSize="20"
               TextColor="{AppThemeBinding Light={StaticResource LightMuted}, Dark={StaticResource ForgeMuted}}"
               VerticalOptions="Center"/>
        <Entry Grid.Column="1"
               Text="{Binding SearchText}"
               Placeholder="Sök övning..."
               PlaceholderColor="{AppThemeBinding Light={StaticResource LightMuted}, Dark={StaticResource ForgeMuted}}"
               BackgroundColor="Transparent"
               TextColor="{AppThemeBinding Light={StaticResource LightText}, Dark={StaticResource ForgeText}}"
               FontFamily="DMSansRegular" FontSize="15"
               VerticalOptions="Center"/>
    </Grid>
</Border>
```

- [ ] **Step 2: Verifiera build**

Run: `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | grep -E "error|Error\(s\)" | grep -v warning`

Expected: `    0 Error(s)`

- [ ] **Step 3: Commit**

```bash
git add LockIn/Views/LibraryPage.xaml
git commit -m "refactor(library): pill-formad sökruta i Bevel-stil"
```

---

## Task 2: LibraryPage — Pill-formade muskelchips

**Files:**
- Modify: `LockIn/Views/LibraryPage.xaml:147-163`

- [ ] **Step 1: Ändra muskelchip-Border till pill-form**

Hitta `DataTemplate x:DataType="vm:MuscleGroupChip"`. Inuti finns en `<Border ...>` med `StrokeShape="RoundRectangle 10"`.

Ersätt hela `<Border ...>...</Border>`-blocket med:
```xml
<Border BackgroundColor="{Binding Background}"
        StrokeShape="RoundRectangle 22" StrokeThickness="0"
        Padding="14,0"
        HeightRequest="34">
    <Label Text="{Binding Label}"
           TextColor="{Binding Foreground}"
           FontFamily="BebasNeue" FontSize="13" CharacterSpacing="1"
           VerticalOptions="Center"/>
    <Border.GestureRecognizers>
        <TapGestureRecognizer
            Command="{Binding Source={RelativeSource AncestorType={x:Type vm:LibraryViewModel}}, Path=SelectMuscleChipCommand}"
            CommandParameter="{Binding .}"/>
    </Border.GestureRecognizers>
</Border>
```

(Förändringar: `RoundRectangle 10` → `22`, fast `HeightRequest="34"`, lite mer horisontell padding för pill-känsla.)

- [ ] **Step 2: Verifiera build**

Run: `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | grep -E "error|Error\(s\)" | grep -v warning`

Expected: `    0 Error(s)`

- [ ] **Step 3: Commit**

```bash
git add LockIn/Views/LibraryPage.xaml
git commit -m "refactor(library): pill-formade muskelchips"
```

---

## Task 3: LibraryPage — Mjuka övningskort (ÖVNINGAR-tab)

**Files:**
- Modify: `LockIn/Views/LibraryPage.xaml:187-223`

- [ ] **Step 1: Byt ut DataTemplate för exercise-card**

Hitta blocket som börjar med `<CollectionView.ItemTemplate>` (för ÖVNINGAR-tabbens CollectionView som har `IsVisible="{Binding ShowExercises}"`) och innehåller `<DataTemplate x:DataType="models:Exercise">`.

Ersätt hela `<DataTemplate x:DataType="models:Exercise">...</DataTemplate>`-blocket med:
```xml
<DataTemplate x:DataType="models:Exercise">
    <Border Margin="16,0,16,8"
            BackgroundColor="{AppThemeBinding Light={StaticResource LightSurface}, Dark={StaticResource ForgeSurface}}"
            StrokeShape="RoundRectangle 18"
            StrokeThickness="0"
            Padding="18,14">
        <Border.Shadow>
            <Shadow Brush="Black" Offset="0,2" Radius="10" Opacity="0.18"/>
        </Border.Shadow>
        <Border.GestureRecognizers>
            <TapGestureRecognizer
                Command="{Binding Source={RelativeSource AncestorType={x:Type vm:LibraryViewModel}}, Path=OpenExerciseProgressCommand}"
                CommandParameter="{Binding .}"/>
        </Border.GestureRecognizers>
        <Grid ColumnDefinitions="12,*,Auto" ColumnSpacing="12">
            <Ellipse Grid.Column="0"
                     WidthRequest="10" HeightRequest="10"
                     Fill="{Binding MuscleGroup, Converter={StaticResource MuscleGroupColorConverter}}"
                     VerticalOptions="Center"/>
            <VerticalStackLayout Grid.Column="1" Spacing="3" VerticalOptions="Center">
                <Label Text="{Binding Name}"
                       FontFamily="DMSansMedium" FontSize="15"/>
                <Label Text="{Binding MuscleGroup, Converter={StaticResource MuscleGroupLabelConverter}}"
                       FontSize="12"
                       Opacity="0.85"
                       TextColor="{Binding MuscleGroup, Converter={StaticResource MuscleGroupColorConverter}}"/>
            </VerticalStackLayout>
            <Border Grid.Column="2"
                    IsVisible="{Binding IsCustom}"
                    BackgroundColor="{StaticResource ForgeAccentOrangeDim}"
                    StrokeShape="RoundRectangle 8"
                    StrokeThickness="0" Padding="8,4"
                    VerticalOptions="Center">
                <Label Text="EGNA" TextColor="{StaticResource ForgeAccentOrange}"
                       FontSize="10" FontFamily="BebasNeue"
                       CharacterSpacing="1"/>
            </Border>
        </Grid>
    </Border>
</DataTemplate>
```

- [ ] **Step 2: Verifiera build**

Run: `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | grep -E "error|Error\(s\)" | grep -v warning`

Expected: `    0 Error(s)`

- [ ] **Step 3: Commit**

```bash
git add LockIn/Views/LibraryPage.xaml
git commit -m "refactor(library): mjuka övningskort utan vänsterbar"
```

---

## Task 4: LibraryPage — Mjuka mallkort (MALLAR-tab)

**Files:**
- Modify: `LockIn/Views/LibraryPage.xaml:237-285`

- [ ] **Step 1: Byt ut mallkortets DataTemplate**

Hitta `<!-- Tab 1: Templates -->` och därefter `<DataTemplate x:DataType="models:WorkoutTemplate">`. Ersätt hela `<DataTemplate>...</DataTemplate>`-blocket med:
```xml
<DataTemplate x:DataType="models:WorkoutTemplate">
    <Border Margin="16,0,16,10"
            BackgroundColor="{AppThemeBinding Light={StaticResource LightSurface}, Dark={StaticResource ForgeSurface}}"
            StrokeShape="RoundRectangle 18"
            StrokeThickness="0"
            Padding="18,14,8,14">
        <Border.Shadow>
            <Shadow Brush="Black" Offset="0,2" Radius="10" Opacity="0.18"/>
        </Border.Shadow>
        <Border.GestureRecognizers>
            <TapGestureRecognizer
                Command="{Binding Source={RelativeSource AncestorType={x:Type vm:LibraryViewModel}}, Path=EditTemplateCommand}"
                CommandParameter="{Binding .}"/>
        </Border.GestureRecognizers>
        <Grid ColumnDefinitions="12,*,44,40" ColumnSpacing="10">
            <Ellipse Grid.Column="0"
                     WidthRequest="10" HeightRequest="10"
                     Fill="{StaticResource ForgeAccentOrange}"
                     VerticalOptions="Center"/>
            <StackLayout Grid.Column="1" Spacing="2" VerticalOptions="Center">
                <Label Text="{Binding Name}"
                       FontFamily="BebasNeue" FontSize="20" CharacterSpacing="1"/>
                <Label Text="TRYCK FÖR ATT REDIGERA"
                       Style="{StaticResource SectionLabel}" FontSize="9"/>
            </StackLayout>
            <Border Grid.Column="2"
                    WidthRequest="36" HeightRequest="36"
                    BackgroundColor="{StaticResource ForgeAccentOrange}"
                    StrokeShape="Ellipse" StrokeThickness="0"
                    VerticalOptions="Center">
                <views:AppIcon Source="ic_play.png"
                               WidthRequest="18" HeightRequest="18"
                               ForceTint="White"
                               HorizontalOptions="Center"
                               VerticalOptions="Center"/>
                <Border.GestureRecognizers>
                    <TapGestureRecognizer
                        Command="{Binding Source={RelativeSource AncestorType={x:Type vm:LibraryViewModel}}, Path=StartFromTemplateCommand}"
                        CommandParameter="{Binding .}"/>
                </Border.GestureRecognizers>
            </Border>
            <Button Grid.Column="3"
                    Text="✕"
                    BackgroundColor="Transparent"
                    TextColor="{StaticResource ForgeMuted}"
                    FontSize="15" HeightRequest="36"
                    Padding="0"
                    Command="{Binding Source={RelativeSource AncestorType={x:Type vm:LibraryViewModel}}, Path=DeleteTemplateCommand}"
                    CommandParameter="{Binding .}"/>
        </Grid>
    </Border>
</DataTemplate>
```

(Förändringar: tar bort `Style="{StaticResource CardFrame}"` + 4dp vänster-`BoxView`, ersätter med `Ellipse` 10x10 orange + ny mjuk Border med radius 18 och egen skugga. Play-knapp + delete-knapp behålls oförändrade.)

- [ ] **Step 2: Verifiera build**

Run: `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | grep -E "error|Error\(s\)" | grep -v warning`

Expected: `    0 Error(s)`

- [ ] **Step 3: Commit**

```bash
git add LockIn/Views/LibraryPage.xaml
git commit -m "refactor(library): mjuka mallkort utan vänsterbar"
```

---

## Task 5: LibraryPage — Mjuka programkort (PROGRAM-tab)

**Files:**
- Modify: `LockIn/Views/LibraryPage.xaml:306-336`

- [ ] **Step 1: Byt ut programkortets DataTemplate**

Hitta `<!-- Tab 2: Programs -->` och därefter `<DataTemplate x:DataType="data:WorkoutProgram">`. Ersätt hela `<DataTemplate>...</DataTemplate>`-blocket med:
```xml
<DataTemplate x:DataType="data:WorkoutProgram">
    <Border BackgroundColor="{AppThemeBinding Light={StaticResource LightSurface}, Dark={StaticResource ForgeSurface}}"
            StrokeShape="RoundRectangle 18"
            StrokeThickness="0"
            Padding="18,14">
        <Border.Shadow>
            <Shadow Brush="Black" Offset="0,2" Radius="10" Opacity="0.18"/>
        </Border.Shadow>
        <Border.GestureRecognizers>
            <TapGestureRecognizer
                Command="{Binding Source={RelativeSource AncestorType={x:Type vm:LibraryViewModel}}, Path=OpenProgramCommand}"
                CommandParameter="{Binding .}"/>
        </Border.GestureRecognizers>
        <StackLayout Spacing="8">
            <Grid ColumnDefinitions="*,Auto">
                <Label Grid.Column="0"
                       Text="{Binding Name}"
                       FontFamily="BebasNeue" FontSize="20"
                       CharacterSpacing="1"/>
                <Border Grid.Column="1"
                        BackgroundColor="{StaticResource ForgeAccentAmberDim}"
                        StrokeShape="RoundRectangle 10"
                        StrokeThickness="0" Padding="10,5">
                    <Label Text="{Binding DaysPerWeek, StringFormat='{0} dgr/v'}"
                           TextColor="{StaticResource ForgeAccentAmber}"
                           FontFamily="DMSansMedium" FontSize="12"/>
                </Border>
            </Grid>
            <Label Text="{Binding Description}"
                   Style="{StaticResource MutedLabel}"
                   LineBreakMode="WordWrap"/>
            <Label Text="SE PROGRAM →"
                   TextColor="{StaticResource ForgeAccentAmber}"
                   FontFamily="BebasNeue" FontSize="13"
                   CharacterSpacing="2"/>
        </StackLayout>
    </Border>
</DataTemplate>
```

(Förändringar: tar bort `Style="{StaticResource CardFrame}"` som hade 1px-border + 16-radius. Inline Border har nu 18-radius, ingen border, egen mjuk skugga. Inre `StackLayout`-padding flyttas till `Border.Padding="18,14"`.)

- [ ] **Step 2: Verifiera build**

Run: `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | grep -E "error|Error\(s\)" | grep -v warning`

Expected: `    0 Error(s)`

- [ ] **Step 3: Commit**

```bash
git add LockIn/Views/LibraryPage.xaml
git commit -m "refactor(library): mjukare programkort"
```

---

## Task 6: ExercisePickerPage — Pill-formad sökruta

**Files:**
- Modify: `LockIn/Views/ExercisePickerPage.xaml:41-44`

- [ ] **Step 1: Ersätt SearchBar med Border + Entry**

Hitta blocket på rad 41–44:
```xml
<SearchBar Grid.Row="1"
           Text="{Binding SearchText}"
           Placeholder="Sök övning..."
           Margin="16,0,16,8"/>
```

Ersätt med:
```xml
<!-- Pill-formad sökruta (Bevel-stil) -->
<Border Grid.Row="1"
        BackgroundColor="{StaticResource ForgeSurface2}"
        StrokeShape="RoundRectangle 26"
        StrokeThickness="0"
        Padding="18,0"
        HeightRequest="46"
        Margin="16,0,16,10">
    <Grid ColumnDefinitions="Auto,*" ColumnSpacing="10">
        <Label Grid.Column="0"
               Text="⌕"
               FontSize="20"
               TextColor="{StaticResource ForgeMuted}"
               VerticalOptions="Center"/>
        <Entry Grid.Column="1"
               Text="{Binding SearchText}"
               Placeholder="Sök övning..."
               PlaceholderColor="{StaticResource ForgeMuted}"
               BackgroundColor="Transparent"
               TextColor="{StaticResource ForgeText}"
               FontFamily="DMSansRegular" FontSize="15"
               VerticalOptions="Center"/>
    </Grid>
</Border>
```

- [ ] **Step 2: Verifiera build**

Run: `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | grep -E "error|Error\(s\)" | grep -v warning`

Expected: `    0 Error(s)`

- [ ] **Step 3: Commit**

```bash
git add LockIn/Views/ExercisePickerPage.xaml
git commit -m "refactor(picker): pill-formad sökruta i Bevel-stil"
```

---

## Task 7: ExercisePickerPage — Pill-formade muskelchips + mjuk custom-rad

**Files:**
- Modify: `LockIn/Views/ExercisePickerPage.xaml:57-73` (muskelchip)
- Modify: `LockIn/Views/ExercisePickerPage.xaml:77-99` (custom-rad)

- [ ] **Step 1: Pill-form på muskelchips**

Hitta `DataTemplate x:DataType="vm:MuscleGroupChip"`. Inuti finns en `<Border ...>` med `StrokeShape="RoundRectangle 10"`.

Ersätt hela `<Border ...>...</Border>`-blocket med:
```xml
<Border BackgroundColor="{Binding Background}"
        StrokeShape="RoundRectangle 22" StrokeThickness="0"
        Padding="14,0"
        HeightRequest="34">
    <Label Text="{Binding Label}"
           TextColor="{Binding Foreground}"
           FontFamily="BebasNeue" FontSize="13" CharacterSpacing="1"
           VerticalOptions="Center"/>
    <Border.GestureRecognizers>
        <TapGestureRecognizer
            Command="{Binding Source={RelativeSource AncestorType={x:Type vm:ExercisePickerViewModel}}, Path=SelectChipCommand}"
            CommandParameter="{Binding .}"/>
    </Border.GestureRecognizers>
</Border>
```

- [ ] **Step 2: Mjuka "Lägg till custom"-raden (neutral, inte CTA)**

Hitta `<!-- Add custom exercise row -->`-blocket. Ersätt hela `<Border Grid.Row="3" ...>...</Border>` med:
```xml
<!-- Add custom exercise row -->
<Border Grid.Row="3"
        Margin="16,0,16,8"
        StrokeShape="RoundRectangle 18"
        BackgroundColor="{AppThemeBinding Light={StaticResource LightSurface}, Dark={StaticResource ForgeSurface}}"
        StrokeThickness="0"
        Padding="18,14">
    <Border.Shadow>
        <Shadow Brush="Black" Offset="0,2" Radius="10" Opacity="0.18"/>
    </Border.Shadow>
    <Grid ColumnDefinitions="24,*" ColumnSpacing="10">
        <Label Grid.Column="0"
               Text="+"
               FontSize="22" FontAttributes="Bold"
               TextColor="{StaticResource ForgeAccentOrange}"
               VerticalOptions="Center" HorizontalOptions="Center"/>
        <Label Grid.Column="1"
               Text="Lägg till custom övning"
               FontFamily="DMSansMedium" FontSize="15"
               TextColor="{AppThemeBinding Light={StaticResource LightText}, Dark={StaticResource ForgeText}}"
               VerticalOptions="Center"/>
    </Grid>
    <Border.GestureRecognizers>
        <TapGestureRecognizer Tapped="OnAddCustomExerciseTapped"/>
    </Border.GestureRecognizers>
</Border>
```

(Förändringar: tar bort 1px border, ökar radius från 14 till 18, lägger till mjuk skugga. Bakgrund förblir neutral `ForgeSurface` så raden inte blir för pushig.)

- [ ] **Step 3: Verifiera build**

Run: `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | grep -E "error|Error\(s\)" | grep -v warning`

Expected: `    0 Error(s)`

- [ ] **Step 4: Commit**

```bash
git add LockIn/Views/ExercisePickerPage.xaml
git commit -m "refactor(picker): pill-chips + mjuk custom-rad"
```

---

## Task 8: ExercisePickerPage — Mjuka övningskort

**Files:**
- Modify: `LockIn/Views/ExercisePickerPage.xaml:106-140`

- [ ] **Step 1: Byt ut DataTemplate för picker-cards**

Hitta blocket `<DataTemplate x:DataType="vm:ExercisePickerRow">...</DataTemplate>`.

Ersätt hela blocket med:
```xml
<DataTemplate x:DataType="vm:ExercisePickerRow">
    <Border Margin="16,0,16,8"
            BackgroundColor="{AppThemeBinding Light={StaticResource LightSurface}, Dark={StaticResource ForgeSurface}}"
            StrokeShape="RoundRectangle 18"
            StrokeThickness="0"
            Padding="18,14">
        <Border.Shadow>
            <Shadow Brush="Black" Offset="0,2" Radius="10" Opacity="0.18"/>
        </Border.Shadow>
        <Border.GestureRecognizers>
            <TapGestureRecognizer
                Command="{Binding Source={RelativeSource AncestorType={x:Type vm:ExercisePickerViewModel}}, Path=SelectExerciseCommand}"
                CommandParameter="{Binding .}"/>
            <PointerGestureRecognizer
                PointerPressed="OnExercisePointerPressed"
                PointerReleased="OnExercisePointerReleased"/>
        </Border.GestureRecognizers>
        <Grid ColumnDefinitions="12,*,Auto" ColumnSpacing="12">
            <Ellipse Grid.Column="0"
                     WidthRequest="10" HeightRequest="10"
                     Fill="{Binding MuscleColor}"
                     VerticalOptions="Center"/>
            <VerticalStackLayout Grid.Column="1" Spacing="3" VerticalOptions="Center">
                <Label Text="{Binding Name}"
                       FontFamily="DMSansMedium" FontSize="15"/>
                <Label Text="{Binding MuscleLabel}"
                       FontSize="12"
                       Opacity="0.85"
                       TextColor="{Binding MuscleColor}"/>
            </VerticalStackLayout>
            <Border Grid.Column="2"
                    IsVisible="{Binding IsCustom}"
                    BackgroundColor="{StaticResource ForgeAccentOrangeDim}"
                    StrokeShape="RoundRectangle 8"
                    StrokeThickness="0" Padding="8,4"
                    VerticalOptions="Center">
                <Label Text="CUSTOM" TextColor="{StaticResource ForgeAccentOrange}"
                       FontSize="10" FontFamily="BebasNeue" CharacterSpacing="1"/>
            </Border>
        </Grid>
    </Border>
</DataTemplate>
```

- [ ] **Step 2: Verifiera build**

Run: `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | grep -E "error|Error\(s\)" | grep -v warning`

Expected: `    0 Error(s)`

- [ ] **Step 3: Commit**

```bash
git add LockIn/Views/ExercisePickerPage.xaml
git commit -m "refactor(picker): mjuka övningskort utan vänsterbar"
```

---

## Task 9: Version bump + push

**Files:**
- Modify: `LockIn/LockIn.csproj:34`

- [ ] **Step 1: Bumpa ApplicationVersion 5 → 6**

Hitta rad 34:
```xml
<ApplicationVersion>5</ApplicationVersion>
```

Ändra till:
```xml
<ApplicationVersion>6</ApplicationVersion>
```

- [ ] **Step 2: Verifiera full build**

Run: `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | grep -E "error|Error\(s\)" | grep -v warning`

Expected: `    0 Error(s)`

- [ ] **Step 3: Commit version bump**

```bash
git add LockIn/LockIn.csproj
git commit -m "chore: bumpa ApplicationVersion till 6"
```

- [ ] **Step 4: Push**

```bash
git push origin master
```

Expected: push lyckas, GitHub Actions triggar TestFlight-bygge.

---

## Spec coverage

| Användarens krav | Task |
|------------------|------|
| Cleaner look på Bibliotek (övningslistan) | Task 1, 2, 3 |
| Cleaner look på MALLAR-tabben | Task 4 |
| Cleaner look på PROGRAM-tabben | Task 5 |
| Cleaner look på "Lägg till övning" i pågående pass (ExercisePicker) | Task 6, 7, 8 |
| Inga hårda kanter | Task 3, 4, 5, 7, 8 (tar bort 1px border + 4dp vänsterbar) |
| Sökrutans card är kantigt | Task 1, 6 (ersätter MAUI default SearchBar med pill-formad Border + Entry) |
| Liknar framsidan (HemPage Bevel-look) | Alla tasks (samma radius/spacing/shadow-mönster) |
| Färgad prick som muskelindikator | Task 3, 4, 8 |
| "Lägg till custom"-rad behåller neutral bakgrund | Task 7 |
| Version bump + push | Task 9 |
