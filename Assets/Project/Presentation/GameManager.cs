using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using Chess.Core;
using Chess.Simulation;

namespace Chess.Presentation
{
    public class GameManager : MonoBehaviour
    {
        [Header("Prefabs")]
        public GameObject unitViewPrefab;

        [Header("UI")]
        public VictoryUI victoryUI;
        public UnitInfoPanel unitInfoPanel;
        public EnemyUnitInfoPanel enemyUnitInfoPanel;
        public PlayerInfoUI myPlayerInfoUI;
        public PlayerInfoUI enemyPlayerInfoUI;

        [Header("Skill Slots")]
        public SkillSlotUI skillSlot1;
        public SkillSlotUI skillSlot2;

        [Header("Settings")]
        public Button menuButton;
        public InGameSettingsPanel inGameSettingsPanel;

        [Header("Deck")]
        public PlayerDeck player0Deck;
        public PlayerDeck player1Deck;

        private BoardState board;
        private Dictionary<int, UnitView> unitViews = new Dictionary<int, UnitView>();
        private int nextUnitID = 0;

        private UnitState selectedUnit;
        private List<Vector2Int> validMoves = new List<Vector2Int>();

        private SkillDefinition pendingActiveSkill;
        private int pendingActiveSkillSlot = -1;
        private bool isSelectingActiveSkillTarget = false;

        private SkillDefinition pendingPassiveSkill;
        private UnitState pendingPassiveCaster;
        private List<Vector2Int> pendingPassiveValidMoves;
        private bool isSelectingPassiveSkillTarget = false;
        private UnitState passiveSkillSelectedTarget = null;
        private bool passiveTargetConfirmed = false;

        private TurnManager turnManager;
        private BoardVisualizer boardVisualizer;
        private CombatAnimator combatAnimator;

        private bool hasMovedThisTurn = false;
        private int[] skillSlotRemainingUses = new int[2];

        void Start()
        {
            inGameSettingsPanel?.Initialize(this);
            menuButton?.onClick.AddListener(() => inGameSettingsPanel?.Open());

            turnManager = GetComponent<TurnManager>();
            boardVisualizer = GetComponent<BoardVisualizer>();
            combatAnimator = GetComponent<CombatAnimator>();

            if (DeckTransfer.Instance?.Player0Deck != null)
                player0Deck = DeckTransfer.Instance.Player0Deck;

            player1Deck = GenerateAIDeck();

            board = new BoardState();

            boardVisualizer.Initialize(board);
            boardVisualizer.CreateBoard();
            combatAnimator.Initialize(unitViews);
            turnManager.Initialize(board, ExecuteMove);

            if (player0Deck != null && player1Deck != null)
            {
                SetupUnitsFromDeck(player0Deck, 0);
                SetupUnitsFromDeck(player1Deck, 1);
            }
            else
            {
                Debug.LogError("[Deck] PlayerDeck not assigned!");
            }

            myPlayerInfoUI?.UpdateInfo(turnManager.Player0Data);
            enemyPlayerInfoUI?.UpdateInfo(turnManager.Player1Data);

            SetupSkillSlots();
            AudioManager.Instance?.Play("sfx_battle_start");
        }

        PlayerDeck GenerateAIDeck()
        {
            if (PlayerInventory.Instance == null)
            {
                Debug.LogError("[AI Deck] PlayerInventory가 없습니다.");
                return player1Deck;
            }

            var db = PlayerInventory.Instance.allUnitsDatabase;
            var king = db.Find(u => u.isKing);
            var pawn = db.Find(u => u.isPawn);
            var pool = db.Where(u => !u.isKing && !u.isPawn && u.rarity <= UnitRarity.Epic).ToList();

            if (king == null || pawn == null)
            {
                Debug.LogError("[AI Deck] King 또는 Pawn 유닛을 찾을 수 없습니다.");
                return player1Deck;
            }

            if (pool.Count == 0)
            {
                Debug.LogError("[AI Deck] Rare 이하 유닛이 없습니다.");
                return player1Deck;
            }

            var deck = ScriptableObject.CreateInstance<PlayerDeck>();
            deck.king = king;
            deck.pawn = pawn;
            deck.customUnits = new List<UnitDefinition>();
            for (int i = 0; i < deck.maxCustomUnits; i++)
                deck.customUnits.Add(pool[Random.Range(0, pool.Count)]);

            return deck;
        }

