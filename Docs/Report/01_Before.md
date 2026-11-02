# 0단계: Before 기준 측정 (2026-11-02)

## 측정 조건 (이후 모든 단계에서 동일하게 유지)
- 측정 환경: Unity 6000.3.22f1 에디터, Play 모드, Game View 1920x1080
- 품질 레벨: PC (`PC_RPAsset`), VSync 꺼짐(vSyncCount 0), Target Frame Rate 제한 없음
- 시점: 게임 시작 직후 플레이어 스폰 위치(1인칭), 입력 없음, 좀비 2마리
- 방법: GameState가 Playing이 된 뒤 `ProfilerRecorder`로 약 8초(420프레임 샘플) 동안 평균, 최소, 최대값 기록
- 주의: 에디터 측정값이라 메모리는 에디터 자체 사용량이 포함되어 실제 빌드보다 크게 나와요. 그래서 전후 **비교용**으로만 사용해요.

## 현재 설정 상태
| 항목 | 값 |
|---|---|
| URP | HDR 켜짐, MSAA 1x, Render Scale 1, SRP Batcher 켜짐 |
| 그림자 | Shadow Distance 50, Cascade 4, Main Shadow 해상도 2048, Soft Shadow |
| 조명 | **Directional Light 2개가 모두 켜져 있음** (Realtime 강도 2 + Mixed 강도 1.5, 둘 다 Soft Shadow) |
| GI | Lightmap 0개, Light Probe 0개, Reflection Probe 0개, Ambient = Skybox (Default-Skybox), Fog 꺼짐 |
| 포스트 프로세싱 | Global Volume(`SampleSceneProfile`): Tonemapping, Bloom, Vignette (Motion Blur 꺼짐) |
| 카메라 | Main Camera(PP 켜짐, Far 1000) + **MinimapCamera(RenderTexture, Far 1000, 씬 전체를 한 번 더 렌더링)** |
| 씬 | Renderer 446개 (Static 402개), MeshFilter 기준 폴리곤 약 6.6만 개 |

## 성능 측정 결과
| 지표 | 평균 | 최소 | 최대 |
|---|---|---|---|
| FPS | **122.4** | | |
| Main Thread (CPU) | **7.87 ms** | 7.16 | 10.90 |
| GPU Frame Time | 약 1.39 ms (단일 샘플) | | |
| Batches | **559** | 534 | 576 |
| SetPass Calls | **99** | 96 | 99 |
| Draw Calls | 560 | 539 | 575 |
| Triangles | **255.7만** | 233.6만 | 257.6만 |
| Shadow Casters | **347** | 315 | 359 |
| GC Alloc / 프레임 | 0.02 MB | 0.02 | 0.09 |
| Gfx Used Memory | 525.7 MB | | |
| Texture Memory | 468.4 MB | | |
| Mesh Memory | 40.6 MB | | |

### 메모리를 많이 쓰는 텍스처 (상위)
| 크기 | 에셋 |
|---|---|
| 32.0 MB | `TextMesh Pro/Fonts/BMKkubulim_2.asset` (4096x4096 Alpha8) |
| 32.0 MB | `TextMesh Pro/Fonts/펀플로 생존자_6.asset` (4096x4096 Alpha8) |
| 10.7 MB | Meshy 캐릭터 normal 맵 (2048, DXT5) |
| 10.7 MB | `JC_LP_MegaCity/Textures/T_MegaCity_01.png` (2048, BC7) |
| 10.7 MB x2 | `Blink/.../Sword1` Metallic / Normal (2048, DXT5): 씬에서 실제로 쓰이는지 확인 필요 |
| 8.0 MB | `TextMesh Pro/Fonts/BMJUA_2.asset` (2048 Alpha8) |
| 5.4 MB 다수 | 공장 에셋 tga (2048, DXT1) |

### 고정 조건 재측정 (이후 비교 기준)
게임을 Ready 상태로 고정해 좀비와 플레이어가 움직이지 않게 하고 다시 측정했어요. 이후 단계는 이 값과 비교해요. 이유는 `02_Lighting.md`에 정리했어요.

| FPS | Main Thread | GPU | Batches | SetPass | Triangles | Shadow Casters | Texture Mem | Mesh Mem |
|---|---|---|---|---|---|---|---|---|
| 119.6 | 8.46 ms | 1.52 ms | 528 | 86 | 255만 | 329 | 484.3 MB | 40.6 MB |

## 스크린샷
- `Before/01_game_spawn.png`: 스폰 지점 게임 화면 (HUD 포함)
- `Before/02_minimap_camera.png`: 미니맵 카메라가 렌더링하는 화면
- `Before/03_stats_*.png`: Game 뷰 Stats 창 (직접 캡처)
- `Before/04_game_frozen.png`: 고정 조건 게임 화면
- `Before/06_view_overview.png`, `Before/07_view_street.png`: 고정 위치 비교 카메라 (전경 / 골목)

## 관찰 및 개선 후보 (다음 단계에서 검증)
1. **조명 중복**: Directional Light 2개가 둘 다 그림자를 그려요. 그림자 패스가 2배로 들어가고 화면이 과하게 밝아져요. 1개(Mixed)로 정리하고 Lightmap 베이크를 하면 조명 단계의 첫 비교 포인트가 돼요.
2. **삼각형 255만 개**: MeshFilter 기준 씬 폴리곤은 6.6만 개인데 실제 렌더링되는 삼각형은 255만 개예요. 메인 카메라, 미니맵 카메라, 그림자 Cascade 4개, 그림자를 그리는 라이트 2개가 겹쳐서 같은 메시를 여러 번 그리기 때문이에요. Skinned Mesh(좀비, 캐릭터)는 이 계산에서 빠져 있어요. 그래서 GPU 최적화 효과가 가장 클 것 같은 영역이에요.
3. **미니맵 카메라**: Far 1000으로 씬 전체를 렌더링해요. Culling Mask, Far, 그림자/PP 끄기, 해상도 낮추기, 갱신 주기 조절을 후보로 둘게요.
4. **Batches 559 / SetPass 99**: Static 오브젝트가 402개라 Static Batching 효과를 확인해 볼 만해요. Occlusion Culling도 아직 베이크하지 않았어요.
5. **CPU**: Main Thread 7.9ms이고 GC Alloc도 프레임당 약 20KB로 낮아요. 평상시에는 병목이 아니에요. 오브젝트 풀링 효과는 **사격, 좀비 피격 중**에 측정해야 차이가 드러나요(GC 스파이크).
6. **RAM**: TMP 폰트 아틀라스 4096x4096이 2개(64MB)예요. 아틀라스 크기를 줄이거나 Dynamic으로 바꾸고, 2048 텍스처는 Max Size를 조정하는 게 효과가 크고 증명하기 쉬워요.
