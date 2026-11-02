# 1단계: 조명(Lighting) 적용 (2026-11-02)

## 측정 방법 변경: "고정 조건" 측정
처음 Before 측정(`01_Before.md`)은 게임이 시작된 뒤 그대로 측정했어요. 그런데 조명을 적용한 뒤 같은 방식으로 재 보니 **측정 중에 좀비가 플레이어를 공격**해서 핏자국, 데미지 텍스트, 화면 플래시가 섞여 들어갔어요. 이 때문에 Before와 조건이 달라졌어요.

그래서 측정 방법을 아래처럼 바꾸고, **Before도 원래 씬(git HEAD)으로 되돌려 다시 측정**했어요.
- Play 직후 `GameManager`의 시작 카운트다운 코루틴을 멈춰서 `GameState.Ready`로 고정했어요. 플레이어와 좀비의 `Update` 가드가 동작해서 모든 오브젝트가 정지하고, 렌더링만 계속돼요.
- 같은 시점(스폰 위치)에서 `ProfilerRecorder`로 약 8~15초 동안 평균을 냈어요.
- 화면 비교용으로 고정 위치 카메라 2개를 임시로 만들어 캡처했어요. `CompareCam1`은 공장 전경을 내려다보는 시점, `CompareCam2`는 골목 지면 높이 시점이에요.
- 교훈: **같은 조건으로 측정하는 것**이 생각보다 어려워요. 움직이는 요소(AI, 이펙트)를 통제해야 수치를 비교할 수 있어요.

## 적용한 조명 기술
| 구분 | Before | After |
|---|---|---|
| Directional Light | 2개 (기본 씬 라이트 Realtime 강도 2 + 맵 에셋 라이트 Mixed 강도 1.5) | 맵 라이트 1개 (Mixed). 기본 라이트는 비활성화 |
| Lighting Settings | 없음 (씬에 할당 안 됨) | `GameScene_LightingSettings.lighting` 생성 |
| Global Illumination | Realtime만 사용 (Lightmap 0장) | **Baked GI** (Progressive GPU, 해상도 6 texel/unit, Max 2048, Non-Directional, AO, Bounce 3, Indirect 1.5) |
| Mixed Lighting 모드 | - | **Shadowmask** (Quality 설정: Distance Shadowmask에서 Shadowmask로 변경) |
| Light Probe | 없음 | **Light Probe Group 612개** (NavMesh 위 8m 격자, 지면 +1m / +3.5m 두 층). 좀비와 플레이어 같은 동적 오브젝트가 간접광을 받아요 |
| Reflection Probe | 없음 | Baked Reflection Probe 1개 (맵 전체 박스, 128px) |

결과물은 Lightmap 2장(1024, 512)과 Shadowmask 2장으로 총 **10.1 MB**예요.

## 성능 비교 (고정 조건, 에디터 Play 모드)
| 지표 | Before | 조명 v1 (Distance Shadowmask) | 조명 v2 최종 (Shadowmask) | 변화 (Before→v2) |
|---|---|---|---|---|
| FPS | 119.6 | 119.4 | 121.3 | +1.4% |
| CPU Main Thread | 8.46 ms | 8.36 ms | 8.42 ms | 거의 같음 |
| GPU Frame | 1.52 ms | 1.43 ms | 1.34 ms | -12% (단일 샘플) |
| **Batches** | **528** | 565 | **282** | **-47%** |
| **SetPass Calls** | **86** | 93 | **75** | **-13%** |
| **Shadow Casters** | **329** | 367 | **80** | **-76%** |
| Triangles | 255만 | 253만 | 250만 | -2% |
| Texture Memory | 484.3 MB | 494.3 MB | 494.5 MB | +10.2 MB (Lightmap) |
| Mesh Memory | 40.6 MB | 44.5 MB | 44.5 MB | +4 MB |

### 분석
1. **v1에서는 오히려 나빠졌어요 (Batches +7%, 그림자 캐스터 +12%).**
   - Lightmap만 굽고 Quality 설정이 **Distance Shadowmask**로 남아 있었어요. 이 모드에서는 Shadow Distance(50m) 안의 Static 오브젝트도 계속 실시간 그림자를 그려요. 베이크한 그림자는 50m 밖에서만 쓰이기 때문에 성능 이득이 없었어요.
   - 메인 라이트가 다른 각도의 맵 라이트로 바뀌면서 그림자 범위 안에 들어오는 오브젝트도 늘었어요.
   - Lightmap 인덱스가 서로 다른 오브젝트는 같은 배치로 묶이지 않아서 Batches도 늘었어요.
   - 결론: **"베이크만 하면 빨라진다"는 틀린 생각**이었어요. Mixed Lighting 모드까지 맞춰야 효과가 나요.
