# Stage 1 — 코어 시스템 상세 명세

> **선행 문서:** `SKILL_SYSTEM_ROADMAP.md` 먼저 읽고 시작
>
> **목표:** 가장 많은 스킬이 의존하는 4개 코어 시스템 구축
>
> **완료 시 작동 스킬 수:** 9개 / 27개 (33%)

---

## 1-1. 받는 데미지 증감 시스템

### 영향받는 스킬 (5개 효과)

| 캐릭터 | 스킬 | 효과 |
|---|---|---|
| 모노리스 | 부동의 의지 | 같은 가로줄 아군 `DamageReceived -10` |
| 모르카 | 멸망의 각인 (액티브) | 적 전체 `DamageReceived +5`, 3턴 |
| 아스트레오 | 운명의 별빛 (액티브) | 아군 `DamageReceived -5`, 3턴 (ATK 버프와 조합) |
| 아스트레오 | 별의 인도 (패시브) | 매 턴 무작위 아군 `DamageReceived -5`, 1턴 |
| 카이로스 | 소멸의 그림자 (패시브) | 공격한 적 `DamageReceived +10`, 1턴 |

### 네이밍 규칙

**변수명:** `DamageReceived`
- 양수 = 데미지 더 받음 (취약)
- 음수 = 데미지 덜 받음 (저항)
- 부호 의미가 일관됨

### 구현 명세

#### 1. StatType.cs

`DamageReceived` enum 값 추가:

```csharp
public enum StatType
{
    HP,
    ATK,
    SPD,
    DEF,
    // Damage modifier when this unit is hit
    // Positive = takes more damage, Negative = takes less damage
    DamageReceived
}
```

#### 2. UnitState.cs

다음 메서드 추가:

```csharp
// Damage received modifier from buffs
public int GetDamageReceivedModifier()
{
    int modifier = 0;
    foreach (var buff in activeBuffs)
        if (buff.statType == StatType.DamageReceived)
            modifier += buff.amount;
    return modifier;
}

// Calculate final damage this unit will take
// Formula: max(0, incomingDamage - DEF + DamageReceivedModifier)
public int CalculateFinalDamage(int incomingDamage)
{
    int afterDef = incomingDamage - GetDefense();
    int afterModifier = afterDef + GetDamageReceivedModifier();
    return Mathf.Max(0, afterModifier);
}
```

#### 3. GameManager.cs 통합

`PlayCombatSequence()`에서 데미지 적용 부분 수정:

```csharp
// 기존
target.TakeDamage(attacker.GetAttack());

// 변경
int finalDamage = target.CalculateFinalDamage(attacker.GetAttack());
target.TakeDamage(finalDamage);
```

### 테스트 시나리오

1. **양수 테스트** — 모르카 액티브 사용 → 적이 받는 데미지 증가 확인
2. **음수 테스트** — 모노리스 같은 가로줄 아군 → 데미지 감소 확인
3. **중첩 테스트** — 여러 버프 동시 적용 시 누적 계산 확인

### 체크리스트

- [x] `StatType.cs`에 `DamageReceived` 추가
- [x] `UnitState.cs`에 `GetDamageReceivedModifier()` 추가
- [x] `UnitState.cs`에 `CalculateFinalDamage()` 추가
- [x] `GameManager.cs`의 데미지 적용 로직 수정
- [x] 인게임 테스트 (모르카 액티브 시뮬레이션)

---

## 1-2. 위치 기반 탐지 시스템

### 영향받는 스킬 (5개 효과)

| 캐릭터 | 스킬 | 필요 탐지 |
|---|---|---|
| 모노리스 | 부동의 의지 | 같은 가로줄의 아군 |
| 시르반 | 숲의 가호 | 인접 8방향 아군 |
| 돔 | 증기 충격 | 인접 8방향 적 |
| 연무 | 환영의 속삭임 | 직선 8방향(가로/세로/대각선)의 적 |
| 아노말리 | 심연의 인력 (액티브) | 직선 8방향의 적 |

### 구현 명세

