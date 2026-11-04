using System.Collections;
using UnityEngine;

// 활성화될 때마다 지정한 방향에서 미끄러져 들어오며 깜빡이듯 나타나는 연출.
// 패널을 다시 켤 때도 재생되므로 로그인/회원가입 전환에도 그대로 쓴다.
[RequireComponent(typeof(CanvasGroup))]
public class UIEntrance : MonoBehaviour
{
    [SerializeField] private float _delay;
    [SerializeField] private float _duration = 0.45f;
    [SerializeField] private Vector2 _offset = new Vector2(-50f, 0f);
    [SerializeField, Range(0f, 1f)] private float _flicker = 0.6f;

    private CanvasGroup _group;
    private RectTransform _rect;
    private Vector2 _home;

    private void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        _rect = (RectTransform)transform;
        _home = _rect.anchoredPosition;
    }

    private void OnEnable()
    {
        StartCoroutine(Play_Coroutine());
    }

    private void OnDisable()
    {
        _group.alpha = 1f;
        _rect.anchoredPosition = _home;
    }

    private IEnumerator Play_Coroutine()
    {
        _group.alpha = 0f;
        _rect.anchoredPosition = _home + _offset;
        if (_delay > 0f)
        {
            yield return new WaitForSecondsRealtime(_delay);
        }

        float elapsed = 0f;
        while (elapsed < _duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / _duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            _rect.anchoredPosition = Vector2.LerpUnclamped(_home + _offset, _home, eased);

            float alpha = eased;
            if (t < 0.7f && Random.value < _flicker * 0.5f)
            {
                alpha *= 0.3f;
            }
            _group.alpha = alpha;
            yield return null;
        }

        _group.alpha = 1f;
        _rect.anchoredPosition = _home;
    }
}
