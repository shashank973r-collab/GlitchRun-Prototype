using UnityEngine;
using UnityEngine.UI;
using GlitchRun.Core;
using GlitchRun.Player;

namespace GlitchRun.UI
{
    /// <summary>
    /// Cyberpunk HUD and Game State UI Overlay.
    /// Manages data fragment counts, simulation integrity (lives), glitch status bar,
    /// Game Over screen, Victory screen, and restart triggers.
    /// Features full uGUI support with automatic high-contrast Cyberpunk IMGUI fallback.
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        [Header("Optional uGUI References")]
        [SerializeField] private Text fragmentsText;
        [SerializeField] private Text livesText;
        [SerializeField] private Text glitchStatusText;
        [SerializeField] private Slider glitchCooldownSlider;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private Text victoryStatsText;

        [Header("Player Reference")]
        [SerializeField] private GlitchAbility playerGlitch;

        // Cached UI textures for Cyberpunk OnGUI
        private Texture2D panelTex;
        private Texture2D greenTex;
        private Texture2D redTex;
        private Texture2D darkTex;
        private GUIStyle headerStyle;
        private GUIStyle bodyStyle;
        private GUIStyle labelStyle;
        private GUIStyle buttonStyle;
        private GUIStyle panelStyle;
        private bool stylesInitialized = false;

