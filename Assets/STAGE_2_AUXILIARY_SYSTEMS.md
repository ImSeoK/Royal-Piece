# Stage 2 — 보조 시스템 상세 명세

> **선행 단계:** `STAGE_1_CORE_SYSTEMS.md` 완료 후 진행
>
> **목표:** 일부 스킬용 보조 시스템 4개 구축
>
> **누적 작동 스킬 수:** 15개 / 27개 (55%)

---

## 2-1. 이동 봉쇄 시스템

### 영향받는 스킬 (3개 효과)

| 캐릭터 | 스킬 | 봉쇄 효과 |
|---|---|---|
| 워든 | 대지의 결박 (액티브) | 전체 적 1턴 이동 불가 |
| 워든 | 뿌리의 속박 (패시브) | 공격한 적 1턴 이동 불가 |
| 글래시온 | 빙하의 일격 (액티브) | 대상 적 1턴 이동 불가 + 50 데미지 |

### 구현 명세

#### 1. UnitState.cs에 상태 추가

```csharp
// Number of turns this unit cannot move
public int movementLockedTurns = 0;

public bool IsMovementLocked => movementLockedTurns > 0;

// Apply movement lock for given turns
public void ApplyMovementLock(int turns)
{
    if (turns > movementLockedTurns)
        movementLockedTurns = turns;
}

// Decrement movement lock at turn end
public void TickMovementLock()
{
    if (movementLockedTurns > 0)
        movementLockedTurns--;
}
```

#### 2. TickBuffs() 내부에서 같이 호출

```csharp
public void TickBuffs()
{
    // existing buff tick code...

    TickMovementLock();
}
```

#### 3. MovementResolver.cs 수정

`GetValidMoves` 시작 부분에 봉쇄 체크 추가:

```csharp
public static List<Vector2Int> GetValidMoves(UnitState unit, BoardState board)
{
    // Return empty list if movement is locked
    if (unit.IsMovementLocked) return new List<Vector2Int>();

    // existing movement logic...
}
```

#### 4. 새로운 SkillEffect 추가 — LockMovementEffect

```csharp
namespace Chess.Simulation
{
    public class LockMovementEffect : SkillEffect
    {
        private int turns;

        public LockMovementEffect(int turns)
        {
            this.turns = turns;
        }

        public override void Execute(UnitState caster, List<UnitState> targets, BoardState board)
        {
            foreach (var target in targets)
            {
                if (target.IsAlive)
                    target.ApplyMovementLock(turns);
            }
        }
    }
}
```

#### 5. SkillExecutor에서 효과 매핑 추가

```csharp
// SkillExecutor.CreateEffect()
case SkillEffectType.LockMovement:
    return new LockMovementEffect(skill.power); // power = duration
```

#### 6. SkillEffectType enum에 추가

```csharp
public enum SkillEffectType
{
    Damage,
    Heal,
    Buff,
    Debuff,
    Move,
    LockMovement  // new
}
```

### UI 표시 (선택사항)

봉쇄된 유닛 위에 아이콘 또는 색상 변경:

```csharp
// UnitView.cs
public void UpdateVisuals()
{
    // existing code...

    if (unit.IsMovementLocked)
    {
        spriteRenderer.color = new Color(0.5f, 0.7f, 1f); // 푸른 톤 (얼음/봉쇄)
    }
    else
    {
        spriteRenderer.color = unit.ownerID == 0 ? Color.white : Color.black;
    }
}
```

### 테스트 시나리오

1. **워든 액티브** → 모든 적 SkillSlot 사용 → 적이 이동 시도 → 이동 불가 확인
2. **글래시온 액티브** → 단일 적 봉쇄 → 1턴 후 봉쇄 해제 확인
3. **봉쇄 중첩** → 워든 + 워든 패시브로 같은 적 봉쇄 → 더 긴 기간 적용

### 체크리스트

