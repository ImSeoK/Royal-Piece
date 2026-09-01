using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Chess.Core;

namespace Chess.Presentation
{
    public class CombatAnimator : MonoBehaviour
    {
        [Header("프리팹")]
        public GameObject attackLinePrefab;
        public GameObject damagePopupPrefab;

        [Header("스킬 이펙트 Canvas (UI 기반 이펙트용)")]
        public Canvas skillEffectCanvas;

        private Dictionary<int, UnitView> unitViews;

        public void Initialize(Dictionary<int, UnitView> unitViews)
        {
            this.unitViews = unitViews;
        }

        public IEnumerator PlayAttackEffect(UnitState attacker, UnitState target, int damage)
        {
            Vector3 attackerPos = unitViews[attacker.id].transform.position;
            Vector3 targetPos = unitViews[target.id].transform.position;

            if (attackLinePrefab != null)
            {
                GameObject lineObj = Instantiate(attackLinePrefab, Vector3.zero, Quaternion.identity);
                lineObj.GetComponent<AttackLine>().Initialize(attackerPos, targetPos);
            }

            if (unitViews.ContainsKey(target.id))
                StartCoroutine(unitViews[target.id].PlayHitEffect());

            if (damagePopupPrefab != null)
            {
                GameObject popup = Instantiate(damagePopupPrefab, targetPos, Quaternion.identity);
                popup.GetComponent<DamagePopup>().Initialize(damage, targetPos);
            }

            yield return new WaitForSeconds(0.35f);
        }

        public IEnumerator PlayDotEffect(UnitState target, int damage)
        {
            if (!unitViews.ContainsKey(target.id)) yield break;

            Vector3 targetPos = unitViews[target.id].transform.position;

            if (unitViews.ContainsKey(target.id))
                StartCoroutine(unitViews[target.id].PlayHitEffect());

            if (damagePopupPrefab != null)
            {
                GameObject popup = Instantiate(damagePopupPrefab, targetPos, Quaternion.identity);
                popup.GetComponent<DamagePopup>().Initialize(damage, targetPos);
            }

            yield return new WaitForSeconds(0.25f);
        }

        public IEnumerator PlayDeathEffect(UnitView view)
        {
            AudioManager.Instance?.Play("sfx_unit_death");
            yield return StartCoroutine(view.PlayDeathEffect());
        }

        public void SpawnDamagePopup(int amount, Vector3 position)
        {
            if (damagePopupPrefab == null || amount == 0) return;
            GameObject popup = Instantiate(damagePopupPrefab, position, Quaternion.identity);
            popup.GetComponent<DamagePopup>()?.Initialize(amount, position);
        }

        public void SpawnBuffPopup(SkillDefinition skill, Vector3 position)
        {
            if (damagePopupPrefab == null) return;
            string text;
            Color color;
            if (!UIConfig.TryGetBuffPopup(skill, out text, out color)) return;
            GameObject popup = Instantiate(damagePopupPrefab, position, Quaternion.identity);
            popup.GetComponent<DamagePopup>()?.Initialize(text, color, position);
        }

        public IEnumerator PlaySkillEffect(GameObject prefab, Vector3 position)
        {
            if (prefab == null) yield break;

            // RectTransform 기반 UI 프리팹이면 Canvas 자식으로 생성 후 위치 변환
            if (prefab.GetComponent<RectTransform>() != null && skillEffectCanvas != null)
            {
                GameObject obj = Instantiate(prefab, skillEffectCanvas.transform);
                RectTransform rt = obj.GetComponent<RectTransform>();
                Camera cam = skillEffectCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null : skillEffectCanvas.worldCamera;
                Vector2 screenPos = Camera.main.WorldToScreenPoint(position);
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    skillEffectCanvas.GetComponent<RectTransform>(), screenPos, cam, out Vector2 localPos))
                    rt.anchoredPosition = localPos;
            }
            else
            {
                Instantiate(prefab, position, Quaternion.identity);
            }

            yield return new WaitForSeconds(0.4f);
        }
    }
}
