using UnityEngine;
using System.Collections;
using Chess.Core;
using Chess.Simulation;

namespace Chess.Presentation
{
    public class TurnManager : MonoBehaviour
    {
        [Header("플레이어 정보")]
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
        public TurnUI turnUI;

        public PlayerData Player0Data { get; private set; }
        public PlayerData Player1Data { get; private set; }
        public int CurrentPlayer { get; private set; } = 0;
        public bool IsAIThinking { get; private set; } = false;
        public bool UseAI => useAI;

        private SimpleAI ai;
        private System.Action<UnitState, Vector2Int> onExecuteMove;

        public void Initialize(BoardState board, System.Action<UnitState, Vector2Int> executeMove)
        {
            Player0Data = new PlayerData(0, player0Name, false, player0Wins, player0Losses);
            Player1Data = new PlayerData(1, player1Name, useAI, player1Wins, player1Losses);
            ai = new SimpleAI(board, 1);
            onExecuteMove = executeMove;

            turnUI?.UpdateTurn(GetCurrentPlayerData());
        }

        public PlayerData GetCurrentPlayerData() => CurrentPlayer == 0 ? Player0Data : Player1Data;
        public PlayerData GetPlayerData(int id) => id == 0 ? Player0Data : Player1Data;

        public void StartAITurn()
        {
            if (!IsAIThinking)
                StartCoroutine(AITurnCoroutine());
        }

        public void SwitchTurn()
        {
            CurrentPlayer = 1 - CurrentPlayer;
            turnUI?.UpdateTurn(GetCurrentPlayerData());
        }

        public void SetAIThinking(bool value)
        {
            IsAIThinking = value;
        }

        IEnumerator AITurnCoroutine()
        {
            IsAIThinking = true;

            yield return new WaitForSeconds(aiDelaySeconds);

            var (unit, targetPos) = ai.DecideMove();

            if (unit != null)
            {
                onExecuteMove?.Invoke(unit, targetPos);
            }
            else
            {
                SwitchTurn();
                IsAIThinking = false;
            }
        }
    }
}
