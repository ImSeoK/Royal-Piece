using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.UI;
using Chess.Core;
using Chess.Simulation;

namespace Chess.Presentation
{
    public class GameManager : MonoBehaviour
    {
        [Header("Prefabs")]
        public GameObject unitViewPrefab;
        public BuffIconConfig buffIconConfig;

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

        [Header("관전 모드")]
        public int spectatorMatchCount = 5;
        public TMPro.TextMeshProUGUI matchInfoText;

        // 온라인 모드
        private bool _isOnlineMode;
        private int  _localPlayerID;          // Host=0, Client=1
        private bool _waitingForOnlineSetup;

        // 게임 이벤트 로그 (Phase 5 Cloud Function 검증용)
        [System.Serializable]
        struct MoveLog
        {
            public int    turn;
            public int    playerID;
            public string eventType;    // "move" | "skill_range" | "skill_single" | "passive_target"
            public string unitName;
            public int    fromX, fromY;
            public int    toX,   toY;
            public string skillName;    // skill_* / passive_target 이벤트용
            public int    targetUnitId; // skill_single / passive_target용 (-1 = 없음)
        }
        private List<MoveLog> _moveLogs = new List<MoveLog>();
        private int _turnCount;

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

        // 턴당 패시브 1회 제한 추적 (unitId, skillName)
        private HashSet<(int, string)> _passivesFiredThisTurn = new HashSet<(int, string)>();

        [Header("랭크 패널")]

        private int completedMatches = 0;
        private List<int> matchWinners = new List<int>();
        private bool isRankedMode = false;

        void Start()
        {
            menuButton?.onClick.AddListener(() => inGameSettingsPanel?.Open());

            turnManager = GetComponent<TurnManager>();
            boardVisualizer = GetComponent<BoardVisualizer>();
            combatAnimator = GetComponent<CombatAnimator>();

            if (DeckTransfer.Instance?.Player0Deck != null)
                player0Deck = DeckTransfer.Instance.Player0Deck;

            isRankedMode  = DeckTransfer.Instance?.CurrentMode == GameMode.Ranked;
            _isOnlineMode = DeckTransfer.Instance?.CurrentMode != GameMode.AI;
            inGameSettingsPanel?.Initialize(this, isRankedMode);

            if (_isOnlineMode)
            {
                _waitingForOnlineSetup = true;
                turnManager.useAI = false;
                // NetworkGameBridge.Spawned() 이후 OnDecksReady() 호출까지 대기
            }
            else
            {
                BoardVisualizer.IsFlipped = false;  // 오프라인: P0가 항상 아래
                player1Deck = GenerateAIDeck();
                InitializeGame();
            }
        }

        /// <summary>온라인 모드: NetworkGameBridge에서 양쪽 덱 준비 완료 시 호출.</summary>
        public void OnDecksReady(PlayerDeck p0Deck, PlayerDeck p1Deck, int localPlayerID)
        {
            _localPlayerID         = localPlayerID;
            _waitingForOnlineSetup = false;
            player0Deck            = p0Deck;
            player1Deck            = p1Deck;
            BoardVisualizer.IsFlipped = (localPlayerID == 1);  // Player 2는 보드 반전
            Debug.Log($"[GameManager] 온라인 덱 준비 완료 LocalPlayer={localPlayerID} IsFlipped={BoardVisualizer.IsFlipped}");
            InitializeGame();
        }

        /// <summary>온라인 모드: 항복 버튼 클릭 시 RPC 전송.</summary>
        public void NotifyForfeit()
        {
            if (_isOnlineMode)
                NetworkGameBridge.Instance?.RPC_Surrender(_localPlayerID);
        }

        /// <summary>항복한 플레이어에게 DEFEAT 패널 표시 + 전적/랭크 처리.</summary>
        public void ShowForfeitDefeat(bool rankedMode)
        {
            enabled = false;

            int opponentID = 1 - _localPlayerID;  // 항복자 기준 상대 = 승자
            int rankDelta  = rankedMode ? -8 : 0;

            PlayerPrefs.SetInt("stat_losses", PlayerPrefs.GetInt("stat_losses", 0) + 1);
            PlayerPrefs.SetInt("stat_total",  PlayerPrefs.GetInt("stat_total",  0) + 1);
            PlayerPrefs.Save();

            if (!_isOnlineMode) PlayerInventory.Instance?.AddCurrency(15);
            if (rankedMode && !_isOnlineMode) Chess.Core.RankSystem.AddLoss();

            _ = SubmitGameLog(opponentID);
            victoryUI?.Show(opponentID, 15, 0, rankDelta, rankedMode, _localPlayerID, !_isOnlineMode);
        }

