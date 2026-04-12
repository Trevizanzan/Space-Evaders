using UnityEngine;

[CreateAssetMenu(fileName = "NewGameSequence", menuName = "Game/Game Sequence")]
public class GameSequence : ScriptableObject
{
    public LevelProfile[] levels;
}