using System.Collections.Generic;
using UnityEngine;

// One Euro 필터 논문 기반 노이즈 필터 클래스 모음
// 원문 https://gery.casiez.net/1euro/
// 번역본 https://dancefirst.tistory.com/entry/One-Euro-Filter-%EB%85%BC%EB%AC%B8-%EB%B2%88%EC%97%AD
namespace BKK.Filter.OneEuro
{
    public class OneEuroFilterBase<T>
    {
        public float frequency; // 샘플링 주파수 (Hz)
        public float minCutoff; // 최소 cutoff 주파수
        public float beta; // 속도에 따른 cutoff 보정 계수
        public float dCutoff; // 속도 필터용 cutoff

        protected bool initialized = false;

        protected float Alpha(float cutoff)
        {
            float te = 1.0f / frequency;
            float tau = 1.0f / (2 * Mathf.PI * cutoff);
            return 1.0f / (1.0f + tau / te);
        }

        public virtual T Filter(T value)
        {
            return value;
        }
    }

    public class OneEuroFilterFloat : OneEuroFilterBase<float>
    {
        private float prevRawValue; // 이전 원본 값
        private float prevFilteredValue;
        private float prevFilteredDeriv;
        
        public OneEuroFilterFloat(float frequency, float minCutoff = 1.0f, float beta = 0.0f, float dCutoff = 1.0f)
        {
            this.frequency = frequency;
            this.minCutoff = minCutoff;
            this.beta = beta;
            this.dCutoff = dCutoff;
        }
        
        public override float Filter(float value)
        {
            if (!initialized)
            {
                prevRawValue = value;
                prevFilteredValue = value;
                prevFilteredDeriv = 0;
                initialized = true;
                return value;
            }

            // 1. 속도(도함수) 추정
            float dx = (value - prevRawValue) * frequency;
            float alphaD = Alpha(dCutoff);
            float dxHat = Mathf.Lerp(dx, prevFilteredDeriv, alphaD);

            // 2. cutoff 주파수 동적 계산
            float cutoff = minCutoff + beta * dxHat;

            // 3. 값 필터링
            float alpha = Alpha(cutoff);
            float xHat = Mathf.Lerp(value, prevFilteredValue, alpha);

            // 상태 갱신
            prevRawValue = value;
            prevFilteredValue = xHat;
            prevFilteredDeriv = dxHat;

            return xHat;
        }
    }

    public class OneEuroFilterPosition : OneEuroFilterBase<Vector3>
    {
        private Vector3 prevRawValue; // 이전 원본 값
        private Vector3 prevFilteredValue;
        private Vector3 prevFilteredDeriv;

        public OneEuroFilterPosition(float frequency, float minCutoff = 1.0f, float beta = 0.0f, float dCutoff = 1.0f)
        {
            this.frequency = frequency;
            this.minCutoff = minCutoff;
            this.beta = beta;
            this.dCutoff = dCutoff;
        }

        // private Vector3 ExponentialSmoothing(Vector3 x, Vector3 xPrev, float alpha)
        // {
        //     return alpha * x + (1 - alpha) * xPrev;// 유니티 Vector3.Lerp랑 로직이 동일
        // }

        public override Vector3 Filter(Vector3 value)
        {
            if (!initialized)
            {
                prevRawValue = value;
                prevFilteredValue = value;
                prevFilteredDeriv = Vector3.zero;
                initialized = true;
                return value;
            }

            // 1. 속도(도함수) 추정
            Vector3 dx = (value - prevRawValue) * frequency;
            float alphaD = Alpha(dCutoff);
            Vector3 dxHat = Vector3.Lerp(dx, prevFilteredDeriv, alphaD);
            // Vector3 dxHat = ExponentialSmoothing(dx, prevFilteredDeriv, alphaD); // 유니티 Vector3.Lerp랑 로직이 동일

            // 2. cutoff 주파수 동적 계산
            float cutoff = minCutoff + beta * dxHat.magnitude;

            // 3. 값 필터링
            float alpha = Alpha(cutoff);
            Vector3 xHat = Vector3.Lerp(value, prevFilteredValue, alpha);
            // Vector3 xHat = ExponentialSmoothing(value, prevFilteredValue, alpha); // 유니티 Vector3.Lerp랑 로직이 동일

            // 상태 갱신
            prevRawValue = value;
            prevFilteredValue = xHat;
            prevFilteredDeriv = dxHat;

            return xHat;
        }
    }

    public class OneEuroFilterQuaternion : OneEuroFilterBase<Quaternion>
    {
        private Quaternion prevRawValue;
        private Quaternion prevFilteredValue;
        private Vector3 prevFilteredDeriv;

        public OneEuroFilterQuaternion(float frequency, float minCutoff = 1.0f, float beta = 0.0f, float dCutoff = 1.0f)
        {
            this.frequency = frequency;
            this.minCutoff = minCutoff;
            this.beta = beta;
            this.dCutoff = dCutoff;
        }

        // Quaternion 차이를 Vector3 각속도로 변환
        private Vector3 QuaternionDerivative(Quaternion current, Quaternion previous)
        {
            Quaternion delta = current * Quaternion.Inverse(previous);
            delta.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f;
            return axis * (angle * Mathf.Deg2Rad) * frequency;
        }