        void SetupSkillSlots()
        {
            if (player0Deck == null) return;

            var skills = player0Deck.selectedActiveSkills;

            var skill1 = skills != null && skills.Count > 0 ? skills[0] : null;
            var skill2 = skills != null && skills.Count > 1 ? skills[1] : null;

            skillSlotRemainingUses[0] = skill1?.maxUseCount ?? 0;
            skillSlotRemainingUses[1] = skill2?.maxUseCount ?? 0;

            skillSlot1?.SetSkill(skill1, _ => OnSkillUsed(0));
            skillSlot2?.SetSkill(skill2, _ => OnSkillUsed(1));
        }

        void OnSkillUsed(int slotIndex)
        {
            if (turnManager.CurrentPlayer != 0) return;
            if (hasMovedThisTurn) return;
            if (skillSlotRemainingUses[slotIndex] <= 0) return;

            SkillDefinition skill = slotIndex == 0 ? skillSlot1?.Skill : skillSlot2?.Skill;
            if (skill == null) return;

            if (skill.targetType == SkillTargetType.SingleEnemy ||
                skill.targetType == SkillTargetType.SingleAlly)
            {
                // 타게팅 확정 후 차감 — 취소 시 횟수 낭비 방지
                pendingActiveSkill = skill;
                pendingActiveSkillSlot = slotIndex;
                isSelectingActiveSkillTarget = true;
                ShowActiveSkillTargetHighlights(skill);
            }
            else
            {
                skillSlotRemainingUses[slotIndex]--;
                UpdateSkillSlotUI(slotIndex);
                AudioManager.Instance?.Play("sfx_skill_activate");
                SkillExecutor.ExecuteActive(skill, board);

                foreach (var view in unitViews.Values)
                {
                    view.UpdatePosition();
                    view.UpdateVisuals();
                }
            }
        }

        void ShowActiveSkillTargetHighlights(SkillDefinition skill)
        {
            bool targetAlly = skill.targetType == SkillTargetType.SingleAlly;
            var validPositions = board.GetAllUnits()
                .Where(u => u.IsAlive && (targetAlly ? u.ownerID == 0 : u.ownerID != 0))
                .Select(u => u.position)
                .ToList();
            boardVisualizer.ShowSkillTargetHighlights(validPositions, targetAlly, 0);
        }

        void UpdateSkillSlotUI(int slotIndex)
        {
            int remaining = skillSlotRemainingUses[slotIndex];
            if (slotIndex == 0) skillSlot1?.UpdateUses(remaining);
            else skillSlot2?.UpdateUses(remaining);
        }

        void Update()
        {
            if (turnManager.IsAIThinking) return;

            if (turnManager.CurrentPlayer == 0)
                HandleInput();
            else if (turnManager.UseAI)
                turnManager.StartAITurn();
        }

        void HandleInput()
        {
            // 우클릭으로 액티브 스킬 타게팅 취소 (횟수 차감 없음)
            if (Input.GetMouseButtonDown(1) && isSelectingActiveSkillTarget)
            {
                pendingActiveSkill = null;
                isSelectingActiveSkillTarget = false;
                boardVisualizer.ClearHighlights();
                return;
            }

            if (!Input.GetMouseButtonDown(0)) return;

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int gridPos = new Vector2Int(
                Mathf.FloorToInt(worldPos.x + 0.5f),
                Mathf.FloorToInt(worldPos.y + 0.5f)
            );

            if (!board.IsInBounds(gridPos)) return;

            if (isSelectingActiveSkillTarget)
            {
                HandleActiveSkillTargetSelection(gridPos);
                return;
            }

            if (isSelectingPassiveSkillTarget)
            {
                HandlePassiveSkillTargetSelection(gridPos);
                return;
            }

            HandleClick(gridPos);
        }

