using UnityEngine;

public class GameManager : MonoBehaviour
{
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