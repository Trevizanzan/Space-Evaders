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
    public float speedMultiplier = 1f;
    public float healthMultiplier = 1f;

    [Header("Asteroid Size Focus")]
    [Tooltip("0 = tutti, 1 = solo piccoli, 2 = solo medi, 3 = solo grandi")]
    public int asteroidSizeFocus = 0;
}

[CreateAssetMenu(fileName = "NewLevelProfile", menuName = "Game/Level Profile")]
public class LevelProfile : ScriptableObject
{
    [Header("Level Info")]
    public string levelName = "Default Level";
    public float levelDuration = 30f;

    [Header("Phase Configurations")]
    public PhaseConfig phase1 = new PhaseConfig();
    public PhaseConfig phase2 = new PhaseConfig();
    public PhaseConfig phase3 = new PhaseConfig();

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