2. **v2 (Shadowmask)에서 효과가 크게 나타났어요.** Static 오브젝트 그림자는 Shadowmask 텍스처(베이크)로 처리되고, 실시간 그림자는 좀비, 플레이어, 드럼통 같은 동적 오브젝트만 그려요.
   - 그림자 캐스터가 329에서 80으로 줄었어요.
   - 그림자 패스의 Draw Call이 사라져서 Batches가 거의 절반으로 줄었어요.
   - 대가는 두 가지예요. Lightmap/Shadowmask 텍스처 메모리가 약 10MB 늘었고, Static 오브젝트 그림자는 이제 움직이지 않아요(시간대 변화 불가).
3. **FPS는 거의 변하지 않았어요.** 에디터 Play 모드에서는 Main Thread 8ms 중 상당 부분이 에디터 자체 오버헤드라 CPU가 병목이고, GPU는 1.3~1.5ms로 여유가 있어요. 그래서 GPU 부하를 줄여도 FPS에 바로 반영되지 않아요. 저사양 GPU나 모바일 같은 목표 기기, 또는 빌드에서는 Batches/SetPass 감소 효과가 더 크게 나타날 거예요.
   - Profiler 증거 (`Light/v2_08_profiler_highlights.png`, `Light/v2_09_profiler_timeline.png`):
     - Highlights의 평균 시간은 Others(에디터) 7.66ms, Scripts 1.82ms, Rendering 0.57ms예요.
     - Timeline의 한 프레임(약 7.5ms) 중 **EditorLoop가 4.52ms**이고, 게임 자체인 PlayerLoop는 1.57ms예요.
     - GPU 병목 프레임은 0%예요. 즉 에디터 측정 FPS는 게임 렌더링 비용보다 에디터 비용에 좌우돼요. 정확한 FPS 비교는 빌드에서 해야 해요.
     - 첫 프레임 최대 1075ms는 씬 로딩(JIT 컴파일, TMP 폰트 로딩 1.8MB)이에요. 그래서 측정에서 제외해야 해요.
4. **Triangles 255만 개는 그대로예요.** 확인해 보니 플레이어 모델(`Player/Model_Meshy/Mesh_0`)이 **삼각형 22만 개짜리 Skinned Mesh**였어요. 1인칭인데도 메인 카메라, 미니맵 카메라, 그림자 패스에서 모두 그려져요. 조명 문제가 아니므로 GPU 최적화 단계에서 다룰게요.

## 화면 비교
| 시점 | Before | v1 | v2 (최종) |
|---|---|---|---|
| 전경 | `Before/06_view_overview.png` | `Light/v1_06_view_overview.png` | `Light/v2_06_view_overview.png` |
| 골목 | `Before/07_view_street.png` | `Light/v1_07_view_street.png` | `Light/v2_07_view_street.png` |
| 게임 화면 | `Before/04_game_frozen.png` | `Light/v1_04_game_frozen.png` | `Light/v2_04_game_frozen.png` |

- **좋아진 점**
  - 건물 사이와 구조물 아래에 베이크된 AO와 간접광 그림자가 생겨서 입체감이 커졌어요.
  - Before에서는 Directional Light 2개가 겹쳐 전체가 평평하고 밝게 보였는데, 이 문제가 사라졌어요.
- **아쉬운 점**
  - 골목 시점에서 그늘진 곳이 많이 어두워요. 베이크 전에는 라이트 2개와 Skybox 실시간 Ambient가 그늘을 밝혀주고 있었어요.
  - Indirect Intensity를 1.0에서 1.5로 올려 다시 베이크했지만(v2) 개선은 조금뿐이었어요.
  - 노출과 톤 보정은 다음 단계(포스트 프로세싱: Color Adjustments의 Post Exposure, Tonemapping)에서 맞출 예정이에요.

## AI 활용 과정
- 처음에는 "Directional Light 정리, Lightmap 베이크, Light Probe 배치" 순서로 진행했어요.
- v1 측정에서 수치가 나빠지자 AI와 원인을 분석했어요. Distance Shadowmask와 Shadowmask의 차이를 확인했고, Shadowmask로 바꾸는 튜닝을 적용했어요.
- Light Probe 배치는 AI가 NavMesh 삼각형 정보로 격자를 만들어 자동 배치했어요. 손으로 찍는 것보다 고르고, 걸을 수 있는 곳에만 배치돼요.

## 변경된 파일
- `Assets/01.Scenes/GameScene.unity`: 기본 Directional Light 비활성화, `Lighting/LightProbeGroup`, `Lighting/ReflectionProbe` 추가, Lighting Settings와 Lighting Data 연결
- `Assets/01.Scenes/GameScene_LightingSettings.lighting`: 새 파일
- `Assets/01.Scenes/GameScene/`: Lightmap, Shadowmask, Reflection Probe 베이크 결과 (새 파일)
- `ProjectSettings/QualitySettings.asset`: PC 레벨 shadowmaskMode를 Distance Shadowmask에서 Shadowmask로 변경
