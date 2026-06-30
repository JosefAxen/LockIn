# Renaissance Periodization — Hypertrofi-metoden: Research-rapport

> **Syfte:** Informera framtida designbeslut för Vana Strength, en svensk .NET MAUI iOS-träningsapp som vill implementera utvalda delar av RP:s autoreglerings-metodik utan att bygga en full mesocykelmotor.
>
> **Datum:** 2026-06-30  
> **Källstatus:** Se avsnitt "Källkaveat och begränsningar" sist i dokumentet.

---

## 1. Mesocykelstruktur

### Övergripande paradigm

RP:s hypertrofimodell är **inte** den klassiska tredelade periodiseringsstrukturen (Accumulation → Intensification → Realization) som används i powerlifting-peaking. RP:s hypertrofimesocykel är en **tvåfas-struktur:**

```
Accumulation (vecka 1 → vecka N)  →  Deload (1 mikrocykel)
```

Under Accumulation-fasen startar lyftaren vid MEV och adderar sets progressivt mot MRV. När volymtoleransen är uttömd (data-driven eller kalenderbaserad) följer en deload-vecka med sänkt volym, intensitet och relativ ansträngning.

### Fasernas parametrar

| Parameter | Accumulation | Deload |
|-----------|-------------|--------|
| Volym | MEV → mot MRV (ökar varje vecka) | ~50 % av peak-volym |
| Intensitet (vikt) | Stiger vecka-för-vecka | Sänkt (~10-20 %) |
| RIR-mål | 3–4 → 0–1 (trappas ned) | 4–5+ (aldrig failure) |
| Längd | 4–8 veckor typiskt (varierar) | 1 vecka |

