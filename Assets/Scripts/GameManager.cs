using System.Collections;
using System.Collections.Generic;
using DataTypes;
using UnityEngine;
using UnityEngine.Networking;

public class GameManager : MonoBehaviour
{
    public int matchID;
    [SerializeField] private MatchUI matchUI;
    private Dictionary<Question, Answer[]> _questions = new();
    private int questioCounter = 0;
    private bool moveNext = false;

    [ContextMenu( "Start Game" )]
    public void StartGame()
    {
        StartCoroutine(GameCor());
    }

    private IEnumerator GameCor()
    {
        yield return GetQuestions();
        
        foreach (var question in _questions)
        {
            moveNext = false;
            print(question.Key.questionText);
            matchUI.SetQuestion(question.Key);
            matchUI.SetQuestionNumber(++questioCounter, _questions.Count);
            matchUI.SetAnswers(question.Value, HandleCorrectAnswer, HandleWrongAnswer);
            
            while (!moveNext)
            {
                yield return null;
            }
        }
    }

    private IEnumerator GetQuestions()
    {
        var questionsRequest = UnityWebRequest.Get(
            $"{LoginManager.BASE_URL}/Questions/questions-in-match/{matchID}");
        
        yield return questionsRequest.SendWebRequest();
        
        var questions = 
            JsonUtility.FromJson<QuestionArray>(
                "{\"questions\": " + questionsRequest.downloadHandler.text + "}")
                .questions;

        foreach (var question in questions)
        {
            var answersRequest = UnityWebRequest.Get(
                $"{LoginManager.BASE_URL}/Questions/answers/{question.id}");
            
            yield return answersRequest.SendWebRequest();

            var answers = JsonUtility.FromJson<AnswerArray>(
                    "{\"answers\": " + answersRequest.downloadHandler.text + "}")
                .answers;
            
            _questions.Add(question, answers);
        }
    }

    public void HandleCorrectAnswer()
    {
        print("Correct");
        moveNext = true;
    }

    public void HandleWrongAnswer()
    {
        print("Wrong");
        moveNext = true;
    }
}
