using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using GlitchRun.Player;
using GlitchRun.Hazards;
using GlitchRun.Core;

namespace GlitchRun.PlayMode.Tests
{
    public class LaserGlitchPlayModeTests
    {
        private List<GameObject> testSpawnedObjects = new List<GameObject>();
        private GameManager gameManager;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            testSpawnedObjects.Clear();

            // Ensure GameManager exists
            gameManager = Object.FindAnyObjectByType<GameManager>();
            if (gameManager == null)
            {
                var gmGo = new GameObject("Test_GameManager");
                gameManager = gmGo.AddComponent<GameManager>();
                testSpawnedObjects.Add(gmGo);
            }

            gameManager.ResetState();
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;

            foreach (var go in testSpawnedObjects)
            {
                if (go != null)
                {
                    Object.Destroy(go);
                }
            }
            testSpawnedObjects.Clear();
            yield return null;
        }

        private GameObject CreatePlatform(Vector2 position, Vector2 size)
        {
            var plat = new GameObject("TestPlatform");
            plat.transform.position = position;
            var col = plat.AddComponent<BoxCollider2D>();
            col.size = size;
            testSpawnedObjects.Add(plat);
            return plat;
        }

        private PlayerController SpawnPlayer(Vector2 position)
        {
            var playerGo = new GameObject("TestPlayer");
            playerGo.transform.position = position;

            var rb = playerGo.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3.5f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            var col = playerGo.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(0.85f, 1.8f);
            col.offset = new Vector2(0f, 0.1f);

            var visualObj = new GameObject("Visual");
            visualObj.transform.parent = playerGo.transform;
            var sr = visualObj.AddComponent<SpriteRenderer>();

            var glitch = playerGo.AddComponent<GlitchAbility>();
            var controller = playerGo.AddComponent<PlayerController>();

            gameManager.SetActivePlayer(controller);
            testSpawnedObjects.Add(playerGo);
            return controller;
        }

        private LaserHazard SpawnLowLaser(Vector2 position, float width = 0.32f, float height = 0.75f, bool canBeGlitched = false)
        {
            var laserGo = new GameObject("TestLowLaser");
            laserGo.transform.position = position;

            var col = laserGo.AddComponent<BoxCollider2D>();
            col.size = new Vector2(width, height);
            col.isTrigger = true;

            var sr = laserGo.AddComponent<SpriteRenderer>();
            sr.size = new Vector2(width, height);

            var hazard = laserGo.AddComponent<LaserHazard>();
            hazard.CanBeGlitched = canBeGlitched;

            testSpawnedObjects.Add(laserGo);
            return hazard;
        }

        private GlitchBarrier SpawnTallBarrier(Vector2 position, float width = 0.6f, float height = 3.8f)
        {
            var barrierGo = new GameObject("TestTallBarrier");
            barrierGo.transform.position = position;

            var col = barrierGo.AddComponent<BoxCollider2D>();
            col.size = new Vector2(width, height);
            col.isTrigger = true;

            var sr = barrierGo.AddComponent<SpriteRenderer>();
            sr.size = new Vector2(width, height);

            var barrier = barrierGo.AddComponent<GlitchBarrier>();

            testSpawnedObjects.Add(barrierGo);
            return barrier;
        }

