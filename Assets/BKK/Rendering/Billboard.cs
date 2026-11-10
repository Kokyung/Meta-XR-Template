using UnityEngine;

namespace BKK.Rendering
{
    /// <summary>
    /// 빌보드 컴포넌트입니다. 
    /// 
    /// 작성자: 변고경
    /// 작성일자: 2021/8/5
    /// </summary>
    public sealed class Billboard : MonoBehaviour
    {
        public enum RotationOnly
        {
            None,
            X,
            Y,
            Z
        }
        
        [HideInInspector] public Camera cam;

        [Tooltip("회전할 축")] public RotationOnly rotationOnly = RotationOnly.Y;
        
        [Tooltip("좌우 반전 여부")] public bool flipX = true;// 기존 Billboard 방식을 Sprite에 적용하면 스프라이트가 좌우 반전이 되기 때문에 기본을 true로 함.

        public bool manual = false;
        
        private Vector3 v;
    
        private void Update()
        {
            if (!manual) Look();
        }

        /// <summary>
        /// 메인 카메라(플레이어)를 바라본다.
        /// </summary>
        public void Look()
        {
            if (cam == null)
            {
                if (Camera.main == null)
                {
                    Debug.LogWarning("빌보드: 메인 카메라가 존재하지 않습니다.");
                    return;
                }
                
                else cam = Camera.main;
            }

            v = cam.transform.position - transform.position;

            switch (rotationOnly)
            {
                case RotationOnly.X:
                    v.y = 0;
                    v.z = 0;
                    break;
                case RotationOnly.Y:
                    v.x = 0;
                    v.z = 0;
                    break;
                case RotationOnly.Z:
                    v.x = 0;
                    v.y = 0;
                    break;
                case RotationOnly.None:
                    v = Vector3.zero;
                    break;
                default:
                    v = Vector3.zero;
                    break;
            }

            transform.LookAt(cam.transform.position - v);
            if (flipX) transform.rotation *= Quaternion.Euler(0, -180, 0);
        }
    }
}