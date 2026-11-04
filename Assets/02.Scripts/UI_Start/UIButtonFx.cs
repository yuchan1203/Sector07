using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 버튼 호버/클릭 연출: 확대, 배경·글자색 전환, 화살표 밀기, 누를 때 찌그러짐.
public class UIButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("대상")]
    [SerializeField] private Graphic _background;
    [SerializeField] private TMP_Text _label;
    [SerializeField] private RectTransform _arrow;

    [Header("색")]
    [SerializeField] private Color _backgroundNormal = Color.white;
    [SerializeField] private Color _backgroundHover = Color.white;
    [SerializeField] private Color _labelNormal = Color.black;
    [SerializeField] private Color _labelHover = Color.black;

    [Header("움직임")]
    [SerializeField] private float _hoverScale = 1.03f;
    [SerializeField] private float _pressedScale = 0.96f;
    [SerializeField] private float _arrowShift = 10f;
    [SerializeField] private float _speed = 14f;

    private bool _hover;
    private bool _pressed;
    private float _blend;
    private Vector2 _arrowHome;

    private void Awake()
    {
        if (_arrow != null)
        {
            _arrowHome = _arrow.anchoredPosition;
        }
    }

    private void OnEnable()
    {
        _hover = false;
        _pressed = false;
        _blend = 0f;
        Apply(0f, 1f);
    }

    public void OnPointerEnter(PointerEventData eventData) => _hover = true;
    public void OnPointerExit(PointerEventData eventData) { _hover = false; _pressed = false; }
    public void OnPointerDown(PointerEventData eventData) => _pressed = true;
    public void OnPointerUp(PointerEventData eventData) => _pressed = false;

    private void Update()
    {
        float target = _hover ? 1f : 0f;
        _blend = Mathf.MoveTowards(_blend, target, Time.unscaledDeltaTime * _speed);

        float scale = Mathf.Lerp(1f, _hoverScale, _blend);
        if (_pressed)
        {
            scale = _pressedScale;
        }
        Apply(_blend, Mathf.Lerp(transform.localScale.x, scale, Time.unscaledDeltaTime * _speed * 1.5f));
    }

    private void Apply(float blend, float scale)
    {
        transform.localScale = new Vector3(scale, scale, 1f);
        if (_background != null)
        {
            _background.color = Color.Lerp(_backgroundNormal, _backgroundHover, blend);
        }
        if (_label != null)
        {
            _label.color = Color.Lerp(_labelNormal, _labelHover, blend);
        }
        if (_arrow != null)
        {
            _arrow.anchoredPosition = _arrowHome + new Vector2(_arrowShift * blend, 0f);
        }
    }
}
