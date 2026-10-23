using UnityEngine;
using GlitchRun.Player;
using GlitchRun.Core;

namespace GlitchRun.Hazards
{
    /// <summary>
    /// Catch-all bottom hazard trigger for pits and gaps.
    /// Resets or kills the player upon falling off platforms into the abyss.
    /// </summary>
    public class DeathZone : MonoBehaviour
    {
        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerController>();
            if (player == null)
            {
                player = other.GetComponentInParent<PlayerController>();
            }

            if (player != null)
            {
                Debug.Log("[GlitchRun] Player fell into the simulation void!");
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.PlayerDied();
                }
            }
        }
    }
}
