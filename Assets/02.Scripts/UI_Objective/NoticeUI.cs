using System.Collections;
using TMPro;
using UnityEngine;

// 화면 중앙 아래에 짧은 안내 문구(무기 획득 등)를 띄웠다가 사라지게 한다.
public class NoticeUI : SingletonBehaviour<NoticeUI>
{
    [SerializeField] private TMP_Text _text;
    [SerializeField] private CanvasGroup _group;
    [SerializeField] private float _fadeTime = 0.4f;

    private Coroutine _routine;

    protected override void Awake()
    {
        base.Awake();
        if (_group != null)
        {
            _group.alpha = 0f;
        }
    }

    public static void Show(string message, float duration = 2.5f)
    {
        if (Instance == null)
        {
            GameLogger.LogWarning("NoticeUI가 씬에 없습니다: " + message);
            return;
        }
        Instance.Display(message, duration);
    }

    private void Display(string message, float duration)
    {
        if (_text == null || _group == null)
        {
            GameLogger.LogError("NoticeUI: 참조가 비어 있습니다.");
            return;
        }
        if (_routine != null)
        {
            StopCoroutine(_routine);
        }
        _text.text = message;
        _routine = StartCoroutine(Show_Coroutine(duration));
    }

    private IEnumerator Show_Coroutine(float duration)
    {
        yield return Fade(_group.alpha, 1f);
        yield return new WaitForSecondsRealtime(duration);
        yield return Fade(1f, 0f);
        _routine = null;
    }

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < _fadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            _group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / _fadeTime));
            yield return null;
        }
        _group.alpha = to;
    }
}
