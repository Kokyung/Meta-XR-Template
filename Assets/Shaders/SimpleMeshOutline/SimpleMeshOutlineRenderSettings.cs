using UnityEngine;

namespace BKK.Graphics
{
    /// <summary>
    /// SimpleMeshOutlineUnionFeature가 참조하는 전역 설정.
    /// 아웃라인은 다른 렌더링 기능을 알지 못하므로, 필요한 값은 게임 쪽에서 밀어 넣는다.
    /// </summary>
    public static class SimpleMeshOutlineRenderSettings
    {
        /// <summary>
        /// Renderer Feature가 그리는 대상 레이어. 기본값은 모든 레이어다.
        /// Screen Space Outline은 화면 공간에서 합성되어 특정 영역 안에 가둘 수 없으므로,
        /// 화면에 직접 보이지 않아야 하는 레이어가 있으면 그 레이어를 뺀 값을 넣는다.
        /// </summary>
        public static int LayerMask { get; set; } = ~0;

        // 도메인 리로드를 끈 상태에서 플레이 모드를 반복해도 이전 값이 남지 않도록 초기화한다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            LayerMask = ~0;
        }
    }
}
