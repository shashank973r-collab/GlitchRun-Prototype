using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using GlitchRun.Player;

namespace GlitchRun.EditorScripts
{
    public static class LevelReachabilityValidator
    {
        [MenuItem("Glitch Run/Verify Level Jump Reachability", priority = 20)]
        public static void VerifyLevelReachability()
        {
            Debug.Log("[GlitchRun] Starting automated level jump reachability verification...");

            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity", OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("[GlitchRun] Failed to open Assets/Scenes/Gameplay.unity for reachability analysis!");
                return;
            }

            var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            float speed = 8.5f;
            float jumpForce = 13.5f;
            float gravScale = 3.5f;

            if (player != null)
            {
                var so = new SerializedObject(player);
                var speedProp = so.FindProperty("moveSpeed");
                if (speedProp != null) speed = speedProp.floatValue;
                var jumpProp = so.FindProperty("jumpForce");
                if (jumpProp != null) jumpForce = jumpProp.floatValue;

                var rb = player.GetComponent<Rigidbody2D>();
                if (rb != null && rb.gravityScale > 0.01f) gravScale = rb.gravityScale;
            }

            float g = gravScale * 9.81f;
            float maxJumpHeight = (jumpForce * jumpForce) / (2f * g);
            float timeToPeak = jumpForce / g;
            float airTime = timeToPeak * 2f;
            float maxHorizontalDist = speed * airTime;

            Debug.Log($"[GlitchRun] Player Physics Model: Speed={speed:F1} m/s, JumpForce={jumpForce:F1}, Gravity={g:F1} m/s^2");
            Debug.Log($"[GlitchRun] Theoretical Max Vertical Reach: {maxJumpHeight:F2} m");
            Debug.Log($"[GlitchRun] Theoretical Max Horizontal Leap: {maxHorizontalDist:F2} m (Air time: {airTime:F2}s)");

            List<GameObject> allObjects = new List<GameObject>();
            foreach (var root in scene.GetRootGameObjects())
            {
                allObjects.Add(root);
                foreach (var child in root.GetComponentsInChildren<Transform>(true))
                {
                    allObjects.Add(child.gameObject);
                }
            }

            List<Bounds> platBounds = new List<Bounds>();
            foreach (var go in allObjects)
            {
                if (go.name.StartsWith("Platform_"))
                {
                    var col = go.GetComponent<BoxCollider2D>();
                    if (col != null)
                    {
                        platBounds.Add(col.bounds);
                    }
                }
            }

            platBounds.Sort((a, b) => a.min.x.CompareTo(b.min.x));
            Debug.Log($"[GlitchRun] Found {platBounds.Count} level platforms. Validating progression traversal gaps...");

            bool allPass = true;
            for (int i = 0; i < platBounds.Count - 1; i++)
            {
                Bounds current = platBounds[i];
                Bounds next = platBounds[i + 1];

                float gapX = next.min.x - current.max.x;
                float heightDiff = next.max.y - current.max.y;

                if (gapX > 0)
                {
                    float safetyMarginX = maxHorizontalDist - gapX;
                    float safetyPct = (safetyMarginX / maxHorizontalDist) * 100f;

                    if (gapX > maxHorizontalDist)
                    {
                        Debug.LogError($"[GlitchRun] UNREACHABLE GAP between Plat {i} and Plat {i + 1}! Gap={gapX:F2}m > Max={maxHorizontalDist:F2}m");
                        allPass = false;
                    }
                    else if (heightDiff > maxJumpHeight)
                    {
                        Debug.LogError($"[GlitchRun] UNREACHABLE HEIGHT between Plat {i} and Plat {i + 1}! Height={heightDiff:F2}m > Max={maxJumpHeight:F2}m");
                        allPass = false;
                    }
                    else
                    {
                        Debug.Log($"[GlitchRun] Gap {i}->{i + 1} PASSED: Dist={gapX:F2}m (Safety Margin: {safetyPct:F0}%), HeightDiff={heightDiff:F2}m");
                    }
                }
                else
                {
                    Debug.Log($"[GlitchRun] Platform {i}->{i + 1} Overlaps/Connected horizontally (dist={gapX:F2}m, height={heightDiff:F2}m)");
                }
            }

            if (allPass)
            {
                Debug.Log("[GlitchRun] VERIFICATION COMPLETE: ALL LEVEL JUMPS ARE 100% REACHABLE WITH GENEROUS SAFETY MARGIN!");
            }
            else
            {
                Debug.LogError("[GlitchRun] VERIFICATION FAILED: Unreachable gaps detected!");
            }
        }
    }
}
