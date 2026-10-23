using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using GlitchRun.Core;
using GlitchRun.Player;
using GlitchRun.Hazards;
using GlitchRun.Gameplay;
using GlitchRun.UI;

namespace GlitchRun.EditorScripts
{
    [InitializeOnLoad]
    public static class PrototypeSceneBuilder
    {
        private const string PrefKey = "GlitchRun_Prototype_Built_v1";

        static PrototypeSceneBuilder()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorPrefs.GetBool(PrefKey, false))
                {
                    EditorPrefs.SetBool(PrefKey, true);
                    BuildAll();
                }
            };
        }

        [MenuItem("Glitch Run/Build Prototype Scene and Prefabs", priority = 1)]
        public static void BuildAll()
        {
            Debug.Log("[GlitchRun] Starting Prototype Scene & Prefabs generation...");

            EnsureDirectories();
            var prefabs = CreatePrefabs();
            CreatePrototypeScene(prefabs);
            AddSceneToBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[GlitchRun] Prototype Scene & Prefabs generated successfully!");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists("Assets/Prefabs")) Directory.CreateDirectory("Assets/Prefabs");
            if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");
        }

        public struct PrefabReferences
        {
            public GameObject PlayerPrefab;
            public GameObject PlatformPrefab;
            public GameObject LaserPrefab;
            public GameObject FragmentPrefab;
            public GameObject ExitGatePrefab;
        }

        private static PrefabReferences CreatePrefabs()
        {
            PrefabReferences refs = new PrefabReferences();

            // Load sprite assets
            var platformSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/spr_platform.png");
            var laserSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/spr_laser.png");
            var fragmentSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/spr_fragment.png");
            var exitSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/spr_exit_portal.png");
            var playerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Player/main_player.png");

            // 1. Platform Prefab
            var platObj = new GameObject("PF_Platform");
            var platSr = platObj.AddComponent<SpriteRenderer>();
            platSr.sprite = platformSprite;
            platSr.sortingOrder = 1;
            var platCol = platObj.AddComponent<BoxCollider2D>();
            if (platformSprite != null) platCol.size = platformSprite.bounds.size;
            refs.PlatformPrefab = PrefabUtility.SaveAsPrefabAsset(platObj, "Assets/Prefabs/PF_Platform.prefab");
            Object.DestroyImmediate(platObj);

            // 2. Laser Hazard Prefab
            var laserObj = new GameObject("PF_LaserHazard");
            var laserSr = laserObj.AddComponent<SpriteRenderer>();
            laserSr.sprite = laserSprite;
            laserSr.sortingOrder = 5;
            laserSr.color = new Color(1f, 0.1f, 0.25f, 0.95f);
            var laserCol = laserObj.AddComponent<BoxCollider2D>();
            laserCol.isTrigger = true;
            if (laserSprite != null) laserCol.size = laserSprite.bounds.size;
            laserObj.AddComponent<LaserHazard>();
            refs.LaserPrefab = PrefabUtility.SaveAsPrefabAsset(laserObj, "Assets/Prefabs/PF_LaserHazard.prefab");
            Object.DestroyImmediate(laserObj);

            // 3. Data Fragment Prefab
            var fragObj = new GameObject("PF_DataFragment");
            fragObj.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            var fragSr = fragObj.AddComponent<SpriteRenderer>();
            fragSr.sprite = fragmentSprite;
            fragSr.sortingOrder = 6;
            var fragCol = fragObj.AddComponent<CircleCollider2D>();
            fragCol.isTrigger = true;
            fragCol.radius = 0.35f;
            fragObj.AddComponent<DataFragment>();
            refs.FragmentPrefab = PrefabUtility.SaveAsPrefabAsset(fragObj, "Assets/Prefabs/PF_DataFragment.prefab");
            Object.DestroyImmediate(fragObj);

            // 4. Exit Gate Prefab
            var gateObj = new GameObject("PF_ExitGate");
            gateObj.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
            var gateSr = gateObj.AddComponent<SpriteRenderer>();
            gateSr.sprite = exitSprite;
            gateSr.sortingOrder = 2;
            var gateCol = gateObj.AddComponent<BoxCollider2D>();
            gateCol.isTrigger = true;
            if (exitSprite != null) gateCol.size = exitSprite.bounds.size;
            gateObj.AddComponent<ExitGate>();
            refs.ExitGatePrefab = PrefabUtility.SaveAsPrefabAsset(gateObj, "Assets/Prefabs/PF_ExitGate.prefab");
            Object.DestroyImmediate(gateObj);

            // 5. Player Prefab
            var playerObj = new GameObject("PF_Player");
            var rb = playerObj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.gravityScale = 3.5f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            var playerCol = playerObj.AddComponent<CapsuleCollider2D>();
            playerCol.size = new Vector2(0.85f, 1.8f);
            playerCol.offset = new Vector2(0f, 0.1f);

            var visualObj = new GameObject("Visual");
            visualObj.transform.SetParent(playerObj.transform, false);
            visualObj.transform.localScale = new Vector3(0.18f, 0.18f, 1f);
            var visualSr = visualObj.AddComponent<SpriteRenderer>();
            visualSr.sprite = playerSprite;
            visualSr.sortingOrder = 10;

            var glitch = playerObj.AddComponent<GlitchAbility>();
            var controller = playerObj.AddComponent<PlayerController>();

            refs.PlayerPrefab = PrefabUtility.SaveAsPrefabAsset(playerObj, "Assets/Prefabs/PF_Player.prefab");
            Object.DestroyImmediate(playerObj);

            Debug.Log("[GlitchRun] Prefabs created successfully.");
            return refs;
        }

        private static void CreatePrototypeScene(PrefabReferences refs)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Global 2D Light (Uniform clear lighting for cyberpunk palette)
            var lightObj = new GameObject("Global 2D Light");
            var light2D = lightObj.AddComponent<Light2D>();
            light2D.lightType = Light2D.LightType.Global;
            light2D.color = Color.white;
            light2D.intensity = 1.0f;

            // 2. Animated Cyberpunk City Background
            var bgObj = new GameObject("BG_City_Animated");
            bgObj.transform.position = new Vector3(15f, 2.5f, 0f);
            bgObj.transform.localScale = new Vector3(2.8f, 2.8f, 1f);
            var bgSr = bgObj.AddComponent<SpriteRenderer>();
            bgSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environments/Background/BG_City_01.jpeg");
            bgSr.sortingOrder = -10;
            var bgAnim = bgObj.AddComponent<Animator>();
            bgAnim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Art/Environments/Background/BG_City_Animated.controller");

            // 3. Player Instance
            GameObject playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(refs.PlayerPrefab);
            playerInstance.name = "Player";
            playerInstance.transform.position = new Vector3(0f, 0f, 0f);

            // 4. Main Camera with CameraFollow2D
            var camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.backgroundColor = new Color(0.04f, 0.05f, 0.08f, 1f); // Deep cyberpunk void
            var audioType = System.Type.GetType("UnityEngine.AudioListener, UnityEngine.AudioModule") ?? System.Type.GetType("UnityEngine.AudioListener, UnityEngine");
            if (audioType != null) camObj.AddComponent(audioType);

            var follow = camObj.AddComponent<CameraFollow2D>();
            // Setup follow fields via serialized object
            var followSo = new SerializedObject(follow);
            followSo.FindProperty("target").objectReferenceValue = playerInstance.transform;
            followSo.FindProperty("backgroundTransform").objectReferenceValue = bgObj.transform;
            followSo.FindProperty("minBounds").vector2Value = new Vector2(-2f, -1f);
            followSo.FindProperty("maxBounds").vector2Value = new Vector2(58f, 8f);
            followSo.ApplyModifiedProperties();

            // 5. Game Manager
            var gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();

            // 6. Game HUD
            var hudObj = new GameObject("GameHUD");
            hudObj.AddComponent<GameHUD>();

            // 7. Death Zone (covers all pits/gaps at y = -7)
            var deathObj = new GameObject("DeathZone");
            deathObj.transform.position = new Vector3(28f, -6f, 0f);
            var deathCol = deathObj.AddComponent<BoxCollider2D>();
            deathCol.isTrigger = true;
            deathCol.size = new Vector2(90f, 4f);
            deathObj.AddComponent<DeathZone>();

            // 8. Level Geometry & Mechanics

            // --- Section 1: Spawn & Ground Zero ---
            SpawnPlatform(refs.PlatformPrefab, new Vector3(1f, -1.5f, 0f), new Vector3(5.0f, 1.0f, 1f), "Platform_Start");
            SpawnFragment(refs.FragmentPrefab, new Vector3(5.0f, 0.0f, 0f), "DataFragment_01");

            // --- Section 2: The Firewall (Glitch Phase Tutorial) ---
            SpawnPlatform(refs.PlatformPrefab, new Vector3(13.5f, -1.5f, 0f), new Vector3(3.5f, 1.0f, 1f), "Platform_Section2");
            SpawnLaser(refs.LaserPrefab, new Vector3(13.5f, 0.2f, 0f), new Vector3(1.4f, 2.4f, 1f), false, "LaserHazard_Static_01");
            SpawnFragment(refs.FragmentPrefab, new Vector3(16.5f, 0.0f, 0f), "DataFragment_02");

            // --- Section 3: Floating Platforms & Timed Laser ---
            SpawnPlatform(refs.PlatformPrefab, new Vector3(22.5f, -0.5f, 0f), new Vector3(1.6f, 1.0f, 1f), "Platform_Step_A");
            SpawnPlatform(refs.PlatformPrefab, new Vector3(27.0f, 1.0f, 0f), new Vector3(1.6f, 1.0f, 1f), "Platform_Step_B");
            SpawnLaser(refs.LaserPrefab, new Vector3(27.0f, 2.7f, 0f), new Vector3(1.2f, 2.2f, 1f), true, "LaserHazard_Cycling_02");
            SpawnFragment(refs.FragmentPrefab, new Vector3(27.0f, 4.3f, 0f), "DataFragment_03_ObjectiveMet");
            SpawnPlatform(refs.PlatformPrefab, new Vector3(32.0f, 0.0f, 0f), new Vector3(1.8f, 1.0f, 1f), "Platform_Step_C");

            // --- Section 4: The Core Run (Dual Lasers & Secret Ledge) ---
            SpawnPlatform(refs.PlatformPrefab, new Vector3(41.5f, -1.5f, 0f), new Vector3(4.5f, 1.0f, 1f), "Platform_Corridor");
            SpawnLaser(refs.LaserPrefab, new Vector3(39.5f, 0.2f, 0f), new Vector3(1.2f, 2.4f, 1f), false, "LaserHazard_Dual_A");
            SpawnFragment(refs.FragmentPrefab, new Vector3(41.5f, 0.0f, 0f), "DataFragment_04");
            SpawnLaser(refs.LaserPrefab, new Vector3(43.5f, 0.2f, 0f), new Vector3(1.2f, 2.4f, 1f), false, "LaserHazard_Dual_B");

            // High secret platform with bonus fragment
            SpawnPlatform(refs.PlatformPrefab, new Vector3(41.5f, 2.8f, 0f), new Vector3(1.5f, 0.8f, 1f), "Platform_Secret");
            SpawnFragment(refs.FragmentPrefab, new Vector3(41.5f, 4.0f, 0f), "DataFragment_05_Bonus");

            // --- Section 5: Terminal Breach (Exit Gate) ---
            SpawnPlatform(refs.PlatformPrefab, new Vector3(53.0f, -1.0f, 0f), new Vector3(3.5f, 1.2f, 1f), "Platform_Exit");
            GameObject exitGate = (GameObject)PrefabUtility.InstantiatePrefab(refs.ExitGatePrefab);
            exitGate.name = "ExitGate";
            exitGate.transform.position = new Vector3(54.0f, 0.6f, 0f);

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/PrototypeScene.unity");
            Debug.Log("[GlitchRun] PrototypeScene.unity saved at Assets/Scenes/PrototypeScene.unity");
        }

        private static GameObject SpawnPlatform(GameObject prefab, Vector3 pos, Vector3 scale, string name)
        {
            GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            obj.name = name;
            obj.transform.position = pos;
            obj.transform.localScale = scale;
            return obj;
        }

        private static GameObject SpawnLaser(GameObject prefab, Vector3 pos, Vector3 scale, bool cycling, string name)
        {
            GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            obj.name = name;
            obj.transform.position = pos;
            obj.transform.localScale = scale;

            if (cycling)
            {
                var hazard = obj.GetComponent<LaserHazard>();
                if (hazard != null)
                {
                    var so = new SerializedObject(hazard);
                    so.FindProperty("isCycling").boolValue = true;
                    so.FindProperty("activeTime").floatValue = 2.2f;
                    so.FindProperty("inactiveTime").floatValue = 1.6f;
                    so.ApplyModifiedProperties();
                }
            }
            return obj;
        }

        private static GameObject SpawnFragment(GameObject prefab, Vector3 pos, string name)
        {
            GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            obj.name = name;
            obj.transform.position = pos;
            return obj;
        }

        private static void AddSceneToBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes;
            string scenePath = "Assets/Scenes/PrototypeScene.unity";

            foreach (var s in scenes)
            {
                if (s.path == scenePath) return;
            }

            var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
            for (int i = 0; i < scenes.Length; i++)
            {
                newScenes[i] = scenes[i];
            }
            newScenes[scenes.Length] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = newScenes;
            Debug.Log($"[GlitchRun] Added {scenePath} to EditorBuildSettings!");
        }
    }
}
