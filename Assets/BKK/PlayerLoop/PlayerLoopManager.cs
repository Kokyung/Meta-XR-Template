using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityPlayerLoop = UnityEngine.LowLevel.PlayerLoop;

namespace BKK.PlayerLoop
{
    /// <summary>
    /// 커스텀 PlayerLoop 제어 및 관리 클래스
    /// </summary>
    internal static class PlayerLoopManager
    {
        private static readonly LoopList<IPostEarlyUpdate> postEarlyUpdates = new();
        private static readonly LoopList<IPreFixedUpdate> preFixedUpdates = new();
        private static readonly LoopList<IPostFixedUpdate> postFixedUpdates = new();
        private static readonly LoopList<IPreUpdate> preUpdates = new();
        private static readonly LoopList<IPostUpdate> postUpdates = new();
        private static readonly LoopList<IPreLateUpdate> preLateUpdates = new();
        private static readonly LoopList<IPostLateUpdate> postLateUpdates = new();

        private static readonly LoopList<IPostFixedAnimationUpdate> postLegacyFixedAnimationUpdates = new();
        private static readonly LoopList<IPostAnimationUpdate> postLegacyAnimationUpdates = new();

        private static readonly LoopList<IPrePhysicsSimulate> prePhysicsSimulates = new();
        private static readonly LoopList<IPostPhysicsSimulate> postPhysicsSimulates = new();

        private enum InsertPosition
        {
            Before,
            After,
            Last,
        }

        /// <summary>
        /// ILoop 인터페이스를 사용하는 컴포넌트를 등록한다.
        /// </summary>
        /// <param name="update"></param>
        /// <typeparam name="T"></typeparam>
        public static void Register<T>(T update) where T : ILoop
        {
            if (update is IPostEarlyUpdate postEarlyUpdate) postEarlyUpdates.Add(postEarlyUpdate);
            if (update is IPreFixedUpdate preFixedUpdate) preFixedUpdates.Add(preFixedUpdate);
            if (update is IPostFixedUpdate postFixedUpdate) postFixedUpdates.Add(postFixedUpdate);
            if (update is IPreUpdate preUpdate) preUpdates.Add(preUpdate);
            if (update is IPostUpdate postUpdate) postUpdates.Add(postUpdate);
            if (update is IPreLateUpdate preLateUpdate) preLateUpdates.Add(preLateUpdate);
            if (update is IPostLateUpdate postLateUpdate) postLateUpdates.Add(postLateUpdate);
            if (update is IPostFixedAnimationUpdate postFixedAnimationUpdate) postLegacyFixedAnimationUpdates.Add(postFixedAnimationUpdate);
            if (update is IPostAnimationUpdate postAnimationUpdate) postLegacyAnimationUpdates.Add(postAnimationUpdate);
            if (update is IPrePhysicsSimulate prePhysicsSimulate) prePhysicsSimulates.Add(prePhysicsSimulate);
            if (update is IPostPhysicsSimulate postPhysicsSimulate) postPhysicsSimulates.Add(postPhysicsSimulate);
        }

        public static void Unregister<T>(T update) where T : ILoop
        {
            if (update is IPostEarlyUpdate postEarlyUpdate) postEarlyUpdates.Remove(postEarlyUpdate);
            if (update is IPreFixedUpdate preFixedUpdate) preFixedUpdates.Remove(preFixedUpdate);
            if (update is IPostFixedUpdate postFixedUpdate) postFixedUpdates.Remove(postFixedUpdate);
            if (update is IPreUpdate preUpdate) preUpdates.Remove(preUpdate);
            if (update is IPostUpdate postUpdate) postUpdates.Remove(postUpdate);
            if (update is IPreLateUpdate preLateUpdate) preLateUpdates.Remove(preLateUpdate);
            if (update is IPostLateUpdate postLateUpdate) postLateUpdates.Remove(postLateUpdate);
            if (update is IPostFixedAnimationUpdate postFixedAnimationUpdate) postLegacyFixedAnimationUpdates.Remove(postFixedAnimationUpdate);
            if (update is IPostAnimationUpdate postAnimationUpdate) postLegacyAnimationUpdates.Remove(postAnimationUpdate);
            if (update is IPrePhysicsSimulate prePhysicsSimulate) prePhysicsSimulates.Remove(prePhysicsSimulate);
            if (update is IPostPhysicsSimulate postPhysicsSimulate) postPhysicsSimulates.Remove(postPhysicsSimulate);
        }

