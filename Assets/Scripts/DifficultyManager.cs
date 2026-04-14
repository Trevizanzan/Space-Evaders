using System.Collections;
using TMPro;
using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance;

    [Header("Sequence")]
    [SerializeField] private GameSequence gameSequence; // Trascina qui il tuo GameSequence asset

    [Header("Level Settings")]
    [SerializeField] private float transitionDuration = 5f; // pausa tra level e boss

    [Header("Difficulty Scaling")]
    [SerializeField] private float globalDifficultyMultiplier = 1f;
    [SerializeField] private float difficultyIncreasePerLoop = 0.5f; // +50% ogni loop completo

    [Header("Debug")]
    [SerializeField] private bool skipToFirstBoss = false;
    [SerializeField] private bool debugSpecificLevel = false;
    [SerializeField] private int debugLevelIndex = 0; // Quale level testare (0-based)

    [Header("Level UI - Top Bar")]
    [SerializeField] private GameObject levelInfoGroup;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private UnityEngine.UI.Image levelProgressBarFill;

    [Header("Boss UI - Top Bar")]
    [SerializeField] private GameObject bossInfoGroup;
    [SerializeField] private TMP_Text bossNameText;
    [SerializeField] private UnityEngine.UI.Image bossHealthBarFill;

    [Header("Progress Bar Colors")]
    [SerializeField] private Color barColorStart = new Color(0.2f, 1f, 0.3f);
    [SerializeField] private Color barColorMid = new Color(1f, 0.9f, 0.2f);
    [SerializeField] private Color barColorEnd = new Color(1f, 0.3f, 0.2f);
    [SerializeField] private Color barColorGold = new Color(1f, 0.85f, 0f);

    // State
    private float levelTime = 0f;
    private float progress = 0f;
    private bool isInTransition = false;
    private bool isBossFight = false;
    private int currentLevelIndex = 0;
    private int loopCount = 0;

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
        currentLevelIndex++;

        if (currentLevelIndex >= gameSequence.levels.Length)
        {
            // Loop completo: ricomincia con difficoltà aumentata
            currentLevelIndex = 0;
            loopCount++;
            globalDifficultyMultiplier += difficultyIncreasePerLoop;
            Debug.Log($"[DifficultyManager] Loop {loopCount} iniziato! Difficoltà: {globalDifficultyMultiplier:F2}x");
        }
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
        float timeout = 30f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            bool hasAsteroids = GameObject.FindGameObjectsWithTag("Asteroid").Length > 0;
            bool hasEnemies = GameObject.FindGameObjectsWithTag("Enemy").Length > 0;

            if (!hasAsteroids && !hasEnemies) yield break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Failsafe: dopo 30s distruggi tutto quello che è rimasto
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
        else
        {
            Debug.LogWarning("[DifficultyManager] Level Boss senza bossPrefab assegnato!");
        }

        ShowBossUI();
        Debug.Log($"[DifficultyManager] Boss fight iniziato!");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // BOSS DEFEATED
    // ══════════════════════════════════════════════════════════════════════════

    public void OnBossDefeated()
    {
        if (RunStats.Instance != null)
            RunStats.Instance.RegisterBossKilled();

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
        if (levelInfoGroup != null) levelInfoGroup.SetActive(true);
        if (bossInfoGroup != null) bossInfoGroup.SetActive(false);

        if (levelProgressBarFill != null)
        {
            levelProgressBarFill.fillAmount = 0f;
            levelProgressBarFill.color = barColorStart;
        }
    }

    public void ShowBossUI()
    {
        if (levelInfoGroup != null) levelInfoGroup.SetActive(false);
        if (bossInfoGroup != null) bossInfoGroup.SetActive(true);

        if (bossHealthBarFill != null)
        {
            bossHealthBarFill.fillAmount = 1f;
            bossHealthBarFill.color = new Color(1f, 0.2f, 0.2f);
        }
    }

    void UpdateLevelUI()
    {
        if (levelText == null || levelProgressBarFill == null) return;

        // Conta quanti Level ci sono nella sequenza per mostrare "LEVEL X/Y"
        int totalLevels = 0;
        int currentLevelNumber = 0;
        for (int i = 0; i < gameSequence.levels.Length; i++)
        {
            if (!gameSequence.levels[i].isBoss)  // Conta solo i Level, salta i Boss
            {
                totalLevels++;
                if (i <= currentLevelIndex % gameSequence.levels.Length)
                    currentLevelNumber = totalLevels;
            }
        }

        levelText.text = $"LEVEL {currentLevelNumber}/{totalLevels}";
        levelProgressBarFill.fillAmount = progress;
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
                levelProgressBarFill.color = barColorGold;
                yield return new WaitForSeconds(0.15f);
                levelProgressBarFill.color = Color.white;
                yield return new WaitForSeconds(0.15f);
            }

            levelProgressBarFill.color = barColorGold;
        }
    }

    public void UpdateBossHealthDirect(float fillAmount)
    {
        if (bossHealthBarFill != null && isBossFight)
            bossHealthBarFill.fillAmount = fillAmount;
    }

    public void SetBossName(string name)
    {
        if (bossNameText != null)
            bossNameText.text = name;
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
        }
        else
        {
            levelTime = 0f;
            progress = 0f;
            ShowLevelUI();
        }
    }

    void OnGUI()
    {
        if (!Application.isPlaying) return;

        LevelProfile level = GetCurrentLevel();

        GUILayout.Label($"Level: {currentLevelIndex} ({(level != null ? (level.isBoss ? "Boss" : "Level") : "NULL")})");
        GUILayout.Label($"Level: {(level != null ? level.levelName : "Boss Fight")}");
        GUILayout.Label($"Level Time: {levelTime:F1}s");
        GUILayout.Label($"Progress: {progress:F2}");
        GUILayout.Label($"Phase: {GetCurrentPhase()}");
        GUILayout.Label($"Loop: {loopCount}");
        GUILayout.Label($"Global Multiplier: {globalDifficultyMultiplier:F2}x");
        GUILayout.Label($"Boss Fight: {isBossFight}");
    }
}