        /// <summary>온라인 모드: 상대가 항복 RPC를 보냈을 때 호출 → 본인은 승리.</summary>
        public void ReceiveSurrender(int surrenderingPlayerID)
        {
            if (surrenderingPlayerID == _localPlayerID) return;  // 자기 자신이 보낸 항복은 무시
            Debug.Log($"[GameManager] 상대 항복 수신 → 승리");
            enabled = false;
            int goldReward = 40;
            int rankDelta  = isRankedMode ? 15 : 0;
            if (!_isOnlineMode) PlayerInventory.Instance?.AddCurrency(goldReward);
            if (isRankedMode && !_isOnlineMode) Chess.Core.RankSystem.AddWin();
            PlayerPrefs.SetInt("stat_wins",  PlayerPrefs.GetInt("stat_wins",  0) + 1);
            PlayerPrefs.SetInt("stat_total", PlayerPrefs.GetInt("stat_total", 0) + 1);
            PlayerPrefs.Save();
            _ = SubmitGameLog(_localPlayerID);
            victoryUI?.Show(_localPlayerID, goldReward, 0, rankDelta, isRankedMode, _localPlayerID, !_isOnlineMode);
        }

        /// <summary>온라인 모드: 상대방 이동 RPC 수신 시 호출.</summary>
        public void ExecuteRemoteMove(int unitId, Vector2Int targetPos)
        {
            UnitState unit = null;
            foreach (var u in board.GetAllUnits())
                if (u.id == unitId) { unit = u; break; }

            if (unit == null)
            {
                Debug.LogWarning($"[GameManager] ExecuteRemoteMove: unitId={unitId} 없음");
                return;
            }
            ExecuteMove(unit, targetPos);
        }

        void InitializeGame()
        {
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
            UpdateMatchCountText();
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
            if (turnManager.CurrentPlayer != _localPlayerID) return;
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

                // 시전 사운드
                string castSfx = !string.IsNullOrEmpty(skill.castSoundId) ? skill.castSoundId : "sfx_skill_activate";
                AudioManager.Instance?.Play(castSfx);

                // 시전자 위치 이펙트
                UnitState rangeCaster = board.GetAllUnits().FirstOrDefault(u => u.IsAlive && u.ownerID == _localPlayerID && u.definition.activeSkill == skill);
                if (skill.castEffectPrefab != null && rangeCaster != null && unitViews.ContainsKey(rangeCaster.id))
                    StartCoroutine(combatAnimator.PlaySkillEffect(skill.castEffectPrefab, unitViews[rangeCaster.id].transform.position));

                // 영향받는 타겟 계산 (hitEffect / hitSound 재생용)
                bool targetEnemy = skill.targetType == SkillTargetType.RangeEnemy;
                var rangeTargets = board.GetAllUnits()
                    .Where(u => u.IsAlive && (targetEnemy ? u.ownerID != _localPlayerID : u.ownerID == _localPlayerID))
                    .ToList();

                SkillExecutor.ExecuteActive(skill, board, _localPlayerID);

                _moveLogs.Add(new MoveLog { turn = _turnCount++, playerID = _localPlayerID, eventType = "skill_range", skillName = skill.skillName, targetUnitId = -1 });

                if (_isOnlineMode)
                    NetworkGameBridge.Instance?.RPC_SendActiveSkill(_localPlayerID, skill.skillName, -1);

                foreach (var t in rangeTargets)
                {
                    if (!unitViews.ContainsKey(t.id)) continue;
                    if (!string.IsNullOrEmpty(skill.hitSoundId))
                        AudioManager.Instance?.Play(skill.hitSoundId);
                    if (skill.hitEffectPrefab != null)
                        StartCoroutine(combatAnimator.PlaySkillEffect(skill.hitEffectPrefab, unitViews[t.id].transform.position));
                }

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
            if (_waitingForOnlineSetup) return;

            // 유닛 정보 표시는 턴/AI 상태 무관하게 항상 처리
            HandleInfoClick();

            if (turnManager.IsAIThinking) return;

            if (turnManager.IsSpectatorMode)
                turnManager.StartAITurn();
            else if (turnManager.CurrentPlayer == _localPlayerID)
                HandleInput();
            else if (turnManager.UseAI)
                turnManager.StartAITurn();
            // else: 온라인 상대 턴 → RPC 수신 대기
        }

        void HandleInfoClick()
        {
            if (!Input.GetMouseButtonDown(0)) return;

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int gridPos = BoardVisualizer.WorldToGrid(worldPos);

            if (!board.IsInBounds(gridPos)) return;

            if (board.TryGetUnit(gridPos, out UnitState unit))
            {
                if (unit.ownerID == _localPlayerID)
                    unitInfoPanel?.Show(unit);
                else
                    enemyUnitInfoPanel?.Show(unit);
            }
            else
            {
                unitInfoPanel?.Hide();
                enemyUnitInfoPanel?.Hide();
            }
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
            Vector2Int gridPos = BoardVisualizer.WorldToGrid(worldPos);

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

            if (hasMovedThisTurn) return;

            HandleClick(gridPos);
        }

