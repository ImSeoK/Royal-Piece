# 가챠 UI/VFX 제작 파이프라인 — 툴별 역할 정의

---

## 전체 흐름 요약

```
ComfyUI         →  카드 아트 베이스 이미지 생성
Aseprite        →  기물 픽셀 아트 일러스트 제작
Figma           →  카드 UI 레이아웃 & 스프라이트 export
        ↓
Unity (UI)      →  카드 프리팹 조립 & 레이아웃
Shader Graph    →  동적 글로우 / 테두리 펄스 효과
Particle System →  소환 VFX (파티클, 링, 광선)
DOTween         →  카드 애니메이션 & 타이밍
        ↓
C# 코드         →  전체 흐름 오케스트레이션
```

---

## 1. ComfyUI

**역할: 카드 아트워크 베이스 생성**

카드의 아트워크 영역(기물 실루엣 배경, 분위기 텍스처)을 img2img로 뽑는다.
픽셀 아트 스타일로 가중치 잡아서 Aseprite 작업과 결을 맞춘다.

**작업 내용**
- 등급별 배경 분위기 이미지 생성 (Common: 회색조 / Rare: 차가운 블루 / Epic: 퍼플 / Legendary: 골드)
- Inspyrenet Rembg로 배경 제거 → 투명 PNG export
- 기물 실루엣 베이스 이미지 생성 (Aseprite에서 수정할 초안)

**Unity 적용**
- 카드 아트워크 영역 `Image` 컴포넌트에 Sprite로 할당
- 등급별로 별도 Sprite 에셋 관리

---

## 2. Aseprite

**역할: 기물 픽셀 아트 일러스트 최종 제작**

ComfyUI 초안을 받아 픽셀 아트로 다듬거나, 처음부터 직접 제작.
체스 기물 특성(룩/나이트/비숍/퀸/인챈터)을 캐릭터화한 일러스트.

**작업 내용**
- 기물별 정면 일러스트 (카드 아트워크용, 권장 사이즈 64×96px or 128×192px)
- 등급별 컬러 팔레트 분리
  - Common: 회색/무채색 팔레트
  - Rare: 블루 계열 하이라이트
  - Epic: 퍼플 계열 하이라이트
  - Legendary: 골드/앰버 하이라이트
- 가챠 연출용 기물 실루엣 (VFX 중 등장하는 큰 심볼용, 단색 PNG)

**Unity 적용**
- PNG로 export → Unity Sprite 설정 (Pixels Per Unit 조정)
- 카드 아트워크 Image에 할당
- VFX 중 실루엣은 Particle Texture로도 활용 가능

---

## 3. Figma

**역할: 카드 UI 레이아웃 디자인 & 스프라이트 export**

카드의 모든 정적 디자인 요소를 Figma에서 완성한 뒤 스프라이트로 뽑는다.
Unity에서 레이아웃을 잡지 않고 Figma export 결과를 그대로 붙이는 방식.

**작업 내용**

카드 프레임 (9-slice 스프라이트)
- 등급별 테두리 4종 (Common / Rare / Epic / Legendary)
- 9-slice로 설정해서 카드 크기 변경 시 테두리 깨지지 않도록
- 코너 장식, 내부 이중 라인 포함

UI 아이콘 & 배지
- NEW 뱃지 (등급 색상별 4종)
- 별 아이콘 (★ 채움 / ☆ 비움)
- 등급 라벨 배지 (Common / Rare / Epic / Legendary)
- 구분선 (Divider) 스프라이트

카드 배경 텍스처
- 체스 그리드 패턴 PNG (반투명, 타일링용)
- 아트워크 영역 어두운 오버레이

결과 요약 화면 배경 요소
- 헤더 영역 레이아웃
- 등급 카운트 배지 프레임

**export 설정**
- 모든 스프라이트 @2x PNG export
- 9-slice 스프라이트는 border 수치 메모 후 Unity Sprite Editor에서 적용
- 색상 팔레트를 Figma Variables로 관리 → 등급 변경 시 일괄 교체 가능

**Unity 적용**
- 카드 테두리 → Image (Sliced)
- 배지류 → Image (Simple)
- 배경 텍스처 → Image (Tiled)

---

## 4. Shader Graph (Unity)

**역할: 애니메이션이 필요한 동적 시각 효과**

정적인 건 Figma 스프라이트로 처리하고,
시간에 따라 변하는 효과만 Shader에서 담당.

**작업 내용**

