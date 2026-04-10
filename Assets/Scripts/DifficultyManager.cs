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
    public bool allowPulsars = false;

    [Header("Difficulty Modifiers")]
    public float speedMultiplier = 1f; // Velocità asteroidi    // TODO da implementare
    public float healthMultiplier = 1f; // Vita asteroidi (da implementare)     // TODO da implementare

    [Header("Asteroid Size Focus")]
    [Tooltip("0 = tutti, 1 = solo piccoli, 2 = solo medi, 3 = solo grandi")]
    public int asteroidSizeFocus = 0; // 0 = mix, 1 = small only, 2 = medium only, 3 = large only   // TODO Implementare negli spawner
}

[System.Serializable]
public class LevelProfile
{  
    [Header("Level Info")]
    public string levelName = "Default level";
    public float levelDuration = 30f; // Durata totale della level in secondi

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

    [Header("Level Settings")]
    [SerializeField] private float transitionDuration = 3f; // pausa tra level e boss

    [Header("Level Profiles - Configure Each Level!")]
    [SerializeField] private LevelProfile[] levelProfiles = new LevelProfile[6]; // 6 level (una per boss)

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
    [SerializeField] private bool debugSpecificLevel = false;    // Test level specifica
    [SerializeField] private int debugLevelIndex = 0;    // Quale level testare (0-5)

    [Header("Level UI - Top Bar")]
    [SerializeField] private GameObject levelInfoGroup; // Gruppo level UI
    [SerializeField] private TMP_Text levelText; // "level 3/6"
    [SerializeField] private UnityEngine.UI.Image levelProgressBarFill; // Barra progresso

    [Header("Boss UI - Top Bar (opzionale per futuro)")]
    [SerializeField] private GameObject bossInfoGroup; // Gruppo boss UI
    [SerializeField] private TMP_Text bossNameText; // "MEGA DESTROYER"
    [SerializeField] private UnityEngine.UI.Image bossHealthBarFill; // Barra vita boss

    [Header("Progress Bar Colors")]
    [SerializeField] private Color barColorStart = new Color(0.2f, 1f, 0.3f); // Verde
    [SerializeField] private Color barColorMid = new Color(1f, 0.9f, 0.2f); // Giallo
    [SerializeField] private Color barColorEnd = new Color(1f, 0.3f, 0.2f); // Rosso
    [SerializeField] private Color barColorGold = new Color(1f, 0.85f, 0f); // Oro

    // Level state
    private float levelTime = 0f;
    private float progress = 0f; // 0–1
    private bool isInTransition = false;

    // Events
    public System.Action OnLevelComplete;   // evento per notificare altri sistemi (es. UI) quando un livello è completato
    //public GameObject levelCompletePanel;
    //public TMP_Text levelCompleteCountdown;

    void Awake()
    {
        if (Instance == null) Instance = this;

        // Inizializza level profiles se vuoto (default)
        if (levelProfiles == null || levelProfiles.Length == 0)
        {
            InitializeDefaultLevelProfiles();
        }
    }

    private void Start()
    {
        if (DifficultyManager.Instance != null)
        {
            DifficultyManager.Instance.OnLevelComplete += ShowLevelComplete;
        }

        // Inizializza UI: mostra level bar, nascondi boss bar
        ShowLevelUI();

        // Debug: salta direttamente al primo boss
        if (skipToFirstBoss)
        {
            StartCoroutine(DebugSkipToBoss());
        }
        else if (debugSpecificLevel)
        {
            StartCoroutine(DebugSkipToLevel());
        }
    }

    void ShowLevelComplete()
    {
        StartCoroutine(LevelCompleteRoutine());
    }
    IEnumerator LevelCompleteRoutine()
    {
        // ANIMAZIONE BARRA: Pulsa e diventa ORO
        if (levelProgressBarFill != null)
        {
            levelProgressBarFill.fillAmount = 1f;

            // Pulsa 3 volte
            for (int pulse = 0; pulse < 3; pulse++)
            {
                levelProgressBarFill.color = barColorGold;
                yield return new WaitForSeconds(0.15f);
                levelProgressBarFill.color = Color.white;
                yield return new WaitForSeconds(0.15f);
            }

            levelProgressBarFill.color = barColorGold; // Lascia oro
        }
    }

