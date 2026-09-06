using UnityEngine;
using System;
using System.Collections.Generic;
using Chess.Core;

namespace Chess.Presentation
{
    public class DailySystem : MonoBehaviour
    {
        public static DailySystem Instance { get; private set; }

        public const int DAILY_BONUS_GOLD    = 100;
        public const int DAILY_BONUS_GEMS    = 5;
        public const int MISSION_REWARD_GOLD = 150;
        public const int MISSION_WIN_TARGET  = 3;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            if (Instance != this) return;
            FirebaseManager.Instance?.SubscribeDataReady(ApplyFromFirestore);
        }

        void ApplyFromFirestore(FirestorePlayerData data)
        {
            if (data == null) return; // 신규 유저 — 로컬 데이터 유지

            // 서버 daily 데이터로 덮어쓰기
            if (!string.IsNullOrEmpty(data.lastDailyBonus))
            {
                PlayerPrefs.SetString("daily_bonus_date", data.lastDailyBonus);
            }
            if (!string.IsNullOrEmpty(data.dailyMissionDate))
            {
                PlayerPrefs.SetString("daily_mission_date",    data.dailyMissionDate);
                PlayerPrefs.SetInt   ("daily_mission_wins",    data.dailyMissionWins);
                PlayerPrefs.SetInt   ("daily_mission_claimed", data.dailyMissionClaimed ? 1 : 0);
            }
            PlayerPrefs.Save();
            Debug.Log($"[DailySystem] Firestore 동기화 완료");
        }

        // ─── Daily Bonus ───────────────────────────────────────────

        public bool IsDailyBonusClaimed()
            => PlayerPrefs.GetString("daily_bonus_date", "") == Today();

        /// <summary>오늘 보너스가 미수령이면 골드 지급 후 true 반환.</summary>
        public bool ClaimDailyBonus()
        {
            if (IsDailyBonusClaimed()) return false;
            string today = Today();
            PlayerPrefs.SetString("daily_bonus_date", today);
            PlayerPrefs.Save();
            PlayerInventory.Instance?.AddCurrency(DAILY_BONUS_GOLD);
            PlayerInventory.Instance?.AddGems(DAILY_BONUS_GEMS);
            FirebaseManager.Instance?.UpdateFields(new Dictionary<string, object>
            {
                { "lastDailyBonus", today }
            });
            return true;
        }

        // ─── Daily Mission ─────────────────────────────────────────

        public int GetMissionWins()
        {
            RefreshMissionIfNewDay();
            return PlayerPrefs.GetInt("daily_mission_wins", 0);
        }

        public bool IsMissionClaimed()
        {
            RefreshMissionIfNewDay();
            return PlayerPrefs.GetInt("daily_mission_claimed", 0) == 1;
        }

        public bool IsMissionComplete() => GetMissionWins() >= MISSION_WIN_TARGET;

        /// <summary>
        /// 승리 시 호출. 미션 진행도 증가.
        /// 이번 승리로 미션이 완료되면 자동 지급 후 보상 금액 반환, 아니면 0 반환.
        /// </summary>
        public int RecordWin()
        {
            RefreshMissionIfNewDay();
            int wins = PlayerPrefs.GetInt("daily_mission_wins", 0) + 1;
            PlayerPrefs.SetInt("daily_mission_wins", wins);
            PlayerPrefs.Save();

            bool missionComplete = wins >= MISSION_WIN_TARGET && PlayerPrefs.GetInt("daily_mission_claimed", 0) == 0;
            if (missionComplete)
            {
                PlayerPrefs.SetInt("daily_mission_claimed", 1);
                PlayerPrefs.Save();
                PlayerInventory.Instance?.AddCurrency(MISSION_REWARD_GOLD); // AddCurrency → SaveInventory → Firestore 포함
            }

            // daily mission 진행도 Firestore 부분 업데이트
            FirebaseManager.Instance?.UpdateFields(new Dictionary<string, object>
            {
                { "dailyMissionWins",    wins },
                { "dailyMissionClaimed", missionComplete || PlayerPrefs.GetInt("daily_mission_claimed", 0) == 1 },
            });

            return missionComplete ? MISSION_REWARD_GOLD : 0;
        }

        void RefreshMissionIfNewDay()
        {
            string today = Today();
            if (PlayerPrefs.GetString("daily_mission_date", "") == today) return;
            PlayerPrefs.SetString("daily_mission_date",    today);
            PlayerPrefs.SetInt   ("daily_mission_wins",    0);
            PlayerPrefs.SetInt   ("daily_mission_claimed", 0);
            PlayerPrefs.Save();
            FirebaseManager.Instance?.UpdateFields(new Dictionary<string, object>
            {
                { "dailyMissionDate",    today },
                { "dailyMissionWins",    0 },
                { "dailyMissionClaimed", false },
            });
        }

        static string Today() => DateTime.Now.ToString("yyyy-MM-dd");

        [ContextMenu("데일리 데이터 초기화 (테스트용)")]
        public void ResetDailyData()
        {
            PlayerPrefs.DeleteKey("daily_bonus_date");
            PlayerPrefs.DeleteKey("daily_mission_date");
            PlayerPrefs.DeleteKey("daily_mission_wins");
            PlayerPrefs.DeleteKey("daily_mission_claimed");
            PlayerPrefs.Save();
            Debug.Log("[DailySystem] 데일리 데이터 초기화 완료");
        }

        [ContextMenu("전적 및 랭크 초기화 (테스트용)")]
        public void ResetStats()
        {
            PlayerPrefs.DeleteKey("stat_wins");
            PlayerPrefs.DeleteKey("stat_losses");
            PlayerPrefs.DeleteKey("stat_total");
            PlayerPrefs.DeleteKey("rank_points");
            PlayerPrefs.Save();
            Debug.Log("[DailySystem] 전적/랭크 초기화 완료");
        }
    }
}
