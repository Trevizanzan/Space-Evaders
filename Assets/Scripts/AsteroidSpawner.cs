using UnityEngine;

public class AsteroidSpawner : MonoBehaviour
{
    public GameObject[] asteroidPrefabs;  // array di prefab
    public float spawnRate = 2f;

    private float timer;
    private float minX;
    private float maxX;
    private float spawnY;   // TODO: aumentarlo di un offset per evitare spawn troppo vicini alla camera (grandezza asteroide)

    void Start()
    {
        // Calcola bordi camera
        float camDistance = -Camera.main.transform.position.z;
        Vector2 bottomLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, camDistance));
        Vector2 topRight = Camera.main.ViewportToWorldPoint(new Vector3(1, 1, camDistance));

        minX = bottomLeft.x + 1f;
        maxX = topRight.x - 1f;
        spawnY = topRight.y + 2f;
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnRate)
        {
            SpawnAsteroid();
            timer = 0f;
        }
    }

    void SpawnAsteroid()
    {
        float randomX = Random.Range(minX, maxX);       // TODO mettere offset per evitare spawn troppo vicini ai bordi
        Vector3 spawnPosition = new Vector3(randomX, spawnY, 0);    // TODO alzare spawnY per evitare spawn troppo vicini alla camera

        int i = Random.Range(0, asteroidPrefabs.Length);
        Instantiate(asteroidPrefabs[i], spawnPosition, Quaternion.identity);
    }
}