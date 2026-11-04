using System.Collections;
using TMPro;
using UnityEngine;

// 제목 글자에 켜질 때의 네온 깜빡임과, 주기적인 RGB 분리 글리치를 입힌다.
// 고스트 텍스트 두 개(빨강/청록)를 본문 뒤에 두고 짧게 흔든다.
public class UIGlitch : MonoBehaviour
{
    [SerializeField] private TMP_Text _main;
    [SerializeField] private TMP_Text _ghostA;
    [SerializeField] private TMP_Text _ghostB;

    [Header("타이밍")]
    [SerializeField] private float _bootDelay = 0.9f;
    [SerializeField] private Vector2 _intervalRange = new Vector2(2.5f, 5f);
    [SerializeField] private float _burstDuration = 0.22f;
    [SerializeField] private float _maxShift = 12f;

    private Vector2 _mainHome;
    private Vector2 _ghostAHome;
    private Vector2 _ghostBHome;

    private void Awake()
    {
        _mainHome = _main.rectTransform.anchoredPosition;
        _ghostAHome = _ghostA.rectTransform.anchoredPosition;
        _ghostBHome = _ghostB.rectTransform.anchoredPosition;
        _ghostA.text = _main.text;
        _ghostB.text = _main.text;
    }

    private void OnEnable()
    {
        StartCoroutine(Loop_Coroutine());
    }

    private IEnumerator Loop_Coroutine()
    {
        SetAlpha(_main, 0f);
        SetAlpha(_ghostA, 0f);
        SetAlpha(_ghostB, 0f);
        yield return new WaitForSecondsRealtime(_bootDelay);

        // 부팅 깜빡임: 형광등이 켜지듯 몇 번 껌뻑인다.
        float[] flicker = { 1f, 0f, 0.8f, 0.1f, 1f, 0.4f, 1f };
        foreach (float a in flicker)
        {
            SetAlpha(_main, a);
            yield return new WaitForSecondsRealtime(Random.Range(0.04f, 0.09f));
        }
        yield return Burst_Coroutine();

        while (true)
        {
            yield return new WaitForSecondsRealtime(Random.Range(_intervalRange.x, _intervalRange.y));
            yield return Burst_Coroutine();
        }
    }

    private IEnumerator Burst_Coroutine()
    {
        float elapsed = 0f;
        while (elapsed < _burstDuration)
        {
            _ghostA.rectTransform.anchoredPosition = _ghostAHome + new Vector2(Random.Range(-_maxShift, _maxShift), Random.Range(-3f, 3f));
            _ghostB.rectTransform.anchoredPosition = _ghostBHome + new Vector2(Random.Range(-_maxShift, _maxShift), Random.Range(-3f, 3f));
            _main.rectTransform.anchoredPosition = _mainHome + new Vector2(Random.Range(-3f, 3f), 0f);
            SetAlpha(_ghostA, 0.75f);
            SetAlpha(_ghostB, 0.75f);
            SetAlpha(_main, Random.value < 0.25f ? 0.5f : 1f);

            float step = Random.Range(0.025f, 0.05f);
            elapsed += step;
            yield return new WaitForSecondsRealtime(step);
        }

        _ghostA.rectTransform.anchoredPosition = _ghostAHome;
        _ghostB.rectTransform.anchoredPosition = _ghostBHome;
        _main.rectTransform.anchoredPosition = _mainHome;
        SetAlpha(_ghostA, 0f);
        SetAlpha(_ghostB, 0f);
        SetAlpha(_main, 1f);
    }

    private static void SetAlpha(TMP_Text text, float alpha)
    {
        Color c = text.color;
        c.a = alpha;
        text.color = c;
    }
}
