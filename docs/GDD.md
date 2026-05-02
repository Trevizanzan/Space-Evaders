# SPACE EVADER — Game Design Document

**Version:** 1.0
**Status:** Living document (riflette sia la visione sia lo stato attuale del progetto)
**Last updated:** 2026-05-02
**Repository:** Unity 2D project (C#, URP/built-in, Input System nuovo — migrazione completata)

---

## 1. Identità del gioco

**Titolo:** Space Evader
**Genere:** Vertical scrolling shoot 'em up con elementi roguelite
**Tagline proposta:** *"The universe wants you dead. Evade it."*
**Tono narrativo:** Cupo ma non deprimente. Senso di solitudine e di essere *braccati*. L'Evader non vuole salvare l'universo: vuole **uscirne**.

### Pitch

Il player è un **Evader** — un fuggitivo solitario braccato in un universo ostile. Deve attraversare zone sempre più pericolose, schivare il male che lo insegue e affrontare il Mega Boss finale. Runs brevi, morte permanente, armi diverse, lore progressiva. Uno contro tutti.

---

## 1.5 Stile visivo e palette

Pixel art "arcade moderno" — stile tra R-Type e fumetto arcade anni '90. Palette intensa su sfondo spaziale scuro. Boss con facce mostruose ed espressive, navicelle con animazioni thruster.

### Palette ufficiale

| Colore | Hex | Ruolo |
|---|---|---|
| Nero | `#000000` | Sfondo spazio, ombre profonde |
| Viola scuro | `#2A0E54` | Sfondo nebula, UI dark |
| Magenta | `#AA1E65` | Nemici, accenti ostili |
| Rosa/Rosso | `#FB4F69` | Pericolo, proiettili nemici, HP basso |
| Bianco caldo | `#F9F7F7` | Testi, highlight neutri |
| Arancione | `#FC8141` | Esplosioni, energia, thruster |
| Giallo | `#FAD946` | Score, loot, accenti dorati |
| Blu | `#2F68DC` | Player, proiettili player, UI primaria |
| Ciano | `#46E7EC` | Armi speciali, charge effect, UI secondaria |

---

## 2. Core loop

MainMenu (PlayGame) → GameScene → Run (sequenza di Level + Boss da GameSequence) → Morte/Vittoria → Game Over con stats → [futuro: sblocchi] → MainMenu

- **Durata run completa target:** 15-20 minuti
- **Permadeath:** sì, morte = ricarica della GameScene
- **Meta-progressione:** ✅ implementata (unlock armi + perk in-run)

---

## 3. Struttura di una run

### Prospettiva di gioco

**Vertical scrolling shooter.** La telecamera è fissa (orthographic, `16:9`, `orthographicSize: 16.875`); il background scorre verticalmente verso il basso (`BackgroundScroll.cs`, `scrollSpeed: 0.3`). I nemici entrano dall'alto. Il player si muove liberamente entro i bordi visibili.

### Struttura temporale

- Ogni **Level** dura `levelDuration` secondi (configurabile per Level; target design: ~45s)
- Ogni Level è diviso in **3 fasi uguali** (`PhaseConfig × 3` in `LevelProfile`): fase 1 = più rilassata, fase 2 = intermedia, fase 3 = picco. Lo switch di fase è calcolato automaticamente in `DifficultyManager.GetCurrentPhase()`.

### Blocchi (sequenza)

- La sequenza dei Level è definita in un asset **`GameSequence`** (ScriptableObject) che contiene un array di `LevelProfile`
- Un `LevelProfile` può essere di tipo *Level* normale o *Boss* (tramite flag `isBoss`)
- Numero esatto di blocchi e composizione: **[TBD — da configurare nel GameSequence asset]**

### Flow di scrolling

**Continuous scroll.** Il background scorre senza interruzione durante il gameplay. Quando parte una Boss Fight:

- Gli spawner (`AsteroidSpawner`, `EnemySpawner`) vengono disabilitati
- Il boss viene istanziato dal prefab specificato in `LevelProfile.bossPrefab`
- L'UI passa da "Level Progress Bar" a "Boss Health Bar" (`DifficultyManager.ShowBossUI()`)
- Lo scrolling del background resta attivo per mantenere senso di moto
- Alla sconfitta del boss → `BossDefeatedTransition` con anima della barra a 0 → `AdvanceToNextLevel()`

### Transizione tra Level

Nessuna cinematica di transizione: lo scroll continuo non viene interrotto. Il `DifficultyManager` ha un parametro `transitionDuration` per una pausa logica fra level. La transizione narrativa sarà gestita dall'overlay di lore (vedi §6) all'inizio di ogni Level.

---

## 3.5 Tipologie di livelli

### Livelli Standard

Le regole di gameplay restano invariate: il player si muove, spara, schiva. Ciò che cambia è il **contenuto sullo schermo** (tipi di nemici, asteroidi, pattern).

Già supportato in `PhaseConfig` con flag e multiplicatori per:

- 3 tipi di asteroidi (Normal, Diagonal, Horizontal) con size distribution
- 4 tipi di nemici (Fighters, Kamikazes, Bombers, Pulsars) con multiplicatori di speed, shoot rate, drop rate, ecc.

### Livelli Gimmick

Un livello gimmick altera temporaneamente le regole base.

**Già implementato nel codice:**

- ✅ **`disableShooting`** (flag in `LevelProfile`): il player non può sparare. Applicato automaticamente da `DifficultyManager.ApplyLevelConstraints()` → `PlayerShooting.SetShootingDisabled()`.

**Idee future (non implementate):**

- **Oscurità totale:** illuminazione ridotta
- **Gravità anomala:** la nave acquista inerzia
- **Occhio del ciclone:** scrolling si ferma o rallenta

### Distribuzione per blocco (proposta)

Massimo **1 gimmick per blocco**. Il Blocco 1 è 100% standard per insegnare il gameplay base. Dopo, uno ogni tanto per variare. La distribuzione esatta è configurabile direttamente nel `GameSequence` asset.

---

## 4. Sistema armi

### ✅ Stato attuale

**Tutte e 3 le armi implementate.** Il sistema è basato su `WeaponData` (ScriptableObject astratto). Ogni arma è una sottoclasse concreta con asset in `Assets/ScriptableObjects/Weapons/`:

- `BlasterData` → `Blaster.asset`
- `RailgunData` → `Railgun.asset`
- `SpreadGunData` → `SpreadGun.asset`

`PlayerShooting.cs` delega lo spawn a `currentWeapon.Fire(firePoint)`. Supporta 3 fire behavior: `autoFire`, `requiresCharging` (manuale), e `autoFire + requiresCharging` (auto-carica e spara in loop).

Script di supporto:

- `ChargeEffect.cs` — effetto visivo di carica (sprite che cresce + pulse) parented al `firePoint`, sorting order automatico rispetto alla nave
- `WeaponSelection.cs` — classe statica di handoff tra il menu pre-run e la GameScene
- `WeaponSelectionMenu.cs` — istanzia le `WeaponCard` da prefab; array `availableWeapons` configurabile in Inspector
- `WeaponCard.cs` — singola card selezionabile (icona, nome, descrizione, stats); layout verticale auto-sizing via `VerticalLayoutGroup` + `ContentSizeFitter`

### Visione target: 3 armi sidegrade

Le 3 armi sono **sidegrade, non upgrade**. Il player sceglie una sola arma **prima della run** e la mantiene per tutta la run.

#### BLASTER — *implementato* ✅

- Proiettile singolo, dritto verso l'alto
- Fire rate medio (`shootCooldown: 0.15` = ~6.7 colpi/sec)
- Danno baseline (`1` nel TakeDamage)
- **Fire behavior:** autoFire (spara da solo, il player non preme nulla)
- L'arma "onesta": il baseline

#### SPREAD GUN — *implementato* ✅

- 3 proiettili a ventaglio (angolo ~30°)
- Fire rate basso (~2.5 colpi/sec)
- Danno per proiettile 0.4x del Blaster
- Portata corta (divergenza)
- **Fire behavior:** autoFire (coerente con il feel "sciame")
- Devastante contro sciami, debole contro bersagli singoli distanti

#### RAILGUN — *implementato* ✅

- Colpo caricato, perforante (attraversa più nemici in colonna)
- 1 colpo ogni ~2.5 sec (incluso tempo di carica ~1s)
- Danno 3.0x del Blaster
- Portata lunghissima
- **Fire behavior:** manual fire (tieni premuto per caricare, rilascia per sparare — il "feel" di carica è parte integrante dell'arma); supporta anche modalità `autoFire + requiresCharging` per auto-caricare e sparare in loop
- Vulnerabile durante la carica, inefficace contro sciami vicini

### Sblocco armi (✅ implementato)

| Arma | Condizione di sblocco | Quando arriva |
|---|---|---|
| **Blaster** | Sempre disponibile (`alwaysUnlocked = true`) | Run 1 |
| **Spread Gun** | 1° boss kill lifetime | ~Run 2-3 |
| **Railgun** | 2° boss kill lifetime | ~Run 3-5 |

Gestito da `UnlockManager` (classe statica, PlayerPrefs). `WeaponSelectionMenu.BuildCards()` filtra le armi locked automaticamente. Vedi §5 per i dettagli.

### Fire behavior (autoFire vs manual vs auto-charging)

Ogni arma definisce il proprio fire behavior tramite i flag in `WeaponData`. Non è una scelta del player, è una caratteristica dell'arma che concorre al suo gameplay distintivo:

- **autoFire:** la nave spara in continuo a cadenza fissa. Adatto per armi "baseline" o "sciame" dove il ritmo è costante.
- **manual fire (`requiresCharging`):** il player tiene premuto Fire per caricare e rilascia per sparare. Adatto per armi a colpo caricato dove il "feel" tattile conta (es. Railgun).
- **autoFire + requiresCharging:** la nave carica automaticamente, mostra l'effetto visivo, spara quando pronta e riparte. Variante automatica del Railgun.

---

### Menu di selezione armi (pre-run) — ✅ Implementato

Sub-panel del MainMenu (`WeaponSelectionMenu.cs`). Mostra solo le armi sbloccate come card orizzontali (`WeaponCard` prefab). La selezione chiama `WeaponSelection.SetWeapon(weapon)` prima di caricare la GameScene; `PlayerShooting` lo legge in `Awake()` con fallback al `defaultWeapon`.

---

## 4.5 Movement abilities (idea in esplorazione)

**Stato:** ❌ Non implementato. Idea da validare.

### Concept

Oltre a scegliere un'arma prima della run (§4), il player potrebbe scegliere anche una **movement ability**. Anche qui logica sidegrade, non upgrade: ogni scelta ha un pregio e un costo, nessuna domina.

### Abilità candidate (brainstorm)

#### BASIC — *baseline* (sempre disponibile)

- Movimento uniforme in tutte le direzioni (come oggi)
- Velocità base standard
- Nessun extra

#### DIAGONAL BOOST — *idea*

- Il movimento in diagonale è ~1.4x più veloce che in ortogonale (il comportamento "bugged" del vecchio input, trasformato in feature)
- Rewards player che sanno posizionarsi: chi muove la nave in zig-zag copre più terreno
- Costo: velocità base ortogonale lievemente ridotta

#### DIRECTIONAL DASH — *idea*

- Il player può fare un dash direzionale breve (scatto istantaneo con brevi i-frames durante il dash) per uscire da situazioni critiche
- Cooldown: ~3-5 secondi
- Costo: velocità base della navicella ridotta (es. ~0.8x)

### Principi di design

- **Al massimo 3 abilità totali** inclusa la baseline, per non esplodere la matrice di combinazioni con le armi
- Sidegrade veri: ogni abilità non-baseline ha un costo compensativo
- Sblocco permanente via meta-progressione (§5), come per le armi
- Il player sceglie **1 arma + 1 movement ability** prima della run

### Prerequisiti

- Sistema di meta-progressione persistente (§5)
- Schermata di selezione pre-run (§4) estesa per mostrare anche le movement abilities
- Potenziali hook in `PlayerHealth` (i-frames durante il dash) e `Spaceship.cs` (logica di scatto e di boost diagonale)

### Interazioni da progettare

- **Con le armi:** verificare che nessuna combinazione arma+movimento sia dominante (es. Railgun + Dash potrebbe trivializzare i boss)
- **Con i gimmick level** (§3.5): un dash con i-frames interagisce in modo non banale con eventuali futuri livelli "gravità anomala" o "oscurità". Da valutare caso per caso.
- **Con `disableShooting`:** ortogonale, nessun problema

---

## 5. Meta-progressione

### ✅ Stato attuale — Implementato

Sistema a due livelli implementato in `feat/weapon-selection-menu`.

#### Livello 1 — Unlock permanenti (tra run)

Le armi si sbloccano battendo i boss in qualsiasi run (contatore lifetime). Persistito in `PlayerPrefs`:

| Arma | Condizione | Chiave PlayerPrefs |
|---|---|---|
| **Blaster** | Sempre disponibile (`alwaysUnlocked = true`) | — |
| **Spread Gun** | 1° boss kill lifetime | `unlock_spreadgun` |
| **Railgun** | 2° boss kill lifetime | `unlock_railgun` |

Chiave aggiuntiva: `lifetime_boss_kills` (int). Gestita da `UnlockManager` (classe statica in `Assets/Scripts/Meta/`). `WeaponSelectionMenu.BuildCards()` filtra automaticamente le armi locked.

#### Livello 2 — Perk in-run (reset ogni run)

Dopo ogni boss kill appare un overlay fullscreen (`PerkSelectionOverlay`) con 3 perk casuali estratti da un pool. Il player ne sceglie 1. I perk durano solo per la run corrente (reset al game over o al ritorno al menu).

**Perk disponibili** (`Assets/ScriptableObjects/Perks/`):

| Asset | Tipo | Valore | Effetto |
|---|---|---|---|
| `ExtraLife.asset` | ExtraLife | — | +1 vita immediata |
| `FireBoost.asset` | FireRate | +15% | `FireRateMultiplier` in `PerkManager` |
| `SpeedBoost.asset` | MoveSpeed | +15% | `SpeedMultiplier` in `PerkManager` |
| `DamageBoost.asset` | Damage | +1 | `DamageBonus` passato a `WeaponData.Fire()` |
| `Shield.asset` | Shield | — | Assorbe 1 colpo (`ShieldActive` in `PerkManager`) |

**Architettura:**

- `PerkData` (ScriptableObject): definisce un perk (nome, descrizione, icona, tipo, valore)
- `PerkManager` (singleton MonoBehaviour in GameScene): accumula i moltiplicatori attivi per la run
- `PerkSelectionOverlay` (MonoBehaviour su pannello in GameScene, starts inactive): usa `OnEnable`/`OnDisable` per disabilitare/riabilitare input player; `DifficultyManager.BossDefeatedTransition()` chiama `SetActive(true)` e attende `WaitUntil(() => perkOverlay.IsDone)`
- `PerkCard` (MonoBehaviour su prefab): card selezionabile con icona, nome, descrizione

**Dove vengono applicati i perk in gameplay:**

- `PlayerShooting.cs`: `shootCooldown / FireRateMultiplier`; `damage + DamageBonus`
- `Spaceship.cs`: velocità × `SpeedMultiplier`
- `PlayerHealth.cs`: se `ShieldActive`, il primo colpo viene assorbito

#### ✅ Perk icon in HUD — Implementato

Dopo aver scelto un perk appare una piccola icona (+ valore se applicabile) nel Perk HUD in-game. Architettura:

- `PerkHUD.cs` — MonoBehaviour nell'HUD. Si sottoscrive a `PerkManager.OnPerkAdded` / `OnShieldConsumed`. Istanzia un `PerkHUDIcon` per ogni perk attivo; il Shield viene rimosso quando consumato.
- `PerkHUDIcon.prefab` — icona quadrata 12×12 + `TMP_Text` auto-width (ContentSizeFitter). Layout orizzontale interno (`HorizontalLayoutGroup`).
- Posizione: colonna verticale ancorata in basso-destra della TopBar, immediatamente a sinistra del pannello vite. Ogni nuovo perk aggiunge una riga sotto le precedenti (VerticalLayoutGroup + ContentSizeFitter).
- Valori mostrati: `+X%` per FireRate/MoveSpeed, `+X` per Damage, nessun testo per Shield/ExtraLife.
- `PerkManager` espone `List<PerkData> ActivePerks`, `event Action<PerkData> OnPerkAdded`, `event Action OnShieldConsumed`.

### Cosa NON si sblocca (esplicitamente escluso)

- Nessuno stat boost permanente tra run
- Nessun upgrade passivo permanente
- Nessuna skin/cosmetico
- Nessuna nave aggiuntiva

---

## 6. Lore e narrativa

### Stato attuale

**✅ Sistema di dialogo/lore implementato.** Architettura:

- `DialogueSequence` (ScriptableObject): array di `DialogueLine` (speaker + testo localizzato IT/EN)
- `DialogueOverlay` (singleton MonoBehaviour): mostra le linee in sequenza con fade, avatar speaker, avanzamento con Fire o skip con Pause (hold ~0.6s)
- `LevelProfile` espone `dialogueIntro` e `dialogueOutro` (`DialogueSequence`): riprodotti automaticamente all'inizio/fine del level e al completamento del boss
- `LanguageManager`: gestisce la lingua attiva (IT default); testo scelto automaticamente da `DialogueLine.GetText()`
- `DialogueSequenceEditor`: editor Unity custom per anteprima e editing delle sequenze in-Inspector

Asset dialoghi in `Assets/ScriptableObjects/Dialogues/`. Documentazione narrativa in `docs/LORE.md` e `docs/DIALOGO.md`.

**Layer 2 — Lore agli sblocchi:**

- Alla sblocco di un'arma: schermata con 2-3 righe tematiche — **non ancora implementato**
- Esempio Spread Gun: *"Scavenged from a derelict outpost. The previous owner never made it out."*

---

## 7. Nemici

### ✅ Stato attuale (tutti e 4 implementati)

| Archetipo | Script | Comportamento |
|---|---|---|
| **Fighter** | `EnemyFighter.cs` | Spara verso il player. 3 traiettorie: Straight, Diagonal, SCurve. Ruota verso il player mentre si muove. |
| **Kamikaze** | `EnemyKamikaze.cs` | Entra, si ferma in *hovering*, poi carica verso la posizione del player. |
| **Bomber** | `EnemyBomber.cs` | Pattuglia orizzontale nel terzo superiore della camera. Sgancia bombe a intervalli randomici. |
| **Pulsar** | `EnemyPulsar.cs` | Si posiziona in alto, mira con delay, spara burst di laser. Si riposiziona dopo N burst. |

Tutti derivano da `EnemyBase`. Tutti hanno un metodo `Initialize(PhaseConfig phase)` che applica i multiplicatori della fase corrente.

### Collegamento con il design armi

Per far emergere i tradeoff delle armi serve il giusto mix di nemici in ogni Level. La mappatura concettuale:

- **Kamikaze + Fighter in Straight/Diagonal** → puniti dal Blaster (serve precisione e fire rate)
- **Kamikaze a gruppi** → puniti dallo Spread Gun (sciame vicino)
- **Pulsar e Bomber** → puniti dalla Railgun (bersaglio singolo ad alta priorità)

### Da valutare (futuro)

- Nemici "sciame" veri e propri: gruppi grandi di piccoli nemici deboli. Al momento gli archetipi sono tutti a spawn singolo.
- Varianti per blocco: stessi archetipi con color palette/behavior variants per distinguere le zone.

---

## 8. Boss

### ✅ Stato attuale

- **1 boss implementato: `BossAngel`** (`BossAngel.cs`)
- Esiste `BossBase` come classe padre e prefab `BossHealthBar.prefab` per la UI boss

#### BossAngel — Pattern di combattimento

**Movimento:**
- Asse X: ping-pong verso target X random entro i bordi schermo
- Asse Y: si sposta verso una quota Y random scelta nel range `[centerY, topY]`, aspetta un tempo random, poi sceglie una nuova quota — comportamento imprevedibile ma sempre dentro i bounds configurabili

**Sistema a 3 fasi (HP-based):**

| Fase | HP | Cadenza sparo | Colpi mirati | Velocità |
|---|---|---|---|---|
| 1 | > 66% | 0.25–0.8s | nessuno | base |
| 2 | 33–66% | 0.15–0.5s | 50% (configurabile) | base |
| 3 | < 33% | 0.1–0.35s | 100% | aumentata |

Al cambio di fase: 3 flash bianchi come feedback visivo.

**Sparo mirato:** in fase 2/3, i proiettili calcolano la direzione verso il player al momento dello sparo (`BossBullet.SetDirection()`). Velocità dei colpi mirati ridotta rispetto ai colpi diritti (configurabile via `aimedShotSpeed`).

**Comportamento dei proiettili (`BossBullet`):** distruzione basata su viewport (`WorldToViewportPoint`, margine 10% su tutti i 4 lati). Dopo la morte del boss, il `BossDefeatedTransition` attende che tutti i bullet escano dallo schermo prima di mostrare il perk overlay.

**Invulnerabilità durante l'entrance:** ✅ i proiettili del player in volo al momento dello spawn vengono assorbiti senza danno finché `isEntering` è `true`. Lo sparo del player è disabilitato durante tutta la discesa del boss.

### Da implementare

- Altri boss (almeno uno per blocco, più il Mega Boss finale)
- Ogni boss dovrebbe avere pattern distintivo e telegrafato

### Principi di design boss

- Pattern telegrafati e imparabili (no random puro)
- Fasi multiple (es: a metà HP cambia pattern)
- Durata 60-120 secondi per boss intermedi, più lungo per Mega Boss
- Il Mega Boss deve richiedere padronanza delle armi e dei pattern

---

## 9. Controlli

### ✅ Stato attuale (migrazione al nuovo Input System completata)

Tutti gli script del player usano il nuovo Input System tramite l'asset `SpaceEvaderInputActions.inputactions` (in `Assets/Scripts/Input/`) con action map `Player` contenente:

- **Move** (Value/Vector2): WASD + Frecce + Gamepad Left Stick
- **Fire** (Button): Space + Mouse Left + Gamepad South (A/Cross) — usato anche per avanzare i dialoghi
- **Pause** (Button): Escape + Gamepad Start + Gamepad North (Y) — tenuto ~0.6s durante un dialogo skippa l'intera sequenza
- **Restart** (Button): Enter + Gamepad East (B/Circle) — attivo solo durante Game Over

Script migrati: `Spaceship.cs` (movimento), `PlayerShooting.cs` (fuoco), `GameManager.cs` (pause/restart). La classe C# `SpaceEvaderInputActions` è auto-generata dall'asset — non modificarla direttamente.

Cursor manager esiste per gestire cursore in menu vs gameplay (`CursorManager.cs`).

### Decisione presa (e implementata)

Migrazione al nuovo Input System completata. Architettura scelta: **Strada B** (generated C# class, no `PlayerInput` component). Migrazione effettuata in 4 sotto-step isolati: setup asset, `Spaceship` movement, `PlayerShooting` fire, `GameManager` pause/restart.

### Pausa

Il gioco supporta una pausa vera: `Esc` (tastiera) o `Start` (gamepad) attivano la pausa, fermando `Time.timeScale = 0` e mutando l'audio globalmente via `AudioListener.pause = true`. Un `PausePanel` con pulsanti "Resume" e "Back to Main Menu" appare in overlay.

Cliccare "Back to Main Menu" mostra un dialog di conferma (`ConfirmExitDialog`) che richiede esplicitamente di confermare l'uscita, dato il permadeath. Il dialog può essere chiuso cliccando "Cancel" o premendo nuovamente `Esc` (cascata: dialog → panel → game).

La pausa è bloccata durante Game Over (resta gestito dalla schermata Game Over esistente). Lo stato del gioco è gestito dall'enum `GameState { Playing, Paused, GameOver }` in `GameManager`.

---

## 10. Sistemi tecnici già in piedi

### Architettura

- **Managers singleton** con `DontDestroyOnLoad` per alcuni (`SoundManager`, `StatsRecorder`, `CursorManager`)
- **Managers di scena** per altri (`GameManager`, `DifficultyManager`) — ripopolati ad ogni caricamento di `GameScene`
- **ScriptableObject** per la configurazione dati (`LevelProfile`, `PhaseConfig`, `GameSequence`, `WeaponData`)

### Scene

- `MainMenu.unity` — menu principale (Play/Quit)
- `GameScene.unity` — gioco vero e proprio

### Sistemi periferici

- `SoundManager`: gestione SFX con cooldown per evitare spam audio
- `StatsRecorder`: salvataggio stats su file JSON + invio a Discord via webhook (`webhook_config.txt` in `StreamingAssets`)
- `RunStats`: contatori in-memory della run corrente
- `ScoreManager`: gestione score
- `ExplosionManager`: effetti esplosione
- `BackgroundScroll`: scrolling infinito con 2 sprite che si alternano
- `CameraSetup`: mantiene aspect ratio 16:9 con orthographicSize 16.875
- `AsteroidSpawner`: spawn di 3 tipi di asteroidi (Normal/Diagonal/Horizontal) in 3 size (Small/Medium/Large) con pesi configurabili

### Pattern di design usati

- Event-based: `DifficultyManager.OnLevelComplete`, `OnWaveComplete`
- Debug flags in ogni spawner per testare singoli tipi di nemico
- `PhaseConfig` con multiplicatori normalizzati (1.0 = base prefab)

---

## 11. Roadmap di sviluppo

### ✅ Fase 0 — Fondamenta (completa)

- Controlli player base + collisioni + HP
- 1 arma funzionante (Blaster)
- 4 tipi di nemici
- 1 boss
- Sistema di Level + Fasi + Boss via `GameSequence`
- UI base (progress bar, health bar boss, game over panel)
- Stats tracking + webhook Discord per playtest
- Scrolling background continuo

### 🔄 Fase 1 — Completamento del core (in corso)

In ordine consigliato di priorità:

1. ✅ **Migrazione al nuovo Input System** (completata)
2. ✅ **Sistema armi multiple** (`WeaponData` + `BlasterData`/`SpreadGunData`/`RailgunData`)
3. ✅ **Sistema di meta-progressione** — unlock armi permanenti via PlayerPrefs + perk in-run dopo ogni boss + Perk HUD in-game (icone verticali accanto alle vite, auto-width)
4. ✅ **Schermata di selezione arma pre-run** — `WeaponSelectionMenu` sub-panel del MainMenu; 3 card (Blaster, SpreadGun, Railgun) tutte sbloccate; selezione chiama `WeaponSelection.SetWeapon()` e carica la GameScene; layout orizzontale con `HorizontalLayoutGroup` + `VerticalLayoutGroup` interno per auto-sizing delle card
4.5. ✅ **TopBar rework** — barra full-width unificata (blu per livelli, rossa per boss); testo bordo destro mostra numero livello o nome boss; pannello score a sinistra, vite + perk HUD a destra
5. ✅ **Sistema di lore/dialogo in-game** — `DialogueSequence`, `DialogueOverlay`, `LanguageManager`, editor custom; dialoghi intro/outro per ogni Level e Boss
6. **Popolamento del GameSequence** (definire i 4 blocchi + mega boss)
7. **Creazione degli altri boss** (almeno 3 nuovi + mega boss)

### 🔜 Fase 2 — Contenuto e varietà

- Livelli gimmick oltre al `disableShooting` (oscurità, gravità, ecc.)
- Bilanciamento fine delle armi
- **Movement abilities** (§4.5) — validare concept, implementare e bilanciare 1-2 sidegrade oltre la baseline
- Polish dei pattern nemici per blocco
- Audio completo

### 🔜 Fase 3 — Release-ready

- Menu opzioni, audio, controlli rebinding
- Localizzazione (se vuoi)
- Ottimizzazione performance
- Bug fixing

### 🔜 Fase 4 — Post-launch (opzionale)

- Universi paralleli / New Game+
- Assist Mode per casual
- Mutators / modificatori
- Nuove armi

---

## 12. Decisioni esplicitamente scartate

- ❌ **Navi multiple selezionabili** — armi diverse coprono la varietà
- ❌ **Skin e cosmetici** — non di interesse
- ❌ **Checkpoint ai boss (default)** — minano la tensione permadeath
- ❌ **Continue con penalità** — rompe il senso della run
- ❌ **Scaling dinamico della difficoltà** — meglio bilanciare le armi
- ❌ **Stat boost permanenti tra run** — minano il "migliori perché impari"
- ❌ **Twin-stick shooter** — non coerente col tradizionalismo shmup

---

## 13. Note tecniche da rivedere (debito tecnico identificato)

- **`ApplyHealthMultiplier` in AsteroidSpawner:** marcato come TODO, non ancora implementato.
- **Commenti "TODO: cosa fa l'if?"** in `DifficultyManager` sul loop infinito — decidere come gestire il ciclo completo del `GameSequence`.
- **`PlayShoot()` in `SoundManager`** è di fatto vuoto (tutto commentato): decidere se reintrodurre il suono di sparo o rimuovere.
