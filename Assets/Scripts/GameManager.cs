using System.Collections;
using DataTypes;
using UnityEngine;
using UnityEngine.Networking;

public class GameManager : MonoBehaviour
{
    public int matchID;
    [SerializeField] private MatchUI matchUI;
    private Question[] _questions;

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
            print(question.questionText);
            yield return new WaitForSeconds(2);
        }
    }

    private IEnumerator GetQuestions()
    {
        var request = UnityWebRequest.Get(
            $"{LoginManager.BASE_URL}/Questions/questions-in-match/{matchID}");
        
        yield return request.SendWebRequest();
        
        _questions = 
            JsonUtility.FromJson<QuestionArray>(
                "{\"questions\": " + request.downloadHandler.text + "}")
                .questions;
    }
}
