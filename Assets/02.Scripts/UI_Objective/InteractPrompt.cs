using TMPro;
using UnityEngine;

// 화면 중앙 아래에 "[E] ..." 같은 상호작용 안내를 띄운다. 여러 오브젝트가 쓸 수 있도록 안내를 낸 쪽(owner)만 지울 수 있다.
public class InteractPrompt : SingletonBehaviour<InteractPrompt>
{
    [SerializeField] private TMP_Text _text;
    [SerializeField] private CanvasGroup _group;

    private object _owner;

    protected override void Awake()
    {
        base.Awake();
        if (_group != null)
        {
            _group.alpha = 0f;
        }
    }

    public static void Show(object owner, string message)
    {
        if (Instance != null)
        {
            Instance.Display(owner, message);
        }
    }

    public static void Hide(object owner)
    {
        if (Instance != null && Instance._owner == owner)
        {
            Instance.Clear();
        }
    }

    private void Display(object owner, string message)
    {
        if (_text == null || _group == null)
        {
            return;
        }
        _owner = owner;
        if (_text.text != message)
        {
            _text.text = message;
        }
        _group.alpha = 1f;
    }

    private void Clear()
    {
        _owner = null;
        if (_group != null)
        {
            _group.alpha = 0f;
        }
    }
}
