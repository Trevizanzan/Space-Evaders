using System.Collections;
using TMPro;
using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance;

    [Header("Sequence")]
    [SerializeField] private GameSequence gameSequence; // Trascina qui il tuo GameSequence asset

    [Header("Level Settings")]
    [SerializeField] private float transitionDuration = 3f; // pausa tra level e boss

    [Header("Difficulty Scaling")]
    [SerializeField] private float globalDifficultyMultiplier = 1f;
    [SerializeField] private float difficultyIncreasePerLoop = 0.5f; // +50% ogni loop completo

    [Header("Debug")]
    [SerializeField] private bool skipToFirstBoss = false;
    [SerializeField] private bool debugSpecificStep = false;
    [SerializeField] private int debugStepIndex = 0; // Quale step testare (0-based)

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
    private int currentStepIndex = 0;
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
        else if (debugSpecificStep)
            StartCoroutine(DebugSkipToStep());
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver()) return;
        if (isInTransition) return;
        if (isBossFight) return;
        if (EnemySpawner.IsDebugMode || AsteroidSpawner.IsDebugMode) return;

        // Solo se lo step corrente è un Level
        if (GetCurrentStep() == null || GetCurrentStep().type != StepType.Level) return;

        levelTime += Time.deltaTime;

        LevelProfile currentLevel = GetCurrentLevelProfile();
        if (currentLevel == null) return;

        progress = Mathf.Clamp01(levelTime / currentLevel.levelDuration);

        UpdateLevelUI();

        if (levelTime >= currentLevel.levelDuration)
            StartCoroutine(StepTransition());
    }

    // ══════════════════════════════════════════════════════════════════════════
    // SEQUENCE NAVIGATION
    // ══════════════════════════════════════════════════════════════════════════

    public SequenceStep GetCurrentStep()
    {
        if (gameSequence == null || gameSequence.steps == null || gameSequence.steps.Length == 0)
        {
            Debug.LogWarning("[DifficultyManager] GameSequence non assegnata o vuota!");
            return null;
        }

        int index = currentStepIndex % gameSequence.steps.Length;
        return gameSequence.steps[index];
    }

    public LevelProfile GetCurrentLevelProfile()
    {
        SequenceStep step = GetCurrentStep();
        if (step == null || step.type != StepType.Level) return null;
        return step.levelProfile;
    }

    // Compatibilità con AsteroidSpawner / EnemySpawner che chiamano GetCurrentWaveProfile()
    public LevelProfile GetCurrentWaveProfile() => GetCurrentLevelProfile();

    public int GetCurrentPhase()
    {
        LevelProfile level = GetCurrentLevelProfile();
        if (level == null) return 1;

        float third = level.levelDuration / 3f;
        if (levelTime < third) return 1;
        if (levelTime < third * 2f) return 2;
        return 3;
    }

    void AdvanceToNextStep()
    {
        currentStepIndex++;

        if (currentStepIndex >= gameSequence.steps.Length)
        {
            // Loop completo: ricomincia con difficoltà aumentata
            currentStepIndex = 0;
            loopCount++;
            globalDifficultyMultiplier += difficultyIncreasePerLoop;
            Debug.Log($"[DifficultyManager] Loop {loopCount} iniziato! Difficoltà: {globalDifficultyMultiplier:F2}x");
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // TRANSITIONS
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gestisce la transizione da uno step al successivo (Level→Boss o Boss→Level)
    /// </summary>
    // Sostituisci StepTransition in DifficultyManager.cs con questo:

    IEnumerator StepTransition()
    {
        isInTransition = true;
        OnLevelComplete?.Invoke();
        OnWaveComplete?.Invoke();

        // Aspetta che l'animazione gold della barra finisca (~1 secondo: 3 pulse x 0.3s)
        yield return new WaitForSeconds(1f);

        // Ferma spawner
        AsteroidSpawner spawner = FindFirstObjectByType<AsteroidSpawner>();
        if (spawner != null) spawner.enabled = false;

        EnemySpawner enemySpawner = FindFirstObjectByType<EnemySpawner>();
        if (enemySpawner != null) enemySpawner.enabled = false;

        // Pausa prima del boss
        yield return new WaitForSeconds(transitionDuration);

        AdvanceToNextStep();

        SequenceStep nextStep = GetCurrentStep();

        if (nextStep.type == StepType.Boss)
            StartBossFight(nextStep);
        else
            StartLevel();

        isInTransition = false;
    }

    void StartLevel()
    {
        levelTime = 0f;
        progress = 0f;
        isBossFight = false;

        ShowLevelUI();

        // Riattiva spawner
        AsteroidSpawner spawner = FindFirstObjectByType<AsteroidSpawner>();
        if (spawner != null) spawner.enabled = true;

        EnemySpawner enemySpawner = FindFirstObjectByType<EnemySpawner>();
        if (enemySpawner != null) enemySpawner.enabled = true;

        LevelProfile level = GetCurrentLevelProfile();
        Debug.Log($"[DifficultyManager] Iniziato Level: {(level != null ? level.levelName : "NULL")}");
    }

    void StartBossFight(SequenceStep step)
    {
        isBossFight = true;

        if (step.bossPrefab != null)
        {
            float cameraTop = Camera.main.orthographicSize;
            Vector3 spawnPos = new Vector3(0, cameraTop * 1.1f, 0);
            Instantiate(step.bossPrefab, spawnPos, step.bossPrefab.transform.rotation);
        }
        else
        {
            Debug.LogWarning("[DifficultyManager] Step Boss senza bossPrefab assegnato!");
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

        // Avanza al prossimo step
        AdvanceToNextStep();

        SequenceStep nextStep = GetCurrentStep();

        if (nextStep.type == StepType.Boss)
        {
            // Due boss di fila
            StartBossFight(nextStep);
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
        for (int i = 0; i < gameSequence.steps.Length; i++)
        {
            if (gameSequence.steps[i].type == StepType.Level)
            {
                totalLevels++;
                if (i <= currentStepIndex % gameSequence.steps.Length)
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

        // Trova il primo step Boss nella sequenza
        for (int i = 0; i < gameSequence.steps.Length; i++)
        {
            if (gameSequence.steps[i].type == StepType.Boss)
            {
                currentStepIndex = i;
                break;
            }
        }

        StartBossFight(GetCurrentStep());
    }

    IEnumerator DebugSkipToStep()
    {
        yield return null;

        currentStepIndex = Mathf.Clamp(debugStepIndex, 0, gameSequence.steps.Length - 1);

        Debug.Log($"[DEBUG] Saltato allo step {currentStepIndex}: {GetCurrentStep().type}");

        if (GetCurrentStep().type == StepType.Boss)
        {
            AsteroidSpawner spawner = FindFirstObjectByType<AsteroidSpawner>();
            if (spawner != null) spawner.enabled = false;

            EnemySpawner enemySpawner = FindFirstObjectByType<EnemySpawner>();
            if (enemySpawner != null) enemySpawner.enabled = false;

            StartBossFight(GetCurrentStep());
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

        SequenceStep step = GetCurrentStep();
        LevelProfile level = GetCurrentLevelProfile();

        GUILayout.Label($"Step: {currentStepIndex} ({(step != null ? step.type.ToString() : "NULL")})");
        GUILayout.Label($"Level: {(level != null ? level.levelName : "Boss Fight")}");
        GUILayout.Label($"Level Time: {levelTime:F1}s");
        GUILayout.Label($"Progress: {progress:F2}");
        GUILayout.Label($"Phase: {GetCurrentPhase()}");
        GUILayout.Label($"Loop: {loopCount}");
        GUILayout.Label($"Global Multiplier: {globalDifficultyMultiplier:F2}x");
        GUILayout.Label($"Boss Fight: {isBossFight}");
    }
}