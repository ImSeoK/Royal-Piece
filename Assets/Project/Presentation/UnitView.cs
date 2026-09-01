using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Chess.Core;

namespace Chess.Presentation
{
    public class UnitView : MonoBehaviour
    {
        public UnitState state;
        public SpriteRenderer spriteRenderer;

        public BuffIconConfig buffIconConfig;

        private SpriteRenderer hpBarBg;
        private SpriteRenderer hpBarFill;
        private Color originalColor;
        private Sprite _whiteSprite;
        private List<GameObject> _indicatorObjects = new List<GameObject>();

        // 인디케이터 월드 크기 상수
        const float worldIndicatorGap     = 0.04f;
        const float worldIndicatorSize    = 0.24f;
        const float worldIndicatorSpacing = 0.24f;
        const int   maxIndicators         = 4;

        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            originalColor = spriteRenderer.color;
            _whiteSprite = CreateWhiteSprite();
        }

        public void Initialize(UnitState unitState)
        {
            state = unitState;

            if (spriteRenderer != null && state.definition.icon != null)
            {
                spriteRenderer.sprite = state.definition.icon;

                float spriteLocalHeight = state.definition.icon.bounds.size.y;
                float scale = state.definition.displayScale / spriteLocalHeight;
                transform.localScale = new Vector3(scale, scale, 1f);
            }
            else if (state.definition.icon == null)
            {
                Debug.LogWarning($"[UnitView] {state.definition.unitName} 아이콘이 없습니다!");
            }

            originalColor = spriteRenderer.color;
            CreateHPBar();
            UpdateVisuals();
        }

        void CreateHPBar()
        {
            if (spriteRenderer.sprite == null) return;

            Sprite white = _whiteSprite;
            float parentScale = transform.localScale.x;
            float halfH = spriteRenderer.sprite.bounds.extents.y;

            // 월드 고정 크기 상수 (조절 필요 시 이 값만 변경)
            const float worldBarWidth  = 0.9f;
            const float worldBarHeight = 0.08f;
            const float worldBarOffset = 0.12f; // 스프라이트 하단에서 아래 여백

            float barWidth  = worldBarWidth  / parentScale;
            float barHeight = worldBarHeight / parentScale;
            float barY      = -halfH - worldBarOffset / parentScale;

            // 배경 바
            GameObject bgObj = new GameObject("HPBar_BG");
            bgObj.transform.SetParent(transform, false);
            bgObj.transform.localPosition = new Vector3(0f, barY, 0f);
            bgObj.transform.localScale = new Vector3(barWidth, barHeight, 1f);

            hpBarBg = bgObj.AddComponent<SpriteRenderer>();
            hpBarBg.sprite = white;
            hpBarBg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
            hpBarBg.sortingOrder = 5;

            // 채움 바 (좌측 피벗으로 좌→우 감소)
            Sprite whitePivotLeft = CreateWhiteSpritePivotLeft();
            GameObject fillObj = new GameObject("HPBar_Fill");
            fillObj.transform.SetParent(bgObj.transform, false);
            fillObj.transform.localPosition = new Vector3(-0.5f, 0f, 0f);
            fillObj.transform.localScale = Vector3.one;

            hpBarFill = fillObj.AddComponent<SpriteRenderer>();
            hpBarFill.sprite = whitePivotLeft;
            hpBarFill.color = Color.green;
            hpBarFill.sortingOrder = 6;
        }