        void HandleActiveSkillTargetSelection(Vector2Int gridPos)
        {
            if (board.TryGetUnit(gridPos, out UnitState target))
            {
                bool isEnemy = pendingActiveSkill.targetType == SkillTargetType.SingleEnemy;
                bool validTarget = isEnemy ? target.ownerID != 0 : target.ownerID == 0;

                if (validTarget)
                {
                    skillSlotRemainingUses[pendingActiveSkillSlot]--;
                    UpdateSkillSlotUI(pendingActiveSkillSlot);
                    AudioManager.Instance?.Play("sfx_skill_activate");

                    // Pull 등 방향 계산이 필요한 스킬을 위해 보드에서 시전자 유닛을 탐색
                    UnitState activeCaster = null;
                    foreach (var u in board.GetAllUnits())
                        if (u.IsAlive && u.ownerID == 0 && u.definition.activeSkill == pendingActiveSkill)
                        { activeCaster = u; break; }

                    SkillExecutor.ExecuteActiveSingle(pendingActiveSkill, target, board, activeCaster);

                    foreach (var view in unitViews.Values)
                    {
                        view.UpdatePosition();
                        view.UpdateVisuals();
                    }
                }
            }

            pendingActiveSkill = null;
            isSelectingActiveSkillTarget = false;
            boardVisualizer.ClearHighlights();
        }

        void HandlePassiveSkillTargetSelection(Vector2Int gridPos)
        {
            if (!board.TryGetUnit(gridPos, out UnitState target)) return;
            if (!target.IsAlive) return;
            if (!pendingPassiveValidMoves.Contains(gridPos)) return;

            bool isEnemy = pendingPassiveSkill.targetType == SkillTargetType.SingleEnemy;
            bool validTarget = isEnemy
                ? target.ownerID != pendingPassiveCaster.ownerID
                : target.ownerID == pendingPassiveCaster.ownerID;

            if (!validTarget) return;

            passiveSkillSelectedTarget = target;
            passiveTargetConfirmed = true;
            isSelectingPassiveSkillTarget = false;
            boardVisualizer.ClearHighlights();
        }

        void HandleClick(Vector2Int position)
        {
            if (selectedUnit != null)
            {
                if (validMoves.Contains(position))
                    ExecuteMove(selectedUnit, position);

                selectedUnit = null;
                validMoves.Clear();
                boardVisualizer.ClearHighlights();
                unitInfoPanel?.Hide();
                return;
            }

            if (board.TryGetUnit(position, out UnitState unit))
            {
                if (unit.ownerID == turnManager.CurrentPlayer)
                {
                    selectedUnit = unit;
                    validMoves = MovementResolver.GetValidMoves(unit, board);
                    boardVisualizer.ShowHighlights(unit, validMoves);
                    unitInfoPanel?.Show(unit);
                    AudioManager.Instance?.Play("sfx_piece_select");
                }
                else
                {
                    enemyUnitInfoPanel?.Show(unit);
                }
            }
            else
            {
                enemyUnitInfoPanel?.Hide();
            }
        }

        void ExecuteMove(UnitState unit, Vector2Int targetPos)
        {
            AudioManager.Instance?.Play("sfx_piece_move");
            board.MoveUnit(unit, targetPos);
            unitViews[unit.id].UpdatePosition();
            hasMovedThisTurn = true;
            skillSlot1?.SetInteractable(false);
            skillSlot2?.SetInteractable(false);
            StartCoroutine(PlayMoveAndCombat(unit));
        }

