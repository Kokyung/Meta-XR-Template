using System;
using UnityEngine;

#if DOTWEEN
using DG.Tweening;
#endif

namespace BKK.PlayerLoop
{
    /// <summary>
    /// PlayerLoop(Update문) 정밀 제어를 위한 매니저 클래스.
    /// PuppetMaster와 DOTween이 여기서 ManualUpdate를 수행한다.
    /// </summary>
    public sealed class UpdateManager : Singleton<UpdateManager>, IPostEarlyUpdate, IPreFixedUpdate, IPostFixedUpdate,
        IPreUpdate, IPostUpdate, IPreLateUpdate, IPostLateUpdate,
        IPostAnimationUpdate, IPostFixedAnimationUpdate, IPrePhysicsSimulate, IPostPhysicsSimulate
    {
        public static int tweenerCapacity = 5000;
        public static int sequencesCapacity = 50;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            GetInstance();

            // 물리 연산 수동 업데이트 버그 있어서 주석 처리
            // UnityEngine.Physics.simulationMode = SimulationMode.Script;
            // Physics.simulationMode는 프로젝트 설정을 그대로 따르고, 모드에 맞는 위치에서 시뮬레이션 전후 콜백을 호출한다.
            // UnityEngine.Physics.simulationMode = SimulationMode.FixedUpdate;

#if DOTWEEN
            // 동시 트윈 제한을 늘려서 트윈 제한 도달 순간 프레임 드랍 막기.
            DOTween.Init().SetCapacity(tweenerCapacity, sequencesCapacity);
#endif
        }

        private void OnEnable()
        {
            PlayerLoopManager.Register(this);
        }

        private void OnDisable()
        {
            PlayerLoopManager.Unregister(this);
        }

        public void PostEarlyUpdate()
        {
            UpdateCallbacks.onPostEarlyUpdate.Invoke();
        }

        public void PreFixedUpdate()
        {
            UpdateCallbacks.onPreFixedUpdate.Invoke();
        }

        private void FixedUpdate()
        {
            UpdateCallbacks.onFixedUpdate.Invoke();
        }

        public void PostFixedUpdate()
        {
            UpdateCallbacks.onPostFixedUpdate.Invoke();
        }

        public void PreUpdate()
        {
            UpdateCallbacks.onPreUpdate.Invoke();
        }

        private void Update()
        {
            UpdateCallbacks.onUpdate.Invoke();
        }

        public void PostUpdate()
        {
            UpdateCallbacks.onPostUpdate.Invoke();

            // 애니메이션 업데이트와 같은 타이밍에 호출
            ManualTweenUpdate(); // 호출 위치 변경하지 말 것!!!
        }

        public void PreLateUpdate()
        {
            UpdateCallbacks.onPreLateUpdate.Invoke();
        }

        private void LateUpdate()
        {
            UpdateCallbacks.onLateUpdate.Invoke();
        }

        public void PostLateUpdate()
        {
            UpdateCallbacks.onPostLateUpdate.Invoke();
        }

        public void PostLegacyAnimationUpdate()
        {
            UpdateCallbacks.onPostAnimationUpdate.Invoke();
        }

        public void PostLegacyFixedAnimationUpdate()
        {
            UpdateCallbacks.onPostFixedAnimationUpdate.Invoke();
        }

        /// <summary>
        /// 물리 시뮬레이션 직전에 PlayerLoopManager가 호출한다. Script 모드에서는 PlayerLoopManager.Simulate 호출 시에만 호출된다.
        /// 참고: https://rootmotion.freshdesk.com/support/solutions/articles/77000057786-faq
        /// </summary>
        public void PrePhysicsSimulate()
        {
            UpdateCallbacks.onBeforePreSimulate.Invoke();

            // PuppetManager.OnPreSimulate();

            UpdateCallbacks.onAfterPreSimulate.Invoke();
        }

