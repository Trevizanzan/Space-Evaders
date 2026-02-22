using System;
using UnityEngine;

/// <summary>
/// calcola movimento della Spaceship in base a input WASD o frecce, e limita movimento ai bordi della camera (considerando anche la larghezza della nave per non farla uscire)
/// </summary>
public class Spaceship : MonoBehaviour
{
    private float moveSpeed = 100f;
    private float minX;
    private float maxX;
    private float minY;
    private float maxY;
    private float shipHalfWidth;

    private Rigidbody2D spaceShipRigidbody2D;
    public event EventHandler OnDied;

    private static Spaceship instance;
    public static Spaceship GetInstance()
    {
        return instance;
    }

    void Awake()
    {
        spaceShipRigidbody2D = GetComponent<Rigidbody2D>();
        instance = this;
    }

    void Start()
    {
        // Calcola bordi camera
        float camDistance = transform.position.z - Camera.main.transform.position.z;
        Vector2 bottomLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, camDistance));
        Vector2 topRight = Camera.main.ViewportToWorldPoint(new Vector3(1, 1, camDistance));

        Debug.Log($"Camera bounds: minX={bottomLeft.x}, maxX={topRight.x}, minY={bottomLeft.y}, maxY={topRight.y}");

        minX = bottomLeft.x;
        maxX = topRight.x;
        minY = bottomLeft.y;
        maxY = topRight.y;

        // Considera larghezza nave per non farla uscire
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            shipHalfWidth = sr.bounds.extents.x;
            minX += shipHalfWidth;
            maxX -= shipHalfWidth;
            minY += shipHalfWidth; // Assuming the ship is roughly square for simplicity
            maxY -= shipHalfWidth;
        }
    }

    void Update()
    {
        float horizontalInput = 0f;
        float verticalInput = 0f;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            verticalInput = 1f;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            horizontalInput = -1f;

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            verticalInput = -1f;

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            horizontalInput = 1f;

        float newX = transform.position.x + horizontalInput * moveSpeed * Time.deltaTime;
        float newY = transform.position.y + verticalInput * moveSpeed * Time.deltaTime;
        
        newX = Mathf.Clamp(newX, minX, maxX);
        newY = Mathf.Clamp(newY, minY, maxY);

        transform.position = new Vector3(newX, newY, transform.position.z);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log($"Spaceship collided with {collision.gameObject.name}");

        spaceShipRigidbody2D.linearVelocity = Vector2.zero; // Ferma la nave in caso di collisione

        if (collision.gameObject.CompareTag("Asteroid"))
        {
            if (OnDied != null)
                OnDied.Invoke(this, EventArgs.Empty);

            //Destroy(gameObject);
            GameManager.GetInstance().GameOver();
        }
    }
}
