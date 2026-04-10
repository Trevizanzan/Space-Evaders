using UnityEngine;

public enum StepType
{
    Level,
    Boss
}

[CreateAssetMenu(fileName = "NewSequenceStep", menuName = "Game/Sequence Step")]
public class SequenceStep : ScriptableObject
{
    public StepType type = StepType.Level;

    [Tooltip("Usato se type == Level")]
    public LevelProfile levelProfile;

    [Tooltip("Usato se type == Boss")]
    public GameObject bossPrefab;
}