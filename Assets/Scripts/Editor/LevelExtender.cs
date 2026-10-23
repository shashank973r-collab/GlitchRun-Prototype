using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using GlitchRun.Hazards;
using GlitchRun.Gameplay;
using GlitchRun.Core;

namespace GlitchRun.EditorScripts
{
    public static class LevelExtender
    {
        [MenuItem("Glitch Run/Extend Level")]
        public static void ExtendLevel()
        {
            Debug.Log("[GlitchRun] Starting Level Extension (5 Sections, 8-10 min run, Parallax Background)...");

            // 1. Generate seamless cyberpunk background textures
            CyberpunkBackgroundGenerator.GenerateAllBackgrounds();

            // 2. Open Gameplay Scene
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity", OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("[GlitchRun] Failed to open Assets/Scenes/Gameplay.unity!");
                return;
            }

            // 3. Load Prefabs
            var platformPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PF_Platform.prefab");
            var laserPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PF_LaserHazard.prefab");
            var glitchBarrierPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PF_GlitchBarrier.prefab");
            var dronePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PF_DroneHazard.prefab");
            var fragmentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PF_DataFragment.prefab");
            var checkpointPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PF_Checkpoint.prefab");

            // 4. Setup Parallax Background Hierarchy
            SetupParallaxBackground();

            // 5. Build 5 Clean Section Groups
            OrganizeExistingSection1();
            BuildSection2(platformPrefab, laserPrefab, fragmentPrefab);
            BuildSection3(platformPrefab, laserPrefab, glitchBarrierPrefab, checkpointPrefab, fragmentPrefab);
            BuildSection4(platformPrefab, laserPrefab, glitchBarrierPrefab, dronePrefab, checkpointPrefab, fragmentPrefab);
            BuildSection5(platformPrefab, laserPrefab, glitchBarrierPrefab, dronePrefab, checkpointPrefab, fragmentPrefab);

            // 6. Update DeathZone, Camera Bounds, and Boundaries
            UpdateWorldBoundariesAndCamera();

            // 7. Update GameManager required fragments
            var gm = Object.FindAnyObjectByType<GameManager>();
            if (gm != null)
            {
                var so = new SerializedObject(gm);
                so.FindProperty("requiredFragments").intValue = 15;
                so.ApplyModifiedProperties();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Gameplay.unity");
            AssetDatabase.SaveAssets();

            Debug.Log("[GlitchRun] Level successfully extended across 5 sections to X=370m! All prefabs, encounters, and parallax configured.");
        }

        private static void SetupParallaxBackground()
        {
            // Remove old Animator-based Background if present
            var oldBg = GameObject.Find("Background");
            if (oldBg != null)
            {
                var anim = oldBg.GetComponent<Animator>();
                if (anim != null) Object.DestroyImmediate(anim);
                oldBg.name = "Background_Old_Disabled";
                oldBg.SetActive(false);
            }

            var parallaxRoot = GameObject.Find("Parallax_CyberpunkCity");
            if (parallaxRoot == null)
            {
                parallaxRoot = new GameObject("Parallax_CyberpunkCity");
            }

            // Layer 1: Far Skyline (Factor 0.25)
            CreateParallaxLayer(parallaxRoot, "Layer_FarSkyline", "Assets/Art/Environments/Background/bg_cyberpunk_far.png", 0.2f, 0.05f, 2f, -25, 20.48f);

            // Layer 2: Mid City Skyline (Factor 0.5)
            CreateParallaxLayer(parallaxRoot, "Layer_MidSkyline", "Assets/Art/Environments/Background/bg_cyberpunk_mid.png", 0.5f, 0.12f, 0.5f, -15, 20.48f);

            // Layer 3: Near Silhouette Rooftops (Factor 0.75)
            CreateParallaxLayer(parallaxRoot, "Layer_NearSkyline", "Assets/Art/Environments/Background/bg_cyberpunk_near.png", 0.75f, 0.2f, -1.5f, -8, 20.48f);
        }

