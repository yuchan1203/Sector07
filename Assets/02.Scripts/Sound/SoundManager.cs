using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 효과음 재생 담당. 씬마다 하나를 두고 같은 SoundBank 에셋을 연결한다.
// 오디오 소스를 미리 만들어 돌려 쓰므로, 좀비가 사라져도 재생 중이던 소리는 끝까지 난다.
// 씬에 없으면 효과음 호출은 조용히 무시된다.
public class SoundManager : SingletonBehaviour<SoundManager>
{
    [SerializeField] private SoundBank _bank;
    [SerializeField, Range(0f, 1f)] private float _masterVolume = 0.9f;

    [Header("동시 재생")]
    [Tooltip("동시에 낼 수 있는 효과음 개수. 넘치면 가장 먼저 끝날 소리를 끊는다.")]
    [SerializeField] private int _voiceCount = 16;

    [Header("위치 소리(3D)")]
    [SerializeField] private float _minDistance = 3f;
    [SerializeField] private float _maxDistance = 40f;

    [Header("버튼 클릭음")]
    [Tooltip("켜면 씬에 있는 모든 UI 버튼에 클릭음을 자동으로 붙인다.")]
    [SerializeField] private bool _autoButtonClick = true;

    private AudioSource[] _voices;
    private float[] _voiceBusyUntil;
    private float[] _lastPlayTime;
    private readonly HashSet<SfxId> _warned = new HashSet<SfxId>();

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
        {
            return;
        }
        if (_bank == null)
        {
            GameLogger.LogError("SoundManager: SoundBank가 비어 있습니다.");
        }

        _voiceCount = Mathf.Max(1, _voiceCount);
        _voices = new AudioSource[_voiceCount];
        _voiceBusyUntil = new float[_voiceCount];
        _lastPlayTime = new float[System.Enum.GetValues(typeof(SfxId)).Length];
        for (int i = 0; i < _lastPlayTime.Length; i++)
        {
            _lastPlayTime[i] = float.NegativeInfinity;
        }
        for (int i = 0; i < _voiceCount; i++)
        {
            GameObject voiceObject = new GameObject("Voice " + i);
            voiceObject.transform.SetParent(transform, false);
            AudioSource source = voiceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            _voices[i] = source;
        }
    }

    private void Start()
    {
        if (_autoButtonClick && Instance == this)
        {
            AttachButtonClicks();
        }
    }

    // 화면 효과음. 거리와 상관없이 같은 크기로 들린다. delay는 초 단위 지연.
    public static void Play(SfxId id, float delay = 0f)
    {
        if (Instance != null)
        {
            Instance.PlayInternal(id, false, Vector3.zero, 1f, delay);
        }
    }

    // 위치 효과음. 가까울수록 크게, 멀수록 작게 들린다. pitchScale이 1보다 작으면 낮고 굵은 소리가 된다.
    public static void PlayAt(SfxId id, Vector3 position, float pitchScale = 1f)
    {
        if (Instance != null)
        {
            Instance.PlayInternal(id, true, position, pitchScale, 0f);
        }
    }

    private void PlayInternal(SfxId id, bool positional, Vector3 position, float pitchScale, float delay)
    {
        if (id == SfxId.None || _bank == null || _voices == null)
        {
            return;
        }
        if (!_bank.TryGet(id, out SoundEntry entry) || !entry.HasClips)
        {
            if (_warned.Add(id))
            {
                GameLogger.LogWarning("SoundManager: " + id + " 효과음이 SoundBank에 없거나 클립이 비어 있습니다.");
            }
            return;
        }

        float now = Time.unscaledTime;
        if (now - _lastPlayTime[(int)id] < entry.MinInterval)
        {
            return;
        }
        _lastPlayTime[(int)id] = now;

        AudioClip clip = entry.PickClip();
        int voiceIndex = AcquireVoice(now);
        AudioSource voice = _voices[voiceIndex];

        float pitch = Mathf.Max(0.1f, pitchScale * (1f + Random.Range(-entry.PitchVariation, entry.PitchVariation)));
        voice.Stop();
        voice.clip = clip;
        voice.volume = entry.Volume * _masterVolume;
        voice.pitch = pitch;
        voice.spatialBlend = positional ? 1f : 0f;
        voice.minDistance = _minDistance;
        voice.maxDistance = _maxDistance;
        voice.transform.position = positional ? position : transform.position;
        if (delay > 0f)
        {
            voice.PlayDelayed(delay);
        }
        else
        {
            voice.Play();
        }
        _voiceBusyUntil[voiceIndex] = now + delay + clip.length / pitch;
    }

    // 쉬고 있는 소리 칸을 찾는다. 모두 쓰는 중이면 가장 먼저 끝날 칸을 가져온다.
    private int AcquireVoice(float now)
    {
        int oldest = 0;
        for (int i = 0; i < _voices.Length; i++)
        {
            if (_voiceBusyUntil[i] <= now)
            {
                return i;
            }
            if (_voiceBusyUntil[i] < _voiceBusyUntil[oldest])
            {
                oldest = i;
            }
        }
        return oldest;
    }

    private void AttachButtonClicks()
    {
        foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            button.onClick.AddListener(PlayButtonClick);
        }
    }

    private static void PlayButtonClick()
    {
        Play(SfxId.UiClick);
    }
}
