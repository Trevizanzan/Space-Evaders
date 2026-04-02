using System.Collections;
using TMPro;
using UnityEngine;

[System.Serializable]
public class PhaseConfig
{
    [Header("Spawn Types")]
    public bool spawnNormal = false;
    public bool spawnDiagonal = false;
    public bool spawnHorizontal = false;

    [Header("Spawn Rate Multipliers")]
    public float normalSpawnMultiplier = 1f;
    public float diagonalSpawnMultiplier = 1f;
    public float horizontalSpawnMultiplier = 1f;

    [Header("Enemy Types Unlocked")]
    public bool allowFighters = false;
    public bool allowKamikazes = false;
    public bool allowBombers = false;

    [Header("Difficulty Modifiers")]
    public float speedMultiplier = 1f; // Velocità asteroidi    // TODO da implementare
    public float healthMultiplier = 1f; // Vita asteroidi (da implementare)     // TODO da implementare

    [Header("Asteroid Size Focus")]
    [Tooltip("0 = tutti, 1 = solo piccoli, 2 = solo medi, 3 = solo grandi")]
    public int asteroidSizeFocus = 0; // 0 = mix, 1 = small only, 2 = medium only, 3 = large only   // TODO Implementare negli spawner
}

[System.Serializable]
public class WaveProfile
{  
    [Header("Wave Info")]
    public string waveName = "Default wave";
    public float waveDuration = 30f; // Durata totale della wave in secondi

    [Header("Phase Configurations")]
    public PhaseConfig phase1 = new PhaseConfig();
    public PhaseConfig phase2 = new PhaseConfig();
    public PhaseConfig phase3 = new PhaseConfig();

