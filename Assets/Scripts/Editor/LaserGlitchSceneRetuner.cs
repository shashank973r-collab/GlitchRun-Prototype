using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using GlitchRun.Hazards;
using GlitchRun.Gameplay;
using GlitchRun.Player;

namespace GlitchRun.EditorScripts
{
    public static class LaserGlitchSceneRetuner
    {
        [MenuItem("Glitch Run/Retune Laser Hazards & Glitch Barrier")]
        public static void RetuneSceneAndPrefabs()
        {
            Debug.Log("[GlitchRun] Starting Laser Hazard & Glitch Barrier retune via Editor API...");

            // 1. Update Prefabs
            UpdateLaserPrefab();
            UpdateGlitchBarrierPrefab();

            // 2. Open Gameplay Scene
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity", OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("[GlitchRun] Could not open Assets/Scenes/Gameplay.unity!");
                return;
            }

            // 3. Retune Platforms & Geometry
            RetuneLevelGeometry();

            // 4. Mark dirty and save
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Gameplay.unity");
            AssetDatabase.SaveAssets();

            Debug.Log("[GlitchRun] Scene and prefabs successfully retuned and saved!");
        }

        [MenuItem("Glitch Run/Validate Scene Integrity")]
        public static void ValidateSceneNoMissingScripts()
        {
            Debug.Log("[GlitchRun] Reopening Gameplay.unity headlessly to validate scene integrity...");
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity", OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("[GlitchRun] Failed to open Assets/Scenes/Gameplay.unity!");
                return;
            }

