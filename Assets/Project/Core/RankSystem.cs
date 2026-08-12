using UnityEngine;
using System.Collections.Generic;

namespace Chess.Core
{
    public static class RankSystem
    {
        const int WIN_POINTS  = 15;
        const int LOSS_POINTS = 8;

        // 서버에서 받은 값 캐시. -1이면 아직 서버 동기화 전.
        static int _serverPoints = -1;

        /// <summary>서버 로드 완료 시 호출. 이후 GetPoints()가 서버 값 반환.</summary>
        public static void SetFromServer(int pts)
        {
            _serverPoints = pts;
            PlayerPrefs.SetInt("rank_points", pts);
            PlayerPrefs.Save();
        }

        public static int GetPoints()
        {
            if (_serverPoints >= 0) return _serverPoints;
            return PlayerPrefs.GetInt("rank_points", 0);
        }

        public static void AddWin()  => SetPoints(GetPoints() + WIN_POINTS);
        public static void AddLoss() => SetPoints(Mathf.Max(0, GetPoints() - LOSS_POINTS));

        static void SetPoints(int pts)
        {
            _serverPoints = pts;
            PlayerPrefs.SetInt("rank_points", pts);
            PlayerPrefs.Save();
            SyncToFirestore(pts);
        }

        static void SyncToFirestore(int pts)
        {
            FirebaseManager.Instance?.UpdateFields(new Dictionary<string, object>
            {
                { "rankPoints", pts }
            });
        }

        public static string GetRankName()
        {
            int pts = GetPoints();
            if (pts >= 1200) return "S";
            if (pts >= 1100) return "A1";
            if (pts >= 1000) return "A2";
            if (pts >=  900) return "A3";
            if (pts >=  800) return "B1";
            if (pts >=  700) return "B2";
            if (pts >=  600) return "B3";
            if (pts >=  500) return "C1";
            if (pts >=  400) return "C2";
            if (pts >=  300) return "C3";
            if (pts >=  200) return "D1";
            if (pts >=  100) return "D2";
            return "D3";
        }

        /// <summary>등급 알파벳만 반환 (S/A/B/C/D)</summary>
        public static string GetTierLetter()
        {
            string rank = GetRankName();
            return rank.Length > 0 ? rank[0].ToString() : "D";
        }

        /// <summary>현재 서브랭크 내 진행도 0~1</summary>
        public static float GetTierProgress()
        {
            int pts = GetPoints();
            if (pts >= 1200) return 1f;
            int tierMin = (pts / 100) * 100;
            return (pts - tierMin) / 100f;
        }

        /// <summary>다음 서브랭크까지 필요 포인트. S면 0.</summary>
        public static int GetPointsToNextTier()
        {
            int pts = GetPoints();
            if (pts >= 1200) return 0;
            return ((pts / 100) + 1) * 100 - pts;
        }

        /// <summary>등급별 뱃지 색상</summary>
        public static Color GetTierColor()
        {
            switch (GetTierLetter())
            {
                case "S": return HexToColor("#C9913A");
                case "A": return HexToColor("#B57BFF");
                case "B": return HexToColor("#5AADFF");
                case "C": return HexToColor("#888888");
                default:  return HexToColor("#666666");
            }
        }

        static Color HexToColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color c);
            return c;
        }
    }
}