        private void Start()
        {
            if (playerGlitch == null)
            {
#if UNITY_6000_0_OR_NEWER
                playerGlitch = FindAnyObjectByType<GlitchAbility>();
#else
                playerGlitch = FindFirstObjectByType<GlitchAbility>();
#endif
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnFragmentsUpdated += UpdateFragmentsUI;
                GameManager.Instance.OnLivesUpdated += UpdateLivesUI;
                GameManager.Instance.OnGameOver += HandleGameOver;
                GameManager.Instance.OnVictory += HandleVictory;

                UpdateFragmentsUI(GameManager.Instance.CollectedFragments, GameManager.Instance.RequiredFragments);
                UpdateLivesUI(GameManager.Instance.CurrentLives, GameManager.Instance.MaxLives);
            }

            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (victoryPanel != null) victoryPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnFragmentsUpdated -= UpdateFragmentsUI;
                GameManager.Instance.OnLivesUpdated -= UpdateLivesUI;
                GameManager.Instance.OnGameOver -= HandleGameOver;
                GameManager.Instance.OnVictory -= HandleVictory;
            }
        }

        private void Update()
        {
            if (playerGlitch == null)
            {
#if UNITY_6000_0_OR_NEWER
                playerGlitch = FindAnyObjectByType<GlitchAbility>();
#else
                playerGlitch = FindFirstObjectByType<GlitchAbility>();
#endif
            }

            UpdateGlitchUI();
        }

        private void UpdateFragmentsUI(int collected, int required)
        {
            if (fragmentsText != null)
            {
                int total = GameManager.Instance != null ? GameManager.Instance.TotalFragments : required;
                fragmentsText.text = $"DATA: {collected} / {required} (MAX: {total})";
            }
        }

        private void UpdateLivesUI(int current, int max)
        {
            if (livesText != null)
            {
                string bars = new string('■', Mathf.Max(0, current));
                livesText.text = $"INTEGRITY: [{bars}] ({current}/{max})";
            }
        }

        private void UpdateGlitchUI()
        {
            if (playerGlitch == null) return;

            if (glitchStatusText != null)
            {
                if (playerGlitch.IsGlitching)
                {
                    glitchStatusText.text = $"GLITCH: ACTIVE ({playerGlitch.RemainingActiveTime:F1}s)";
                    glitchStatusText.color = new Color(0.2f, 1f, 0.6f);
                }
                else if (playerGlitch.CanGlitch)
                {
                    glitchStatusText.text = "GLITCH: READY [SHIFT]";
                    glitchStatusText.color = new Color(0f, 0.9f, 1f);
                }
                else
                {
                    glitchStatusText.text = $"RECHARGING: {playerGlitch.CooldownTimer:F1}s";
                    glitchStatusText.color = new Color(1f, 0.6f, 0.2f);
                }
            }

            if (glitchCooldownSlider != null)
            {
                if (playerGlitch.IsGlitching)
                {
                    glitchCooldownSlider.value = playerGlitch.NormalizedActiveTime;
                }
                else
                {
                    glitchCooldownSlider.value = 1f - playerGlitch.NormalizedCooldown;
                }
            }
        }

        private void HandleGameOver()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
        }

        private void HandleVictory()
        {
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
            }

            if (victoryStatsText != null && GameManager.Instance != null)
            {
                victoryStatsText.text = $"Data Fragments: {GameManager.Instance.CollectedFragments} / {GameManager.Instance.TotalFragments}\nEscape Time: {GameManager.Instance.ElapsedTime:F1}s";
            }
        }

        public void OnRestartButtonClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartLevel();
            }
        }

        // ==========================================
        // Cyberpunk IMGUI Overlay (Guaranteed fallback)
        // ==========================================

        private void InitStyles()
        {
            if (stylesInitialized) return;

            darkTex = MakeColorTex(new Color(0.05f, 0.07f, 0.1f, 0.88f));
            panelTex = MakeColorTex(new Color(0.08f, 0.1f, 0.15f, 0.92f));
            greenTex = MakeColorTex(new Color(0f, 1f, 0.45f, 0.85f));
            redTex = MakeColorTex(new Color(1f, 0.15f, 0.25f, 0.85f));

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = panelTex }
            };

            stylesInitialized = true;
        }

        private Texture2D MakeColorTex(Color col)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, col);
            tex.Apply();
            return tex;
        }

        private void OnGUI()
        {
            InitStyles();

            var gm = GameManager.Instance;
            if (gm == null) return;

            // Draw HUD if playing
            if (gm.CurrentState == GameState.Playing)
            {
                DrawHUD(gm);
            }
            else if (gm.CurrentState == GameState.GameOver)
            {
                DrawGameOver(gm);
            }
            else if (gm.CurrentState == GameState.Victory)
            {
                DrawVictory(gm);
            }
        }

        private void DrawHUD(GameManager gm)
        {
            // Top HUD Banner
            GUI.Box(new Rect(10, 10, 380, 105), "", panelStyle);

            // Lives / Integrity
            labelStyle.normal.textColor = gm.CurrentLives > 1 ? new Color(0.2f, 1f, 0.5f) : new Color(1f, 0.2f, 0.2f);
            string bars = new string('■', Mathf.Max(0, gm.CurrentLives));
            string emptyBars = new string('□', Mathf.Max(0, gm.MaxLives - gm.CurrentLives));
            GUI.Label(new Rect(20, 15, 360, 24), $"INTEGRITY: [{bars}{emptyBars}] ({gm.CurrentLives}/{gm.MaxLives})", labelStyle);

            // Data fragments
            bool hasEnough = gm.CollectedFragments >= gm.RequiredFragments;
            labelStyle.normal.textColor = hasEnough ? new Color(0f, 1f, 0.5f) : new Color(0.9f, 0.9f, 0.9f);
            string reqStatus = hasEnough ? " [EXIT UNLOCKED!]" : $" (Need {gm.RequiredFragments} to exit)";
            GUI.Label(new Rect(20, 40, 360, 24), $"DATA FRAGMENTS: {gm.CollectedFragments}/{gm.TotalFragments}{reqStatus}", labelStyle);

            // Glitch Ability Status
            if (playerGlitch != null)
            {
                if (playerGlitch.IsGlitching)
                {
                    labelStyle.normal.textColor = new Color(0.2f, 1f, 0.6f);
                    GUI.Label(new Rect(20, 65, 360, 24), $"GLITCH: ACTIVE ({playerGlitch.RemainingActiveTime:F1}s) [PHASING HAZARDS]", labelStyle);
                    
                    // Active glitch bar
                    GUI.DrawTexture(new Rect(20, 92, 350 * playerGlitch.NormalizedActiveTime, 10), greenTex);
                }
                else if (playerGlitch.CanGlitch)
                {
                    labelStyle.normal.textColor = new Color(0f, 0.9f, 1f);
                    GUI.Label(new Rect(20, 65, 360, 24), "GLITCH: READY [SHIFT] - Phase through red lasers", labelStyle);
                    GUI.DrawTexture(new Rect(20, 92, 350, 10), greenTex);
                }
                else
                {
                    labelStyle.normal.textColor = new Color(1f, 0.6f, 0.2f);
                    GUI.Label(new Rect(20, 65, 360, 24), $"GLITCH: RECHARGING ({playerGlitch.CooldownTimer:F1}s)", labelStyle);
                    float fill = 1f - playerGlitch.NormalizedCooldown;
                    GUI.DrawTexture(new Rect(20, 92, 350 * fill, 10), panelTex);
                }
            }

            // Controls Banner (Bottom)
            GUI.Box(new Rect(10, Screen.height - 35, 480, 25), "", panelStyle);
            labelStyle.normal.textColor = new Color(0.7f, 0.8f, 0.9f);
            GUI.Label(new Rect(15, Screen.height - 35, 470, 25), "CONTROLS: [A/D] Move | [SPACE] Jump | [SHIFT] Glitch Phase | [R] Restart", labelStyle);
        }

        private void DrawGameOver(GameManager gm)
        {
            // Dark full screen backdrop
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), darkTex);

            float w = 480;
            float h = 260;
            float x = (Screen.width - w) * 0.5f;
            float y = (Screen.height - h) * 0.5f;

            GUI.Box(new Rect(x, y, w, h), "", panelStyle);

            headerStyle.normal.textColor = new Color(1f, 0.2f, 0.3f);
            GUI.Label(new Rect(x, y + 25, w, 35), "SIMULATION PURGED", headerStyle);

            bodyStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
            GUI.Label(new Rect(x, y + 70, w, 25), "SIGNAL LOST - SECURITY PROTOCOL TERMINATED", bodyStyle);

            bodyStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
            GUI.Label(new Rect(x, y + 105, w, 25), $"Data Secured: {gm.CollectedFragments} / {gm.TotalFragments} fragments", bodyStyle);

            if (GUI.Button(new Rect(x + 90, y + 155, 300, 45), "REBOOT SIMULATION [R]", buttonStyle))
            {
                OnRestartButtonClicked();
            }

            bodyStyle.normal.textColor = new Color(0.5f, 0.8f, 1f);
            GUI.Label(new Rect(x, y + 215, w, 20), "Press [R] on keyboard or click button to restart", bodyStyle);
        }

        private void DrawVictory(GameManager gm)
        {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), darkTex);

            float w = 520;
            float h = 300;
            float x = (Screen.width - w) * 0.5f;
            float y = (Screen.height - h) * 0.5f;

            GUI.Box(new Rect(x, y, w, h), "", panelStyle);

            headerStyle.normal.textColor = new Color(0f, 1f, 0.5f);
            GUI.Label(new Rect(x, y + 25, w, 35), "SIMULATION BREACHED!", headerStyle);

            bodyStyle.normal.textColor = new Color(0f, 0.9f, 1f);
            GUI.Label(new Rect(x, y + 70, w, 25), "FIREWALL BYPASSED — GLITCH RUN COMPLETE", bodyStyle);

            bodyStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
            GUI.Label(new Rect(x, y + 110, w, 25), $"Data Fragments Secured: {gm.CollectedFragments} / {gm.TotalFragments}", bodyStyle);
            GUI.Label(new Rect(x, y + 135, w, 25), $"Breach Time: {gm.ElapsedTime:F1} seconds", bodyStyle);

            if (GUI.Button(new Rect(x + 110, y + 185, 300, 45), "RE-ENTER MATRIX [R]", buttonStyle))
            {
                OnRestartButtonClicked();
            }

            bodyStyle.normal.textColor = new Color(0.5f, 0.8f, 1f);
            GUI.Label(new Rect(x, y + 250, w, 20), "Press [R] on keyboard or click button to restart", bodyStyle);
        }
    }
}
