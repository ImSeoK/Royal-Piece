using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using Chess.Core;

namespace Chess.Presentation
{
    public class DeckFilterPanel : MonoBehaviour
    {
        [Header("타입 토글")]
        public Toggle toggleRook;
        public Toggle toggleBishop;
        public Toggle toggleKnight;
        public Toggle toggleEnchanter;
        public Toggle toggleQueen;


        [Header("등급 토글")]
        public Toggle toggleLegendary;
        public Toggle toggleEpic;
        public Toggle toggleRare;
        public Toggle toggleCommon;

        [Header("버튼")]
        public Button applyButton;
        public Button resetButton;
        public Button backgroundOverlay;

        public System.Action OnApply;

        public void Initialize()
        {
            if (applyButton != null)       applyButton.onClick.AddListener(Apply);
            if (resetButton != null)       resetButton.onClick.AddListener(Reset);
            if (backgroundOverlay != null) backgroundOverlay.onClick.AddListener(Close);

            RegisterToggle(toggleRook);
            RegisterToggle(toggleBishop);
            RegisterToggle(toggleKnight);
            RegisterToggle(toggleEnchanter);
            RegisterToggle(toggleQueen);
            RegisterToggle(toggleLegendary);
            RegisterToggle(toggleEpic);
            RegisterToggle(toggleRare);
            RegisterToggle(toggleCommon);

            UpdateFilterVisuals();
        }

        void RegisterToggle(Toggle toggle)
        {
            if (toggle == null) return;
            toggle.transition = Selectable.Transition.None;
            toggle.graphic = null; // Toggle이 isOn 기준으로 alpha 강제 제어하는 것 차단
            toggle.onValueChanged.AddListener(_ => UpdateFilterVisuals());
        }

        static readonly Color InactiveColor     = new Color(1f, 1f, 1f, 0.3f);
        static readonly Color InactiveTextColor = new Color(1f, 1f, 1f, 0.3f);
        static readonly Color ActiveType       = new Color(1.00f, 0.95f, 0.00f); // 순수 노랑
        static readonly Color ActiveLegendary  = new Color(1.00f, 0.80f, 0.00f); // 진한 골드
        static readonly Color ActiveEpic       = new Color(0.75f, 0.20f, 1.00f); // 진한 보라
        static readonly Color ActiveRare       = new Color(0.10f, 0.60f, 1.00f); // 진한 파랑
        static readonly Color ActiveCommon     = new Color(0.70f, 0.80f, 0.90f); // 회청

        void UpdateFilterVisuals()
        {
            SetFilterToggle(toggleRook,      toggleRook?.isOn ?? false,      ActiveType,      true);
            SetFilterToggle(toggleBishop,    toggleBishop?.isOn ?? false,    ActiveType,      true);
            SetFilterToggle(toggleKnight,    toggleKnight?.isOn ?? false,    ActiveType,      true);
            SetFilterToggle(toggleEnchanter, toggleEnchanter?.isOn ?? false, ActiveType,      true);
            SetFilterToggle(toggleQueen,     toggleQueen?.isOn ?? false,     ActiveType,      true);
            SetFilterToggle(toggleLegendary, toggleLegendary?.isOn ?? false, ActiveLegendary, true);
            SetFilterToggle(toggleEpic,      toggleEpic?.isOn ?? false,      ActiveEpic,      true);
            SetFilterToggle(toggleRare,      toggleRare?.isOn ?? false,      ActiveRare,      true);
            SetFilterToggle(toggleCommon,    toggleCommon?.isOn ?? false,    ActiveCommon,    true);
        }

        void SetFilterToggle(Toggle toggle, bool active, Color activeColor, bool applyToText)
        {
            if (toggle == null) return;

            // Background: 원래 스프라이트 유지, 비활성 시 어둡게만
            Image bg = toggle.targetGraphic as Image
                    ?? toggle.GetComponent<Image>()
                    ?? toggle.GetComponentInChildren<Image>();
            if (bg != null) bg.color = active ? Color.white : InactiveColor;

            // 활성 색상 오버레이 (같은 스프라이트 모양으로 위에 씌움)
            var overlay = GetOrCreateTintOverlay(toggle, bg);
            if (overlay != null)
                overlay.color = active ? new Color(activeColor.r, activeColor.g, activeColor.b, 0.80f) : Color.clear;

            // 텍스트
            if (applyToText)
            {
                var tmp = toggle.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null)
                    tmp.color = active ? activeColor : InactiveTextColor;
                else
                {
                    var legacyText = toggle.GetComponentInChildren<UnityEngine.UI.Text>();
                    if (legacyText != null) legacyText.color = active ? activeColor : InactiveTextColor;
                }
            }
        }

        Image GetOrCreateTintOverlay(Toggle toggle, Image source)
        {
            const string overlayName = "_TintOverlay";
            var existing = toggle.transform.Find(overlayName);
            if (existing != null) return existing.GetComponent<Image>();

            var go = new GameObject(overlayName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(toggle.transform, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.color = Color.clear;
            if (source != null)
            {
                img.sprite = source.sprite;
                img.type   = source.type;
            }
            // Background 바로 위, 텍스트 아래에 배치
            go.transform.SetSiblingIndex(source != null ? source.transform.GetSiblingIndex() + 1 : 0);
            return img;
        }

        public void Open()
        {
            gameObject.SetActive(true);
        }

        void Close()
        {
            gameObject.SetActive(false);
        }

        void Apply()
        {
            OnApply?.Invoke();
            Close();
        }

        void Reset()
        {
            SetAll(false);
        }

        void SetAll(bool value)
        {
            if (toggleRook != null)       toggleRook.isOn       = value;
            if (toggleBishop != null)     toggleBishop.isOn     = value;
            if (toggleKnight != null)     toggleKnight.isOn     = value;
            if (toggleEnchanter != null)  toggleEnchanter.isOn  = value;
            if (toggleQueen != null)      toggleQueen.isOn      = value;
            if (toggleLegendary != null)  toggleLegendary.isOn  = value;
            if (toggleEpic != null)       toggleEpic.isOn       = value;
            if (toggleRare != null)       toggleRare.isOn       = value;
            if (toggleCommon != null)     toggleCommon.isOn     = value;
        }

        public HashSet<string> GetActiveTypes()
        {
            var set = new HashSet<string>();
            if (toggleRook != null      && toggleRook.isOn)      set.Add("룩");
            if (toggleBishop != null    && toggleBishop.isOn)    set.Add("비숍");
            if (toggleKnight != null    && toggleKnight.isOn)    set.Add("나이트");
            if (toggleEnchanter != null && toggleEnchanter.isOn) set.Add("인챈터");
            if (toggleQueen != null     && toggleQueen.isOn)     set.Add("퀸");
            return set;
        }

        public HashSet<UnitRarity> GetActiveRarities()
        {
            var set = new HashSet<UnitRarity>();
            if (toggleLegendary != null && toggleLegendary.isOn) set.Add(UnitRarity.Legendary);
            if (toggleEpic != null      && toggleEpic.isOn)      set.Add(UnitRarity.Epic);
            if (toggleRare != null      && toggleRare.isOn)      set.Add(UnitRarity.Rare);
            if (toggleCommon != null    && toggleCommon.isOn)    set.Add(UnitRarity.Common);
            return set;
        }

        public bool HasActiveFilters()
        {
            return GetActiveTypes().Count > 0 || GetActiveRarities().Count > 0;
        }
    }
}
