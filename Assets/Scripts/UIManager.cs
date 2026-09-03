using System;
using System.Collections.Generic;
using DataTypes;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] List<UIScreen_GameObject_Pair> UIScreens;
    [SerializeField] TMP_Text waitingText;
    private Dictionary<UIScreen, GameObject> _UIScreens;

    private void Awake()
    {
        _UIScreens = new();
        foreach (var pair in UIScreens)
        {
            _UIScreens.Add(pair.Screen, pair.GameObject);
        }
        
        ShowScreen(UIScreen.Login);
    }
    
    public void ShowLoggingIn()
    {
        ShowScreen(UIScreen.Waiting);
        waitingText.text = "Logging in...";
    }

    public void ShowWaitingForMatch()
    {
        ShowScreen(UIScreen.Waiting);
        waitingText.text = "Waiting for match to begin...";
    }

    public void ShowGameScreen() => ShowScreen(UIScreen.Game);
    
    public void ShowLoginScreen() => ShowScreen(UIScreen.Login);
    public void ShowResultsScreen() => ShowScreen(UIScreen.Results);

    public void ShowScreen(UIScreen screen)
    {
        foreach (var pair in _UIScreens)
        {
            pair.Value.SetActive(pair.Key == screen);
        }
    }
}

[Serializable]
public class UIScreen_GameObject_Pair
{
    public UIScreen Screen;
    public GameObject GameObject;
}