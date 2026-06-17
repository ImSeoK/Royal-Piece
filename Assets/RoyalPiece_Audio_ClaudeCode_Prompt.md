# Royal Piece 오디오 시스템 구현 — Claude Code 작업 지시서

---

## 프로젝트 개요

- **게임명**: Royal Piece (전투체스)
- **장르**: 모바일 가챠 / 덱빌더 전략 게임
- **엔진**: Unity (C#)
- **네임스페이스**: `Chess.Presentation`
- **목표**: 오디오 시스템 전체 구축 (구조 → UI → BGM → 가챠 → 덱빌더 → 전투)

---

## 전체 작업 로드맵

| 단계 | 내용 | 상태 |
|------|------|------|
| Step 1 | AudioManager 구조 확장 | ✅ 완료 |
| Step 2 | AudioDatabase ScriptableObject | ✅ 완료 |
| Step 3 | UI 사운드 연결 | 👉 현재 작업 |
| Step 4 | BGM 전체 (로비 + 전투 + 결과 스팅) + 볼륨 설정 UI | 대기 |
| Step 5 | 가챠 사운드 | 대기 |
| Step 6 | 덱빌더 / 인벤토리 + 유닛 배치음 | 대기 |
| Step 7 | 전투 인게임 SFX (공격 / 피격 / 스킬) | 대기 |

---

## 완료된 작업 내용

### AudioManager.cs (Step 1)
기존 BGM 단일 소스 구조에서 다음으로 확장 완료:
- **BGM 채널**: `bgmSourceA` + `bgmSourceB` 두 개로 크로스페이드 지원
- **SFX 풀**: `AudioSource` 8개 코드로 자동 생성, 유휴 소스 우선 탐색 + 라운드로빈 fallback
- **UI 채널**: `uiSource` 별도 AudioSource (볼륨 덕킹 영향 없음)
- **볼륨 4채널**: master / bgm / sfx / ui — PlayerPrefs로 저장/로드
- **Public API**:
  - `Play(string id)` — AudioDatabase에서 id로 조회 후 자동 재생 (핵심 호출 방식)
  - `PlaySFX(AudioClip, float volumeScale)`
  - `PlaySFXWithPitch(AudioClip, float variance, float volumeScale)`
  - `PlayUI(AudioClip)`
  - `PlayBGM(AudioClip)` — 크로스페이드
  - `SetMasterVolume / SetBGMVolume / SetSFXVolume / SetUIVolume`

### AudioClipData.cs (Step 2)
```csharp
// ScriptableObject — 개별 사운드 정의
public class AudioClipData : ScriptableObject
{
    public string id;              // 예: "ui_button_tap"
    public AudioType type;         // SFX / UI / BGM
    public AudioClip clip;
    public float volumeScale;
    public float pitchVariance;    // 0이면 피치 고정
}
```

### AudioDatabase.cs (Step 2)
```csharp
// ScriptableObject — 전체 클립 등록 테이블
public class AudioDatabase : ScriptableObject
{
    public List<AudioClipData> clips;
    public void Initialize();      // Awake에서 Dictionary 빌드
    public AudioClipData Get(string id);
}
```

---

## 현재 작업: Step 3 — UI 사운드 연결

### 목표
버튼 탭, 팝업 열기/닫기 등 UI 인터랙션에 사운드를 붙이는 공통 컴포넌트를 만든다.
애셋(AudioClip)은 아직 없으므로 **슬롯만 준비**하고, 나중에 채울 수 있는 구조로 작성한다.

### 만들어야 할 것

#### 1. `UIButtonSound.cs`
버튼 오브젝트에 붙이는 컴포넌트.
- `AudioClipData` 슬롯을 인스펙터에서 직접 연결하는 방식과
- id 문자열로 `AudioManager.Instance.Play(id)` 호출하는 방식 둘 다 지원
- `OnClick()` public 메서드 → Unity Button 이벤트에 연결

```csharp
// 사용 예시
public class UIButtonSound : MonoBehaviour
{
    public string soundId; // "ui_button_tap"
    public void OnClick() => AudioManager.Instance.Play(soundId);
}
```

#### 2. `UIPopupSound.cs`
팝업 오브젝트에 붙이는 컴포넌트.
- `openSoundId`, `closeSoundId` 두 슬롯
- `OnOpen()`, `OnClose()` public 메서드
- 팝업 애니메이션 시작 시점에 호출되도록 설계

```csharp
public class UIPopupSound : MonoBehaviour
{
    public string openSoundId;   // "ui_popup_open"
    public string closeSoundId;  // "ui_popup_close"
    public void OnOpen()  => AudioManager.Instance.Play(openSoundId);
    public void OnClose() => AudioManager.Instance.Play(closeSoundId);
}
```

#### 3. AudioClipData 에디터 생성 가이드 (주석 또는 README)
Step 3에서 사용할 id 목록:

| id | type | 용도 |
|----|------|------|
| `ui_button_tap` | UI | 일반 버튼 |
| `ui_button_confirm` | UI | 확인/완료 버튼 |
| `ui_button_cancel` | UI | 취소/닫기 버튼 |
| `ui_popup_open` | UI | 팝업 열기 |
| `ui_popup_close` | UI | 팝업 닫기 |
| `ui_tab_switch` | UI | 탭 전환 |
| `ui_unlock` | UI | 잠금 해제 알림 |
| `ui_error` | UI | 에러 알림 |

### 작업 범위 요약
- `UIButtonSound.cs` 작성
- `UIPopupSound.cs` 작성
- 위 두 파일은 `Chess.Presentation` 네임스페이스 사용
- AudioManager, AudioDatabase, AudioClipData는 이미 존재하므로 건드리지 않음
- 실제 AudioClip 연결은 에디터 작업이므로 코드에서는 null-safe 처리 필수

---

## 코드 작성 규칙

- 네임스페이스: `Chess.Presentation`
- null 체크 필수 (`AudioManager.Instance`가 없을 경우 방어 처리)
- `Debug.Log`는 `#if UNITY_EDITOR` 블록 안에서만 사용
- 불필요한 Update 루프 사용 금지
- SerializeField 또는 public 필드로 인스펙터 노출

---

## 참고: 전체 사운드 ID 목록 (향후 단계 포함)

### UI (Step 3)
`ui_button_tap`, `ui_button_confirm`, `ui_button_cancel`,
`ui_popup_open`, `ui_popup_close`, `ui_tab_switch`,
`ui_unlock`, `ui_error`

### BGM (Step 4)
`bgm_lobby`, `bgm_battle`, `bgm_gacha`, `sting_victory`, `sting_defeat`

### 가챠 (Step 5)
`sfx_gacha_start`, `sfx_gacha_grade_n`, `sfx_gacha_grade_r`,
`sfx_gacha_grade_sr`, `sfx_gacha_grade_ssr`, `sfx_gacha_fanfare`

### 덱빌더 / 인벤토리 (Step 6)
`sfx_unit_equip`, `sfx_unit_unequip`, `sfx_enhance_start`,
`sfx_enhance_complete`, `sfx_skill_select`, `sfx_unit_place`

### 전투 인게임 (Step 7)
`sfx_battle_start`, `sfx_attack_melee`, `sfx_attack_ranged`,
`sfx_attack_magic`, `sfx_hit_light`, `sfx_hit_heavy`,
`sfx_unit_death`, `sfx_skill_activate`, `sfx_round_win`, `sfx_round_lose`
