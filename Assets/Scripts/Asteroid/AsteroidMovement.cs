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
    [Header("Rotation Settings")]
    [SerializeField] private float minRotationSpeed = -180f;
    [SerializeField] private float maxRotationSpeed = 180f;

    [Header("Destroy Boundaries")]
    [SerializeField] private float destroyBorderMultiplier = 2f; // Quanto fuori dallo schermo può andare prima di essere distrutto
    private Rigidbody2D rb;

    private float destroyY;
    private float destroyXLimit;

    public float defalutFallSpeed = 3f;
    private float rotationSpeed;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // Rotazione casuale su se stesso
        float rotationSpeed = Random.Range(minRotationSpeed, maxRotationSpeed);
        rb.angularVelocity = rotationSpeed;

        // Calcola i bordi di distruzione (più ampi dello schermo per dare margine)
        CalculateDestroyBounds();
    }

    void CalculateDestroyBounds()
    {
        float cameraHeight = Camera.main.orthographicSize;
        float cameraWidth = cameraHeight * Camera.main.aspect;
        float camDistance = transform.position.z - Camera.main.transform.position.z;
        Vector2 bottomLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, camDistance));

        // Bordo inferiore (con margine)
        destroyY = bottomLeft.y - (cameraHeight * 0.2f);

        // Bordi laterali (più ampi per gestire asteroidi orizzontali/diagonali)
        destroyXLimit = cameraWidth * destroyBorderMultiplier;
    }

    void Update()
    {
        //// Forza la velocity OGNI FRAME (solo per debug!)
        //if (rb.linearVelocity.magnitude > 0)
        //{
        //    // Non fare niente, lascia che la physics engine faccia il suo
        //}

        // Distruggi se l'asteroide esce completamente dallo schermo
        if (transform.position.y < destroyY ||
            Mathf.Abs(transform.position.x) > destroyXLimit)
        {
            Destroy(gameObject);
        }
    }
}