using UnityEngine;
using UnityEngine.UI;
using Chess.Core;

namespace Chess.Presentation
{
    public class SkillSlotUI : MonoBehaviour
    {
        [Header("UI")]
        public Image skillIcon;
        public GameObject cooldownOverlay;
        public Button button;

        public SkillDefinition Skill => skill;

        private SkillDefinition skill;
        private System.Action<SkillDefinition> onSkillUsed;

        void Awake()
        {
            if (button != null)
                button.onClick.AddListener(OnButtonClicked);
        }

        public void SetSkill(SkillDefinition def, System.Action<SkillDefinition> callback)
        {
            skill = def;
            onSkillUsed = callback;

            if (skill == null)
            {
                Clear();
                return;
            }

            if (skillIcon != null)
            {
                skillIcon.sprite = skill.icon;
                skillIcon.color = skill.icon != null ? Color.white : new Color(1f, 1f, 1f, 0.2f);
            }

            if (cooldownOverlay != null)
                cooldownOverlay.SetActive(false);

            SetInteractable(true);
        }

        void OnButtonClicked()
        {
            if (skill == null) return;
            onSkillUsed?.Invoke(skill);
        }

        public void UpdateUses(int remaining)
        {
            bool exhausted = remaining <= 0;
            SetInteractable(!exhausted);
            if (cooldownOverlay != null)
                cooldownOverlay.SetActive(exhausted);
        }

        public void SetInteractable(bool value)
        {
            if (button != null)
                button.interactable = value;
        }

        public void Clear()
        {
            skill = null;

            if (skillIcon != null)
                skillIcon.color = new Color(1f, 1f, 1f, 0.2f);

            if (cooldownOverlay != null)
                cooldownOverlay.SetActive(false);

            SetInteractable(false);
        }
    }
}