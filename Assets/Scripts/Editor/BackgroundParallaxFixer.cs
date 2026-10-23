using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GlitchRun.Editor
{
    /// <summary>
    /// One-shot fixer: disables the BG_City_Animated Animator/GameObject in Gameplay.unity
    /// and ensures the Parallax_CyberpunkCity layer is active.
    /// Menu: GlitchRun / Fix Background Parallax
    /// </summary>
    public static class BackgroundParallaxFixer
    {
        private const string GameplayScenePath = "Assets/Scenes/Gameplay.unity";
        private const string AnimatedBGName    = "BG_City_Animated";
        private const string ParallaxRootName  = "Parallax_CyberpunkCity";

        [MenuItem("GlitchRun/Fix Background Parallax")]
        public static void FixBackgroundParallax()
        {
            // Back up scene first (copy .unity alongside)
            string backupDir = "Assets/_Backup";
            if (!AssetDatabase.IsValidFolder(backupDir))
            {
                AssetDatabase.CreateFolder("Assets", "_Backup");
            }
            string backupPath = $"{backupDir}/Gameplay_BeforeParallaxFix.unity";
            AssetDatabase.CopyAsset(GameplayScenePath, backupPath);
            Debug.Log($"[BackgroundParallaxFixer] Scene backed up to {backupPath}");

            // Open the Gameplay scene additively so we don't disturb the editor state
            bool wasLoaded = false;
            Scene gameplayScene = default;

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).path == GameplayScenePath)
                {
                    gameplayScene = SceneManager.GetSceneAt(i);
                    wasLoaded = true;
                    break;
                }
            }

            if (!wasLoaded)
            {
                gameplayScene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Additive);
            }

            bool changed = false;

            foreach (var root in gameplayScene.GetRootGameObjects())
            {
                // Disable the old animated background
                if (root.name == AnimatedBGName)
                {
                    if (root.activeSelf)
                    {
                        root.SetActive(false);
                        Debug.Log($"[BackgroundParallaxFixer] Deactivated '{AnimatedBGName}'");
                        changed = true;
                    }

                    // Also disable the Animator component even if re-activated manually
                    var anim = root.GetComponent<Animator>();
                    if (anim != null && anim.enabled)
                    {
                        anim.enabled = false;
                        Debug.Log($"[BackgroundParallaxFixer] Disabled Animator on '{AnimatedBGName}'");
                        changed = true;
                    }
                }

                // Ensure the parallax root is active
                if (root.name == ParallaxRootName)
                {
                    if (!root.activeSelf)
                    {
                        root.SetActive(true);
                        Debug.Log($"[BackgroundParallaxFixer] Activated '{ParallaxRootName}'");
                        changed = true;
                    }

                    // Verify ParallaxLayer children are enabled
                    foreach (var pl in root.GetComponentsInChildren<GlitchRun.Gameplay.ParallaxLayer>(includeInactive: true))
                    {
                        if (!pl.enabled)
                        {
                            pl.enabled = true;
                            changed = true;
                        }
                        if (!pl.gameObject.activeSelf)
                        {
                            pl.gameObject.SetActive(true);
                            changed = true;
                        }
                    }
                }
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(gameplayScene);
                EditorSceneManager.SaveScene(gameplayScene);
                Debug.Log("[BackgroundParallaxFixer] Gameplay scene saved with background fix applied.");
            }
            else
            {
                Debug.Log("[BackgroundParallaxFixer] No changes needed — scene already correct.");
            }

            if (!wasLoaded)
            {
                EditorSceneManager.CloseScene(gameplayScene, removeScene: true);
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "Background Parallax Fixed",
                changed
                    ? $"Done!\n• '{AnimatedBGName}' deactivated\n• '{ParallaxRootName}' active\n• Scene saved\n• Backup: {backupPath}"
                    : "No changes were needed. Scene was already in the correct state.",
                "OK");
        }
    }
}
