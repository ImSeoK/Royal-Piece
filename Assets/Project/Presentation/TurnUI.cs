using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Chess.Core;

namespace Chess.Presentation
{
    public class TurnUI : MonoBehaviour
    {
        [Header("턴 배지")]
        public Image turnBadge;
        public Image turnDot;
        public TextMeshProUGUI turnText;

        [Header("타이머")]
        public RectTransform timerBarFill;
        public TextMeshProUGUI timerText;
        public float turnDuration = 30f;

        // 색상
        private readonly Color myColor = new Color(0.29f, 0.62f, 1.00f);   // #4A9EFF
        private readonly Color enemyColor = new Color(1.00f, 0.35f, 0.35f);   // #FF5A5A
        private readonly Color safeColor = new Color(0.24f, 0.86f, 0.52f);   // #3DDC84
        private readonly Color warnColor = new Color(0.94f, 0.80f, 0.42f);   // #F0CC6A
        private readonly Color dangerColor = new Color(1.00f, 0.35f, 0.35f);   // #FF5A5A

        private Coroutine timerCoroutine;
        private bool isMyTurn;

        public void UpdateTurn(PlayerData playerData)
        {
            isMyTurn = playerData.playerID == 0;
            Color c = isMyTurn ? myColor : enemyColor;

            if (turnText != null)
            {
                turnText.text = isMyTurn ? "내 턴" : (playerData.isAI ? "AI 턴" : "상대 턴");
                turnText.color = c;
            }
            if (turnBadge != null)
                turnBadge.color = new Color(c.r, c.g, c.b, 0.12f);
            if (turnDot != null)
                turnDot.color = c;

            StartTimer();
        }

        public void UpdateTurn(int currentPlayer, bool isAI)
        {
            isMyTurn = currentPlayer == 0;
            Color c = isMyTurn ? myColor : enemyColor;

            if (turnText != null)
            {
                turnText.text = isMyTurn ? "내 턴" : (isAI ? "AI 턴" : "상대 턴");
                turnText.color = c;
            }
            if (turnBadge != null)
                turnBadge.color = new Color(c.r, c.g, c.b, 0.12f);
            if (turnDot != null)
                turnDot.color = c;

            StartTimer();
        }

        void StartTimer()
        {
            if (timerCoroutine != null)
                StopCoroutine(timerCoroutine);
            timerCoroutine = StartCoroutine(RunTimer());
        }

        IEnumerator RunTimer()
        {
            float remaining = turnDuration;

            while (remaining > 0f)
            {
                remaining -= Time.deltaTime;
                float ratio = remaining / turnDuration;
                int seconds = Mathf.CeilToInt(remaining);

                // 바 업데이트
                if (timerBarFill != null)
                {
                    timerBarFill.anchorMin = new Vector2(0, 0);
                    timerBarFill.anchorMax = new Vector2(ratio, 1);
                    timerBarFill.offsetMin = Vector2.zero;
                    timerBarFill.offsetMax = Vector2.zero;
                }

                // 숫자 + 색상
                Color barColor = ratio > 0.5f ? safeColor : ratio > 0.25f ? warnColor : dangerColor;
                if (timerBarFill != null)
                    timerBarFill.GetComponent<Image>().color = barColor;
                if (timerText != null)
                {
                    timerText.text = seconds.ToString();
                    timerText.color = barColor;
                }

                yield return null;
            }

            // 타이머 종료
            if (timerBarFill != null)
            {
                timerBarFill.anchorMax = new Vector2(0, 1);
                timerBarFill.GetComponent<Image>().color = dangerColor;
            }
            if (timerText != null)
            {
                timerText.text = "0";
                timerText.color = dangerColor;
            }
        }
    }
}