    // Helper per ottenere la config della fase corrente
    public PhaseConfig GetPhaseConfig(int phase)
    {
        switch (phase)
        {
            case 1: return phase1;
            case 2: return phase2;
            case 3: return phase3;
            default: return phase1;
        }
    }
}

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance;

    [Header("Wave Settings")]
    [SerializeField] private float transitionDuration = 3f; // pausa tra wave e boss

    [Header("Wave Profiles - Configure Each Wave!")]
    [SerializeField] private WaveProfile[] waveProfiles = new WaveProfile[6]; // 6 wave (una per boss)

    [Header("Difficulty Scaling")]
    [SerializeField] private float globalDifficultyMultiplier = 1f;
    [SerializeField] private float difficultyIncreasePerLoop = 0.5f; // +50% ogni loop completo

    [Header("Boss System")]
    [SerializeField] private GameObject[] bossPrefabs; // 6 boss in ordine
    private int bossIndex = 0;
    private int totalBossesDefeated = 0; // Traccia quanti boss hai battuto in totale
    private bool isBossFight = false;

    [Header("Debug")]
    [SerializeField] private bool skipToFirstBoss = false;
    private int debugBossIndex = 0;
    [SerializeField] private bool debugSpecificWave = false;    // Test wave specifica
    [SerializeField] private int debugWaveIndex = 0;    // Quale wave testare (0-5)

    [Header("Wave UI - Top Bar")]
    [SerializeField] private GameObject waveInfoGroup; // Gruppo wave UI
    [SerializeField] private TMP_Text waveText; // "WAVE 3/6"
    [SerializeField] private UnityEngine.UI.Image waveProgressBarFill; // Barra progresso

    [Header("Boss UI - Top Bar (opzionale per futuro)")]
    [SerializeField] private GameObject bossInfoGroup; // Gruppo boss UI
    [SerializeField] private TMP_Text bossNameText; // "MEGA DESTROYER"
    [SerializeField] private UnityEngine.UI.Image bossHealthBarFill; // Barra vita boss

    [Header("Progress Bar Colors")]
    [SerializeField] private Color barColorStart = new Color(0.2f, 1f, 0.3f); // Verde
    [SerializeField] private Color barColorMid = new Color(1f, 0.9f, 0.2f); // Giallo
    [SerializeField] private Color barColorEnd = new Color(1f, 0.3f, 0.2f); // Rosso
    [SerializeField] private Color barColorGold = new Color(1f, 0.85f, 0f); // Oro

    // Wave state
    private float waveTime = 0f;
    private float progress = 0f; // 0–1
    private bool isInTransition = false;

    // Events
    public System.Action OnWaveComplete;
    public GameObject levelCompletePanel;
    public TMP_Text levelCompleteCountdown;

    //[Header("Difficulty Curves (0 = inizio, 1 = fine livello)")]
    //[SerializeField] private AnimationCurve spawnRateCurve = AnimationCurve.Linear(0, 1.5f, 1, 0.4f);
    //[SerializeField] private AnimationCurve fallSpeedCurve = AnimationCurve.Linear(0, 4f, 1, 8f);
    //[SerializeField] private AnimationCurve asteroidHealthMultiplier = AnimationCurve.Constant(0, 1, 1f);

    private int currentLevel = 1;
    //private float levelTime = 0f;
    public System.Action OnLevelComplete; // evento per notificare altri sistemi (es. UI) quando un livello è completato


    void Awake()
    {
        if (Instance == null) Instance = this;

        // Inizializza wave profiles se vuoto (default)
        if (waveProfiles == null || waveProfiles.Length == 0)
        {
            InitializeDefaultWaveProfiles();
        }
    }

    private void Start()
    {
        if (DifficultyManager.Instance != null)
        {
            DifficultyManager.Instance.OnWaveComplete += ShowWaveComplete;
        }

        // Inizializza UI: mostra wave bar, nascondi boss bar
        ShowWaveUI();

        // Debug: salta direttamente al primo boss
        if (skipToFirstBoss)
        {
            StartCoroutine(DebugSkipToBoss());
        }
        else if (debugSpecificWave)
        {
            StartCoroutine(DebugSkipToWave());
        }
    }

    void ShowWaveComplete()
    {
        StartCoroutine(WaveCompleteRoutine());
    }
    IEnumerator WaveCompleteRoutine()
    {
        // ANIMAZIONE BARRA: Pulsa e diventa ORO
        if (waveProgressBarFill != null)
        {
            waveProgressBarFill.fillAmount = 1f;

            // Pulsa 3 volte
            for (int pulse = 0; pulse < 3; pulse++)
            {
                waveProgressBarFill.color = barColorGold;
                yield return new WaitForSeconds(0.15f);
                waveProgressBarFill.color = Color.white;
                yield return new WaitForSeconds(0.15f);
            }

            waveProgressBarFill.color = barColorGold; // Lascia oro
        }

        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(true);

        for (int i = 3; i > 0; i--)
        {
            if (levelCompleteCountdown != null)
                levelCompleteCountdown.text = $"⭐ WAVE COMPLETE! ⭐\nBoss incoming in {i}...";
            yield return new WaitForSeconds(1f);
        }

        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);
    }

    // Inizializza wave profiles di default (chiamato una volta)
    void InitializeDefaultWaveProfiles()
    {
        waveProfiles = new WaveProfile[6];

        // ═══════════════════════════════════════════════════════════════════════
        // WAVE 1 - "WARM UP" - Solo asteroidi normali, introduzione graduale
        // ═══════════════════════════════════════════════════════════════════════
        waveProfiles[0] = new WaveProfile
        {
            waveName = "Wave 1 - Asteroid Field",
            waveDuration = 45f,
            phase1 = new PhaseConfig
            {
                // Solo asteroidi normali, lenti, mix di dimensioni
                spawnNormal = true,
                spawnDiagonal = false,
                spawnHorizontal = false,
                normalSpawnMultiplier = 0.8f, // Spawn più lento del base
                speedMultiplier = 0.7f, // 70% velocità base
                healthMultiplier = 1f,
                asteroidSizeFocus = 0, // Mix di tutte le dimensioni
                allowFighters = false,
                allowKamikazes = false,
                allowBombers = false
            },
            phase2 = new PhaseConfig
            {
                // Aumenta leggermente spawn rate e velocità
                spawnNormal = true,
                spawnDiagonal = false,
                spawnHorizontal = false,
                normalSpawnMultiplier = 1f, // Velocità normale
                speedMultiplier = 0.85f,
                healthMultiplier = 1f,
                asteroidSizeFocus = 1, // Solo piccoli per ora
                allowFighters = false,
                allowKamikazes = false,
                allowBombers = false
            },
            phase3 = new PhaseConfig
            {
                // Incremento finale prima del boss
                spawnNormal = true,
                spawnDiagonal = false,
                spawnHorizontal = false,
                normalSpawnMultiplier = 1.2f,
                speedMultiplier = 1f, // Velocità standard
                healthMultiplier = 1f,
                asteroidSizeFocus = 0, // Ritorna a mix
                allowFighters = false,
                allowKamikazes = false,
                allowBombers = false
            }
        };
        // ═══════════════════════════════════════════════════════════════════════
        // WAVE 2 - "DIAGONAL ASSAULT" - Introduzione spawn diagonali
        // ═══════════════════════════════════════════════════════════════════════
        waveProfiles[1] = new WaveProfile
        {
            waveName = "Wave 2 - Diagonal Assault",
            waveDuration = 50f,
            phase1 = new PhaseConfig
            {
                // Solo normali, ma più veloci di wave 1
                spawnNormal = true,
                spawnDiagonal = false,
                spawnHorizontal = false,
                normalSpawnMultiplier = 1f,
                speedMultiplier = 0.9f,
                healthMultiplier = 1f,
                asteroidSizeFocus = 0,
                allowFighters = false,
                allowKamikazes = false,
                allowBombers = false
            },
            phase2 = new PhaseConfig
            {
                // INTRODUZIONE DIAGONALI - primo contatto
                spawnNormal = true,
                spawnDiagonal = true, // NOVITÀ!
                spawnHorizontal = false,
                normalSpawnMultiplier = 1f,
                diagonalSpawnMultiplier = 0.7f, // Spawn lento per imparare
                speedMultiplier = 0.95f,
                healthMultiplier = 1f,
                asteroidSizeFocus = 0,
                allowFighters = false,
                allowKamikazes = false,
                allowBombers = false
            },
            phase3 = new PhaseConfig
            {
                // Mix normale + diagonale intenso
                spawnNormal = true,
                spawnDiagonal = true,
                spawnHorizontal = false,
                normalSpawnMultiplier = 1.1f,
                diagonalSpawnMultiplier = 1f,
                speedMultiplier = 1.1f,
                healthMultiplier = 1f,
                asteroidSizeFocus = 2, // Solo medi per aumentare sfida
                allowFighters = false,
                allowKamikazes = false,
                allowBombers = false
            }
        };
        // ═══════════════════════════════════════════════════════════════════════
        // WAVE 3 - "TRI-DIRECTIONAL CHAOS" - Tutti i tipi + primi nemici
        // ═══════════════════════════════════════════════════════════════════════
        waveProfiles[2] = new WaveProfile
        {
            waveName = "Wave 3 - Tri-Directional Chaos",
            waveDuration = 55f,
            phase1 = new PhaseConfig
            {
                // Normale + diagonale insieme
                spawnNormal = true,
                spawnDiagonal = true,
                spawnHorizontal = false,
                normalSpawnMultiplier = 1f,
                diagonalSpawnMultiplier = 0.8f,
                speedMultiplier = 1f,
                healthMultiplier = 1.1f, // Primi asteroidi più resistenti
                asteroidSizeFocus = 0,
                allowFighters = true, // PRIMI NEMICI!
                allowKamikazes = false,
                allowBombers = false
            },
            phase2 = new PhaseConfig
            {
                // INTRODUZIONE HORIZONTAL
                spawnNormal = true,
                spawnDiagonal = true,
                spawnHorizontal = true, // NOVITÀ!
                normalSpawnMultiplier = 1f,
                diagonalSpawnMultiplier = 1f,
                horizontalSpawnMultiplier = 0.6f, // Lento per introduzione
                speedMultiplier = 1.05f,
                healthMultiplier = 1.1f,
                asteroidSizeFocus = 0,
                allowFighters = true,
                allowKamikazes = false,
                allowBombers = false
            },
            phase3 = new PhaseConfig
            {
                // TUTTI ATTIVI - primo vero caos
                spawnNormal = true,
                spawnDiagonal = true,
                spawnHorizontal = true,
                normalSpawnMultiplier = 1.2f,
                diagonalSpawnMultiplier = 1.1f,
                horizontalSpawnMultiplier = 0.9f,
                speedMultiplier = 1.15f,
                healthMultiplier = 1.2f,
                asteroidSizeFocus = 0,
                allowFighters = true,
                allowKamikazes = false,
                allowBombers = false
            }
        };
        // ═══════════════════════════════════════════════════════════════════════
        // WAVE 4 - "KAMIKAZE RAIN" - Introduzione kamikazes + focus horizontal
        // ═══════════════════════════════════════════════════════════════════════
        waveProfiles[3] = new WaveProfile
        {
            waveName = "Wave 4 - Kamikaze Rain",
            waveDuration = 60f,
            phase1 = new PhaseConfig
            {
                // Focus su horizontal + diagonal
                spawnNormal = false, // Pausa dai normali
                spawnDiagonal = true,
                spawnHorizontal = true,
                diagonalSpawnMultiplier = 1.2f,
                horizontalSpawnMultiplier = 1.1f,
                speedMultiplier = 1.1f,
                healthMultiplier = 1.2f,
                asteroidSizeFocus = 1, // Solo piccoli, veloci e tanti
                allowFighters = true,
                allowKamikazes = true, // KAMIKAZES UNLOCKED!
                allowBombers = false
            },
            phase2 = new PhaseConfig
            {
                // Tutti attivi ma focus su dimensioni grandi
                spawnNormal = true,
                spawnDiagonal = true,
                spawnHorizontal = true,
                normalSpawnMultiplier = 1f,
                diagonalSpawnMultiplier = 1.2f,
                horizontalSpawnMultiplier = 1.2f,
                speedMultiplier = 1.15f,
                healthMultiplier = 1.3f,
                asteroidSizeFocus = 3, // Solo grandi - muro di asteroidi lenti ma tank
                allowFighters = true,
                allowKamikazes = true,
                allowBombers = false
            },
            phase3 = new PhaseConfig
            {
                // Caos totale - tutti attivi, alta velocità
                spawnNormal = true,
                spawnDiagonal = true,
                spawnHorizontal = true,
                normalSpawnMultiplier = 1.3f,
                diagonalSpawnMultiplier = 1.3f,
                horizontalSpawnMultiplier = 1.2f,
                speedMultiplier = 1.25f,
                healthMultiplier = 1.3f,
                asteroidSizeFocus = 0, // Mix caotico
                allowFighters = true,
                allowKamikazes = true,
                allowBombers = false
            }
        };
        // ═══════════════════════════════════════════════════════════════════════
        // WAVE 5 - "BOMBING RUN" - Tutti i nemici sbloccati, difficoltà alta
        // ═══════════════════════════════════════════════════════════════════════
        waveProfiles[4] = new WaveProfile
        {
            waveName = "Wave 5 - Bombing Run",
            waveDuration = 65f,
            phase1 = new PhaseConfig
            {
                // Introduzione bombers con scenario controllato
                spawnNormal = true,
                spawnDiagonal = false, // Pausa dai diagonali
                spawnHorizontal = true,
                normalSpawnMultiplier = 1.1f,
                horizontalSpawnMultiplier = 1f,
                speedMultiplier = 1.2f,
                healthMultiplier = 1.4f,
                asteroidSizeFocus = 2, // Solo medi
                allowFighters = true,
                allowKamikazes = true,
                allowBombers = true // BOMBERS UNLOCKED!
            },
            phase2 = new PhaseConfig
            {
                // Tutti i tipi attivi, focus su asteroidi piccoli veloci
                spawnNormal = true,
                spawnDiagonal = true,
                spawnHorizontal = true,
                normalSpawnMultiplier = 1.4f,
                diagonalSpawnMultiplier = 1.3f,
                horizontalSpawnMultiplier = 1.2f,
                speedMultiplier = 1.35f, // Velocità alta
                healthMultiplier = 1.3f,
                asteroidSizeFocus = 1, // Piccoli veloci - bullet hell style
                allowFighters = true,
                allowKamikazes = true,
                allowBombers = true
            },
            phase3 = new PhaseConfig
            {
                // Pre-boss finale - massima intensità
                spawnNormal = true,
                spawnDiagonal = true,
                spawnHorizontal = true,
                normalSpawnMultiplier = 1.5f,
                diagonalSpawnMultiplier = 1.4f,
                horizontalSpawnMultiplier = 1.3f,
                speedMultiplier = 1.4f,
                healthMultiplier = 1.5f,
                asteroidSizeFocus = 0, // Mix totale
                allowFighters = true,
                allowKamikazes = true,
                allowBombers = true
            }
        };
        // ═══════════════════════════════════════════════════════════════════════
        // WAVE 6 - "FINAL GAUNTLET" - Massima difficoltà, preparazione al boss
        // ═══════════════════════════════════════════════════════════════════════
        waveProfiles[5] = new WaveProfile
        {
            waveName = "Wave 6 - Final Gauntlet",
            waveDuration = 70f,
            phase1 = new PhaseConfig
            {
                // Apertura aggressiva - solo grandi lenti ma tank
                spawnNormal = true,
                spawnDiagonal = true,
                spawnHorizontal = false,
                normalSpawnMultiplier = 1.3f,
                diagonalSpawnMultiplier = 1.2f,
                speedMultiplier = 1f, // Lenti ma resistenti
                healthMultiplier = 2f, // DOPPIA VITA
                asteroidSizeFocus = 3, // Solo grandi
                allowFighters = true,
                allowKamikazes = true,
                allowBombers = true
            },
            phase2 = new PhaseConfig
            {
                // Bullet hell - piccoli velocissimi
                spawnNormal = true,
                spawnDiagonal = true,
                spawnHorizontal = true,
                normalSpawnMultiplier = 2f, // DOPPIO SPAWN RATE
                diagonalSpawnMultiplier = 1.8f,
                horizontalSpawnMultiplier = 1.6f,
                speedMultiplier = 1.6f, // Velocissimi
                healthMultiplier = 1f, // Poca vita ma tanti
                asteroidSizeFocus = 1, // Solo piccoli
                allowFighters = true,
                allowKamikazes = true,
                allowBombers = true
            },
            phase3 = new PhaseConfig
            {
                // FINALE APOCALITTICO - tutto al massimo
                spawnNormal = true,
                spawnDiagonal = true,
                spawnHorizontal = true,
                normalSpawnMultiplier = 1.8f,
                diagonalSpawnMultiplier = 1.8f,
                horizontalSpawnMultiplier = 1.6f,
                speedMultiplier = 1.5f,
                healthMultiplier = 1.8f,
                asteroidSizeFocus = 0, // Mix caotico totale
                allowFighters = true,
                allowKamikazes = true,
                allowBombers = true
            }
        };
    }

    // Debug: Testa una wave specifica
    IEnumerator DebugSkipToWave()
    {
        yield return null;

        totalBossesDefeated = debugWaveIndex; // Simula progressione

        Debug.Log($"[DEBUG] Starting Wave {debugWaveIndex + 1}: {GetCurrentWaveProfile().waveName}");

        //// Disabilita UI testo
        //ScoreManager scoreManager = FindFirstObjectByType<ScoreManager>();
        //if (scoreManager != null)
        //{
        //    scoreManager.DisableLevelAndTimerText();
        //}

        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);
    }

    // Ottieni il profilo della wave corrente
    public WaveProfile GetCurrentWaveProfile()
    {
        int waveIndex = totalBossesDefeated % waveProfiles.Length;

        if (waveIndex < 0 || waveIndex >= waveProfiles.Length)
        {
            Debug.LogWarning($"[DIFFICULTY] Wave index {waveIndex} out of range, using Wave 0");
            return waveProfiles[0];
        }

        return waveProfiles[waveIndex];
    }


    //void ShowLevelComplete()
    //{
    //    StartCoroutine(LevelCompleteRoutine());
    //}
    //// Mostra pannello di completamento livello e countdown, poi nasconde
    //IEnumerator LevelCompleteRoutine()
    //{
    //    if (levelCompletePanel != null)
    //        levelCompletePanel.SetActive(true);

    //    for (int i = 3; i > 0; i--)
    //    {
    //        if (levelCompleteCountdown != null)
    //            levelCompleteCountdown.text = $"LEVEL COMPLETE! \n Next level in {i}...";

    //        yield return new WaitForSeconds(1f);
    //    }
    //    if (levelCompletePanel != null)
    //        levelCompletePanel.SetActive(false);
    //}

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver()) return;
        if (isInTransition) return;
        if (isBossFight) return;

        waveTime += Time.deltaTime;

        WaveProfile currentWave = GetCurrentWaveProfile();
        progress = Mathf.Clamp01(waveTime / currentWave.waveDuration); // USA waveDuration del profilo corrente

        // Aggiorna UI ogni frame
        UpdateWaveUI();

        // Check se wave finita → spawna boss
        if (waveTime >= currentWave.waveDuration)
        {
            StartCoroutine(BossTransition());
        }
    }

    /// <summary>
    /// Mostra UI Wave, nasconde UI Boss
    /// </summary>
    void ShowWaveUI()
    {
        if (waveInfoGroup != null) waveInfoGroup.SetActive(true);
        if (bossInfoGroup != null) bossInfoGroup.SetActive(false);

        // Reset barra wave
        if (waveProgressBarFill != null)
        {
            waveProgressBarFill.fillAmount = 0f;
            waveProgressBarFill.color = barColorStart;
        }
    }

    /// <summary>
    /// Mostra UI Boss, nasconde UI Wave
    /// </summary>
    void ShowBossUI()
    {
        if (waveInfoGroup != null) waveInfoGroup.SetActive(false);
        if (bossInfoGroup != null) bossInfoGroup.SetActive(true);

        // Reset barra boss
        if (bossHealthBarFill != null)
        {
            bossHealthBarFill.fillAmount = 1f; // Boss a vita piena
        }
    }

    /// <summary>
    /// Aggiorna UI della wave: testo, nome, barra di progresso con colori dinamici
    /// </summary>
    void UpdateWaveUI()
    {
        if (waveText == null || waveProgressBarFill == null) return;

        WaveProfile currentWave = GetCurrentWaveProfile();
        int currentWaveNumber = (totalBossesDefeated % waveProfiles.Length) + 1; // 1-6

        // Aggiorna testo wave
        waveText.text = $"WAVE {currentWaveNumber}/{waveProfiles.Length}";

        // Aggiorna barra di progresso
        waveProgressBarFill.fillAmount = progress;

        // Cambia colore in base al progresso (verde → giallo → rosso)
        Color barColor;
        if (progress < 0.5f)
        {
            // 0% - 50%: Verde → Giallo
            barColor = Color.Lerp(barColorStart, barColorMid, progress * 2f);
        }
        else
        {
            // 50% - 100%: Giallo → Rosso
            barColor = Color.Lerp(barColorMid, barColorEnd, (progress - 0.5f) * 2f);
        }

        waveProgressBarFill.color = barColor;
    }

    void OnGUI()
    {
        if (!Application.isPlaying) return;

        WaveProfile currentWave = GetCurrentWaveProfile();

        GUILayout.Label($"Wave: {currentWave.waveName}");
        GUILayout.Label($"Wave Time: {waveTime:F1}s");
        GUILayout.Label($"Progress: {progress:F2}");
        GUILayout.Label($"Phase: {GetCurrentPhase()}");
        GUILayout.Label($"Bosses Defeated: {totalBossesDefeated}");
        GUILayout.Label($"Global Multiplier: {globalDifficultyMultiplier:F2}x");
        GUILayout.Label($"Boss Fight: {isBossFight}");
    }

    #region Boss System

    // Gestisce la transizione al boss: notifica, ferma spawn asteroidi, aspetta, spawna boss, attiva modalità boss fight
    IEnumerator BossTransition()
    {
        isInTransition = true;
        OnLevelComplete?.Invoke();

        //// Nascondi UI wave durante il boss fight
        //if (waveText != null) waveText.gameObject.SetActive(false);
        //if (waveProgressBarFill != null) waveProgressBarFill.transform.parent.gameObject.SetActive(false);

        // Ferma spawn asteroidi
        AsteroidSpawner spawner = FindFirstObjectByType<AsteroidSpawner>();
        if (spawner != null) spawner.enabled = false;

        // TODO: Ferma anche EnemySpawner quando lo crei
        // EnemySpawner enemySpawner = FindFirstObjectByType<EnemySpawner>();
        // if (enemySpawner != null) enemySpawner.enabled = false;

        // Switch da Wave UI a Boss UI
        ShowBossUI();

        // Imposta nome boss (se disponibile)
        if (bossNameText != null && bossIndex < bossPrefabs.Length)
        {
            bossNameText.text = $"BOSS {bossIndex + 1}"; // Puoi personalizzare con nomi custom
        }
        yield return new WaitForSeconds(transitionDuration);

        // Spawna il boss corrente con posizione e rotazione corrette
        if (bossIndex < bossPrefabs.Length && bossPrefabs[bossIndex] != null)
        {
            float cameraTop = Camera.main.orthographicSize;
            Vector3 spawnPos = new Vector3(0, cameraTop + 1f, 0);
            Instantiate(bossPrefabs[bossIndex], spawnPos, bossPrefabs[bossIndex].transform.rotation);
        }

        // Attiva il boss fight mode
        isBossFight = true;
        isInTransition = false;
    }

    public void OnBossDefeated()
    {
        // Disattiva boss fight mode
        isBossFight = false;
        bossIndex++;
        totalBossesDefeated++; // Incrementa il contatore totale

        //// Riattiva UI wave
        //if (waveText != null) waveText.gameObject.SetActive(true);
        //if (waveNameText != null) waveNameText.gameObject.SetActive(true);
        //if (waveProgressBarFill != null) waveProgressBarFill.transform.parent.gameObject.SetActive(true);
        // ⭐ Switch da Boss UI a Wave UI
        ShowWaveUI();

        // TODO: game finale??
        // Se hai battuto tutti e 6 i boss, ricomincia loop con difficoltà aumentata
        if (bossIndex >= bossPrefabs.Length)
        {
            bossIndex = 0;
            globalDifficultyMultiplier += difficultyIncreasePerLoop; // +50% difficoltà ogni loop
        }

        // Resetta wave
        waveTime = 0f;
        progress = 0f;

        // Riattiva spawner asteroidi
        AsteroidSpawner spawner = FindFirstObjectByType<AsteroidSpawner>();
        if (spawner != null) spawner.enabled = true;

        // TODO: Riattiva EnemySpawner
        // EnemySpawner enemySpawner = FindFirstObjectByType<EnemySpawner>();
        // if (enemySpawner != null) enemySpawner.enabled = true;

        StartCoroutine(WaveStartDelay()); // normale transizione
    }

    IEnumerator WaveStartDelay()
    {
        isInTransition = true;
        yield return new WaitForSeconds(3f); // Breve pausa prima di ricominciare
        isInTransition = false;
    }

    // Metodo debug per testare il boss direttamente come primo livello
    IEnumerator DebugSkipToBoss()
    {
        debugBossIndex = 0; // o qualsiasi boss tu voglia testare

        // Aspetta 1 frame per far inizializzare tutto
        yield return null;

        // Disabilita lo spawner degli asteroidi
        AsteroidSpawner spawner = FindFirstObjectByType<AsteroidSpawner>();
        if (spawner != null) spawner.enabled = false;

        //// Disabilita UI testo (livello e timer)
        //ScoreManager scoreManager = FindFirstObjectByType<ScoreManager>();
        //if (scoreManager != null)
        //{
        //    // Se ScoreManager ha riferimenti pubblici a questi testi
        //    scoreManager.DisableLevelAndTimerText();
        //}

        // Spawna il primo boss (o quello che vuoi testare) con posizione e rotazione corretta
        if (bossPrefabs.Length > 0 && bossPrefabs[debugBossIndex] != null)
        {
            Debug.Log($"[DEBUG] Spawning boss {debugBossIndex} for testing");
            float cameraTop = Camera.main.orthographicSize;
            Vector3 spawnPos = new Vector3(0, cameraTop + 1f, 0);
            Instantiate(bossPrefabs[bossIndex], spawnPos, bossPrefabs[bossIndex].transform.rotation);   // mantengo rotazione del prefab
        }
        else
            Debug.LogWarning("[DEBUG] No boss prefab assigned!");

        // Attiva il boss fight mode
        isBossFight = true;

        // Opzionale: nascondi il pannello di level complete se è attivo
        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);
        else
            Debug.LogWarning("[DEBUG] No level complete panel assigned!");
    }

    #endregion

    #region Public Getters for Spawners

    /// <summary>
    /// Ritorna la fase corrente della wave
    /// (1, 2 o 3) in base al progress della wave
    /// </summary>
    public int GetCurrentPhase()
    {
        // FOR DEBUG:
        //return 3;

        if (progress < 0.33f) return 1; // Fase 1: 0-20s
        if (progress < 0.66f) return 2; // Fase 2: 20-40s
        return 3;                       // Fase 3: 40-60s
    }

    public float GetProgress() => progress;
    public float GetWaveTime() => waveTime;
    public bool IsInTransition() => isInTransition;
    public int GetTotalBossesDefeated() => totalBossesDefeated;
    public float GetGlobalMultiplier() => globalDifficultyMultiplier;
    public int GetCurrentLevel() => currentLevel;

    #endregion
}