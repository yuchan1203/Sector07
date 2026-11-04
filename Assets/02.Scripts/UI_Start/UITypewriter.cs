using System.Collections;
using TMPro;
using UnityEngine;

// 활성화되면 글자가 한 글자씩 찍히듯 나타난다.
[RequireComponent(typeof(TMP_Text))]
public class UITypewriter : MonoBehaviour
{
    [SerializeField] private float _delay;
    [SerializeField] private float _charInterval = 0.04f;

    private TMP_Text _text;

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        StartCoroutine(Type_Coroutine());
    }

    private IEnumerator Type_Coroutine()
    {
        _text.maxVisibleCharacters = 0;
        if (_delay > 0f)
        {
            yield return new WaitForSecondsRealtime(_delay);
        }

        _text.ForceMeshUpdate();
        int total = _text.textInfo.characterCount;
        for (int i = 1; i <= total; i++)
        {
            _text.maxVisibleCharacters = i;
            yield return new WaitForSecondsRealtime(_charInterval);
        }
        _text.maxVisibleCharacters = int.MaxValue;
    }
}
