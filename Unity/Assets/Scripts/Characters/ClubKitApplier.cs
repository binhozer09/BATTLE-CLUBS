using System;
using UnityEngine;

namespace BattleClubs.Characters
{
    /// <summary>
    /// Applies an original, configurable team kit to a player's visual renderers.
    /// Uses MaterialPropertyBlock so player/team colors do not instantiate or mutate
    /// shared materials. Attach to the visual character root and call ApplyKit when
    /// the match assigns the player to a team.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ClubKitApplier : MonoBehaviour
    {
        [Serializable]
        public sealed class KitDefinition
        {
            public string displayName = "Equipe";
            public Color primaryColor = Color.white;
            public Color secondaryColor = Color.black;
            public Texture baseTexture;
        }

        [Header("Team kits (index 0-3)")]
        [SerializeField] private KitDefinition[] kits =
        {
            new KitDefinition { displayName = "Equipe 1", primaryColor = new Color(0.10f, 0.28f, 0.78f), secondaryColor = Color.white },
            new KitDefinition { displayName = "Equipe 2", primaryColor = new Color(0.78f, 0.12f, 0.10f), secondaryColor = Color.white },
            new KitDefinition { displayName = "Equipe 3", primaryColor = new Color(0.10f, 0.48f, 0.24f), secondaryColor = Color.white },
            new KitDefinition { displayName = "Equipe 4", primaryColor = new Color(0.92f, 0.68f, 0.12f), secondaryColor = new Color(0.12f, 0.12f, 0.12f) }
        };

        [Header("Renderer targets")]
        [Tooltip("Leave empty to collect all child renderers, including inactive objects.")]
        [SerializeField] private Renderer[] targets;

        [Tooltip("Material slot indices that receive the primary team color. Empty means all slots.")]
        [SerializeField] private int[] primaryMaterialSlots;

        [Tooltip("Optional material slot indices that receive the secondary team color.")]
        [SerializeField] private int[] secondaryMaterialSlots;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

        private MaterialPropertyBlock block;
        private int currentTeam = -1;
        private int playerNumber;

        public int CurrentTeam => currentTeam;
        public int PlayerNumber => playerNumber;
        public string CurrentKitName =>
            IsValidTeam(currentTeam) ? kits[currentTeam].displayName : string.Empty;

        private void Awake()
        {
            block = new MaterialPropertyBlock();
            ResolveTargets();
        }

        /// <summary>
        /// Applies a team's kit and records the shirt number for UI/nameplate systems.
        /// Team indices are zero-based: 0, 1, 2 or 3.
        /// </summary>
        public bool ApplyKit(int teamIndex, int shirtNumber = 0)
        {
            if (!IsValidTeam(teamIndex))
            {
                Debug.LogWarning($"[{nameof(ClubKitApplier)}] Invalid team index: {teamIndex}.", this);
                return false;
            }

            if (block == null) block = new MaterialPropertyBlock();
            ResolveTargets();

            KitDefinition kit = kits[teamIndex];
            currentTeam = teamIndex;
            playerNumber = Mathf.Max(0, shirtNumber);

            foreach (Renderer target in targets)
            {
                if (target == null) continue;

                Material[] materials = target.sharedMaterials;
                for (int slot = 0; slot < materials.Length; slot++)
                {
                    Material material = materials[slot];
                    if (material == null) continue;

                    bool primary = IsSlotSelected(primaryMaterialSlots, slot, primaryMaterialSlots == null || primaryMaterialSlots.Length == 0);
                    bool secondary = IsSlotSelected(secondaryMaterialSlots, slot, false);
                    if (!primary && !secondary && kit.baseTexture == null) continue;

                    target.GetPropertyBlock(block, slot);
                    if (primary) SetColorProperties(block, material, kit.primaryColor);
                    if (secondary) SetColorProperties(block, material, kit.secondaryColor);
                    if (kit.baseTexture != null)
                    {
                        if (material.HasProperty(BaseMapId)) block.SetTexture(BaseMapId, kit.baseTexture);
                        if (material.HasProperty(MainTexId)) block.SetTexture(MainTexId, kit.baseTexture);
                    }
                    target.SetPropertyBlock(block, slot);
                    block.Clear();
                }
            }

            return true;
        }

        public void ClearKit()
        {
            if (targets == null) ResolveTargets();
            foreach (Renderer target in targets)
            {
                if (target == null) continue;
                target.SetPropertyBlock(null);
                Material[] materials = target.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                    target.SetPropertyBlock(null, i);
            }
            currentTeam = -1;
            playerNumber = 0;
        }

        private void ResolveTargets()
        {
            if (targets == null || targets.Length == 0)
                targets = GetComponentsInChildren<Renderer>(true);
        }

        private bool IsValidTeam(int teamIndex) =>
            kits != null && teamIndex >= 0 && teamIndex < kits.Length && kits[teamIndex] != null;

        private static bool IsSlotSelected(int[] slots, int slot, bool defaultValue)
        {
            if (slots == null || slots.Length == 0) return defaultValue;
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] == slot) return true;
            return false;
        }

        private static void SetColorProperties(MaterialPropertyBlock properties, Material material, Color color)
        {
            if (material.HasProperty(BaseColorId)) properties.SetColor(BaseColorId, color);
            if (material.HasProperty(ColorId)) properties.SetColor(ColorId, color);
        }
    }
}