        private static void CreateParallaxLayer(GameObject root, string name, string spritePath, float factorX, float factorY, float vertOffset, int order, float tileWidth)
        {
            var layerObj = GameObject.Find(name);
            if (layerObj == null)
            {
                layerObj = new GameObject(name);
                layerObj.transform.parent = root.transform;
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            var pl = layerObj.GetComponent<ParallaxLayer>();
            if (pl == null) pl = layerObj.AddComponent<ParallaxLayer>();

            var so = new SerializedObject(pl);
            so.FindProperty("parallaxFactorX").floatValue = factorX;
            so.FindProperty("parallaxFactorY").floatValue = factorY;
            so.FindProperty("verticalOffset").floatValue = vertOffset;

            // Setup 3 child tile SpriteRenderers
            SpriteRenderer[] tiles = new SpriteRenderer[3];
            for (int i = 0; i < 3; i++)
            {
                string tileName = $"Tile_{i}";
                Transform child = layerObj.transform.Find(tileName);
                if (child == null)
                {
                    var childObj = new GameObject(tileName);
                    childObj.transform.parent = layerObj.transform;
                    child = childObj.transform;
                }
                var sr = child.GetComponent<SpriteRenderer>();
                if (sr == null) sr = child.gameObject.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = order;
                tiles[i] = sr;
            }

            var tilesProp = so.FindProperty("tileRenderers");
            tilesProp.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                tilesProp.GetArrayElementAtIndex(i).objectReferenceValue = tiles[i];
            }
            so.ApplyModifiedProperties();
        }

        private static void OrganizeExistingSection1()
        {
            var sec1 = GameObject.Find("Section_1_SimulationEntry");
            if (sec1 == null) sec1 = new GameObject("Section_1_SimulationEntry");

            string[] s1Names = new string[] {
                "Platform_Start", "Platform_Checkpoint1", "Platform_Section2", "Platform_GlitchGate",
                "Platform_Step_A", "Platform_Step_B", "Platform_Step_C", "Platform_Corridor", "Platform_Secret",
                "LaserHazard_Static_01", "GlitchBarrier_01", "LaserHazard_Cycling_02", "LaserHazard_Cycling_03",
                "Drone_Patrol_01", "Checkpoint_01", "Checkpoint_Glitch", "Checkpoint_02",
                "DataFragment_01", "DataFragment_02", "DataFragment_03_ObjectiveMet", "DataFragment_04", "DataFragment_05_Bonus"
            };

            foreach (var n in s1Names)
            {
                var go = GameObject.Find(n);
                if (go != null) go.transform.parent = sec1.transform;
            }
        }

        private static void BuildSection2(GameObject platPrefab, GameObject laserPrefab, GameObject fragPrefab)
        {
            var sec2 = GameObject.Find("Section_2_NeonRooftops");
            if (sec2 == null) sec2 = new GameObject("Section_2_NeonRooftops");

            // Platforms
            SpawnPlat(platPrefab, sec2, "Platform_Sec2_01", new Vector3(68f, -1.0f, 0f), new Vector3(8.0f, 1.0f, 1f));
            SpawnPlat(platPrefab, sec2, "Platform_Sec2_02", new Vector3(79.5f, 0.2f, 0f), new Vector3(7.0f, 1.0f, 1f));
            SpawnPlat(platPrefab, sec2, "Platform_Sec2_03", new Vector3(91.0f, 1.5f, 0f), new Vector3(8.0f, 1.0f, 1f));
            SpawnPlat(platPrefab, sec2, "Platform_Sec2_04", new Vector3(103.0f, 0.2f, 0f), new Vector3(7.0f, 1.0f, 1f));
            SpawnPlat(platPrefab, sec2, "Platform_Sec2_05", new Vector3(115.0f, -0.8f, 0f), new Vector3(9.0f, 1.0f, 1f));
            SpawnPlat(platPrefab, sec2, "Platform_Sec2_06", new Vector3(128.0f, 0.5f, 0f), new Vector3(8.0f, 1.0f, 1f));
            SpawnPlat(platPrefab, sec2, "Platform_Sec2_Secret", new Vector3(91.0f, 3.8f, 0f), new Vector3(2.5f, 0.8f, 1f));

            // Encounters: Low jumpable lasers
            SpawnLowLaser(laserPrefab, sec2, "LaserHazard_Sec2_01", new Vector3(68f, -0.125f, 0f), false);
            SpawnLowLaser(laserPrefab, sec2, "LaserHazard_Sec2_02", new Vector3(91f, 2.375f, 0f), false);
            SpawnLowLaser(laserPrefab, sec2, "LaserHazard_Sec2_03", new Vector3(115f, 0.075f, 0f), true);

            // Data Fragments
            SpawnFrag(fragPrefab, sec2, "DataFragment_06", new Vector3(68f, 1.0f, 0f));
            SpawnFrag(fragPrefab, sec2, "DataFragment_07", new Vector3(79.5f, 1.8f, 0f));
            SpawnFrag(fragPrefab, sec2, "DataFragment_08_Bonus", new Vector3(91.0f, 4.8f, 0f));
            SpawnFrag(fragPrefab, sec2, "DataFragment_09", new Vector3(103.0f, 1.8f, 0f));
            SpawnFrag(fragPrefab, sec2, "DataFragment_10", new Vector3(128.0f, 2.0f, 0f));

            // Section 2 Visual Flavor Props
            AddNeonSign(sec2, "Prop_NeonAd_01", new Vector3(75f, 2.5f, 0f), new Color(0f, 1f, 0.85f, 0.9f));
            AddNeonSign(sec2, "Prop_NeonAd_02", new Vector3(110f, 1.5f, 0f), new Color(1f, 0.1f, 0.6f, 0.9f));
        }