        /// <summary>
        /// 물리 시뮬레이션 직후에 PlayerLoopManager가 호출한다.
        /// </summary>
        public void PostPhysicsSimulate()
        {
            UpdateCallbacks.onBeforePostSimulate.Invoke();

            // PuppetManager.OnPostSimulate();

            UpdateCallbacks.onAfterPostSimulate.Invoke();
        }

        /// <summary>
        /// 참고: https://dotween.demigiant.com/documentation.php
        /// </summary>
        private void ManualTweenUpdate()
        {
#if DOTWEEN
            DOTween.ManualUpdate(Time.deltaTime, Time.unscaledDeltaTime);
#endif
        }
    }

    // public static class PuppetManager
    // {
    //     private static HashSet<PuppetMaster> puppetMasters = new HashSet<PuppetMaster>();
    //
    //     public static void RegisterPuppet(PuppetMaster p)
    //     {
    //         if (!PhysicsUtility.IsScriptMode()) return;
    //
    //         if (puppetMasters.Contains(p)) return;
    //
    //         puppetMasters.Add(p);
    //     }
    //
    //     public static void UnregisterPuppet(PuppetMaster p)
    //     {
    //         if (!PhysicsUtility.IsScriptMode()) return;
    //
    //         if (!puppetMasters.Contains(p)) return;
    //
    //         puppetMasters.Remove(p);
    //     }
    //
    //     public static void OnPreSimulate()
    //     {
    //         foreach (PuppetMaster puppetMaster in puppetMasters)
    //         {
    //             if (puppetMaster.initiated) puppetMaster.OnPreSimulate(Time.deltaTime);
    //         }
    //     }
    //
    //     public static void OnPostSimulate()
    //     {
    //         foreach (PuppetMaster puppetMaster in puppetMasters)
    //         {
    //             if (puppetMaster.initiated) puppetMaster.OnPostSimulate();
    //         }
    //     }
    // }

    public static class UpdateCallbacks
    {
        public static ActionList onPostEarlyUpdate = new();
        public static ActionList onPreFixedUpdate = new();
        public static ActionList onFixedUpdate = new();
        public static ActionList onPostFixedUpdate = new();
        public static ActionList onPreUpdate = new();
        public static ActionList onUpdate = new();
        public static ActionList onPostUpdate = new();
        public static ActionList onPreLateUpdate = new();
        public static ActionList onLateUpdate = new();
        public static ActionList onPostLateUpdate = new();

        public static ActionList onPostAnimationUpdate = new();
        public static ActionList onPostFixedAnimationUpdate = new();

        public static ActionList onBeforePreSimulate = new();
        public static ActionList onAfterPreSimulate = new();
        public static ActionList onBeforePostSimulate = new();
        public static ActionList onAfterPostSimulate = new();

        /// <summary>
        /// Domain Reload를 끈 에디터에서는 이전 플레이의 구독이 남으므로 씬 로드 전에 비운다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            onPostEarlyUpdate.Clear();
            onPreFixedUpdate.Clear();
            onFixedUpdate.Clear();
            onPostFixedUpdate.Clear();
            onPreUpdate.Clear();
            onUpdate.Clear();
            onPostUpdate.Clear();
            onPreLateUpdate.Clear();
            onLateUpdate.Clear();
            onPostLateUpdate.Clear();

            onPostAnimationUpdate.Clear();
            onPostFixedAnimationUpdate.Clear();

            onBeforePreSimulate.Clear();
            onAfterPreSimulate.Clear();
            onBeforePostSimulate.Clear();
            onAfterPostSimulate.Clear();
        }
    }

    /// <summary>
    /// 같은 콜백은 한 번만 등록된다. 호출 중 추가·제거와 콜백 예외에도 나머지 콜백은 계속 호출된다.
    /// </summary>
    public class ActionList
    {
        private readonly LoopList<Action> actions = new LoopList<Action>();

        public void Add(Action action)
        {
            actions.Add(action);
        }

        public void Remove(Action action)
        {
            actions.Remove(action);
        }

        public void Clear()
        {
            actions.Clear();
        }

        public void Invoke()
        {
            actions.Invoke(action => action());
        }
    }
}
