using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AnswerUIElement : MonoBehaviour
{
    [SerializeField] private TMP_Text answerText;
    public Button answerButton;
    
    public void SetText(string text)
    {
        answerText.text = text;
    }
}
