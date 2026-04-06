using UnityEngine;
using System.Collections;

public class ShipTransitionController : MonoBehaviour
{
    [SerializeField] private float exitSpeed = 24f;
    //[SerializeField] private float exitHeight = 15f; // altezza oltre la camera

    private Spaceship ship;
    private bool isTransitioning = false;

    void Start()
    {
        ship = Spaceship.GetInstance();

        if (DifficultyManager.Instance != null)
        {
            DifficultyManager.Instance.OnLevelComplete += StartTransition;
        }
    }

    void StartTransition()
    {
        if (ship != null && !isTransitioning)
        {
            StartCoroutine(TransitionRoutine());
        }
    }

    // Gestisce la transizione: disabilita controlli, muove verso l'alto, teletrasporta in basso, riabilita controlli
    IEnumerator TransitionRoutine()
    {
        isTransitioning = true;

        // Disabilita input e collisioni
        ship.enabled = false;
        ship.GetComponent<Collider2D>().enabled = false;

        // Calcola exitHeight dalla camera (20% sopra il bordo superiore)
        float cameraTop = Camera.main.orthographicSize;
        float exitHeight = cameraTop + (cameraTop * 0.2f);

        // Movimento verso l'alto
        Vector3 startPos = ship.transform.position;
        Vector3 targetPos = new Vector3(startPos.x, exitHeight, startPos.z);

        float elapsed = 0f;
        float duration = Vector3.Distance(startPos, targetPos) / exitSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            ship.transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            yield return null;
        }

        // Teletrasporta in basso per il prossimo livello
        yield return new WaitForSeconds(0.5f);

        // Calcola entryPos dalla camera (30% dal basso)
        float cameraBottom = -Camera.main.orthographicSize;
        float entryY = cameraBottom + (Camera.main.orthographicSize * 0.3f);

        Vector3 entryPos = new Vector3(0f, entryY, startPos.z);
        ship.transform.position = entryPos;

        // Riattiva controlli e collisioni
        ship.enabled = true;
        ship.GetComponent<Collider2D>().enabled = true;

        isTransitioning = false;
    }

    void OnDestroy()
    {
        if (DifficultyManager.Instance != null)
        {
            DifficultyManager.Instance.OnLevelComplete -= StartTransition;
        }
    }
}