# 2단계: 포스트 프로세싱(Post Processing) 적용 (2026-11-02)

측정 조건은 조명 단계와 같아요: 에디터 Play 모드, 1920x1080, 게임을 Ready로 고정, 약 9초 평균. 조명은 최종본(Shadowmask 베이크)을 그대로 쓰고, 포스트 프로세싱 구성만 바꿔 가며 측정했어요.

## 기존 설정 (Before)
- Global Volume `SampleSceneProfile`: Tonemapping(Neutral), Bloom(Intensity 0.25, HQ Filtering), Vignette(0.2), Motion Blur(꺼짐)
- PC_Renderer의 **SSAO Renderer Feature**: Source = DepthNormals, Downsample 꺼짐, Samples Medium, Blur High

## 적용한 포스트 프로세싱 (새 프로필 `Assets/Settings/GameScene_VolumeProfile.asset`)
목표는 "좀비가 나오는 버려진 공장" 분위기예요. 차갑고 채도가 낮지만, 그늘 속 디테일은 보여야 해요.

| 효과 | 설정 | 의도 |
|---|---|---|
| Tonemapping | **ACES** (기존 Neutral) | 필름 같은 대비, 하이라이트 롤오프 |
| Color Adjustments | Post Exposure +0.4, Contrast +8, Saturation -25, Color Filter (0.96, 0.99, 1.0) | 베이크 후 어두워진 화면 보정, 채도를 낮춰 음울하게 |
| White Balance | Temperature -8, Tint +4 | 차가운 색감 |
| Shadows Midtones Highlights | Shadows (0.95, 1.0, 1.03, +0.25) | 어두운 영역만 살짝 들어 올리고 푸른 기를 더함 |
| Lift Gamma Gain | Lift (0.98, 1.0, 1.03, +0.015) | 완전한 검정이 뭉개지지 않게 아주 약하게 |
| Bloom | Threshold 1.0, Intensity 0.4, Scatter 0.6 | 하늘과 밝은 금속의 은은한 번짐 |
| Vignette | Intensity 0.3, Smoothness 0.4 | 시선을 화면 중앙(조준점)으로 모음 |
| SSAO (Renderer Feature) | 최적화 후: Source **Depth**, **Downsample 켬**, Samples **Low**, Blur **Medium** | 구조물 접촉부 음영 (아래 비용 분석 참고) |

### 튜닝 과정 (AI와 반복)
1. **1차:** ACES, Contrast +12, Exposure +0.35로 설정했어요. ACES가 어두운 영역을 눌러서 골목 그늘이 **완전히 검게 뭉개졌어요**.
2. **2차:** Contrast를 0으로, Exposure를 +0.6으로, Shadows를 +0.35로 올렸어요. 밝은 곳은 괜찮았지만 그늘은 여전히 너무 어두웠어요.
3. **3차:** Lift Gamma Gain의 Lift를 +0.08로 넣었어요. 이번에는 화면 전체가 **회색으로 뿌옇게 떠서** 실패했어요. Lift는 0.01 단위로 아주 작게 조절해야 한다는 걸 알게 됐어요.
4. **최종:** Lift +0.015, Shadows +0.25, Exposure +0.4, Contrast +8로 정했어요. 전경은 차갑고 거친 분위기가 됐어요. 골목 앞쪽 그늘은 여전히 어두운데, 탱크 바로 아래라 Lightmap 자체가 어두운 곳이에요. 포스트 프로세싱보다는 조명(Light Probe, 간접광) 쪽 문제예요.

## 성능 비교
| 구성 | FPS | Main Thread | Batches | SetPass | Triangles | Texture Mem |
|---|---|---|---|---|---|---|
| ① PP 없음, SSAO 없음 | 129.0 | 7.95 ms | 182 | 44 | 202만 | 445.5 MB |
| ② 기존 프로필 + 기존 SSAO (Before) | 123.8 | 7.93 ms | 283 | 75 | 250만 | 493.8 MB |
| ③ 새 프로필 + 기존 SSAO | 124.9 | 7.89 ms | 283 | 75 | 250만 | 493.8 MB |
| ④ 새 프로필, SSAO 끔 | 127.2 | 7.70 ms | 199 | 61 | 204만 | 450.8 MB |
| ⑤ **새 프로필 + 최적화 SSAO (최종)** | 125.3 | 7.89 ms | 281 | 73 | 250만 | **464.7 MB** |
| ⑥ ⑤에서 Bloom 끔 | 124.4 | 7.89 ms | 281 | 73 | 250만 | 464.7 MB |