        IEnumerator PlayMoveAndCombat(UnitState unit)
        {
            yield return StartCoroutine(TriggerPassiveSkills(SkillTrigger.OnMove, subject: unit));
            yield return StartCoroutine(TriggerPassiveSkills(SkillTrigger.OnAdjacent, subject: unit));
            yield return StartCoroutine(PlayCombatSequence(unit));
        }

        IEnumerator PlayCombatSequence(UnitState movedUnit)
        {
            var combatants = new List<UnitState> { movedUnit };

            foreach (var enemy in board.GetAllUnits().Where(u => u.ownerID != movedUnit.ownerID && u.IsAlive))
            {
                if (AttackResolver.GetAttackTargets(enemy, board).Contains(movedUnit))
                    combatants.Add(enemy);
            }

            foreach (var target in AttackResolver.GetAttackTargets(movedUnit, board))
            {
                if (!combatants.Contains(target))
                    combatants.Add(target);
            }

            // 전투 전 패시브 체크
            yield return StartCoroutine(TriggerPassiveSkills(SkillTrigger.OnAttack, subject: movedUnit));

            // 패시브로 죽은 유닛 처리
            var passiveDeadUnits = board.GetAllUnits()
                .Where(u => !u.IsAlive)
                .ToList();

            foreach (var deadUnit in passiveDeadUnits)
            {
                yield return StartCoroutine(TriggerPassiveSkills(SkillTrigger.OnDeath, subject: deadUnit));

                if (unitViews.ContainsKey(deadUnit.id))
                    yield return StartCoroutine(combatAnimator.PlayDeathEffect(unitViews[deadUnit.id]));

                DestroyUnit(deadUnit);

                if (deadUnit.definition.isKing)
                {
                    int winnerID = 1 - deadUnit.ownerID;
                    var winner = turnManager.GetPlayerData(winnerID);
                    var loser = turnManager.GetPlayerData(deadUnit.ownerID);
                    winner.wins++;
                    loser.losses++;

                    myPlayerInfoUI?.UpdateInfo(turnManager.Player0Data);
                    enemyPlayerInfoUI?.UpdateInfo(turnManager.Player1Data);

                    victoryUI?.Show(winner);
                    enabled = false;
                    yield break;
                }
            }

            // 패시브로 죽은 유닛이 전투 참여자에 있으면 제거
            combatants.RemoveAll(u => !u.IsAlive);

            var sortedCombatants = combatants
                .OrderByDescending(u => u.GetSpeed())
                .ThenBy(u => Random.value)
                .ToList();

            var deadUnits = new List<UnitState>();

            foreach (var attacker in sortedCombatants)
            {
                if (!attacker.IsAlive) continue;

                foreach (var target in AttackResolver.GetAttackTargets(attacker, board))
                {
                    if (!target.IsAlive) continue;

                    int rawDamage = attacker.GetAttack();
                    ApplyOneTimePassive(attacker, ref rawDamage);
                    int finalDamage = target.CalculateFinalDamage(rawDamage);
                    yield return StartCoroutine(combatAnimator.PlayAttackEffect(attacker, target, finalDamage));
                    AudioManager.Instance?.Play(GetAttackSoundId(attacker));

                    target.TakeDamage(finalDamage);
                    AudioManager.Instance?.Play(target.IsAlive ? "sfx_hit_light" : "sfx_hit_heavy");
                    yield return StartCoroutine(TriggerPassiveSkills(SkillTrigger.OnDamaged, subject: target, contextUnit: attacker));

                    if (unitViews.ContainsKey(target.id))
                        unitViews[target.id].UpdateVisuals();

                    if (!target.IsAlive)
                        deadUnits.Add(target);

                    yield return new WaitForSeconds(0.2f);
                }
            }

            foreach (var deadUnit in deadUnits)
            {
                yield return StartCoroutine(TriggerPassiveSkills(SkillTrigger.OnDeath, subject: deadUnit));

                if (unitViews.ContainsKey(deadUnit.id))
                    yield return StartCoroutine(combatAnimator.PlayDeathEffect(unitViews[deadUnit.id]));

                DestroyUnit(deadUnit);

                if (deadUnit.definition.isKing)
                {
                    int winnerID = 1 - deadUnit.ownerID;
                    var winner = turnManager.GetPlayerData(winnerID);
                    var loser = turnManager.GetPlayerData(deadUnit.ownerID);
                    winner.wins++;
                    loser.losses++;

                    myPlayerInfoUI?.UpdateInfo(turnManager.Player0Data);
                    enemyPlayerInfoUI?.UpdateInfo(turnManager.Player1Data);

                    victoryUI?.Show(winner);
                    enabled = false;
                    yield break;
                }
            }

            if (movedUnit.ownerID == turnManager.CurrentPlayer)
                yield return StartCoroutine(TriggerPassiveSkills(SkillTrigger.OnTurnEnd, playerID: movedUnit.ownerID));

            foreach (var unit in board.GetAllUnits())
                unit.TickBuffs();

            // DoT 틱 — 살아있는 유닛에게 지속 데미지 적용
            var dotDeadUnits = new List<UnitState>();
            bool anyDotDamage = false;
            foreach (var unit in board.GetAllUnits().Where(u => u.IsAlive).ToList())
            {
                int dotDmg = unit.TickDamageOverTime();
                if (dotDmg > 0)
                {
                    anyDotDamage = true;
                    yield return StartCoroutine(combatAnimator.PlayDotEffect(unit, dotDmg));
                    if (!unit.IsAlive)
                        dotDeadUnits.Add(unit);
                }
            }

            if (anyDotDamage)
                foreach (var view in unitViews.Values)
                    view.UpdateVisuals();

            foreach (var deadUnit in dotDeadUnits)
            {
                yield return StartCoroutine(TriggerPassiveSkills(SkillTrigger.OnDeath, subject: deadUnit));

                if (unitViews.ContainsKey(deadUnit.id))
                    yield return StartCoroutine(combatAnimator.PlayDeathEffect(unitViews[deadUnit.id]));

                DestroyUnit(deadUnit);

                if (deadUnit.definition.isKing)
                {
                    int winnerID = 1 - deadUnit.ownerID;
                    var winner = turnManager.GetPlayerData(winnerID);
                    var loser  = turnManager.GetPlayerData(deadUnit.ownerID);
                    winner.wins++;
                    loser.losses++;

                    myPlayerInfoUI?.UpdateInfo(turnManager.Player0Data);
                    enemyPlayerInfoUI?.UpdateInfo(turnManager.Player1Data);

                    victoryUI?.Show(winner);
                    enabled = false;
                    yield break;
                }
            }

            if (dotDeadUnits.Count > 0)
                foreach (var view in unitViews.Values)
                    view.UpdateVisuals();

            if (movedUnit.ownerID == turnManager.CurrentPlayer)
            {
                // 추가 행동 체크 — 현재 플레이어 유닛 중 extra action 보유자가 있으면 턴 전환 스킵
                UnitState extraActionUnit = null;
                foreach (var u in board.GetAllUnits())
                {
                    if (u.IsAlive && u.ownerID == movedUnit.ownerID && u.HasExtraAction)
                    {
                        extraActionUnit = u;
                        break;
                    }
                }

                if (extraActionUnit != null)
                {
                    extraActionUnit.ConsumeExtraAction();
                    hasMovedThisTurn = false;

                    if (turnManager.CurrentPlayer == 0)
                    {
                        if (skillSlot1 != null && skillSlotRemainingUses[0] > 0) skillSlot1.SetInteractable(true);
                        if (skillSlot2 != null && skillSlotRemainingUses[1] > 0) skillSlot2.SetInteractable(true);
                    }

                    yield break;
                }

                int prevPlayer = turnManager.CurrentPlayer;
                turnManager.SwitchTurn();

                yield return StartCoroutine(TriggerPassiveSkills(SkillTrigger.OnTurnStart, playerID: turnManager.CurrentPlayer));

                hasMovedThisTurn = false;

                // 플레이어 턴이 돌아왔을 때만 슬롯 재활성화
                if (turnManager.CurrentPlayer == 0)
                {
                    if (skillSlot1 != null && skillSlotRemainingUses[0] > 0) skillSlot1.SetInteractable(true);
                    if (skillSlot2 != null && skillSlotRemainingUses[1] > 0) skillSlot2.SetInteractable(true);
                }

                if (prevPlayer == 1 && turnManager.UseAI)
                    turnManager.SetAIThinking(false);

                if (turnManager.CurrentPlayer == 1 && turnManager.UseAI)
                    yield return new WaitForSeconds(0.5f);
            }
        }

