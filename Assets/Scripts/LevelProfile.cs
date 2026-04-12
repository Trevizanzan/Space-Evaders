using UnityEngine;

[System.Serializable]
public class PhaseConfig
{
    [Header("Design Notes")]
    [TextArea(3, 10)]
    public string designNotes = "";

    [Header("_____________ ASTEROIDS _____________")]
    [Header("Normal Asteroids")]
    public bool spawnNormal = false;
    public float normalSpawnInterval = 2f;
    public float normalSpeed = 8f;
    [Tooltip("0 = mix pesato, 1 = solo piccoli, 2 = solo medi, 3 = solo grandi")]
    public int normalSizeDistribution = 0;

    [Header("Diagonal Asteroids")]
    public bool spawnDiagonal = false;
    public float diagonalSpawnInterval = 3f;
    public float diagonalSpeed = 9f;
    [Tooltip("0 = mix pesato, 1 = solo piccoli, 2 = solo medi, 3 = solo grandi")]
    public int diagonalSizeDistribution = 0;

    [Header("Horizontal Asteroids")]
    public bool spawnHorizontal = false;
    public float horizontalSpawnInterval = 4f;
    public float horizontalSpeed = 11f;
    [Tooltip("0 = mix pesato, 1 = solo piccoli, 2 = solo medi, 3 = solo grandi")]
    public int horizontalSizeDistribution = 0;

    [Header("Asteroid Health")]
    public float healthMultiplier = 1f;

    [Header("_____________ ENEMIES _____________")]
    [Header("Fighters")]
    public bool allowFighters = false;
    public float fighterSpawnInterval = 5f;

    [Header("Kamikazes")]
    public bool allowKamikazes = false;
    public float kamikazeSpawnInterval = 4f;

    [Header("Bombers")]
    public bool allowBombers = false;
    public float bomberSpawnInterval = 8f;

    [Header("Pulsars")]
    public bool allowPulsars = false;
    public float pulsarSpawnInterval = 10f;
}

[CreateAssetMenu(fileName = "NewLevelProfile", menuName = "Game/Level Profile")]
public class LevelProfile : ScriptableObject
{
    [Header("Step Type")]
    public bool isBoss = false;
    [Tooltip("Usato solo se isBoss = true")]
    public GameObject bossPrefab;

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