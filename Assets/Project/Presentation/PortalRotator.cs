using UnityEngine;

namespace Chess.Presentation
{
    public class PortalRotator : MonoBehaviour
    {
        [Header("링 오브젝트")]
        public RectTransform ring1;
        public RectTransform ring2;
        public RectTransform ring3;

        [Header("회전 속도 (도/초)")]
        public float ring1Speed = 15f;
        public float ring2Speed = -25f;
        public float ring3Speed = 40f;

        void Update()
        {
            if (ring1 != null) ring1.Rotate(0, 0, ring1Speed * Time.deltaTime);
            if (ring2 != null) ring2.Rotate(0, 0, ring2Speed * Time.deltaTime);
            if (ring3 != null) ring3.Rotate(0, 0, ring3Speed * Time.deltaTime);
        }
    }
}