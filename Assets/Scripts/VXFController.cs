using System.Collections;
using UnityEngine;

public class VXFController : MonoBehaviour
{
    [SerializeField] private GameObject_Float_Pair correctEffect, wrongEffect;

    [ContextMenu("Show Correct Effect")]
    public void ShowCorrectEffect() => StartCoroutine(
        FlashObject(correctEffect.gameObject, correctEffect.number));
    
    [ContextMenu("Show Wrong Effect")]
    public void ShowWrongEffect() => StartCoroutine(
        FlashObject(wrongEffect.gameObject, wrongEffect.number));

    private IEnumerator FlashObject(GameObject obj, float duration)
    {
        obj.SetActive(true);

        yield return new WaitForSeconds(duration);
        
        obj.SetActive(false);
    }
}
