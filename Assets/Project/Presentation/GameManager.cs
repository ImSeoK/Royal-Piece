using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Chess.Core;
using Chess.Simulation;

namespace Chess.Presentation
{
    public class GameManager : MonoBehaviour
    {
        [Header("프리팹")]
        public GameObject unitViewPrefab;
        public GameObject tilePrefab;
        public GameObject moveHighlightPrefab;
        public GameObject damagePopupPrefab;
        public GameObject attackLinePrefab;

        [Header("보드 설정")]
        public Color lightTileColor = Color.white;
        public Color darkTileColor = new Color(0.4f, 0.4f, 0.4f);

        [Header("하이라이트 색상")]
        public Color moveHighlightColor = new Color(0, 1, 0, 0.5f);
        public Color attackHighlightColor = new Color(1, 0, 0, 0.5f);

        [Header("플레이어 설정")]
        public string player0Name = "플레이어";
        public string player1Name = "AI";
        public int player0Wins = 0;
        public int player0Losses = 0;
        public int player1Wins = 0;
        public int player1Losses = 0;

        [Header("AI 설정")]
        public bool useAI = true;
        public float aiDelaySeconds = 1f;

        [Header("UI")]
        public VictoryUI victoryUI;
        public TurnUI turnUI;
        public UnitInfoPanel unitInfoPanel;
        public EnemyUnitInfoPanel enemyUnitInfoPanel;
        public PlayerInfoUI myPlayerInfoUI;
        public PlayerInfoUI enemyPlayerInfoUI;

        [Header("덱 설정")]  // ← 추가!
        public PlayerDeck player0Deck;
        public PlayerDeck player1Deck;

        // 게임 상태
        private BoardState board;
        private Dictionary<int, UnitView> unitViews = new Dictionary<int, UnitView>();
        private int nextUnitID = 0;

        // 플레이어 데이터
        private PlayerData player0Data;
        private PlayerData player1Data;

        // 입력 관련
        private UnitState selectedUnit;
        private List<Vector2Int> validMoves = new List<Vector2Int>();
        private int currentPlayer = 0;

        // 하이라이트 관리
        private List<GameObject> activeHighlights = new List<GameObject>();

        // AI
        private SimpleAI ai;
        private bool isAIThinking = false;

        void Start()
        {
            // DeckTransfer에서 덱 가져오기 (우선순위 1)
            if (DeckTransfer.Instance != null && DeckTransfer.Instance.Player0Deck != null)
            {
                player0Deck = DeckTransfer.Instance.Player0Deck;
                Debug.Log("[GameManager] DeckTransfer에서 Player0 덱 로드!");
            }
            else if (player0Deck == null)
            {
                Debug.LogError("[GameManager] Player0 덱이 설정되지 않았습니다!");
            }

            // Player1 덱 (AI 또는 테스트용)
            if (player1Deck == null)
            {
                Debug.LogError("[GameManager] Player1 덱이 설정되지 않았습니다!");
            }

            // PlayerData 초기화
            player0Data = new PlayerData(0, player0Name, false, player0Wins, player0Losses);
            player1Data = new PlayerData(1, player1Name, useAI, player1Wins, player1Losses);

            board = new BoardState();
            ai = new SimpleAI(board, 1);

            CreateBoard();

            // 덱 기반 배치  // ← 수정!
            if (player0Deck != null && player1Deck != null)
            {
                SetupUnitsFromDeck(player0Deck, 0);
                SetupUnitsFromDeck(player1Deck, 1);
            }
            else
            {
                Debug.LogError("[Deck] PlayerDeck이 설정되지 않았습니다!");
            }

            // UI 초기화
            if (turnUI != null)
            {
                turnUI.UpdateTurn(GetCurrentPlayerData());
            }

            if (myPlayerInfoUI != null)
            {
                myPlayerInfoUI.UpdateInfo(player0Data);
            }

            if (enemyPlayerInfoUI != null)
            {
                enemyPlayerInfoUI.UpdateInfo(player1Data);
            }
        }

