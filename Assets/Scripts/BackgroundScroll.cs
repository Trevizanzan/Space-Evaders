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
    public float scrollSpeed = 40f;
    [SerializeField] private float extraMargin = 1f; // margine anti-barre

    private float bgHeight;
    private float camBottomY;
    private float camX;

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
        // Posiziona impilati
        float startY = cam.transform.position.y;
        backgroundBottom.position = new Vector3(cam.transform.position.x, startY, backgroundBottom.position.z);
        backgroundTop.position = new Vector3(cam.transform.position.x, startY + bgHeight, backgroundTop.position.z);

        //camX = Camera.main.transform.position.x;

        //// Altezza reale sprite in world units (dipende da PPU e scale)
        //var sr = backgroundBottom.GetComponent<SpriteRenderer>();
        //bgHeight = sr.bounds.size.y;

        //// Basso al centro camera (o y=0 se preferisci, ma meglio legarlo alla camera)
        //float startY = Camera.main.transform.position.y;

        //backgroundBottom.position = new Vector3(camX, startY, backgroundBottom.position.z);
        //backgroundTop.position = new Vector3(camX, startY + bgHeight, backgroundTop.position.z);

        //camBottomY = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, 0)).y;
    }

    void Update()
    {
        float dy = scrollSpeed * Time.deltaTime;
        backgroundTop.Translate(0, -dy, 0, Space.World);
        backgroundBottom.Translate(0, -dy, 0, Space.World);

        // Wrap quando esce sotto
        float camBottomY = cam.ViewportToWorldPoint(new Vector3(0, 0, 0)).y;
        if (backgroundBottom.position.y + bgHeight < camBottomY)
        {
            backgroundBottom.position = new Vector3(
                cam.transform.position.x,
                backgroundTop.position.y + bgHeight,
                backgroundBottom.position.z
            );
            // Swap
            var tmp = backgroundTop;
            backgroundTop = backgroundBottom;
            backgroundBottom = tmp;
            var tmpSR = topSR;
            topSR = bottomSR;
            bottomSR = tmpSR;
        }

        //// Se il bottom è completamente sotto la camera, mettilo sopra top
        //if (backgroundBottom.position.y + bgHeight < camBottomY)
        //{
        //    backgroundBottom.position = new Vector3(
        //        camX,
        //        backgroundTop.position.y + bgHeight,
        //        backgroundBottom.position.z
        //    );

        //    // swap dei riferimenti (così continuiamo a ciclare correttamente)
        //    var tmp = backgroundTop;
        //    backgroundTop = backgroundBottom;
        //    backgroundBottom = tmp;
        //}
    }

    void ScaleToFillCamera(SpriteRenderer sr)
    {
        // Calcola dimensioni REALI della viewport in world units
        Vector3 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(1, 1, 0));
        float camWidth = topRight.x - bottomLeft.x + extraMargin;
        float camHeight = topRight.y - bottomLeft.y + extraMargin;
        // Size "naturale" dello sprite (prima dello scaling)
        Vector2 spriteSize = sr.sprite.bounds.size;
        // Scala per coprire
        sr.transform.localScale = new Vector3(
            camWidth / spriteSize.x,
            camHeight / spriteSize.y,
            1f
        );
    }
}