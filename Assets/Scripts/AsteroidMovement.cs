using UnityEngine;

public class AsteroidMovement : MonoBehaviour
{
    private float fallSpeed = 25f;
    private float rotationSpeed;
    private float destroyY;

    void Start()
    {
        // Rotazione casuale
        rotationSpeed = Random.Range(-50f, 50f);
        Debug.Log("rotationSpeed: " + rotationSpeed);

        // Calcola bordo inferiore camera
        float camDistance = transform.position.z - Camera.main.transform.position.z;
        Debug.Log($"camDistance: {camDistance}");

        Vector2 bottomLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, camDistance));
        destroyY = bottomLeft.y - 2f;
    }

    void Update()
    {
        // Movimento verso il basso
        transform.Translate(0, -fallSpeed * Time.deltaTime, 0);

        // Rotazione
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);

        // Distruzione se esce sotto
        if (transform.position.y < destroyY)
        {
            Destroy(gameObject);
        }
    }
}