using System;
using System.Collections;
using UnityEngine;

public class SinWiggle : MonoBehaviour
{
    [SerializeField] private Vector2 initPos;
    [SerializeField] private float freqX, ampX, freqY, ampY, phase;

    public void OnValidate()
    {
        initPos = transform.position;
    }

    private void OnEnable()
    {
        StartCoroutine(WiggleCor());
    }

    private IEnumerator WiggleCor()
    {
        while (true)
        {
            transform.position = initPos
                                 + ampX * Mathf.Sin(Time.time * 2 * Mathf.PI * freqX) * Vector2.right
                                 + ampY * Mathf.Sin(Time.time * 2 * Mathf.PI * freqY + phase) * Vector2.up;
            yield return null;
        }
    }
}
