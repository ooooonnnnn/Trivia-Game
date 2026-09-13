using System;

namespace GameDataTypes
{
    [Serializable]
    public class PlayerInMatch
    {
        public int id;
        public int matchId;
        public int playerId;
        public float score;
        public DateTime createdAt;
    }
}