# Design Spec: Ångra "avsluta pass"

**Datum:** 2026-06-28
**Feature-ID:** undo-finish-workout
**Prioritet:** M (Medium)

---

## Problem

När användaren råkar trycka "Avsluta" på ActiveWorkoutPage finns det ingen väg tillbaka. Passet sparas direkt och `ActiveWorkoutViewModel.ForceDeactivate()` raderar all state — det pågående passet är förlorat.

---

## Lösning (vald approach)

**Alternativ A — Defer-save:** Spara INTE `CompletedAt` till databasen vid navigering till PostWorkoutPage. Behåll `_session`-objektet och hela VM-state i `ActiveWorkoutViewModel` (Singleton) tills användaren aktivt bekräftar med "Klar"-knappen. Om användaren trycker "Ångra" på PostWorkoutPage navigeras de tillbaka till `ActiveWorkoutPage` — utan att någon ny DB-operation behövs.

### Varför Alternativ A (inte B)

- **Ingen rollback-logik** — vi behöver aldrig radera en redan commitad session.
- **State finns redan kvar** — `ActiveWorkoutViewModel` är Singleton och lever hela appens livstid. `Exercises`-kollektionen, timern och klockan är intakta tills `ForceDeactivate()` anropas.
- **Minimal förändring** — exakt en flytt: `_session.CompletedAt = DateTime.Now; await db.SaveSessionAsync(_session)` och `ApplyProgressionAsync()` flyttas från `FinishWorkoutAsync()` till `PostWorkoutViewModel.DoneAsync()`.
- **Inga race conditions** — inga parallella skrivningar; VM äger sessionen till "Klar" klickas.

---

## Nuvarande flöde (före ändring)

```
ActiveWorkoutPage → [Avsluta] → FinishWorkoutAsync()
  1. _clockCts.Cancel()             // stoppar klockan
  2. timer.Cancel()                  // stoppar vilotimer
  3. _session.CompletedAt = Now      // ← CompletedAt sätts
  4. db.SaveSessionAsync(_session)   // ← sparas i DB
  5. ApplyProgressionAsync()         // ← auto-progression skrivs
  6. _session = null                 // ← state rensas
  7. state.Deactivate()
  8. GoToAsync(PostWorkoutPage, SessionId)
PostWorkoutPage → LoadAsync(sessionId) → läser från DB
```

---

## Nytt flöde (efter ändring)

```
ActiveWorkoutPage → [Avsluta] → FinishWorkoutAsync()
  1. _clockCts.Cancel()             // stoppar klockan
  2. timer.Cancel()                  // stoppar vilotimer
  3. (CompletedAt sätts INTE här)    // ← borttaget
  4. state.Deactivate()              // döljer "PASS PÅGÅR"-banner
  5. GoToAsync(PostWorkoutPage, SessionId: _session.Id)
  (ActiveWorkoutViewModel._session lever kvar, Exercises lever kvar)

PostWorkoutPage:
  → [Ångra] → UndoFinishCommand
      1. state.Reactivate()           // återaktiverar "PASS PÅGÅR"-banner
      2. StartClock() återupptas      // klockan fortsätter
      3. GoToAsync("..")              // tillbaka till ActiveWorkoutPage
      (ingen DB-operation krävs — session har aldrig fått CompletedAt)

  → [Klar] / OnDisappearing (om användaren swipe-stänger)
      1. _session.CompletedAt = Now   // sätts nu
      2. db.SaveSessionAsync()        // sparas i DB
      3. ApplyProgressionAsync()      // auto-progression
      4. ForceDeactivate()            // rensar VM-state
      5. Navigation.PopToRootAsync()
```

---

## Komponentpåverkan

### `ActiveWorkoutViewModel.cs`

