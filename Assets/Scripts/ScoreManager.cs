using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("UI")]
    //public Text scoreText;
    public TMP_Text scoreText;

    public Text highscoreText;
    public Text livesText;

    [Header("References")]
    public PlayerHealth playerHealth;  // gliela colleghiamo dall Inspector

    private int score;
    private int highscore;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    void Start()
    {
        // Carica highscore salvato (0 se non esiste)
        highscore = PlayerPrefs.GetInt("highscore", 0);
        UpdateScoreUI();
        UpdateLivesUI();
    }

    public void AddScore(int amount)
    {
        score += amount;

        if (score > highscore)
        {
            highscore = score;
            PlayerPrefs.SetInt("highscore", highscore);
            PlayerPrefs.Save();
        }

        UpdateScoreUI();
    }

    public void UpdateLivesUI()
    {
        if (livesText != null && playerHealth != null)
        {
            livesText.text = "Lives: " + playerHealth.CurrentHealth;
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + score;

        if (highscoreText != null)
            highscoreText.text = "Best: " + highscore;
    }

    public int GetCurrentScore() => score;
    public int GetHighscore() => highscore;
}