        public override Quaternion Filter(Quaternion value)
        {
            if (!initialized)
            {
                prevRawValue = value;
                prevFilteredValue = value;
                prevFilteredDeriv = Vector3.zero;
                initialized = true;
                return value;
            }

            // 1. 회전 속도 추정
            Vector3 d = QuaternionDerivative(value, prevRawValue);
            float alphaD = Alpha(dCutoff);
            Vector3 dHat = Vector3.Lerp(prevFilteredDeriv, d, alphaD);

            // 2. cutoff 계산
            float cutoff = minCutoff + beta * dHat.magnitude;

            // 3. 회전 보정
            float alpha = Alpha(cutoff);
            Quaternion xHat = Quaternion.Slerp(prevFilteredValue, value, alpha);// 쿼터니언은 유클리드 공간이 아닌 구면 공간이기 때문에 구형 보간 사용

            // 상태 갱신
            prevRawValue = value;
            prevFilteredValue = xHat;
            prevFilteredDeriv = dHat;

            return xHat;
        }
    }

    [System.Serializable]
    public class OneEuroFilterOptions
    {
        [Tooltip("샘플링 주파수 (Hz). 필터링 대상의 업데이트 속도에 맞춘다.")] public float frequency = 90f;
        [Tooltip("최소 Cutoff 주파수. 부드러움 정도를 조절하며 값을 높힐수록 부드러워지지만 지연 시간이 길어진다.")] public float minCutoff = 0.5f;
        [Tooltip("속도에 따른 Cutoff 보정 계수. 움직임 속도에 따른 민감도를 조절하며 값을 올릴 수록 빠른 움직임에 더 즉각 반응을 한다.")] public float beta = 1f;
        [Tooltip("속도 필터용 Cutoff")] public float dCutoff = 2.0f;
    }

    [System.Serializable]
    public class OneEuroFilterTransform
    {
        [System.Serializable]
        public class SimpleTransform
        {
            public Vector3 position = Vector3.zero;
            public Quaternion rotation = Quaternion.identity;
            public Vector3 eulerAngles => rotation.eulerAngles;
            public Vector3 scale = Vector3.one;

            public SimpleTransform(Vector3 position, Quaternion rotation)
            {
                this.position = position;
                this.rotation = rotation;
            }
        }
        
        public OneEuroFilterTransform()
        {
            posOptions.minCutoff = 2.0f;
            posOptions.beta = 1.0f;
            rotOptions.minCutoff = 1.0f;
            rotOptions.beta = 2.0f;
        }
        
        public OneEuroFilterTransform(Transform[] targets)
        {
            Initialize(targets);
        }

        public class OneEuroFilterDictionary
        {
            public OneEuroFilterPosition pos;
            public OneEuroFilterQuaternion rot;

            public OneEuroFilterDictionary(OneEuroFilterPosition pos, OneEuroFilterQuaternion rot)
            {
                this.pos = pos;
                this.rot = rot;
            }

            public SimpleTransform Filter(Vector3 pos, Quaternion rot)
            {
                return new SimpleTransform(this.pos.Filter(pos), this.rot.Filter(rot));
            }
            
            public void Filter(Transform from, Transform to)
            {
                to.position = pos.Filter(from.position);
                to.rotation = rot.Filter(from.rotation);
            }
        }

        private Dictionary<string, OneEuroFilterDictionary> filters = new();
        public Dictionary<string, OneEuroFilterDictionary> Filters => filters;

        [SerializeField] private OneEuroFilterOptions posOptions = new();
        [SerializeField] private OneEuroFilterOptions rotOptions = new();

        public OneEuroFilterOptions PosOptions
        {
            get => posOptions;
            set
            {
                posOptions = value;

                if (filters.Count > 0)
                {
                    foreach (var filter in filters)
                    {
                        if (filter.Value.pos == null) continue;

                        filter.Value.pos.frequency = posOptions.frequency;
                        filter.Value.pos.minCutoff = posOptions.minCutoff;
                        filter.Value.pos.beta = posOptions.beta;
                        filter.Value.pos.dCutoff = posOptions.dCutoff;
                    }
                }
            }
        }

        public OneEuroFilterOptions RotOptions
        {
            get => rotOptions;
            set
            {
                rotOptions = value;

                if (filters.Count > 0)
                {
                    foreach (var filter in filters)
                    {
                        filter.Value.rot.frequency = rotOptions.frequency;
                        filter.Value.rot.minCutoff = rotOptions.minCutoff;
                        filter.Value.rot.beta = rotOptions.beta;
                        filter.Value.rot.dCutoff = rotOptions.dCutoff;
                    }
                }
            }
        }

        public void Initialize(Transform[] targets = null)
        {
            if (filters == null) filters = new();
            else filters.Clear();

            if (targets == null) return;
            
            foreach (var target in targets)
            {
                AddFilter(target);
            }
        }

        public void AddFilter(Transform target)
        {
            filters.TryAdd(target.name,
                new OneEuroFilterDictionary(new OneEuroFilterPosition(frequency: posOptions.frequency, minCutoff: posOptions.minCutoff, beta: posOptions.beta, dCutoff: posOptions.dCutoff),
                    new OneEuroFilterQuaternion(frequency: rotOptions.frequency, minCutoff: rotOptions.minCutoff, beta: rotOptions.beta, dCutoff: rotOptions.dCutoff)));
        }

        public bool HasFilter(Transform target)
        {
            return filters.ContainsKey(target.name);
        }

        public void UpdateProperties()
        {
            if (filters == null || filters.Count == 0) return;

            foreach (var filter in filters)
            {
                filter.Value.pos.frequency = posOptions.frequency;
                filter.Value.pos.minCutoff = posOptions.minCutoff;
                filter.Value.pos.beta = posOptions.beta;
                filter.Value.pos.dCutoff = posOptions.dCutoff;

                filter.Value.rot.frequency = rotOptions.frequency;
                filter.Value.rot.minCutoff = rotOptions.minCutoff;
                filter.Value.rot.beta = rotOptions.beta;
                filter.Value.rot.dCutoff = rotOptions.dCutoff;
            }
        }
    }
}