- [ ] `UnitState`에 봉쇄 상태 추가
- [ ] `MovementResolver`에 봉쇄 체크 추가
- [ ] `LockMovementEffect` 클래스 작성
- [ ] `SkillEffectType` enum 확장
- [ ] `SkillExecutor`에 매핑 추가
- [ ] UI 시각화 (선택)
- [ ] 인게임 테스트

---

## 2-2. 지속 데미지 (출혈) 시스템

### 영향받는 스킬 (1개 효과)

| 캐릭터 | 스킬 | 출혈 효과 |
|---|---|---|
| 모르카 | 천천한 부패 (패시브) | 매 턴 5 데미지, 2턴 |

### 구현 명세

#### 1. StatType에 출혈 효과 추가하지 말고 별도 클래스로

지속 데미지는 능력치가 아니므로 별도 데이터 구조 사용:

```csharp
namespace Chess.Core
{
    [System.Serializable]
    public class DamageOverTime
    {
        public int damagePerTurn;
        public int remainingTurns;

        public DamageOverTime(int damage, int turns)
        {
            damagePerTurn = damage;
            remainingTurns = turns;
        }
    }
}
```

#### 2. UnitState에 DoT 리스트 추가

```csharp
public List<DamageOverTime> damageOverTimes = new List<DamageOverTime>();

public void ApplyDamageOverTime(int damage, int turns)
{
    damageOverTimes.Add(new DamageOverTime(damage, turns));
}

// Apply DoT damage at turn start, returns total damage taken
public int TickDamageOverTime()
{
    int totalDamage = 0;
    for (int i = damageOverTimes.Count - 1; i >= 0; i--)
    {
        var dot = damageOverTimes[i];

        // Apply damage (bypass DEF and modifiers, fixed damage)
        TakeDamage(dot.damagePerTurn);
        totalDamage += dot.damagePerTurn;

        // Decrement and remove if expired
        dot.remainingTurns--;
        if (dot.remainingTurns <= 0)
            damageOverTimes.RemoveAt(i);
    }
    return totalDamage;
}
```

#### 3. 새로운 SkillEffect — DamageOverTimeEffect

```csharp
namespace Chess.Simulation
{
    public class DamageOverTimeEffect : SkillEffect
    {
        private int damagePerTurn;
        private int duration;

        public DamageOverTimeEffect(int damage, int duration)
        {
            this.damagePerTurn = damage;
            this.duration = duration;
        }

        public override void Execute(UnitState caster, List<UnitState> targets, BoardState board)
        {
            foreach (var target in targets)
            {
                if (target.IsAlive)
                    target.ApplyDamageOverTime(damagePerTurn, duration);
            }
        }
    }
}
```

#### 4. SkillEffectType enum 확장

```csharp
public enum SkillEffectType
{
    Damage,
    Heal,
    Buff,
    Debuff,
    Move,
    LockMovement,
    DamageOverTime  // new
}
```

#### 5. GameManager.PlayCombatSequence 통합

턴 시작 시 DoT 발동:

```csharp
// Apply DoT at start of combat sequence
foreach (var unit in board.GetAllUnits())
{
    if (unit.IsAlive)
        unit.TickDamageOverTime();
}

// Check for deaths after DoT
// ... (handle dead units)
```

#### 6. 시각 효과 (선택)

DoT 적용 시 빨간색 깜빡임 등.

### 테스트 시나리오

1. **모르카가 공격** → 적에게 출혈 적용 → 다음 턴 5 데미지 → 그 다음 턴 5 데미지 → 종료
2. **출혈 중첩** — 여러 모르카 공격 시 출혈이 누적되는지 확인 (현재 구조는 누적됨)
3. **출혈로 사망** — 출혈 데미지로 HP 0이 되면 정상 사망 처리

### 체크리스트

- [x] `DamageOverTime` 클래스 작성
- [x] `UnitState`에 DoT 리스트 + 메서드 추가
- [x] `DamageOverTimeEffect` 클래스 작성
- [x] `SkillEffectType` enum 확장
- [x] `SkillExecutor` 매핑 추가
- [x] `GameManager`에서 턴 종료 시 DoT 발동
- [x] DoT로 사망 시 처리 로직 추가 (OnDeath 트리거 + 킹 사망 체크 포함)
- [ ] 인게임 테스트 (모르카 패시브 asset 설정 후 확인)

