using UnityEngine;

namespace BKK.Graphics
{
    /// <summary>
    /// Screen Space Outline 대상임을 알리는 컴포넌트.
    /// 켜고 끌 때 <see cref="SimpleMeshOutlineRegistry"/>의 수를 조절해,
    /// 대상이 없는 프레임에서는 전체 화면 확장 패스를 통째로 건너뛰게 한다.
    /// </summary>
    [AddComponentMenu("BKK/Simple Mesh Outline Target")]
    public sealed class SimpleMeshOutlineRuntime : MonoBehaviour
    {
        private void OnEnable()
        {
            SimpleMeshOutlineRegistry.Add();
        }

        private void OnDisable()
        {
            SimpleMeshOutlineRegistry.Remove();
        }

        // 이전 버전에서 메시를 준비하던 진입점. 지금은 할 일이 없지만 참조를 위해 남겨 둔다.
        public void Refresh() { }
    }
}