    // Inizializza level profiles di default (chiamato una volta)
    void InitializeDefaultLevelProfiles()
    {
        levelProfiles = new LevelProfile[6];

        // ═══════════════════════════════════════════════════════════════════════
        // level 1 - "WARM UP" - Solo asteroidi normali, introduzione graduale
        // ═══════════════════════════════════════════════════════════════════════
        levelProfiles[0] = new LevelProfile
        {
            levelName = "Level 1 - Asteroid Field",
            levelDuration = 30f,
            phase1 = new PhaseConfig
            {
                // Solo asteroidi normali, lenti, mix di dimensioni
                spawnNormal = true,
                spawnDiagonal = false,
                spawnHorizontal = false,
                normalSpawnMultiplier = 0.8f, // Spawn più lento del base
                speedMultiplier = 0.7f, // 70% velocità base
                healthMultiplier = 1f,  // gli asteroidi hanno vita normale
                asteroidSizeFocus = 1, // Mix di tutte le dimensioni
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
                asteroidSizeFocus = 0, // mix di tutte le dimensioni
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
                asteroidSizeFocus = 0, 
                allowFighters = false,
                allowKamikazes = false,
                allowBombers = false
            }
        };
        // ═══════════════════════════════════════════════════════════════════════
        // level 2 - "DIAGONAL ASSAULT" - Introduzione spawn diagonali - intrduce il Fight
        // ═══════════════════════════════════════════════════════════════════════
        levelProfiles[1] = new LevelProfile
        {
            levelName = "Level 2 - Diagonal Assault",
            levelDuration = 40f,
            phase1 = new PhaseConfig
            {
                // INTRODUZIONE DIAGONALI - primo contatto
                spawnNormal = true,
                spawnDiagonal = true,   // novità!
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
                // Introduzione Fighter
                spawnNormal = true,
                spawnDiagonal = true, 
                spawnHorizontal = false,
                normalSpawnMultiplier = 1f,
                diagonalSpawnMultiplier = 0.9f, // Spawn rate diagonali più alto per aumentare pressione
                speedMultiplier = 0.95f,
                healthMultiplier = 1f,
                asteroidSizeFocus = 0,
                allowFighters = true,   // FIGHTERS UNLOCKED!
                allowKamikazes = false,
                allowBombers = false
            },
            phase3 = new PhaseConfig
            {
                spawnNormal = true,
                spawnDiagonal = true,
                spawnHorizontal = false,
                normalSpawnMultiplier = 1.1f,   // Aumenta spawn rate normali per bilanciare l'introduzione dei diagonali
                diagonalSpawnMultiplier = 1f,
                speedMultiplier = 1.1f,
                healthMultiplier = 1f,
                asteroidSizeFocus = 0,  //2, // Solo medi per aumentare sfida
                allowFighters = true,   
                allowKamikazes = false, // fighter + diagonali
                allowBombers = false
            }
        };
        // ═══════════════════════════════════════════════════════════════════════
        // level 3 - "TRI-DIRECTIONAL CHAOS" - Tutti i tipi + primi nemici
        // ═══════════════════════════════════════════════════════════════════════
        levelProfiles[2] = new LevelProfile
        {
            levelName = "Level 3 - Tri-Directional Chaos",
            levelDuration = 55f,
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
        // level 4 - "KAMIKAZE RAIN" - Introduzione kamikazes + focus horizontal
        // ═══════════════════════════════════════════════════════════════════════
        levelProfiles[3] = new LevelProfile
        {
            levelName = "Level 4 - Kamikaze Rain",
            levelDuration = 60f,
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
        // level 5 - "BOMBING RUN" - Tutti i nemici sbloccati, difficoltà alta
        // ═══════════════════════════════════════════════════════════════════════
        levelProfiles[4] = new LevelProfile
        {
            levelName = "Level 5 - Bombing Run",
            levelDuration = 65f,
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
        // level 6 - "FINAL GAUNTLET" - Massima difficoltà, preparazione al boss
        // ═══════════════════════════════════════════════════════════════════════
        levelProfiles[5] = new LevelProfile
        {
            levelName = "Level 6 - Final Gauntlet",
            levelDuration = 70f,
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

    // Debug: Testa una level specifica
    IEnumerator DebugSkipToLevel()
    {
        yield return null;

        totalBossesDefeated = debugLevelIndex; // Simula progressione

        Debug.Log($"[DEBUG] Starting Level {debugLevelIndex + 1}: {GetCurrentLevelProfile().levelName}");

        //// Disabilita UI testo
        //ScoreManager scoreManager = FindFirstObjectByType<ScoreManager>();
        //if (scoreManager != null)
        //{
        //    scoreManager.DisableLevelAndTimerText();
        //}

        //if (levelCompletePanel != null)
        //    levelCompletePanel.SetActive(false);
    }

    // Ottieni il profilo della level corrente
    public LevelProfile GetCurrentLevelProfile()
    {
        int levelIndex = totalBossesDefeated % levelProfiles.Length;

        if (levelIndex < 0 || levelIndex >= levelProfiles.Length)
        {
            Debug.LogWarning($"[DIFFICULTY] Level index {levelIndex} out of range, using Level 0");
            return levelProfiles[0];
        }

        return levelProfiles[levelIndex];
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
        if (EnemySpawner.IsDebugMode || AsteroidSpawner.IsDebugMode) return; // ← blocca tutto: level timer, boss transition, ship transition

        levelTime += Time.deltaTime;

        LevelProfile currentLevel = GetCurrentLevelProfile();
        progress = Mathf.Clamp01(levelTime / currentLevel.levelDuration); // USA levelDuration del profilo corrente

        // Aggiorna UI ogni frame
        UpdateLevelUI();

        // Check se level finita → spawna boss
        if (levelTime >= currentLevel.levelDuration)
        {
            StartCoroutine(BossTransition());
        }
    }

    /// <summary>
    /// Mostra UI Level, nasconde UI Boss
    /// </summary>
    void ShowLevelUI()
    {
        if (levelInfoGroup != null) levelInfoGroup.SetActive(true);
        if (bossInfoGroup != null) bossInfoGroup.SetActive(false);

        // Reset barra level
        if (levelProgressBarFill != null)
        {
            levelProgressBarFill.fillAmount = 0f;
            levelProgressBarFill.color = barColorStart;
        }
    }

    /// <summary>
    /// Mostra UI Boss, nasconde UI Level
    /// </summary>
    public void ShowBossUI()
    {
        Debug.Log("[DifficultyManager] ShowBossUI() chiamato!");

        //if (levelInfoGroup != null) levelInfoGroup.SetActive(false);
        //if (bossInfoGroup != null) bossInfoGroup.SetActive(true);

        if (levelInfoGroup != null)
        {
            levelInfoGroup.SetActive(false);
            Debug.Log("[DifficultyManager] LevelInfoGroup nascosto");
        }
        else
        {
            Debug.LogError("[DifficultyManager] LevelInfoGroup è NULL!");
        }

        if (bossInfoGroup != null)
        {
            bossInfoGroup.SetActive(true);
            Debug.Log("[DifficultyManager] BossInfoGroup mostrato");
        }
        else
        {
            Debug.LogError("[DifficultyManager] BossInfoGroup è NULL!");
        }

        // Reset barra boss
        if (bossHealthBarFill != null)
        {
            bossHealthBarFill.fillAmount = 1f;
            bossHealthBarFill.color = new Color(1f, 0.2f, 0.2f);
        }
    }
    IEnumerator FadeOutLevelBar()
    {
        CanvasGroup cg = levelInfoGroup.GetComponent<CanvasGroup>();
        if (cg == null) cg = levelInfoGroup.AddComponent<CanvasGroup>();

        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            cg.alpha = 1f - (elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        levelInfoGroup.SetActive(false);
        cg.alpha = 1f; // Reset per prossima volta
    }

    /// <summary>
    /// Aggiorna UI della level: testo, nome, barra di progresso con colori dinamici
    /// </summary>
    void UpdateLevelUI()
    {
        if (levelText == null || levelProgressBarFill == null) return;

        LevelProfile currentLevel = GetCurrentLevelProfile();
        int currentLevelNumber = (totalBossesDefeated % levelProfiles.Length) + 1; // 1-6

        // Aggiorna testo level
        levelText.text = $"level {currentLevelNumber}/{levelProfiles.Length}";

        // Aggiorna barra di progresso
        levelProgressBarFill.fillAmount = progress;

        //// Cambia colore in base al progresso (verde → giallo → rosso)
        //Color barColor;
        //if (progress < 0.5f)
        //{
        //    // 0% - 50%: Verde → Giallo
        //    barColor = Color.Lerp(barColorStart, barColorMid, progress * 2f);
        //}
        //else
        //{
        //    // 50% - 100%: Giallo → Rosso
        //    barColor = Color.Lerp(barColorMid, barColorEnd, (progress - 0.5f) * 2f);
        //}
        //levelProgressBarFill.color = barColor;
    }

    /// <summary>
    /// Aggiorna direttamente il fillAmount (usato da BossHealthBar per smooth transition)
    /// </summary>
    public void UpdateBossHealthDirect(float fillAmount)
    {
        if (bossHealthBarFill != null && isBossFight)
        {
            bossHealthBarFill.fillAmount = fillAmount;

            //// Cambia colore in base alla vita
            //if (fillAmount > 0.5f)
            //    bossHealthBarFill.color = new Color(1f, 0.2f, 0.2f); // Rosso
            //else if (fillAmount > 0.25f)
            //    bossHealthBarFill.color = new Color(1f, 0.5f, 0f); // Arancione
            //else
            //    bossHealthBarFill.color = new Color(1f, 1f, 0f); // Giallo (critico)
        }
    }

    /// <summary>
    /// Imposta il nome del boss nella UI
    /// </summary>
    public void SetBossName(string name)
    {
        if (bossNameText != null)
        {
            bossNameText.text = name;
        }
    }

    /// <summary>
    /// Verifica se è attivo un boss fight
    /// </summary>
    public bool IsBossFightActive()
    {
        return isBossFight;
    }

    void OnGUI()
    {
        if (!Application.isPlaying) return;

        LevelProfile currentLevel = GetCurrentLevelProfile();

        GUILayout.Label($"Level: {currentLevel.levelName}");
        GUILayout.Label($"Level Time: {levelTime:F1}s");
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

        // Ferma spawn asteroidi
        AsteroidSpawner spawner = FindFirstObjectByType<AsteroidSpawner>();
        if (spawner != null) spawner.enabled = false;

        // Ferma EnemySpawner 
        EnemySpawner enemySpawner = FindFirstObjectByType<EnemySpawner>();
        if (enemySpawner != null) enemySpawner.enabled = false;

        yield return new WaitForSeconds(transitionDuration);

        // Spawna il boss corrente con posizione e rotazione corrette
        if (bossIndex < bossPrefabs.Length && bossPrefabs[bossIndex] != null)
        {
            float cameraTop = Camera.main.orthographicSize;
            Vector3 spawnPos = new Vector3(0, cameraTop *1.1f, 0);
            GameObject bossInstance = Instantiate(bossPrefabs[bossIndex], spawnPos, bossPrefabs[bossIndex].transform.rotation);
        }

        // Attiva il boss fight mode
        isBossFight = true;
        isInTransition = false;
    }

    public void OnBossDefeated()
    {
        if (RunStats.Instance != null)
            RunStats.Instance.RegisterBossKilled();

        // Disattiva boss fight mode
        isBossFight = false;
        bossIndex++;
        totalBossesDefeated++; // Incrementa il contatore totale

        //// Riattiva UI level
        //if (levelText != null) levelText.gameObject.SetActive(true);
        //if (levelNameText != null) levelNameText.gameObject.SetActive(true);
        //if (levelProgressBarFill != null) levelProgressBarFill.transform.parent.gameObject.SetActive(true);

        // TODO: game finale??
        // Se hai battuto tutti e 6 i boss, ricomincia loop con difficoltà aumentata
        if (bossIndex >= bossPrefabs.Length)
        {
            bossIndex = 0;
            globalDifficultyMultiplier += difficultyIncreasePerLoop; // +50% difficoltà ogni loop
        }

        // Resetta level
        levelTime = 0f;
        progress = 0f;

        // Transizione ritardata con UI Level che appare dopo
        StartCoroutine(BossDefeatedTransition()); // normale transizione
    }

    IEnumerator BossDefeatedTransition()
    {
        isInTransition = true;

        // Forza la barra boss a 0 (animazione morte)
        if (bossHealthBarFill != null)
        {
            float duration = 0.2f;
            float elapsed = 0f;
            float startFill = bossHealthBarFill.fillAmount;

            while (elapsed < duration)
            {
                bossHealthBarFill.fillAmount = Mathf.Lerp(startFill, 0f, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            bossHealthBarFill.fillAmount = 0f; // Assicura che arrivi esattamente a 0
        }

        // Aspetta qualche secondo prima di mostrare la level bar
        yield return new WaitForSeconds(3f); 

        // Mostra Level UI con il numero corretto (totalBossesDefeated è già aggiornato)
        ShowLevelUI();

        // Riattiva AsteroidSpawner DOPO che la level bar è apparsa
        AsteroidSpawner spawner = FindFirstObjectByType<AsteroidSpawner>();
        if (spawner != null) spawner.enabled = true;

        // Riattiva EnemySpawner 
        EnemySpawner enemySpawner = FindFirstObjectByType<EnemySpawner>();
        if (enemySpawner != null) enemySpawner.enabled = true;

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
            float cameraTop = Camera.main.orthographicSize;
            Vector3 spawnPos = new Vector3(0, cameraTop *1.1f, 0);
            Instantiate(bossPrefabs[bossIndex], spawnPos, bossPrefabs[bossIndex].transform.rotation);   // mantengo rotazione del prefab
        }

        // Attiva il boss fight mode
        isBossFight = true;

        //// Opzionale: nascondi il pannello di level complete se è attivo
        //if (levelCompletePanel != null)
        //    levelCompletePanel.SetActive(false);
    }

    #endregion

    #region Public Getters for Spawners

    /// <summary>
    /// Ritorna la fase corrente della level
    /// (1, 2 o 3) in base al progress della level
    /// </summary>
    public int GetCurrentPhase()
    {
        // FOR DEBUG:
        //return 3;

        if (progress < 0.33f) return 1; // Fase 1: 0-20s
        if (progress < 0.66f) return 2; // Fase 2: 20-40s
        return 3;                       // Fase 3: 40-60s
    }

    #endregion
}