using System;
using System.Linq;
using System.Reflection;
using BKK.Extension;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public class InspectorButtonAttribute : Attribute
{
    public readonly string name;

    public readonly bool executeOnlyPlaying = true; 

    public InspectorButtonAttribute(string name = "", bool executeOnlyPlaying = false)
    {
        this.name = name;
        this.executeOnlyPlaying = executeOnlyPlaying;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof (MonoBehaviour), true)]
public class InspectorButton : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var mono = target as MonoBehaviour;
        
        var methods = mono.GetType()
            .GetMembers(BindingFlags.Instance | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                        BindingFlags.NonPublic)
            .Where(o => Attribute.IsDefined(o, typeof (InspectorButtonAttribute)));
 
        
        foreach (var memberInfo in methods)
        {
            var attribute = Attribute.GetCustomAttribute(memberInfo, typeof(InspectorButtonAttribute)) as InspectorButtonAttribute;
            var name = attribute.name != string.Empty ? attribute.name : memberInfo.Name;
            name = name.InsertSpacesBetweenWords();
            var canExecute = (attribute.executeOnlyPlaying && Application.isPlaying) || !attribute.executeOnlyPlaying;
            
            if (GUILayout.Button(name))
            {
                if (!canExecute)
                {
                    Debug.Log("플레이 중이 아니라서 실행되지 않았습니다.");
                    return;
                }
                
                var method = memberInfo as MethodInfo;
                method.Invoke(mono, null);
                EditorUtility.SetDirty(target);
            }
        }
    }
}
#endif
