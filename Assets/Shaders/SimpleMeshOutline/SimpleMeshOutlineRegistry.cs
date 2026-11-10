using UnityEngine;

namespace BKK.Graphics
{
    /// <summary>
    /// 화면에 그릴 Screen Space Outline 대상이 있는지 알려 주는 등록부.
    /// 렌더러 리스트가 비었는지 묻는 엔진 API가 없어 대상 쪽에서 직접 알린다.
    /// 아무것도 등록하지 않으면 Screen Space Outline 패스를 기록하지 않으므로,
    /// 등록을 쓰지 않는 프로젝트는 Renderer Feature에서 등록 없이 그리기를 켠다.
    /// </summary>
    public static class SimpleMeshOutlineRegistry
    {
        private static int activeCount;

        /// <summary>현재 켜져 있는 아웃라인 대상 수.</summary>
        public static int ActiveCount => activeCount;

        /// <summary>이번 프레임에 Screen Space Outline 패스를 기록해야 하는지.</summary>
        public static bool HasActiveTarget => activeCount > 0;

        /// <summary>아웃라인 대상 하나가 보이기 시작했음을 알린다.</summary>
        public static void Add()
        {
            activeCount++;
        }

        /// <summary>아웃라인 대상 하나가 사라졌음을 알린다.</summary>
        public static void Remove()
        {
            // Add 없이 Remove가 불린 것이다. 그대로 삼키면 짝이 어긋난 원인을 되짚을 단서가 남지 않는다.
            if (activeCount <= 0)
            {
                activeCount = 0;
                Debug.LogWarning("[아웃라인] 활성 대상 수가 이미 0인데 Remove가 불렸습니다. " +
                                 "SimpleMeshOutlineRegistry의 Add/Remove 짝이 맞지 않습니다.");
                return;
            }

            activeCount--;
        }

        // 도메인 리로드를 끈 상태에서 플레이 모드를 반복해도 이전 값이 남지 않도록 초기화한다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            activeCount = 0;
        }
    }
}
