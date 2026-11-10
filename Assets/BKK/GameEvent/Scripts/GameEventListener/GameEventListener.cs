using System.Collections;
using BKK.Extension;
using UnityEngine;
using UnityEngine.Events;

namespace BKK.GameEventArchitecture
{
    public class GameEventListener : BaseGameEventListener
    {
        public GameEvent GameEvent => gameEvent as GameEvent;
        
        [Tooltip("이벤트 시작할 때 호출될 유니티 이벤트"), SerializeField]
        private UnityEvent onStart;

        [Tooltip("이벤트 끝날 때 호출될 유니티 이벤트"), SerializeField]
        private UnityEvent onEnd;
        
        [Tooltip("이벤트 취소할 때 호출될 유니티 이벤트"), SerializeField]
        private UnityEvent onCancel;
        
        public UnityEvent OnStart => onStart;

        public UnityEvent OnEnd => onEnd;

        public UnityEvent OnCancel => onCancel;

        [Tooltip("OnStart가 다시 호출되기 까지 걸리는 시간"), SerializeField]
        private float restartDelay = 0f;

        [Tooltip("OnStart가 호출되기 까지 걸리는 시간"), SerializeField]
        private float startTiming = 0f;
        
        [Tooltip("OnEnd가 호출되기 까지 걸리는 시간"), SerializeField]
        private float endTiming = 1f;

        [HideInInspector] public bool startDelayed = false;
        [HideInInspector] public bool endDelayed = false;
        [HideInInspector] public bool restartDelayed = false;

        private Coroutine startRoutine;
        private Coroutine endRoutine;

        // 비활성 상태의 리스너는 이벤트를 받지 않도록 활성화 상태에 맞춰 등록·해지한다.
        private void OnEnable()
        {
            ResetState();

            if (gameEvent == null)
            {
                Debug.LogError($"{this.gameObject.name}의 Game Event Listener에 Game Event가 존재하지 않습니다.");
                return;
            }

            gameEvent.Register(this);
        }

        private void OnDisable()
        {
            // 비활성화·파괴되면 대기 중인 호출도 함께 중단한다.
            StopRoutines();
            ResetState();

            if (gameEvent != null) gameEvent.Deregister(this);
        }

        private void ResetState()
        {
            startDelayed = false;
            endDelayed = false;
            restartDelayed = false;
        }

        private void OnValidate()
        {
            restartDelay = Mathf.Clamp(restartDelay, 0, float.MaxValue);
            startTiming = Mathf.Clamp(startTiming, 0, float.MaxValue);
            endTiming = Mathf.Clamp(endTiming, 0, float.MaxValue);
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawIcon(transform.position,"");
        }

        /// <summary>
        /// 단일 게임 이벤트 리스너에 등록된 유니티 이벤트를 호출합니다.
        /// </summary>
        public override void RaiseEvent()
        {
            // 딜레이 중에는 onStart와 onEnd 모두 호출하지 않는다.
            if (restartDelayed) return;

            startRoutine = StartCoroutine(RunEvent());
            if (!endDelayed) endRoutine = StartCoroutine(RunEndEvent());
        }

        /// <summary>
        /// 이벤트를 정지합니다.
        /// </summary>
        public override void StopEvent()
        {
            onCancel.Invoke();
            StopRoutines();
            ResetState();
        }

        // Time.timeScale의 영향을 받지 않도록 실제 시간으로 대기한다.
        private IEnumerator RunEvent()
        {
            startDelayed = true;
            restartDelayed = true;

            // 딜레이가 0이면 Raise 호출 안에서 바로 onStart를 호출한다.
            if (startTiming > 0) yield return new WaitForSecondsRealtime(startTiming);

            startDelayed = false;

            onStart.Invoke();

            if (restartDelay > 0) yield return new WaitForSecondsRealtime(restartDelay);

            restartDelayed = false;
        }

        private IEnumerator RunEndEvent()
        {
            endDelayed = true;

            if (endTiming > 0) yield return new WaitForSecondsRealtime(endTiming);

            onEnd.Invoke();

            endDelayed = false;
        }

        private void StopRoutines()
        {
            if (startRoutine != null) StopCoroutine(startRoutine);
            if (endRoutine != null) StopCoroutine(endRoutine);

            startRoutine = null;
            endRoutine = null;
        }

        public override GameEventAsset GetGameEventAsset()
        {
            return gameEvent;
        }

        public override string GetListenerPath()
        {
            return this.GetPath();
        }
    }
    