        /// <summary>
        /// Domain Reload를 끈 에디터에서는 이전 플레이의 등록 목록이 남으므로 씬 로드 전에 비운다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            postEarlyUpdates.Clear();
            preFixedUpdates.Clear();
            postFixedUpdates.Clear();
            preUpdates.Clear();
            postUpdates.Clear();
            preLateUpdates.Clear();
            postLateUpdates.Clear();
            postLegacyFixedAnimationUpdates.Clear();
            postLegacyAnimationUpdates.Clear();
            prePhysicsSimulates.Clear();
            postPhysicsSimulates.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            PlayerLoopSystem playerLoop = UnityPlayerLoop.GetCurrentPlayerLoop();

            // 정적 플래그 대신 현재 루프를 기준으로 판단한다.
            // 이전 플레이에서 넣은 시스템이 남아 있든, 다른 코드가 루프를 초기화했든 항상 한 벌만 들어가게 된다.
            RemoveSystems(ref playerLoop);

            InsertSystem(ref playerLoop, "EarlyUpdate", null, InsertPosition.Last, OnPostEarlyUpdate);

            InsertSystem(ref playerLoop, "FixedUpdate", "ScriptRunBehaviourFixedUpdate", InsertPosition.Before, OnPreFixedUpdate);
            InsertSystem(ref playerLoop, "FixedUpdate", "ScriptRunBehaviourFixedUpdate", InsertPosition.After, OnPostFixedUpdate);
            // DirectorFixedUpdate(Animator의 Animate Physics)와 LegacyFixedAnimationUpdate(Animation)가 모두 끝난 뒤
            InsertSystem(ref playerLoop, "FixedUpdate", "LegacyFixedAnimationUpdate", InsertPosition.After, OnPostLegacyFixedAnimationUpdate);
            // SimulationMode.FixedUpdate에서 Unity가 물리 시뮬레이션을 하는 시스템의 앞뒤
            InsertSystem(ref playerLoop, "FixedUpdate", "PhysicsFixedUpdate", InsertPosition.Before, OnPreFixedPhysicsSimulate);
            InsertSystem(ref playerLoop, "FixedUpdate", "PhysicsFixedUpdate", InsertPosition.After, OnPostFixedPhysicsSimulate);

            InsertSystem(ref playerLoop, "PreUpdate", null, InsertPosition.Last, OnPreUpdate);

            InsertSystem(ref playerLoop, "Update", "ScriptRunBehaviourUpdate", InsertPosition.After, OnPostUpdate);

            // Animator(DirectorUpdateAnimationBegin~End)와 Animation(LegacyAnimationUpdate) 갱신이 모두 끝난 뒤
            InsertSystem(ref playerLoop, "PreLateUpdate", "DirectorUpdateAnimationEnd", InsertPosition.After, OnPostAnimationUpdate);
            // SimulationMode.Update에서 Unity가 물리 시뮬레이션을 하는 시스템의 앞뒤
            InsertSystem(ref playerLoop, "PreLateUpdate", "PhysicsLateUpdate", InsertPosition.Before, OnPreUpdatePhysicsSimulate);
            InsertSystem(ref playerLoop, "PreLateUpdate", "PhysicsLateUpdate", InsertPosition.After, OnPostUpdatePhysicsSimulate);
            InsertSystem(ref playerLoop, "PreLateUpdate", "ScriptRunBehaviourLateUpdate", InsertPosition.Before, OnPreLateUpdate);

            InsertSystem(ref playerLoop, "PostLateUpdate", null, InsertPosition.Last, OnPostLateUpdate);

            UnityPlayerLoop.SetPlayerLoop(playerLoop);
        }

        /// <summary>
        /// 각 단계에서 이 클래스가 넣은 시스템을 모두 제거한다.
        /// </summary>
        private static void RemoveSystems(ref PlayerLoopSystem playerLoop)
        {
            for (var i = 0; i < playerLoop.subSystemList.Length; i++)
            {
                var phase = playerLoop.subSystemList[i];
                if (phase.subSystemList == null) continue;

                phase.subSystemList = Array.FindAll(phase.subSystemList, s => s.type != typeof(PlayerLoopManager));
                playerLoop.subSystemList[i] = phase;
            }
        }

