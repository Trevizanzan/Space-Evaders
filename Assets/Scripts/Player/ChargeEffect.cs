using UnityEngine;

// Effetto visivo di carica del Railgun (o qualsiasi arma con requiresCharging).
// Attaccalo a un prefab con SpriteRenderer posizionato sulla punta della nave (firePoint).
//
// Comportamento:
//   - Durante la carica: scala da 0 a maxScale in chargeTime secondi (easing morbido)
//   - A carica completa: pulsa tra maxScale e maxScale*(1+pulseAmount) a frequenza pulseSpeed
public class ChargeEffect : MonoBehaviour
{
    [SerializeField] private float maxScale = 0.35f;
    [SerializeField] private float pulseSpeed = 6f;
    [SerializeField] private float pulseAmount = 0.25f;

    private float chargeTime;
    private float startTime;
    private bool fullyCharged;

    // Chiamato da PlayerShooting subito dopo Instantiate
    public void Initialize(float chargeTime)
    {
        this.chargeTime = chargeTime;
        this.startTime = Time.time;
    }

    void Awake()
    {
        transform.localScale = Vector3.zero;

        var sr = GetComponent<SpriteRenderer>();
        var parentSr = transform.parent != null
            ? transform.parent.GetComponentInParent<SpriteRenderer>()
            : null;
        if (sr != null && parentSr != null)
            sr.sortingOrder = parentSr.sortingOrder - 1;
    }

    void Update()
    {
        float elapsed = Time.time - startTime;
        float progress = chargeTime > 0f ? Mathf.Clamp01(elapsed / chargeTime) : 1f;

        if (!fullyCharged)
        {
            float scale = Mathf.Lerp(0f, maxScale, SmoothStep(progress));
            transform.localScale = Vector3.one * scale;
            if (progress >= 1f) fullyCharged = true;
        }
        else
        {
            // Pulsa quando la carica è pronta
            float pulse = maxScale * (1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount);
            transform.localScale = Vector3.one * pulse;
        }
    }

    // Smooth interpolazione cubica (s-curve)
    private static float SmoothStep(float t) => t * t * (3f - 2f * t);
}
