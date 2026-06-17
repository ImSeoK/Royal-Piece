using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Chess.Core;
using Chess.Simulation;

namespace Chess.Balance
{
    // 단판 결과 데이터
    public class UnitBattleRecord
    {
        public UnitDefinition definition;
        public int ownerID;
        public int damageDealt;
        public int damageReceived;
        public int turnsAlive;
        public int kills;
        public int passiveActivations;
        public bool survived;
    }

    public class BattleResult
    {
        public int winnerID = -1; // 0, 1, or -1 (무승부/최대턴 초과)
        public int totalTurns;
        public List<UnitBattleRecord> unitRecords = new List<UnitBattleRecord>();
    }

    // WaitForSeconds 없는 즉시 실행 시뮬레이션 엔진
    public class BattleSimulator
    {
        public const int MAX_TURNS = 300;

        private BoardState board;
        private BalanceAI ai0;
        private BalanceAI ai1;
        private int nextUnitID;
        private int currentPlayer;
        private int turnCount;

        private Dictionary<int, UnitBattleRecord> records;

        public BattleResult Run(PlayerDeck deck0, PlayerDeck deck1)
        {
            board = new BoardState();
            nextUnitID = 0;
            currentPlayer = 0;
            turnCount = 0;
            records = new Dictionary<int, UnitBattleRecord>();

            SetupDeck(deck0, 0);
            SetupDeck(deck1, 1);

            ai0 = new BalanceAI(board, 0);
            ai1 = new BalanceAI(board, 1);

            while (turnCount < MAX_TURNS)
            {
                var ai = currentPlayer == 0 ? ai0 : ai1;
                var (unit, targetPos) = ai.DecideMove();

                if (unit == null)
                {
                    // 이동 가능한 유닛 없음 → 턴 넘김
                    SwitchTurn();
                    continue;
                }

                if (ExecuteTurn(unit, targetPos))
                    break;
            }

            return BuildResult();
        }

        private BattleResult BuildResult()
        {
            var result = new BattleResult { totalTurns = turnCount };

            bool p0KingAlive = board.GetAllUnits().Any(u => u.IsAlive && u.ownerID == 0 && u.definition.isKing);
            bool p1KingAlive = board.GetAllUnits().Any(u => u.IsAlive && u.ownerID == 1 && u.definition.isKing);

            if (p1KingAlive && !p0KingAlive)       result.winnerID = 1;
            else if (p0KingAlive && !p1KingAlive)  result.winnerID = 0;
            else                                    result.winnerID = -1; // 둘 다 사망 or 최대턴

            foreach (var unit in board.GetAllUnits().Where(u => u.IsAlive))
                if (records.ContainsKey(unit.id))
                    records[unit.id].survived = true;

            result.unitRecords = records.Values.ToList();
            return result;
        }

        // 한 턴 실행. 킹이 죽으면 true 반환.
        private bool ExecuteTurn(UnitState unit, Vector2Int targetPos)
        {
            turnCount++;
            int actingPlayer = currentPlayer;

            board.MoveUnit(unit, targetPos);

            // 살아있는 유닛 전부 생존 턴 +1
            foreach (var u in board.GetAllUnits().Where(u => u.IsAlive))
                if (records.ContainsKey(u.id))
                    records[u.id].turnsAlive++;

            // 이동 관련 패시브
            TriggerPassives(SkillTrigger.OnMove, unit);
            if (HasAdjacentUnit(unit))
                TriggerPassives(SkillTrigger.OnAdjacent, unit);

            // 공격 전 패시브 (OnAttack)
            TriggerPassives(SkillTrigger.OnAttack, unit);

            // 패시브로 사망한 유닛 처리
            if (ProcessDeadUnits()) return true;

            // 전투 수행
            if (RunCombat(unit)) return true;

            // 턴 종료 패시브
            TriggerPassivesByPlayer(SkillTrigger.OnTurnEnd, actingPlayer);

            // 버프 턴 차감
            foreach (var u in board.GetAllUnits().ToList())
                u.TickBuffs();

            // DoT 데미지
            if (RunDoTTick()) return true;

            // 추가 행동 체크
            var extraUnit = board.GetAllUnits()
                .FirstOrDefault(u => u.IsAlive && u.ownerID == actingPlayer && u.HasExtraAction);
            if (extraUnit != null)
            {
                extraUnit.ConsumeExtraAction();
                return false; // 턴 전환 없이 계속
            }

            SwitchTurn();
            TriggerPassivesByPlayer(SkillTrigger.OnTurnStart, currentPlayer);
            return false;
        }

