using UnityEngine;

public class BackgroundScaler : MonoBehaviour
{
    private float securityMargin = 12f; // Margine di sicurezza per evitare bordi neri (è un po' eccessivo, ma meglio abbondare, si tratta di un quadrato)

    public void Start()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;
        
        float worldScreenHeight = Camera.main.orthographicSize * 2f + securityMargin;
        float worldScreenWidth = worldScreenHeight / Screen.height * Screen.width + securityMargin;

        transform.localScale = new Vector3(
            worldScreenWidth / sr.sprite.bounds.size.x,
            worldScreenHeight / sr.sprite.bounds.size.y,
            1
        );
    }
}