        // subject 또는 playerID 기반으로 패시브 스킬을 트리거하는 단일 진입점
        // subject != null → 해당 유닛만 / playerID >= 0 → 해당 플레이어 전 유닛
        IEnumerator TriggerPassiveSkills(SkillTrigger trigger, UnitState subject = null, int playerID = -1, UnitState contextUnit = null)
        {
            List<UnitState> units;
            if (subject != null)
                units = new List<UnitState> { subject };
            else if (playerID >= 0)
                units = board.GetAllUnits().Where(u => u.IsAlive && u.ownerID == playerID).ToList();
            else
                yield break;

            bool anyFired = false;
            foreach (var unit in units)
            {
                if (unit == null || unit.definition?.passiveSkills == null) continue;
                if (trigger == SkillTrigger.OnAdjacent && !HasAdjacentUnit(unit)) continue;

                foreach (var skill in unit.definition.passiveSkills)
                {
                    if (skill == null || skill.skillType != SkillType.Passive) continue;
                    if (skill.trigger != trigger) continue;

                    if (trigger == SkillTrigger.OnAttack &&
                        (skill.targetType == SkillTargetType.SingleEnemy || skill.targetType == SkillTargetType.SingleAlly))
                    {
                        yield return StartCoroutine(WaitForPassiveSkillTarget(skill, unit));
                    }
                    else
                    {
                        bool allowDead = trigger == SkillTrigger.OnDeath;
                        SkillExecutor.ExecutePassive(skill, unit, board, contextUnit, allowDead);
                    }
                    anyFired = true;
                }
            }

            if (anyFired)
                foreach (var view in unitViews.Values)
                    view.UpdateVisuals();
        }

