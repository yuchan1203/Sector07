using System;
using UnityEngine;

// 효과음 한 종류의 재생 설정. 클립을 여러 개 넣으면 재생할 때마다 하나를 골라 쓴다.
[Serializable]
public class SoundEntry
{
    [SerializeField] private SfxId _id;
    [Tooltip("여러 개면 무작위로 고른다. 직전에 쓴 소리는 연달아 나오지 않게 피한다.")]
    [SerializeField] private AudioClip[] _clips;
    [SerializeField, Range(0f, 1f)] private float _volume = 1f;
    [Tooltip("재생할 때마다 피치를 이 범위만큼 무작위로 흔든다.")]
    [SerializeField] private float _pitchVariation = 0.05f;
    [Tooltip("같은 소리를 다시 낼 수 있는 최소 간격(초). 연사나 다중 피격에서 소리가 겹쳐 뭉개지는 것을 막는다.")]
    [SerializeField] private float _minInterval = 0.05f;

    private int _lastIndex = -1;

    public SfxId Id => _id;
    public float Volume => _volume;
    public float PitchVariation => _pitchVariation;
    public float MinInterval => _minInterval;
    public bool HasClips => _clips != null && _clips.Length > 0;

    public AudioClip PickClip()
    {
        if (!HasClips)
        {
            return null;
        }
        int index = UnityEngine.Random.Range(0, _clips.Length);
        if (_clips.Length > 1 && index == _lastIndex)
        {
            index = (index + 1 + UnityEngine.Random.Range(0, _clips.Length - 1)) % _clips.Length;
        }
        _lastIndex = index;
        return _clips[index];
    }
}
