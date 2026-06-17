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
        [Header("뱃지")]
        public Image rankBadge;
        public TextMeshProUGUI rankBadgeText;

        [Header("진행도")]
        public Image rankProgressFill;

        [Header("텍스트")]
        public TextMeshProUGUI rankPointText;
        public TextMeshProUGUI rankRemainText;

        public void Initialize(bool isRanked)
        {
            gameObject.SetActive(isRanked);
            if (!isRanked) return;
            Refresh();
        }

        public void Refresh()
        {
            string rankName  = RankSystem.GetRankName();
            Color  tierColor = RankSystem.GetTierColor();
            float  progress  = RankSystem.GetTierProgress();
            int    pts       = RankSystem.GetPoints();
            int    remain    = RankSystem.GetPointsToNextTier();

            if (rankBadge        != null) rankBadge.color      = tierColor;
            if (rankBadgeText    != null) rankBadgeText.text   = rankName;
            if (rankProgressFill != null) rankProgressFill.fillAmount = progress;
            if (rankPointText    != null) rankPointText.text   = pts.ToString();
            if (rankRemainText   != null)
                rankRemainText.text = remain > 0 ? $"next {remain} pt" : "MAX";
        }
    }
}
