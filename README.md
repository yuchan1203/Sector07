# SECTOR 07

> SURVIVE THE QUARANTINE ZONE

격리 구역 SECTOR 07에 퍼진 좀비를 상대로 튜토리얼을 마치고, 구역을 정리한 뒤 탈출 지점까지 도달하는 1인칭 슈터입니다.
Unity로 3D 게임 개발을 학습하며 만든 프로젝트를 기획, 구조, 연출 단위로 다듬은 포트폴리오 작품입니다.

## 플레이 흐름

1. **로그인**: 로컬 계정으로 가입하고 로그인합니다. (계정은 이 PC에만 저장됩니다)
2. **튜토리얼 (11단계)**: 이동, 시점, 점프, 질주, 사격, 조준, 재장전, 근접 공격, 훈련용 좀비 처치, 폭탄 투척, 무기 교체를 직접 해 보며 익힙니다. 훈련 좀비를 처치하면 폭탄을 얻습니다.
3. **STAGE 1**: 구역의 좀비를 모두 처치합니다. 강화 좀비가 섞여 있습니다.
4. **STAGE 2**: 탈출 지점으로 이동하면 클리어입니다. 클리어 시간과 처치 수가 결과 화면에 표시됩니다.

무기는 처음부터 모두 주어지지 않습니다. 권총과 검으로 시작해 단계 보상과 바닥의 무기 픽업으로 하나씩 해금합니다.

## 조작

| 입력 | 동작 |
|---|---|
| W A S D | 이동 |
| 마우스 | 시점 |
| Space | 점프 (2단 점프) |
| Shift | 질주 (스태미나 소모) |
| 마우스 좌클릭 | 사격 / 투척 / 휘두르기 |
| 마우스 우클릭 | 조준 |
| R | 재장전 (게임오버, 클리어 화면에서는 다시 시작) |
| F | 근접 공격 |
| C | 무기 교체 (해금한 무기만) |
| P | 일시정지 |

## 무기

| 무기 | 종류 | 특징 |
|---|---|---|
| 권총 | 총 | 기본 무기 |
| 샷건 | 총 | 여러 발이 퍼져 나가는 근거리 무기 |
| 스나이퍼 | 총 | 조준 시 확대 |
| 기관총 | 총 | 빠른 연사 |
| 수류탄 | 투척 | 시간차 폭발, 거리에 따라 대미지 감소 |
| 화염병 | 투척 | 충돌 시 폭발 |
| 검 | 근접 | 탄약 없음, 휘두르는 동작에 맞춘 판정 |

## 구현 내용

- **게임 흐름**: `GameManager`의 상태(Ready, Playing, Pause, GameOver, Clear)와 `LevelManager`의 단계 진행. 단계마다 목표 유형(전멸, 지점 도달, 버티기, 조작 확인)을 설정합니다.
- **무기 시스템**: 무기 수치와 소리, 1인칭 모델을 `WeaponData`(ScriptableObject)로 정의하고, 총(레이캐스트), 투척, 근접을 `WeaponBase` 계열 클래스로 처리합니다. 새 무기는 에셋만 추가하면 됩니다.
- **좀비 AI**: Idle, Trace, Comeback, Attack 상태머신과 `NavMeshAgent` 이동. 플레이어가 감지 범위 안에 있으면 스폰에서 멀어져도 쫓아갑니다. 강화 좀비는 프리팹 베리언트로 체력, 공격력, 크기, 목소리를 다르게 했습니다.
- **피격 계약**: 공격 쪽은 `IDamageAble`만 호출하므로 좀비, 상자, 드럼통, 플레이어가 같은 방식으로 피해를 받습니다.
- **효과음**: `SoundManager`가 `SoundBank`의 `SfxId`별 클립을 오디오 소스 풀로 재생합니다. 위치 소리와 화면 소리를 구분하고, 연사에서 소리가 뭉개지지 않도록 소리별 최소 간격을 둡니다.
- **씬 이동**: `SceneLoader`와 `LoadingScene`을 거쳐 목표 씬을 비동기로 불러옵니다. 시작 화면과 재시작이 같은 로더를 씁니다.
- **UI**: 시작 화면(로그인, 회원가입, 전환 연출), 목표 HUD, 알림, 체력/스태미나/탄약, 미니맵, 클리어 결과 화면.
- **계정**: 비밀번호를 솔트와 SHA-256으로 해시해 `PlayerPrefs`에 저장합니다.
- **기타**: 오브젝트 풀링, 조건부 로깅(`GameLogger`), 발자국 소리, 카메라 반동.

## 기술 스택

- Unity `6000.3.22f1` (Unity 6)
- Universal Render Pipeline 17.3
- AI Navigation, TextMeshPro, Input System 1.20.1 (현재 입력은 레거시 `Input` 사용)
- C#

## 실행 방법

1. Unity Hub에서 Unity `6000.3.22f1`을 설치합니다.
2. 이 저장소를 열고 `Assets/01.Scenes/StartScene.unity`를 실행합니다. (씬 순서: StartScene → LoadingScene → GameScene)
3. 회원가입 후 로그인하면 게임이 시작됩니다.

별도의 빌드, 린트, 테스트 명령은 없습니다. 에디터에서 Play 모드로 실행합니다.

## 프로젝트 구조

```
Assets/
  01.Scenes     StartScene, LoadingScene, GameScene
  02.Scripts    기능별 폴더 (Camera, Core, Manager, Monster, Player, Sound, Weapon, UI_* 등)
  03.Prefabs    좀비, 이펙트 등
  06.Sounds     BGM, 효과음, 발자국
  10.Datas      WeaponData, SoundBank
Docs/
  CodeReviewGuide.md   코드 리뷰 기준
  Report/              그래픽 품질 개선 및 최적화 보고서
```

## 문서

- [그래픽 품질 개선 및 최적화 보고서](Docs/Report/01_Before.md): 측정 기준(Before), 라이팅, 포스트 프로세싱, 최적화 단계별 기록
- [코드 리뷰 가이드](Docs/CodeReviewGuide.md)

## 사용한 외부 에셋

Low Poly Weapons VOL.1, Blink Free Swords, Zombie 모델·애니메이션 팩, Not So Scary Zombie Pack, LowPolySoldiers demo, RPG FPS game assets (industrial), Blood Decals and Effects, Meshy AI로 만든 캐릭터 모델 등을 사용했습니다. 각 에셋의 라이선스는 해당 배포처의 약관을 따릅니다. 효과음은 ElevenLabs로 생성했습니다.