        private static void BuildSection3(GameObject platPrefab, GameObject laserPrefab, GameObject barrierPrefab, GameObject cpPrefab, GameObject fragPrefab)
        {
            var sec3 = GameObject.Find("Section_3_SecurityCorridor");
            if (sec3 == null) sec3 = new GameObject("Section_3_SecurityCorridor");

            // Checkpoint BEFORE hard sequence
            SpawnCheckpoint(cpPrefab, sec3, "Checkpoint_03", new Vector3(137.0f, 0.0f, 0f));

            // Platforms
            SpawnPlat(platPrefab, sec3, "Platform_Sec3_01", new Vector3(142.0f, -0.8f, 0f), new Vector3(12.0f, 1.0f, 1f));
            SpawnPlat(platPrefab, sec3, "Platform_Sec3_02", new Vector3(158.0f, -0.8f, 0f), new Vector3(11.0f, 1.0f, 1f));
            SpawnPlat(platPrefab, sec3, "Platform_Sec3_03", new Vector3(175.0f, -0.2f, 0f), new Vector3(12.0f, 1.0f, 1f));
            SpawnPlat(platPrefab, sec3, "Platform_Sec3_04", new Vector3(192.0f, -0.2f, 0f), new Vector3(12.0f, 1.0f, 1f));

            // Encounters: Tall Glitch Barriers alternating with Low Lasers
            SpawnTallBarrier(barrierPrefab, sec3, "GlitchBarrier_Sec3_A", new Vector3(143.0f, 1.6f, 0f)); // Approach 7m, landing 4m
            SpawnLowLaser(laserPrefab, sec3, "LaserHazard_Sec3_01", new Vector3(158.0f, 0.075f, 0f), false);
            SpawnTallBarrier(barrierPrefab, sec3, "GlitchBarrier_Sec3_B", new Vector3(176.0f, 2.2f, 0f)); // >3.5s cooldown space from prev phase!
            SpawnLowLaser(laserPrefab, sec3, "LaserHazard_Sec3_02", new Vector3(192.0f, 0.675f, 0f), true);

            // Fragments
            SpawnFrag(fragPrefab, sec3, "DataFragment_11", new Vector3(145.0f, 0.5f, 0f));
            SpawnFrag(fragPrefab, sec3, "DataFragment_12", new Vector3(158.0f, 1.5f, 0f));
            SpawnFrag(fragPrefab, sec3, "DataFragment_13", new Vector3(178.0f, 1.0f, 0f));
            SpawnFrag(fragPrefab, sec3, "DataFragment_14", new Vector3(192.0f, 2.0f, 0f));
            SpawnFrag(fragPrefab, sec3, "DataFragment_15", new Vector3(196.0f, 0.8f, 0f));

            // Security gate warning banners
            AddNeonSign(sec3, "Prop_GateMarker_01", new Vector3(141.0f, 3.2f, 0f), new Color(0.9f, 0.1f, 0.95f, 0.9f));
            AddNeonSign(sec3, "Prop_GateMarker_02", new Vector3(174.0f, 3.8f, 0f), new Color(0.9f, 0.1f, 0.95f, 0.9f));
        }