            int missingCount = 0;
            int totalComponents = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var child in root.GetComponentsInChildren<Transform>(true))
                {
                    var comps = child.GetComponents<Component>();
                    foreach (var c in comps)
                    {
                        totalComponents++;
                        if (c == null)
                        {
                            Debug.LogError($"[GlitchRun] Missing MonoBehaviour script reference on GameObject '{child.gameObject.name}'!");
                            missingCount++;
                        }
                    }
                }
            }

            if (missingCount == 0)
            {
                Debug.Log($"[GlitchRun] SCENE INTEGRITY VERIFIED: All {totalComponents} components across all GameObjects in Gameplay.unity are valid. Zero missing scripts or references!");
            }
            else
            {
                Debug.LogError($"[GlitchRun] SCENE INTEGRITY FAILED: Found {missingCount} missing component references!");
            }
        }

        private static void UpdateLaserPrefab()
        {
            string prefabPath = "Assets/Prefabs/PF_LaserHazard.prefab";
            var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefabRoot != null)
            {
                var col = prefabRoot.GetComponent<BoxCollider2D>();
                if (col != null)
                {
                    col.size = new Vector2(0.32f, 0.75f);
                    col.offset = Vector2.zero;
                    col.isTrigger = true;
                }

                var sr = prefabRoot.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.size = new Vector2(0.32f, 0.75f);
                }

                var laser = prefabRoot.GetComponent<LaserHazard>();
                if (laser != null)
                {
                    var so = new SerializedObject(laser);
                    so.FindProperty("canBeGlitched").boolValue = false;
                    so.ApplyModifiedProperties();
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                Debug.Log("[GlitchRun] PF_LaserHazard prefab updated.");
            }
        }

        private static void UpdateGlitchBarrierPrefab()
        {
            string prefabPath = "Assets/Prefabs/PF_GlitchBarrier.prefab";
            var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefabRoot != null)
            {
                var col = prefabRoot.GetComponent<BoxCollider2D>();
                if (col != null)
                {
                    col.size = new Vector2(0.6f, 3.8f);
                    col.offset = Vector2.zero;
                    col.isTrigger = true;
                }

                var sr = prefabRoot.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.size = new Vector2(0.6f, 3.8f);
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                Debug.Log("[GlitchRun] PF_GlitchBarrier prefab updated.");
            }
        }

        private static void RetuneLevelGeometry()
        {
            // Platforms
            SetTransform("Platform_Start", new Vector3(2.0f, -1.5f, 0f), new Vector3(6.0f, 1.0f, 1f));
            SetTransform("DataFragment_01", new Vector3(5.0f, 0.0f, 0f), Vector3.one);
            SetTransform("Platform_Checkpoint1", new Vector3(10.5f, -1.5f, 0f), new Vector3(4.5f, 1.0f, 1f));
            SetTransform("Checkpoint_01", new Vector3(10.5f, -0.6f, 0f), Vector3.one);

            // Section 2: Platform for Low Laser + Platform for Glitch Barrier
            SetTransform("Platform_Section2", new Vector3(17.0f, -1.5f, 0f), new Vector3(6.5f, 1.0f, 1f));
            
            // Low jumpable laser (height 0.75m above floor y = -1.0 -> center y = -0.625)
            SetTransform("LaserHazard_Static_01", new Vector3(17.0f, -0.625f, 0f), Vector3.one);
            var laser1 = GameObject.Find("LaserHazard_Static_01");
            if (laser1 != null)
            {
                var col = laser1.GetComponent<BoxCollider2D>();
                if (col != null) { col.size = new Vector2(0.32f, 0.75f); col.offset = Vector2.zero; }
                var sr = laser1.GetComponent<SpriteRenderer>();
                if (sr != null) { sr.size = new Vector2(0.32f, 0.75f); }
                var hz = laser1.GetComponent<LaserHazard>();
                if (hz != null)
                {
                    var so = new SerializedObject(hz);
                    so.FindProperty("canBeGlitched").boolValue = false;
                    so.FindProperty("isCycling").boolValue = false;
                    so.ApplyModifiedProperties();
                }
            }

            // Platform for Glitch Barrier: Ensure Platform_GlitchGate exists
            var platGlitch = GameObject.Find("Platform_GlitchGate");
            if (platGlitch == null)
            {
                var platSection2 = GameObject.Find("Platform_Section2");
                if (platSection2 != null)
                {
                    platGlitch = Object.Instantiate(platSection2);
                    platGlitch.name = "Platform_GlitchGate";
                }
            }
            if (platGlitch != null)
            {
                platGlitch.transform.position = new Vector3(25.5f, -1.5f, 0f);
                platGlitch.transform.localScale = new Vector3(8.0f, 1.0f, 1f);
            }

            // Checkpoint before first glitch barrier
            var cpGlitch = GameObject.Find("Checkpoint_Glitch");
            if (cpGlitch == null)
            {
                var cp1 = GameObject.Find("Checkpoint_01");
                if (cp1 != null)
                {
                    cpGlitch = Object.Instantiate(cp1);
                    cpGlitch.name = "Checkpoint_Glitch";
                }
            }
            if (cpGlitch != null)
            {
                cpGlitch.transform.position = new Vector3(22.5f, -0.6f, 0f);
            }

            // Glitch Barrier 01 (Tall Barrier: height 3.8m above floor y = -1.0 -> center y = 0.9)
            SetTransform("GlitchBarrier_01", new Vector3(26.0f, 0.9f, 0f), Vector3.one);
            var gb1 = GameObject.Find("GlitchBarrier_01");
            if (gb1 != null)
            {
                var col = gb1.GetComponent<BoxCollider2D>();
                if (col != null) { col.size = new Vector2(0.6f, 3.8f); col.offset = Vector2.zero; }
                var sr = gb1.GetComponent<SpriteRenderer>();
                if (sr != null) { sr.size = new Vector2(0.6f, 3.8f); }
            }

            SetTransform("DataFragment_02", new Vector3(27.5f, 0.0f, 0f), Vector3.one);

            // Section 3: Step Platforms
            SetTransform("Platform_Step_A", new Vector3(32.0f, -0.5f, 0f), new Vector3(3.0f, 1.0f, 1f));
            SetTransform("Platform_Step_B", new Vector3(38.0f, 0.8f, 0f), new Vector3(7.0f, 1.0f, 1f));
            
            // Low cycling laser on Step B (surface at y = 1.3 -> center at y = 1.675)
            SetTransform("LaserHazard_Cycling_02", new Vector3(38.0f, 1.675f, 0f), Vector3.one);
            var laser2 = GameObject.Find("LaserHazard_Cycling_02");
            if (laser2 != null)
            {
                var col = laser2.GetComponent<BoxCollider2D>();
                if (col != null) { col.size = new Vector2(0.32f, 0.75f); col.offset = Vector2.zero; }
                var sr = laser2.GetComponent<SpriteRenderer>();
                if (sr != null) { sr.size = new Vector2(0.32f, 0.75f); }
                var hz = laser2.GetComponent<LaserHazard>();
                if (hz != null)
                {
                    var so = new SerializedObject(hz);
                    so.FindProperty("canBeGlitched").boolValue = false;
                    so.FindProperty("isCycling").boolValue = true;
                    so.FindProperty("activeTime").floatValue = 2.2f;
                    so.FindProperty("inactiveTime").floatValue = 1.6f;
                    so.ApplyModifiedProperties();
                }
            }

            SetTransform("DataFragment_03_ObjectiveMet", new Vector3(38.0f, 3.0f, 0f), Vector3.one);
            SetTransform("Platform_Step_C", new Vector3(44.0f, 0.0f, 0f), new Vector3(3.0f, 1.0f, 1f));
            SetTransform("Checkpoint_02", new Vector3(44.0f, 1.0f, 0f), Vector3.one);

            // Section 4: Corridor & Drone
            SetTransform("Platform_Corridor", new Vector3(53.0f, -1.5f, 0f), new Vector3(12.0f, 1.0f, 1f));
            SetTransform("Drone_Patrol_01", new Vector3(50.5f, 0.5f, 0f), Vector3.one);
            var drone = GameObject.Find("Drone_Patrol_01");
            if (drone != null)
            {
                var dh = drone.GetComponent<DroneHazard>();
                if (dh != null)
                {
                    dh.SetWaypoints(new Vector3(48.5f, 0.5f, 0f), new Vector3(52.5f, 0.5f, 0f));
                }
            }

            SetTransform("DataFragment_04", new Vector3(51.0f, 0.0f, 0f), Vector3.one);

            // Low cycling laser on Corridor (surface at y = -1.0 -> center at y = -0.625)
            SetTransform("LaserHazard_Cycling_03", new Vector3(56.0f, -0.625f, 0f), Vector3.one);
            var laser3 = GameObject.Find("LaserHazard_Cycling_03");
            if (laser3 != null)
            {
                var col = laser3.GetComponent<BoxCollider2D>();
                if (col != null) { col.size = new Vector2(0.32f, 0.75f); col.offset = Vector2.zero; }
                var sr = laser3.GetComponent<SpriteRenderer>();
                if (sr != null) { sr.size = new Vector2(0.32f, 0.75f); }
                var hz = laser3.GetComponent<LaserHazard>();
                if (hz != null)
                {
                    var so = new SerializedObject(hz);
                    so.FindProperty("canBeGlitched").boolValue = false;
                    so.FindProperty("isCycling").boolValue = true;
                    so.FindProperty("activeTime").floatValue = 2.2f;
                    so.FindProperty("inactiveTime").floatValue = 1.6f;
                    so.ApplyModifiedProperties();
                }
            }

            SetTransform("Platform_Secret", new Vector3(53.0f, 2.5f, 0f), new Vector3(2.0f, 0.8f, 1f));
            SetTransform("DataFragment_05_Bonus", new Vector3(53.0f, 3.7f, 0f), Vector3.one);

            // Section 5: Exit
            SetTransform("Platform_Exit", new Vector3(63.5f, -1.0f, 0f), new Vector3(6.0f, 1.2f, 1f));
            SetTransform("ExitGate", new Vector3(64.5f, 0.8f, 0f), Vector3.one);
        }

        private static void SetTransform(string objectName, Vector3 pos, Vector3 scale)
        {
            var go = GameObject.Find(objectName);
            if (go != null)
            {
                go.transform.position = pos;
                go.transform.localScale = scale;
            }
            else
            {
                Debug.LogWarning($"[GlitchRun] Object '{objectName}' not found in scene for transform update.");
            }
        }
    }
}
