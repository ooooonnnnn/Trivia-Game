using System.Collections.Generic;
using DataTypes;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MatchUI : MonoBehaviour
{
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private Transform answerContainer; 
    [SerializeField] private AnswerUIElement answerUIPrefab; 
    
    public void SetQuestion(Question question)
    {
        questionText.text = question.questionText;
    }

    public void SetAnswers(Answer[] answers, UnityAction onCorrectClicked, UnityAction onWrongClicked)
    {
        foreach (Transform child in answerContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var answer in answers)
        {
            var newAnswerUI = Instantiate(answerUIPrefab, answerContainer);
            newAnswerUI.SetText(answer.answerText);
            newAnswerUI.answerButton.onClick.AddListener(
                answer.isCorrectAnswer ? onCorrectClicked : onWrongClicked);
        }
    }
}