카드 테두리 글로우 펄스
- 소환 직후 테두리가 한 번 밝게 빛났다가 잦아드는 효과
- 파라미터: GlowColor, GlowIntensity, GlowDuration
- DOTween으로 Material Property 제어

방사형 광선 (Epic / Legendary VFX)
- UV → Polar Coordinates → 각도 마스크 → Time으로 회전
- Additive 블렌딩
- 파라미터: RayColor, RotationSpeed, Opacity

등급 홀로그램 shimmer (선택, 카드 상시 효과)
- 카드 표면에 은은하게 흐르는 빛 줄기
- Legendary 카드 전용

**Unity 적용**
- UI에 Shader Graph 적용 시 UI/Unlit 베이스 사용
- Material을 카드 Image 컴포넌트에 할당
- C#에서 `material.SetFloat("_GlowIntensity", value)` 로 제어

---

## 5. Particle System (Unity)

**역할: 소환 VFX 연출**

소환 버튼을 눌렀을 때 재생되는 모든 파티클 효과.
등급별 프리팹 4종으로 관리.

**작업 내용**

파티클 버스트
- 중심에서 방사형으로 터지는 메인 파티클
- Aseprite에서 뽑은 기물 실루엣을 Particle Texture로 일부 적용
- Additive 블렌딩, Color over Lifetime (등급 색 → 흰색 → 투명)

확장 링
- Particle System 또는 DOTween Scale 애니메이션으로 처리
- 등급별 링 개수 차이 (Common 1 → Legendary 4)

방사형 광선 (Shader Graph 방식이 아닌 경우 우회)
- Stretched Billboard 모드
- Start Speed 높게, Start Size Y 크게
- Epic: 10개 / Legendary: 18개

**Unity 적용**
- 등급별 프리팹: `VFX_Common`, `VFX_Rare`, `VFX_Epic`, `VFX_Legendary`
- `GachaVFXController.cs`에서 등급에 맞는 프리팹 `Play()` 호출
- World Space로 캔버스 위에 배치 또는 Screen Space Overlay 위에 Particle Layer 별도 운용

---

## 6. DOTween (Unity)

**역할: 모든 UI 애니메이션 타이밍 제어**

코루틴과 조합해서 연출 흐름을 만드는 핵심 도구.

**작업 내용**

| 애니메이션 | 코드 |
|---|---|
| 카드 슬라이드인 | `DOAnchorPosY(target, 0.75f).SetEase(Ease.OutBack)` |
| 화면 플래시 | `DOFade(0f, 0.25f)` |
| 링 확장 | `DOScale(2.5f, 0.6f).SetEase(Ease.OutCubic)` |
| 링 페이드 | `DOFade(0f, 0.6f)` |
| 테두리 펄스 | `DOFade(0.5f, 0.3f).SetLoops(3, LoopType.Yoyo)` |
| 카드 글로우 | `DOFloat(1f, "_GlowIntensity", 0.5f)` |
| 소형 카드 순차 등장 | `Sequence` + `AppendInterval(0.08f)` |

---

## 7. C# 코드 (Unity)

**역할: 전체 흐름 오케스트레이션 & 데이터 연결**

위 모든 도구의 결과물을 연결하고 게임 로직과 이어주는 역할.

**주요 스크립트**

`GachaManager.cs`
- 확률 계산, 뽑기 결과 반환
- `UnitDefinition` ScriptableObject에서 유닛 데이터 참조

`GachaVFXController.cs`
- 등급별 VFX 프리팹 선택 및 재생
- 코루틴으로 연출 타이밍 제어

`CardUI.cs`
- 뽑기 결과 데이터를 카드 UI에 바인딩
- 등급별 색상/스프라이트 교체

`ResultSummaryUI.cs`
- 10회 결과 요약 화면 구성
- 최고 등급 피처 카드 + 3×3 그리드 세팅

---

## 작업 순서 (권장)

```
1. Figma에서 카드 프레임 & UI 스프라이트 제작 → export
2. Aseprite에서 기물 일러스트 제작
3. Unity에 스프라이트 import, CardUI 프리팹 조립
4. DOTween 카드 슬라이드인 구현
5. Particle System VFX 프리팹 4종 제작
6. Shader Graph 글로우/광선 제작
7. GachaVFXController 코루틴 오케스트레이션
8. 결과 요약 화면 ResultSummaryUI 구현
9. 등급별 연출 차이 튜닝
```