---

## 2-3. 추가 행동 시스템

### 영향받는 스킬 (2개 효과)

| 캐릭터 | 스킬 | 효과 |
|---|---|---|
| 제피로스 | 질풍 (액티브) | 이번 턴 종료 시 추가 행동 1회 |
| 연각 | 인형의 가호 (패시브) | 사망 시 무작위 아군 추가 행동 부여 |

### 구현 명세

#### 1. UnitState에 추가 행동 권한 플래그

```csharp
public int extraActionsAvailable = 0;

public bool HasExtraAction => extraActionsAvailable > 0;

public void GrantExtraAction()
{
    extraActionsAvailable++;
}

public void ConsumeExtraAction()
{
    if (extraActionsAvailable > 0)
        extraActionsAvailable--;
}
```

#### 2. TurnManager 수정

턴 종료 시 추가 행동 가진 유닛 체크:

```csharp
// TurnManager.cs

public bool HasUnitWithExtraAction(int playerID, BoardState board)
{
    foreach (var unit in board.GetAllUnits())
    {
        if (unit.ownerID == playerID && unit.IsAlive && unit.HasExtraAction)
            return true;
    }
    return false;
}

// Modified turn switch logic
public void SwitchTurn()
{
    // If current player has units with extra actions, don't switch yet
    if (HasUnitWithExtraAction(currentPlayer, board))
    {
        // Player can move again with extra action unit
        // Don't actually switch turn
        return;
    }

    // Normal turn switch
    currentPlayer = 1 - currentPlayer;
}
```

#### 3. GameManager 통합

기물 이동 시 추가 행동 소모 처리:

```csharp
void ExecuteMove(UnitState unit, Vector2Int targetPos)
{
    // existing move code...

    // Consume extra action if this unit had one
    if (unit.HasExtraAction)
        unit.ConsumeExtraAction();
}
```

#### 4. 새로운 SkillEffect — GrantExtraActionEffect

```csharp
namespace Chess.Simulation
{
    public class GrantExtraActionEffect : SkillEffect
    {
        public override void Execute(UnitState caster, List<UnitState> targets, BoardState board)
        {
            foreach (var target in targets)
            {
                if (target.IsAlive)
                    target.GrantExtraAction();
            }
        }
    }
}
```

#### 5. SkillEffectType 확장

```csharp
public enum SkillEffectType
{
    Damage,
    Heal,
    Buff,
    Debuff,
    Move,
    LockMovement,
    DamageOverTime,
    GrantExtraAction  // new
}
```

### UI 표시 (필수)

추가 행동 가능한 유닛은 시각적으로 강조:

```csharp
// UnitView.cs
public void UpdateVisuals()
{
    // ...

    if (unit.HasExtraAction)
    {
        // Glow effect or arrow indicator
        // Show "+1 Action" indicator above unit
    }
}
```

### 테스트 시나리오

1. **제피로스 액티브 사용** → 이번 턴 이동 → 턴 종료 안 됨 → 제피로스 다시 이동 가능
2. **연각 사망** → 무작위 아군에게 추가 행동 부여 → 그 아군 이동 가능
3. **추가 행동 중첩** — 여러 효과가 동일 유닛에 추가 행동 부여 시 누적되는지

### 체크리스트

- [x] `UnitState`에 추가 행동 플래그 추가
- [x] `TurnManager`에 추가 행동 체크 로직 (GameManager에서 처리, TurnManager 수정 불필요)
- [x] `GameManager`에서 행동 소모 처리 (SwitchTurn 전 extra action 체크)
- [x] `GrantExtraActionEffect` 클래스
- [x] `SkillEffectType` 확장 및 매핑
- [ ] UI 표시 (추가 행동 가능 표시) — 선택사항, 추후 구현
- [ ] 인게임 테스트 (제피로스/연각 asset 설정 후 확인)

---

