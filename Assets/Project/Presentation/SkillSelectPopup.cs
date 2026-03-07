using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Chess.Core;

namespace Chess.Presentation
{
    public class SkillSelectPopup : MonoBehaviour
    {
        [Header("팝업")]
        public GameObject popupRoot;
        public Button closeButton;
        public Button confirmButton;
        public TextMeshProUGUI selectedCountText;

        [Header("스킬 목록")]
        public Transform skillListContent;
        public GameObject skillItemPrefab;

        [Header("슬롯 (SkillPanel)")]
        public Image slot1Icon;
        public Image slot2Icon;

        private int activeSlotIndex = -1;
        private SkillDefinition[] slotSkills = new SkillDefinition[2];
        private List<GameObject> spawnedItems = new List<GameObject>();
        private List<SkillDefinition> availableSkills = new List<SkillDefinition>();

        void Awake()
        {
            if (popupRoot != null) popupRoot.SetActive(false);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmClicked);
        }

        // 슬롯 클릭 시 외부에서 호출
        public void OpenForSlot(int slotIndex, List<SkillDefinition> skills)
        {
            activeSlotIndex = slotIndex;
            availableSkills = skills;

            foreach (var obj in spawnedItems) if (obj != null) Destroy(obj);
            spawnedItems.Clear();

            if (popupRoot != null) popupRoot.SetActive(true);

            foreach (var skill in availableSkills)
            {
                if (skill == null) continue;
                SkillDefinition captured = skill;
                GameObject item = Instantiate(skillItemPrefab, skillListContent);
                spawnedItems.Add(item);

                Image icon = item.transform.Find("SkillIcon")?.GetComponent<Image>();
                if (icon != null && skill.icon != null) { icon.sprite = skill.icon; icon.color = Color.white; }

                TextMeshProUGUI[] texts = item.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length > 0) texts[0].text = skill.skillName;
                if (texts.Length > 1) texts[1].text = skill.description;

                // 현재 슬롯에 선택된 스킬 하이라이트
                Button btn = item.GetComponent<Button>();
                Image bg = item.GetComponent<Image>();
                if (slotSkills[slotIndex] == skill && bg != null)
                    bg.color = new Color(0.29f, 0.62f, 1f, 0.15f);

                if (btn != null) btn.onClick.AddListener(() => OnSkillSelected(captured, item));
            }

            UpdateUI();
        }

        void OnSkillSelected(SkillDefinition skill, GameObject item)
        {
            if (activeSlotIndex < 0) return;

            // 다른 슬롯에 이미 같은 스킬 있으면 무시
            int otherSlot = activeSlotIndex == 0 ? 1 : 0;
            if (slotSkills[otherSlot] == skill) return;

            slotSkills[activeSlotIndex] = skill;

            // 슬롯 아이콘 업데이트
            Image slotIcon = activeSlotIndex == 0 ? slot1Icon : slot2Icon;
            if (slotIcon != null && skill.icon != null)
            {
                slotIcon.sprite = skill.icon;
                slotIcon.color = Color.white;
                slotIcon.gameObject.SetActive(true);
            }

            Close();
        }

        void OnConfirmClicked() => Close();

        public void Close()
        {
            if (popupRoot != null) popupRoot.SetActive(false);
        }

        public List<string> GetSelectedSkillNames()
        {
            var names = new List<string>();
            foreach (var s in slotSkills)
                if (s != null) names.Add(s.skillName);
            return names;
        }

        void UpdateUI()
        {
            int count = 0;
            foreach (var s in slotSkills) if (s != null) count++;
            if (selectedCountText != null) selectedCountText.text = $"{count}";
        }
    }
}