        IEnumerator WaitForPassiveSkillTarget(SkillDefinition skill, UnitState caster)
        {
            pendingPassiveSkill = skill;
            pendingPassiveCaster = caster;

            var attackTargets = AttackResolver.GetAttackTargets(caster, board);
            pendingPassiveValidMoves = attackTargets.Select(u => u.position).ToList();

            passiveSkillSelectedTarget = null;
            passiveTargetConfirmed = false;

            bool targetAlly = skill.targetType == SkillTargetType.SingleAlly;

            bool hasValidTarget = pendingPassiveValidMoves.Any(pos =>
            {
                if (!board.TryGetUnit(pos, out var unit)) return false;
                if (!unit.IsAlive) return false;
                bool isAlly = unit.ownerID == caster.ownerID;
                return targetAlly == isAlly;
            });

            if (!hasValidTarget)
            {
                ClearPassiveSkillState();
                yield break;
            }

            isSelectingPassiveSkillTarget = true;
            boardVisualizer.ShowSkillTargetHighlights(pendingPassiveValidMoves, targetAlly, caster.ownerID);

            while (!passiveTargetConfirmed)
                yield return null;

            if (passiveSkillSelectedTarget != null)
            {
                SkillExecutor.ExecuteActiveSingle(skill, passiveSkillSelectedTarget, board);

                foreach (var view in unitViews.Values)
                    view.UpdateVisuals();
            }

            ClearPassiveSkillState();
        }

