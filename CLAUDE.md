# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트 개요

3D 게임개발을 학습하기 위한 Unity 프로젝트 (3인칭/FPS 슈터). 좀비를 상대로 총(Raycast)과 폭탄을 사용한다.

- Unity 에디터 버전: `6000.3.22f1` (Unity 6), URP 17.3, Input System 1.20.1, AI Navigation, TextMeshPro
- 빌드/린트/테스트 CLI 명령은 없다. Unity 에디터에서 열어 Play 모드로 실행하며, `Assembly-CSharp.csproj` 등은 에디터가 생성하는 파일이므로 직접 수정하지 않는다.
- 코드 리뷰 시에는 `Docs/CodeReviewGuide.md`의 기준(중요도 분류, SOLID, Unity 확인 항목, 결과 형식)을 따른다. `Docs/GEMINI.md`에 따라 리뷰와 무관한 파일은 수정하지 않고, 요청 없이 커밋하지 않으며, 답변은 한국어로 작성한다.

## 폴더 구조

`Assets/`는 번호 접두사 폴더(`01.Scenes`, `02.Scripts`, `03.Prefabs`, `04.Images`, `05.Models`, `06.Sounds`, `07.Animations`, `08.Fonts`, `09.Materials`, `10.Datas`, `11. Textures`)로 구성된다. 에셋은 이 분류에 맞춰 배치한다. 폴더 이동은 Unity 에디터 안에서 해야 `.meta` GUID 참조가 유지된다. 씬은 `Assets/01.Scenes/`의 `StartScene`(로그인) → `LoadingScene`(로딩) → `GameScene`(플레이) 순서로 이어지며 모두 Build Settings에 들어 있다.

스크립트는 `Assets/02.Scripts/` 아래 기능별 폴더(Camera, Core, Debug, Environment, Level, Manager, Monster, Player, Sound, UI_Damage, UI_Loading, UI_Minimap, UI_Objective, UI_PlayerStats, UI_Start, VFX, Weapon)로 나뉜다. 어셈블리 정의(asmdef)는 없고 모두 `Assembly-CSharp`에 속한다.

## 아키텍처

- **게임 상태 흐름**: `GameManager`(씬 단위 싱글톤)가 `GameState`(Ready → Playing → Pause/GameOver/Clear)를 보유한다. 시작 시 코루틴으로 "준비/시작" 연출 후 Playing으로 전환한다. 플레이어/좀비의 `Update`는 `GameManager.Instance.GameState != GameState.Playing`이면 즉시 return하는 가드를 공통으로 사용하므로, 새 행동 스크립트에도 같은 가드를 넣는다.
- **데미지 계약**: `Core/IDamageAble.TakeDamage(DamageInfo)`와 `DamageInfo`(Value, HitPoint, HitNormal) 구조체. 총(`HitscanWeapon`), 근접(`MeleeWeapon`), 폭탄(`BombProjectile`) 등 공격 쪽은 대상의 구체 클래스를 모르고 `IDamageAble`만 호출한다. `Zombie`, `Box`, `Drum`, `PlayerHit` 등이 이를 구현한다. 새 피격 대상은 이 인터페이스를 구현한다.
- **좀비 AI**: `Zombie`가 `ZombieState`(Idle/Trace/Comeback/Attack) 열거형 기반의 switch 상태머신으로 동작하며 `CharacterController`로 이동한다. 플레이어는 `Player` 태그로 `Start`에서 찾는다. 체력바는 `ZombieHealthBar`가 별도 컴포넌트로 표시한다.
- **UI**: `UI_PlayerStats/PlayerHealthBar`는 `PlayerHit.HealthChanged`, `PlayerMove.StaminaChanged` 이벤트로 체력바와 스태미나바를 갱신하고, 총알 텍스트는 `WeaponSwitcher`가 `CurrentAmmo`/`CurrentMaxAmmo`로 갱신한다. `UI_Minimap`은 `MinimapCamera`와 함께 동작한다.
- **구역 진행**: `LevelManager`의 `StageInfo` 목록이 튜토리얼과 구역(Zone) 단계를 순서대로 진행한다. 구역마다 ① `KillAll`(좀비 전멸) → ② `Collect`(폭발물 `ExplosivePickup` 획득) → ③ `DestroyBarrier`(막힌 길 `ZoneBarrier`을 [E]로 설치해 폭파) 단계를 둔다. 폭발물과 장애물은 `ObjectiveTarget`을 상속하고, 좀비 그룹에 붙인 `ZombieSpawner`가 그룹이 켜질 때 좀비를 만든다. 단계 시작 때 `_briefing`은 `RadioUI` 무전 자막으로, 가까이 가면 뜨는 안내는 `InteractPrompt`로 표시한다.
- **씬 이동**: 씬을 바꿀 때는 `SceneManager.LoadScene`을 직접 부르지 않고 `SceneLoader.Load(씬 이름, 문구, 최소 대기 시간)`을 쓴다. 가벼운 `LoadingScene`을 먼저 열어 이전 씬을 내린 뒤 `LoadingUI`가 목표 씬을 비동기로 불러온다. 새 씬은 Build Settings에 추가해야 한다.
- **효과음**: `SoundManager`(씬마다 하나)가 `SoundBank` 에셋(`Assets/10.Datas/SoundBank.asset`)의 `SfxId`별 클립을 재생한다. 코드에서는 `SoundManager.Play(SfxId)`(화면 소리), `SoundManager.PlayAt(SfxId, 위치)`(위치 소리)만 호출하며, 씬에 매니저가 없으면 조용히 무시된다. 무기별 소리는 `WeaponData`의 `SfxId` 필드로 정한다. 새 소리는 `SfxId` 맨 아래에 추가하고 `SoundBank`에 연결한다.
- **로깅**: `GameLogger.Log`는 `[Conditional("ENABLE_LOG")]`이므로 스크립팅 심볼 `ENABLE_LOG`가 정의된 경우에만 호출이 컴파일된다. Warning/Error는 항상 출력된다. `Debug.Log` 대신 `GameLogger`를 사용한다.
- **입력**: 프로젝트에 `InputSystem_Actions.inputactions`가 있지만, 현재 스크립트(예: `WeaponSwitcher`, `PlayerMove`)는 레거시 `Input.GetKeyDown`/`GetButton`을 사용한다. 기존 코드 스타일에 맞춰 작성하되, 입력 방식을 바꿀 때는 Player Settings의 Active Input Handling 설정을 확인한다.

## 코드 컨벤션 (기존 코드 기준)

- 인스펙터 노출 필드는 `[SerializeField] private _camelCase`, `[Header("스탯")]`처럼 한국어 헤더를 사용한다. 주석과 UI 문자열도 한국어다.
- 외부에는 읽기 전용 프로퍼티(`public float Health => _health;`)만 노출한다.
- 커밋 메시지는 `이모지 타입: 한국어 설명` 형식 (예: `✨ feat: 조준선 추가`, `🎨 asset: 폴더 위치 변경`, `🔧 chore: 기타 수정`).
