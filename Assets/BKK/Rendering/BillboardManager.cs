using System.Collections.Generic;
using BKK.Rendering;
using Cysharp.Threading.Tasks;

public static class BillboardManager
{
    private static List<Billboard> billboards = new List<Billboard>();
    
    private static bool running = false;

    public static bool Running => running;

    public static void Register(Billboard billboard)
    {
        billboards.Add(billboard);
    }

    public static void Unregister(Billboard billboard)
    {
        if (billboards.Contains(billboard)) billboards.Remove(billboard);
    }

    public static async void Run()
    {
        if (!running) running = true;
        
        while (running)
        {
            foreach (var billboard in billboards)
            {
                if (billboard) billboard.Look();
                // Graphics.DrawMeshInstancedIndirect();
            }

            await UniTask.DelayFrame(1, PlayerLoopTiming.PreUpdate);
        }
    }

    public static void Stop()
    {
        running = false;
    }
}
