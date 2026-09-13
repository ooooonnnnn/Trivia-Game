using System;

namespace GameDataTypes
{
    [Serializable]
    public class Answer
    {
        public int id;
        public DateTime CreatedAt;
        public int questionId;
        public string answerText;
        public bool isCorrectAnswer;
    }
}