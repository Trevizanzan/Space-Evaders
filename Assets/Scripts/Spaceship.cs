using System;
using UnityEngine;

/// <summary>
/// calcola movimento della Spaceship in base a input WASD o frecce, e limita movimento ai bordi della camera (considerando anche la larghezza della nave per non farla uscire)
/// </summary>
public class Spaceship : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 20f;
    [SerializeField] private float padding = 0.1f; // margine extra dai bordi

    private float minX, maxX, minY, maxY;

    //private Rigidbody2D spaceShipRigidbody2D;
    //public event EventHandler OnDied;
    private SpriteRenderer sr;
    private Camera cam;

    private int lastScreenWidth, lastScreenHeight;


    private static Spaceship instanceSpaceShip;
    public static Spaceship GetInstance()
    {
        return instanceSpaceShip;
    }

    void Awake()
    {
        //spaceShipRigidbody2D = GetComponent<Rigidbody2D>();
        instanceSpaceShip = this;
        sr = GetComponent<SpriteRenderer>();
        cam = Camera.main;
    }

    void Start()
    {
        RecalculateBounds(); // primo calcolo
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
    }

    void Update()
    {
        // Ricalcola solo se cambia risoluzione
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            RecalculateBounds();
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
        }

        float horizontalInput = 0f;
        float verticalInput = 0f;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) verticalInput = 1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) horizontalInput = -1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) verticalInput = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) horizontalInput = 1f;

        float newX = transform.position.x + horizontalInput * moveSpeed * Time.deltaTime;
        float newY = transform.position.y + verticalInput * moveSpeed * Time.deltaTime;

        // Clampa posizione per rimanere dentro i bordi
        newX = Mathf.Clamp(newX, minX, maxX);
        newY = Mathf.Clamp(newY, minY, maxY);

        // muove la nave alla nuova posizione
        transform.position = new Vector3(newX, newY, transform.position.z);
    }

    /// <summary>
    /// Il player perde vita se colpito da un asteroide o dal boss, e muore se la vita arriva a 0 (gestito da PlayerHealth)
    /// </summary>
    /// <param name="collision"></param>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        //Debug.Log($"Spaceship collided with {collision.gameObject.name}");
        // Ferma la nave in caso di collisione
        //spaceShipRigidbody2D.linearVelocity = Vector2.zero; 

        if (collision.gameObject.CompareTag("Asteroid") || collision.gameObject.CompareTag("Boss"))
        {
            //if (OnDied != null)
            //    OnDied.Invoke(this, EventArgs.Empty);

            // Calcola danno alla nave
            PlayerHealth playerHealth = GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(1);
            }
        }
    }

    void RecalculateBounds()
    {
        if (cam == null) cam = Camera.main;

        // In ortografica la Z è irrilevante per x/y, possiamo passare 0
        Vector3 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0f, 0f, 0f));
        Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(1f, 1f, 0f));
        Vector2 ext = Vector2.zero;

        if (sr != null) ext = sr.bounds.extents; // ext.x metà larghezza, ext.y metà altezza
        minX = bottomLeft.x + ext.x + padding;
        maxX = topRight.x - ext.x - padding;
        minY = bottomLeft.y + ext.y + padding;
        maxY = topRight.y - ext.y - padding;
    }
}
