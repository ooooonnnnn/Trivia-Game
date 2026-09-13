using UnityEngine;
using UnityEngine.Events;

public class MatchOverPoller : Poller
{
    public UnityEvent OnMatchOver;
    [SerializeField] private GameManager gameManager;
    
    protected override string PollUrl => pollUrl;
    private string pollUrl;
    
    public void StartPoll()
    {
        var matchId = gameManager.matchID;
        pollUrl = $"http://localhost:5246/Match/is-complete/{matchId}";
        StartCoroutine(PollCor(matchId));
    }
    
    protected override bool PollSuccessCondition(string resultText) 
        => resultText == "true";

    protected override void HandlePollSuccess()
         => OnMatchOver.Invoke();
}
