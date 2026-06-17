# Stage 3 — 특수 시스템 상세 명세

> **선행 단계:** `STAGE_2_AUXILIARY_SYSTEMS.md` 완료 후 진행
>
> **목표:** 1~2개 스킬만 사용하는 특수 시스템 6개 구축
>
> **누적 작동 스킬 수:** 21개 / 27개 (78%)

---

## 3-1. 강제 이동 시스템 (끌어당김)

### 영향받는 스킬 (1개 효과)

| 캐릭터 | 스킬 | 효과 |
|---|---|---|
| 아노말리 | 심연의 인력 (액티브) | 직선상 적 1명을 자신과 1칸 거리로 끌어당김 |

### 구현 명세

#### 1. 새로운 SkillEffect — PullEffect

```csharp
namespace Chess.Simulation
{
    public class PullEffect : SkillEffect
    {
        public override void Execute(UnitState caster, List<UnitState> targets, BoardState board)
        {
            foreach (var target in targets)
            {
                if (!target.IsAlive) continue;

                PullTarget(caster, target, board);
            }
        }

        private void PullTarget(UnitState caster, UnitState target, BoardState board)
        {
            // Determine direction from target to caster
            Vector2Int diff = caster.position - target.position;
            Vector2Int direction = new Vector2Int(
                System.Math.Sign(diff.x),
                System.Math.Sign(diff.y)
            );

            // Target position should be 1 tile away from caster
            Vector2Int targetPos = caster.position - direction;

            // Verify target position is valid
            if (!board.IsInBounds(targetPos)) return;
            if (!board.IsEmpty(targetPos))
            {
                // Position is occupied, try moving as close as possible
                Vector2Int current = target.position;
                while (current != targetPos)
                {
                    Vector2Int next = current + direction;
                    if (!board.IsInBounds(next)) break;
                    if (!board.IsEmpty(next))
                    {
                        // Stop one before the occupied tile
                        break;
                    }
                    current = next;
                }

                if (current != target.position)
                    board.MoveUnit(target, current);
                return;
            }

            // Move target to the new position
            board.MoveUnit(target, targetPos);
        }
    }
}
```

#### 2. SkillEffectType 확장

```csharp
public enum SkillEffectType
{
    // ... existing
    Pull  // new
}
```

#### 3. SkillExecutor 매핑

```csharp
case SkillEffectType.Pull:
    return new PullEffect();
```

#### 4. UnitView 위치 업데이트

강제 이동 후 시각적 위치 갱신:

```csharp
// GameManager에서 PullEffect 실행 후
foreach (var view in unitViews.Values)
    view.UpdatePosition();
```

### 테스트 시나리오

1. **아노말리 (3,3)에서 적 (3,7) 끌어당김** → 적이 (3,4)로 이동
2. **중간에 다른 유닛 있을 때** → 가능한 위치까지만 이동
3. **대각선 끌어당김** → 대각선 방향으로 1칸 거리까지

### 체크리스트

- [ ] `PullEffect` 클래스 작성
- [ ] `SkillEffectType` 확장
- [ ] `SkillExecutor` 매핑
- [ ] 시각적 이동 처리
- [ ] 인게임 테스트

---

## 3-2. 게임당 1회 한정 + 첫 공격 추적

### 영향받는 스킬 (1개 효과)

| 캐릭터 | 스킬 | 효과 |
|---|---|---|
| 벨크라스 | 심연의 일격 (패시브) | 첫 공격 시 데미지 2배 (게임당 1회) |

### 구현 명세

#### 1. UnitState에 추적 플래그

```csharp
// One-time triggered passive skills tracking
public bool firstAttackUsed = false;
```

#### 2. 패시브 처리 위치 — GameManager

`TriggerPassiveSkillsBeforeCombat` 또는 공격 시점에서:

```csharp
void HandleBelcrasPassive(UnitState attacker, UnitState target, ref int damage)
{
    // Find Belcras's passive skill
    var skill = attacker.definition.passiveSkills?.FirstOrDefault(
        s => s != null && s.skillName == "심연의 일격");

    if (skill == null) return;
    if (attacker.firstAttackUsed) return;

    // Double damage and mark as used
    damage *= 2;
    attacker.firstAttackUsed = true;
}
```

**문제점:** 스킬 이름 하드코딩은 안 좋아요. 더 좋은 방법은 SkillDefinition에 플래그 추가:

