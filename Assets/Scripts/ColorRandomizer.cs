using System.Collections.Generic;
using UnityEngine;

public class ColorRandomizer : MonoBehaviour
{
    public List<Renderer> renderers;
    
    public void RandomizeColors()
    {
        Random.InitState(System.DateTime.Now.Millisecond);
        
        foreach (var renderer in renderers)
        {
            if (renderer != null)
            {
                var c = Random.ColorHSV();
                c.a = 1;
                renderer.material.color = c;
            }
        }
    }
}
