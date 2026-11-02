using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.AI;

public class Zombie : MonoBehaviour, IDamageAble
{
    [Header("스탯")]
    [SerializeField] private float _detectDistance = 8;
    [SerializeField] private float _comebackDistance = 15;
    [SerializeField] private float _attackRange = 1.5f;
    [SerializeField] private float _attackInterval = 1.2f;
    [SerializeField] private float _moveSpeed = 4;
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private float _attackDamage = 10f;

    [Header("애니메이션 타이밍")]
    [Tooltip("공격 트리거 후 실제 대미지가 들어가기까지의 시간(초). Attack 재생 속도 1.2배 기준")]
    [SerializeField] private float _attackHitDelay = 1.25f;
    [Tooltip("대미지 후 공격 동작이 끝날 때까지의 시간(초)")]
    [SerializeField] private float _attackRecoverTime = 0.5f;
    [Tooltip("피격 애니메이션 동안 멈춰 있는 시간(초). Hit → Move 전이 시점과 맞춘다")]
    [SerializeField] private float _hitDuration = 1.2f;
    [Tooltip("쓰러지는 애니메이션 길이(초). Death 상태 클립 길이와 맞춘다")]
    [SerializeField] private float _deathAnimationTime = 1.4f;
    [Tooltip("다 쓰러진 뒤 사라지기까지 기다리는 시간(초)")]
    [SerializeField] private float _corpseRemainTime = 1f;

    [Header("연결")]
    [SerializeField] private GameObject _bloodSprayPrefab;
    [Tooltip("핏자국 이펙트가 반복 재생되므로 이 시간 뒤에 풀로 돌려보낸다(초)")]
    [SerializeField] private float _bloodSprayDuration = 1f;
    [SerializeField] private GameObject _damageTextPrefab;
    [SerializeField] private Transform _textSpawnPoint;

    [Header("상태별 몸 색 (Idle은 원래 색 유지)")]
    [Tooltip("비워 두면 자식의 SkinnedMeshRenderer/MeshRenderer를 자동으로 찾는다")]
    [SerializeField] private Renderer[] _bodyRenderers;
    [SerializeField] private Color _traceColor = Color.green;
    [SerializeField] private Color _comebackColor = Color.cyan;
    [SerializeField] private Color _attackColor = new Color(1f, 0.5f, 0f);
    [SerializeField] private Color _hitColor = Color.red;
    [SerializeField] private Color _deathColor = Color.gray;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private Animator _animator;
    private MaterialPropertyBlock _propertyBlock;
    private ZombieState _appliedColorState = ZombieState.Idle;
    private NavMeshAgent _agent;
    private float _health = 30;
    private Vector3 _startPosition;
    private GameObject _player;
    private ZombieState _state = ZombieState.Idle;
    private float _attackTimer = 0f;
    private bool _isAttacking = false;
    public event System.Action HealthChanged;
    public float Health => _health;
    public float MaxHealth => _maxHealth;
    private void Awake()
    {
        // 체력바가 Start에서 처음 값을 읽을 수 있도록 Awake에서 초기화한다.
        _health = _maxHealth;
    }
    private void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponentInChildren<Animator>();
        _agent.speed = _moveSpeed;
        _startPosition = transform.position;
        _player = GameObject.FindGameObjectWithTag("Player");

