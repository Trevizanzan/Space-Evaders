using System;
using UnityEngine;

/// <summary>
/// calcola movimento della Spaceship in base a input WASD o frecce, e limita movimento ai bordi della camera (considerando anche la larghezza della nave per non farla uscire)
/// </summary>
public class Spaceship : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 30f;
    [SerializeField] private float padding = 0.1f; // margine extra dai bordi

    private float minX, maxX, minY, maxY;

    //private float shipHalfWidth;

    private Rigidbody2D spaceShipRigidbody2D;
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

        //// Calcola bordi camera
        //float camDistance = transform.position.z - Camera.main.transform.position.z;
        //Vector2 bottomLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, camDistance));
        //Vector2 topRight = Camera.main.ViewportToWorldPoint(new Vector3(1, 1, camDistance));

        ////Debug.Log($"Camera bounds: minX={bottomLeft.x}, maxX={topRight.x}, minY={bottomLeft.y}, maxY={topRight.y}");

        //minX = bottomLeft.x;
        //maxX = topRight.x;
        //minY = bottomLeft.y;
        //maxY = topRight.y;

        //// Considera larghezza nave per non farla uscire
        //SpriteRenderer sr = GetComponent<SpriteRenderer>();
        //if (sr != null)
        //{
        //    shipHalfWidth = sr.bounds.extents.x;
        //    minX += shipHalfWidth;
        //    maxX -= shipHalfWidth;
        //    minY += shipHalfWidth; // Assuming the ship is roughly square for simplicity
        //    maxY -= shipHalfWidth;
        //}
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
        
        newX = Mathf.Clamp(newX, minX, maxX);
        newY = Mathf.Clamp(newY, minY, maxY);

        transform.position = new Vector3(newX, newY, transform.position.z);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        //Debug.Log($"Spaceship collided with {collision.gameObject.name}");
        // Ferma la nave in caso di collisione
        //spaceShipRigidbody2D.linearVelocity = Vector2.zero; 

        if (collision.gameObject.CompareTag("Asteroid"))
        {
            //if (OnDied != null)
            //    OnDied.Invoke(this, EventArgs.Empty);

            // Calcola danno alla nave
            PlayerHealth playerHealth = GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(1);
            }

            //Destroy(gameObject);
            //GameManager.GetInstance().GameOver();
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
