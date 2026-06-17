using UnityEngine;

namespace Chess.Core
{
    public static class RankSystem
    {
        const int WIN_POINTS  = 15;
        const int LOSS_POINTS = 8;

        public static int GetPoints() => PlayerPrefs.GetInt("rank_points", 0);

        public static void AddWin()  => SetPoints(GetPoints() + WIN_POINTS);
        public static void AddLoss() => SetPoints(Mathf.Max(0, GetPoints() - LOSS_POINTS));

        static void SetPoints(int pts)
        {
            PlayerPrefs.SetInt("rank_points", pts);
            PlayerPrefs.Save();
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
