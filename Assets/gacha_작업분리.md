# 가챠 구현 — 클로드 코드 vs 직접 작업 분리

Claude Code 점검 결과 반영 (URP 확인, 훅 포인트 확인)

---

## 클로드 코드가 할 일

### 스크립트 작성

| 파일 | 내용 |
|---|---|
| `GachaVFXController.cs` | 코루틴 오케스트레이션 전체, 등급별 VFX 프리팹 선택 및 Play() |
| `CardUI.cs` | UnitDefinition 데이터 → 카드 UI 바인딩, 등급별 Color/Sprite 교체 |
| `ResultSummaryUI.cs` | 10회 결과 데이터 수신, 피처 카드 + 3×3 그리드 세팅 |
| `GachaManager.cs` 수정 | `RevealCardsSequentially()` 훅 포인트에 VFXController 연결 |
| `RingEffect.cs` | DOTween Scale + Fade 링 순차 발사 |
| `EdgePulse.cs` | Legendary 전용 테두리 DOFade 펄스 |

### DOTween 애니메이션 코드

```
- 카드 슬라이드인: DOAnchorPosY + Ease.OutBack
- 링 확장: DOScale + DOFade
- 화면 플래시: DOFade
- Shader 글로우 제어: material.DOFloat("_GlowIntensity")
- 소형 카드 순차 등장: Sequence + AppendInterval
```

### Particle System 코드 세팅

등급별 프리팹 수치를 Inspector 대신 코드로 전부 작성.
`ParticleSystem.main`, `emission`, `colorOverLifetime` 등 C#으로 설정.

---

## 직접 해야 할 일

### Unity Editor 작업

**캔버스 설정**
- Canvas Render Mode → Screen Space - Camera 변경
- Particle용 Sorting Layer 추가 및 설정
- Camera 레퍼런스 연결

**씬 프리팹 작업 (Claude Code 점검 결과: 코드보다 큰 부분)**
- ResultSummaryUI 레이아웃 — 피처 카드 + 3×3 그리드 RectTransform 배치
- GachaScene 계층구조 구성 (VFXRoot, CardRoot, UIRoot)
- 카드 프리팹 내 Image/TMP 컴포넌트 배치

**Shader Graph 제작 (노드 연결은 비주얼 에디터에서 직접)**
- 글로우 펄스 Shader (GlowIntensity 파라미터 노출)
- 방사형 광선 Shader (Polar Coordinates + Time 회전)
- 홀로그램 shimmer Shader (Legendary 전용)
- 완성 후 Material 생성 → Image 컴포넌트에 할당

**Inspector 레퍼런스 연결**
- 스크립트의 SerializeField 항목들 드래그 연결
- 등급별 VFX 프리팹 슬롯 연결
- 등급별 Color/Sprite 배열 세팅

### 외부 툴 작업

**Figma**
- 카드 테두리 프레임 9-slice 4종
- NEW 배지, 등급 라벨, 별 아이콘 export
- 카드 배경 체스 그리드 텍스처 export

**Aseprite**
- 기물별 픽셀 아트 일러스트
- 등급별 컬러 팔레트 분리

**ComfyUI (선택)**
- 카드 아트워크 배경 베이스 이미지 생성
- 배경 제거 후 PNG export

---

## 작업 순서 (Claude Code 점검 결과 기준)

```
Step 1  직접   DOTween 패키지 설치
Step 2  코드   GachaVFXController.cs + GachaManager 훅 연결
Step 3  코드   CardUI.cs 작성
Step 4  직접   Canvas Render Mode 변경, Sorting Layer 설정
Step 5  코드   Particle 프리팹 4종 수치 코드 작성
Step 6  직접   Particle 프리팹 Inspector 확인 & 조정
Step 7  직접   Shader Graph 3종 제작
Step 8  코드   Shader 파라미터 DOTween 제어 코드 작성
Step 9  직접   Figma 스프라이트 제작 & import
Step 10 직접   ResultSummaryUI 씬 프리팹 레이아웃 작업
Step 11 코드   ResultSummaryUI.cs 데이터 바인딩
Step 12 직접   Inspector 레퍼런스 전체 연결
Step 13 코드   튜닝 요청 시 수치 조정
```

---

## 한 줄 요약

```
클로드 코드  →  코드로 표현되는 모든 것
직접        →  눈으로 보고 손으로 배치하는 모든 것
```