        /// <summary>
        /// phaseName 단계의 targetName 시스템 앞이나 뒤, 또는 단계의 맨 마지막에 시스템을 삽입한다.
        /// 대상을 찾지 못하면 오류만 남기고 건너뛰어 나머지 시스템 삽입과 SetPlayerLoop는 계속 진행한다.
        /// </summary>
        private static void InsertSystem(ref PlayerLoopSystem playerLoop, string phaseName, string targetName,
            InsertPosition position, PlayerLoopSystem.UpdateFunction updateFunction)
        {
            var phaseIndex = Array.FindIndex(playerLoop.subSystemList, s => s.type?.Name == phaseName);
            if (phaseIndex < 0)
            {
                Debug.LogError($"PlayerLoop에서 {phaseName} 단계를 찾지 못해 {updateFunction.Method.Name} 삽입을 건너뜁니다.");
                return;
            }

            var phase = playerLoop.subSystemList[phaseIndex];
            var systems = new List<PlayerLoopSystem>(phase.subSystemList ?? Array.Empty<PlayerLoopSystem>());
            var insertIndex = systems.Count;

            if (position != InsertPosition.Last)
            {
                var targetIndex = systems.FindIndex(s => s.type?.Name == targetName);
                if (targetIndex < 0)
                {
                    Debug.LogError($"PlayerLoop에서 {phaseName}.{targetName} 시스템을 찾지 못해 {updateFunction.Method.Name} 삽입을 건너뜁니다.");
                    return;
                }

                insertIndex = position == InsertPosition.Before ? targetIndex : targetIndex + 1;
            }

            systems.Insert(insertIndex, new PlayerLoopSystem
            {
                subSystemList = null,
                updateDelegate = updateFunction,
                type = typeof(PlayerLoopManager)
            });

            phase.subSystemList = systems.ToArray();
            playerLoop.subSystemList[phaseIndex] = phase;
        }

        private static void CreatePlayerLoopAtLast(ref PlayerLoopSystem playerLoop, string subSystemName, PlayerLoopSystem.UpdateFunction updateFunction)
        {
            PlayerLoopSystem loopSystem = new PlayerLoopSystem
            {
                subSystemList = null,
                updateDelegate = updateFunction,
                type = typeof(PlayerLoopManager)
            };

            int subSystemIndex = Array.FindIndex(playerLoop.subSystemList, (s) => s.type.Name == subSystemName);
            PlayerLoopSystem targetLoopSystem = playerLoop.subSystemList[subSystemIndex];
            var subSystemList = new List<PlayerLoopSystem>(targetLoopSystem.subSystemList);
            subSystemList.Add(loopSystem);
            targetLoopSystem.subSystemList = subSystemList.ToArray();
            playerLoop.subSystemList[subSystemIndex] = targetLoopSystem;
        }

        private static void OnPostEarlyUpdate() => postEarlyUpdates.Invoke(e => e.PostEarlyUpdate());

        private static void OnPreFixedUpdate() => preFixedUpdates.Invoke(e => e.PreFixedUpdate());

        private static void OnPostFixedUpdate() => postFixedUpdates.Invoke(e => e.PostFixedUpdate());

        private static void OnPreUpdate() => preUpdates.Invoke(e => e.PreUpdate());

        private static void OnPostUpdate() => postUpdates.Invoke(e => e.PostUpdate());

        private static void OnPreLateUpdate() => preLateUpdates.Invoke(e => e.PreLateUpdate());

        private static void OnPostLateUpdate() => postLateUpdates.Invoke(e => e.PostLateUpdate());

        private static void OnPostLegacyFixedAnimationUpdate()
        {
            postLegacyFixedAnimationUpdates.Invoke(e => e.PostLegacyFixedAnimationUpdate());
        }

        private static void OnPostAnimationUpdate()
        {
            postLegacyAnimationUpdates.Invoke(e => e.PostLegacyAnimationUpdate());
        }

        private static bool simulating;