        void HandleActiveSkillTargetSelection(Vector2Int gridPos)
        {
            if (board.TryGetUnit(gridPos, out UnitState target))
            {
                bool isEnemy = pendingActiveSkill.targetType == SkillTargetType.SingleEnemy;
                bool validTarget = isEnemy ? target.ownerID != _localPlayerID : target.ownerID == _localPlayerID;

                if (validTarget)
                {
                    skillSlotRemainingUses[pendingActiveSkillSlot]--;
                    UpdateSkillSlotUI(pendingActiveSkillSlot);
                    string singleCastSfx = !string.IsNullOrEmpty(pendingActiveSkill.castSoundId) ? pendingActiveSkill.castSoundId : "sfx_skill_activate";
                    AudioManager.Instance?.Play(singleCastSfx);
                    if (!string.IsNullOrEmpty(pendingActiveSkill.hitSoundId))
                        AudioManager.Instance?.Play(pendingActiveSkill.hitSoundId);

                    // Pull 등 방향 계산이 필요한 스킬을 위해 보드에서 시전자 유닛을 탐색
                    UnitState activeCaster = null;
                    foreach (var u in board.GetAllUnits())
                        if (u.IsAlive && u.ownerID == _localPlayerID && u.definition.activeSkill == pendingActiveSkill)
                        { activeCaster = u; break; }

                    if (pendingActiveSkill.castEffectPrefab != null && activeCaster != null && unitViews.ContainsKey(activeCaster.id))
                        StartCoroutine(combatAnimator.PlaySkillEffect(pendingActiveSkill.castEffectPrefab, unitViews[activeCaster.id].transform.position));
                    if (pendingActiveSkill.hitEffectPrefab != null && unitViews.ContainsKey(target.id))
                        StartCoroutine(combatAnimator.PlaySkillEffect(pendingActiveSkill.hitEffectPrefab, unitViews[target.id].transform.position));

                    SkillExecutor.ExecuteActiveSingle(pendingActiveSkill, target, board, activeCaster);

                    _moveLogs.Add(new MoveLog { turn = _turnCount++, playerID = _localPlayerID, eventType = "skill_single", skillName = pendingActiveSkill.skillName, targetUnitId = target.id });

                    if (_isOnlineMode)
                        NetworkGameBridge.Instance?.RPC_SendActiveSkill(_localPlayerID, pendingActiveSkill.skillName, target.id);

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
                {
                    ExecuteMove(selectedUnit, position);
                    selectedUnit = null;
                    validMoves.Clear();
                    boardVisualizer.ClearHighlights();
                    return;
                }

                // 이동 아닌 클릭 → 선택 해제 후 클릭 위치 유닛 정보 표시로 fall-through
                selectedUnit = null;
                validMoves.Clear();
                boardVisualizer.ClearHighlights();
            }

            if (board.TryGetUnit(position, out UnitState unit))
            {
                if (unit.ownerID == turnManager.CurrentPlayer)
                {
                    selectedUnit = unit;
                    validMoves = MovementResolver.GetValidMoves(unit, board);
                    boardVisualizer.ShowHighlights(unit, validMoves);
                    AudioManager.Instance?.Play("sfx_piece_select");
                }
            }
        }

        void ExecuteMove(UnitState unit, Vector2Int targetPos)
        {
            // 이동 로그 기록 (Phase 5 Cloud Function 검증용)
            _moveLogs.Add(new MoveLog
            {
                turn         = _turnCount++,
                playerID     = turnManager.CurrentPlayer,
                eventType    = "move",
                unitName     = unit.definition.unitName,
                fromX        = unit.position.x,
                fromY        = unit.position.y,
                toX          = targetPos.x,
                toY          = targetPos.y,
                targetUnitId = -1,
            });

            // 온라인 모드 + 내 턴이면 상대에게 RPC 전송
            if (_isOnlineMode && turnManager.CurrentPlayer == _localPlayerID)
                NetworkGameBridge.Instance?.RPC_SendMove(_localPlayerID, unit.id, targetPos.x, targetPos.y);

            AudioManager.Instance?.Play("sfx_piece_move");
            board.MoveUnit(unit, targetPos);
            unitViews[unit.id].UpdatePosition();
            if (turnManager.CurrentPlayer == _localPlayerID)
            {
                hasMovedThisTurn = true;
                skillSlot1?.SetInteractable(false);
                skillSlot2?.SetInteractable(false);
            }
            StartCoroutine(PlayMoveAndCombat(unit));
        }

        IEnumerator PlayMoveAndCombat(UnitState unit)
        {
            yield return StartCoroutine(TriggerPassiveSkills(SkillTrigger.OnMove, subject: unit));
            yield return StartCoroutine(TriggerPassiveSkills(SkillTrigger.OnAdjacent, subject: unit));
            yield return new WaitForSeconds(0.3f);
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
            yield return new WaitForSeconds(0.2f);

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
                    yield return StartCoroutine(HandleKingDeath(deadUnit));
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
            var damageShareEffectShown = new System.Collections.Generic.HashSet<int>();

            foreach (var attacker in sortedCombatants)
            {
                if (!attacker.IsAlive) continue;

                foreach (var target in AttackResolver.GetAttackTargets(attacker, board))
                {
                    if (!target.IsAlive) continue;

                    int rawDamage = attacker.GetAttack();
                    ApplyOneTimePassive(attacker, ref rawDamage);

                    // OnDamaged 패시브를 데미지 적용 전에 발동 (버프가 이번 피해에 반영됨)
                    yield return StartCoroutine(TriggerPassiveSkills(SkillTrigger.OnDamaged, subject: target, contextUnit: attacker));

                    // 패시브 버프 반영 후 finalDamage 계산
                    int finalDamage = target.CalculateFinalDamage(rawDamage);
                    ApplySpeedPassiveReduction(attacker, target, ref finalDamage);

                    UnitState shareProvider; float shareRate; SkillDefinition shareSkill;
                    bool hasShare = SkillExecutor.TryGetDamageShareProvider(target, board, out shareProvider, out shareRate, out shareSkill);
                    int sharedDamage = hasShare ? Mathf.RoundToInt(finalDamage * shareRate) : 0;
                    int targetDamage = finalDamage - sharedDamage;

                    yield return StartCoroutine(combatAnimator.PlayAttackEffect(attacker, target, targetDamage));
                    AudioManager.Instance?.Play(GetAttackSoundId(attacker));

                    if (hasShare)
                    {
                        target.TakeDamage(targetDamage);
                        shareProvider.TakeDamage(sharedDamage);
                        if (unitViews.ContainsKey(shareProvider.id))
                            combatAnimator.SpawnDamagePopup(sharedDamage, unitViews[shareProvider.id].transform.position);
                        if (!damageShareEffectShown.Contains(target.id))
                        {
                            if (shareSkill.hitEffectPrefab != null)
                            {
                                if (unitViews.ContainsKey(target.id))
                                    StartCoroutine(combatAnimator.PlaySkillEffect(shareSkill.hitEffectPrefab, unitViews[target.id].transform.position));
                                if (unitViews.ContainsKey(shareProvider.id))
                                    StartCoroutine(combatAnimator.PlaySkillEffect(shareSkill.hitEffectPrefab, unitViews[shareProvider.id].transform.position));
                            }
                            if (!string.IsNullOrEmpty(shareSkill.hitSoundId))
                                AudioManager.Instance?.Play(shareSkill.hitSoundId);
                            damageShareEffectShown.Add(target.id);
                        }
                        if (unitViews.ContainsKey(shareProvider.id))
                            unitViews[shareProvider.id].UpdateVisuals();
                    }
                    else
                    {
                        target.TakeDamage(finalDamage);
                    }
                    AudioManager.Instance?.Play(target.IsAlive ? "sfx_hit_light" : "sfx_hit_heavy");

                    if (unitViews.ContainsKey(target.id))
                        unitViews[target.id].UpdateVisuals();

                    if (!target.IsAlive)
                        deadUnits.Add(target);

                    yield return new WaitForSeconds(0.4f);
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
                    yield return StartCoroutine(HandleKingDeath(deadUnit));
                    yield break;
                }
            }

            if (movedUnit.ownerID == turnManager.CurrentPlayer)
                yield return StartCoroutine(TriggerPassiveSkills(SkillTrigger.OnTurnEnd, playerID: movedUnit.ownerID));

            foreach (var unit in board.GetAllUnits())
                unit.TickBuffs();

            foreach (var view in unitViews.Values)
                view.UpdateVisuals();

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
                    yield return StartCoroutine(HandleKingDeath(deadUnit));
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

                    if (turnManager.CurrentPlayer == _localPlayerID)
                    {
                        if (skillSlot1 != null && skillSlotRemainingUses[0] > 0) skillSlot1.SetInteractable(true);
                        if (skillSlot2 != null && skillSlotRemainingUses[1] > 0) skillSlot2.SetInteractable(true);
                    }

                    yield break;
                }

                turnManager.SetAIThinking(true);
                int prevPlayer = turnManager.CurrentPlayer;
                turnManager.SwitchTurn();

                _passivesFiredThisTurn.Clear();
                yield return StartCoroutine(TriggerPassiveSkills(SkillTrigger.OnTurnStart, playerID: turnManager.CurrentPlayer));

                hasMovedThisTurn = false;

                // 플레이어 턴이 돌아왔을 때만 슬롯 재활성화
                if (turnManager.CurrentPlayer == _localPlayerID)
                {
                    if (skillSlot1 != null && skillSlotRemainingUses[0] > 0) skillSlot1.SetInteractable(true);
                    if (skillSlot2 != null && skillSlotRemainingUses[1] > 0) skillSlot2.SetInteractable(true);
                }

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
            bool anyHitEffect = false;
            foreach (var unit in units)
            {
                if (unit == null || unit.definition?.passiveSkills == null) continue;
                if (trigger == SkillTrigger.OnAdjacent && !HasAdjacentUnit(unit)) continue;

                foreach (var skill in unit.definition.passiveSkills)
                {
                    if (skill == null || skill.skillType != SkillType.Passive) continue;

                    // Always + Buff 스킬: OnTurnStart 시 VFX만 재생 (효과는 stat 계산에서 상시 적용)
                    if (trigger == SkillTrigger.OnTurnStart && skill.trigger == SkillTrigger.Always
                        && (skill.effectType == SkillEffectType.Buff || skill.effectType == SkillEffectType.FixedBuff))
                    {
                        if (skill.castEffectPrefab != null && unitViews.ContainsKey(unit.id))
                            yield return StartCoroutine(combatAnimator.PlaySkillEffect(skill.castEffectPrefab, unitViews[unit.id].transform.position));
                        if (skill.hitEffectPrefab != null && unitViews.ContainsKey(unit.id))
                        {
                            StartCoroutine(combatAnimator.PlaySkillEffect(skill.hitEffectPrefab, unitViews[unit.id].transform.position));
                            anyHitEffect = true;
                        }
                        anyFired = true;
                        continue;
                    }

                    if (skill.trigger != trigger) continue;

                    // 턴당 1회 제한 (OnTurnStart/OnTurnEnd는 이미 구조상 1회이므로 제외)
                    if (trigger != SkillTrigger.OnTurnStart && trigger != SkillTrigger.OnTurnEnd)
                    {
                        var key = (unit.id, skill.skillName);
                        if (_passivesFiredThisTurn.Contains(key)) continue;
                        _passivesFiredThisTurn.Add(key);
                    }

                    // isOneTimeUse 스킬은 ApplyOneTimePassive에서 처리 — 여기서 중복 실행 방지
                    if (skill.isOneTimeUse)
                    {
                        if (unit.firstAttackUsed) continue; // 이미 발동됨 — VFX 스킵
                        if (skill.castEffectPrefab != null && unitViews.ContainsKey(unit.id))
                            yield return StartCoroutine(combatAnimator.PlaySkillEffect(skill.castEffectPrefab, unitViews[unit.id].transform.position));
                        if (skill.hitEffectPrefab != null)
                        {
                            var firstTarget = AttackResolver.GetAttackTargets(unit, board).FirstOrDefault();
                            if (firstTarget != null && unitViews.ContainsKey(firstTarget.id))
                            {
                                StartCoroutine(combatAnimator.PlaySkillEffect(skill.hitEffectPrefab, unitViews[firstTarget.id].transform.position));
                                anyHitEffect = true;
                            }
                        }
                        continue;
                    }

                    if (trigger == SkillTrigger.OnAttack &&
                        (skill.targetType == SkillTargetType.SingleEnemy || skill.targetType == SkillTargetType.SingleAlly))
                    {
                        yield return StartCoroutine(WaitForPassiveSkillTarget(skill, unit));
                    }
                    else
                    {
                        bool allowDead = trigger == SkillTrigger.OnDeath;
                        if (!string.IsNullOrEmpty(skill.castSoundId))
                            AudioManager.Instance?.Play(skill.castSoundId);
                        if (skill.castEffectPrefab != null && unitViews.ContainsKey(unit.id))
                            yield return StartCoroutine(combatAnimator.PlaySkillEffect(skill.castEffectPrefab, unitViews[unit.id].transform.position));

                        // 실행 전 HP 스냅샷
                        var hpBefore = new Dictionary<int, int>();
                        foreach (var u in board.GetAllUnits())
                            hpBefore[u.id] = u.currentHP;

                        var hitTargets = SkillExecutor.ExecutePassive(skill, unit, board, contextUnit, allowDead);

                        // 타겟별 HP 변화 → 데미지 팝업 / 버프 팝업
                        foreach (var t in hitTargets)
                        {
                            if (!hpBefore.ContainsKey(t.id) || !unitViews.ContainsKey(t.id)) continue;
                            int delta = hpBefore[t.id] - t.currentHP;
                            if (delta != 0)
                                combatAnimator.SpawnDamagePopup(Mathf.Abs(delta), unitViews[t.id].transform.position);
                            else
                                combatAnimator.SpawnBuffPopup(skill, unitViews[t.id].transform.position);
                        }

                        if (skill.hitEffectPrefab != null || !string.IsNullOrEmpty(skill.hitSoundId))
                            foreach (var t in hitTargets)
                            {
                                if (!string.IsNullOrEmpty(skill.hitSoundId))
                                    AudioManager.Instance?.Play(skill.hitSoundId);
                                if (skill.hitEffectPrefab != null && unitViews.ContainsKey(t.id))
                                {
                                    StartCoroutine(combatAnimator.PlaySkillEffect(skill.hitEffectPrefab, unitViews[t.id].transform.position));
                                    anyHitEffect = true;
                                }
                            }
                    }
                    anyFired = true;
                }
            }

            // hitEffect VFX가 재생됐으면 애니메이션이 끝날 때까지 대기 (1초)
            if (anyHitEffect)
                yield return new WaitForSeconds(1.0f);

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

            bool isLocalPlayerCaster = caster.ownerID == _localPlayerID;

            // AI 자동 선정: 로컬 플레이어가 아닌 유닛 + 오프라인 모드
            if (!isLocalPlayerCaster && !_isOnlineMode)
            {
                var validTargets = pendingPassiveValidMoves
                    .Where(pos => board.TryGetUnit(pos, out var u) && u.IsAlive && (u.ownerID == caster.ownerID) == targetAlly)
                    .Select(pos => { board.TryGetUnit(pos, out var u); return u; })
                    .ToList();

                if (validTargets.Count > 0)
                {
                    var target = validTargets[UnityEngine.Random.Range(0, validTargets.Count)];
                    SkillExecutor.ExecuteActiveSingle(skill, target, board);
                    _moveLogs.Add(new MoveLog { turn = _turnCount++, playerID = caster.ownerID, eventType = "passive_target", skillName = skill.skillName, targetUnitId = target.id });
                    if (skill.castEffectPrefab != null && unitViews.ContainsKey(caster.id))
                        StartCoroutine(combatAnimator.PlaySkillEffect(skill.castEffectPrefab, unitViews[caster.id].transform.position));
                    if (skill.hitEffectPrefab != null && unitViews.ContainsKey(target.id))
                        StartCoroutine(combatAnimator.PlaySkillEffect(skill.hitEffectPrefab, unitViews[target.id].transform.position));
                    foreach (var view in unitViews.Values)
                        view.UpdateVisuals();
                }
                ClearPassiveSkillState();
                yield break;
            }

            // 온라인 - 상대 유닛의 패시브: RPC로 타겟 수신 대기 (하이라이트 없음)
            if (!isLocalPlayerCaster && _isOnlineMode)
            {
                passiveSkillSelectedTarget = null;
                passiveTargetConfirmed = false;
                while (!passiveTargetConfirmed)
                    yield return null;

                if (passiveSkillSelectedTarget != null)
                {
                    SkillExecutor.ExecuteActiveSingle(skill, passiveSkillSelectedTarget, board);
                    _moveLogs.Add(new MoveLog { turn = _turnCount++, playerID = caster.ownerID, eventType = "passive_target", skillName = skill.skillName, targetUnitId = passiveSkillSelectedTarget.id });
                    if (skill.castEffectPrefab != null && unitViews.ContainsKey(caster.id))
                        StartCoroutine(combatAnimator.PlaySkillEffect(skill.castEffectPrefab, unitViews[caster.id].transform.position));
                    if (skill.hitEffectPrefab != null && unitViews.ContainsKey(passiveSkillSelectedTarget.id))
                        StartCoroutine(combatAnimator.PlaySkillEffect(skill.hitEffectPrefab, unitViews[passiveSkillSelectedTarget.id].transform.position));
                    foreach (var view in unitViews.Values)
                        view.UpdateVisuals();
                }
                ClearPassiveSkillState();
                yield break;
            }

            // 로컬 플레이어가 직접 타겟 선택
            isSelectingPassiveSkillTarget = true;
            boardVisualizer.ShowSkillTargetHighlights(pendingPassiveValidMoves, targetAlly, caster.ownerID);

            while (!passiveTargetConfirmed)
                yield return null;

            if (passiveSkillSelectedTarget != null)
            {
                // 온라인 모드면 선택한 타겟을 상대에게 전송
                if (_isOnlineMode)
                    NetworkGameBridge.Instance?.RPC_SendPassiveTarget(_localPlayerID, passiveSkillSelectedTarget.id);

                SkillExecutor.ExecuteActiveSingle(skill, passiveSkillSelectedTarget, board);
                _moveLogs.Add(new MoveLog { turn = _turnCount++, playerID = caster.ownerID, eventType = "passive_target", skillName = skill.skillName, targetUnitId = passiveSkillSelectedTarget.id });
                if (skill.castEffectPrefab != null && unitViews.ContainsKey(caster.id))
                    StartCoroutine(combatAnimator.PlaySkillEffect(skill.castEffectPrefab, unitViews[caster.id].transform.position));
                if (skill.hitEffectPrefab != null && unitViews.ContainsKey(passiveSkillSelectedTarget.id))
                    StartCoroutine(combatAnimator.PlaySkillEffect(skill.hitEffectPrefab, unitViews[passiveSkillSelectedTarget.id].transform.position));

                foreach (var view in unitViews.Values)
                    view.UpdateVisuals();
            }

            ClearPassiveSkillState();
        }

