using UnityEngine;
using UnityEngine.SceneManagement;

namespace GlitchRun.UI
{
    /// <summary>
    /// Controller for the main menu screen in GLITCH RUN.
    /// Manages transitions from the title/menu screen into the gameplay scene,
    /// validates scene availability in Build Settings, and handles application exit.
    /// Does not require or alter GameManager state.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        #region Inspector Settings
        [Header("Scene Navigation")]
        [Tooltip("The name of the gameplay scene to load when clicking Play. Must match a scene added to File > Build Settings.")]
        [SerializeField] private string gameplaySceneName = "SampleScene";
        #endregion

        #region Public Methods (Button Callbacks)
        /// <summary>
        /// Loads the target gameplay scene.
        /// Checks that the scene is registered in Unity Build Settings before attempting to load.
        /// Bind this method to the 'Play' Button's OnClick() event in the Unity Inspector.
        /// </summary>
        public void PlayGame()
        {
            if (string.IsNullOrWhiteSpace(gameplaySceneName))
            {
                Debug.LogError("[MainMenuUI] Gameplay scene name is empty. Please assign a valid scene name in the Inspector.");
                return;
            }

            // Verify whether the scene exists and is registered in Build Settings.
            if (!Application.CanStreamedLevelBeLoaded(gameplaySceneName))
            {
                Debug.LogError($"[MainMenuUI] Cannot load scene '{gameplaySceneName}'. " +
                               "The scene does not exist or has not been added to 'File > Build Settings > Scenes In Build'. " +
                               "Please open Build Settings in Unity, add the gameplay scene, and try again.");
                return;
            }

            Debug.Log($"[MainMenuUI] Loading gameplay scene: '{gameplaySceneName}'...");
            SceneManager.LoadScene(gameplaySceneName);
        }

        /// <summary>
        /// Closes the application.
        /// In the Unity Editor, stops Play Mode. In a built standalone executable, exits the game.
        /// Bind this method to the 'Quit' Button's OnClick() event in the Unity Inspector.
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("[MainMenuUI] Quitting game application...");
            Application.Quit();

#if UNITY_EDITOR
            // Allow the quit button to be verified inside the Unity Editor without building an executable.
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
        #endregion
    }
}
