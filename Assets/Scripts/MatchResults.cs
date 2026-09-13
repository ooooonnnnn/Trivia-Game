using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameDataTypes;
using HelperDataTypes;
using UnityEngine;
using UnityEngine.Networking;

public class MatchResults : MonoBehaviour
{
    [SerializeField] private ResultsUI resultsUI;
    [SerializeField] private GameManager gameManager;

    public void WriteResults()
    {
        StartCoroutine(WriteResultsCor());
    }

    private IEnumerator WriteResultsCor()
    {
        var matchId = gameManager.matchID;
        
        List<(int, float)> playerId_scores = new();
        yield return GetMatchResultsCor(matchId, playerId_scores);
        
        List<string> playerNames = new();
        yield return GetPlayerNamesCor(playerId_scores.Select(r => r.Item1).ToArray(), playerNames);
        
        resultsUI.UpdatePlayerScoreList(playerId_scores.Zip(playerNames, (s, n) => (n, s.Item2)).ToList());
    }
    
    public int GetWinnerID(List<(int, float)> results) => 
        results.OrderByDescending(r => r.Item2).First().Item1;
    
    private IEnumerator GetMatchResultsCor(int matchId, List<(int, float)> results)
    {
        var resultsRequest = UnityWebRequest.Get($"http://localhost:5246/Match/players-in-match/{matchId}");
        yield return resultsRequest.SendWebRequest();

        var playersInMatchContainer = JsonUtility.FromJson<PlayerInMatch_Array>(
            "{\"playersInMatch\": " + resultsRequest.downloadHandler.text + "}");
            //.playersInMatch;
        var playersInMatch = playersInMatchContainer.playersInMatch;

        results.Clear();
        results.AddRange(playersInMatch.Select(p => (p.playerId, p.score)));
    }

    private IEnumerator GetPlayerNamesCor(int[] playerIds, List<string> playerNames)
    {
         string baseUrl = "http://localhost:5246/Players/player-names";
         string parameters = string.Join("&", playerIds.Select(id => $"playerIds={id}"));
         var namesRequest = UnityWebRequest.Get($"{baseUrl}?{parameters}");
         yield return namesRequest.SendWebRequest();
         
         var names = JsonUtility.FromJson<StringArrayContainer>(
             "{\"strings\": " + namesRequest.downloadHandler.text + "}")
             .strings;
         
         playerNames.Clear();
         playerNames.AddRange(names);
    }
}
