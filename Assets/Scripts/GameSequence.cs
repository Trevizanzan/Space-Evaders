using UnityEngine;

[CreateAssetMenu(fileName = "NewGameSequence", menuName = "Game/Game Sequence")]
public class GameSequence : ScriptableObject
{
    [Tooltip("Lista ordinata degli step della run. Ogni step è un Level o un Boss.")]
    public SequenceStep[] steps;
}