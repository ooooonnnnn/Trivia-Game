using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.Networking;
using System.Collections.Generic;
using DataTypes;
using DefaultNamespace;

public class LoginManager : MonoBehaviour
{
    public UnityEvent<string> OnLoginFail;
    public UnityEvent OnLoginSuccess;
    public UnityEvent OnStartLogin;
    [SerializeField] private MatchReadyPoller matchReadyPoller;
    [SerializeField] private GameManager gameManager;
    private MatchData _currentMatch = null;
    public const string BASE_URL = "http://localhost:5246";

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
            $"{BASE_URL}/Match/login/{_playerName}", ""
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
        matchReadyPoller.StartPoll(_currentMatch.id);
        gameManager.matchID = _currentMatch.id;
        print($"connected to {_currentMatch.id}");
    }

    private void OnApplicationQuit()
    {
        print("OnQuit");
        Logout();
    }

    public void Logout() => StartCoroutine(LogoutCor());
    
    public IEnumerator LogoutCor()
    {
        var logoutRequest = UnityWebRequest.Post(
            $"{BASE_URL}/Match/logout/{_playerName}", "");
        yield return logoutRequest.SendWebRequest();
    }
}
