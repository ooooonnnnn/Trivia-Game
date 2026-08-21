using UnityEngine;
using UnityEngine.Events;

public class StringChecker : MonoBehaviour
{
    public UnityEvent OnStringEmpty;
    public UnityEvent OnStringNotEmpty;
    
    public void IsEmpty(string target)
    {
        bool isempty = string.IsNullOrEmpty(target);
        if (isempty)
            OnStringEmpty.Invoke();
        else
            OnStringNotEmpty.Invoke();
    }
}