#### BoardState.cs에 헬퍼 메서드 추가

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Chess.Core
{
    public partial class BoardState
    {
        // Returns all alive units in the same row (y coordinate)
        // If filterByOwner = true, only returns units of given ownerID
        public List<UnitState> GetUnitsInRow(int y, int? ownerID = null)
        {
            var result = new List<UnitState>();
            foreach (var unit in GetAllUnits())
            {
                if (!unit.IsAlive) continue;
                if (unit.position.y != y) continue;
                if (ownerID.HasValue && unit.ownerID != ownerID.Value) continue;
                result.Add(unit);
            }
            return result;
        }

        // Returns all alive units in the same column (x coordinate)
        public List<UnitState> GetUnitsInColumn(int x, int? ownerID = null)
        {
            var result = new List<UnitState>();
            foreach (var unit in GetAllUnits())
            {
                if (!unit.IsAlive) continue;
                if (unit.position.x != x) continue;
                if (ownerID.HasValue && unit.ownerID != ownerID.Value) continue;
                result.Add(unit);
            }
            return result;
        }

        // Returns alive units in 8 adjacent tiles (1 step in all directions)
        // Excludes the caster itself
        public List<UnitState> GetAdjacentUnits(Vector2Int pos, int? ownerID = null)
        {
            var result = new List<UnitState>();
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    var target = new Vector2Int(pos.x + dx, pos.y + dy);
                    if (!IsInBounds(target)) continue;
                    if (!TryGetUnit(target, out var unit)) continue;
                    if (!unit.IsAlive) continue;
                    if (ownerID.HasValue && unit.ownerID != ownerID.Value) continue;
                    result.Add(unit);
                }
            }
            return result;
        }

        // Returns alive units in 8-direction straight lines from pos
        // Includes row, column, and both diagonals
        // Stops at first unit in each direction (for line-of-sight)
        public List<UnitState> GetUnitsInLine(Vector2Int pos, int? ownerID = null)
        {
            var result = new List<UnitState>();
            Vector2Int[] directions = {
                new Vector2Int(1, 0), new Vector2Int(-1, 0),
                new Vector2Int(0, 1), new Vector2Int(0, -1),
                new Vector2Int(1, 1), new Vector2Int(-1, 1),
                new Vector2Int(1, -1), new Vector2Int(-1, -1)
            };

            foreach (var dir in directions)
            {
                var current = pos + dir;
                while (IsInBounds(current))
                {
                    if (TryGetUnit(current, out var unit) && unit.IsAlive)
                    {
                        if (!ownerID.HasValue || unit.ownerID == ownerID.Value)
                            result.Add(unit);
                        break; // stop at first unit (line-of-sight)
                    }
                    current += dir;
                }
            }
            return result;
        }
    }
}
```

**중요:** `BoardState`가 기존 클래스라면 `partial` 키워드를 추가하거나 기존 클래스에 직접 메서드 추가.

### 테스트 시나리오

1. **가로줄 탐지** — 모노리스와 같은 y좌표의 아군이 정확히 반환되는지
2. **인접 탐지** — 시르반 주변 8칸의 아군이 정확히 반환되는지
3. **직선 탐지** — 연무의 8방향 첫 번째 유닛만 반환되는지 (벽에 막히면 중단)

### 체크리스트

- [x] `BoardState.cs`에 `GetUnitsInRow()` 추가
- [x] `BoardState.cs`에 `GetUnitsInColumn()` 추가
- [x] `BoardState.cs`에 `GetAdjacentUnits()` 추가
- [x] `BoardState.cs`에 `GetUnitsInLine()` 추가
- [x] 단위 테스트 (보드 디버그 출력)

---

## 1-3. HP 비율 조건 시스템

### 영향받는 스킬 (2개 효과)

| 캐릭터 | 스킬 | 조건 |
|---|---|---|
| 벤트 | 핏빛 광기 | 자신 HP 50% 이하일 때 ATK +20 |
| 크로노스 | 시간의 가속 | 자신 HP 50% 이상일 때 모든 아군 SPD +3 |

### 구현 명세

#### UnitState.cs에 메서드 추가

```csharp
// Returns HP percentage as float (0.0 ~ 1.0)
public float GetHPPercent()
{
    if (definition.maxHP <= 0) return 0f;
    return (float)currentHP / definition.maxHP;
}

// Returns true if HP percent is within range
public bool IsHPInRange(float min, float max)
{
    float percent = GetHPPercent();
    return percent >= min && percent <= max;
}
```

### 테스트 시나리오

1. **벤트 HP 60%** → 광기 발동 안 함 (`GetHPPercent() > 0.5`)
2. **벤트 HP 40%** → 광기 발동 (`GetHPPercent() <= 0.5`)
3. **크로노스 HP 60%** → 팀 SPD +3 발동
4. **크로노스 HP 40%** → 팀 SPD +3 미발동

### 체크리스트

- [x] `UnitState.cs`에 `GetHPPercent()` 추가
- [x] `UnitState.cs`에 `IsHPInRange()` 추가
- [x] 인게임 테스트 (HP 조건 확인)

---

## 1-4. Always 트리거 처리 시스템

### 영향받는 스킬 (6개 효과)

| 캐릭터 | 스킬 | 발동 방식 |
|---|---|---|
| 모노리스 | 부동의 의지 | 상시 (같은 가로줄 아군에게 효과) |
| 벤트 | 핏빛 광기 | 조건부 (자신 HP 50% 이하) |
| 시르반 | 숲의 가호 | 매 턴 종료 시 (인접 아군 회복) |
| 크로노스 | 시간의 가속 | 조건부 (자신 HP 50% 이상) |
| 연무 | 환영의 속삭임 | 상시 (직선상 적에게 효과) |
| 아스트레오 | 별의 인도 | 매 턴 시작 시 (무작위 아군 보호) |

### 두 가지 패턴 구분

**패턴 A — 상시 효과 (능력치 조회 시 자동 반영)**
- 모노리스, 벤트, 크로노스, 연무
- 다른 유닛의 능력치를 조회할 때 동적으로 계산
- 별도 트리거 호출 불필요

**패턴 B — 매 턴 발동 효과 (Timer 기반)**
- 시르반 (턴 종료 시), 아스트레오 (턴 시작 시)
- TurnManager에서 트리거 호출 필요

### 구현 명세

#### 패턴 A — UnitState의 능력치 getter 확장

각 능력치 getter에서 보드 상태를 참조하여 상시 효과 적용.

```csharp
// UnitState.cs

