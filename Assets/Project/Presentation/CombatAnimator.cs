using UnityEngine;
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

        private Dictionary<int, UnitView> unitViews;

        public void Initialize(Dictionary<int, UnitView> unitViews)
        {
            this.unitViews = unitViews;
        }

        public IEnumerator PlayAttackEffect(UnitState attacker, UnitState target)
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
                popup.GetComponent<DamagePopup>().Initialize(attacker.definition.attackPower, targetPos);
            }

            yield return new WaitForSeconds(0.15f);
        }

        public IEnumerator PlayDeathEffect(UnitView view)
        {
            yield return StartCoroutine(view.PlayDeathEffect());
        }
    }
}
