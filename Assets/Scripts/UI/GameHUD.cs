using UnityEngine;
using UnityEngine.UI;
using GlitchRun.Core;
using GlitchRun.Player;
using GlitchRun.Audio;

namespace GlitchRun.UI
{
    /// <summary>
    /// Pure uGUI Cyberpunk HUD controller.
    /// Manages data fragment counts, simulation integrity (lives), glitch status meter,
    /// score, level timer, pause overlay, and settings overlay.
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        [Header("HUD Core References")]
        [SerializeField] private Text scoreText;
        [SerializeField] private Text fragmentsText;
        [SerializeField] private Text timerText;
        [SerializeField] private Image glitchMeterFill;
        [SerializeField] private Text glitchStatusText;
        [SerializeField] private Image[] lifeHearts;

        [Header("Pause Overlay")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Settings Overlay")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Toggle screenShakeToggle;
        [SerializeField] private Button closeSettingsButton;

        [Header("Player Reference")]
        [SerializeField] private GlitchAbility playerGlitch;

        private void Start()
        {
            if (playerGlitch == null)
            {
                playerGlitch = FindAnyObjectByType<GlitchAbility>();
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnFragmentsUpdated += UpdateFragmentsUI;
                GameManager.Instance.OnLivesUpdated += UpdateLivesUI;
                GameManager.Instance.OnScoreUpdated += UpdateScoreUI;
                GameManager.Instance.OnStateChanged += HandleStateChanged;

                UpdateFragmentsUI(GameManager.Instance.CollectedFragments, GameManager.Instance.RequiredFragments);
                UpdateLivesUI(GameManager.Instance.CurrentLives, GameManager.Instance.MaxLives);
                UpdateScoreUI(GameManager.Instance.CurrentScore);
            }

            SetupButtons();
            SetupSettings();

            if (pausePanel != null) pausePanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnFragmentsUpdated -= UpdateFragmentsUI;
                GameManager.Instance.OnLivesUpdated -= UpdateLivesUI;
                GameManager.Instance.OnScoreUpdated -= UpdateScoreUI;
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
            }
        }

        private void SetupButtons()
        {
            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(() =>
                {
                    PlayClickSound();
                    if (GameManager.Instance != null) GameManager.Instance.TogglePause();
                });
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(() =>
                {
                    PlayClickSound();
                    if (GameManager.Instance != null) GameManager.Instance.RestartLevel();
                });
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(() =>
                {
                    PlayClickSound();
                    OpenSettings();
                });
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(() =>
                {
                    PlayClickSound();
                    if (GameManager.Instance != null) GameManager.Instance.LoadMainMenu();
                });
            }

            if (closeSettingsButton != null)
            {
                closeSettingsButton.onClick.AddListener(() =>
                {
                    PlayClickSound();
                    CloseSettings();
                });
            }
        }

        private void SetupSettings()
        {
            if (AudioManager.Instance != null)
            {
                if (masterSlider != null)
                {
                    masterSlider.value = AudioManager.Instance.MasterVolume;
                    masterSlider.onValueChanged.AddListener(v => AudioManager.Instance.SetMasterVolume(v));
                }

                if (musicSlider != null)
                {
                    musicSlider.value = AudioManager.Instance.MusicVolume;
                    musicSlider.onValueChanged.AddListener(v => AudioManager.Instance.SetMusicVolume(v));
                }

                if (sfxSlider != null)
                {
                    sfxSlider.value = AudioManager.Instance.SfxVolume;
                    sfxSlider.onValueChanged.AddListener(v => AudioManager.Instance.SetSfxVolume(v));
                }
            }

            if (fullscreenToggle != null)
            {
                fullscreenToggle.isOn = Screen.fullScreen;
                fullscreenToggle.onValueChanged.AddListener(isFull =>
                {
                    Screen.fullScreen = isFull;
                    PlayerPrefs.SetInt("Fullscreen", isFull ? 1 : 0);
                });
            }

            if (screenShakeToggle != null)
            {
                screenShakeToggle.isOn = PlayerPrefs.GetInt("ScreenShake", 1) == 1;
                screenShakeToggle.onValueChanged.AddListener(enabled =>
                {
                    PlayerPrefs.SetInt("ScreenShake", enabled ? 1 : 0);
                });
            }
        }

        private void Update()
        {
            // Update live timer
            if (GameManager.Instance != null && timerText != null)
            {
                float t = GameManager.Instance.ElapsedTime;
                int mins = (int)(t / 60f);
                int secs = (int)(t % 60f);
                timerText.text = $"TIME: {mins:00}:{secs:00}";
            }

            // Update glitch ability meter
            if (playerGlitch == null)
            {
                playerGlitch = FindAnyObjectByType<GlitchAbility>();
            }

            if (playerGlitch != null)
            {
                if (playerGlitch.IsGlitching)
                {
                    if (glitchMeterFill != null) glitchMeterFill.fillAmount = playerGlitch.NormalizedActiveTime;
                    if (glitchStatusText != null) glitchStatusText.text = "GLITCH ACTIVE [PHASING]";
                }
                else if (playerGlitch.CanGlitch)
                {
                    if (glitchMeterFill != null) glitchMeterFill.fillAmount = 1f;
                    if (glitchStatusText != null) glitchStatusText.text = "GLITCH READY [SHIFT]";
                }
                else
                {
                    if (glitchMeterFill != null) glitchMeterFill.fillAmount = 1f - playerGlitch.NormalizedCooldown;
                    if (glitchStatusText != null) glitchStatusText.text = $"RECHARGING: {playerGlitch.CooldownTimer:F1}s";
                }
            }
        }

        public void UpdateFragmentsUI(int collected, int required)
        {
            if (fragmentsText != null)
            {
                fragmentsText.text = $"DATA: {collected} / {required}";
            }
        }

        public void UpdateLivesUI(int current, int max)
        {
            if (lifeHearts != null)
            {
                for (int i = 0; i < lifeHearts.Length; i++)
                {
                    if (lifeHearts[i] != null)
                    {
                        lifeHearts[i].enabled = (i < current);
                    }
                }
            }
        }

        public void UpdateScoreUI(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = $"SCORE: {score:00000}";
            }
        }

        private void HandleStateChanged(GameState state)
        {
            if (state == GameState.Paused)
            {
                if (pausePanel != null) pausePanel.SetActive(true);
            }
            else if (state == GameState.Playing)
            {
                if (pausePanel != null) pausePanel.SetActive(false);
                if (settingsPanel != null) settingsPanel.SetActive(false);
            }
            else if (state == GameState.Victory)
            {
                // Transition to dedicated Victory scene
                GameManager.Instance.LoadScene("Victory");
            }
            else if (state == GameState.GameOver)
            {
                // Transition to dedicated GameOver scene
                GameManager.Instance.LoadScene("GameOver");
            }
        }

        public void OpenSettings()
        {
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        public void CloseSettings()
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        private void PlayClickSound()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySfx("click");
            }
        }
    }
}
