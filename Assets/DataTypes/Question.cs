using System;

namespace DataTypes
{
    [Serializable]
    public class Question
    {
        public int id;
        public string questionText;
        public DateTime CreatedAt;
    }
}