        Sprite CreateWhiteSprite()
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        Sprite CreateWhiteSpritePivotLeft()
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0f, 0.5f), 1f);
        }

        public void UpdatePosition()
        {
            if (state == null) return;
            transform.position = BoardVisualizer.GridToWorld(state.position);
        }

        public void UpdateVisuals()
        {
            if (state == null) return;

            UpdatePosition();

            if (hpBarFill != null)
            {
                float hpPercent = (float)state.currentHP / state.definition.GetEnhancedHP(state.enhanceLevel);
                hpPercent = Mathf.Clamp01(hpPercent);

                Vector3 s = hpBarFill.transform.localScale;
                hpBarFill.transform.localScale = new Vector3(hpPercent, s.y, s.z);

                if (hpPercent > 0.6f)
                    hpBarFill.color = Color.green;
                else if (hpPercent > 0.3f)
                    hpBarFill.color = Color.yellow;
                else
                    hpBarFill.color = Color.red;
            }

            RebuildBuffIndicators();
        }

        void RebuildBuffIndicators()
        {
            foreach (var go in _indicatorObjects)
                if (go != null) Destroy(go);
            _indicatorObjects.Clear();

            if (state == null || spriteRenderer.sprite == null) return;

            // 표시할 항목 수집 (버프 → 상태이상 → DOT 순)
            var entries = new List<(Sprite sprite, Color color)>();

            foreach (var buff in state.activeBuffs)
            {
                Sprite icon;
                if (buff.statType == StatType.DamageReceived && buffIconConfig != null)
                    icon = buffIconConfig.GetDamageReceivedIcon(buff.amount > 0);
                else
                    icon = buffIconConfig != null ? buffIconConfig.GetIcon(buff.statType) : null;
                Color color = GetIndicatorColor(buff);
                entries.Add((icon, color));
            }

            foreach (var dot in state.damageOverTimes)
            {
                Sprite icon = buffIconConfig != null ? buffIconConfig.dotIcon : null;
                entries.Add((icon, UIConfig.DotColor));
            }

            if (entries.Count == 0) return;

            int count = Mathf.Min(entries.Count, maxIndicators);
            float parentScale = transform.localScale.x;
            float halfH = spriteRenderer.sprite.bounds.extents.y;


            const float worldBarOffset = 0.12f;
            const float worldBarHeight = 0.08f;
            float barY = -halfH - worldBarOffset / parentScale;
            float indicatorLocalY = barY - (worldBarHeight * 0.5f + worldIndicatorGap + worldIndicatorSize * 0.5f) / parentScale;
            float indicatorLocalSize    = worldIndicatorSize    / parentScale;
            float indicatorLocalSpacing = worldIndicatorSpacing / parentScale;
            // border는 항상 _whiteSprite(PPU=1, extents=0.5) 기준 → 0.5f * indicatorLocalSize
            float startX = -(0.9f / 2f) / parentScale + 0.5f * indicatorLocalSize;

            const float symbolFraction = 0.72f;

            for (int i = 0; i < count; i++)
            {
                var (sprite, color) = entries[i];

                // 컨테이너
                GameObject go = new GameObject($"Indicator_{i}");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(startX + i * indicatorLocalSpacing, indicatorLocalY, 0f);
                go.transform.localScale = new Vector3(indicatorLocalSize, indicatorLocalSize, 1f);

                // 테두리 (버프 색)
                GameObject border = new GameObject("Border");
                border.transform.SetParent(go.transform, false);
                border.transform.localScale = Vector3.one;
                SpriteRenderer borderSr = border.AddComponent<SpriteRenderer>();
                borderSr.sprite = _whiteSprite;
                borderSr.color = color;
                borderSr.sortingOrder = 7;

                // 배경 (어두운 색)
                GameObject bg = new GameObject("BG");
                bg.transform.SetParent(go.transform, false);
                bg.transform.localScale = new Vector3(0.82f, 0.82f, 1f);
                SpriteRenderer bgSr = bg.AddComponent<SpriteRenderer>();
                bgSr.sprite = _whiteSprite;
                bgSr.color = new Color(0.1f, 0.1f, 0.18f, 1f);
                bgSr.sortingOrder = 8;

                // 심볼
                if (sprite != null)
                {
                    GameObject symbol = new GameObject("Symbol");
                    symbol.transform.SetParent(go.transform, false);
                    float symScale = symbolFraction / sprite.bounds.size.x;
                    symbol.transform.localScale = new Vector3(symScale, symScale, 1f);
                    SpriteRenderer symSr = symbol.AddComponent<SpriteRenderer>();
                    symSr.sprite = sprite;
                    symSr.color = color;
                    symSr.sortingOrder = 9;
                }

                _indicatorObjects.Add(go);
            }
        }

        Color GetIndicatorColor(ActiveBuff buff)
        {
            if (UIConfig.IsStatusEffect(buff.statType)) return UIConfig.StatusColor;
            if (buff.statType == StatType.DamageReceived)
                return buff.amount < 0 ? UIConfig.BuffColor : UIConfig.DebuffColor;
            return buff.amount >= 0 ? UIConfig.BuffColor : UIConfig.DebuffColor;
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
            Color bgStartColor = hpBarBg != null ? hpBarBg.color : Color.clear;
            Color fillStartColor = hpBarFill != null ? hpBarFill.color : Color.clear;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                transform.localScale = Vector3.Lerp(originalScale, originalScale * 1.2f, t);

                Color c = startColor;
                c.a = 1 - t;
                spriteRenderer.color = c;

                if (hpBarBg != null)
                {
                    Color bg = bgStartColor;
                    bg.a = (1 - t) * bgStartColor.a;
                    hpBarBg.color = bg;
                }

                if (hpBarFill != null)
                {
                    Color fill = fillStartColor;
                    fill.a = 1 - t;
                    hpBarFill.color = fill;
                }

                yield return null;
            }
        }
    }
}