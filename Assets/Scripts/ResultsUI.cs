using ooooonnnnn.ui;
using UnityEngine;

public class ResultsUI : MonoBehaviour
{
    [SerializeField] private NumberDisplay scoreNumber;
    
    public void UpdateScore(float score) => scoreNumber.SetNumber(score);
}
