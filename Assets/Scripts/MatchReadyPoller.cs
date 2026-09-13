using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace DefaultNamespace
{
    public class MatchReadyPoller : Poller
    {
        public UnityEvent OnMatchReady;
        
        [ContextMenu( "Test Poll" )]
        private void TestPoll() => StartPoll(10);

        protected override string PollUrl => pollUrl;
        private string pollUrl;
        
        public void StartPoll(int matchId)
        {
            pollUrl = $"http://localhost:5246/Match/is-active/{matchId}";
            StartCoroutine(PollCor(matchId));
        }

        protected override bool PollSuccessCondition(string resultText)
        {
            return resultText == "true";
        }

        protected override void HandlePollSuccess()
        {
            OnMatchReady.Invoke();
        }
    }
}