```csharp
// SkillDefinition.cs
[Header("게임당 1회 한정")]
public bool isOneTimeUse = false;
public float damageMultiplier = 1.0f; // 첫 공격 데미지 배율
```

그리고 일반적인 처리:

```csharp
void ApplyOneTimePassive(UnitState attacker, ref int damage)
{
    if (attacker.firstAttackUsed) return;
    if (attacker.definition.passiveSkills == null) return;

    foreach (var skill in attacker.definition.passiveSkills)
    {
        if (skill == null) continue;
        if (!skill.isOneTimeUse) continue;
        if (skill.trigger != SkillTrigger.OnAttack) continue;

        damage = (int)(damage * skill.damageMultiplier);
        attacker.firstAttackUsed = true;
    }
}
```

### 테스트 시나리오

1. **벨크라스 첫 공격** → 데미지 2배 적용
2. **벨크라스 두 번째 공격** → 일반 데미지 (2배 적용 안 됨)
3. **여러 게임 진행** → 게임 시작 시 플래그 초기화 확인

### 체크리스트

- [ ] `UnitState`에 `firstAttackUsed` 플래그
- [ ] `SkillDefinition`에 `isOneTimeUse`, `damageMultiplier` 필드
- [ ] `GameManager` 데미지 계산 시 처리
- [ ] 게임 시작 시 플래그 초기화
- [ ] 인게임 테스트

---

## 3-3. SPD 비교 시스템

### 영향받는 스킬 (1개 효과)

| 캐릭터 | 스킬 | 효과 |
|---|---|---|
| 제피로스 | 바람의 발걸음 (패시브) | 자신 SPD가 모든 적보다 높을 때 받는 데미지 -5 |

### 구현 명세

#### BoardState에 비교 메서드 추가

```csharp
// Check if caster has higher SPD than all enemies
public bool HasHighestSpeedAgainstEnemies(UnitState caster)
{
    int casterSpeed = caster.GetSpeed();

    foreach (var unit in GetAllUnits())
    {
        if (!unit.IsAlive) continue;
        if (unit.ownerID == caster.ownerID) continue;
        if (unit.GetSpeed() >= casterSpeed) return false;
    }
    return true;
}
```

#### 데미지 계산 시 적용

```csharp
// In damage calculation
int finalDamage = target.CalculateFinalDamage(attacker.GetAttack());

// Check Zephyros's wind step
if (target.definition.passiveSkills != null)
{
    foreach (var skill in target.definition.passiveSkills)
    {
        if (skill?.skillName == "바람의 발걸음") // or use a flag
        {
            if (board.HasHighestSpeedAgainstEnemies(target))
                finalDamage = Mathf.Max(0, finalDamage - 5);
        }
    }
}
```

**개선:** Always 트리거 처리 시스템(1-4)과 통합. SPD 비교를 조건으로 가진 Always 패시브로 처리.

### 체크리스트

- [ ] `BoardState`에 SPD 비교 메서드
- [ ] 데미지 계산 시 조건 체크
- [ ] 1-4 시스템과 통합 가능성 검토
- [ ] 인게임 테스트

---

## 3-4. SPD 고정값 강제 설정

### 영향받는 스킬 (1개 효과)

| 캐릭터 | 스킬 | 효과 |
|---|---|---|
| 미요 | 매혹의 춤 (패시브) | 피격 시 공격한 적 SPD를 1로 고정, 1턴 |

### 구현 명세

#### 1. 새로운 효과 타입 — FixedStatBuff

기존 BuffEffect는 +/- 가산이지만, 미요는 절대값 설정이라 별도 처리 필요.

```csharp
namespace Chess.Core
{
    public class FixedStatBuff
    {
        public StatType statType;
        public int fixedValue;
        public int remainingTurns;

        public FixedStatBuff(StatType statType, int value, int turns)
        {
            this.statType = statType;
            this.fixedValue = value;
            this.remainingTurns = turns;
        }
    }
}
```

#### 2. UnitState에 FixedBuff 리스트

```csharp
public List<FixedStatBuff> fixedBuffs = new List<FixedStatBuff>();

public void ApplyFixedBuff(StatType statType, int value, int turns)
{
    // Remove existing fixed buff of same type
    fixedBuffs.RemoveAll(b => b.statType == statType);
    fixedBuffs.Add(new FixedStatBuff(statType, value, turns));
}

public int? GetFixedStat(StatType statType)
{
    foreach (var buff in fixedBuffs)
        if (buff.statType == statType)
            return buff.fixedValue;
    return null;
}

public void TickFixedBuffs()
{
    for (int i = fixedBuffs.Count - 1; i >= 0; i--)
    {
        fixedBuffs[i].remainingTurns--;
        if (fixedBuffs[i].remainingTurns <= 0)
            fixedBuffs.RemoveAt(i);
    }
}
```

