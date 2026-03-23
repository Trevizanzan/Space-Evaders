using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // MISURE DELLA CAMEREA
    //  Con orthographicSize = 5.5:
    //  Altezza visibile = 2 * 5.5 = 11 unità
    //  Con 16:9, larghezza visibile ≈ 11 * 16/9 = 19.56 unità

    [Header("Gameplay")]
    public AsteroidSpawner asteroidSpawner;

    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public TMP_Text goScoreText;
    public TMP_Text goBestText;

    private bool isGameOver = false;

    public static GameManager Instance;
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

        // suona suono di game over
        if (SoundManager.Instance != null) 
            SoundManager.Instance.PlayGameOver();

        // Ferma lo spawner
        if (asteroidSpawner != null)
            asteroidSpawner.enabled = false;
        
        //Time.timeScale = 0f;

        // Aggiorna testi della schermata
        if (ScoreManager.Instance != null)
        {
            int score = ScoreManager.Instance.GetCurrentScore();
            int best = ScoreManager.Instance.GetHighscore();
            if (goScoreText != null)
                goScoreText.text = $"SCORE: {score}";
            if (goBestText != null)
                goBestText.text = $"BEST: {best}";
        }

        // Mostra UI dopo un attimo (usando tempo non scalato),
        // questo per dare tempo alle animazioni di esplosione di completarsi
        StartCoroutine(GameOverRoutine());
    }

    private System.Collections.IEnumerator GameOverRoutine()
    {
        // Aspetta un attimo (tempo reale, non affected dal timeScale)
        yield return new WaitForSecondsRealtime(0.333f); 

        // Ferma il gioco
        Time.timeScale = 0f;

        // Mostra pannello game over
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.name);
    }
    void Update()
    {
        // Restart da tastiera
        if (isGameOver && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
        {
            RestartGame();
        }
    }
}