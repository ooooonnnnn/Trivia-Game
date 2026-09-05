using System;
using System.Collections.Generic;
using ooooonnnnn.ui;
using UnityEngine;

public class ResultsUI : MonoBehaviour
{
    [SerializeField] private NumberDisplay scoreNumber;
    [SerializeField] private Transform playerScoreListContainer;
    [SerializeField] private GameObject winMessage;
    [SerializeField] private GameObject loseMessage;
    [SerializeField] private GameObject waitingMessage;
    [SerializeField] private NumberDisplay playerScorePrefab;
    
    public void UpdateLocalScore(float score) => scoreNumber.SetNumber(score);

    private void Start()
    {
        UpdatePlayerScoreList(new List<(string, float)>());
        
        winMessage.SetActive(false);
        loseMessage.SetActive(false);
        waitingMessage.SetActive(true);
    }
    
    [ContextMenu("Test Update Player Score List")]
    private void TestUpdatePlayerScoreList() =>
        UpdatePlayerScoreList(new List<(string, float)> {("Player 1", 100f), ("Player 2", 200f)});

    public void UpdatePlayerScoreList(List<(string, float)> playerScores)
    {
        if (!playerScoreListContainer)
        {
            Debug.LogWarning("No player score list container found!");
            return;
        }
        
        if (waitingMessage) waitingMessage.SetActive(false);
        
        foreach (Transform child in playerScoreListContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var playerScore in playerScores)
        {
            var newRow = Instantiate(playerScorePrefab, playerScoreListContainer);

            newRow.format = $"{playerScore.Item1}: {{0:N1}}";
            newRow.SetNumber(playerScore.Item2);
        }
    }
    
    [ContextMenu("Show Win Message")]
    public void ShowWinMessage()
    {
        winMessage.SetActive(true);
        loseMessage.SetActive(false);
    }

    [ContextMenu("Show Lose Message")]
    public void ShowLoseMessage()
    {
        loseMessage.SetActive(true);
        winMessage.SetActive(false);
    }
}
