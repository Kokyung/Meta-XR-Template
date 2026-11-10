using UnityEngine;

public class NoFoldoutAttribute : PropertyAttribute
{
    /// <summary>
    /// 자식 포함 여부
    /// </summary>
    public bool includeChildren { get; private set; }

    public NoFoldoutAttribute(bool includeChildren = true)
    {
        this.includeChildren = includeChildren;
    }
}
