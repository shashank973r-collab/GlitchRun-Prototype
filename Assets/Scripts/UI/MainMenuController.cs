using UnityEngine;
using UnityEngine.UI;
using GlitchRun.Core;
using GlitchRun.Audio;

namespace GlitchRun.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Menu Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button howToPlayButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("Modals")]
        [SerializeField] private GameObject howToPlayModal;
        [SerializeField] private Button closeHowToPlayButton;
        [SerializeField] private GameObject settingsModal;
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Button closeSettingsButton;

        private void Start()
        {
            Time.timeScale = 1f;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMusic("menu");
            }

            SetupButtons();
            SetupSettings();

            if (howToPlayModal != null) howToPlayModal.SetActive(false);
            if (settingsModal != null) settingsModal.SetActive(false);
        }

        private void SetupButtons()
        {
            if (playButton != null)
            {
                playButton.onClick.AddListener(() =>
                {
                    PlayClick();
                    if (GameManager.Instance != null) GameManager.Instance.StartGame();
                    else UnityEngine.SceneManagement.SceneManager.LoadScene("Gameplay");
                });
            }

            if (howToPlayButton != null)
            {
                howToPlayButton.onClick.AddListener(() =>
                {
                    PlayClick();
                    if (howToPlayModal != null) howToPlayModal.SetActive(true);
                });
            }

            if (closeHowToPlayButton != null)
            {
                closeHowToPlayButton.onClick.AddListener(() =>
                {
                    PlayClick();
                    if (howToPlayModal != null) howToPlayModal.SetActive(false);
                });
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(() =>
                {
                    PlayClick();
                    if (settingsModal != null) settingsModal.SetActive(true);
                });
            }

            if (closeSettingsButton != null)
            {
                closeSettingsButton.onClick.AddListener(() =>
                {
                    PlayClick();
                    if (settingsModal != null) settingsModal.SetActive(false);
                });
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(() =>
                {
                    PlayClick();
                    Application.Quit();
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
        }

        private void PlayClick()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySfx("click");
            }
        }
    }
}
