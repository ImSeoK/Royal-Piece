using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Chess.Core;

namespace Chess.Presentation
{
    /// <summary>
    /// SampleScene 내 인게임 RankPanel에 부착.
    /// GameManager.Start()에서 Initialize() 호출.
    /// </summary>
    public class RankPanelUI : MonoBehaviour
    {
        [Header("뱃지 — 아웃라인 (tier 색)")]
        public Image rankBadgeOutline;

        [Header("뱃지 — 내부 배경 (고정 어두운 색)")]
        public Image rankBadgeBg;

        public TextMeshProUGUI rankBadgeText;

        [Header("뱃지 스프라이트 (D/C/B/A/S)")]
        public Sprite badgeSpriteD;
        public Sprite badgeSpriteC;
        public Sprite badgeSpriteB;
        public Sprite badgeSpriteA;
        public Sprite badgeSpriteS;

        [Header("진행도")]
        public Image rankProgressFill;

        // 티어별 내부 BG 색상
        static readonly Color BG_S = new Color(0.24f, 0.17f, 0.05f, 1f); // 어두운 금
        static readonly Color BG_A = new Color(0.16f, 0.10f, 0.30f, 1f); // 어두운 보라
        static readonly Color BG_B = new Color(0.05f, 0.16f, 0.25f, 1f); // 어두운 파랑
        static readonly Color BG_C = new Color(0.10f, 0.10f, 0.10f, 1f); // 어두운 회색
        static readonly Color BG_D = new Color(0.08f, 0.08f, 0.08f, 1f); // 더 어두운 회색


        // ── 디버그 (에디터 전용) ──────────────────────────────────
        [ContextMenu("Debug / Set D")] void DbgD() { PlayerPrefs.SetInt("rank_points",    0); Refresh(); }
        [ContextMenu("Debug / Set C")] void DbgC() { PlayerPrefs.SetInt("rank_points",  300); Refresh(); }
        [ContextMenu("Debug / Set B")] void DbgB() { PlayerPrefs.SetInt("rank_points",  600); Refresh(); }
        [ContextMenu("Debug / Set A")] void DbgA() { PlayerPrefs.SetInt("rank_points",  900); Refresh(); }
        [ContextMenu("Debug / Set S")] void DbgS() { PlayerPrefs.SetInt("rank_points", 1200); Refresh(); }
        // ─────────────────────────────────────────────────────────

        public void Initialize(bool isRanked)
        {
            gameObject.SetActive(isRanked);
            if (!isRanked) return;
            Refresh();
        }

        public void Refresh()
        {
            string rankName  = RankSystem.GetRankName();
            float  progress  = RankSystem.GetTierProgress();
            string tier      = RankSystem.GetTierLetter();
            Color  tierColor = RankSystem.GetTierColor();

            if (rankBadgeOutline != null)
            {
                rankBadgeOutline.sprite = GetBadgeSprite(tier);
                rankBadgeOutline.color  = tierColor;
            }
            if (rankBadgeBg != null)
            {
                rankBadgeBg.sprite = GetBadgeSprite(tier);
                rankBadgeBg.color  = GetBgColor(tier);
            }
            if (rankBadgeText    != null) rankBadgeText.text          = rankName;
            if (rankProgressFill != null) rankProgressFill.fillAmount = progress;
        }

        Sprite GetBadgeSprite(string tier)
        {
            return tier switch
            {
                "S" => badgeSpriteS,
                "A" => badgeSpriteA,
                "B" => badgeSpriteB,
                "C" => badgeSpriteC,
                _   => badgeSpriteD,
            };
        }

        Color GetBgColor(string tier)
        {
            return tier switch
            {
                "S" => BG_S,
                "A" => BG_A,
                "B" => BG_B,
                "C" => BG_C,
                _   => BG_D,
            };
        }
    }
}