| Ändring | Detalj |
|---------|--------|
| `FinishWorkoutAsync()` | Ta bort `CompletedAt`-sättning, `db.SaveSessionAsync`, `ApplyProgressionAsync`, `_session = null`, `state.Deactivate()`. Lägg till `state.Deactivate()` *utan* att rensa `_session`. Skicka `_session.Id` som query-param. |
| `ResumeFromPostWorkout()` | Ny publik metod — återstartar klockan och anropar `state.Activate()`. Anropas av `UndoFinishCommand` i PostWorkoutViewModel. |
| `CommitFinishAsync(string notes)` | Ny publik metod — sätter `CompletedAt`, sparar, kör progression, nollar `_session`, anropar `ForceDeactivate()`. Anropas av `PostWorkoutViewModel.DoneAsync()`. |

### `PostWorkoutViewModel.cs`

| Ändring | Detalj |
|---------|--------|
| Injicera `ActiveWorkoutViewModel` | Via constructor DI (redan Singleton i DI-container). |
| `UndoFinishCommand` | Ny `[RelayCommand]` — anropar `ActiveWorkoutViewModel.ResumeFromPostWorkout()`, navigerar `GoToAsync("..")`. |
| `DoneAsync()` | Anropar `ActiveWorkoutViewModel.CommitFinishAsync(Notes)` innan navigation. |
| `PostWorkoutPage.OnDisappearing` | Triggar commit om sidan försvinner på annat sätt (swipe-back, systemnavigation). Guard: commit bara om session ännu inte är commitad. |

### `DatabaseService.cs`

Inga nya metoder — befintliga `SaveSessionAsync`, `DeleteSessionAsync` räcker. `DeleteSessionAsync` kan behöva läggas till om vi vill städa orphan-sessioner (inte nödvändigt för MVP).

### `PostWorkoutPage.xaml`

Ny diskret "Ångra"-länk längst ned på sidan, ovanför "Klar"-knappen.

---

## UI-design

```
[ PostWorkoutPage — botten ]

  ┌──────────────────────────────────────┐
  │  KLAR                               │  ← primärknapp (befintlig)
  └──────────────────────────────────────┘
  
     Ångra — återgå till passet           ← diskret TextButton, ForgeMuted-färg
```

- **"Ångra"-texten:** `ForgeMuted`-färg, `DMSansRegular`, 13pt, centrerad, understruken.
- Synlig alltid på PostWorkoutPage (inga timers, ingen auto-försvinnande).
- Ingen modal eller bekräftelsedialog — direkt action.

---

## i18n-strängar (AppResources)

| Nyckel | Svenska | Engelska |
|--------|---------|----------|
| `PostWorkout_UndoFinish` | `Ångra — återgå till passet` | `Undo — return to workout` |

---

## Edge cases

| Scenario | Hantering |
|----------|-----------|
| Användaren swipe-stänger PostWorkoutPage (iOS swipe-back) | `OnDisappearing` i code-behind committar sessionen om `_session != null && CompletedAt == null`. |
| Appen kraschar/termineras på PostWorkoutPage | Sessionen har inget `CompletedAt` — visas inte i Historik (filtreras bort som orphan via befintlig `CompletedAt != null`-check). Acceptabelt för MVP. |
| Användaren trycker Ångra, avslutar igen | Normalt flöde — `FinishWorkoutAsync()` körs igen. |
| Ångra när vilotimer var igång | `ResumeFromPostWorkout()` återstartar inte timern — timern var redan stoppad i `FinishWorkoutAsync()`. Timer återupptas inte, acceptabelt. |

---

## Beroenden

- `ActiveWorkoutStateService` — behöver `Reactivate()`-metod (eller `Activate()` som redan finns).
- Befintlig `state.Activate()` i `ActiveWorkoutStateService` kan återanvändas.

---

## Avgränsningar (MVP)

- Ingen countdown-timer på "Ångra"-knappen.
- Ångra finns tills användaren klickar "Klar" eller lämnar sidan.
- Vilotimern återupptas inte efter Ångra.
- Inga foton som tagits på PostWorkoutPage sparas vid Ångra (sessions-id är giltigt, fotona ligger kvar i DB kopplade till sessionen — de förblir kopplade om passet avslutas igen).
