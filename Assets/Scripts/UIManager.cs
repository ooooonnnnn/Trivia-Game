using System;
using System.Collections.Generic;
using DataTypes;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] List<UIScreen_GameObject_Pair> UIScreens;
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
    
    public void ShowWaitScreen() => ShowScreen(UIScreen.Waiting);
    
    public void ShowGameScreen() => ShowScreen(UIScreen.Game);
    
    public void ShowLoginScreen() => ShowScreen(UIScreen.Login);

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