        private static void BuildSection4(GameObject platPrefab, GameObject laserPrefab, GameObject barrierPrefab, GameObject dronePrefab, GameObject cpPrefab, GameObject fragPrefab)
        {
            var sec4 = GameObject.Find("Section_4_CorruptedDataZone");
            if (sec4 == null) sec4 = new GameObject("Section_4_CorruptedDataZone");

            // Checkpoint BEFORE corrupted sequence
            SpawnCheckpoint(cpPrefab, sec4, "Checkpoint_04", new Vector3(204.0f, 0.0f, 0f));

            // Platforms
            SpawnPlat(platPrefab, sec4, "Platform_Sec4_01", new Vector3(210.0f, -0.8f, 0f), new Vector3(14.0f, 1.0f, 1f));
            SpawnPlat(platPrefab, sec4, "Platform_Sec4_02", new Vector3(228.0f, 0.5f, 0f), new Vector3(10.0f, 1.0f, 1f));
            SpawnPlat(platPrefab, sec4, "Platform_Sec4_03", new Vector3(243.0f, 1.8f, 0f), new Vector3(11.0f, 1.0f, 1f));
            SpawnPlat(platPrefab, sec4, "Platform_Sec4_04", new Vector3(259.0f, 0.5f, 0f), new Vector3(11.0f, 1.0f, 1f));
            SpawnPlat(platPrefab, sec4, "Platform_Sec4_05", new Vector3(276.0f, -0.6f, 0f), new Vector3(13.0f, 1.0f, 1f));

            // Encounters: Drone Patrol + Glitch Barrier + Low Lasers
            SpawnDrone(dronePrefab, sec4, "Drone_Patrol_Sec4_A", new Vector3(208.0f, 1.2f, 0f), new Vector3(206f, 1.2f, 0f), new Vector3(211f, 1.2f, 0f));
            SpawnLowLaser(laserPrefab, sec4, "LaserHazard_Sec4_01", new Vector3(214.0f, 0.075f, 0f), true);
            SpawnTallBarrier(barrierPrefab, sec4, "GlitchBarrier_Sec4_A", new Vector3(229.0f, 2.9f, 0f));
            SpawnLowLaser(laserPrefab, sec4, "LaserHazard_Sec4_02", new Vector3(243.0f, 2.675f, 0f), false);
            SpawnDrone(dronePrefab, sec4, "Drone_Patrol_Sec4_B", new Vector3(259.0f, 2.5f, 0f), new Vector3(256f, 2.5f, 0f), new Vector3(262f, 2.5f, 0f));
            SpawnTallBarrier(barrierPrefab, sec4, "GlitchBarrier_Sec4_B", new Vector3(278.0f, 1.8f, 0f));

            // Fragments
            SpawnFrag(fragPrefab, sec4, "DataFragment_16", new Vector3(211.0f, 0.5f, 0f));
            SpawnFrag(fragPrefab, sec4, "DataFragment_17", new Vector3(230.5f, 1.5f, 0f));
            SpawnFrag(fragPrefab, sec4, "DataFragment_18", new Vector3(243.0f, 3.5f, 0f));
            SpawnFrag(fragPrefab, sec4, "DataFragment_19", new Vector3(259.0f, 1.5f, 0f));
            SpawnFrag(fragPrefab, sec4, "DataFragment_20", new Vector3(280.0f, 0.5f, 0f));

            // Glitch flavor props
            AddNeonSign(sec4, "Prop_GlitchDistortion_01", new Vector3(220f, 3.0f, 0f), new Color(0f, 1f, 0.5f, 0.8f));
            AddNeonSign(sec4, "Prop_GlitchDistortion_02", new Vector3(250f, 3.8f, 0f), new Color(0.8f, 0f, 1f, 0.8f));
        }