#### 3. GetSpeed 등 메서드 수정

고정값이 있으면 우선 적용:

```csharp
public int GetSpeed()
{
    var fixedValue = GetFixedStat(StatType.SPD);
    if (fixedValue.HasValue) return fixedValue.Value;

    // existing buff calculation
    int spd = definition.speed;
    foreach (var buff in activeBuffs)
        if (buff.statType == StatType.SPD)
            spd += buff.amount;
    return Mathf.Max(0, spd);
}
```

#### 4. 새로운 SkillEffect — FixedBuffEffect

```csharp
namespace Chess.Simulation
{
    public class FixedBuffEffect : SkillEffect
    {
        private StatType statType;
        private int fixedValue;
        private int duration;

        public FixedBuffEffect(StatType statType, int value, int duration)
        {
            this.statType = statType;
            this.fixedValue = value;
            this.duration = duration;
        }

        public override void Execute(UnitState caster, List<UnitState> targets, BoardState board)
        {
            foreach (var target in targets)
            {
                if (target.IsAlive)
                    target.ApplyFixedBuff(statType, fixedValue, duration);
            }
        }
    }
}
```

#### 5. TickFixedBuffs 호출

TickBuffs()와 같이:

```csharp
public void TickBuffs()
{
    // existing buff tick

    TickFixedBuffs();
    TickMovementLock();
}
```

### 테스트 시나리오

1. **미요 피격** → 공격자의 SPD가 1로 변경
2. **1턴 후** → 공격자의 SPD 원래대로 복귀
3. **공격자가 다른 버프 가진 상태** → 고정값이 우선 적용

### 체크리스트

- [ ] `FixedStatBuff` 클래스
- [ ] `UnitState`에 FixedBuff 리스트 + 메서드
- [ ] 각 능력치 getter에 고정값 우선 처리
- [ ] `FixedBuffEffect` 클래스
- [ ] `SkillEffectType` 확장
- [ ] 인게임 테스트

---

## 3-5. 광역 버프/디버프 제거

### 영향받는 스킬 (1개 효과)

| 캐릭터 | 스킬 | 효과 |
|---|---|---|
| 카이로스 | 공허로의 회귀 (액티브) | 모든 유닛의 모든 버프/디버프 제거 |

### 구현 명세

#### 새로운 SkillEffect — ClearBuffsEffect

```csharp
namespace Chess.Simulation
{
    public class ClearBuffsEffect : SkillEffect
    {
        public override void Execute(UnitState caster, List<UnitState> targets, BoardState board)
        {
            foreach (var target in targets)
            {
                if (target.IsAlive)
                {
                    target.activeBuffs.Clear();
                    target.fixedBuffs.Clear();
                    target.damageOverTimes.Clear();
                    // Optionally clear movement lock?
                    // target.movementLockedTurns = 0;
                }
            }
        }
    }
}
```

**디자인 결정 필요:** 봉쇄, DoT도 같이 제거할지는 게임 디자인 결정사항. 일반적으로는:
- ✅ 버프/디버프 (ActiveBuff, FixedBuff) 제거
- ⚠️ 봉쇄 (Movement Lock) — 제거할지 결정
- ⚠️ DoT (출혈) — 제거할지 결정

**제안:** 카이로스 컨셉상 "모든 효과 무로 돌림"이니 봉쇄와 DoT도 같이 제거하는 게 맞을 것 같음.

#### Active 스킬 처리

카이로스 액티브는 보드 전체 (아군+적군 모두) 대상이므로 ExecuteActive에서 처리:

```csharp
// SkillExecutor.cs
public static void ExecuteActive(SkillDefinition skill, BoardState board)
{
    // Special case for ClearBuffs: targets all units
    if (skill.effectType == SkillEffectType.ClearBuffs)
    {
        var allUnits = board.GetAllUnits().Where(u => u.IsAlive).ToList();
        var effect = new ClearBuffsEffect();
        effect.Execute(null, allUnits, board);
        return;
    }

    // existing logic
}
```

#### SkillEffectType 확장

```csharp
public enum SkillEffectType
{
    // ... existing
    ClearBuffs  // new
}
```

### 테스트 시나리오