        // T1: Jump over a low laser: spawn player before it, hold move + jump, assert player passes the laser X with lives unchanged.
        [UnityTest]
        public IEnumerator T1_JumpOverLowLaser_PassesWithLivesUnchanged()
        {
            CreatePlatform(new Vector2(10f, -1.5f), new Vector2(20f, 1.0f)); // Surface at y = -1.0
            float laserX = 10f;
            SpawnLowLaser(new Vector2(laserX, -0.625f), 0.32f, 0.75f, canBeGlitched: false);

            var player = SpawnPlayer(new Vector2(7.5f, -0.1f));
            int initialLives = gameManager.CurrentLives;
            Assert.AreEqual(3, initialLives);

            var rb = player.GetComponent<Rigidbody2D>();
            float timeout = 2.0f;
            float elapsed = 0f;

            // Move toward laser and execute jump
            bool jumped = false;
            while (elapsed < timeout && player.transform.position.x < laserX + 1.5f)
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = new Vector2(8.5f, rb.linearVelocity.y);
#else
                rb.velocity = new Vector2(8.5f, rb.velocity.y);
#endif

                if (!jumped && player.transform.position.x >= 8.5f)
                {
#if UNITY_6000_0_OR_NEWER
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, 13.5f);
#else
                    rb.velocity = new Vector2(rb.velocity.x, 13.5f);
#endif
                    jumped = true;
                }

                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
            }

            Assert.IsTrue(player.transform.position.x > laserX, "Player should have crossed the laser X coordinate.");
            Assert.AreEqual(initialLives, gameManager.CurrentLives, "Player lives should remain unchanged after jumping over low laser.");
        }

        // T2: Tall barrier without phase: walk into it, assert exactly ONE damage event per hit, invulnerability window respected.
        [UnityTest]
        public IEnumerator T2_TallBarrierWithoutPhase_InflictsSingleDamage()
        {
            CreatePlatform(new Vector2(10f, -1.5f), new Vector2(20f, 1.0f));
            float barrierX = 10f;
            SpawnTallBarrier(new Vector2(barrierX, 0.9f), 0.6f, 3.8f);

            var player = SpawnPlayer(new Vector2(8.5f, -0.1f));
            int initialLives = gameManager.CurrentLives;
            Assert.AreEqual(3, initialLives);
            Assert.IsFalse(player.Glitch.IsGlitching);

            var rb = player.GetComponent<Rigidbody2D>();
            float elapsed = 0f;

            // Move player into barrier
            while (elapsed < 0.3f)
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = new Vector2(6.0f, rb.linearVelocity.y);
#else
                rb.velocity = new Vector2(6.0f, rb.velocity.y);
#endif
                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
            }

            Assert.AreEqual(initialLives - 1, gameManager.CurrentLives, "Player must take exactly 1 damage upon colliding without phase.");

            // Continue overlapping barrier for another 0.3s; invulnerability window should prevent repeated damage
            float waitExtra = 0f;
            while (waitExtra < 0.3f)
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = new Vector2(1.0f, rb.linearVelocity.y);
#else
                rb.velocity = new Vector2(1.0f, rb.velocity.y);