// Always-active passive effects from other units must be applied here
// Note: requires BoardState reference. Pass via parameter or property.
private BoardState board; // assigned externally

public void SetBoardReference(BoardState board)
{
    this.board = board;
}

public int GetAttack()
{
    int atk = definition.attackPower;

    // Apply buffs
    foreach (var buff in activeBuffs)
        if (buff.statType == StatType.ATK)
            atk += buff.amount;

    // Apply Always passive effects from other units
    if (board != null)
        atk += GetAlwaysAttackModifier();

    return Mathf.Max(0, atk);
}

// Calculate ATK modifier from Always passive effects of other units
private int GetAlwaysAttackModifier()
{
    int modifier = 0;
    foreach (var otherUnit in board.GetAllUnits())
    {
        if (otherUnit == this || !otherUnit.IsAlive) continue;
        if (otherUnit.definition.passiveSkills == null) continue;

        foreach (var skill in otherUnit.definition.passiveSkills)
        {
            if (skill == null) continue;
            if (skill.trigger != SkillTrigger.Always) continue;
            if (skill.effectType != SkillEffectType.Buff &&
                skill.effectType != SkillEffectType.Debuff) continue;

            // Check if this unit is in the effect's target range
            if (IsTargetOfAlwaysPassive(otherUnit, skill))
            {
                int amount = skill.effectType == SkillEffectType.Debuff
                    ? -skill.power : skill.power;
                // TODO: Apply based on stat type
                modifier += amount;
            }
        }
    }
    return modifier;
}

private bool IsTargetOfAlwaysPassive(UnitState caster, SkillDefinition skill)
{
    // Check conditions like "same row", "adjacent", "line of sight"
    // Also check caster's HP percent condition if applicable
    // Implementation depends on how passive conditions are stored

    // Example: 모노리스 (same row, ally only)
    // Example: 연무 (line of sight, enemy only)

    return false; // implement per case
}
```

**복잡도 경고:** 패턴 A는 능력치 계산이 매번 보드 전체를 순회하게 됩니다. 성능 영향이 있을 수 있으니 캐싱 검토 필요.

#### 패턴 B — TurnManager 통합

```csharp
// TurnManager.cs or GameManager.cs

void OnTurnStart(int playerID)
{
    foreach (var unit in board.GetAllUnits())
    {
        if (unit.ownerID != playerID || !unit.IsAlive) continue;
        TriggerAlwaysOnTurnStart(unit);
    }
}

void OnTurnEnd(int playerID)
{
    foreach (var unit in board.GetAllUnits())
    {
        if (unit.ownerID != playerID || !unit.IsAlive) continue;
        TriggerAlwaysOnTurnEnd(unit);
    }
}
```

### 구현 권장 순서

1. **패턴 B (Timer 기반) 먼저** — 단순하고 성능 영향 적음
2. **패턴 A (상시 효과)** — 복잡도 높으니 추후 최적화 포함 구현

### 체크리스트

- [x] TurnManager에 `OnTurnStart`/`OnTurnEnd` 훅 추가
- [x] Always 패시브 트리거 메서드 작성 (패턴 B)
- [x] UnitState에 BoardState 참조 추가 (패턴 A 준비)
- [x] 능력치 getter에 Always 효과 반영 (패턴 A) — `GetAlwaysModifier` + dirty flag 캐시
- [x] 성능 최적화 (alwaysCache + _isDirty flag 적용)
- [x] 인게임 테스트 (시르반 회복, 모노리스 방어 확인)

---

## 1단계 완료 시 작동하는 스킬 목록

다음 9개 스킬이 1단계 완료 후 작동 가능:

1. **모노리스** - 부동의 의지 ✓
2. **벤트** - 핏빛 광기 ✓
3. **시르반** - 숲의 가호 ✓
4. **크로노스** - 시간의 가속 ✓
5. **돔** - 증기 충격 ✓
6. **연무** - 환영의 속삭임 ✓
7. **모르카** - 멸망의 각인 (액티브) ✓
8. **아스트레오** - 운명의 별빛, 별의 인도 ✓
9. **카이로스** - 소멸의 그림자 ✓

---

## 다음 단계

1단계 완료 후 `STAGE_2_AUXILIARY_SYSTEMS.md` 진행.