1. **여러 캐릭터에 버프 적용 상태** → 카이로스 액티브 발동 → 모든 버프 제거 확인
2. **봉쇄된 적이 있는 상태** → 카이로스 액티브 → 봉쇄 해제 확인
3. **자기 팀 버프도 제거** → 양날의 검 동작 확인

### 체크리스트

- [ ] `ClearBuffsEffect` 클래스
- [ ] `SkillEffectType` 확장
- [ ] `SkillExecutor`의 ExecuteActive에 특수 처리
- [ ] 봉쇄/DoT 제거 여부 결정 및 구현
- [ ] 인게임 테스트

---

## 3-6. 액티브 사용권 제거

### 영향받는 스킬 (1개 효과)

| 캐릭터 | 스킬 | 효과 |
|---|---|---|
| 벨 | 망각의 실 (액티브) | 무작위 적 1명의 모든 버프 + 액티브 사용권 제거 (King 제외) |

### 구현 명세

**문제점:** 적의 액티브는 적이 갖고 있지 않음. 액티브는 상대 플레이어의 `PlayerDeck.selectedActiveSkills`와 인게임 `SkillSlotUI`에 있음.

**해결:** AI 플레이어(player 1)에게도 액티브 슬롯 데이터 추가하거나, 다른 방식으로 해석.

#### 대안 — "버프 + 액티브 사용권" 의미 재해석

현재 게임 구조에서 적(AI)은 액티브를 사용하지 않을 수 있음. 따라서 다음 중 선택:

**선택 A:** AI도 액티브 사용 — 적의 SkillSlot 시스템 추가 후 벨이 그 사용권을 빼앗음
**선택 B:** 효과 단순화 — 적의 모든 버프만 제거 (액티브 부분 빼기)
**선택 C:** 다른 효과로 재디자인 — "공격받은 적 1명을 N턴간 무력화 (이동/공격 불가)"

#### 선택 B 구현 (가장 간단)

```csharp
namespace Chess.Simulation
{
    public class ClearTargetBuffsEffect : SkillEffect
    {
        public override void Execute(UnitState caster, List<UnitState> targets, BoardState board)
        {
            foreach (var target in targets)
            {
                if (target.IsAlive)
                {
                    target.activeBuffs.Clear();
                    target.fixedBuffs.Clear();
                }
            }
        }
    }
}
```

#### 선택 C 구현 (재디자인)

```csharp
// "Disable" effect - target cannot move or be involved in combat
// Implementation similar to movement lock but also affects combat
```

### 디자인 결정 필요

이 스킬은 **선택 B로 단순화하는 것을 추천**. 액티브 사용권 시스템은 너무 복잡하고 1개 스킬을 위해 별도 시스템을 만드는 건 비효율적임.

### 체크리스트

- [ ] 디자인 결정 (B 또는 C)
- [ ] 선택한 효과 클래스 작성
- [ ] `SkillEffectType` 확장
- [ ] `SkillExecutor` 매핑
- [ ] 인게임 테스트

---

## 3단계 완료 시 추가 작동 스킬

다음 6개 스킬이 3단계 완료 후 추가 작동:

1. **아노말리** - 심연의 인력 (액티브) ✓
2. **벨크라스** - 심연의 일격 ✓
3. **제피로스** - 바람의 발걸음 (패시브) ✓
4. **미요** - 매혹의 춤 ✓
5. **카이로스** - 공허로의 회귀 (액티브) ✓
6. **벨** - 망각의 실 (액티브) ✓

**누적 작동:** 21개 / 27개 (78%)

---

## 추가 작업 (선택적)

3단계까지 완료한 후 나머지 6개 스킬 중 일부는 기존 시스템으로 작동 가능:

- **자르칸 - 모래폭풍**: 단순 데미지 추가 → 기존 시스템
- **글래시온 - 동결의 한기**: SPD 디버프 → 기존 시스템
- **루미엘 - 빛의 자비**: 자가 회복 → 기존 시스템 (OnDamaged + Self 대상)
- **아노말리 - 중력의 왜곡**: SPD 디버프 → 기존 시스템

이런 스킬들은 기존 BuffEffect를 활용하면 별도 시스템 없이도 작동.

---

## 전체 완료 후

모든 27개 스킬이 작동하면:

- 캐릭터 차별화 완성
- 시너지 시스템 (덱 조합 보너스) 추가 검토
- 강화 시스템 (만렙 +10) 적용
- 가챠 시스템과 연동
- 밸런스 테스트 및 조정