### 분석
1. **컬러 그레이딩은 사실상 공짜예요 (② → ③).** Tonemapping, Color Adjustments, White Balance, Shadows/Midtones, Lift Gamma Gain을 여러 개 추가해도 수치 변화가 없어요. URP는 컬러 그레이딩 효과를 모두 **LUT(32x32x32 텍스처) 한 장에 구워서** 마지막 패스에서 한 번만 적용하기 때문이에요. 효과를 몇 개 쌓든 비용이 같아요.
2. **SSAO가 가장 비싼 효과예요 (③ → ④).** SSAO를 켜면 다음이 늘어나요.
   - Batches +84, SetPass +14, 삼각형 +46만
   - 텍스처 메모리 +43 MB
   - 원인: AO를 계산하려면 깊이/노멀 정보가 필요해요. URP는 이를 위해 **씬 전체를 한 번 더 그리는 프리패스**를 돌려요. 그래서 Draw Call과 삼각형이 늘어요. 1080p 해상도의 노멀 텍스처와 AO 텍스처도 추가로 필요해서 메모리가 늘어요.
3. **SSAO 최적화 (③ → ⑤):** Source를 Depth로(노멀을 깊이로부터 재구성), Downsample(절반 해상도), Samples Low, Blur Medium으로 바꿨어요.
   - 텍스처 메모리가 **-29 MB (493.8 → 464.7)** 줄었고, SetPass도 -2 줄었어요. 노멀 텍스처가 없어지고 AO 텍스처가 1/4 크기가 됐기 때문이에요.
   - **하지만 Batches는 그대로예요 (283 → 281).** Depth 소스여도 깊이 프리패스는 여전히 필요해서, 씬을 한 번 더 그리는 비용은 남아요.
   - 화질 비교(`PP/02_new_overview.png`와 `PP/04_new_SSAOopt_overview.png`): 해상도를 절반으로 줄였는데도 이 거리에서는 차이가 거의 안 보여요. **메모리 29MB를 아끼는 대가로 눈에 띄는 손해는 없었어요.**
4. **Bloom, Vignette의 비용은 측정되지 않았어요 (⑤ → ⑥).**
   - 화면 전체 후처리 패스는 Stats의 Batches/SetPass 카운터에 거의 잡히지 않아요.
   - 에디터에서 `GPU Frame Time` 카운터가 0.00ms 샘플을 섞어 내는 등 불안정해서 GPU 시간 차이를 믿을 수 없었어요. 이 PC의 GPU는 프레임당 약 1~1.5ms만 쓰고 있어서 여유도 커요.
   - 정확한 비교는 **빌드 + Profiler의 GPU Usage 모듈**이나 저사양 기기에서 해야 해요. "측정 도구의 한계로 차이를 확인하지 못함"도 결과로 남겨 둘게요.
5. **FPS 변화가 작은 이유:** 조명 단계와 같아요. 에디터 Play 모드에서는 EditorLoop가 프레임의 절반 이상을 차지해 CPU가 병목이에요. PP를 모두 꺼도(①) FPS는 129로 4%밖에 오르지 않아요.

## 결론
- 분위기를 바꾸는 **컬러 그레이딩은 마음껏 써도 돼요** (LUT 한 장).
- 비용 대부분은 **SSAO**에서 나와요. 메모리는 설정만으로 29MB 줄였지만, 깊이 프리패스(Batches +80)는 남아요. 모바일이나 저사양을 목표로 한다면 SSAO를 끄고, 베이크된 AO(Lightmap AO, 이미 적용됨)로 대체하는 게 맞아요.

## 스크린샷 (`Docs/Report/PP/`)
| 파일 | 내용 |
|---|---|
| `00_none_*` | PP와 SSAO 모두 없음 |
| `01_old_*` | 기존 프로필 (Before) |
| `02_new_*` | 새 프로필 + 기존 SSAO |
| `03_new_noSSAO_overview` | 새 프로필, SSAO 끔 (AO 비교용) |
| `04_new_SSAOopt_overview` | 새 프로필 + 최적화 SSAO |
| `05_final_*` | 최종 (게임 화면 / 골목) |

## 변경된 파일
- `Assets/Settings/GameScene_VolumeProfile.asset`: 새 파일. 기존 `SampleSceneProfile`은 비교용으로 남겨 뒀어요.
- `Assets/01.Scenes/GameScene.unity`: Global Volume의 프로필을 새 프로필로 교체
- `Assets/Settings/PC_Renderer.asset`: SSAO 설정 변경 (Source Depth, Downsample, Samples Low, Blur Medium). 나머지 diff는 Unity가 YAML 줄바꿈을 다시 쓴 것으로, 값은 바뀌지 않았어요.
