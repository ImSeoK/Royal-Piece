using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Chess.Core;

namespace Chess.Simulation
{
    public class CombatResult
    {
        public List<UnitState> deadUnits = new List<UnitState>();
        public List<(UnitState attacker, UnitState target, int damage)> combatLog = new List<(UnitState, UnitState, int)>();
    }

    public static class CombatResolver
    {
        // 이동 후 전투 페이즈
        public static CombatResult ResolveCombatPhase(UnitState movedUnit, BoardState board)
        {
            var result = new CombatResult();

            // 1단계: 전투 참가자 수집
            var combatants = new List<UnitState>();

            // 이동한 유닛 추가
            combatants.Add(movedUnit);

            // 이동한 유닛을 공격할 수 있는 적들 찾기
            var allEnemies = board.GetAllUnits()
                .Where(u => u.ownerID != movedUnit.ownerID && u.IsAlive);

            foreach (var enemy in allEnemies)
            {
                var targets = AttackResolver.GetAttackTargets(enemy, board);
                if (targets.Contains(movedUnit))
                {
                    combatants.Add(enemy);
                }
            }

            // 이동한 유닛이 공격할 수 있는 적들도 추가
            var myTargets = AttackResolver.GetAttackTargets(movedUnit, board);
            foreach (var target in myTargets)
            {
                if (!combatants.Contains(target))
                {
                    combatants.Add(target);
                }
            }

            // 2단계: 속도 순으로 정렬
            var sortedCombatants = combatants
                .OrderByDescending(u => u.definition.speed)
                .ThenBy(u => Random.value) // 동일 속도면 랜덤
                .ToList();

            Debug.Log($"[전투] 참가자: {combatants.Count}명");

            // 3단계: 순차적으로 공격 처리
            foreach (var attacker in sortedCombatants)
            {
                // 이미 죽었으면 스킵
                if (!attacker.IsAlive)
                {
                    Debug.Log($"[전투] {attacker.definition.unitName}는 이미 사망하여 공격 불가");
                    continue;
                }

                // 공격 대상 찾기
                var targets = AttackResolver.GetAttackTargets(attacker, board);

                Debug.Log($"[전투] {attacker.definition.unitName}(속도:{attacker.definition.speed}) 공격 대상: {targets.Count}명");

                // 모든 사정거리 내 적 공격 ← 수정!
                foreach (var target in targets)
                {
                    // 이미 죽은 적은 스킵
                    if (!target.IsAlive)
                        continue;

                    int damage = attacker.definition.attackPower;
                    target.TakeDamage(damage);

                    Debug.Log($"[전투] {attacker.definition.unitName}이(가) {target.definition.unitName}을(를) 공격! ({damage} 데미지, 남은 HP: {target.currentHP})");

                    result.combatLog.Add((attacker, target, damage));

                    // 사망 처리
                    if (!target.IsAlive)
                    {
                        result.deadUnits.Add(target);
                        Debug.Log($"[전투] {target.definition.unitName} 사망!");
                    }
                }
            }

            return result;
        }
    }
}