        /// <summary>온라인 모드: 상대방의 액티브 스킬 사용 RPC 수신 시 호출.</summary>
        public void ReceiveActiveSkill(int senderPlayerID, string skillName, int targetUnitId)
        {
            // 스킬 찾기: 상대 덱의 activeSkills에서 skillName 매칭
            var opponentDeck = senderPlayerID == 0 ? player0Deck : player1Deck;
            SkillDefinition skill = opponentDeck?.selectedActiveSkills?.FirstOrDefault(s => s.skillName == skillName);
            if (skill == null)
            {
                Debug.LogWarning($"[GameManager] ReceiveActiveSkill: skillName={skillName} 없음");
                return;
            }

            if (targetUnitId == -1)
            {
                // Range 스킬: 시전자 ownerID 기준으로 타겟 결정
                SkillExecutor.ExecuteActive(skill, board, senderPlayerID);
                _moveLogs.Add(new MoveLog { turn = _turnCount++, playerID = senderPlayerID, eventType = "skill_range", skillName = skillName, targetUnitId = -1 });
            }
            else
            {
                // Single 타겟 스킬
                UnitState target = null;
                UnitState caster = null;
                foreach (var u in board.GetAllUnits())
                {
                    if (u.id == targetUnitId) target = u;
                    if (u.IsAlive && u.ownerID == senderPlayerID && u.definition.activeSkill == skill) caster = u;
                }
                if (target == null)
                {
                    Debug.LogWarning($"[GameManager] ReceiveActiveSkill: targetUnitId={targetUnitId} 없음");
                    return;
                }
                SkillExecutor.ExecuteActiveSingle(skill, target, board, caster);
                _moveLogs.Add(new MoveLog { turn = _turnCount++, playerID = senderPlayerID, eventType = "skill_single", skillName = skillName, targetUnitId = targetUnitId });
            }

            foreach (var view in unitViews.Values)
            {
                view.UpdatePosition();
                view.UpdateVisuals();
            }
        }

