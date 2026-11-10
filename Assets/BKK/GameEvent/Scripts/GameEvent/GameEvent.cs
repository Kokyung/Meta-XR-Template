using System.Collections.Generic;
using UnityEngine;

namespace BKK.GameEventArchitecture
{
    [CreateAssetMenu(menuName = "BKK/Game Event Architecture/Base Game Event", fileName = "New Base Game Event",order = 0)]
    public class GameEvent : BaseGameEvent
    {
        private readonly HashSet<BaseGameEventListener> listeners = new HashSet<BaseGameEventListener>();

        // Raise 도중 리스너가 등록·해지되어도 순회가 깨지지 않도록 사본을 순회한다. 목록이 바뀔 때만 다시 만든다.
        private BaseGameEventListener[] snapshot;

        /// <summary>
        /// 게임 이벤트에 등록된 모든 게임 이벤트 리스너의 유니티 이벤트들을 호출합니다.
        /// </summary>
        public override void Raise()
        {
            foreach (var listener in GetSnapshot())
            {
                // 앞선 리스너의 호출로 해지된 리스너는 건너뛴다.
                if (!listeners.Contains(listener)) continue;

                listener.RaiseEvent();
                BKKDebug.Log($"{this.name} 이벤트가 실행되었습니다.\n경로: {listener.GetListenerPath()}");
            }
        }

        public override void Cancel()
        {
            foreach (var listener in GetSnapshot())
            {
                // 앞선 리스너의 호출로 해지된 리스너는 건너뛴다.
                if (!listeners.Contains(listener)) continue;

                listener.StopEvent();
                BKKDebug.Log($"{this.name} 이벤트가 취소되었습니다.\n경로: {listener.GetListenerPath()}");
            }
        }

        /// <summary>
        /// 게임 이벤트 리스너를 등록합니다.
        /// </summary>
        /// <param name="listener">등록할 게임 이벤트 리스너</param>
        public override void Register(BaseGameEventListener listener)
        {
            if (listeners.Add(listener)) snapshot = null;
        }

        /// <summary>
        /// 게임 이벤트 리스너를 해지합니다.
        /// </summary>
        /// <param name="listener">해지할 게임 이벤트 리스너</param>
        public override void Deregister(BaseGameEventListener listener)
        {
            if (listeners.Remove(listener)) snapshot = null;
        }

        /// <summary>
        /// 게임 이벤트에 등록된 게임 이벤트 리스너가 있는지 체크합니다.
        /// </summary>
        /// <returns></returns>
        public override bool HasListeners()
        {
            return listeners.Count > 0;
        }

        private BaseGameEventListener[] GetSnapshot()
        {
            if (snapshot != null) return snapshot;

            snapshot = new BaseGameEventListener[listeners.Count];
            listeners.CopyTo(snapshot);
            return snapshot;
        }
    }
    
    public class GameEvent<T> : BaseGameEvent<T>
    {
        private readonly HashSet<BaseGameEventListener<T>> listeners = new HashSet<BaseGameEventListener<T>>();

        // Raise 도중 리스너가 등록·해지되어도 순회가 깨지지 않도록 사본을 순회한다. 목록이 바뀔 때만 다시 만든다.
        private BaseGameEventListener<T>[] snapshot;

        /// <summary>
        /// 게임 이벤트에 등록된 모든 게임 이벤트 리스너의 유니티 이벤트들을 호출합니다.
        /// </summary>
        public override void Raise(T value)
        {
            foreach (var listener in GetSnapshot())
            {
                // 앞선 리스너의 호출로 해지된 리스너는 건너뛴다.
                if (!listeners.Contains(listener)) continue;

                listener.RaiseEvent(value);
                BKKDebug.Log($"{this.name} 이벤트가 실행되었습니다. / 매개변수: {value}\n경로: {listener.GetListenerPath()}");
            }
        }

        public override void Cancel(T value)
        {
            foreach (var listener in GetSnapshot())
            {
                // 앞선 리스너의 호출로 해지된 리스너는 건너뛴다.
                if (!listeners.Contains(listener)) continue;

                listener.StopEvent(value);
                BKKDebug.Log($"{this.name} 이벤트가 취소되었습니다. / 매개변수: {value}\n경로: {listener.GetListenerPath()}");
            }
        }

        /// <summary>
        /// 게임 이벤트 리스너를 등록합니다.
        /// </summary>
        /// <param name="listener">등록할 게임 이벤트 리스너</param>
        public override void Register(BaseGameEventListener<T> listener)
        {
            if (listeners.Add(listener)) snapshot = null;
        }

        /// <summary>
        /// 게임 이벤트 리스너를 해지합니다.
        /// </summary>
        /// <param name="listener">해지할 게임 이벤트 리스너</param>
        public override void Deregister(BaseGameEventListener<T> listener)
        {
            if (listeners.Remove(listener)) snapshot = null;
        }

        /// <summary>
        /// 게임 이벤트에 등록된 게임 이벤트 리스너가 있는지 체크합니다.
        /// </summary>
        /// <returns></returns>
        public override bool HasListeners()
        {
            return listeners.Count > 0;
        }

        private BaseGameEventListener<T>[] GetSnapshot()
        {
            if (snapshot != null) return snapshot;

            snapshot = new BaseGameEventListener<T>[listeners.Count];
            listeners.CopyTo(snapshot);
            return snapshot;
        }
    }
}
