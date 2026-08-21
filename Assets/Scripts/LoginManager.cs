using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.Networking;

public class LoginManager : MonoBehaviour
{
    public UnityEvent<string> OnLoginFail;
    public UnityEvent OnLoginSuccess;

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
        
        yield return loginRequest.SendWebRequest();

        if (loginRequest.result != UnityWebRequest.Result.Success)
        {
            OnLoginFail.Invoke(loginRequest.error);
            print(loginRequest.error);
            yield break;
        }
        
        OnLoginSuccess.Invoke();
        print("Logged in");
    }
}
