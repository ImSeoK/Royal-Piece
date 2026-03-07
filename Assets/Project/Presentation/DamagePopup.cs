using UnityEngine;
using TMPro;
using System.Collections;

namespace Chess.Presentation
{
    public class DamagePopup : MonoBehaviour
    {
        public TextMeshProUGUI damageText;
        public float lifetime = 0.8f;
        public float moveDistance = 1f;
        public AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        private Vector3 startPos;

        void Awake()
        {
            if (damageText == null)
            {
                damageText = GetComponentInChildren<TextMeshProUGUI>();
            }
        }

        public void Initialize(int damage, Vector3 worldPosition)
        {
            startPos = worldPosition + Vector3.up * 0.5f;
            transform.position = startPos;

            damageText.text = damage.ToString();
            damageText.fontSize = 80;
            damageText.color = new Color(1f, 0.2f, 0.2f, 1f); // 빨강

            StartCoroutine(AnimatePopup());
        }

        IEnumerator AnimatePopup()
        {
            float elapsed = 0f;
            Color originalColor = damageText.color;

            while (elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / lifetime;

                // 위로 이동
                transform.position = startPos + Vector3.up * (moveDistance * t);

                // 페이드아웃
                Color c = originalColor;
                c.a = alphaCurve.Evaluate(t);
                damageText.color = c;

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}