        /// <summary>온라인 모드: 상대방의 패시브 스킬 타겟 선택 RPC 수신 시 호출.</summary>
        public void ReceivePassiveTarget(int targetUnitId)
        {
            foreach (var u in board.GetAllUnits())
            {
                if (u.id == targetUnitId)
                {
                    passiveSkillSelectedTarget = u;
                    passiveTargetConfirmed = true;
                    return;
                }
            }
            Debug.LogWarning($"[GameManager] ReceivePassiveTarget: unitId={targetUnitId} 없음");
        }

        /// <summary>온라인 게임 종료 시 이벤트 로그를 Firestore gameLogs에 제출.</summary>
        async Task SubmitGameLog(int winnerPlayerID)
        {
            if (!_isOnlineMode) return;
            if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsLoggedIn) return;

            string gameId = NetworkGameBridge.Instance?.SessionId ?? System.Guid.NewGuid().ToString();
            string p0uid  = NetworkGameBridge.Instance?.GetPlayerUID(0) ?? "";
            string p1uid  = NetworkGameBridge.Instance?.GetPlayerUID(1) ?? "";

            var events = new List<Dictionary<string, object>>();
            foreach (var log in _moveLogs)
            {
                events.Add(new Dictionary<string, object>
                {
                    { "turn",         log.turn },
                    { "playerID",     log.playerID },
                    { "eventType",    log.eventType ?? "" },
                    { "unitName",     log.unitName ?? "" },
                    { "fromX",        log.fromX },
                    { "fromY",        log.fromY },
                    { "toX",          log.toX },
                    { "toY",          log.toY },
                    { "skillName",    log.skillName ?? "" },
                    { "targetUnitId", log.targetUnitId },
                });
            }