        void ClearPassiveSkillState()
        {
            pendingPassiveSkill = null;
            pendingPassiveCaster = null;
            pendingPassiveValidMoves = null;
            passiveSkillSelectedTarget = null;
            passiveTargetConfirmed = false;
            isSelectingPassiveSkillTarget = false;
        }

        bool HasAdjacentUnit(UnitState unit)
        {
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    if (board.TryGetUnit(unit.position + new Vector2Int(dx, dy), out _))
                        return true;
                }
            return false;
        }

        void SetupUnitsFromDeck(PlayerDeck deck, int owner)
        {
            if (!deck.IsValid(out string error))
            {
                Debug.LogError($"[Deck] Player {owner} deck invalid: {error}");
                return;
            }

            int mainRow = owner == 0 ? 0 : 7;
            int pawnRow = owner == 0 ? 1 : 6;

            int kingLevel = owner == 0 ? deck.kingEnhanceLevel : 0;
            CreateUnit(deck.king, new Vector2Int(3, mainRow), owner, kingLevel);

            var positions = new List<int> { 0, 1, 2, 4, 5, 6, 7 };
            for (int i = 0; i < deck.customUnits.Count && i < positions.Count; i++)
            {
                int lvl = owner == 0 && i < deck.customUnitEnhanceLevels.Count
                    ? deck.customUnitEnhanceLevels[i] : 0;
                CreateUnit(deck.customUnits[i], new Vector2Int(positions[i], mainRow), owner, lvl);
            }

            int pawnLevel = owner == 0 ? deck.pawnEnhanceLevel : 0;
            for (int x = 0; x < 8; x++)
                CreateUnit(deck.pawn, new Vector2Int(x, pawnRow), owner, pawnLevel);
        }

        void CreateUnit(UnitDefinition def, Vector2Int position, int owner, int enhanceLevel = 0)
        {
            UnitState unit = new UnitState(nextUnitID++, def, position, owner, enhanceLevel);
            board.PlaceUnit(unit, position);
            unit.SetBoardReference(board);

            GameObject viewObj = Instantiate(unitViewPrefab, new Vector3(position.x, position.y, 0), Quaternion.identity);
            viewObj.name = $"{def.unitName}_{unit.id}";
            viewObj.transform.parent = transform;

            UnitView view = viewObj.GetComponent<UnitView>();
            view.Initialize(unit);
            // 적군(owner=1)은 스프라이트를 좌우 반전해 팀 구분
            view.spriteRenderer.flipX = owner == 1;

            unitViews[unit.id] = view;
        }

        void DestroyUnit(UnitState unit)
        {
            if (unitViews.TryGetValue(unit.id, out UnitView view))
            {
                Destroy(view.gameObject);
                unitViews.Remove(unit.id);
            }
            board.RemoveUnit(unit.position);
        }

        // 3-2: 게임당 1회 한정 데미지 배율 적용
        void ApplyOneTimePassive(UnitState attacker, ref int damage)
        {
            if (attacker.firstAttackUsed) return;
            if (attacker.definition.passiveSkills == null) return;

            foreach (var skill in attacker.definition.passiveSkills)
            {
                if (skill == null) continue;
                if (!skill.isOneTimeUse) continue;
                if (skill.trigger != SkillTrigger.OnAttack) continue;

                damage = Mathf.RoundToInt(damage * skill.damageMultiplier);
                attacker.firstAttackUsed = true;
                break;
            }
        }

        string GetAttackSoundId(UnitState unit)
        {
            var attrs = unit.definition.moveAttributes;
            if ((attrs & MovementAttribute.Bishop) != 0 || (attrs & MovementAttribute.Enchanter) != 0)
                return "sfx_attack_magic";
            if ((attrs & MovementAttribute.Rook) != 0)
                return "sfx_attack_ranged";
            return "sfx_attack_melee";
        }
    }
}