    public class GameEventListener<Ttype, Tevent, Tresponse> : BaseGameEventListener<Ttype>
        where Tevent : GameEvent<Ttype> 
        where Tresponse : UnityEvent<Ttype>
    {
        // [Tooltip("등록할 게임 이벤트"), SerializeField]
        // public Tevent gameEvent;
        public Tevent GameEvent => gameEvent as Tevent;

        [Tooltip("이벤트 시작할 때 호출될 유니티 이벤트"), SerializeField]
        private Tresponse onStart;

        [Tooltip("이벤트 끝날 때 호출될 유니티 이벤트"), SerializeField]
        private Tresponse onEnd;
        
        [Tooltip("이벤트 취소할 때 호출될 유니티 이벤트"), SerializeField]
        private Tresponse onCancel;
        
        // [Tooltip("이벤트 시작할 때 호출될 유니티 이벤트")]
        public Tresponse OnStart => onStart;

        // [Tooltip("이벤트 끝날 때 호출될 유니티 이벤트")]
        public Tresponse OnEnd => onEnd;
        
        // [Tooltip("이벤트 취소할 때 호출될 유니티 이벤트")]
        public Tresponse OnCancel => onCancel;

        [Tooltip("OnStart가 다시 호출되기 까지 걸리는 시간"), SerializeField]
        private float restartDelay = 0f;

        [Tooltip("OnStart가 호출되기 까지 걸리는 시간"), SerializeField]
        private float startTiming = 0f;
        
        [Tooltip("OnEnd가 호출되기 까지 걸리는 시간"), SerializeField]
        private float endTiming = 1f;

        [HideInInspector] public bool startDelayed = false;
        [HideInInspector] public bool endDelayed = false;
        [HideInInspector] public bool restartDelayed = false;

        private Coroutine startRoutine;
        private Coroutine endRoutine;

        // 비활성 상태의 리스너는 이벤트를 받지 않도록 활성화 상태에 맞춰 등록·해지한다.
        private void OnEnable()
        {
            ResetState();

            if (gameEvent == null)
            {
                Debug.LogError($"{this.gameObject.name}의 Game Event Listener에 Game Event가 존재하지 않습니다.");
                return;
            }

            gameEvent.Register(this);
        }

        private void OnDisable()
        {
            // 비활성화·파괴되면 대기 중인 호출도 함께 중단한다.
            StopRoutines();
            ResetState();

            if (gameEvent != null) gameEvent.Deregister(this);
        }

        private void ResetState()
        {
            startDelayed = false;
            endDelayed = false;
            restartDelayed = false;
        }

        private void OnValidate()
        {
            restartDelay = Mathf.Clamp(restartDelay, 0, float.MaxValue);
            startTiming = Mathf.Clamp(startTiming, 0, float.MaxValue);
            endTiming = Mathf.Clamp(endTiming, 0, float.MaxValue);
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawIcon(transform.position,"");
        }

        /// <summary>
        /// 단일 게임 이벤트 리스너에 등록된 유니티 이벤트를 호출합니다.
        /// </summary>
        public override void RaiseEvent(Ttype value)
        {
            // 딜레이 중에는 onStart와 onEnd 모두 호출하지 않는다.
            if (restartDelayed) return;

            startRoutine = StartCoroutine(RunEvent(value));
            if (!endDelayed) endRoutine = StartCoroutine(RunEndEvent(value));
        }

        /// <summary>
        /// 이벤트를 정지합니다.
        /// </summary>
        public override void StopEvent(Ttype value)
        {
            onCancel.Invoke(value);
            StopRoutines();
            ResetState();
        }

        // Time.timeScale의 영향을 받지 않도록 실제 시간으로 대기한다.
        private IEnumerator RunEvent(Ttype value)
        {
            startDelayed = true;
            restartDelayed = true;

            // 딜레이가 0이면 Raise 호출 안에서 바로 onStart를 호출한다.
            if (startTiming > 0) yield return new WaitForSecondsRealtime(startTiming);

            startDelayed = false;

            onStart.Invoke(value);

            if (restartDelay > 0) yield return new WaitForSecondsRealtime(restartDelay);

            restartDelayed = false;
        }

        private IEnumerator RunEndEvent(Ttype value)
        {
            endDelayed = true;

            if (endTiming > 0) yield return new WaitForSecondsRealtime(endTiming);

            onEnd.Invoke(value);

            endDelayed = false;
        }

        private void StopRoutines()
        {
            if (startRoutine != null) StopCoroutine(startRoutine);
            if (endRoutine != null) StopCoroutine(endRoutine);

            startRoutine = null;
            endRoutine = null;
        }

        public override GameEventAsset GetGameEventAsset()
        {
            return gameEvent;
        }

        public override string GetListenerPath()
        {
            return this.GetPath();
        }
    }
}
