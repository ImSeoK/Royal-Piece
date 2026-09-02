# Royal Piece — 클라이언트 개발자 포트폴리오 기술 문서

> 코드베이스 실측 기준. 확인되지 않은 사항은 "미확인"으로 표기.

---

## 프로젝트 개요

| 항목 | 내용 |
|---|---|
| 장르 | 체스 기반 전략 PvP 모바일 게임 |
| 플랫폼 | iOS / Android |
| 엔진 | Unity (C#) |
| 개발 기간 | 2026.03 ~ 2026.09 (약 6개월) |
| 개발 형태 | 1인 개발 (AI 협업) |
| 총 커밋 수 | 30회 |
| 총 코드 라인 | 약 13,700줄 |
| C# 파일 수 | 95개 |

---

## 1. 기술 스택

### 엔진 / 언어

| 기술 | 용도 |
|---|---|
| Unity (C#) | 게임 엔진 전반 |
| TextMeshPro | 인게임/UI 텍스트 렌더링 |
| Unity IAP | 인 앱 결제 (구현 중) |

### 외부 서비스

| 서비스 | 용도 | 선택 이유 |
|---|---|---|
| **Photon Fusion** | 실시간 PvP 매칭 및 RPC 통신 | 소규모 모바일 게임에 적합한 무료 티어, AutoHostOrClient 방식으로 서버리스 운영 가능 |
| **Firebase Auth** | 구글/애플/익명 로그인 | 멀티 플랫폼 인증을 단일 SDK로 처리 |
| **Firebase Firestore** | 유저 데이터(인벤토리/랭크/데일리) 서버 저장 | 실시간 DB, 서버리스 구조 |
| **Firebase Cloud Functions** | 게임 결과 검증, 랭크 포인트 지급 | 클라이언트 조작 방지 목적으로 골드/랭크 수정을 서버 전용으로 분리 |
| **Google Sign-In SDK** | 구글 계정 로그인 처리 | Firebase Auth와 연동 |
| **Apple Auth SDK** | 애플 ID 로그인 처리 (iOS 전용) | App Store 심사 요건 대응 |
| **ParrelSync** | 에디터 내 멀티 인스턴스 온라인 테스트 | 단일 PC에서 Host/Client 동시 실행 |

### Cloud Functions 런타임

| 항목 | 내용 |
|---|---|
| 런타임 | Node.js 22 |
| SDK | firebase-functions ^5.1.0, firebase-admin ^12.0.0 |
| 트리거 방식 | `onDocumentCreated` (v2 API) |

---

## 2. 아키텍처 구조

### 폴더 구조

```
Assets/Project/
├── Core/           # 순수 게임 로직 (MonoBehaviour 최소화)
│   ├── UnitDefinition.cs       # ScriptableObject — 유닛 스탯/스킬 정의
│   ├── SkillDefinition.cs      # ScriptableObject — 스킬 전체 정의
│   ├── BoardState.cs           # 보드 상태 (유닛 위치/생존 관리)
│   ├── MovementResolver.cs     # 이동 가능 좌표 계산 (static)
│   ├── UnitState.cs            # 인게임 유닛 런타임 상태
│   ├── PlayerInventory.cs      # 유닛 소유/강화/골드 관리 (Singleton)
│   ├── FirebaseManager.cs      # Firebase 초기화/Auth/Firestore (Singleton)
│   ├── PhotonManager.cs        # Photon Fusion 매칭 관리 (Singleton)
│   ├── GachaSystem.cs          # 가챠 확률/가중치 계산 (static)
│   └── RankSystem.cs           # 랭크 포인트/티어 계산
│
├── Simulation/     # 전투 계산 레이어 (Unity 의존성 없음)
│   ├── AttackResolver.cs       # 공격 대상 계산 (static)
│   ├── SkillExecutor.cs        # 스킬 발동/타겟 처리
│   ├── CombatResolver.cs       # 공격/방어/데미지 계산
│   ├── BattleSimulator.cs      # AI 밸런싱용 즉시 전투 시뮬레이션 엔진
│   ├── BalanceAI.cs            # 밸런스 시뮬레이션 전용 AI
│   ├── SimpleAI.cs             # 인게임 AI
│   └── [Effect 클래스들]       # 스킬 효과별 개별 구현체
│
├── Presentation/   # UI / 씬 흐름 / 렌더링 레이어
│   ├── GameManager.cs          # 인게임 전체 흐름 (1,317줄)
│   ├── TurnManager.cs          # 턴 진행 / AI 판단
│   ├── NetworkGameBridge.cs    # Photon RPC 송수신 / 덱 교환
│   ├── BoardVisualizer.cs      # 보드 렌더링 / 하이라이트
│   ├── UnitView.cs             # 유닛 스프라이트 / HP바 / 버프 아이콘
│   ├── DeckBuilderManager.cs   # 덱 빌더 씬 전체
│   ├── InventoryManager.cs     # 인벤토리 씬
│   ├── GachaManager.cs         # 가챠 씬 UI / VFX 연출
│   └── [기타 Panel/UI 클래스]
│
└── Data/           # ScriptableObject 에셋
    ├── Rook/       # 룩 클래스 유닛 에셋
    ├── Bishop/     # 비숍 클래스 유닛 에셋
    ├── Knight/     # 나이트 클래스 유닛 에셋
    ├── Enchanter/  # 인챈터 클래스 유닛 에셋
    ├── Queen/      # 퀸 클래스 유닛 에셋
    └── Sound/      # 오디오 에셋
```

### 주요 씬 구성

| 씬 | 역할 |
|---|---|
| Login | Firebase 인증, 터치 시작 화면 |
| MainMenu | 메인 허브, 모드 선택, 랭크/데일리 표시 |
| Inventory | 유닛 보유/강화/매각 |
| DeckBuilder | 덱 편성 + 스킬 선택 |
| Gacha | 뽑기 연출 |
| SampleScene | 인게임 전투 |

### DontDestroyOnLoad 매니저 (Login씬 기준)

`FirebaseManager` → `PlayerInventory` → `AudioManager` → `DeckTransfer` → `PhotonManager` → `IAPManager`

### 데이터 흐름

```
[로그인]
Firebase Auth → UID 확정 → Firestore 로드 → PlayerInventory 동기화

[덱 빌더 → 인게임]
DeckBuilderManager → DeckTransfer(Singleton) → SampleScene 로드

[인게임 이동 (온라인)]
유저 입력 → GameManager.ExecuteMove()
→ NetworkGameBridge.RPC_SendMove() (Photon RPC)
→ 상대방 GameManager.ExecuteMove() 동기 실행
→ BoardState 갱신 → BoardVisualizer 렌더링

[게임 종료 (랭크)]
GameManager → FirebaseManager.SubmitGameLogAsync()
→ Firestore gameLogs 문서 생성
→ Cloud Function onDocumentCreated 트리거
→ 랭크포인트 / 골드 지급 → Firestore users 업데이트
```

---

## 3. 핵심 시스템 구현 상세

### 3-1. 전투 시스템 (이동 / 공격 리졸버)

**설계 의도**
체스 기물의 이동 방식을 그대로 계승하되, 각 기물 타입을 비트 플래그로 조합해 복합 이동 속성을 지원.

**구현 방식**
- `MovementAttribute` — `[Flags]` enum으로 Rook, Bishop, Knight, Enchanter, King, Pawn 조합 가능
- `MovementResolver.GetValidMoves()` — 슬라이딩 이동(Rook/Bishop)은 방향벡터 루프로 장애물 감지, L자(Knight)는 오프셋 배열로 처리
- `AttackResolver.GetAttackTargets()` — 이동 속성별 공격 범위를 독립 계산 후 합산, 아군/적 여부로 필터링
- Enchanter 클래스: 이동 `(±1,0),(0,±1),(±2,±2)`, 공격은 동일 오프셋이나 중간 기물 무시

**마주친 문제**
인챈터 공격 범위 구현 시 이동과 공격이 동일한 오프셋이어야 하는데 초기에 `(±2,0),(0,±2)`로 잘못 구현

**해결**
AttackResolver의 인챈터 전용 오프셋 배열을 MovementResolver와 일치시켜 수정

---

### 3-2. 스킬 시스템

**설계 의도**
디자이너가 코드 수정 없이 ScriptableObject 인스펙터만으로 새 스킬을 만들 수 있는 데이터 주도 설계.

**구현 방식**

`SkillDefinition` (ScriptableObject) 주요 필드:

| 필드 | 역할 |
|---|---|
| `SkillTrigger` | 발동 조건 (9종: None/OnAttack/OnDamaged/OnMove/OnAdjacent/OnDeath/Always/OnTurnStart/OnTurnEnd) |
| `SkillEffectType` | 효과 종류 (15종: Damage/Heal/Buff/Debuff/Move/Pull/FixedDamage/DamageShareAura 등) |
| `SkillTargetType` | 타겟 선택 방식 (Self/SingleEnemy/SingleAlly/RangeEnemy/RangeAlly) |
| `SkillAuraRange` | 패시브 범위 (7종: Self/AllAllies/SameRow/Adjacent/LineOfSight/Attacker/AttackRange) |
| `hasSecondaryEffect` | 보조 효과 On/Off 및 별도 효과 타입 지정 |

스킬 효과는 `SkillEffect` 추상 클래스를 상속받는 개별 Effect 클래스(DamageEffect, HealEffect, BuffEffect 등)로 구현.

**패시브 발동 흐름**
```
GameManager 이벤트 발생 → SkillExecutor.TryFirePassives()
→ trigger 타입 매칭 → auraRange로 실제 타겟 산출 → Effect 실행
→ _passivesFiredThisTurn HashSet으로 턴당 1회 중복 차단
```

**마주친 문제**
패시브가 동일 턴 내 여러 번 발동되는 문제, OnTurnStart/OnTurnEnd는 매 턴 발동해야 하나 다른 트리거와 같은 중복 차단 로직에 묶임

**해결**
`_passivesFiredThisTurn`에서 OnTurnStart/OnTurnEnd를 예외 처리, 해당 두 트리거는 중복 차단 대상에서 제외

---

### 3-3. 가챠 시스템

**설계 의도**
희귀도별 확률 보장 + 유닛별 가중치 지원.

**구현 방식**
- `GachaSystem.cs`: 희귀도 풀을 먼저 결정(확률 기반) → 해당 풀 내에서 `gachaWeight`에 따른 가중 랜덤 선택
- 10연차는 Legendary 1개 이상 보장 로직 포함
- 비용: 단뽑 90G / 10연차 900G (`PlayerInventory.SpendCurrency` 연동)

**VFX 연출**
- `GachaVFXController`: 카드 뒤집기 애니메이션, `RayBurstEffect`(방사형 광선), `RingEffect`(링 페이드) 조합
- 희귀도별 연출 분기 (Legendary일 때 별도 이펙트 재생)

**마주친 문제**
VFX Animator Frame07/08에서 value=0으로 복귀되는 버그로 마지막 프레임에 깜빡임 발생

**해결**
AnimationClip 키프레임 수동 수정 (미확인: 현재 완전 해결 여부)

---

### 3-4. 네트워크 동기화

**설계 의도**
서버에서 게임 로직을 실행하지 않고, 양쪽 클라이언트가 완전히 동일한 입력으로 동일한 로직을 실행하는 Deterministic 방식 채택. (서버 비용 없음)

**구현 방식**

```
Host(Player0) 이동 결정
→ RPC_SendMove(playerID, unitId, targetX, targetY) 브로드캐스트
→ Host / Client 모두 GameManager.ExecuteMove() 동일 실행
→ BoardState 양쪽 동기화 유지
```

RPC 목록 (`NetworkGameBridge.cs`):

| RPC | 역할 |
|---|---|
| `RPC_SendDeck` | 게임 시작 전 덱 데이터 교환 |
| `RPC_SendMove` | 유닛 이동 동기화 |
| `RPC_SendPassiveTarget` | 패시브 스킬 타겟 동기화 |
| `RPC_SendActiveSkill` | 액티브 스킬 발동 동기화 |
| `RPC_Surrender` | 항복 처리 |

**랭크 매칭 알고리즘**
100점 버킷 단위로 매칭 시도 → 60초 경과마다 허용 범위 ±100점씩 확장 → 상대 없으면 무한 확장

**마주친 문제 1**
덱 RPC 타이밍 문제: Client가 늦게 접속해 Host의 최초 덱 RPC를 못 받는 경우 게임 시작 불가

**해결**
Host가 Client 덱을 수신하면 자신의 덱을 재전송하는 로직 추가 (`_deck0Json` 캐시)

**마주친 문제 2**
`hasMovedThisTurn` 플래그와 스킬슬롯 비활성화가 상대방 이동 처리 시에도 실행되어 Player1이 자기 턴에 이동 불가

**해결**
해당 처리를 `if (senderPlayerID == _localPlayerID)` 조건으로 분리

**마주친 문제 3**
보드 시각이 Player0 기준 고정이라 Player1 시점에서 좌우가 반전되어야 하나 미처리

**해결**
`BoardVisualizer.IsFlipped` 플래그 + `GridToWorld` / `WorldToGrid` 헬퍼 메서드로 좌표 변환 일원화

---

### 3-5. Firebase 연동

**Auth 흐름**
```
LoginManager.Start()
→ FirebaseManager.IsLoggedIn 체크
→ 기존 세션: ShowTouchToStart() (터치 후 메인 이동)
→ 신규 세션: OnFirebaseReady 이벤트 → 로그인 버튼 표시
→ 로그인 성공: OnLoginSuccess → FadeAndLoad() → MainMenu
```

**Firestore 데이터 구조 (`users/{uid}`)**

```
currency          : int
rankPoints        : int      (Cloud Function 전용)
ownedUnits        : Array<{instanceId, unitName, enhanceLevel}>
lastDailyBonus    : string   (날짜 문자열)
dailyMissionDate  : string
dailyMissionWins  : int
dailyMissionClaimed : bool
```

**Cloud Function (`verifyGameResult`)**
- 트리거: `gameLogs/{gameId}` 문서 생성 시 자동 발동
- 검증: 제출 시각 30분 이내, 승자/패자 UID 유효성
- 처리: 승자 `rankPoints += 15`, 골드 지급 → `users` 문서 업데이트
- `rankPoints` / `currency`는 클라이언트에서 직접 수정 불가 (Cloud Function 전용)

**에뮬레이터 연동**
`#if UNITY_EDITOR` 블록으로 Firestore를 `127.0.0.1:8080` 에뮬레이터에 자동 연결 (Windows IPv6 이슈로 localhost 대신 IP 직접 지정)

---

### 3-6. UI 시스템

**구조 원칙**
- 각 씬은 전담 Manager 클래스 1개 (DeckBuilderManager, InventoryManager 등)
- 복잡한 UI 컴포넌트는 별도 Panel 클래스 분리 (DeckFilterPanel, ShopPanel 등)
- DontDestroyOnLoad 매니저와 씬 전용 UI 완전 분리

**주요 UI 컴포넌트**

| 클래스 | 역할 |
|---|---|
| `UnitView` | 유닛 스프라이트, HP바, 버프 아이콘 월드 UI |
| `DamagePopup` | 피해/회복 수치 팝업 애니메이션 |
| `MatchmakingOverlay` | 씬 유지형 매칭 대기 UI (DontDestroyOnLoad) |
| `DeckFilterPanel` | 타입/등급 토글 필터 패널 |
| `BuffIconConfig` | 버프/디버프 아이콘 ScriptableObject 매핑 |

**버프 아이콘 구현**
월드 스페이스 SpriteRenderer로 유닛 위 HP바 좌측 기준 수평 배열, `UnitView.worldIndicatorSize`로 크기 제어

---

## 4. 트러블슈팅 이력

| 증상 | 원인 | 해결 |
|---|---|---|
| 제피로스 패시브 StackOverflow | 패시브 발동 → 스탯 캐시 재계산 → 패시브 재발동 무한 재귀 | `_isRebuildingCache` 플래그로 재귀 차단 |
| AI 패시브 freeze | `SetAIThinking(false)` 조건에 관전 모드 체크 누락 | `IsSpectatorMode` 조건 추가 |
| 항복 후 패배 미반영 | `OnForfeit()`에서 PlayerPrefs 저장 누락 | 저장 로직 추가 |
| 온라인 Player1 이동 불가 | `hasMovedThisTurn` 제어가 `_localPlayerID` 기준 미분리 | `senderPlayerID == _localPlayerID` 조건으로 분리 |
| 덱 RPC 수신 실패 | Client 늦은 접속으로 Host 최초 RPC 누락 | Host가 Client 덱 수신 시 자신의 덱 재전송 |
| Firestore 연결 실패 (에디터) | Windows IPv6에서 `localhost` 미해석 | `127.0.0.1:8080` 직접 지정 |
| Cloud Function 미발동 | `SetAsync` 재호출 시 `onDocumentCreated` 트리거 안 됨 | gameId 중복 생성 방지 처리 |
| Firebase SDK 13.14.0 API 변경 | `SignInAnonymouslyAsync()`가 직접 User 반환에서 Result로 변경 | `result.User.UserId`로 수정 |
| TouchToStart 텍스트 즉시 비활성화 | `_waitingForTouch = true`가 `StartCoroutine` 뒤에 위치, 코루틴 즉시 실행 시 while 조건 false | 플래그 설정을 StartCoroutine 앞으로 이동 |
| DeckFilterPanel 토글 비활성 시 투명 | `toggle.graphic`이 Background Image 자체를 가리켜 isOn=false 시 alpha 강제 0 | `toggle.graphic = null`로 Toggle 자동 제어 차단 |
| DeckFilterPanel 텍스트 색상 미적용 | 토글 Label이 `TextMeshProUGUI`가 아닌 레거시 `UnityEngine.UI.Text` | `GetComponentInChildren<UnityEngine.UI.Text>()` fallback 추가 |
| 인챈터 공격 범위 오류 | 공격 오프셋이 이동 오프셋과 불일치 | AttackResolver 인챔터 오프셋 수정 |
| PlayerInventory 삭제 버그 | PlayerInventory GameObject에 FirebaseManager 스크립트 중복으로 `Destroy(gameObject)` 호출 | 중복 컴포넌트 제거 |

---

## 5. 정량적 지표

### 코드 규모

| 항목 | 수치 |
|---|---|
| 총 C# 파일 수 | 95개 |
| 총 코드 라인 | 약 13,700줄 |
| 최대 단일 파일 | GameManager.cs (1,317줄) |
| Core 레이어 | 21개 파일 |
| Simulation 레이어 | 18개 파일 |
| Presentation 레이어 | 56개 파일 |

### 콘텐츠 규모

| 항목 | 수치 |
|---|---|
| 유닛 클래스 수 | 5종 (Rook, Bishop, Knight, Enchanter, Queen) |
| 구현된 유닛 캐릭터 수 | 약 48종 + 기본 기물 6종 = 총 54종 |
| 유닛 에셋 총 수 | 110개 (유닛 본체 + 스킬 에셋 포함) |
| 스킬 트리거 종류 | 9종 |
| 스킬 효과 종류 | 15종 |
| 스킬 범위 타입 | 7종 |
| 구현된 씬 수 | 6개 (Login, MainMenu, Inventory, DeckBuilder, Gacha, SampleScene) |

### 외부 서비스 연동

| 항목 | 수치 |
|---|---|
| Firebase 연동 수 | 3종 (Auth, Firestore, Cloud Functions) |
| Cloud Function 배포 수 | 1개 (`verifyGameResult`) |
| Photon RPC 종류 | 5종 |
| 지원 로그인 방식 | 3종 (Google, Apple, 익명) |

### 개발 기간 대비 구현 범위

6개월 1인 개발 기준:
- 체스 이동/공격 리졸버 (6개 기물 타입, 비트 플래그 조합)
- 9종 트리거 × 15종 효과 스킬 시스템 (ScriptableObject 데이터 주도)
- Deterministic Lockstep 기반 온라인 PvP
- 점수 버킷 확장형 랭크 매칭
- Firebase Auth + Firestore + Cloud Functions 풀스택 연동
- 가챠 시스템 + VFX 연출
- 랭크/데일리 보상 시스템
- 6개 씬 UI 전체

---

*최종 업데이트: 2026-09-02*
