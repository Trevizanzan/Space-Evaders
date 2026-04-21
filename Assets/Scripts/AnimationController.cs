using UnityEngine;

// Distrugge automaticamente il GameObject al termine dell'animazione
public class AnimationController : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.5f; // durata animazione

    void Start()
    {
        Destroy(gameObject, lifetime);
    }
}
