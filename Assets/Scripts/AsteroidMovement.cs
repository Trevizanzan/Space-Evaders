using UnityEngine;

/// <summary>
/// OLD version: la rotazione degli asteroidi funziona in maniera strana, iniziano a girare in cerchio dal basso verso l'alto generando una casualit� interessante, come se avessero un ORBITA!!!
/// </summary>
//public class AsteroidMovement : MonoBehaviour
//{
//    private float fallSpeed = 25f;
//    private float rotationSpeed;
//    private float destroyY;

//    void Start()
//    {
//        // Rotazione casuale
//        rotationSpeed = Random.Range(-50f, 50f);
//        Debug.Log("rotationSpeed: " + rotationSpeed);

//        // Calcola bordo inferiore camera
//        float camDistance = transform.position.z - Camera.main.transform.position.z;
//        Debug.Log($"camDistance: {camDistance}");

//        Vector2 bottomLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, camDistance));
//        destroyY = bottomLeft.y - 2f;
//    }

//    void Update()
//    {
//        // Movimento (verso il basso)
//        transform.Translate(0, -fallSpeed * Time.deltaTime, 0);

//        // Rotazione
//        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);

//        // Distruzione se esce sotto
//        if (transform.position.y < destroyY)
//        {
//            Destroy(gameObject);
//        }
//    }
//}

public class AsteroidMovement : MonoBehaviour
{
    public float fallSpeed = 30f;
    private float rotationSpeed;
    private float destroyY;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // Rotazione casuale (in gradi al secondo)
        rotationSpeed = Random.Range(-90f, 90f);
        //Debug.Log("rotationSpeed: " + rotationSpeed);
        rb.angularVelocity = rotationSpeed;                 // rotazione su se stessa

        // Velocità caduta con leggera variazione
        float randomFallSpeed = fallSpeed + Random.Range(-10f, 10f);
        rb.linearVelocity = new Vector2(0, -randomFallSpeed);   // direzione verso il basso

        // Calcola bordo inferiore camera
        float camDistance = transform.position.z - Camera.main.transform.position.z;
        //Debug.Log($"camDistance: {camDistance}");
        Vector2 bottomLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, camDistance));
        destroyY = bottomLeft.y - 2f;
    }

    void Update()
    {
        // Solo controllo distruzione
        if (transform.position.y < destroyY)
        {
            Destroy(gameObject);
        }
    }
}