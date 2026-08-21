using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.Networking;
using System.Collections.Generic;
using DataTypes;

public class LoginManager : MonoBehaviour
{
    public UnityEvent<string> OnLoginFail;
    public UnityEvent OnLoginSuccess;
    public UnityEvent OnStartLogin;
    private MatchData _currentMatch = null;

    public string PlayerName
    {
        get => _playerName;
        set => _playerName = value;
    }
    private string _playerName;
    
    public void Login()
    {
        print("Logging in");
        StartCoroutine(LoginCor());
    }

    private IEnumerator LoginCor()
    {
        UnityWebRequest loginRequest = UnityWebRequest.PostWwwForm(
            $"http://localhost:5246/Match/login/{_playerName}", ""
        );
        
        OnStartLogin.Invoke();
        yield return loginRequest.SendWebRequest();

        if (loginRequest.result != UnityWebRequest.Result.Success)
        {
            OnLoginFail.Invoke(loginRequest.error);
            print(loginRequest.error);
            yield break;
        }
        
        OnLoginSuccess.Invoke();
        var text = loginRequest.downloadHandler.text;
        _currentMatch = JsonUtility.FromJson<MatchData>(text);
        print(_currentMatch.id);
    }
}