**Källa:** [rpstrength.com/blogs/articles/progressing-for-hypertrophy](https://rpstrength.com/blogs/articles/progressing-for-hypertrophy)

### Fasernas längd

RP anger inte en universell längd. Accumulation-fasen varar tills lyftaren antingen:
1. Når kalenderbaserad gräns (vanligtvis 4–8 veckor), eller
2. Uppvisar data-drivna tecken på underåterhämtning (se avsnitt 5)

Kortare mesocyklar (3–4 veckor) passar nybörjare och efter deload. Längre (8–12 veckor) passar avancerade lyftare med hög träningsfrekvens.

---

## 2. Volymlandmärken — MEV / MAV / MRV

### Definitioner

RP definierar fyra volymlandmärken per muskelgrupp, alla uttryckta i **sets per vecka:**

| Landmärke | Engelsk term | Definition |
|-----------|-------------|-----------|
| **MV** | Maintenance Volume | ~6 sets/vecka — bibehåller muskelmassa utan tillväxt |
| **MEV** | Minimum Effective Volume | Lägsta dos som ger mätbar hypertrofi |
| **MAV** | Maximum Adaptive Volume | Volymen där snabbast hypertrofi sker (progression-zonen) |
| **MRV** | Maximum Recoverable Volume | Övre gräns — över denna fallerar återhämtningen |

**Källa:** [rpstrength.com/blogs/articles/training-volume-landmarks-muscle-growth](https://rpstrength.com/blogs/articles/training-volume-landmarks-muscle-growth)

### Viktigt: Värdena är individuella, inte universella

RP:s definition av MEV är operationellt subjektiv: *"MEV is likely around the volume at which you get some pump, some soreness, and some perception of decent performance."* Specifika sets/vecka-tal per muskelgrupp (t.ex. "bröst MEV=8, MRV=22") publiceras av RP som riktlinjervärden och **inte** som universella tal.

Illustrativa exempelintervall (från RP:s material, inte fasta värden):

| Muskelgrupp | MEV (ca) | MRV (ca) |
|-------------|---------|---------|
| Bröst | 8–12 sets/v | 18–22 sets/v |
| Rygg | 10–14 sets/v | 20–25 sets/v |
| Axlar | 8–12 sets/v | 18–22 sets/v |
| Biceps | 6–10 sets/v | 14–20 sets/v |
| Triceps | 6–10 sets/v | 14–20 sets/v |
| Quads | 8–12 sets/v | 18–22 sets/v |
| Hamstrings | 6–10 sets/v | 16–20 sets/v |
| Vader | 6–10 sets/v | 14–20 sets/v |
| Mage/Core | 6–10 sets/v | 16–20 sets/v |

> ⚠️ Dessa tal är illustrativa. Per-muskel-tabeller i exakt form kräver verifiering mot RP:s primärkällor (boken eller RP+ Pro-innehåll).

### Progression från MEV mot MRV

Lyftaren börjar vecka 1 vid MEV. Varje vecka adderas sets villkorligt (se avsnitt 7) tills MRV är uppnått eller recovery-markörer indikerar stopp. Mesocykeln startar sedan om från MEV (eller lägre) efter deload.

---

## 3. RIR/RPE-systemet

### Definitioner

**RIR (Reps In Reserve):** Antalet reps kvar till muskelsvikt vid avslutat set.

- RIR 0 = failure (inga reps kvar)
- RIR 1 = kunde gjort 1 rep till
- RIR 3 = kunde gjort 3 reps till

**RPE (Rate of Perceived Exertion):** En 1–10-skala där RPE ≈ 10 − RIR.

- RPE 10 = failure
- RPE 9 = RIR 1
- RPE 7 = RIR 3

**Källa:** [rpstrength.com/blogs/articles/hypertrophy-training-for-the-drug-tested-athlete](https://rpstrength.com/blogs/articles/hypertrophy-training-for-the-drug-tested-athlete)

### RIR-mål genom mesocykeln

RIR-målet trappas ned progressivt:

| Mesocykelvecka | Typiskt RIR-mål |
|---------------|----------------|
| Vecka 1 | 3–4 RIR |
| Vecka 2 | 2–3 RIR |
| Vecka 3 | 1–2 RIR |
| Sista veckan (före deload) | 0–1 RIR |
| Deload | 4–5+ RIR |

Logiken: Progressivt ökande ansträngning kombinerat med ökande volym ger maximal adaptiv stimulus precis innan kroppen behöver återhämta sig.

### RIR som autoreglerings-mekanism

Lyftaren loggar RIR för varje set. Om faktisk RIR är lägre än target (lyftaren är tröttare än väntat) signalerar det potentiellt underåterhämtning. Om faktisk RIR konsekvent är högre än target kan vikten ökas.

---

## 4. Vecka-till-vecka progression

### Dual-progression

RP:s hypertrofimodell bygger på **simultан progression** i två dimensioner:

1. **Volymprogression:** Fler sets per muskelgrupp varje vecka (villkorat av recovery-markörer)
2. **Intensitetsprogression:** Tyngre vikt när reps-mål nås konsekvent

### Vikt-progressionsalgoritm

RP:s grundregel för viktökning (från "Progressing for Hypertrophy"):

> *"If you hit your rep target on all sets with at least 1 RIR to spare, you should consider adding weight next session."*

Konkret implementation:
- Nådde alla reps + RIR ≥ 1: **Öka vikt** (vanligen 2.5–5 kg compound, 1.25–2.5 kg isolation)
- Nådde inte alla reps (failure på sista setet): **Behåll vikt**, möjligen minska sets
- RIR konsekvent lägre än mål: **Minska vikt** eller sets

### Set-progressionsalgoritm (villkorad)

Set-tillägg per vecka baseras på subjektiva recovery-markörer (se avsnitt 7). Det finns **ingen fast "+1 set/vecka"-regel** — tillägg är villkorade på hur lyftaren återhämtat sig.

---

## 5. Deload-trigger

### Kalenderbaserad vs. data-driven

RP använder i praktiken en **kombination:**

- **Kalenderbaserad:** Lyftaren planerar in deload efter X veckor (vanligen 4–8) som standardplan
- **Data-driven:** Lyftaren kan deloada tidigare om recovery-markörer konstant är negativa

**Signaler om tidig deload behövs:**
- Soreness-betyg konsekvent 3–4 (hög soreness)
- Performance-betyg konsekvent 3–4 (prestation faller)
- RIR faktiskt lägre än target vecka efter vecka
- Joint pain (ledstelhet) ökar

**Källa:** Implicit i RP:s set-progressionssystem — "performance=4 → initiate deload"

### Deload-format

- Volym: ~50 % av peak-volymvecka
- Intensitet (vikt): ~10–20 % lägre än peak
- RIR-mål: 4–5+ (aldrig nära failure)
- Längd: Typiskt 1 vecka (7 dagar)

> ⚠️ Exakt deload-format varierar — "50 % volym i 7 dagar" är en approximation, inte en garanterad RP-regel.

---

## 6. RP Hypertrophy-appens funktionalitet

### Inputs per set/pass

Användaren matar in efter varje pass (primärkälla: rpstrength.com/pages/hypertrophy-app):

| Input | Skala/format |
|-------|-------------|
| **Vikt** | kg/lbs |
| **Reps** | heltal |
| **RIR** | 0–5 (eller 0–4) |
| **Pump** | 1–4 subjektiv skala |
| **Soreness** | 1–4 (muskelömhet inför passet) |
| **Workload/Performance** | 1–4 subjektiv prestationsskala |

> ⚠️ Exakt antal fält är osäkert — primärkällan listar pump+soreness+workload/performance. "Joint pain" förekommer i sekundärkällor men är inte konfirmerat som eget fält i primärkällan.

### Vad appen beräknar och presenterar

- **Prescribes next session:** Vikt och reps för nästa pass baserat på inmatad data
- **Meso Builder:** Genererar hela mesocykeln — volymprogressionsplan vecka-för-vecka per muskelgrupp
- **Set-rekommendation:** Justerar nästa veckas set-antal baserat på recovery-markörer
- **Auto-regulation feedback loop:** Appen vägleder lyftaren att stanna i MAV-zonen

### Prisnivåer (2026)

| Tier | Pris | Innehåll |
|------|------|----------|
| Gratis | $0 | Grundläggande loggning, begränsat Meso Builder |
| RP+ Pro | ~$199 USD/år | Full Meso Builder, auto-reglering, alla muskelgrupper |
| RP Coach | ~$250–400/månad (SEK-ekvivalent) | Personlig coach, individualiserad programmering |

> ⚠️ Priset ($199/år) gäller 2026 och kan ha ändrats.

---

## 7. Set-progressionsheuristiken

### Algoritmens logik

RP:s set-progression är **villkorad och bidirektionell** — inte ett fast "+1 set per vecka". Inputs är de subjektiva recovery-markörerna, output är förändring i nästa veckas set-antal.

### Progressionsmatris

| Pump | Soreness (inför passet) | Performance | Åtgärd |
|------|------------------------|-------------|--------|
| 1 (låg) | 1 (låg) | 1 (bra) | +2–3 sets |
| 2 | 2 | 2 | +1 set |
| 2–3 | 2–3 | 2–3 | Behåll |
| 3 (hög) | 3–4 (hög) | 3 | Minska sets |
| - | - | 4 (mycket dålig) | Initiera deload |

**Förenklat beslutsträd:**

```
After each workout:
  IF performance = 4 → deload
  ELIF soreness = 3-4 AND performance = 3 → reduce sets
  ELIF all markers ≤ 2 → +1-3 sets next week
  ELSE → maintain
```

**Källa:** Rekonstruerat från rpstrength.com/blogs/articles/progressing-for-hypertrophy + rpstrength.com/pages/hypertrophy-app

### Vikt-progressionsdelen

Separerat från set-progression:
- Nådde reps-mål med ≥1 RIR → **öka vikt** nästa set/pass
- Nådde inte reps-mål → **behåll eller minska vikt**

---

## 8. Övningsspecifika regler

### Push vs. Pull

RP räknar push- och pull-mönster separat. En övning kategoriseras som antingen:
- **Push:** Bröst, axlar (front/lateral deltoid), triceps
- **Pull:** Rygg (lat/rhomboid/trap), biceps, rear delt

### Compound vs. Isolation

RP behandlar i allmänhet compound och isolation som likvärdiga i volymberäkning (1 set = 1 set), men betonar att isolation-övningar ofta ger bättre pump och isolation per muskelgrupp.

### Dubbelräkning

RP rekommenderar att räkna sets mot primär muskelgrupp. En chinup räknas primärt som **rygg**, inte biceps — om lyftaren vill räkna biceps-volym adderar de isolationsövningar. Dock kan en del av chinup-volymen räknas mot biceps beroende på greppbredd och form.

> ⚠️ Exakta regler för dubbelräkning (räknas chinup som rygg och/eller biceps) saknas i de verifierade källorna.

---

## 9. Kritisk granskning

### Vad kritiseras i RP-metoden?

**1. MEV/MRV-tal är individspecifika, inte universella**

Den vanligaste missuppfattningen — lyftare behandlar RP:s exempeltal (MEV=10 sets/v för bröst) som prescriptive regler snarare än illustrativa exempelintervall. RP:s egna texter betonar att dessa är startpunkter som kräver individuell kalibrering.

**2. "Effective reps"-kritiken (Nuckols mot RP)**

Greg Nuckols (Stronger by Science) ifrågasätter RP:s tillämpning av "effective reps"-modellen (att bara reps nära failure driver hypertrofi). Evidensen för att volym nära failure är strikt nödvändig är svagare än RP ibland implicerar. Nuckols förordar en mer pragmatisk approach där total volym och progression är primärvariabler.

**3. Är RP empiriskt eller heuristik?**

RP:s system bygger på evidensinspirerade principer men är inte empiriskt validerat i sin helhet som ett system. Enskilda komponenter (progressive overload, RPE-baserad autoregulation, deload-cykler) har evidensbaserat stöd, men den specifika algoritmen för MEV→MRV-progression är en rationell extrapolation, inte direkt testad RCT-data.

**4. Eric Helms och MASS Research Review**

Helms (Mountain Dog / 3DMJ) är generellt positiv till RP:s ramverk men betonar att individuell variation i volymtolerans är enorm — RP:s tal är mer variabla i praktiken än i teorin. MASS Research Review (Helms, Zourdos m.fl.) rapporterar att evidensen för specifika volymtrösklar är svag; MEV och MRV är konceptuellt solida men numeriskt svåra att pinpointa.

**5. Jeff Nippard**

Nippard är generellt positiv till RP-principerna men tar avstånd från att memorisera fasta volymtal. Hans slutsats liknar Helms': progressiv overload och konsekvens trumfar eventuell volymoptimering för majoriteten av lyftare.

### Sammanfattning av kritikens kärna

> RP:s styrka är ramverket och terminologin, inte de specifika talen. Autoreglering via RIR och recovery-markörer är solid metodologi. MEV/MRV-siffror bör ses som ungefärliga personliga experimentpunkter.

---

## 10. Svensk kontext

### Finns RP-material på svenska?

Inget RP-material är publicerat på svenska. Allt material — boken, appen, YouTube, podden — är uteslutande på engelska.

### Terminologi på svenska (etablerad i svenska träningssammanhang)

| Engelsk term | Svensk term (vanlig) | Alternativ |
|-------------|---------------------|-----------|
| Mesocycle | Mesocykel | Träningsfas, programblock |
| Reps In Reserve | Reps i reserv | RIR (används direkt) |
| RPE | RPE (används direkt) | Upplevd ansträngning (ovanligare) |
| Minimum Effective Volume | MEV (används direkt) | Minimivolym |
| Maximum Recoverable Volume | MRV (används direkt) | Maxvolym |
| Deload | Deload (används direkt) | Lätt vecka, återhämtningsvecka |
| Progressive overload | Progressiv överbelastning | Progression |

Svenska träningsforum (Kolozzeum Forum, Flashback Träning) använder i hög grad engelska akronymer direkt (RIR, MEV, MRV, RPE) utan översättning.

### Typiska missuppfattningar i svenska träningscommunities

1. **MEV/MRV-tal som universella** — samma som internationell kritik ovan
2. **Att RIR och RPE är identiska** — de är omvändbara men RIR är mer konkret
3. **Att RP-metoden kräver full mesomotor** — RP:s autoreglering kan implementeras set-för-set utan att bygga 8-veckors program
4. **Att deload alltid är obligatorisk** — vid lägre träningsvolym/frekvens behövs den sällan

---

## Källkaveat och begränsningar

### Vad som är väl verifierat (high confidence)

- Volymlandmärkenas definitioner (MV/MEV/MAV/MRV) — primärkälla rpstrength.com
- RIR/RPE-matematik och RIR-progression genom mesocykeln
- Set-progressionens villkorade, bidirektionella natur (inte fast +1/vecka)
- Accumulation → Deload som grundstruktur (inte Acc/Int/Rea-triaden)
- RP Hypertrophy-appens övergripande funktionalitet

### Vad som är osäkert eller ej verifierat

- Exakta sets/vecka-tal per muskelgrupp (tabellen ovan är illustrativ)
- Exakt input-lista i RP-appen (3 vs 4 vs 5 fält — joint pain är osäkert)
- Övningsspecifik volymviktning (chinup som rygg och/eller biceps)
- Svensk community-reception (inga svenska primärkällor verifierades)
- Kritik från Helms/Nuckols/Nippard är delvis verifierad

### Tidskänsliga uppgifter

- RP Hypertrophy-appens Meso Builder var i beta vid verifiering — feature-set kan ha ändrats
- Priset $199/år gäller 2026

---

## Vad som passar in i Vana Strengths kil

### Realistiskt implementerbara MVP-komponenter (utan full mesomotor)

**1. RIR-input per set (hög prioritet)**

Att låta användaren logga RIR tillsammans med vikt och reps är den mest värdefulla autoregleringsdatan. Det kräver ett extra fält i set-loggningen och kostar minimal UI-komplexitet. RIR-data möjliggör alla nedanstående funktioner.

**2. Vikt-progressionsregel (hög prioritet)**

Deterministisk, förklaringsbar regel baserad på RP:s heuristik:
```
Om: RIR ≥ 1 OCH alla reps nåddes
Då: Föreslå +2.5 kg (compound) / +1.25 kg (isolation) nästa session
Annars: Behåll vikt
```
Denna kan implementeras utan mesomotor — den körs per session, per övning.

**3. Post-workout feedback (3 fält, medel prioritet)**

Pump, soreness och performance på 1–4-skala efter passet. Dessa är inputs till set-progressionsregeln och deload-triggern. Kan presenteras som ett enkelt kortflöde efter avslutat pass.

**4. Set-progressionsregel (medel prioritet)**

Villkorad, baserad på post-workout feedback:
```
Performance = 4 → "Dags för en lätt vecka (deload)"
Soreness ≥ 3 + Performance ≥ 3 → Minska sets nästa vecka
Pump ≤ 2 + Soreness ≤ 2 → Öka sets nästa vecka (om under MRV)
Annars → Behåll
```

**5. Deload-rekommendation (låg prioritet för MVP)**

En enkel flagga — "dina recovery-markörer indikerar att det kan vara dags för en lätt vecka" — utan att kräva att appen "äger" hela mesocykeln.

### Vad som INTE passar in i kilen (ännu)

- Full mesocykelplanering (generera veckoplan 1–8 upfront)
- Per-muskelgrupp volymspårning mot MEV/MRV
- Automatisk programgenerering (Meso Builder)
- Periodiserade fasblock (Accumulation/Deload med kalender)

### MVP-progressionslager: konkret approach

```
Set-loggning:
  [vikt] [reps] [RIR] ← +1 fält vs. idag

Post-workout (kort):
  Hur var pumpen? [1][2][3][4]
  Hur ör det idag? [1][2][3][4]  (soreness)
  Hur presterade du? [1][2][3][4]

Nästa session (förklarat):
  "Baserat på dina senaste 2 pass: öka bänkpress med 2.5 kg"
  "Du verkar behöva återhämtning — kör en lätt vecka"
```

Det är tillräckligt smart för att kännas som en coach. Det är tillräckligt enkelt för att aldrig ge skadliga råd. Det löser kilen.

---

*Rapport genererad 2026-06-30. Primärkällor: rpstrength.com/blogs/articles/* och rpstrength.com/pages/hypertrophy-app. Se källkaveat-sektionen för konfidensgrader.*
