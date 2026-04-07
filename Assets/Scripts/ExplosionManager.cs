using UnityEngine;

public class ExplosionManager : MonoBehaviour
{
    public static ExplosionManager Instance;

    [Header("Prefabs")]
    public GameObject explosionSmallPrefab; // Explosion_1
    public GameObject explosionBigPrefab;   // Explosion_2

    [Header("Variation")]
    public float positionJitter = 0.15f;    // offset casuale
    public float scaleJitter = 0.15f;       // variazione scala
    public float rotationJitter = 10f;      // gradi

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SpawnSmall(Vector3 pos, float scale = 1f) => Spawn(explosionSmallPrefab, pos, scale);
    public void SpawnBig(Vector3 pos, float scale = 1f) => Spawn(explosionBigPrefab, pos, scale);

    private void Spawn(GameObject prefab, Vector3 pos, float baseScale)
    {
        if (prefab == null) return;

        Vector3 jitterPos = pos + new Vector3(
            Random.Range(-positionJitter, positionJitter),
            Random.Range(-positionJitter, positionJitter),
            0f
        );

        // Random rotation around Z-axis
        Quaternion rot = Quaternion.Euler(0, 0, Random.Range(-rotationJitter, rotationJitter));

        // Instanzia l'effetto con posizione e rotazione casuali
        GameObject fx = Instantiate(prefab, jitterPos, rot);

        // Rispetta la scala originale del prefab e moltiplica per baseScale
        Vector3 prefabScale = prefab.transform.localScale;
        float randomScale = baseScale * (1f + Random.Range(-scaleJitter, scaleJitter));

        // Applica la scala con variazione casuale
        fx.transform.localScale = new Vector3(
            prefabScale.x * randomScale,
            prefabScale.y * randomScale,
            1f
        );
    }
}