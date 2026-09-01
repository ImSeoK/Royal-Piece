using UnityEngine;
using TMPro;
using System.Collections;

namespace Chess.Presentation
{
    public class DamagePopup : MonoBehaviour
    {
        public TextMeshProUGUI damageText;

        private Vector3 startPos;

        void Awake()
        {
            if (damageText == null)
                damageText = GetComponentInChildren<TextMeshProUGUI>();
        }

        public void Initialize(int damage, Vector3 worldPosition)
        {
            startPos = worldPosition + Vector3.up * 0.5f;
            transform.position = startPos;

            damageText.text      = damage.ToString();
            damageText.fontSize  = UIConfig.DamagePopupSize;
            damageText.color     = UIConfig.DamageColor;

            StartCoroutine(AnimatePopup());
        }

        public void Initialize(string text, Color color, Vector3 worldPosition)
        {
            startPos = worldPosition + Vector3.up * 0.5f;
            transform.position = startPos;

            damageText.text     = text;
            damageText.fontSize = UIConfig.BuffPopupSize;
            damageText.color    = color;

            StartCoroutine(AnimatePopup());
        }

        IEnumerator AnimatePopup()
        {
            float elapsed = 0f;
            Color originalColor = damageText.color;

            while (elapsed < UIConfig.PopupLifetime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / UIConfig.PopupLifetime;

                transform.position = startPos + Vector3.up * (UIConfig.PopupMoveDistance * t);

                Color c = originalColor;
                c.a = UIConfig.PopupAlphaCurve.Evaluate(t);
                damageText.color = c;

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