        _propertyBlock = new MaterialPropertyBlock();
        if (_bodyRenderers == null || _bodyRenderers.Length == 0)
        {
            _bodyRenderers = GetComponentsInChildren<Renderer>();
        }
    }
    private void Update()
    {
        if (_state != _appliedColorState)
        {
            ApplyStateColor();
        }
        if (GameManager.Instance == null || GameManager.Instance.GameState != GameState.Playing)
        {
            return;
        }
        switch (_state)
        {
            case ZombieState.Idle:
                {
                    Idle();
                    break;
                }
            case ZombieState.Trace:
                {
                    Trace();
                    break;
                }
            case ZombieState.Comeback:
                {
                    Comeback();
                    break;
                }
            case ZombieState.Attack:
                {
                    Attack();
                    break;
                }
        }
    }
    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_state == ZombieState.Death)
        {
            return;
        }
        _health -= damageInfo.Value;
        HealthChanged?.Invoke();
        StopAllCoroutines();
        // 진행 중이던 공격은 피격으로 취소된다.
        _isAttacking = false;
        ShowDamageText(damageInfo.Value);
        if (_health > 0f)
        {
            PoolManager.Spawn(_bloodSprayPrefab, damageInfo.HitPoint, Quaternion.LookRotation(damageInfo.HitNormal), transform, _bloodSprayDuration);
            _state = ZombieState.Hit;
            StartCoroutine(Hit_Coroutine());
        }
        else
        {
            _state = ZombieState.Death;
            StartCoroutine(Death_Coroutine());
        }
    }
    private void ApplyStateColor()
    {
        _appliedColorState = _state;
        foreach (Renderer bodyRenderer in _bodyRenderers)
        {
            if (bodyRenderer == null)
            {
                continue;
            }
            if (_state == ZombieState.Idle)
            {
                // 프로퍼티 블록을 비워 머티리얼의 원래 색으로 되돌린다.
                bodyRenderer.SetPropertyBlock(null);
                continue;
            }
            Color color = GetStateColor(_state);
            bodyRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, color);
            _propertyBlock.SetColor(ColorId, color);
            bodyRenderer.SetPropertyBlock(_propertyBlock);
        }
    }

    private Color GetStateColor(ZombieState state)
    {
        return state switch
        {
            ZombieState.Trace => _traceColor,
            ZombieState.Comeback => _comebackColor,
            ZombieState.Attack => _attackColor,
            ZombieState.Hit => _hitColor,
            ZombieState.Death => _deathColor,
            _ => Color.white,
        };
    }

    private void ShowDamageText(float damage)
    {
        if (_damageTextPrefab == null || _textSpawnPoint == null)
        {
            return;
        }
        GameObject textObj = PoolManager.Spawn(_damageTextPrefab, _textSpawnPoint.position, Quaternion.identity);
        // 3D TextMeshPro(TextMeshPro)와 UI용(TextMeshProUGUI) 모두 TMP_Text를 상속한다.
        TMP_Text tmp = textObj.GetComponentInChildren<TMP_Text>();
        if (tmp != null)
        {
            tmp.text = Mathf.RoundToInt(damage).ToString();
        }
    }

    private IEnumerator Hit_Coroutine()
    {
        Stop();
        ResetActionTriggers();
        _animator.SetTrigger("Hit");
        yield return new WaitForSeconds(_hitDuration);
        _state = ZombieState.Trace;
    }
    private IEnumerator Death_Coroutine()
    {
        Stop();
        // 쓰러진 몸은 더 이상 길을 막거나 공격에 맞지 않는다.
        _agent.enabled = false;
        foreach (Collider bodyCollider in GetComponentsInChildren<Collider>())
        {
            bodyCollider.enabled = false;
        }
        ResetActionTriggers();
        _animator.ResetTrigger("Hit");
        _animator.SetTrigger("Death");
        yield return new WaitForSeconds(_deathAnimationTime + _corpseRemainTime);
        Destroy(this.gameObject);
    }

    // 이전 상태에서 남은 트리거가 피격·사망 이후에 발동하지 않도록 비운다.
    private void ResetActionTriggers()
    {
        _animator.ResetTrigger("IdleToMove");
        _animator.ResetTrigger("MoveToIdle");
        _animator.ResetTrigger("MoveAttackDelay");
        _animator.ResetTrigger("StartAttack");
        _animator.ResetTrigger("AttackMove");
    }
    private void Idle()
    {
        if (_player == null) return;
        float distance = Vector3.Distance(transform.position, _player.transform.position);
        if (distance <= _detectDistance)
        {
            _state = ZombieState.Trace;
            _animator.SetTrigger("IdleToMove");
        }
    }
    private void Trace()
    {
        if (_player == null) return;

        if (Vector3.Distance(transform.position, _startPosition) > _comebackDistance)
        {
            _state = ZombieState.Comeback;
            return;
        }
        if (Vector3.Distance(transform.position, _player.transform.position) <= _attackRange)
        {
            _attackTimer = 0f;
            _state = ZombieState.Attack;
            _animator.SetTrigger("MoveAttackDelay");
            return;
        }
        _agent.SetDestination(_player.transform.position);
    }

    private void Comeback()
    {
        if (!_agent.pathPending && _agent.hasPath && _agent.remainingDistance <= _agent.stoppingDistance)
        {
            // Idle 진입 시 한 번만 정지시킨다.
            Stop();
            _state = ZombieState.Idle;
            _animator.SetTrigger("MoveToIdle");
            return;
        }
        _agent.SetDestination(_startPosition);
    }

    private void Attack()
    {
        if (_player == null) return;
        // 휘두르는 동작 중에는 끝까지 마친 뒤 다음 행동을 정한다.
        if (_isAttacking) return;
        _attackTimer += Time.deltaTime;
        float distance = Vector3.Distance(transform.position, _player.transform.position);
        if (distance > _attackRange)
        {
            _state = ZombieState.Trace;
            _animator.SetTrigger("AttackMove");
        }
        else if (_attackTimer > _attackInterval)
        {
            StartCoroutine(Attack_Coroutine());
        }
    }

    private IEnumerator Attack_Coroutine()
    {
        _isAttacking = true;
        _animator.SetTrigger("StartAttack");
        Stop();
        // 애니메이션에서 팔을 뻗는 시점에 대미지를 준다.
        yield return new WaitForSeconds(_attackHitDelay);
        Collider[] colliders = Physics.OverlapSphere(transform.position, _attackRange, LayerMask.GetMask("Player"));
        foreach (var col in colliders)
        {
            DamageInfo damageInfo = new()
            {
                Value = _attackDamage,
                HitPoint = col.ClosestPoint(transform.position),
                HitNormal = (transform.position - col.ClosestPoint(transform.position)).normalized,
            };
            col.GetComponent<IDamageAble>()?.TakeDamage(damageInfo);
        }
        yield return new WaitForSeconds(_attackRecoverTime);
        _attackTimer = 0f;
        _isAttacking = false;
    }

    private void Stop()
    {
        _agent.ResetPath();
        _agent.velocity = Vector3.zero;
    }
}