using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 시작 화면 오른쪽의 조준 HUD 연출.
// 조준 틀이 천천히 떠다니며 맥박처럼 커졌다 줄고, 거리 수치가 흔들리고, 가끔 "+1 KILL" 문구가 떠오른다.
public class UITargetHud : MonoBehaviour
{
    [Header("대상")]
    [SerializeField] private RectTransform _frame;
    [SerializeField] private RectTransform _crosshair;
    [SerializeField] private TMP_Text _distanceText;
    [SerializeField] private TMP_Text _killText;
    [SerializeField] private Graphic[] _brackets;

    [Header("움직임")]
    [SerializeField] private Vector2 _drift = new Vector2(22f, 10f);
    [SerializeField] private float _pulseAmount = 0.025f;
    [SerializeField] private float _crosshairSpin = 40f;
    [SerializeField] private Vector2 _killInterval = new Vector2(2.2f, 4.5f);

    [Header("색")]
    [SerializeField] private Color _bracketNormal = new Color(0.65f, 0.89f, 0.18f, 0.9f);
    [SerializeField] private Color _bracketFlash = Color.white;

    private Vector2 _frameHome;
    private Vector2 _killHome;
    private int _distance = 240;
    private float _kick;

    private void Awake()
    {
        _frameHome = _frame.anchoredPosition;
        _killHome = _killText.rectTransform.anchoredPosition;
    }

    private void OnEnable()
    {
        SetKillAlpha(0f);
        StartCoroutine(Distance_Coroutine());
        StartCoroutine(Kill_Coroutine());
    }

    private void Update()
    {
        float t = Time.unscaledTime;
        _kick = Mathf.MoveTowards(_kick, 0f, Time.unscaledDeltaTime * 3f);

        _frame.anchoredPosition = _frameHome + new Vector2(Mathf.Sin(t * 0.6f) * _drift.x, Mathf.Sin(t * 0.9f + 1f) * _drift.y);
        float scale = 1f + Mathf.Sin(t * 2.2f) * _pulseAmount + _kick * 0.1f;
        _frame.localScale = new Vector3(scale, scale, 1f);
        _crosshair.localRotation = Quaternion.Euler(0f, 0f, t * _crosshairSpin);

        Color c = Color.Lerp(_bracketNormal, _bracketFlash, _kick);
        foreach (Graphic g in _brackets)
        {
            g.color = c;
        }
    }

    private IEnumerator Distance_Coroutine()
    {
        while (true)
        {
            _distance = Mathf.Clamp(_distance + Random.Range(-4, 5), 180, 300);
            _distanceText.text = "DIST " + _distance + "m";
            yield return new WaitForSecondsRealtime(0.15f);
        }
    }

    private IEnumerator Kill_Coroutine()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(Random.Range(_killInterval.x, _killInterval.y));

            _kick = 1f;
            Vector2 start = _killHome + new Vector2(Random.Range(0f, 220f), Random.Range(-10f, 40f));
            float elapsed = 0f;
            const float duration = 1f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                _killText.rectTransform.anchoredPosition = start + new Vector2(0f, 50f * t);
                SetKillAlpha(Mathf.Sin(t * Mathf.PI));
                yield return null;
            }
            SetKillAlpha(0f);
        }
    }

    private void SetKillAlpha(float alpha)
    {
        Color c = _killText.color;
        c.a = alpha;
        _killText.color = c;
    }
}