        /// <summary>
        /// Script 모드에서 물리 시뮬레이션을 직접 실행한다. 시뮬레이션 전후 콜백도 이 호출을 감싸서 함께 호출된다.
        /// Script 모드에서는 Unity가 시뮬레이션하지 않으므로, 이 메서드를 호출하지 않으면 물리와 전후 콜백 모두 실행되지 않는다.
        /// </summary>
        /// <param name="deltaTime">시뮬레이션할 시간</param>
        public static void SimulatePhysics(float deltaTime)
        {
            if (Physics.simulationMode != SimulationMode.Script)
            {
                Debug.LogWarning($"Physics.simulationMode가 {Physics.simulationMode}이므로 직접 시뮬레이션하지 않습니다. Script 모드에서만 호출하세요.");
                return;
            }

            // 전후 콜백 안에서 다시 호출하면 콜백이 무한히 중첩되므로 막는다.
            if (simulating)
            {
                Debug.LogError("물리 시뮬레이션 전후 콜백 안에서 PlayerLoopManager.Simulate를 다시 호출할 수 없습니다.");
                return;
            }

            simulating = true;

            try
            {
                prePhysicsSimulates.Invoke(e => e.PrePhysicsSimulate());
                Physics.Simulate(deltaTime);
                postPhysicsSimulates.Invoke(e => e.PostPhysicsSimulate());
            }
            finally
            {
                simulating = false;
            }
        }

        // 물리 시뮬레이션 전후 콜백은 Unity가 자동으로 시뮬레이션하는 모드에서 그 위치에서만 호출한다.
        // Script 모드는 Simulate를 직접 호출할 때 그 호출을 감싸서 호출한다.
        private static void OnPreFixedPhysicsSimulate()
        {
            if (Physics.simulationMode == SimulationMode.FixedUpdate) prePhysicsSimulates.Invoke(e => e.PrePhysicsSimulate());
        }

        private static void OnPostFixedPhysicsSimulate()
        {
            if (Physics.simulationMode == SimulationMode.FixedUpdate) postPhysicsSimulates.Invoke(e => e.PostPhysicsSimulate());
        }

        private static void OnPreUpdatePhysicsSimulate()
        {
            if (Physics.simulationMode == SimulationMode.Update) prePhysicsSimulates.Invoke(e => e.PrePhysicsSimulate());
        }

        private static void OnPostUpdatePhysicsSimulate()
        {
            if (Physics.simulationMode == SimulationMode.Update) postPhysicsSimulates.Invoke(e => e.PostPhysicsSimulate());
        }

        /// <summary>
        /// 베이스 루프 인터페이스입니다.
        /// </summary>
        public interface ILoop { }

