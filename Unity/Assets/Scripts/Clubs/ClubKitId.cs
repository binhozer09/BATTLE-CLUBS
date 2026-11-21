namespace BattleClubs.Clubs
{
    public enum KitVariant
    {
        Home,
        Away
    }

    [System.Serializable]
    public sealed class ClubKitId
    {
        public string clubId;
        public string displayName;
        public KitVariant variant;
    }
}