            var data = new Dictionary<string, object>
            {
                { "player0uid",     p0uid },
                { "player1uid",     p1uid },
                { "winnerPlayerID", winnerPlayerID },
                { "isRanked",       isRankedMode },
                { "events",         events },
            };

            await FirebaseManager.Instance.SubmitGameLogAsync(gameId, data);
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
            view.buffIconConfig = buffIconConfig;
            view.Initialize(unit);
            // 적군(owner=1)은 스프라이트를 좌우 반전해 팀 구분
            // IsFlipped(Player 2 화면)이면 전체 반전 — 위에 있는 상대 유닛이 반대쪽을 보도록
            view.spriteRenderer.flipX = (owner == 1) ^ BoardVisualizer.IsFlipped;

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

        // requiresHighestSpeed 패시브: 방어자 SPD > 공격자 SPD이면 데미지 감소 + VFX
        void ApplySpeedPassiveReduction(UnitState attacker, UnitState defender, ref int damage)
        {
            if (defender.definition.passiveSkills == null) return;
            foreach (var skill in defender.definition.passiveSkills)
            {
                if (skill == null || !skill.requiresHighestSpeed) continue;
                if (defender.GetSpeed() <= attacker.GetSpeed()) continue;
                if (skill.effectType == SkillEffectType.Debuff && skill.buffStatType == StatType.DamageReceived)
                {
                    damage = Mathf.Max(0, damage - skill.power);
                    if (skill.hitEffectPrefab != null && unitViews.ContainsKey(defender.id))
                        StartCoroutine(combatAnimator.PlaySkillEffect(skill.hitEffectPrefab, unitViews[defender.id].transform.position));
                }
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

        IEnumerator HandleKingDeath(UnitState deadKing)
        {
            int winnerID = 1 - deadKing.ownerID;
            var winner = turnManager.GetPlayerData(winnerID);
            var loser = turnManager.GetPlayerData(deadKing.ownerID);
            winner.wins++;
            loser.losses++;
            myPlayerInfoUI?.UpdateInfo(turnManager.Player0Data);
            enemyPlayerInfoUI?.UpdateInfo(turnManager.Player1Data);

            matchWinners.Add(winnerID);
            completedMatches++;

            // 골드 보상 및 랭크 포인트 (온라인: localPlayerID 기준, 오프라인: Player0 기준)
            bool localWin    = winnerID == _localPlayerID;
            int baseGold     = localWin ? 40 : 15;
            int missionBonus = 0;
            int rankDelta    = 0;

            // 전적 누적 저장 (관전 모드 제외)
            if (!turnManager.IsSpectatorMode)
            {
                int savedWins   = PlayerPrefs.GetInt("stat_wins",   0);
                int savedLosses = PlayerPrefs.GetInt("stat_losses", 0);
                int savedTotal  = PlayerPrefs.GetInt("stat_total",  0);

                if (localWin) savedWins++;
                else savedLosses++;
                savedTotal++;

                PlayerPrefs.SetInt("stat_wins",   savedWins);
                PlayerPrefs.SetInt("stat_losses", savedLosses);
                PlayerPrefs.SetInt("stat_total",  savedTotal);
                PlayerPrefs.Save();

                if (!_isOnlineMode) PlayerInventory.Instance?.AddCurrency(baseGold);

                if (localWin)
                {
                    missionBonus = DailySystem.Instance?.RecordWin() ?? 0;
                    if (isRankedMode)
                    {
                        rankDelta = 15;
                        if (!_isOnlineMode) Chess.Core.RankSystem.AddWin();
                    }
                }
                else
                {
                    if (isRankedMode)
                    {
                        rankDelta = -8;
                        if (!_isOnlineMode) Chess.Core.RankSystem.AddLoss();
                    }
                }
            }

            if (turnManager.IsSpectatorMode && completedMatches < spectatorMatchCount)
            {
                Debug.Log($"[관전] 매치 {completedMatches}/{spectatorMatchCount} 종료 — 승자: Player {winnerID}");
                enabled = false;
                yield return new WaitForSeconds(1.5f);
                RestartMatch();
            }
            else
            {
                if (turnManager.IsSpectatorMode)
                {
                    int p0wins = matchWinners.Count(w => w == 0);
                    int p1wins = matchWinners.Count(w => w == 1);
                    Debug.Log($"[관전] 전체 종료 ({spectatorMatchCount}판) — Player0: {p0wins}승 / Player1: {p1wins}승");
                }
                if (_isOnlineMode) _ = SubmitGameLog(winnerID);
                victoryUI?.Show(winnerID, baseGold, missionBonus, rankDelta, isRankedMode, _localPlayerID, !_isOnlineMode);
                enabled = false;
            }
        }

        void RestartMatch()
        {
            foreach (var kvp in new Dictionary<int, UnitView>(unitViews))
                Destroy(kvp.Value.gameObject);
            unitViews.Clear();

            board = new BoardState();
            nextUnitID = 0;
            hasMovedThisTurn = false;
            _passivesFiredThisTurn.Clear();
            selectedUnit = null;
            validMoves.Clear();
            ClearPassiveSkillState();

            boardVisualizer.Initialize(board);
            combatAnimator.Initialize(unitViews);

            player1Deck = GenerateAIDeck();
            SetupUnitsFromDeck(player0Deck, 0);
            SetupUnitsFromDeck(player1Deck, 1);

            turnManager.ReinitializeForMatch(board);

            myPlayerInfoUI?.UpdateInfo(turnManager.Player0Data);
            enemyPlayerInfoUI?.UpdateInfo(turnManager.Player1Data);

            UpdateMatchCountText();
            enabled = true;
        }

        void UpdateMatchCountText()
        {
            if (matchInfoText == null) return;
            if (turnManager != null && turnManager.IsSpectatorMode)
                matchInfoText.text = $"매치 {completedMatches + 1} / {spectatorMatchCount}";
            else
                matchInfoText.text = string.Empty;
        }
    }
}