        void CreateBoard()
        {
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    GameObject tile = Instantiate(tilePrefab, new Vector3(x, y, 0), Quaternion.identity);
                    tile.name = $"Tile_{x}_{y}";
                    tile.transform.parent = transform;

                    SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
                    sr.color = (x + y) % 2 == 0 ? lightTileColor : darkTileColor;
                }
            }
        }

        void UpdatePlayerInfoUI()
        {
            if (myPlayerInfoUI != null)
            {
                myPlayerInfoUI.UpdateInfo(player0Data);
            }

            if (enemyPlayerInfoUI != null)
            {
                enemyPlayerInfoUI.UpdateInfo(player1Data);
            }
        }

        void SetupUnitsFromDeck(PlayerDeck deck, int owner)
        {
            if (!deck.IsValid(out string error))
            {
                Debug.LogError($"[Deck] Player {owner}의 덱이 유효하지 않습니다: {error}");
                return;
            }

            Debug.Log($"[Deck] Player {owner} 덱 배치: {deck.GetDeckInfo()}");

            // 플레이어: 0-1줄, AI: 7-6줄 (역순)
            int mainRow = owner == 0 ? 0 : 7;
            int pawnRow = owner == 0 ? 1 : 6;

            // === Row 0 또는 7: King + 커스텀 7개 ===

            // King 배치 (중앙, x=3 또는 4)
            int kingX = 3;  // 또는 4, 원하는 위치
            CreateUnit(deck.king, new Vector2Int(kingX, mainRow), owner);

            // 커스텀 유닛 7개 배치
            List<int> positions = new List<int> { 0, 1, 2, 4, 5, 6, 7 };  // King 위치(3) 제외

            for (int i = 0; i < deck.customUnits.Count && i < positions.Count; i++)
            {
                CreateUnit(deck.customUnits[i], new Vector2Int(positions[i], mainRow), owner);
            }

            // === Row 1 또는 6: Pawn 8개 고정 ===

            for (int x = 0; x < 8; x++)
            {
                CreateUnit(deck.pawn, new Vector2Int(x, pawnRow), owner);
            }
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

            view.spriteRenderer.color = owner == 0 ? Color.white : Color.black;

            unitViews[unit.id] = view;
        }

        void Update()
        {
            if (isAIThinking)
                return;

            if (currentPlayer == 0)
            {
                HandleInput();
            }
            else if (useAI)
            {
                Debug.Log($"[Update] AI 턴 감지, AITurn 코루틴 시작");
                StartCoroutine(AITurn());
            }
        }

        IEnumerator AITurn()
        {
            if (isAIThinking)
            {
                Debug.LogWarning("[AI] 이미 실행 중! 중복 호출 차단");
                yield break;
            }

            isAIThinking = true;
            Debug.Log($"[AI] 턴 시작, isAIThinking = true, currentPlayer = {currentPlayer}");

            yield return new WaitForSeconds(aiDelaySeconds);

            var (unit, targetPos) = ai.DecideMove();

            if (unit != null)
            {
                Debug.Log($"[AI] {unit.definition.unitName} 이동 결정: ({unit.position.x}, {unit.position.y}) → ({targetPos.x}, {targetPos.y})");
                ExecuteMove(unit, targetPos);
            }
            else
            {
                Debug.Log("[AI] 이동 불가, 턴 넘김");
                currentPlayer = 0;

                if (turnUI != null)
                {
                    turnUI.UpdateTurn(GetCurrentPlayerData());
                }

                isAIThinking = false;
                Debug.Log($"[AI] 턴 종료 (이동 불가), isAIThinking = false");
            }

            Debug.Log($"[AI] ExecuteMove 호출 완료, 전투 대기 중...");
        }

        void HandleInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Vector2Int gridPos = new Vector2Int(
                    Mathf.FloorToInt(worldPos.x + 0.5f),
                    Mathf.FloorToInt(worldPos.y + 0.5f)
                );

                if (!board.IsInBounds(gridPos)) return;

                HandleClick(gridPos);
            }
        }

        void HandleClick(Vector2Int position)
        {
            if (selectedUnit != null)
            {
                if (validMoves.Contains(position))
                {
                    ExecuteMove(selectedUnit, position);
                }

                selectedUnit = null;
                validMoves.Clear();
                ClearHighlights();
                unitInfoPanel?.Hide();  // 이동 시에만 내 패널 숨김
                return;
            }

            if (board.TryGetUnit(position, out UnitState unit))
            {
                Debug.Log($"유닛 클릭: {unit.definition.unitName}, 소유자: {unit.ownerID}, 현재 플레이어: {currentPlayer}");

                // 내 유닛 선택
                if (unit.ownerID == currentPlayer)
                {
                    selectedUnit = unit;
                    validMoves = MovementResolver.GetValidMoves(unit, board);

                    Debug.Log($"{unit.definition.unitName} 선택됨. 이동 가능: {validMoves.Count}칸");

                    ShowMoveHighlights(unit);
                    unitInfoPanel?.Show(unit);  // 우측 패널 표시
                                                // enemyUnitInfoPanel은 그대로 유지 (숨기지 않음)
                }
                // 상대 유닛 클릭
                else
                {
                    Debug.Log($"상대 유닛 클릭! EnemyPanel 표시");
                    enemyUnitInfoPanel?.Show(unit);  // 좌측 패널 표시
                                                     // unitInfoPanel은 그대로 유지 (숨기지 않음)
                }
            }
            else
            {
                // 빈 칸 클릭 시 상대 패널만 숨김
                enemyUnitInfoPanel?.Hide();
            }
        }

        void ShowMoveHighlights(UnitState unit)
        {
            ClearHighlights();

            foreach (var pos in validMoves)
            {
                bool hasEnemy = board.TryGetUnit(pos, out var targetUnit) &&
                                targetUnit.ownerID != unit.ownerID;

                GameObject highlight = Instantiate(
                    moveHighlightPrefab,
                    new Vector3(pos.x, pos.y, 0),
                    Quaternion.identity
                );

                highlight.name = $"Highlight_{pos.x}_{pos.y}";
                highlight.transform.parent = transform;

                SpriteRenderer sr = highlight.GetComponent<SpriteRenderer>();
                sr.color = hasEnemy ? attackHighlightColor : moveHighlightColor;

                activeHighlights.Add(highlight);
            }

            var attackTargets = AttackResolver.GetAttackTargets(unit, board);
            foreach (var target in attackTargets)
            {
                if (validMoves.Contains(target.position))
                    continue;

                GameObject highlight = Instantiate(
                    moveHighlightPrefab,
                    new Vector3(target.position.x, target.position.y, 0),
                    Quaternion.identity
                );

                highlight.name = $"AttackHighlight_{target.position.x}_{target.position.y}";
                highlight.transform.parent = transform;

                SpriteRenderer sr = highlight.GetComponent<SpriteRenderer>();
                sr.color = new Color(1, 0, 0, 0.7f);

                activeHighlights.Add(highlight);
            }
        }

        void ClearHighlights()
        {
            foreach (var highlight in activeHighlights)
            {
                if (highlight != null)
                {
                    Destroy(highlight);
                }
            }
            activeHighlights.Clear();
        }

        void ExecuteMove(UnitState unit, Vector2Int targetPos)
        {
            Debug.Log($"=== ExecuteMove 시작 ===");
            Debug.Log($"유닛: {unit.definition.unitName} (소유자: {unit.ownerID})");
            Debug.Log($"현재 턴: {currentPlayer}, isAIThinking: {isAIThinking}");

            board.MoveUnit(unit, targetPos);
            unitViews[unit.id].UpdatePosition();

            Debug.Log($"[이동] {unit.definition.unitName} → ({targetPos.x}, {targetPos.y})");

            StartCoroutine(PlayCombatSequence(unit));
        }

        IEnumerator PlayCombatSequence(UnitState movedUnit)
        {
            Debug.Log($"=== PlayCombatSequence 시작 ===");

            // 1단계: 전투 참가자 수집
            var combatants = new List<UnitState>();
            combatants.Add(movedUnit);

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

            var myTargets = AttackResolver.GetAttackTargets(movedUnit, board);
            foreach (var target in myTargets)
            {
                if (!combatants.Contains(target))
                {
                    combatants.Add(target);
                }
            }

            // 2단계: 속도 순 정렬
            var sortedCombatants = combatants
                .OrderByDescending(u => u.definition.speed)
                .ThenBy(u => Random.value)
                .ToList();

            Debug.Log($"[전투] 참가자: {combatants.Count}명");

            var deadUnits = new List<UnitState>();

            // 3단계: 순차 공격
            foreach (var attacker in sortedCombatants)
            {
                if (!attacker.IsAlive)
                {
                    Debug.Log($"[전투] {attacker.definition.unitName}는 이미 사망");
                    continue;
                }

                var targets = AttackResolver.GetAttackTargets(attacker, board);
                Debug.Log($"[전투] {attacker.definition.unitName}(속도:{attacker.definition.speed}) 공격 대상: {targets.Count}명");

                foreach (var target in targets)
                {
                    if (!target.IsAlive)
                        continue;

                    yield return StartCoroutine(PlayAttackEffect(attacker, target));

                    int damage = attacker.definition.attackPower;
                    target.TakeDamage(damage);

                    Debug.Log($"[전투] {attacker.definition.unitName} → {target.definition.unitName} ({damage} 데미지, 남은 HP: {target.currentHP})");

                    if (unitViews.ContainsKey(target.id))
                    {
                        unitViews[target.id].UpdateVisuals();
                    }

                    if (!target.IsAlive)
                    {
                        deadUnits.Add(target);
                        Debug.Log($"[전투] {target.definition.unitName} 사망!");
                    }

                    yield return new WaitForSeconds(0.2f);
                }
            }

            // 4단계: 사망 처리
            foreach (var deadUnit in deadUnits)
            {
                if (unitViews.ContainsKey(deadUnit.id))
                {
                    yield return StartCoroutine(unitViews[deadUnit.id].PlayDeathEffect());
                }

                DestroyUnit(deadUnit);

                if (deadUnit.definition.isKing)
                {
                    int winnerID = 1 - deadUnit.ownerID;
                    int loserID = deadUnit.ownerID;

                    PlayerData winner = GetPlayerData(winnerID);
                    PlayerData loser = GetPlayerData(loserID);

                    // 전적 업데이트  // ← 추가
                    winner.wins++;
                    loser.losses++;

                    Debug.Log($"★ {winner.GetDisplayName()} 승리! ★");
                    Debug.Log($"[전적] {winner.GetDisplayName()}: {winner.GetRecord()}");
                    Debug.Log($"[전적] {loser.GetDisplayName()}: {loser.GetRecord()}");

                    // UI 갱신  // ← 추가
                    UpdatePlayerInfoUI();

                    if (victoryUI != null)
                    {
                        victoryUI.Show(winner);
                    }

                    enabled = false;
                    yield break;
                }
            }

            // 5단계: 턴 전환
            int moverOwner = movedUnit.ownerID;

            Debug.Log($"[턴 전환 체크] 이동 유닛 주인: {moverOwner}, 현재 플레이어: {currentPlayer}");

            if (moverOwner == currentPlayer)
            {
                int prevPlayer = currentPlayer;

                currentPlayer = 1 - currentPlayer;
                Debug.Log($"[턴 전환] {GetPlayerData(prevPlayer).GetDisplayName()} → {GetCurrentPlayerData().GetDisplayName()}");

                if (turnUI != null)
                {
                    turnUI.UpdateTurn(GetCurrentPlayerData());
                }

                if (prevPlayer == 1 && useAI)
                {
                    isAIThinking = false;
                    Debug.Log($"[AI] 턴 종료, isAIThinking = false");
                }

                if (currentPlayer == 1 && useAI)
                {
                    Debug.Log($"[AI] 0.5초 후 다음 AI 턴 시작");
                    yield return new WaitForSeconds(0.5f);
                }
            }
            else
            {
                Debug.LogError($"[버그!] 이동 유닛 주인({moverOwner})과 현재 플레이어({currentPlayer})가 다름!");
            }

            Debug.Log($"=== PlayCombatSequence 종료 ===");
            Debug.Log($"currentPlayer: {currentPlayer}, isAIThinking: {isAIThinking}");
        }

        IEnumerator PlayAttackEffect(UnitState attacker, UnitState target)
        {
            Vector3 attackerPos = unitViews[attacker.id].transform.position;
            Vector3 targetPos = unitViews[target.id].transform.position;

            if (attackLinePrefab != null)
            {
                GameObject lineObj = Instantiate(attackLinePrefab, Vector3.zero, Quaternion.identity);
                AttackLine line = lineObj.GetComponent<AttackLine>();
                line.Initialize(attackerPos, targetPos);
            }

            if (unitViews.ContainsKey(target.id))
            {
                StartCoroutine(unitViews[target.id].PlayHitEffect());
            }

            if (damagePopupPrefab != null)
            {
                GameObject popup = Instantiate(damagePopupPrefab, targetPos, Quaternion.identity);
                DamagePopup damagePopup = popup.GetComponent<DamagePopup>();
                damagePopup.Initialize(attacker.definition.attackPower, targetPos);
            }

            yield return new WaitForSeconds(0.15f);
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

        PlayerData GetCurrentPlayerData()
        {
            return currentPlayer == 0 ? player0Data : player1Data;
        }

        PlayerData GetPlayerData(int playerID)
        {
            return playerID == 0 ? player0Data : player1Data;
        }
    }
}