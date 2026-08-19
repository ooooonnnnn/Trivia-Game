using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace DefaultNamespace
{
    public class MatchReadyPoller : MonoBehaviour
    {
        [SerializeField] private float pollInterval = 1.5f;
        [SerializeField] private bool LimitPollAttempts = true;
        [SerializeField] private int maxPollAttempts = 10;
        public UnityEvent OnMatchReady;
        private Coroutine _pollCoroutine;
        
        [ContextMenu( "Test Poll" )]
        private void TestPoll() => StartPoll(10);
        
        public void StartPoll(int matchId)
        {
            _pollCoroutine = StartCoroutine(PollMatchReady(matchId));
        }

        private IEnumerator PollMatchReady(int matchId)
        {
            for (int numAttempts = 0; 
                 (numAttempts < maxPollAttempts) || !LimitPollAttempts;
                 numAttempts++)
            {
                UnityWebRequest matchreadyReq = UnityWebRequest.Get(
                    $"http://localhost:5246/Match/is-active/{matchId}");
            
                var request = matchreadyReq;

                yield return request.SendWebRequest();
                
                if (request.result != UnityWebRequest.Result.Success)
                    continue;
                
                var resultText = request.downloadHandler.text;
                
                print(resultText);
                if (resultText == "true")
                {
                    OnMatchReady.Invoke();
                    yield break;
                }

                yield return new WaitForSeconds(pollInterval);
            }
        }
    }
}