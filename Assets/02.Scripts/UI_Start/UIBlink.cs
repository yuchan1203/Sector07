using UnityEngine;
using UnityEngine.UI;

// Graphic(이미지/텍스트)의 알파를 깜빡이거나 숨 쉬듯 오르내린다.
[RequireComponent(typeof(Graphic))]
public class UIBlink : MonoBehaviour
{
    [SerializeField] private float _period = 1.2f;
    [SerializeField, Range(0f, 1f)] private float _minAlpha = 0.15f;
    [Tooltip("true면 부드럽게, false면 켜짐/꺼짐으로 딱딱 끊어서 깜빡인다.")]
    [SerializeField] private bool _smooth = true;

    private Graphic _graphic;

    private void Awake()
    {
        _graphic = GetComponent<Graphic>();
    }

    private void Update()
    {
        float phase = Mathf.Repeat(Time.unscaledTime / _period, 1f);
        float alpha;
        if (_smooth)
        {
            alpha = Mathf.Lerp(_minAlpha, 1f, 0.5f + 0.5f * Mathf.Sin(phase * Mathf.PI * 2f));
        }
        else
        {
            alpha = phase < 0.6f ? 1f : _minAlpha;
        }

        Color c = _graphic.color;
        c.a = alpha;
        _graphic.color = c;
    }
}
