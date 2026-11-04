using System;
using TMPro;
using UnityEngine;

// 상단 HUD의 UTC 시계. 1초마다 갱신한다.
[RequireComponent(typeof(TMP_Text))]
public class UIClock : MonoBehaviour
{
    [SerializeField] private string _format = "UTC {0:HH:mm:ss}";

    private TMP_Text _text;
    private int _lastSecond = -1;

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        DateTime now = DateTime.UtcNow;
        if (now.Second == _lastSecond)
        {
            return;
        }
        _lastSecond = now.Second;
        _text.text = string.Format(_format, now);
    }
}
