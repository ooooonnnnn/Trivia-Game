using System;

namespace GameDataTypes
{
    public class MatchData
    {
        public int id;
        public DateTime createdAt;
        public bool isActive, isCompleted;
        public int? winner_PlayerID;
        public float? score;
    }
}