        private static void BuildSection5(GameObject platPrefab, GameObject laserPrefab, GameObject barrierPrefab, GameObject dronePrefab, GameObject cpPrefab, GameObject fragPrefab)
        {
            var sec5 = GameObject.Find("Section_5_FinalEscape");
            if (sec5 == null) sec5 = new GameObject("Section_5_FinalEscape");

            // Final Checkpoint BEFORE the climax
            SpawnCheckpoint(cpPrefab, sec5, "Checkpoint_05", new Vector3(288.0f, 0.3f, 0f));

            // Platforms
            SpawnPlat(platPrefab, sec5, "Platform_Sec5_01", new Vector3(294.0f, -0.6f, 0f), new Vector3(12.0f, 1.0f, 1f));
            SpawnPlat(platPrefab, sec5, "Platform_Sec5_02", new Vector3(310.0f, 0.6f, 0f), new Vector3(11.0f, 1.0f, 1f));
            SpawnPlat(platPrefab, sec5, "Platform_Sec5_03", new Vector3(326.0f, 1.8f, 0f), new Vector3(11.0f, 1.0f, 1f));
            SpawnPlat(platPrefab, sec5, "Platform_Sec5_04", new Vector3(342.0f, 0.5f, 0f), new Vector3(12.0f, 1.0f, 1f));
            SpawnPlat(platPrefab, sec5, "Platform_Sec5_05", new Vector3(358.0f, -0.6f, 0f), new Vector3(11.0f, 1.0f, 1f));
            SpawnPlat(platPrefab, sec5, "Platform_Sec5_Exit", new Vector3(370.0f, -0.6f, 0f), new Vector3(8.0f, 1.2f, 1f));

            // Encounters: Climax combination
            SpawnLowLaser(laserPrefab, sec5, "LaserHazard_Sec5_01", new Vector3(295.0f, 0.275f, 0f), true);
            SpawnTallBarrier(barrierPrefab, sec5, "GlitchBarrier_Sec5_A", new Vector3(311.0f, 3.0f, 0f));
            SpawnDrone(dronePrefab, sec5, "Drone_Patrol_Sec5", new Vector3(326.0f, 3.8f, 0f), new Vector3(323f, 3.8f, 0f), new Vector3(329f, 3.8f, 0f));
            SpawnLowLaser(laserPrefab, sec5, "LaserHazard_Sec5_02", new Vector3(343.0f, 1.375f, 0f), false);
            SpawnTallBarrier(barrierPrefab, sec5, "GlitchBarrier_Sec5_B", new Vector3(359.0f, 1.8f, 0f));

            // Move the real ExitGate to the end of Section 5!
            var exitGate = GameObject.Find("ExitGate");
            if (exitGate != null)
            {
                exitGate.transform.parent = sec5.transform;
                exitGate.transform.position = new Vector3(371.5f, 1.0f, 0f);
            }

            // Final batch of Data Fragments
            SpawnFrag(fragPrefab, sec5, "DataFragment_21", new Vector3(294.0f, 1.5f, 0f));
            SpawnFrag(fragPrefab, sec5, "DataFragment_22", new Vector3(302.0f, 1.5f, 0f));
            SpawnFrag(fragPrefab, sec5, "DataFragment_23", new Vector3(312.5f, 2.0f, 0f));
            SpawnFrag(fragPrefab, sec5, "DataFragment_24", new Vector3(318.0f, 2.5f, 0f));
            SpawnFrag(fragPrefab, sec5, "DataFragment_25", new Vector3(326.0f, 3.0f, 0f));
            SpawnFrag(fragPrefab, sec5, "DataFragment_26", new Vector3(334.0f, 2.5f, 0f));
            SpawnFrag(fragPrefab, sec5, "DataFragment_27", new Vector3(343.0f, 2.5f, 0f));
            SpawnFrag(fragPrefab, sec5, "DataFragment_28", new Vector3(350.0f, 1.5f, 0f));
            SpawnFrag(fragPrefab, sec5, "DataFragment_29", new Vector3(360.5f, 1.0f, 0f));
            SpawnFrag(fragPrefab, sec5, "DataFragment_30_Climax", new Vector3(369.0f, 0.8f, 0f));

            // Grand Exit Visual Markers
            AddNeonSign(sec5, "Prop_ExitPortalMarker_01", new Vector3(367f, 3.0f, 0f), new Color(0f, 1f, 0.5f, 1f));
            AddNeonSign(sec5, "Prop_ExitPortalMarker_02", new Vector3(374f, 3.0f, 0f), new Color(0f, 1f, 0.5f, 1f));
        }

