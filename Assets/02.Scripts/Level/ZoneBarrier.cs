using System.Collections;
using UnityEngine;

// 메인 길을 막은 장애물. 해당 단계에서 폭발물을 얻은 뒤 가까이 가서 [E] 키로 설치하면 잠시 뒤 터지며 길이 열린다.
// 막는 역할은 이 오브젝트의 콜라이더와 NavMeshObstacle(carve)이 맡고, 터지면 오브젝트를 꺼서 함께 사라진다.
public class ZoneBarrier : ObjectiveTarget
{
    [Header("설치")]
    [SerializeField] private float _interactRange = 4f;
    [SerializeField] private float _fuseTime = 3f;

    [Header("폭발")]
    [SerializeField] private ParticleSystem _explosionEffectPrefab;
    [SerializeField] private float _blastRadius = 7f;
    [Tooltip("폭발 범위 안의 좀비와 드럼통이 받는 대미지")]
    [SerializeField] private float _blastDamage = 200f;

    private Transform _player;
    private bool _fusing;

    private void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        _player = playerObject != null ? playerObject.transform : null;
    }

    private void OnDisable()
    {
        InteractPrompt.Hide(this);
    }

    private void Update()
    {
        if (IsDone || _fusing || _player == null)
        {
            return;
        }
        if (GameManager.Instance == null || GameManager.Instance.GameState != GameState.Playing)
        {
            InteractPrompt.Hide(this);
            return;
        }

        Vector3 delta = _player.position - transform.position;
        delta.y = 0f;
        if (delta.magnitude > _interactRange)
        {
            InteractPrompt.Hide(this);
            return;
        }

        bool armed = LevelManager.Instance != null && LevelManager.Instance.IsCurrentTarget(this);
        InteractPrompt.Show(this, armed ? "[E]  폭발물 설치" : "길이 막혀 있다.  이 구역의 폭발물이 필요하다");
        if (armed && Input.GetKeyDown(KeyCode.E))
        {
            StartCoroutine(Fuse_Coroutine());
        }
    }

    private IEnumerator Fuse_Coroutine()
    {
        _fusing = true;
        for (int remaining = Mathf.CeilToInt(_fuseTime); remaining > 0; remaining--)
        {
            InteractPrompt.Show(this, "폭파까지  " + remaining);
            SoundManager.Play(SfxId.UiClick);
            yield return new WaitForSeconds(1f);
        }
        InteractPrompt.Hide(this);
        Explode();
    }

    private void Explode()
    {
        if (_explosionEffectPrefab != null)
        {
            ParticleSystem effect = Instantiate(_explosionEffectPrefab, transform.position, Quaternion.identity);
            effect.Play();
        }

        Collider[] colliders = Physics.OverlapSphere(transform.position, _blastRadius, LayerMask.GetMask("Drum", "Zombie"));
        foreach (Collider collider in colliders)
        {
            Vector3 closest = collider.ClosestPoint(transform.position);
            collider.GetComponent<IDamageAble>()?.TakeDamage(new DamageInfo
            {
                Value = _blastDamage,
                HitPoint = closest,
                HitNormal = (transform.position - closest).normalized,
            });
        }

        MarkDone();
        gameObject.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _interactRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _blastRadius);
    }
}
