using UnityEngine;

public class BackgroundScroll : MonoBehaviour
{
    public float scrollSpeed = 1f;

    public Transform backgroundTop;
    public Transform backgroundBottom;

    private float cameraHeight;
    
    void Start()
    {
        // Calcola altezza viewport camera
        cameraHeight = Camera.main.orthographicSize * 2f;
        Debug.Log("cameraHeight: " + cameraHeight);
    }
    
    void Update()
    {
        // Muove entrambi i background verso il basso
        backgroundTop.Translate(0, -scrollSpeed * Time.deltaTime, 0);
        backgroundBottom.Translate(0, -scrollSpeed * Time.deltaTime, 0);

        // Se top esce sotto → spostalo sopra bottom
        if (backgroundTop.position.y <= -cameraHeight)
        {
            backgroundTop.position = new Vector3(backgroundTop.position.x, backgroundBottom.position.y + cameraHeight, 0);
        }

        // Se bottom esce sotto → spostalo sopra top
        if (backgroundBottom.position.y <= -cameraHeight)
        {
            backgroundBottom.position = new Vector3(backgroundBottom.position.x, backgroundTop.position.y + cameraHeight, 0);
        }
    }
}