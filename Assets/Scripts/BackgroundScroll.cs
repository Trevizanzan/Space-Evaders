//using UnityEngine;

//public class BackgroundScroll : MonoBehaviour
//{
//    public float scrollSpeed = 1f;

//    public Transform backgroundTop;
//    public Transform backgroundBottom;

//    private float cameraHeight;

//    void Start()
//    {
//        // Calcola altezza viewport camera
//        cameraHeight = Camera.main.orthographicSize * 2f;
//        //Debug.Log("cameraHeight: " + cameraHeight);
//    }

//    void Update()
//    {
//        // Muove entrambi i background verso il basso
//        backgroundTop.Translate(0, -scrollSpeed * Time.deltaTime, 0);
//        backgroundBottom.Translate(0, -scrollSpeed * Time.deltaTime, 0);

//        // Se top esce sotto → spostalo sopra bottom
//        if (backgroundTop.position.y <= -cameraHeight)
//        {
//            backgroundTop.position = new Vector3(backgroundTop.position.x, backgroundBottom.position.y + cameraHeight, 0);
//        }

//        // Se bottom esce sotto → spostalo sopra top
//        if (backgroundBottom.position.y <= -cameraHeight)
//        {
//            backgroundBottom.position = new Vector3(backgroundBottom.position.x, backgroundTop.position.y + cameraHeight, 0);
//        }
//    }
//}

using UnityEngine;

public class BackgroundScroll : MonoBehaviour
{
    public Transform backgroundTop;
    public Transform backgroundBottom;
    public float scrollSpeed = 120f;    // 40?
    [SerializeField] private float extraMargin = 1f; // margine anti-barre

    private float bgHeight;       // altezza completa dello sprite in world units
    private float bgHalfHeight;   // metà altezza    private float camBottomY;

    private SpriteRenderer topSR, bottomSR;
    private Camera cam;

    void Start()
    {
        cam = Camera.main;

        topSR = backgroundTop.GetComponent<SpriteRenderer>();
        bottomSR = backgroundBottom.GetComponent<SpriteRenderer>();

        // Scala entrambi per coprire la camera (con margine)
        ScaleToFillCamera(topSR);
        ScaleToFillCamera(bottomSR);

        // Altezza reale DOPO lo scaling
        bgHeight = topSR.bounds.size.y;
        bgHalfHeight = bgHeight * 0.5f;

        // Posiziona impilati
        float startY = cam.transform.position.y;
        float camX = cam.transform.position.x;

        backgroundBottom.position = new Vector3(camX, startY, backgroundBottom.position.z);
        backgroundTop.position = new Vector3(camX, startY + bgHeight, backgroundTop.position.z);
    }

    void Update()
    {
        float dy = scrollSpeed * Time.deltaTime;

        backgroundTop.Translate(0, -dy, 0, Space.World);
        backgroundBottom.Translate(0, -dy, 0, Space.World);

        // Fondo camera in world space
        float camBottomY = cam.ViewportToWorldPoint(new Vector3(0, 0, 0)).y;

        // BORDO SUPERIORE del background bottom
        float bottomTopEdge = backgroundBottom.position.y + bgHalfHeight;

        // Se il background bottom è completamente uscito sotto la camera,
        // cioè il suo bordo superiore è sotto il fondo della camera
        if (bottomTopEdge < camBottomY)
        {
            float camX = cam.transform.position.x;
            // mettilo sopra il top
            backgroundBottom.position = new Vector3(
                camX,
                backgroundTop.position.y + bgHeight,
                backgroundBottom.position.z
            );
            // Swap riferimenti (così "top" rimane sempre quello più in alto)
            var tmp = backgroundTop;
            backgroundTop = backgroundBottom;
            backgroundBottom = tmp;
            var tmpSR = topSR;
            topSR = bottomSR;
            bottomSR = tmpSR;
        }
    }

    void ScaleToFillCamera(SpriteRenderer sr)
    {
        // Dimensioni reali della viewport in world units
        Vector3 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(1, 1, 0));
        float camWidth = topRight.x - bottomLeft.x + extraMargin;
        float camHeight = topRight.y - bottomLeft.y + extraMargin;
        // Size naturale dello sprite
        Vector2 spriteSize = sr.sprite.bounds.size;
        // Scala per coprire la camera
        sr.transform.localScale = new Vector3(
            camWidth / spriteSize.x,
            camHeight / spriteSize.y,
            1f
        );
    }
}