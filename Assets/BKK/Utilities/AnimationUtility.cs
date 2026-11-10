using UnityEngine;

public static class AnimationUtility
{
    /// <summary>
    /// 이름이 일치하는 Animator 파라미터의 타입을 찾는다.
    /// </summary>
    /// <returns>파라미터가 있으면 true</returns>
    public static bool TryGetParameterType(this Animator animator, string parameterName,
        out AnimatorControllerParameterType type)
    {
        // Animator.parameters는 접근할 때마다 배열을 새로 할당하므로 한 번만 가져온다.
        var parameters = animator.parameters;

        for (var index = 0; index < parameters.Length; index++)
        {
            if (parameters[index].name == parameterName)
            {
                type = parameters[index].type;
                return true;
            }
        }

        type = default;
        return false;
    }

    public static bool CheckParameter(this Animator animator, string paramName)
    {
        return animator.TryGetParameterType(paramName, out _);
    }
}
