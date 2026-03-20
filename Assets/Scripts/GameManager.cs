using UnityEngine;

public class GameManager : MonoBehaviour
{
    // MISURE DELLA CAMEREA
    //  Con orthographicSize = 5.5:
    //  Altezza visibile = 2 * 5.5 = 11 unità
    //  Con 16:9, larghezza visibile ≈ 11 * 16/9 = 19.56 unità


    public static GameManager Instance;
    public AsteroidSpawner spawner;
    public GameObject gameOverUI;

    private bool isGameOver = false;

    public static GameManager GetInstance()
    {
        return Instance;
    }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    public void GameOver()
    {
        if (isGameOver) return;

        isGameOver = true;
        spawner.enabled = false;
        Time.timeScale = 0f;

        if (gameOverUI != null)
            gameOverUI.SetActive(true);
    }
}