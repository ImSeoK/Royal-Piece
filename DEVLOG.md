# Royal Piece — 개발 작업 내역

> Unity C# / Photon Fusion / Firebase
> 레포: ImSeoK/Royal-Piece

---

## 목차

1. [프로젝트 개요](#프로젝트-개요)
2. [기술 스택](#기술-스택)
3. [시스템별 작업 내역](#시스템별-작업-내역)
4. [온라인 멀티플레이어 구축](#온라인-멀티플레이어-구축)
5. [버그 수정 이력](#버그-수정-이력)
6. [미완료 항목](#미완료-항목)

---

## 프로젝트 개요

체스 기물 기반의 자동 전투 전략 게임. 플레이어가 기물(유닛)을 배치하고 이동시키면 전투는 게임 로직이 자동 처리. 가챠로 유닛을 수집하고 덱을 구성해 AI 또는 온라인 PvP로 대전.

### 게임 모드
| 모드 | 설명 |
|---|---|
| AI | AI 대전, 랭크 없음 |
| 일반 | 온라인 PvP (구현 중) |
| 랭크 | 온라인 PvP + 랭크포인트 |

### 랭크 구조
- 티어: D3 ~ S (D3/D2/D1/C/B/A/S)
- 포인트: 승 +15 / 패 -8
- 뱃지 색상: S `#C9913A` / A `#B57BFF` / B `#5AADFF` / C `#888888` / D `#666666`

### 보상 구조
- 골드: 승 +40 / 패 +15 / 항복 패 +15
- 가챠: 1뽑 90골드 / 10뽑 900골드
- 데일리: 접속 +100골드 / 3승 +150골드

---

## 기술 스택

| 분류 | 내용 |
|---|---|
| 엔진 | Unity |
| 언어 | C# |
| 네트워크 | Photon Fusion (릴레이) |
| 인증/데이터 | Firebase Auth + Firestore |
| 결과 검증 | Firebase Cloud Functions (예정) |
| 로컬 테스트 | ParrelSync (에디터 2개 동시 실행) |
| 저장 | PlayerPrefs (로컬 폴백) + Firestore (서버) |

---

## 시스템별 작업 내역

---

### 1. 코어 게임 로직

#### 유닛 시스템
- `UnitDefinition.cs` — 유닛 스탯 정의 (ScriptableObject)
- `UnitState.cs` — 전투 중 실시간 상태 (HP, 버프, 위치 등)
- `MovementResolver.cs` — 이동 가능 범위 계산
- `AttackResolver.cs` — 공격 대상 계산

#### 스킬 시스템
- `SkillDefinition.cs` — 스킬 데이터 정의 (ScriptableObject)
- `SkillExecutor.cs` — 스킬 실행 로직

**스킬 트리거 종류**

| 트리거 | 발동 조건 | 대상 범위 |
|---|---|---|
| OnAttack | 이 유닛이 공격할 때 | 해당 유닛만 |
| OnDamaged | 이 유닛이 피해를 받을 때 | 해당 유닛만 |
| OnMove | 이 유닛이 이동할 때 | 해당 유닛만 |
| OnAdjacent | 적이 인접했을 때 | 해당 유닛만 |
| OnDeath | 이 유닛이 죽을 때 | 해당 유닛만 |
| OnTurnStart | 턴 시작 | 해당 플레이어의 살아있는 전체 유닛 |
| OnTurnEnd | 턴 종료 | 해당 플레이어의 살아있는 전체 유닛 |
| Always | 상시 (계산 전 반영) | VFX 없음, 스탯 계산만 |

**SkillDefinition 주요 필드 구분**

| 필드 | 역할 |
|---|---|
| `targetType` | 플레이어가 클릭해서 고르는지 여부 (액티브/OnAttack 수동) |
| `effectType` | 실제 효과 종류 (Damage/Heal/Buff/Debuff/FixedDamage 등) |
| `auraRange` | 패시브 실질 타겟 범위 (Self/Adjacent/AllAllies/SameRow/AttackRange 등) |

**auraRange 종류**

| 값 | 범위 |
|---|---|
| Self | 자기 자신 |
| Adjacent | 8방향 인접 |
| AllAllies | 같은 팀 전체 |
| SameRow | 같은 행 |
| LineOfSight | 공격 직선 경로 |
| Attacker | OnDamaged 전용 — 공격한 적 |
| AttackRange | 이 유닛의 실제 공격 범위 안의 적 |

#### Enchanter 클래스 추가
- 이동 패턴: `(±1,0),(0,±1),(±2,±2)`
- 공격 범위 버그 수정: `(±2,0),(0,±2)` → `(±1,0),(0,±1)`
- 강화 수치 소수점 지원

---

### 2. 스킬 어셋 (ScriptableObject)

#### Stage 2/3 스킬 시스템
- 기물별 액티브/패시브 2단계 스킬 구조 도입
- 기물 스킬 어셋 전체 추가 (Stage 2/3 분리)

#### 유닛별 스킬 현황

**첸 (Chen)**
- 패시브: OnAttack / Adjacent / Debuff SPD -2 / buffDuration=1
- hitEffectPrefab: WIND.prefab (8프레임)

**제피로스 (Zephyros)**
- 패시브: Always / `requiresHighestSpeed=true` — 아군 중 최고 SPD일 때만 발동
- 데미지 감소 + VFX (공격자 vs 방어자 SPD 비교 후 `ApplySpeedPassiveReduction()` 처리)

**워든 (Warden)**
- 패시브: OnAttack / SingleEnemy → `WaitForPassiveSkillTarget` 경로
  - 공격 전 플레이어가 공격범위 내 적 클릭 → LockMovement 적용
- 액티브 hitEffectPrefab: 미연결 (미완료)

**앵크 (Anc)**
- 스킬 어셋 추가 완료

**록 계열 패시브 5종**

| 유닛 | 스킬명 | 트리거 | 효과 |
|---|---|---|---|
| 팔란 (Palan) | 방패 술기 | OnTurnStart / Self | DamageReceived Buff +3 |
| 록 (Rok) | 도끼 강타 | OnAttack / SingleEnemy | Damage +5 |
| 슈테인 (Suetain) | 철갑의 중압 | OnTurnStart / Self | DEF Buff +4 |
| 베덴 (Beden) | 경계 태세 | OnTurnStart / Adjacent 적 | DamageReceived Debuff +3 |
| 팔라디움 (Paladium) | 백금의 성역 | OnTurnStart / Adjacent 아군 | DamageReceived Buff +8 |

**벤트 (Bent)**
- 패시브 trigger: Always → OnTurnStart로 변경 (VFX 출력 위해)

---

### 3. 전투 시퀀스 (GameManager / TurnManager)

#### 전투 흐름
1. 플레이어 유닛 선택 → 이동 범위 하이라이트
2. 이동 클릭 → `PlayCombatSequence()` 코루틴
3. 이동 유닛이 적과 인접 → 전투 판정
4. 패시브 트리거 처리 (`TriggerPassiveSkills()`)
5. 턴 전환 → AI 또는 상대방 입력

#### 인게임 클릭 개선
- `HandleInfoClick()` 추가: 턴/AI 무관하게 항상 클릭 시 유닛 정보 패널 표시
- 내 기물 패널과 상대 기물 패널 독립적으로 동작
- 빈 칸 클릭 시만 두 패널 모두 숨김

#### hitEffect VFX 대기 로직
- `TriggerPassiveSkills()` 안에 `anyHitEffect` 플래그 추가
- hitEffect 생성 시 루프 후 `WaitForSeconds(1.0f)` 대기
- OnTurnStart VFX와 이동 시작 사이 타이밍 확보

---

### 4. VFX 파이프라인

#### 프리팹 구조
- `castEffectPrefab` → 시전자 위치 (자기 버프/오라 등)
- `hitEffectPrefab` → 타겟 위치 (폭발/데미지 등)

#### 생성 방식
- UI 프리팹 (RectTransform): Canvas 자식, 월드→스크린→캔버스 로컬 좌표 변환
- World 프리팹: `Instantiate(prefab, position, Quaternion.identity)`

#### 애니메이션 구조
- 공유 컨트롤러: `VFX_Controller.controller` → `IdleVFX.anim` (8프레임, 1초)
- Frame 자식 없으면 Unity가 자동 무시 (프레임 적은 VFX에 활용)
- Frame07/08 value=0 버그 존재 (켜지는 키프레임이 1이 아닌 0으로 저장됨)

#### 픽셀 아트 설정 규칙
- Filter Mode: **Point (no filter)** 필수
- PPU: 캐릭터/타일과 통일
- 프리팹 편집: 반드시 Prefab Mode에서 직접 편집 후 저장

---

### 5. UI 시스템

#### 메인메뉴 (MainmenuManager)
- 랭크 뱃지: `rankBadgeOutline` + `rankBadgeBg` 2단 구조
- 플레이어 카드: 승률, 전투수, 미션 진행도 표시

#### 가챠 시스템
- 방사형 광선 VFX, 링 페이드 애니메이션
- 퀸 덱 제한 (특정 유닛 중복 불가)
- 정렬 시스템 추가

#### 인게임 설정 패널 (InGameSettingsPanel)
- 항복 / 재개 / 사운드 구조로 개편
- `forfeitResultPanel` 관련 코드 전면 제거 (온라인화 이후)

#### 결과창 (VictoryUI)
- `Show(localPlayerID)` 파라미터 추가 → Player 2 승패 표시 수정
- DEFEAT / VICTORY 패널 온라인 기준으로 재작성

#### 모드 선택 패널 (ModeSelectPanel)
- AI / 일반 / 랭크 3가지 모드 선택
- 씬에서 초기 비활성화 → 활성화 상태로 수정 (첫 클릭 무반응 버그 해결)

#### 랭크 패널 (RankPanelUI)
- `rankBadgeOutline` + `rankBadgeBg` 2단 구조 완성
- ContextMenu 디버그 메서드 추가 (D/C/B/A/S 즉시 설정)

---

### 6. 데이터 영속성

#### PlayerInventory
- `Awake()`: `ResetInventory()` 제거 → `LoadInventory()` 유지
- PlayerPrefs 로컬 저장 + Firestore 서버 저장 병행

#### RankSystem
- `SetFromServer(pts)`: 서버 데이터 적용
- `SyncToFirestore()`: 서버 동기화
- 현재 클라이언트 쓰기 허용 (Phase 5 이후 Cloud Function 전환 예정)

#### DailySystem
- 접속 보너스 +100골드 / 3승 보너스 +150골드
- Firestore 서버 타임스탬프 기준

#### Firestore 데이터 구조
```
users/{uid}/
  currency              int
  ownedUnits            array[{instanceId, unitName, enhanceLevel}]
  rankPoints            int
  lastDailyBonus        string (yyyy-MM-dd)
  dailyMissionDate      string (yyyy-MM-dd)
  dailyMissionWins      int
  dailyMissionClaimed   bool
```

---

## 온라인 멀티플레이어 구축

### Phase 0 — 외부 환경 설정 (완료)
- Firebase 프로젝트 생성 + Auth(Google 로그인) + Firestore + Cloud Functions
- `google-services.json` Unity 프로젝트 추가
- Photon Fusion SDK import, App ID 발급

### Phase 1 — Firebase Auth (완료)
- `FirebaseManager.cs` 신규 생성
- 구글 로그인 / 익명 로그인 / UID 발급
- 앱 시작 시 자동 로그인, MainMenu 연동

### Phase 2 — Firestore 데이터 서버화 (완료)
- `PlayerInventory.cs`: Firestore 연동, `ApplyFromFirestore()` 추가
- `RankSystem.cs`: `SetFromServer()` + `SyncToFirestore()` 추가
- `DailySystem.cs`: 서버 타임스탬프 기반 데일리 처리
- `MainmenuManager.cs`: `OnFirestoreDataReady` 콜백으로 UI 갱신
- 패턴: 로컬 로드(즉시) → Firebase 완료 → 서버 데이터 덮어쓰기

### Phase 3 — Photon 매칭 (코드 완료 / 씬 작업 미완료)
- `PhotonManager.cs` 신규 생성 (전면 재작성 포함)
- 랭크 점수 기반 매칭: 100점 버킷, 1분 간격 ±N 확장
- `OnSessionListUpdated` 세션 브라우징
- 충돌 해소: 낮은 점수가 높은 점수 세션으로 이동 (동점: 세션명 사전순)
- `ExpandTolerance` 코루틴, `MigrateToSession`, `CreateRunner/ShutdownRunner` 분리

**매칭 취소 씬 이동 버그 수정**
- `Scene = SceneRef.FromIndex(4)` 제거 → 연결 즉시 SampleScene 이동 방지
- `PhotonManager.LoadGameScene()` 추가 → 매칭 성사 후 Host만 씬 전환 호출
- `HandleMatchReady()`: SceneManager.LoadScene 제거, Host → LoadGameScene 호출

### Phase 4 — 게임 동기화 (코드 완료 / 씬 작업 미완료)
- `NetworkGameBridge.cs` 신규 생성 (NetworkBehaviour 상속, SampleScene에 부착)
- **Deterministic Lockstep**: 양쪽이 동일한 로직을 각자 실행, Photon은 입력만 중계

**주요 RPC**

| RPC | 역할 |
|---|---|
| `RPC_SendDeck` | 덱 직렬화 → 상대에게 전송, 양쪽 수신 완료 시 `OnDecksReady()` 호출 |
| `RPC_SendMove` | 이동 브로드캐스트, 자신이 보낸 건 스킵 |
| `RPC_SendPassiveTarget` | WaitForPassiveSkillTarget 온라인 동기화 |
| `RPC_Surrender` | 항복 브로드캐스트 |

**주요 버그 수정**

| 버그 | 수정 내용 |
|---|---|
| 덱 RPC 타이밍 | Host가 Client 덱 수신 시 자신의 덱 재전송 (`_deck0Json` 캐시) |
| 보드 미반전 | `BoardVisualizer.IsFlipped` + `GridToWorld/WorldToGrid` 헬퍼 추가, Player 2 반전 표시 |
| ownerID 하드코딩 | 스킬슬롯/액티브스킬타겟 체크 전부 `_localPlayerID`로 교체 |
| WaitForPassiveSkillTarget | 로컬/상대/AI 3경로 분리 |

### 항복 시스템 온라인화 (완료)
- `RPC_Surrender` → 상대에게 브로드캐스트
- `GameManager.ReceiveSurrender()` → 승리 패널 + 전적/랭크 처리
- `GameManager.ShowForfeitDefeat()` → 항복 측 DEFEAT 패널 (VictoryUI 재사용)
- `HandleKingDeath()`: `localWin` 기준으로 전적/골드/랭크 처리
- `OnRematch/OnMainMenu`: `Disconnect()` 후 씬 전환

### Phase 5 — Cloud Function 결과 검증 (미시작)
```
1. 게임 종료 → 양측이 이동 로그 Firestore 제출
2. Cloud Function 트리거
3. 서버에서 게임 전체 재실행 + 결과 검증
4. 일치 → Firestore rankPoints/currency 업데이트
5. 불일치 → 무효 처리
```

### Phase 6 — ParrelSync 테스트 (미시작)
- PC 1대, 에디터 2개로 매칭/동기화/전투 결과 전 항목 검증

### Phase 전체 상태표

| Phase | 내용 | 상태 |
|---|---|---|
| 0 | Firebase/Photon 외부 설정 | 완료 |
| 1 | Firebase Auth (구글 로그인 + UID) | 완료 |
| 2 | Firestore 플레이어 데이터 서버화 | 완료 |
| 3 | Photon 매칭 + 방 연결 | 코드 완료, 씬 작업 미완료 |
| 4 | 게임 동기화 (Deterministic Lockstep) | 코드 완료, 씬 작업 미완료 |
| 5 | Cloud Function 결과 검증 | 미시작 |
| 6 | ParrelSync 테스트 | 미시작 |

---

## 버그 수정 이력

| 버그 | 원인 | 수정 |
|---|---|---|
| 제피로스 StackOverflow | Always 캐시 재빌드 중 재귀 호출 | `_isRebuildingCache` 플래그로 재귀 차단 |
| AI 패시브 freeze | 관전 모드에서 `SetAIThinking(false)` 미호출 | `IsSpectatorMode` 조건 추가 |
| 이동 후 재이동 (코루틴 2개) | `hasMovedThisTurn` 체크 누락 | `HandleInput()`에 체크 추가 |
| 턴 전환 레이스컨디션 | `SwitchTurn()` 전 AI/플레이어 입력이 끼어듦 | `PlayCombatSequence()`에서 `SetAIThinking(true)` 선행 |
| 인챈터 공격범위 오류 | `(±2,0),(0,±2)` 로 잘못 설정 | `(±1,0),(0,±1)`로 수정 |
| 항복 패 미반영 | `OnForfeit()`에 PlayerPrefs 저장 누락 | 저장 코드 추가 |
| ModeSelectPanel 첫 클릭 무반응 | 씬에서 비활성화 상태 | 활성화로 변경 |
| WaitForPassiveSkillTarget VFX 누락 | SingleEnemy 경로가 VFX 코드 else 분기를 타지 않음 | AI/플레이어 양쪽 경로에 VFX 호출 추가 |
| 보드 미반전 (Player 2) | GridToWorld 변환에 flip 미적용 | `BoardVisualizer.GridToWorld()` 헬퍼 추가 |
| EUC-KR 인코딩 깨짐 | 폰트 경로 문제 | 폰트 경로 이동 + UTF-8 재저장 |

---

## 미완료 항목

### 온라인 동기화
- **액티브 스킬 RPC 누락**: Player 2가 액티브 스킬 사용 시 상대에게 동기화 없음
  - `RPC_SendActiveSkill(senderID, skillName, targetUnitId)` 추가 필요

### 콘텐츠
- **워든 액티브 hitEffectPrefab**: WIND.prefab 복사 → Frame08 삭제 → 스프라이트 교체 후 Inspector 연결
- **록 계열 스킬 VFX**: Beden, Rok, Suetain, Bent, Palan, Paladium 6종 프리팹 제작

### UI
- **버프 수치 팝업**: 버프/디버프 발동 시 DamagePopup처럼 보드 위 수치 표기
  - 색상: 버프=초록 / 디버프=빨강 / 피해감소=파랑
  - `DamagePopup` 확장 or 별도 `BuffPopup` 컴포넌트 작성
- **매칭 인디케이터**: 구석에 체스 킹 이미지 + 애니메이션 (위치/스타일 미확정)

### 씬 작업 (Phase 3~4 이월)
- **MainMenu 씬**: 매칭 대기 패널 UI 생성 + `MainmenuManager` Inspector 필드 연결
  - `matchmakingPanel`, `matchmakingStatusText`, `cancelMatchmakingButton`
- **SampleScene**: 빈 오브젝트에 `NetworkObject` + `NetworkGameBridge` 컴포넌트 추가

### 시스템
- **팔라디움 패시브 정식 구현**: 현재 DamageReceived Buff -8 (근사값)
  - 목표: 강제 이동 면역 + 데미지 50% 분담 (SkillEffectType 신규 추가 필요)
- **Phase 5**: Firebase Cloud Function 결과 검증
- **Phase 6**: ParrelSync 로컬 테스트

### 기타
- **포트폴리오/이력서**: 기술 스택 정리 (형식/언어 미확정)

---

## 주요 파일 경로

| 역할 | 경로 |
|---|---|
| 유닛 정의 | `Assets/Project/Core/UnitDefinition.cs` |
| 유닛 전투 상태 | `Assets/Project/Core/UnitState.cs` |
| 스킬 정의 | `Assets/Project/Core/SkillDefinition.cs` |
| 이동 범위 계산 | `Assets/Project/Core/MovementResolver.cs` |
| 공격 대상 계산 | `Assets/Project/Simulation/AttackResolver.cs` |
| 스킬 실행 | `Assets/Project/Simulation/SkillExecutor.cs` |
| 인게임 흐름 | `Assets/Project/Presentation/GameManager.cs` |
| 턴 관리 + AI | `Assets/Project/Presentation/TurnManager.cs` |
| 보드 시각화 | `Assets/Project/Presentation/BoardVisualizer.cs` |
| 네트워크 브릿지 | `Assets/Project/Presentation/NetworkGameBridge.cs` |
| Photon 매칭 | `Assets/Project/Core/PhotonManager.cs` |
| Firebase | `Assets/Project/Core/FirebaseManager.cs` |
| 메인메뉴 | `Assets/Project/Presentation/MainmenuManager.cs` |
| 결과창 UI | `Assets/Project/Presentation/VictoryUI.cs` |
| 랭크 시스템 | `Assets/Project/Core/RankSystem.cs` |
| 데일리 시스템 | `Assets/Project/Presentation/DailySystem.cs` |
| 덱 씬 전달 | `Assets/Project/Core/DeckTransfer.cs` |
| 인벤토리 | `Assets/Project/Core/PlayerInventory.cs` |
