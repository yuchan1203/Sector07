using System.Collections;
using TMPro;
using UnityEngine;

// 본부 무전 자막. 한 글자씩 찍히며 여러 줄을 이어서 보여 준다. 튜토리얼과 구역 시작 때 상황을 알리는 데 쓴다.
public class RadioUI : SingletonBehaviour<RadioUI>
{
    [SerializeField] private TMP_Text _text;
    [SerializeField] private CanvasGroup _group;
    [SerializeField] private string _speaker = "HQ";
    [SerializeField] private float _secondsPerChar = 0.025f;
    [Tooltip("한 줄이 다 찍힌 뒤 머무는 시간(초)")]
    [SerializeField] private float _holdTime = 1.6f;
    [SerializeField] private float _fadeTime = 0.3f;

    private Coroutine _routine;

    protected override void Awake()
    {
        base.Awake();
        if (_group != null)
        {
            _group.alpha = 0f;
        }
    }

    // lines: 차례로 보여 줄 문장들. 새로 부르면 진행 중이던 무전은 끊는다.
    public static void Show(string[] lines)
    {
        if (Instance == null || lines == null || lines.Length == 0)
        {
            return;
        }
        Instance.Play(lines);
    }

    private void Play(string[] lines)
    {
        if (_text == null || _group == null)
        {
            GameLogger.LogError("RadioUI: 참조가 비어 있습니다.");
            return;
        }
        if (_routine != null)
        {
            StopCoroutine(_routine);
        }
        _routine = StartCoroutine(Play_Coroutine(lines));
    }

    private IEnumerator Play_Coroutine(string[] lines)
    {
        _group.alpha = 1f;
        foreach (string line in lines)
        {
            string prefix = "[" + _speaker + "]  ";
            _text.text = prefix;
            for (int i = 1; i <= line.Length; i++)
            {
                _text.text = prefix + line.Substring(0, i);
                yield return new WaitForSecondsRealtime(_secondsPerChar);
            }
            yield return new WaitForSecondsRealtime(_holdTime);
        }

        float elapsed = 0f;
        while (elapsed < _fadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            _group.alpha = 1f - Mathf.Clamp01(elapsed / _fadeTime);
            yield return null;
        }
        _group.alpha = 0f;
        _routine = null;
    }
}