        // 전투 처리. 킹이 죽으면 true.
        private bool RunCombat(UnitState movedUnit)
        {
            var combatants = BuildCombatList(movedUnit);
            var sorted = combatants
                .Where(u => u.IsAlive)
                .OrderByDescending(u => u.GetSpeed())
                .ThenBy(_ => Random.value)
                .ToList();

            var combatDead = new List<UnitState>();

            foreach (var attacker in sorted)
            {
                if (!attacker.IsAlive) continue;

                foreach (var target in AttackResolver.GetAttackTargets(attacker, board))
                {
                    if (!target.IsAlive) continue;

                    int rawDmg = attacker.GetAttack();
                    ApplyOneTimePassive(attacker, ref rawDmg);
                    int finalDmg = target.CalculateFinalDamage(rawDmg);
                    target.TakeDamage(finalDmg);

                    if (records.ContainsKey(attacker.id)) records[attacker.id].damageDealt    += finalDmg;
                    if (records.ContainsKey(target.id))   records[target.id].damageReceived   += finalDmg;

                    TriggerPassives(SkillTrigger.OnDamaged, target, contextUnit: attacker);

                    if (!target.IsAlive)
                    {
                        if (!combatDead.Contains(target)) combatDead.Add(target);
                        if (records.ContainsKey(attacker.id)) records[attacker.id].kills++;
                    }
                }
            }

            foreach (var dead in combatDead)
            {
                TriggerPassives(SkillTrigger.OnDeath, dead, allowDead: true);
                board.RemoveUnit(dead.position);
                if (dead.definition.isKing) return true;
            }

            return false;
        }

        // DoT 틱. 킹이 죽으면 true.
        private bool RunDoTTick()
        {
            var dotDead = new List<UnitState>();
            foreach (var u in board.GetAllUnits().Where(u => u.IsAlive).ToList())
            {
                int dotDmg = u.TickDamageOverTime();
                if (dotDmg > 0)
                {
                    if (records.ContainsKey(u.id)) records[u.id].damageReceived += dotDmg;
                    if (!u.IsAlive) dotDead.Add(u);
                }
            }

            foreach (var dead in dotDead)
            {
                TriggerPassives(SkillTrigger.OnDeath, dead, allowDead: true);
                board.RemoveUnit(dead.position);
                if (dead.definition.isKing) return true;
            }

            return false;
        }

        // hp <= 0 인데 보드에 남아있는 유닛 처리. 킹이 죽으면 true.
        private bool ProcessDeadUnits()
        {
            var dead = board.GetAllUnits().Where(u => !u.IsAlive).ToList();
            foreach (var d in dead)
            {
                TriggerPassives(SkillTrigger.OnDeath, d, allowDead: true);
                board.RemoveUnit(d.position);
                if (d.definition.isKing) return true;
            }
            return false;
        }

        // 이동한 유닛 + 공격 범위가 겹치는 유닛 목록 생성
        private List<UnitState> BuildCombatList(UnitState movedUnit)
        {
            var list = new List<UnitState> { movedUnit };

            foreach (var enemy in board.GetAllUnits().Where(u => u.ownerID != movedUnit.ownerID && u.IsAlive))
                if (AttackResolver.GetAttackTargets(enemy, board).Contains(movedUnit))
                    list.Add(enemy);

            foreach (var target in AttackResolver.GetAttackTargets(movedUnit, board))
                if (!list.Contains(target))
                    list.Add(target);

            return list;
        }

