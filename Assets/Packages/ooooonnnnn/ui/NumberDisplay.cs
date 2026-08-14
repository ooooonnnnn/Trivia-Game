using TMPro;
using UnityEngine;

namespace ooooonnnnn.ui
{
    /// <summary>
    /// Displays a number with a specified format
    /// </summary>
    public class NumberDisplay : MonoBehaviour
    {
        [SerializeField] private string format;
        [SerializeField] private float[] numbers;
        [SerializeField] private TMP_Text text;
        private object[] _boxedNumbers;

        public void SetNumber(float newNumber, int index = -1)
        {
            if (index == -1)
                numbers[0] = newNumber;
            else
                numbers[index] = newNumber;
            UpdateDisplay();
        }

        public void UpdateDisplay()
        {
            BoxNumbers();
            text.text = string.Format(format, _boxedNumbers);
        }

        private void BoxNumbers()
        {
            _boxedNumbers = new object[numbers.Length];
            for (int i = 0; i < numbers.Length; i++)
            {
                _boxedNumbers[i] = numbers[i];
            }
        }

        private void OnValidate()
        {
            UpdateDisplay();
        }
    }
}