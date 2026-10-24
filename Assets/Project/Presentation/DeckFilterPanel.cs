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
        public Toggle toggleAllType;
        public Toggle toggleRook;
        public Toggle toggleBishop;
        public Toggle toggleKnight;
        public Toggle toggleEnchanter;
        public Toggle toggleQueen;

        [Header("등급 토글")]
        public Toggle toggleAllRarity;
        public Toggle toggleLegendary;
        public Toggle toggleEpic;
        public Toggle toggleRare;
        public Toggle toggleCommon;

        public System.Action OnApply;

        bool _suppressUpdate;

        public void Initialize()
        {
            RegisterAllToggle(toggleAllType,   ResetTypes);
            RegisterAllToggle(toggleAllRarity, ResetRarities);
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

        void RegisterAllToggle(Toggle toggle, System.Action resetAction)
        {
            if (toggle == null) return;
            toggle.transition = Selectable.Transition.None;
            toggle.graphic = null;
            toggle.onValueChanged.AddListener(isOn =>
            {
                if (_suppressUpdate) return;
                if (isOn) resetAction();
                UpdateFilterVisuals();
                OnApply?.Invoke();
            });
        }

        void ResetTypes()
        {
            if (toggleRook != null)       toggleRook.isOn       = false;
            if (toggleBishop != null)     toggleBishop.isOn     = false;
            if (toggleKnight != null)     toggleKnight.isOn     = false;
            if (toggleEnchanter != null)  toggleEnchanter.isOn  = false;
            if (toggleQueen != null)      toggleQueen.isOn      = false;
        }

        void ResetRarities()
        {
            if (toggleLegendary != null)  toggleLegendary.isOn  = false;
            if (toggleEpic != null)       toggleEpic.isOn       = false;
            if (toggleRare != null)       toggleRare.isOn       = false;
            if (toggleCommon != null)     toggleCommon.isOn     = false;
        }

        void RegisterToggle(Toggle toggle)
        {
            if (toggle == null) return;
            toggle.transition = Selectable.Transition.None;
            toggle.graphic = null;
            toggle.onValueChanged.AddListener(_ => { UpdateFilterVisuals(); OnApply?.Invoke(); });
        }

        static readonly Color InactiveTextColor = new Color(1f, 1f, 1f, 0.3f);
        static readonly Color ActiveType       = new Color(1.00f, 0.95f, 0.00f); // 순수 노랑
        static readonly Color ActiveLegendary  = new Color(1.00f, 0.80f, 0.00f); // 진한 골드
        static readonly Color ActiveEpic       = new Color(0.75f, 0.20f, 1.00f); // 진한 보라
        static readonly Color ActiveRare       = new Color(0.10f, 0.60f, 1.00f); // 진한 파랑
        static readonly Color ActiveCommon     = new Color(0.70f, 0.80f, 0.90f); // 회청

        void UpdateFilterVisuals()
        {
            bool noTypeFilter    = GetActiveTypes().Count == 0;
            bool noRarityFilter  = GetActiveRarities().Count == 0;

            SetFilterToggle(toggleAllType,   noTypeFilter,                   Color.white);
            SetFilterToggle(toggleRook,      toggleRook?.isOn ?? false,      ActiveType);
            SetFilterToggle(toggleBishop,    toggleBishop?.isOn ?? false,    ActiveType);
            SetFilterToggle(toggleKnight,    toggleKnight?.isOn ?? false,    ActiveType);
            SetFilterToggle(toggleEnchanter, toggleEnchanter?.isOn ?? false, ActiveType);
            SetFilterToggle(toggleQueen,     toggleQueen?.isOn ?? false,     ActiveType);

            SetFilterToggle(toggleAllRarity, noRarityFilter,                 Color.white);
            SetFilterToggle(toggleLegendary, toggleLegendary?.isOn ?? false, ActiveLegendary);
            SetFilterToggle(toggleEpic,      toggleEpic?.isOn ?? false,      ActiveEpic);
            SetFilterToggle(toggleRare,      toggleRare?.isOn ?? false,      ActiveRare);
            SetFilterToggle(toggleCommon,    toggleCommon?.isOn ?? false,    ActiveCommon);

            _suppressUpdate = true;
            if (toggleAllType   != null) toggleAllType.isOn   = noTypeFilter;
            if (toggleAllRarity != null) toggleAllRarity.isOn = noRarityFilter;
            _suppressUpdate = false;
        }

        void SetFilterToggle(Toggle toggle, bool active, Color activeColor)
        {
            if (toggle == null) return;

            // 배경: 활성 시 표시, 비활성 시 투명
            Image bg = toggle.targetGraphic as Image
                    ?? toggle.GetComponent<Image>()
                    ?? toggle.GetComponentInChildren<Image>();
            if (bg != null) bg.color = active ? Color.white : Color.clear;

            // 텍스트
            var tmp = toggle.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
                tmp.color = active ? activeColor : InactiveTextColor;
            else
            {
                var legacyText = toggle.GetComponentInChildren<UnityEngine.UI.Text>();
                if (legacyText != null) legacyText.color = active ? activeColor : InactiveTextColor;
            }
        }

        public void Reset()
        {
            ResetTypes();
            ResetRarities();
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
