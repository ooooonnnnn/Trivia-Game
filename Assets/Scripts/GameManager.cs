using System;
using System.Collections;
using System.Collections.Generic;
using DataTypes;
using HelperDataTypes;
using UnityEngine;
using UnityEngine.Networking;

public class GameManager : MonoBehaviour
{
    public int matchID;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private MatchUI matchUI;
    [SerializeField] private ResultsUI resultsUI;
    private Dictionary<Question, Answer[]> _questions = new();
    private GameSettings _settings;
    private int questioCounter = 0;
    private float _score = 0;
    
    private bool _moveNext = false;
    private FloatContainer _timeLeft = new();
    
    private event Action _OnTimerEnd;

    [ContextMenu( "Start Game" )]
    public void StartGame()
    {
        StartCoroutine(GameCor());
    }

    private IEnumerator GameCor()
    {
        yield return GetQuestions();
        yield return GetSettings();
        
        foreach (var question in _questions)
        {
            _moveNext = false;
            print(question.Key.questionText);
            matchUI.SetQuestion(question.Key);
            matchUI.SetQuestionNumber(++questioCounter, _questions.Count);
            matchUI.SetAnswers(question.Value, HandleCorrectAnswer, HandleWrongAnswer);
            _OnTimerEnd = null;
            _OnTimerEnd += HandleWrongAnswer;
            _OnTimerEnd += () => _moveNext = true;
            var timerCor = StartCoroutine(TimerCor(_settings.timePerQuestion, _timeLeft));
            
            while (!_moveNext)
            {
                yield return null;
            }
            StopCoroutine(timerCor);
        }
        
        resultsUI.UpdateScore(_score);
        uiManager.ShowResultsScreen();
    }

    private IEnumerator TimerCor(float initialTime, FloatContainer timeLeft)
    {
        var t = initialTime;
        timeLeft.value = t;
        while (t > 0)
        {
            matchUI.SetTimer(t);
            yield return null;
            t -= Time.deltaTime;
            timeLeft.value = t;
        }
        _OnTimerEnd?.Invoke();
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

    private IEnumerator GetSettings()
    {
        var settingsRequest = UnityWebRequest.Get(
            $"{LoginManager.BASE_URL}/Settings");
        
        yield return settingsRequest.SendWebRequest();

        _settings = JsonUtility.FromJson<GameSettings>(
            settingsRequest.downloadHandler.text);
    }

    public void HandleCorrectAnswer()
    {
        print("Correct");
        _score += 10 * _timeLeft.value /  _settings.timePerQuestion;
        print($"Gained score: {_score}");
        _moveNext = true;
    }

    public void HandleWrongAnswer()
    {
        print("Wrong");
        _moveNext = true;
    }
}
