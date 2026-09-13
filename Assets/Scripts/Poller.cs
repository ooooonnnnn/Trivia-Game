using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public abstract class Poller : MonoBehaviour
{
    [SerializeField] private float pollInterval = 1.5f;
    [SerializeField] private bool LimitPollAttempts = true;
    [SerializeField] private int maxPollAttempts = 10;
    protected abstract string PollUrl { get; }
    
    protected IEnumerator PollCor(int matchId)
    {
        for (int numAttempts = 0; 
             (numAttempts < maxPollAttempts) || !LimitPollAttempts;
             numAttempts++)
        {
            UnityWebRequest pollRequest = UnityWebRequest.Get(PollUrl);

            yield return pollRequest.SendWebRequest();
                
            if (pollRequest.result != UnityWebRequest.Result.Success)
                continue;
                
            var resultText = pollRequest.downloadHandler.text;
                
            print(resultText);
            if (PollSuccessCondition(resultText))
            {
                HandlePollSuccess();
                yield break;
            }

            yield return new WaitForSeconds(pollInterval);
        }
    }

    protected abstract bool PollSuccessCondition(string resultText);
    
    protected abstract void HandlePollSuccess();
}