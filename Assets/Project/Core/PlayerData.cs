using UnityEngine;

namespace Chess.Core
{
    [System.Serializable]
    public class PlayerData
    {
        public int playerID;
        public string playerName;
        public bool isAI;

        // Phase 1: 전적
        public int wins;
        public int losses;

        // Phase 2: 나중에 추가
        // public Sprite profileImage;
        // public string tier;
        // public Dictionary<string, UnitStats> unitUsageStats;  // 유닛별 통계

        public PlayerData(int id, string name, bool ai = false, int wins = 0, int losses = 0)
        {
            playerID = id;
            playerName = name;
            isAI = ai;
            this.wins = wins;
            this.losses = losses;
        }

        public string GetDisplayName()
        {
            if (isAI)
                return "AI";

            if (string.IsNullOrEmpty(playerName))
                return $"플레이어 {playerID}";

            return playerName;
        }

        public string GetRecord()
        {
            return $"{wins}승 {losses}패";
        }

        public float GetWinRate()
        {
            int totalGames = wins + losses;
            if (totalGames == 0) return 0f;
            return (float)wins / totalGames * 100f;
        }
    }
}