        // public enum PlayerLoopType
        // {
        //     PostEarlyUpdate,
        //     PreFixedUpdate,
        //     FixedUpdate,
        //     PostFixedUpdate,
        //     PreUpdate,
        //     Update,
        //     PostUpdate,
        //     PreLateUpdate,
        //     LateUpdate,
        //     PostLateUpdate,
        //     PostLegacyAnimationUpdate,
        //     PostLegacyFixedAnimationUpdate,
        // }
    }

    /// <summary>
    /// 호출 중에 등록·해지되어도 안전한 콜백 목록.
    /// 순회는 목록이 바뀔 때만 다시 만드는 사본 배열로 하고, 순회 도중 해지된 항목은 건너뛴다.
    /// </summary>
    internal sealed class LoopList<T> where T : class
    {
        private readonly List<T> items = new();
        private readonly HashSet<T> itemSet = new();
        private T[] snapshot = Array.Empty<T>();
        private bool dirty;

        /// <summary>
        /// 이미 등록된 항목이면 추가하지 않는다.
        /// </summary>
        public void Add(T item)
        {
            if (item == null || !itemSet.Add(item)) return;

            items.Add(item);
            dirty = true;
        }

        public void Remove(T item)
        {
            if (item == null || !itemSet.Remove(item)) return;

            items.Remove(item);
            dirty = true;
        }

        public void Clear()
        {
            items.Clear();
            itemSet.Clear();
            dirty = true;
        }

        /// <summary>
        /// 등록된 항목마다 call을 호출한다. 한 항목에서 예외가 나도 나머지 항목은 계속 호출한다.
        /// </summary>
        public void Invoke(Action<T> call)
        {
            if (dirty)
            {
                snapshot = items.ToArray();
                dirty = false;
            }

            var current = snapshot;

            for (var i = 0; i < current.Length; i++)
            {
                var item = current[i];

                if (!itemSet.Contains(item)) continue;

                // 해지하지 않고 파괴된 컴포넌트는 목록에서 정리하고 건너뛴다.
                if (IsDestroyed(item))
                {
                    Remove(item);
                    continue;
                }

                try
                {
                    call(item);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        private static bool IsDestroyed(T item)
        {
            var target = item is Delegate callback ? callback.Target : item;

            // 파괴된 UnityEngine.Object는 C# 참조로는 null이 아니므로 Unity의 == 연산자로 확인한다.
            return target is UnityEngine.Object unityObject && unityObject == null;
        }
    }

    internal interface IPostEarlyUpdate : PlayerLoopManager.ILoop
    {
        /// <summary>
        /// FixedUpdate 전에 호출되는 EarlyUpdate가 호출된 직후에 호출됩니다.
        /// </summary>
        public void PostEarlyUpdate();
    }

    internal interface IPreFixedUpdate : PlayerLoopManager.ILoop
    {
        /// <summary>
        /// FixedUpdate 호출 전에 호출됩니다. 호출 주기는 FixedUpdate와 동일합니다.
        /// </summary>
        public void PreFixedUpdate();
    }

    internal interface IPostFixedUpdate : PlayerLoopManager.ILoop
    {
        /// <summary>
        /// FixedUpdate 호출 후에 호출됩니다. 호출 주기는 FixedUpdate와 동일합니다.
        /// </summary>
        public void PostFixedUpdate();
    }

    internal interface IPreUpdate : PlayerLoopManager.ILoop
    {
        /// <summary>
        /// Update 호출 전에 호출됩니다.
        /// </summary>
        public void PreUpdate();
    }

    internal interface IPostUpdate : PlayerLoopManager.ILoop
    {
        /// <summary>
        /// Update 호출 후에 호출됩니다. 애니메이션 갱신(PreLateUpdate 단계)보다 먼저 호출됩니다.
        /// </summary>
        public void PostUpdate();
    }

    internal interface IPreLateUpdate : PlayerLoopManager.ILoop
    {
        /// <summary>
        /// LateUpdate 호출 전에 호출됩니다.
        /// </summary>
        public void PreLateUpdate();
    }

    internal interface IPostLateUpdate : PlayerLoopManager.ILoop
    {
        /// <summary>
        /// LateUpdate 호출 후에 호출됩니다.
        /// </summary>
        public void PostLateUpdate();
    }

    internal interface IPostAnimationUpdate : PlayerLoopManager.ILoop
    {
        /// <summary>
        /// Animator와 Animation 갱신이 끝난 직후, LateUpdate보다 먼저 호출됩니다.
        /// </summary>
        public void PostLegacyAnimationUpdate();
    }

    internal interface IPostFixedAnimationUpdate : PlayerLoopManager.ILoop
    {
        /// <summary>
        /// FixedUpdate 단계의 Animator(Animate Physics)와 Animation 갱신이 끝난 직후, 물리 시뮬레이션 전에 호출됩니다.
        /// </summary>
        public void PostLegacyFixedAnimationUpdate();
    }

    internal interface IPrePhysicsSimulate : PlayerLoopManager.ILoop
    {
        /// <summary>
        /// 물리 시뮬레이션 직전에 호출됩니다.
        /// Physics.simulationMode가 FixedUpdate면 FixedUpdate 주기로, Update면 매 프레임,
        /// Script면 PlayerLoopManager.Simulate를 호출할 때마다 호출됩니다.
        /// </summary>
        public void PrePhysicsSimulate();
    }

    internal interface IPostPhysicsSimulate : PlayerLoopManager.ILoop
    {
        /// <summary>
        /// 물리 시뮬레이션 직후에 호출됩니다.
        /// Physics.simulationMode가 FixedUpdate면 FixedUpdate 주기로, Update면 매 프레임,
        /// Script면 PlayerLoopManager.Simulate를 호출할 때마다 호출됩니다.
        /// </summary>
        public void PostPhysicsSimulate();
    }
}
