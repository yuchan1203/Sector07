using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 입력창 연출: 포커스 시 테두리가 라임색으로 켜지고, 글자를 칠 때마다 테두리가 번쩍인다.
[RequireComponent(typeof(TMP_InputField))]
public class UIInputFx : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Graphic _border;
    [SerializeField] private Color _idle = new Color(0.23f, 0.27f, 0.24f, 1f);
    [SerializeField] private Color _hover = new Color(0.45f, 0.52f, 0.46f, 1f);
    [SerializeField] private Color _focus = new Color(0.65f, 0.89f, 0.18f, 1f);
    [SerializeField] private Color _typeFlash = Color.white;
    [SerializeField] private float _speed = 12f;

    private TMP_InputField _field;
    private bool _focused;
    private bool _hovered;
    private float _flash;

    private void Awake()
    {
        _field = GetComponent<TMP_InputField>();
        _field.onValueChanged.AddListener(_ => _flash = 1f);
    }

    private void OnEnable()
    {
        _border.color = _idle;
        _flash = 0f;
    }

    public void OnSelect(BaseEventData eventData) => _focused = true;
    public void OnDeselect(BaseEventData eventData) => _focused = false;
    public void OnPointerEnter(PointerEventData eventData) => _hovered = true;
    public void OnPointerExit(PointerEventData eventData) => _hovered = false;

    private void Update()
    {
        Color target = _focused ? _focus : (_hovered ? _hover : _idle);
        _flash = Mathf.MoveTowards(_flash, 0f, Time.unscaledDeltaTime * 6f);
        Color current = Color.Lerp(_border.color, target, Time.unscaledDeltaTime * _speed);
        _border.color = Color.Lerp(current, _typeFlash, _flash * 0.8f);
    }
}