        private static void UpdateWorldBoundariesAndCamera()
        {
            // Update DeathZone to span full length (-10 to 390)
            var deathZone = GameObject.Find("DeathZone");
            if (deathZone != null)
            {
                deathZone.transform.position = new Vector3(190f, -6f, 0f);
                var col = deathZone.GetComponent<BoxCollider2D>();
                if (col != null)
                {
                    col.size = new Vector2(420f, 4f);
                }
            }

            // Anti-bypass wall at the far right
            var rightWall = GameObject.Find("Boundary_RightWall");
            if (rightWall == null)
            {
                rightWall = new GameObject("Boundary_RightWall");
                var col = rightWall.AddComponent<BoxCollider2D>();
                col.size = new Vector2(2f, 20f);
            }
            rightWall.transform.position = new Vector3(376f, 5f, 0f);

            // Camera bounds
            var cam = Object.FindAnyObjectByType<CameraFollow2D>();
            if (cam != null)
            {
                var so = new SerializedObject(cam);
                so.FindProperty("maxBounds").vector2Value = new Vector2(380f, 10f);
                so.ApplyModifiedProperties();
            }
        }

        private static GameObject SpawnPlat(GameObject prefab, GameObject parent, string name, Vector3 pos, Vector3 scale)
        {
            var go = GameObject.Find(name);
            if (go == null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
                go.name = name;
            }
            go.transform.position = pos;
            go.transform.localScale = scale;
            return go;
        }

        private static void SpawnLowLaser(GameObject prefab, GameObject parent, string name, Vector3 pos, bool cycling)
        {
            var go = GameObject.Find(name);
            if (go == null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
                go.name = name;
            }
            go.transform.position = pos;
            go.transform.localScale = Vector3.one;

            var col = go.GetComponent<BoxCollider2D>();
            if (col != null) { col.size = new Vector2(0.32f, 0.75f); col.offset = Vector2.zero; col.isTrigger = true; }
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null) { sr.size = new Vector2(0.32f, 0.75f); }

            var hz = go.GetComponent<LaserHazard>();
            if (hz != null)
            {
                var so = new SerializedObject(hz);
                so.FindProperty("canBeGlitched").boolValue = false;
                so.FindProperty("isCycling").boolValue = cycling;
                if (cycling)
                {
                    so.FindProperty("activeTime").floatValue = 2.2f;
                    so.FindProperty("inactiveTime").floatValue = 1.6f;
                }
                so.ApplyModifiedProperties();
            }
        }

        private static void SpawnTallBarrier(GameObject prefab, GameObject parent, string name, Vector3 pos)
        {
            var go = GameObject.Find(name);
            if (go == null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
                go.name = name;
            }
            go.transform.position = pos;
            go.transform.localScale = Vector3.one;

            var col = go.GetComponent<BoxCollider2D>();
            if (col != null) { col.size = new Vector2(0.6f, 3.8f); col.offset = Vector2.zero; col.isTrigger = true; }
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null) { sr.size = new Vector2(0.6f, 3.8f); }
        }

        private static void SpawnDrone(GameObject prefab, GameObject parent, string name, Vector3 pos, Vector3 wpA, Vector3 wpB)
        {
            var go = GameObject.Find(name);
            if (go == null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
                go.name = name;
            }
            go.transform.position = pos;
            var drone = go.GetComponent<DroneHazard>();
            if (drone != null)
            {
                drone.SetWaypoints(wpA, wpB);
            }
        }

        private static void SpawnCheckpoint(GameObject prefab, GameObject parent, string name, Vector3 pos)
        {
            var go = GameObject.Find(name);
            if (go == null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
                go.name = name;
            }
            go.transform.position = pos;
        }

        private static void SpawnFrag(GameObject prefab, GameObject parent, string name, Vector3 pos)
        {
            var go = GameObject.Find(name);
            if (go == null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
                go.name = name;
            }
            go.transform.position = pos;
        }

        private static void AddNeonSign(GameObject parent, string name, Vector3 pos, Color color)
        {
            var go = GameObject.Find(name);
            if (go == null)
            {
                go = new GameObject(name);
                go.transform.parent = parent.transform;
            }
            go.transform.position = pos;

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/spr_platform.png");
            sr.color = color;
            sr.sortingOrder = 1; // Behind player, in front of background
            go.transform.localScale = new Vector3(1.5f, 0.4f, 1f);
        }
    }
}