## 2-4. 영구 디버프 시스템

### 영향받는 스킬 (1개 효과)

| 캐릭터 | 스킬 | 효과 |
|---|---|---|
| 벨 | 기억의 잔재 (패시브) | 공격 시 적 SPD -3, 영구 |

### 구현 명세

#### 1. ActiveBuff에 무한 지속 지원

`remainingTurns`에 특수 값 사용:

```csharp
namespace Chess.Core
{
    public class ActiveBuff
    {
        public StatType statType;
        public int amount;
        public int remainingTurns; // -1 = permanent

        public bool IsPermanent => remainingTurns < 0;

        public ActiveBuff(StatType statType, int amount, int remainingTurns)
        {
            this.statType = statType;
            this.amount = amount;
            this.remainingTurns = remainingTurns;
        }
    }
}
```

#### 2. TickBuffs() 수정

영구 버프는 감소시키지 않음:

```csharp
public void TickBuffs()
{
    for (int i = activeBuffs.Count - 1; i >= 0; i--)
    {
        // Skip permanent buffs
        if (activeBuffs[i].IsPermanent) continue;

        activeBuffs[i].remainingTurns--;
        if (activeBuffs[i].remainingTurns <= 0)
            activeBuffs.RemoveAt(i);
    }
}
```

#### 3. BuffEffect 수정

duration이 -1이면 영구:

```csharp
namespace Chess.Simulation
{
    public class BuffEffect : SkillEffect
    {
        private StatType statType;
        private int amount;
        private int duration; // -1 = permanent

        public BuffEffect(StatType statType, int amount, int duration)
        {
            this.statType = statType;
            this.amount = amount;
            this.duration = duration;
        }

        public override void Execute(UnitState caster, List<UnitState> targets, BoardState board)
        {
            foreach (var target in targets)
            {
                if (target.IsAlive)
                    target.ApplyBuff(new ActiveBuff(statType, amount, duration));
            }
        }
    }
}
```

#### 4. SkillDefinition Inspector 사용 방법

`SkillDefinition.power` 필드를 양/음 값으로 사용하고, 영구 효과는 별도 필드로:

```csharp
// SkillDefinition.cs
[Header("효과")]
public int power;
public int duration = 2;  // 0 또는 음수 = 영구

// SkillExecutor에서 사용
int actualDuration = skill.duration <= 0 ? -1 : skill.duration;
return new BuffEffect(StatType.SPD, -skill.power, actualDuration);
```

### 테스트 시나리오

1. **벨이 적 A 공격** → A의 SPD -3 영구 적용
2. **벨이 적 A 다시 공격** → SPD -3 누적 (총 -6)
3. **턴 진행** → 영구 버프 사라지지 않는지 확인
4. **다른 임시 버프와 같이 적용** → 임시 버프는 사라지고 영구 버프만 남는지

### 체크리스트

- [x] `ActiveBuff`에 영구 지원 추가
- [x] `TickBuffs()` 영구 버프 스킵 로직
- [x] `BuffEffect`에서 영구 지속 지원 (buffDuration=-1 그대로 전달)
- [x] `SkillDefinition`에 duration 필드 추가 (buffDuration 기존 필드 활용)
- [x] `SkillExecutor`에서 영구 효과 매핑 (변경 불필요)
- [ ] 인게임 테스트 (벨 스킬 asset buffDuration=-1 설정 후 확인)

---

## 2단계 완료 시 추가 작동 스킬

다음 6개 스킬이 2단계 완료 후 추가 작동:

1. **워든** - 대지의 결박, 뿌리의 속박 ✓
2. **글래시온** - 빙하의 일격 ✓
3. **모르카** - 천천한 부패 ✓
4. **제피로스** - 질풍 (액티브) ✓
5. **연각** - 인형의 가호 ✓
6. **벨** - 기억의 잔재 (패시브) ✓

**누적 작동:** 15개 / 27개 (55%)

---

## 다음 단계

2단계 완료 후 `STAGE_3_SPECIAL_SYSTEMS.md` 진행.
