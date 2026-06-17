using UnityEngine;

namespace Chess.Presentation
{
    /// <summary>
    /// Animator 기반 이펙트 프리팹에 붙여서 애니메이션 종료 시 자동 삭제.
    /// Animation Event에서 DestroySelf() 호출하거나, duration을 직접 지정.
    /// </summary>
    public class AutoDestroy : MonoBehaviour
    {
        [Tooltip("0이면 Animation Event 방식 사용. 0보다 크면 해당 시간(초) 후 자동 삭제.")]
        public float duration = 0f;

        void Start()
        {
            if (duration > 0f)
                Destroy(gameObject, duration);
        }

        // Animation Event에서 호출
        public void DestroySelf()
        {
            Destroy(gameObject);
        }
    }
}
