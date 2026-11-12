using System;
using UnityEngine;

public enum ObjectiveType
{
    KillAll,    // 단계의 좀비 그룹을 모두 처치
    ReachArea,  // 목표 지점 반경 안에 도착
    Survive,    // 정해진 시간 동안 버티기
    Action,     // 튜토리얼: 지정한 조작을 해 보기
    Collect,    // 구역 안쪽의 폭발물(ObjectiveTarget)을 획득
    DestroyBarrier, // 막힌 길 장애물(ObjectiveTarget)을 폭파
}

// 튜토리얼 단계에서 확인하는 조작. 레거시 Input 기준이다.
public enum TutorialAction
{
    Move,         // WASD로 이동 (초)
    Look,         // 마우스로 둘러보기 (이동량)
    Jump,         // 점프 (횟수)
    Sprint,       // Shift 질주 (초)
    Fire,         // 발사 버튼 (횟수)
    Aim,          // 우클릭 조준 (초)
    Reload,       // R 재장전 (횟수)
    Melee,        // F 근접 공격 (횟수)
    SwitchWeapon, // C 무기 교체 (횟수)
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

    [Header("튜토리얼")]
    [Tooltip("켜면 HUD에 STAGE 대신 TUTORIAL로 표시하고, 클리어 기록에서 제외한다.")]
    [SerializeField] private bool _isTutorial;
    [Tooltip("Action 목표에서 확인할 조작")]
    [SerializeField] private TutorialAction _action;
    [Tooltip("Action 목표를 채우는 양. 조작에 따라 초 또는 횟수.")]
    [SerializeField] private float _actionAmount = 1f;

    [Header("구역")]
    [Tooltip("0이면 구역에 속하지 않는다. 1 이상이면 HUD에 ZONE n / 전체로 표시한다.")]
    [SerializeField] private int _zone;
    [Tooltip("Collect는 폭발물 픽업, DestroyBarrier는 장애물. Collect는 이 단계가 시작될 때 켜진다.")]
    [SerializeField] private ObjectiveTarget _target;
    [Tooltip("단계가 시작될 때 무전 자막으로 보여 줄 문장. '|'로 나누면 차례로 이어서 보여 준다.")]
    [SerializeField, TextArea] private string _briefing;

    [Tooltip("이 단계를 마치면 얻는 무기 (선택)")]
    [SerializeField] private WeaponData _rewardWeapon;

    public int Zone => _zone;
    public ObjectiveTarget Target => _target;
    public string Briefing => _briefing;
    public WeaponData RewardWeapon => _rewardWeapon;
    public string Title => _title;
    public ObjectiveType Type => _type;
    public GameObject ZombieGroup => _zombieGroup;
    public Transform GoalPoint => _goalPoint;
    public float GoalRadius => _goalRadius;
    public float SurviveSeconds => _surviveSeconds;
    public bool IsTutorial => _isTutorial;
    public TutorialAction Action => _action;
    public float ActionAmount => _actionAmount;
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
    private float _actionProgress;
    private int _tutorialCount;

    // 목표 문구나 진행 문구가 바뀔 때만 알린다.
    public event Action ObjectiveChanged;

    public int StageCount => _stages != null ? _stages.Length : 0;
    public int StageNumber => _stageIndex + 1;
    public int ZoneCount { get; private set; }

