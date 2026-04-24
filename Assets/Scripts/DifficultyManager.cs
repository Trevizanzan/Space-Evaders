using System.Collections;
using TMPro;
using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance;

    [Header("Sequence")]
    [SerializeField] private GameSequence gameSequence; // Trascina qui il tuo GameSequence asset

    [Header("Level Settings")]
    [SerializeField] private float transitionDuration = 3f; // pausa tra un level e l'altro, e tra boss e level

    [Header("Difficulty Scaling")]
    [SerializeField] private float globalDifficultyMultiplier = 1f;
    //[SerializeField] private float difficultyIncreasePerLoop = 0.5f; // +50% ogni loop completo

    [Header("Meta-Progression")]
    [SerializeField] private WeaponData spreadGunWeapon;
    [SerializeField] private WeaponData railgunWeapon;
    [SerializeField] private PerkSelectionOverlay perkOverlay;

    [Header("Debug")]
    [SerializeField] private bool skipToFirstBoss = false;
    [SerializeField] private bool debugSpecificLevel = false;
    [SerializeField] private int debugLevelIndex = 0; // Quale level testare (0-based)

    [Header("Top Bar - Unified Progress Bar")]
    [Tooltip("Image Fill della barra unificata (verde in level, rossa in boss). Usata sia per level progress che per boss HP.")]
    [SerializeField] private UnityEngine.UI.Image levelProgressBarFill;
    [Tooltip("Stessa Image di levelProgressBarFill - mantenuta per retrocompatibilità con chiamate esistenti.")]
    [SerializeField] private UnityEngine.UI.Image bossHealthBarFill;
    [Tooltip("Testo piccolo al bordo destro della barra: numero livello in level, nome boss durante boss fight.")]
    [SerializeField] private TMP_Text barRightText;

    [Header("Top Bar - Deprecated (disabled in scene)")]
    [SerializeField] private GameObject levelInfoGroup;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private GameObject bossInfoGroup;
    [SerializeField] private TMP_Text bossNameText;

    // Palette ufficiale: Blu #2F68DC (level), Rosa #FB4F69 (boss), Giallo #FAD946 (flash)
    private static readonly Color BarColorLevel = new Color(0.184f, 0.408f, 0.863f);
    private static readonly Color BarColorBoss  = new Color(0.984f, 0.310f, 0.412f);
    private static readonly Color BarColorFlash = new Color(0.980f, 0.851f, 0.275f);

