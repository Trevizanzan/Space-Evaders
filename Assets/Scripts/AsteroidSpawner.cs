using UnityEngine;

public class AsteroidSpawner : MonoBehaviour
{
    public GameObject[] asteroidPrefabs;  // array di prefab
    public float spawnRate = 1.25f;          // 2 secondi tra uno spawn e l'altro

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

        // Aggiungiamo una leggera variazione al tempo di spawn per rendere il gioco più dinamico e imprevedibile
        //float randomSpawnRate = spawnRate + Random.Range(1.5f, 3f);

        if (timer >= spawnRate)
        {
            SpawnAsteroid();
            timer = 0f;
        }
    }

    void SpawnAsteroid()
    {
        // TODO mettere offset per evitare spawn troppo vicini ai bordi per la X e per la Y (grandezza asteroide)
        float randomX = Random.Range(minX, maxX);               // X casuale con più varietà    
        float randomSpawnY = spawnY + Random.Range(-2.5f, 5f);  // Y spawn con leggera variazione (alcuni più alti, altri meno)

        Vector3 spawnPosition = new Vector3(randomX, spawnY, 0);    // TODO alzare spawnY per evitare spawn troppo vicini alla camera

        int i = Random.Range(0, asteroidPrefabs.Length);
        Instantiate(asteroidPrefabs[i], spawnPosition, Quaternion.identity);
    }
}