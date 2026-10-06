using System.Collections;
using UnityEngine;

public class VXFController : MonoBehaviour
{
    [SerializeField] private GameObject_Float_Pair correctEffect, wrongEffect;

    [ContextMenu("Show Correct Effect")]
    public void ShowCorrectEffect()
    {
        ResetEffects();

        StartCoroutine(
            FlashObject(correctEffect.gameObject, correctEffect.number));
    }

    private void ResetEffects()
    {
        StopAllCoroutines();
        correctEffect.gameObject.SetActive(false);
        wrongEffect.gameObject.SetActive(false);
    }

    [ContextMenu("Show Wrong Effect")]
    public void ShowWrongEffect()
    {
        ResetEffects();
        
        StartCoroutine(
            FlashObject(wrongEffect.gameObject, wrongEffect.number));
    }

    private IEnumerator FlashObject(GameObject obj, float duration)
    {
        obj.SetActive(true);

        yield return new WaitForSeconds(duration);
        
        obj.SetActive(false);
    }
}
