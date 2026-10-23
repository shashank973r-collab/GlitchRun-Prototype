using UnityEngine;
using UnityEngine.UI;
using GlitchRun.Core;
using GlitchRun.Audio;

namespace GlitchRun.UI
{
    public class EndGameController : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private bool isVictoryScreen = true;

        [Header("UI Elements")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text fragmentsText;
        [SerializeField] private Text timeText;
        [SerializeField] private Text rankText;

        [Header("Buttons")]
        [SerializeField] private Button retryButton;
        [SerializeField] private Button mainMenuButton;

        private void Start()
        {
            Time.timeScale = 1f;

            if (GameManager.Instance != null)
            {
                int score = GameManager.Instance.CurrentScore;
                int collected = GameManager.Instance.CollectedFragments;
                int total = GameManager.Instance.TotalFragments;
                float time = GameManager.Instance.ElapsedTime;

                if (scoreText != null) scoreText.text = $"SCORE: {score:00000}";
                if (fragmentsText != null) fragmentsText.text = $"FRAGMENTS: {collected} / {total}";

                int mins = (int)(time / 60f);
                int secs = (int)(time % 60f);
                if (timeText != null) timeText.text = $"BREACH TIME: {mins:00}:{secs:00}";

                if (rankText != null)
                {
                    if (isVictoryScreen)
                    {
                        string rank = "C";
                        if (score >= 2500) rank = "S+ [SIMULATION GOD]";
                        else if (score >= 1800) rank = "A [CYBER RUNNER]";
                        else if (score >= 1200) rank = "B [OPERATOR]";
                        rankText.text = $"RATING: {rank}";
                    }
                    else
                    {
                        rankText.text = "STATUS: TERMINATED";
                    }
                }
            }

            if (retryButton != null)
            {
                retryButton.onClick.AddListener(() =>
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx("click");
                    if (GameManager.Instance != null) GameManager.Instance.StartGame();
                    else UnityEngine.SceneManagement.SceneManager.LoadScene("Gameplay");
                });
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(() =>
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx("click");
                    if (GameManager.Instance != null) GameManager.Instance.LoadMainMenu();
                    else UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
                });
            }
        }
    }
}
