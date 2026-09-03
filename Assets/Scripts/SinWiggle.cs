using System.Collections;
using UnityEngine;

public class SinWiggle : MonoBehaviour
{
    private Vector2 _initPos;
    [SerializeField] private float freqX, ampX, freqY, ampY, phase;

    public void Start()
    {
        _initPos = transform.position;
        StartCoroutine(WiggleCor());
    }

    private IEnumerator WiggleCor()
    {
        while (true)
        {
            transform.position = _initPos
                                 + ampX * Mathf.Sin(Time.time * 2 * Mathf.PI * freqX) * Vector2.right
                                 + ampY * Mathf.Sin(Time.time * 2 * Mathf.PI * freqY + phase) * Vector2.up;
            yield return null;
        }
    }
}
