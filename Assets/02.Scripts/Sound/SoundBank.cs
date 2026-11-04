using System.Collections.Generic;
using UnityEngine;

// SfxId와 실제 소리 파일을 짝지어 둔 목록. 소리를 바꾸거나 추가할 때는 이 에셋만 수정한다.
[CreateAssetMenu(fileName = "SoundBank", menuName = "Sound/SoundBank")]
public class SoundBank : ScriptableObject
{
    [SerializeField] private List<SoundEntry> _entries = new List<SoundEntry>();

    private Dictionary<SfxId, SoundEntry> _lookup;

    public bool TryGet(SfxId id, out SoundEntry entry)
    {
        if (_lookup == null)
        {
            BuildLookup();
        }
        return _lookup.TryGetValue(id, out entry);
    }

    private void BuildLookup()
    {
        _lookup = new Dictionary<SfxId, SoundEntry>(_entries.Count);
        foreach (SoundEntry entry in _entries)
        {
            if (entry == null || entry.Id == SfxId.None)
            {
                continue;
            }
            if (!_lookup.TryAdd(entry.Id, entry))
            {
                GameLogger.LogWarning("SoundBank: " + entry.Id + " 항목이 중복되어 첫 번째만 씁니다.");
            }
        }
    }

    // 인스펙터에서 값을 고치면 다음 재생 때 목록을 다시 만든다.
    private void OnValidate()
    {
        _lookup = null;
    }
}
