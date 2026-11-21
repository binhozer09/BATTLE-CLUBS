using UnityEngine;
using BattleClubs.Gameplay;

namespace BattleClubs.World
{
    /// <summary>
    /// Places each team member at the team's generated spawn anchor.
    /// Local scene utility only; online spawn authority must be server-owned.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TeamSpawnManager : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float memberSpacing = 1.35f;
        [SerializeField] private bool faceMapCenter = true;

        private Transform[] spawnPoints = new Transform[5];
        private bool cached;

        public bool TrySpawnPlayer(int teamIndex, int playerIndex, Transform player)
        {
            if (player == null || teamIndex < 0 || teamIndex >= 5 ||
                playerIndex < 0 || playerIndex >= 4)
                return false;

            ResolveSpawnPoints();
            Transform anchor = spawnPoints[teamIndex];
            if (anchor == null)
            {
                Debug.LogWarning($"[BATTLE CLUBS] Team spawn {teamIndex + 1} was not generated.", this);
                return false;
            }

            Vector3 localOffset = GetFormationOffset(playerIndex) * memberSpacing;
            Vector3 destination = anchor.TransformPoint(localOffset);
            CharacterController controller = player.GetComponent<CharacterController>();
            bool wasEnabled = controller != null && controller.enabled;

            if (wasEnabled) controller.enabled = false;
            player.SetPositionAndRotation(destination, GetSpawnRotation(anchor));
            if (wasEnabled) controller.enabled = true;

            return true;
        }

        public void RefreshSpawnPoints()
        {
            cached = false;
            ResolveSpawnPoints();
        }

        private void ResolveSpawnPoints()
        {
            if (cached) return;
            for (int i = 0; i < spawnPoints.Length; i++) spawnPoints[i] = null;

            Transform[] children = GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child == transform) continue;
                for (int i = 0; i < spawnPoints.Length; i++)
                {
                    if (child.name == "TeamSpawn_" + (i + 1))
                    {
                        spawnPoints[i] = child;
                        break;
                    }
                }
            }
            cached = true;
        }

        private Vector3 GetSpawnRotation(Transform anchor)
        {
            if (!faceMapCenter) return anchor.rotation;
            Vector3 center = Vector3.zero;
            int count = 0;
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] == null) continue;
                center += spawnPoints[i].position;
                count++;
            }
            if (count > 0) center /= count;
            else center = transform.position;
            Vector3 direction = center - anchor.position;
            direction.y = 0f;
            return direction.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(direction.normalized, Vector3.up)
                : anchor.rotation;
        }

        private static Vector3 GetFormationOffset(int playerIndex)
        {
            switch (playerIndex)
            {
                case 0: return new Vector3(-0.5f, 0f, -0.5f);
                case 1: return new Vector3(0.5f, 0f, -0.5f);
                case 2: return new Vector3(-0.5f, 0f, 0.5f);
                default: return new Vector3(0.5f, 0f, 0.5f);
            }
        }
    }
}
