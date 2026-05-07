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
        private Dictionary<SkillDefinition, int> skillRemainingUses = new Dictionary<SkillDefinition, int>();

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
            var pool = db.Where(u => !u.isKing && !u.isPawn && u.rarity <= UnitRarity.Rare).ToList();

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

            skillRemainingUses.Clear();

            var skill1 = skills != null && skills.Count > 0 ? skills[0] : null;
            var skill2 = skills != null && skills.Count > 1 ? skills[1] : null;

            if (skill1 != null) skillRemainingUses[skill1] = skill1.maxUseCount;
            if (skill2 != null) skillRemainingUses[skill2] = skill2.maxUseCount;

            skillSlot1?.SetSkill(skill1, OnSkillUsed);
            skillSlot2?.SetSkill(skill2, OnSkillUsed);
        }

        void OnSkillUsed(SkillDefinition skill)
        {
            if (turnManager.CurrentPlayer != 0) return;
            if (hasMovedThisTurn) return;
            if (!skillRemainingUses.TryGetValue(skill, out int remaining) || remaining <= 0) return;

            if (skill.targetType == SkillTargetType.SingleEnemy ||
                skill.targetType == SkillTargetType.SingleAlly)
            {
                // 타게팅 확정 후 차감 — 취소 시 횟수 낭비 방지
                pendingActiveSkill = skill;
                isSelectingActiveSkillTarget = true;
                ShowActiveSkillTargetHighlights(skill);
            }
            else
            {
                skillRemainingUses[skill]--;
                UpdateSkillSlotUI(skill);
                SkillExecutor.ExecuteActive(skill, board);

                foreach (var view in unitViews.Values)
                    view.UpdateVisuals();
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

        void UpdateSkillSlotUI(SkillDefinition skill)
        {
            int remaining = skillRemainingUses.TryGetValue(skill, out int r) ? r : 0;
            if (skillSlot1 != null && skillSlot1.Skill == skill) skillSlot1.UpdateUses(remaining);
            if (skillSlot2 != null && skillSlot2.Skill == skill) skillSlot2.UpdateUses(remaining);
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
                    skillRemainingUses[pendingActiveSkill]--;
                    UpdateSkillSlotUI(pendingActiveSkill);
                    SkillExecutor.ExecuteActiveSingle(pendingActiveSkill, target, board);

                    foreach (var view in unitViews.Values)
                        view.UpdateVisuals();
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
            board.MoveUnit(unit, targetPos);
            unitViews[unit.id].UpdatePosition();
            hasMovedThisTurn = true;
            skillSlot1?.SetInteractable(false);
            skillSlot2?.SetInteractable(false);
            StartCoroutine(PlayMoveAndCombat(unit));
        }

        IEnumerator PlayMoveAndCombat(UnitState unit)
        {
            yield return StartCoroutine(TriggerPassiveSkillsAuto(unit, SkillTrigger.OnMove, null));
            yield return StartCoroutine(TriggerPassiveSkillsAuto(unit, SkillTrigger.OnAdjacent, null));
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
            yield return StartCoroutine(TriggerPassiveSkillsBeforeCombat(movedUnit));

            // 패시브로 죽은 유닛 처리
            var passiveDeadUnits = board.GetAllUnits()
                .Where(u => !u.IsAlive)
                .ToList();

            foreach (var deadUnit in passiveDeadUnits)
            {
                yield return StartCoroutine(TriggerPassiveSkillsAuto(deadUnit, SkillTrigger.OnDeath, null));

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

                    yield return StartCoroutine(combatAnimator.PlayAttackEffect(attacker, target));

                    target.TakeDamage(attacker.GetAttack());
                    yield return StartCoroutine(TriggerPassiveSkillsAuto(target, SkillTrigger.OnDamaged, attacker));

                    if (unitViews.ContainsKey(target.id))
                        unitViews[target.id].UpdateVisuals();

                    if (!target.IsAlive)
                        deadUnits.Add(target);

                    yield return new WaitForSeconds(0.2f);
                }
            }

            foreach (var deadUnit in deadUnits)
            {
                yield return StartCoroutine(TriggerPassiveSkillsAuto(deadUnit, SkillTrigger.OnDeath, null));

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

            foreach (var unit in board.GetAllUnits())
                unit.TickBuffs();

            if (movedUnit.ownerID == turnManager.CurrentPlayer)
            {
                int prevPlayer = turnManager.CurrentPlayer;
                turnManager.SwitchTurn();

                hasMovedThisTurn = false;

                // 플레이어 턴이 돌아왔을 때만 슬롯 재활성화
                if (turnManager.CurrentPlayer == 0)
                {
                    if (skillSlot1 != null && skillSlot1.Skill != null && skillRemainingUses.TryGetValue(skillSlot1.Skill, out int r1) && r1 > 0)
                        skillSlot1.SetInteractable(true);
                    if (skillSlot2 != null && skillSlot2.Skill != null && skillRemainingUses.TryGetValue(skillSlot2.Skill, out int r2) && r2 > 0)
                        skillSlot2.SetInteractable(true);
                }

                if (prevPlayer == 1 && turnManager.UseAI)
                    turnManager.SetAIThinking(false);

                if (turnManager.CurrentPlayer == 1 && turnManager.UseAI)
                    yield return new WaitForSeconds(0.5f);
            }
        }

        IEnumerator TriggerPassiveSkillsBeforeCombat(UnitState movedUnit)
        {
            if (movedUnit.definition.passiveSkills == null) yield break;

            foreach (var skill in movedUnit.definition.passiveSkills)
            {
                if (skill == null) continue;
                if (skill.skillType != SkillType.Passive) continue;
                if (skill.trigger != SkillTrigger.OnAttack) continue;

                if (skill.targetType == SkillTargetType.SingleEnemy ||
                    skill.targetType == SkillTargetType.SingleAlly)
                {
                    yield return StartCoroutine(WaitForPassiveSkillTarget(skill, movedUnit));
                }
                else
                {
                    SkillExecutor.ExecutePassive(skill, movedUnit, board);

                    foreach (var view in unitViews.Values)
                        view.UpdateVisuals();
                }
            }
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

        IEnumerator TriggerPassiveSkillsAuto(UnitState unit, SkillTrigger trigger, UnitState other)
        {
            if (unit == null || unit.definition.passiveSkills == null) yield break;

            // OnAdjacent: 인접 유닛이 없으면 발동하지 않음
            if (trigger == SkillTrigger.OnAdjacent && !HasAdjacentUnit(unit))
                yield break;

            bool anyFired = false;
            foreach (var skill in unit.definition.passiveSkills)
            {
                if (skill == null || skill.skillType != SkillType.Passive) continue;
                if (skill.trigger != trigger) continue;

                SkillExecutor.ExecutePassive(skill, unit, board);
                anyFired = true;
            }

            if (anyFired)
                foreach (var view in unitViews.Values)
                    view.UpdateVisuals();
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

            CreateUnit(deck.king, new Vector2Int(3, mainRow), owner);

            var positions = new List<int> { 0, 1, 2, 4, 5, 6, 7 };
            for (int i = 0; i < deck.customUnits.Count && i < positions.Count; i++)
                CreateUnit(deck.customUnits[i], new Vector2Int(positions[i], mainRow), owner);

            for (int x = 0; x < 8; x++)
                CreateUnit(deck.pawn, new Vector2Int(x, pawnRow), owner);
        }

        void CreateUnit(UnitDefinition def, Vector2Int position, int owner)
        {
            UnitState unit = new UnitState(nextUnitID++, def, position, owner);
            board.PlaceUnit(unit, position);

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
    }
}