        // 단일 유닛의 특정 트리거 패시브 실행
        private void TriggerPassives(SkillTrigger trigger, UnitState subject,
                                     UnitState contextUnit = null, bool allowDead = false)
        {
            if (subject?.definition?.passiveSkills == null) return;
            if (!allowDead && !subject.IsAlive) return;

            foreach (var skill in subject.definition.passiveSkills)
            {
                if (skill == null || skill.skillType != SkillType.Passive) continue;
                if (skill.trigger != trigger) continue;

                // SingleEnemy/SingleAlly OnAttack → 자동 랜덤 타겟 선정
                if (trigger == SkillTrigger.OnAttack &&
                    (skill.targetType == SkillTargetType.SingleEnemy ||
                     skill.targetType == SkillTargetType.SingleAlly))
                {
                    ExecutePassiveSingleAuto(skill, subject);
                }
                else
                {
                    SkillExecutor.ExecutePassive(skill, subject, board, contextUnit, allowDead);
                }

                if (records.ContainsKey(subject.id))
                    records[subject.id].passiveActivations++;
            }
        }

        // 플레이어 전체 유닛 패시브 트리거 (OnTurnStart / OnTurnEnd)
        private void TriggerPassivesByPlayer(SkillTrigger trigger, int playerID)
        {
            var units = board.GetAllUnits().Where(u => u.IsAlive && u.ownerID == playerID).ToList();
            foreach (var u in units)
                TriggerPassives(trigger, u);
        }

        // SingleEnemy/SingleAlly 패시브: 공격 대상 중 랜덤 선정 후 즉시 실행
        private void ExecutePassiveSingleAuto(SkillDefinition skill, UnitState caster)
        {
            bool targetAlly = skill.targetType == SkillTargetType.SingleAlly;
            var pool = AttackResolver.GetAttackTargets(caster, board)
                .Where(u => u.IsAlive && (u.ownerID == caster.ownerID) == targetAlly)
                .ToList();
            if (pool.Count == 0) return;
            SkillExecutor.ExecuteActiveSingle(skill, pool[Random.Range(0, pool.Count)], board, caster);
        }

        // 3-2: 게임당 1회 한정 데미지 배율 적용
        private void ApplyOneTimePassive(UnitState attacker, ref int damage)
        {
            if (attacker.firstAttackUsed || attacker.definition.passiveSkills == null) return;
            foreach (var skill in attacker.definition.passiveSkills)
            {
                if (skill == null || !skill.isOneTimeUse || skill.trigger != SkillTrigger.OnAttack) continue;
                damage = Mathf.RoundToInt(damage * skill.damageMultiplier);
                attacker.firstAttackUsed = true;
                break;
            }
        }

        private bool HasAdjacentUnit(UnitState unit)
        {
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    if (board.TryGetUnit(unit.position + new Vector2Int(dx, dy), out _)) return true;
                }
            return false;
        }

        private void SwitchTurn() => currentPlayer = 1 - currentPlayer;

        // GameManager.SetupUnitsFromDeck과 동일한 배치
        private void SetupDeck(PlayerDeck deck, int owner)
        {
            if (deck == null) return;
            int mainRow = owner == 0 ? 0 : 7;
            int pawnRow = owner == 0 ? 1 : 6;

            int kingLvl = owner == 0 ? deck.kingEnhanceLevel : 0;
            CreateUnit(deck.king, new Vector2Int(3, mainRow), owner, kingLvl);

            var xPositions = new[] { 0, 1, 2, 4, 5, 6, 7 };
            for (int i = 0; i < deck.customUnits.Count && i < xPositions.Length; i++)
            {
                int lvl = owner == 0 && i < deck.customUnitEnhanceLevels.Count
                    ? deck.customUnitEnhanceLevels[i] : 0;
                CreateUnit(deck.customUnits[i], new Vector2Int(xPositions[i], mainRow), owner, lvl);
            }

            int pawnLvl = owner == 0 ? deck.pawnEnhanceLevel : 0;
            for (int x = 0; x < 8; x++)
                CreateUnit(deck.pawn, new Vector2Int(x, pawnRow), owner, pawnLvl);
        }

        private void CreateUnit(UnitDefinition def, Vector2Int position, int owner, int level = 0)
        {
            var unit = new UnitState(nextUnitID++, def, position, owner, level);
            board.PlaceUnit(unit, position);
            unit.SetBoardReference(board);
            records[unit.id] = new UnitBattleRecord { definition = def, ownerID = owner };
        }
    }
}
