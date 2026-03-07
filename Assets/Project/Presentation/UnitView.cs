using UnityEngine;
using System.Collections;
using Chess.Core;

namespace Chess.Presentation
{
    public class UnitView : MonoBehaviour
    {
        public UnitState state;
        public SpriteRenderer spriteRenderer;
        public TextMesh hpText;

        private Color originalColor;

        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            originalColor = spriteRenderer.color;
        }

        public void Initialize(UnitState unitState)
        {
            state = unitState;
            originalColor = spriteRenderer.color;

            // ========== 아이콘 설정 (추가!) ==========
            if (spriteRenderer != null && state.definition.icon != null)
            {
                spriteRenderer.sprite = state.definition.icon;
                Debug.Log($"[UnitView] {state.definition.unitName} 아이콘 설정됨");
            }
            else if (state.definition.icon == null)
            {
                Debug.LogWarning($"[UnitView] {state.definition.unitName} 아이콘이 없습니다!");
            }
            // =========================================

            if (hpText == null)
            {
                CreateHPText();
            }

            UpdateVisuals();
        }

        void CreateHPText()
        {
            GameObject hpObj = new GameObject("HP");
            hpObj.transform.parent = transform;
            hpObj.transform.localPosition = new Vector3(0, 0.4f, 0);

            hpText = hpObj.AddComponent<TextMesh>();
            hpText.fontSize = 50;
            hpText.characterSize = 0.1f;
            hpText.anchor = TextAnchor.MiddleCenter;
            hpText.alignment = TextAlignment.Center;
            hpText.color = Color.red;

            MeshRenderer mr = hpObj.GetComponent<MeshRenderer>();
            mr.sortingOrder = 10;
        }

        public void UpdatePosition()
        {
            if (state == null) return;
            transform.position = new Vector3(state.position.x, state.position.y, 0);
        }

        public void UpdateVisuals()
        {
            if (state == null) return;

            UpdatePosition();

            if (hpText != null)
            {
                hpText.text = state.currentHP.ToString();

                float hpPercent = (float)state.currentHP / state.definition.maxHP;
                if (hpPercent > 0.6f)
                    hpText.color = Color.green;
                else if (hpPercent > 0.3f)
                    hpText.color = Color.yellow;
                else
                    hpText.color = Color.red;
            }
        }

        // 피격 플래시 효과
        public IEnumerator PlayHitEffect()
        {
            for (int i = 0; i < 2; i++)
            {
                spriteRenderer.color = Color.red;
                yield return new WaitForSeconds(0.05f);

                spriteRenderer.color = originalColor;
                yield return new WaitForSeconds(0.05f);
            }
        }

        // 사망 효과
        public IEnumerator PlayDeathEffect()
        {
            float duration = 0.3f;
            float elapsed = 0f;

            Vector3 originalScale = transform.localScale;
            Color startColor = spriteRenderer.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // 살짝 커지면서
                transform.localScale = Vector3.Lerp(originalScale, originalScale * 1.2f, t);

                // 페이드아웃
                Color c = startColor;
                c.a = 1 - t;
                spriteRenderer.color = c;

                if (hpText != null)
                {
                    Color hc = hpText.color;
                    hc.a = 1 - t;
                    hpText.color = hc;
                }

                yield return null;
            }
        }
    }
}