    // HUD 상단 문구. 튜토리얼 단계는 TUTORIAL, 이후는 STAGE로 따로 센다.
    public string StageLabel
    {
        get
        {
            StageInfo stage = CurrentStage;
            if (stage == null)
            {
                return "";
            }
            if (stage.IsTutorial)
            {
                return "TUTORIAL " + StageNumber + " / " + _tutorialCount;
            }
            if (stage.Zone > 0)
            {
                return "ZONE " + stage.Zone + " / " + ZoneCount;
            }
            return "STAGE " + (StageNumber - _tutorialCount) + " / " + (StageCount - _tutorialCount);
        }
    }
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
            if (stage == null)
            {
                continue;
            }
            if (stage.IsTutorial)
            {
                _tutorialCount++;
            }
            ZoneCount = Mathf.Max(ZoneCount, stage.Zone);
            // 폭발물은 해당 단계가 시작될 때까지 숨겨 둔다.
            if (stage.Type == ObjectiveType.Collect && stage.Target != null)
            {
                stage.Target.gameObject.SetActive(false);
            }
            if (stage.ZombieGroup != null)
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
        _actionProgress = 0f;
        // 클리어 기록은 튜토리얼이 끝난 뒤부터 잰다.
        if (!stage.IsTutorial && (index == 0 || _stages[index - 1].IsTutorial))
        {
            _elapsed = 0f;
            _totalKills = 0;
        }
        if (stage.Type == ObjectiveType.Collect && stage.Target != null)
        {
            stage.Target.gameObject.SetActive(true);
        }
        // 구역에 처음 들어설 때 알린다.
        if (stage.Zone > 0 && (index == 0 || _stages[index - 1].Zone != stage.Zone))
        {
            NoticeUI.Show("ZONE " + stage.Zone + "  구역 소탕을 시작한다");
        }
        if (!string.IsNullOrEmpty(stage.Briefing))
        {
            RadioUI.Show(stage.Briefing.Split('|'));
        }
        _progressText = "";
        UpdateObjective();
        ObjectiveChanged?.Invoke();
    }

    // 지금 단계가 이 오브젝트를 폭파하는 단계인지 확인한다.
    public bool IsCurrentTarget(ObjectiveTarget target)
    {
        StageInfo stage = CurrentStage;
        return stage != null && stage.Type == ObjectiveType.DestroyBarrier && stage.Target == target;
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
            case ObjectiveType.Action:
                _actionProgress += MeasureAction(stage.Action);
                progress = FormatActionProgress(stage);
                done = _actionProgress >= stage.ActionAmount;
                break;
            case ObjectiveType.Collect:
            case ObjectiveType.DestroyBarrier:
                if (stage.Target == null)
                {
                    GameLogger.LogError("LevelManager: 목표 오브젝트가 비어 있어 바로 넘어갑니다. (" + stage.Title + ")");
                    done = true;
                    break;
                }
                done = stage.Target.IsDone;
                if (!done && _player != null)
                {
                    float targetDistance = FlatDistance(_player.position, stage.Target.transform.position);
                    progress = (stage.Type == ObjectiveType.Collect ? "폭발물까지 " : "막힌 길까지 ") + Mathf.CeilToInt(targetDistance) + "m";
                }
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

    // 이번 프레임에 해당 조작을 얼마나 했는지 (시간형은 초, 횟수형은 횟수).
    private static float MeasureAction(TutorialAction action)
    {
        switch (action)
        {
            case TutorialAction.Move:
                return Mathf.Abs(Input.GetAxisRaw("Horizontal")) + Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0f ? Time.deltaTime : 0f;
            case TutorialAction.Look:
                return Mathf.Abs(Input.GetAxis("Mouse X")) + Mathf.Abs(Input.GetAxis("Mouse Y"));
            case TutorialAction.Jump:
                return Input.GetButtonDown("Jump") ? 1f : 0f;
            case TutorialAction.Sprint:
                bool moving = Mathf.Abs(Input.GetAxisRaw("Horizontal")) + Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0f;
                return Input.GetButton("Sprint") && moving ? Time.deltaTime : 0f;
            case TutorialAction.Fire:
                return Input.GetButtonDown("Fire1") ? 1f : 0f;
            case TutorialAction.Aim:
                return Input.GetButton("Fire2") ? Time.deltaTime : 0f;
            case TutorialAction.Reload:
                return Input.GetKeyDown(KeyCode.R) ? 1f : 0f;
            case TutorialAction.Melee:
                return Input.GetKeyDown(KeyCode.F) ? 1f : 0f;
            case TutorialAction.SwitchWeapon:
                return Input.GetKeyDown(KeyCode.C) ? 1f : 0f;
            default:
                return 0f;
        }
    }

    private string FormatActionProgress(StageInfo stage)
    {
        float amount = Mathf.Max(0.0001f, stage.ActionAmount);
        float shown = Mathf.Min(_actionProgress, amount);
        switch (stage.Action)
        {
            case TutorialAction.Move:
            case TutorialAction.Sprint:
            case TutorialAction.Aim:
                return Mathf.FloorToInt(shown) + " / " + Mathf.CeilToInt(amount) + "초";
            case TutorialAction.Look:
                return Mathf.FloorToInt(shown / amount * 100f) + "%";
            default:
                return Mathf.FloorToInt(shown) + " / " + Mathf.CeilToInt(amount);
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
        StageInfo completed = CurrentStage;
        GiveReward(completed);
        if (_stageIndex + 1 < StageCount)
        {
            if (completed.IsTutorial && !_stages[_stageIndex + 1].IsTutorial)
            {
                NoticeUI.Show("튜토리얼 완료!  본 구역으로 진입합니다");
            }
            // 튜토리얼 단계 사이는 가벼운 완료음, 새 목표가 나올 때는 목표 갱신음. 무기 획득음과 겹치면 조금 늦춘다.
            SfxId cue = _stages[_stageIndex + 1].IsTutorial ? SfxId.TutorialComplete : SfxId.NewObjective;
            SoundManager.Play(cue, completed.RewardWeapon != null ? 0.7f : 0f);
            StartStage(_stageIndex + 1);
            return;
        }

        _finished = true;
        SoundManager.Play(SfxId.GameClear);
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
