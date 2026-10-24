# Royal Piece (전투체스) — Claude 지시 파일

## 절대 규칙
- 코드 수정은 반드시 유저 오더 후에만 진행. 분석/설명 후 허락 없이 절대 작업 진행 금지.
- 대화 종료 전 반드시 진행 지점을 memory에 저장.

---

## 프로젝트 기본 정보
- 이름: 전투체스 (Royal Piece)
- 로컬 경로: `C:\Users\user\전투체스`
- GitHub: `ImSeoK/Royal-Piece`
- 엔진: Unity 2022+ / 언어: C# / 브랜치: main
- 네트워크: Photon **Fusion** (PUN2 아님) — `using Fusion; using Fusion.Sockets;`
- 백엔드: Firebase Auth + Firestore + Cloud Functions

---

## 기술스택 주의사항
- **Unity IAP v5**: `IDetailedStoreListener` / `UnityPurchasing` / `PurchaseProcessingResult` 전부 obsolete
  올바른 패턴: `UnityIAPServices.DefaultStore().Connect()` → `FetchProducts()` → `OnPurchasePending` + `ConfirmPurchase()`
- **Photon Fusion**: PUN2와 API 완전히 다름. `using Fusion; using Fusion.Sockets;`

---

## 용어 / 상수
- 일반 재화: **로얄** (코드 변수명: `currency`)
- 프리미엄 재화: **로열 젬** (코드 변수명: `gems`)
- enhance 공식: `stat × 0.08 반올림` (모든 기물 공통)
- 등급 색상: Common `#aaaaaa` / Rare `#5aadff` / Epic `#b57bff` / Legendary `#ffc642`
- 에셋 라이선스: game-icons.net — CC BY 3.0 출처 표기 필수 / kenney.nl — CC0

---

## 씬 흐름
```
Login → MainMenu → DeckBuilder → SampleScene(인게임)
                 → Gacha
                 → Inventory
```

---

## Firestore 데이터 구조
```
users/{uid}/
  currency            int
  gems                int
  ownedUnits          array[{instanceId, unitName, enhanceLevel}]
  rankPoints          int  ← Cloud Function만 쓰기 가능
  lastDailyBonus      string (yyyy-MM-dd)
  dailyMissionDate    string
  dailyMissionWins    int
  dailyMissionClaimed bool
```

---

## 주요 파일 경로

| 역할 | 경로 |
|---|---|
| 유닛 정의 | `Assets/Project/Core/UnitDefinition.cs` |
| 스킬 실행 | `Assets/Project/Simulation/SkillExecutor.cs` |
| 인게임 흐름 | `Assets/Project/Presentation/GameManager.cs` |
| 턴 관리 + AI | `Assets/Project/Presentation/TurnManager.cs` |
| 메인메뉴 | `Assets/Project/Presentation/MainmenuManager.cs` |
| 덱 씬 전달 | `Assets/Project/Core/DeckTransfer.cs` |
| 인벤토리 | `Assets/Project/Core/PlayerInventory.cs` |
| Firebase 관리 | `Assets/Project/Core/FirebaseManager.cs` |
| IAP | `Assets/Project/Core/IAPManager.cs` |
| 샵 패널 | `Assets/Project/Presentation/ShopPanel.cs` |
| 가챠 시스템 | `Assets/Project/Core/GachaSystem.cs` |
| 데일리 시스템 | `Assets/Project/Presentation/DailySystem.cs` |
| 로그인 | `Assets/Project/Presentation/LoginManager.cs` |
| 네트워크 브릿지 | `Assets/Project/Presentation/NetworkGameBridge.cs` |
| Cloud Function | `functions/index.js` |
| 덱빌더 | `Assets/Project/Presentation/DeckBuilderManager.cs` |
| 덱 필터 | `Assets/Project/Presentation/DeckFilterPanel.cs` |

---

## memory/ 파일 인덱스

| 파일 | 내용 요약 |
|---|---|
| `MEMORY.md` | 최근 작업 현황, 완료 기능, Inspector 대기 항목 |
| `roadmap.md` | 전체 미완료 작업 목록 (IAP·합성·에셋 등) |
| `online_roadmap.md` | Photon Fusion + Firebase Phase별 온라인 전환 계획 |
| `iap_plan.md` | IAP 상품 구조, 개발자 계정 절차, 심사 주의사항 |
| `economy_plan.md` | 재화 순환 구조, 합성 시스템 기획 |
| `skill_vfx.md` | VFX 프리팹 구조, CombatAnimator, 타이밍 상수 |
| `gacha_implementation.md` | 가챠 VFX/플로우/Inspector 연결 목록 |
| `balance_results.md` | 밸런스 시뮬레이션 1~5차 결과 |
| `mainmenu.md` | MainMenu 씬 UI 계층, PlayerCard 구조 |
| `worklog.md` | 세션별 작업 이력 |
