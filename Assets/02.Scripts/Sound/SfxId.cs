// 게임에서 쓰는 효과음 종류. 실제 소리 파일은 SoundBank 에셋에서 이 값에 연결한다.
// 값 순서를 바꾸면 WeaponData 등에 저장된 값이 어긋나므로 새 항목은 맨 아래에 추가한다.
public enum SfxId
{
    None,

    // 총
    Pistol,
    Shotgun,
    Sniper,
    MachineGun,
    Reload,
    EmptyGun,

    // 근접, 투척
    SwordSwing,
    SwordHit,
    GrenadeThrow,
    Molotov,

    // 좀비
    ZombieGrowl,
    ZombieAttack,
    ZombieHit,
    ZombieDeath,
    GiantZombieRoar,

    // 플레이어
    PlayerHit,
    PlayerDeath,
    PlayerLand,

    // 게임 흐름, UI
    GetWeapon,
    NewObjective,
    TutorialComplete,
    GameClear,
    GameOver,
    UiClick,
    LoginSuccess,
}
