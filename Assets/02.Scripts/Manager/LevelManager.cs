using System;
using UnityEngine;

public enum ObjectiveType
{
    KillAll,    // 단계의 좀비 그룹을 모두 처치
    ReachArea,  // 목표 지점 반경 안에 도착
    Survive,    // 정해진 시간 동안 버티기
}

// 한 단계(구역)의 목표와 배치 정보.
[Serializable]
public class StageInfo
{
    [SerializeField] private string _title = "목표";
    [SerializeField] private ObjectiveType _type = ObjectiveType.KillAll;

    [Tooltip("단계가 시작될 때 켜지는 오브젝트. 좀비들을 자식으로 둔다. KillAll은 이 그룹의 좀비를 센다.")]
    [SerializeField] private GameObject _zombieGroup;

    [Tooltip("ReachArea 목표 지점")]
    [SerializeField] private Transform _goalPoint;
    [SerializeField] private float _goalRadius = 3f;

    [Tooltip("Survive 버틸 시간(초)")]
    [SerializeField] private float _surviveSeconds = 30f;

    [Tooltip("이 단계를 마치면 얻는 무기 (선택)")]
    [SerializeField] private WeaponData _rewardWeapon;

    public WeaponData RewardWeapon => _rewardWeapon;
    public string Title => _title;
    public ObjectiveType Type => _type;
    public GameObject ZombieGroup => _zombieGroup;
    public Transform GoalPoint => _goalPoint;
    public float GoalRadius => _goalRadius;
    public float SurviveSeconds => _surviveSeconds;
}

// 단계별 목표를 진행하고, 모두 끝나면 게임을 클리어시킨다.
public class LevelManager : SingletonBehaviour<LevelManager>
{
    [SerializeField] private StageInfo[] _stages;
    [SerializeField] private ClearResultUI _clearUI;

    private int _stageIndex = -1;
    private bool _started;
    private bool _finished;
    private int _remaining;
    private float _surviveLeft;
    private float _elapsed;
    private int _totalKills;
    private string _progressText = "";
    private Transform _player;

    // 목표 문구나 진행 문구가 바뀔 때만 알린다.
    public event Action ObjectiveChanged;

    public int StageCount => _stages != null ? _stages.Length : 0;
    public int StageNumber => _stageIndex + 1;
    public string ObjectiveTitle => CurrentStage != null ? CurrentStage.Title : "";
    public string ProgressText => _progressText;
    public float ElapsedTime => _elapsed;
    public int TotalKills => _totalKills;

    private StageInfo CurrentStage =>
        _stages != null && _stageIndex >= 0 && _stageIndex < _stages.Length ? _stages[_stageIndex] : null;

    protected override void Awake()
    {
        base.Awake();
        if (_stages == null)
        {
            return;
        }
        // 아직 시작하지 않은 단계의 좀비는 꺼 둔다.
        foreach (StageInfo stage in _stages)
        {
            if (stage != null && stage.ZombieGroup != null)
            {
                stage.ZombieGroup.SetActive(false);
            }
        }
    }

    private void OnEnable()
    {
        Zombie.Died += OnZombieDied;
    }

    private void OnDisable()
    {
        Zombie.Died -= OnZombieDied;
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.GameState != GameState.Playing || _finished)
        {
            return;
        }

        if (!_started)
        {
            _started = true;
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            _player = playerObject != null ? playerObject.transform : null;
            if (StageCount == 0)
            {
                GameLogger.LogError("LevelManager: 단계가 비어 있습니다.");
                _finished = true;
                return;
            }
            StartStage(0);
        }

        _elapsed += Time.deltaTime;
        UpdateObjective();
    }

    private void StartStage(int index)
    {
        _stageIndex = index;
        StageInfo stage = CurrentStage;
        _remaining = 0;

        if (stage.ZombieGroup != null)
        {
            stage.ZombieGroup.SetActive(true);
            _remaining = stage.ZombieGroup.GetComponentsInChildren<Zombie>(true).Length;
        }
        if (stage.Type == ObjectiveType.KillAll && _remaining == 0)
        {
            GameLogger.LogWarning("LevelManager: KillAll 단계에 좀비가 없어 바로 넘어갑니다. (" + stage.Title + ")");
        }
        _surviveLeft = stage.SurviveSeconds;
        _progressText = "";
        UpdateObjective();
        ObjectiveChanged?.Invoke();
    }

    private void UpdateObjective()
    {
        StageInfo stage = CurrentStage;
        if (stage == null)
        {
            return;
        }

        bool done = false;
        string progress = "";
        switch (stage.Type)
        {
            case ObjectiveType.KillAll:
                progress = "남은 좀비 " + _remaining;
                done = _remaining <= 0;
                break;
            case ObjectiveType.ReachArea:
                if (stage.GoalPoint == null || _player == null)
                {
                    done = true;
                    break;
                }
                float distance = FlatDistance(_player.position, stage.GoalPoint.position);
                progress = "목표 지점까지 " + Mathf.CeilToInt(Mathf.Max(0f, distance - stage.GoalRadius)) + "m";
                done = distance <= stage.GoalRadius;
                break;
            case ObjectiveType.Survive:
                _surviveLeft -= Time.deltaTime;
                progress = "버티기 " + Mathf.CeilToInt(Mathf.Max(0f, _surviveLeft)) + "초";
                done = _surviveLeft <= 0f;
                break;
        }

        if (progress != _progressText)
        {
            _progressText = progress;
            ObjectiveChanged?.Invoke();
        }
        if (done)
        {
            CompleteStage();
        }
    }

    private void GiveReward(StageInfo stage)
    {
        if (stage.RewardWeapon == null || _player == null)
        {
            return;
        }
        WeaponSwitcher switcher = _player.GetComponent<WeaponSwitcher>();
        if (switcher != null && switcher.Unlock(stage.RewardWeapon, true))
        {
            NoticeUI.Show(stage.RewardWeapon.DisplayName + " 획득!  [C] 키로 무기 교체");
        }
    }

    private void CompleteStage()
    {
        GiveReward(CurrentStage);
        if (_stageIndex + 1 < StageCount)
        {
            StartStage(_stageIndex + 1);
            return;
        }

        _finished = true;
        GameManager.Instance.GameClear();
        if (_clearUI != null)
        {
            _clearUI.Show(_elapsed, _totalKills);
        }
    }

    private void OnZombieDied(Zombie zombie)
    {
        _totalKills++;
        StageInfo stage = CurrentStage;
        if (stage != null && stage.ZombieGroup != null && zombie.transform.IsChildOf(stage.ZombieGroup.transform))
        {
            _remaining = Mathf.Max(0, _remaining - 1);
        }
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
