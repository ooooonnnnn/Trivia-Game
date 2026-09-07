using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.Networking;
using System.Collections.Generic;
using DataTypes;
using DefaultNamespace;
using HelperDataTypes;

public class LoginManager : MonoBehaviour
{
    public UnityEvent<string> OnLoginFail;
    public UnityEvent OnLoginSuccess;
    public UnityEvent OnStartLogin;
    [SerializeField] private MatchReadyPoller matchReadyPoller;
    [SerializeField] private GameManager gameManager;
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
        if (loginRequest.result != UnityWebRequest.Result.Success)
        {
            print($"Login failed: {loginRequest.error}");
            yield break;
        }
        
        var loginResult = JsonUtility.FromJson<IntArrayContainer>("{\"array\": " + text + "}").array;
        gameManager.playerID = loginResult[0];
        gameManager.matchID = loginResult[1];
        print($"connected to match {loginResult[1]} with player id {loginResult[0]}");
        
        matchReadyPoller.StartPoll(loginResult[1]);
    }

    private void OnApplicationQuit()
    {
        print("OnQuit");
        Logout();
    }

    public void Logout() => StartCoroutine(LogoutCor());
    
    private IEnumerator LogoutCor()
    {
        var logoutRequest = UnityWebRequest.Post(
            $"{BASE_URL}/Match/logout/{_playerName}", "");
        yield return logoutRequest.SendWebRequest();
    }
}
