using UnityEngine;

// 1인칭 무기 모델이 붙는 부모. 발사/휘두를 때 반동을 주고 원래 자리로 돌아온다.
public class WeaponViewModelRoot : MonoBehaviour
{
    [Header("총 반동")]
    [SerializeField] private float _gunKickBack = 0.06f;
    [SerializeField] private float _gunKickPitch = 4f;

    [Header("근접 휘두르기")]
    [SerializeField] private float _meleeSwingRoll = 55f;
    [SerializeField] private float _meleeSwingPitch = 25f;

    [Header("복귀")]
    [SerializeField] private float _recoverSpeed = 14f;

    private Vector3 _positionOffset;
    private Vector3 _rotationOffset;

    public void Kick(WeaponCategory category)
    {
        switch (category)
        {
            case WeaponCategory.Gun:
                _positionOffset = new Vector3(0f, 0f, -_gunKickBack);
                _rotationOffset = new Vector3(-_gunKickPitch, 0f, 0f);
                break;
            case WeaponCategory.Projectile:
                _positionOffset = new Vector3(0f, -0.04f, -0.03f);
                _rotationOffset = new Vector3(-12f, 0f, 0f);
                break;
            case WeaponCategory.Melee:
                _positionOffset = new Vector3(-0.08f, 0f, 0.05f);
                _rotationOffset = new Vector3(_meleeSwingPitch, 0f, _meleeSwingRoll);
                break;
        }
    }

    private void Update()
    {
        float t = 1f - Mathf.Exp(-_recoverSpeed * Time.deltaTime);
        _positionOffset = Vector3.Lerp(_positionOffset, Vector3.zero, t);
        _rotationOffset = Vector3.Lerp(_rotationOffset, Vector3.zero, t);
        transform.localPosition = _positionOffset;
        transform.localRotation = Quaternion.Euler(_rotationOffset);
    }
}
