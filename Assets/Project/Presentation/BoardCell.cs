using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using Chess.Core;

namespace Chess.Presentation
{
    /// <summary>
    /// 덱빌더 체스판의 각 칸
    /// </summary>
    public class BoardCell : MonoBehaviour, IPointerClickHandler
    {
        public enum CellType { Normal, FreeSlot, KingSlot, PawnSlot }

        [Header("UI")]
        public Image backgroundImage;
        public Image unitIconImage;
        public TextMeshProUGUI enhanceText;   // +n 강화 표시 (나중에 추가)

        // 칸 정보
        public CellType cellType = CellType.Normal;
        public int slotIndex = -1;           // FreeSlot일 때 0~6 인덱스
        public UnitDefinition placedUnit;

        // 색상 설정
        private Color defaultColor;
        private Color availableColor = new Color(0.29f, 0.45f, 1f, 0.25f);   // 파란 강조
        private Color selectedColor = new Color(0.29f, 0.45f, 1f, 0.45f);   // 선택된 슬롯
        private Color placedRareColor = new Color(0.18f, 0.36f, 0.6f, 0.5f);
        private Color placedEpicColor = new Color(0.35f, 0.18f, 0.6f, 0.5f);
        private Color placedLegendaryColor = new Color(0.6f, 0.5f, 0.1f, 0.5f);
        private Color placedCommonColor = new Color(0.25f, 0.28f, 0.38f, 0.5f);

        private System.Action<BoardCell> onClickCallback;

        void Awake()
        {
            if (backgroundImage == null)
                backgroundImage = GetComponent<Image>();

            defaultColor = new Color(0.10f, 0.13f, 0.21f);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnCellClicked();
        }

        // ───────────────────────────────────────
        // 초기화
        // ───────────────────────────────────────

        public void Setup(CellType type, int index, System.Action<BoardCell> onClick, bool isDarkCell)
        {
            cellType = type;
            slotIndex = index;
            onClickCallback = onClick;

            if (backgroundImage != null)
            {
                defaultColor = isDarkCell
                    ? new Color(0.10f, 0.13f, 0.21f)
                    : new Color(0.13f, 0.17f, 0.26f);
                backgroundImage.color = defaultColor;
            }

            switch (type)
            {
                case CellType.Normal:
                    SetInteractable(false);
                    if (backgroundImage != null) backgroundImage.color = defaultColor;
                    break;
                case CellType.FreeSlot:
                    SetInteractable(true);
                    if (backgroundImage != null) backgroundImage.color = defaultColor;
                    break;
                case CellType.PawnSlot:
                    SetUnitIcon(null, showChessSymbol: true, symbol: "♟");
                    SetInteractable(false);
                    if (backgroundImage != null) backgroundImage.color = defaultColor;
                    break;
                case CellType.KingSlot:
                    SetUnitIcon(null, showChessSymbol: true, symbol: "♚");
                    SetInteractable(false);
                    if (backgroundImage != null)
                        backgroundImage.color = new Color(0.6f, 0.5f, 0.1f, 0.15f);
                    break;
            }
        }

        // ───────────────────────────────────────
        // 유닛 배치 / 제거
        // ───────────────────────────────────────

        public void PlaceUnit(UnitDefinition unit)
        {
            placedUnit = unit;

            if (unit == null)
            {
                SetUnitIcon(null);
                SetColor(defaultColor);
                return;
            }

            // 아이콘
            if (unit.icon != null)
                SetUnitIcon(unit.icon);
            else
                SetUnitIcon(null, showChessSymbol: true, symbol: GetChessSymbol(unit));

            // 등급별 배경색
            SetColor(GetRarityColor(unit.rarity));
        }

        public void ClearUnit()
        {
            PlaceUnit(null);
        }

        // ───────────────────────────────────────
        // 상태 표시
        // ───────────────────────────────────────

        /// <summary>배치 가능한 빈 슬롯 강조 (깜빡임은 Animator로 처리)</summary>
        public void SetAvailableHighlight(bool on)
        {
            if (placedUnit != null) return;  // 배치된 유닛 있으면 강조 안 함
            SetColor(on ? availableColor : defaultColor);
        }

        public void SetSelectedHighlight(bool on)
        {
            SetColor(on ? selectedColor : (placedUnit != null ? GetRarityColor(placedUnit.rarity) : defaultColor));
        }

        // ───────────────────────────────────────
        // 내부 유틸
        // ───────────────────────────────────────

        void SetColor(Color c)
        {
            if (backgroundImage != null)
                backgroundImage.color = c;
        }

        void SetUnitIcon(Sprite sprite, bool showChessSymbol = false, string symbol = "")
        {
            if (unitIconImage == null) return;

            if (sprite != null)
            {
                unitIconImage.sprite = sprite;
                unitIconImage.color = Color.white;
                unitIconImage.enabled = true;
            }
            else if (showChessSymbol && enhanceText != null)
            {
                unitIconImage.enabled = false;
                enhanceText.text = symbol;
            }
            else
            {
                unitIconImage.enabled = false;
                if (enhanceText != null) enhanceText.text = "";
            }
        }

        void SetInteractable(bool value)
        {
            // IPointerClickHandler 방식으로 변경, 비상호작용은 cellType으로 제어
        }

        Color GetRarityColor(UnitRarity rarity)
        {
            switch (rarity)
            {
                case UnitRarity.Legendary: return placedLegendaryColor;
                case UnitRarity.Epic: return placedEpicColor;
                case UnitRarity.Rare: return placedRareColor;
                default: return placedCommonColor;
            }
        }

        string GetChessSymbol(UnitDefinition unit)
        {
            if (unit.isKing) return "♚";
            if (unit.isPawn) return "♟";

            // MovementAttribute 기반 심볼
            if ((unit.moveAttributes & MovementAttribute.Queen) != 0) return "♛";
            if ((unit.moveAttributes & MovementAttribute.Rook) != 0) return "♜";
            if ((unit.moveAttributes & MovementAttribute.Bishop) != 0) return "♝";
            if ((unit.moveAttributes & MovementAttribute.Knight) != 0) return "♞";

            return "♙";
        }

        void OnCellClicked()
        {
            onClickCallback?.Invoke(this);
        }
    }
}