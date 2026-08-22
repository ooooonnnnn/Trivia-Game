using DataTypes;
using TMPro;
using UnityEngine;

public class MatchUI : MonoBehaviour
{
    [SerializeField] private TMP_Text QuestionText;
    
    public void SetQuestion(Question question)
    {
        QuestionText.text = question.questionText;
    }
    
    // public void SetAnswers()
}