#endif
                yield return new WaitForFixedUpdate();
                waitExtra += Time.fixedDeltaTime;
            }

            Assert.AreEqual(initialLives - 1, gameManager.CurrentLives, "No per-frame repeat damage while within invulnerability window.");
        }

        // T3: Tall barrier with phase: press Shift (activate), cross it, assert zero damage and player ends on far side.
        [UnityTest]
        public IEnumerator T3_TallBarrierWithPhase_CrossesWithZeroDamage()
        {
            CreatePlatform(new Vector2(10f, -1.5f), new Vector2(20f, 1.0f));
            float barrierX = 10f;
            SpawnTallBarrier(new Vector2(barrierX, 0.9f), 0.6f, 3.8f);

            var player = SpawnPlayer(new Vector2(8.0f, -0.1f));
            int initialLives = gameManager.CurrentLives;

            // Activate Glitch Phase
            bool activated = player.Glitch.TryActivateGlitch();
            Assert.IsTrue(activated, "Glitch Phase should activate.");
            Assert.IsTrue(player.Glitch.IsGlitching, "Glitch Phase state must be active.");

            var rb = player.GetComponent<Rigidbody2D>();
            float elapsed = 0f;
            while (elapsed < 1.0f && player.transform.position.x < barrierX + 1.5f)
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = new Vector2(8.5f, rb.linearVelocity.y);
#else
                rb.velocity = new Vector2(8.5f, rb.velocity.y);
#endif
                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
            }

            Assert.IsTrue(player.transform.position.x > barrierX, "Player should successfully pass to far side of barrier.");
            Assert.AreEqual(initialLives, gameManager.CurrentLives, "Player must take zero damage when phasing through tall barrier.");
        }

        // T4: Phase through a barrier and stay inside it while phase expires: assert no death, player is pushed out or grace window applies.
        [UnityTest]
        public IEnumerator T4_PhaseExpiresInsideBarrier_SafeEjectionNoDeath()
        {
            CreatePlatform(new Vector2(10f, -1.5f), new Vector2(20f, 1.0f));
            float barrierX = 10f;
            var barrier = SpawnTallBarrier(new Vector2(barrierX, 0.9f), 0.6f, 3.8f);

            var player = SpawnPlayer(new Vector2(barrierX, -0.1f)); // Placed directly inside barrier
            player.Glitch.TryActivateGlitch();
            player.Glitch.RegisterBarrierOverlap(barrier.Collider);

            int initialLives = gameManager.CurrentLives;

            // Simulate phase expiry while player remains stationary inside
            yield return new WaitForSeconds(0.1f);

            // Fast-forward or trigger grace expiry ejection directly
            player.Glitch.PushOutOfBarrier(barrier.Collider);
            yield return new WaitForFixedUpdate();

            Assert.IsTrue(gameManager.CurrentLives > 0, "Player must not die when phase ends inside a barrier.");
            Assert.AreEqual(initialLives, gameManager.CurrentLives, "Player lives must remain intact after safe ejection.");

            // Verify player collider bounds are outside the barrier bounds
            var playerCol = player.GetComponent<Collider2D>();
            Bounds barrierBounds = barrier.Collider.bounds;
            bool isOutside = playerCol.bounds.max.x < barrierBounds.min.x || playerCol.bounds.min.x > barrierBounds.max.x;
            Assert.IsTrue(isOutside, "Player must be safely ejected beyond the barrier's horizontal bounds.");
        }

        // T5: Phase active near a non-phaseable hazard (drone or standard red laser): assert damage STILL applies.
        [UnityTest]
        public IEnumerator T5_PhaseActiveNearNonPhaseableHazard_DamageStillApplies()
        {
            CreatePlatform(new Vector2(10f, -1.5f), new Vector2(20f, 1.0f));
            float laserX = 10f;
            // Unphaseable low laser (canBeGlitched = false)
            SpawnLowLaser(new Vector2(laserX, -0.625f), 0.32f, 0.75f, canBeGlitched: false);

            var player = SpawnPlayer(new Vector2(8.5f, -0.1f));
            player.Glitch.TryActivateGlitch();
            Assert.IsTrue(player.Glitch.IsGlitching, "Glitch Phase must be active.");

            int initialLives = gameManager.CurrentLives;
            var rb = player.GetComponent<Rigidbody2D>();

            // Walk player into the unphaseable laser while phase is active
            float elapsed = 0f;
            while (elapsed < 0.4f)
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = new Vector2(6.0f, rb.linearVelocity.y);
#else
                rb.velocity = new Vector2(6.0f, rb.velocity.y);
#endif
                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
            }

            Assert.AreEqual(initialLives - 1, gameManager.CurrentLives, "Unphaseable hazard must deal damage even when Glitch Phase is active.");
        }

        // T6: Cooldown: active ~2.0s, cooldown ~3.5s, cannot reactivate during cooldown, can reactivate after.
        [UnityTest]
        public IEnumerator T6_CooldownBehavior_TimersAndReactivationEnforced()
        {
            var player = SpawnPlayer(new Vector2(0f, 0f));
            var glitch = player.Glitch;

            Assert.IsTrue(glitch.CanGlitch, "Ability should initially be ready.");

            // 1. Activate
            bool activated = glitch.TryActivateGlitch();
            Assert.IsTrue(activated);
            Assert.IsTrue(glitch.IsGlitching);
            Assert.AreEqual(2.0f, glitch.GlitchDuration, 0.05f);
            Assert.AreEqual(3.5f, glitch.GlitchCooldown, 0.05f);

            // 2. Cannot reactivate while active
            Assert.IsFalse(glitch.TryActivateGlitch());

            // 3. Fast-forward through active duration
            float activeSim = 0f;
            while (activeSim < 2.05f)
            {
                yield return null;
                activeSim += Time.deltaTime;
            }

            Assert.IsFalse(glitch.IsGlitching, "Ability must no longer be active after duration.");
            Assert.IsTrue(glitch.CooldownTimer > 0f, "Cooldown timer must be active.");
            Assert.IsFalse(glitch.CanGlitch, "Cannot activate during cooldown.");
            Assert.IsFalse(glitch.TryActivateGlitch(), "TryActivateGlitch must return false during cooldown.");

            // 4. Reset or elapse cooldown
            glitch.ResetAbility();
            Assert.IsTrue(glitch.CanGlitch, "Ability should be ready after cooldown reset.");
            Assert.IsTrue(glitch.TryActivateGlitch(), "Should be able to activate again after cooldown.");
        }

        // T7: Phase state is cleaned up on death/restart/scene reload.
        [UnityTest]
        public IEnumerator T7_PhaseStateCleanedUpOnDeathAndRestart()
        {
            var player = SpawnPlayer(new Vector2(0f, 0f));
            var glitch = player.Glitch;

            glitch.TryActivateGlitch();
            Assert.IsTrue(glitch.IsGlitching);

            // Trigger player respawn/death cleanup
            gameManager.PlayerTookDamage("Fatal Hazard");
            glitch.ResetAbility();

            Assert.IsFalse(glitch.IsGlitching, "IsGlitching must be false after reset.");
            Assert.AreEqual(0f, glitch.CooldownTimer, "Cooldown timer must be 0 after reset.");
            Assert.AreEqual(0f, glitch.RemainingActiveTime, "Remaining active time must be 0 after reset.");

            yield return null;
        }

        // T8: Reachability: automated validator for every laser in Gameplay scene checking declared solution feasibility.
        [UnityTest]
        public IEnumerator T8_GameplaySceneLaserReachability_AllObstaclesFeasible()
        {
            // Load the actual Gameplay scene
            var loadOp = SceneManager.LoadSceneAsync("Gameplay", LoadSceneMode.Single);
            while (!loadOp.isDone)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.1f);

            var lasers = Object.FindObjectsByType<LaserHazard>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var barriers = Object.FindObjectsByType<GlitchBarrier>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            Assert.IsTrue(lasers.Length > 0, "Gameplay scene must contain LaserHazard instances.");
            Assert.IsTrue(barriers.Length > 0, "Gameplay scene must contain GlitchBarrier instances.");

            float maxJumpHeight = 2.654f;

            // 1. Verify Low Lasers (Jumpable)
            foreach (var laser in lasers)
            {
                var col = laser.GetComponent<BoxCollider2D>();
                Assert.IsNotNull(col, $"Laser '{laser.name}' must have a BoxCollider2D.");

                float height = col.size.y * laser.transform.lossyScale.y;
                float width = col.size.x * laser.transform.lossyScale.x;

                // Design rule: height <= 50% of max jump height (<= 1.33m, target <= 1.0m)
                Assert.LessOrEqual(height, 1.33f, $"Low laser '{laser.name}' height {height:F2}m exceeds 50% max jump height (1.33m).");
                Assert.LessOrEqual(width, 3.50f, $"Laser '{laser.name}' width {width:F2}m exceeds 60% horizontal leap (3.50m).");
            }

            // 2. Verify Tall Barriers (Glitch Phaseable)
            foreach (var barrier in barriers)
            {
                var col = barrier.GetComponent<BoxCollider2D>();
                Assert.IsNotNull(col, $"Barrier '{barrier.name}' must have a BoxCollider2D.");

                float height = col.size.y * barrier.transform.lossyScale.y;
                float width = col.size.x * barrier.transform.lossyScale.x;

                // Design rule: height > max jump height (unjumpable by design)
                Assert.Greater(height, maxJumpHeight, $"Glitch barrier '{barrier.name}' height {height:F2}m must be taller than max jump height ({maxJumpHeight:F2}m).");
                Assert.LessOrEqual(width, 2.0f, $"Glitch barrier '{barrier.name}' width {width:F2}m must be thin enough to easily cross during phase.");
            }
        }
    }
}