//    |---|---|---|
//| Nero | `#000000` | Sfondo spazio, ombre profonde |
//| Viola scuro | `#2A0E54` | Sfondo nebula, UI dark |
//| Magenta | `#AA1E65` | Nemici, accenti ostili |
//| Rosa/Rosso | `#FB4F69` | Pericolo, proiettili nemici, HP basso |
//| Bianco caldo | `#F9F7F7` | Testi, highlight neutri |
//| Arancione | `#FC8141` | Esplosioni, energia, thruster |
//| Giallo | `#FAD946` | Score, loot, accenti dorati |
//| Blu | `#2F68DC` | Player, proiettili player, UI primaria |
//| Ciano | `#46E7EC` | Armi speciali, charge effect, UI secondaria |


    // State
    private float levelTime = 0f;
    private float progress = 0f;
    private bool isInTransition = false;
    private bool isBossFight = false;
    private int currentLevelIndex = 0;
    //private int loopCount = 0;

    // Events
    public System.Action OnLevelComplete;
    public System.Action OnWaveComplete; // mantenuto per compatibilità con altri script

    // ══════════════════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ══════════════════════════════════════════════════════════════════════════

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (DifficultyManager.Instance != null)
            DifficultyManager.Instance.OnWaveComplete += ShowLevelComplete;

        ShowLevelUI();

        if (skipToFirstBoss)
            StartCoroutine(DebugSkipToBoss());
        else if (debugSpecificLevel)
            StartCoroutine(DebugSkipToLevel());

        ApplyLevelConstraints();
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver()) return;
        if (isInTransition) return;
        if (isBossFight) return;
        if (EnemySpawner.IsDebugMode || AsteroidSpawner.IsDebugMode) return;

        // Solo se siamo in un Level normale (non Boss) aggiorniamo il timer e la progress bar
        if (GetCurrentLevel() == null || GetCurrentLevel().isBoss == true) return;

        levelTime += Time.deltaTime;

        LevelProfile currentLevel = GetCurrentLevel();
        if (currentLevel == null) return;

        progress = Mathf.Clamp01(levelTime / currentLevel.levelDuration);

        UpdateLevelUI();

        if (levelTime >= currentLevel.levelDuration)
            StartCoroutine(LevelTransition());
    }

    // ══════════════════════════════════════════════════════════════════════════
    // SEQUENCE NAVIGATION
    // ══════════════════════════════════════════════════════════════════════════

    public LevelProfile GetCurrentLevel()
    {
        if (gameSequence == null || gameSequence.levels == null || gameSequence.levels.Length == 0)
            return null;
        return gameSequence.levels[currentLevelIndex % gameSequence.levels.Length];
    }

    // Compatibilità con AsteroidSpawner / EnemySpawner che chiamano GetCurrentWaveProfile()
    public LevelProfile GetCurrentWaveProfile() => GetCurrentLevel();

    public int GetCurrentPhase()
    {
        LevelProfile level = GetCurrentLevel();
        if (level == null) return 1;

        float third = level.levelDuration / 3f;
        if (levelTime < third) return 1;
        if (levelTime < third * 2f) return 2;
        return 3;
    }

    /// <summary>
    /// TODO: cosa fa l'if? da togliere per loop infinito.
    /// Al momento serve per evitare di fare il loop completo durante lo sviluppo, ma una volta che la sequenza è completa e bilanciata, si può togliere per farla ripartire all'infinito aumentando sempre di più la difficoltà.
    /// </summary>
    void AdvanceToNextLevel()
    {
        // PRIMA di incrementare currentLevelIndex: registra le stats del livello appena completato, che si riferiscono al level corrente (quello che sta per finire, non quello che sta per iniziare)
        if (StatsRecorder.Instance != null)
            StatsRecorder.Instance.OnLevelEnded(completed: true);

        currentLevelIndex++;

        ApplyLevelConstraints();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // TRANSITIONS
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gestisce la transizione da un level al successivo (Level→Boss o Boss→Level)
    /// </summary>
    IEnumerator LevelTransition()
    {
        isInTransition = true;
        OnLevelComplete?.Invoke();
        OnWaveComplete?.Invoke();

        // Ferma spawner
        AsteroidSpawner asteroidSpawner = FindFirstObjectByType<AsteroidSpawner>();
        if (asteroidSpawner != null) asteroidSpawner.enabled = false;

        EnemySpawner enemySpawner = FindFirstObjectByType<EnemySpawner>();
        if (enemySpawner != null) enemySpawner.enabled = false;

        // Aspetta che la scena sia vuota
        yield return StartCoroutine(WaitForSceneClear());

        // Pausa prima del boss
        yield return new WaitForSeconds(transitionDuration);

        AdvanceToNextLevel();

        LevelProfile nextLevel = GetCurrentLevel();
        if (nextLevel.isBoss)
            StartBossFight(nextLevel);
        else
            StartLevel();

        isInTransition = false;
    }

    /// <summary>
    /// Questo metodo aspetta che non ci siano più asteroidi o nemici in scena prima di continuare, per evitare di farli sparire magicamente.
    /// </summary>
    IEnumerator WaitForSceneClear()
    {
        float timeout = 10f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            bool hasAsteroids = GameObject.FindGameObjectsWithTag("Asteroid").Length > 0;
            bool hasEnemies = GameObject.FindGameObjectsWithTag("Enemy").Length > 0;

            if (!hasAsteroids && !hasEnemies) yield break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Failsafe: dopo 10s distruggi tutto quello che è rimasto
        foreach (var a in GameObject.FindGameObjectsWithTag("Asteroid"))
            Destroy(a);
        //foreach (var e in GameObject.FindGameObjectsWithTag("Enemy"))
        //    Destroy(e);
    }

    void StartLevel()
    {
        levelTime = 0f;
        progress = 0f;
        isBossFight = false;

        ShowLevelUI();

        // registra inizio livello (dopo la pausa e dopo che la scena è pulita, per avere dati più accurati)
        if (StatsRecorder.Instance != null)
            StatsRecorder.Instance.OnLevelStarted();

        // Riattiva spawner
        AsteroidSpawner asteroidSpawner = FindFirstObjectByType<AsteroidSpawner>();
        if (asteroidSpawner != null) asteroidSpawner.enabled = true;

        EnemySpawner enemySpawner = FindFirstObjectByType<EnemySpawner>();
        if (enemySpawner != null) enemySpawner.enabled = true;

        LevelProfile level = GetCurrentLevel();
        Debug.Log($"[DifficultyManager] Iniziato Level: {(level != null ? level.levelName : "NULL")}");
    }

    void StartBossFight(LevelProfile level)
    {
        isBossFight = true;

        if (level.bossPrefab != null)
        {
            float cameraTop = Camera.main.orthographicSize;
            Vector3 spawnPos = new Vector3(0, cameraTop * 1.1f, 0);
            Instantiate(level.bossPrefab, spawnPos, level.bossPrefab.transform.rotation);
        }

        ShowBossUI();
        //Debug.Log($"[DifficultyManager] Boss fight iniziato!");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // BOSS DEFEATED
    // ══════════════════════════════════════════════════════════════════════════

    public void OnBossDefeated()
    {
        if (RunStats.Instance != null)
            RunStats.Instance.RegisterBossKilled();

        int lifetimeBossKills = UnlockManager.IncrementAndGetLifetimeBossKills();
        UnlockManager.CheckWeaponUnlocks(lifetimeBossKills, spreadGunWeapon, railgunWeapon);

        isBossFight = false;

        StartCoroutine(BossDefeatedTransition());
    }

    /// <summary>
    /// Questo metodo gestisce la transizione dopo la sconfitta di un boss: anima la barra del boss a 0, aspetta qualche secondo, e poi avanza al prossimo level (che potrebbe essere un altro boss o un level normale)
    /// </summary>
    /// <returns></returns>
    IEnumerator BossDefeatedTransition()
    {
        isInTransition = true;

        // Anima barra boss a 0
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

            bossHealthBarFill.fillAmount = 0f;
        }

        if (perkOverlay != null)
        {
            perkOverlay.gameObject.SetActive(true);
            yield return new WaitUntil(() => perkOverlay.IsDone);
        }
        else
            yield return new WaitForSeconds(3f);

        // Avanza al prossimo level
        AdvanceToNextLevel();

        LevelProfile nextLevel = GetCurrentLevel();

        if (nextLevel.isBoss)
        {
            // Due boss di fila
            StartBossFight(nextLevel);
        }
        else
        {
            StartLevel();
        }

        isInTransition = false;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // UI
    // ══════════════════════════════════════════════════════════════════════════

    void ShowLevelUI()
    {
        // LevelInfoGroup/BossInfoGroup sono disabilitati permanentemente in scena (barra unificata).
        // Qui basta tintare la barra verde e aggiornare il testo destro al numero livello.

        if (levelProgressBarFill != null)
        {
            levelProgressBarFill.fillAmount = 0f;
            levelProgressBarFill.color = BarColorLevel;
        }

        UpdateBarRightText(); // numero livello
    }

    public void ShowBossUI()
    {
        if (bossHealthBarFill != null)
        {
            bossHealthBarFill.fillAmount = 1f;
            bossHealthBarFill.color = BarColorBoss;
        }
        // barRightText viene impostato da BossBase via SetBossName()
    }

    void UpdateLevelUI()
    {
        if (levelProgressBarFill == null) return;

        UpdateBarRightText();
        levelProgressBarFill.fillAmount = progress;
    }

    void UpdateBarRightText()
    {
        if (barRightText == null || gameSequence == null || gameSequence.levels == null) return;

        // Conta i Level (non Boss) fino al currentLevelIndex compreso
        int idx = currentLevelIndex % gameSequence.levels.Length;
        int currentLevelNumber = 0;
        for (int i = 0; i <= idx; i++)
        {
            if (!gameSequence.levels[i].isBoss)
                currentLevelNumber++;
        }

        barRightText.text = currentLevelNumber.ToString();
    }

    void ShowLevelComplete()
    {
        StartCoroutine(LevelCompleteRoutine());
    }

    IEnumerator LevelCompleteRoutine()
    {
        if (levelProgressBarFill != null)
        {
            levelProgressBarFill.fillAmount = 1f;

            for (int pulse = 0; pulse < 3; pulse++)
            {
                levelProgressBarFill.color = BarColorFlash;
                yield return new WaitForSeconds(0.15f);
                levelProgressBarFill.color = Color.white;
                yield return new WaitForSeconds(0.15f);
            }

            levelProgressBarFill.color = BarColorLevel;
        }
    }

    public void UpdateBossHealthDirect(float fillAmount)
    {
        if (bossHealthBarFill != null && isBossFight)
            bossHealthBarFill.fillAmount = fillAmount;
    }

    public void SetBossName(string name)
    {
        if (barRightText != null)
            barRightText.text = name;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // PUBLIC GETTERS
    // ══════════════════════════════════════════════════════════════════════════

    public bool IsBossFightActive() => isBossFight;
    public float GetGlobalMultiplier() => globalDifficultyMultiplier;

    // ══════════════════════════════════════════════════════════════════════════
    // DEBUG
    // ══════════════════════════════════════════════════════════════════════════

    IEnumerator DebugSkipToBoss()
    {
        yield return null;

        AsteroidSpawner spawner = FindFirstObjectByType<AsteroidSpawner>();
        if (spawner != null) spawner.enabled = false;

        // Trova il primo Boss nella sequenza
        for (int i = 0; i < gameSequence.levels.Length; i++)
        {
            if (gameSequence.levels[i].isBoss)
            {
                currentLevelIndex = i;
                break;
            }
        }

        StartBossFight(GetCurrentLevel());
        ApplyLevelConstraints();
    }

    IEnumerator DebugSkipToLevel()
    {
        yield return null;

        currentLevelIndex = Mathf.Clamp(debugLevelIndex, 0, gameSequence.levels.Length - 1);

        Debug.Log($"[DEBUG] Debugging level {currentLevelIndex}: type {(GetCurrentLevel().isBoss ? "Boss" : "Level")}");

        if (GetCurrentLevel().isBoss)
        {
            AsteroidSpawner spawner = FindFirstObjectByType<AsteroidSpawner>();
            if (spawner != null) spawner.enabled = false;

            EnemySpawner enemySpawner = FindFirstObjectByType<EnemySpawner>();
            if (enemySpawner != null) enemySpawner.enabled = false;

            StartBossFight(GetCurrentLevel());
            ApplyLevelConstraints();
        }
        else
        {
            levelTime = 0f;
            progress = 0f;
            ShowLevelUI();
            ApplyLevelConstraints();
        }
    }

    void ApplyLevelConstraints()
    {
        LevelProfile level = GetCurrentLevel();
        if (level == null) return;

        PlayerShooting shooting = FindFirstObjectByType<PlayerShooting>();
        if (shooting != null)
            shooting.SetShootingDisabled(level.disableShooting);
    }

#if UNITY_EDITOR
    //void OnGUI()
    //{
    //    if (!Application.isPlaying) return;

    //    LevelProfile level = GetCurrentLevel();

    //    GUILayout.Label($"Level: {currentLevelIndex} ({(level != null ? (level.isBoss ? "Boss" : "Level") : "NULL")})");
    //    GUILayout.Label($"Level: {(level != null ? level.levelName : "Boss Fight")}");
    //    GUILayout.Label($"Level Time: {levelTime:F1}s");
    //    GUILayout.Label($"Progress: {progress:F2}");
    //    GUILayout.Label($"Phase: {GetCurrentPhase()}");
    //    //GUILayout.Label($"Loop: {loopCount}");
    //    GUILayout.Label($"Global Multiplier: {globalDifficultyMultiplier:F2}x");
    //    GUILayout.Label($"Boss Fight: {isBossFight}");
    //}
#endif

    public int GetCurrentLevelIndex() => currentLevelIndex;
    //public int GetLoopCount() => loopCount;
}