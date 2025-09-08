using System;
using UnityEngine;

namespace RopeSystem
{
    /// <summary>
    /// Rope Component
    /// Forked from GoGoGaGa with some optimization on calculation
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(LineRenderer))]
    public class Rope : MonoBehaviour
    {
        [Header("Rope Transforms")] [Tooltip("绳索起点")] [SerializeField]
        private Transform startPoint;

        [Tooltip("中间点,决定绳索的默认悬垂状态")] [SerializeField]
        private Transform midPoint;

        [Tooltip("绳索终点")] [SerializeField] private Transform endPoint;

        [Header("Rope Settings")] [Tooltip("模拟采样点数量,采样点越多,绳索越平滑")] [Range(2f, 100f)]
        public int linePoints = 10;

        [Tooltip("绳索刚度")] public float stiffness = 350f;

        [Tooltip("模拟阻尼")] public float damping = 15f;

        [Tooltip("绳索长度")] public float ropeLength = 15f;

        [Tooltip("绳索宽度")] public float ropeWidth = 0.1f;

        [Header("Rational Bezier Weight Control")] [Tooltip("中间点在有理贝塞尔采样时的权重,>1增强,<1减弱")] [Range(1f, 15f)]
        public float midPointWeight = 1f;

        [Header("Midpoint Position")] [Tooltip("中间点沿着起点和终点连线，所处的位置")] [Range(0.25f, 0.75f)]
        public float midPointPosition = 0.5f;

        private Vector3 currentValue;
        private Vector3 currentVelocity;
        private Vector3 targetValue;
        private LineRenderer lineRenderer;
        private bool isFirstFrame = true;
        private Vector3 prevStartPointPosition;
        private Vector3 prevEndPointPosition;
        private float prevMidPointPosition;
        private float prevMidPointWeight;
        private float prevLineQuality;
        private float prevRopeWidth;
        private float prevStiffness;
        private float prevDampness;
        private float prevRopeLength;

        public event Action OnPointsChanged;

        public Transform StartPoint => startPoint;
        public Transform MidPoint => midPoint;
        public Transform EndPoint => endPoint;

        public Vector3 LeftItem { get; set; }

        public bool IsPrefab => gameObject.scene.rootCount == 0;

        private void Start()
        {
            Initialize();
            if (!ArePointsValid())
                return;
            currentValue = GetMidPoint();
            targetValue = currentValue;
            currentVelocity = Vector3.zero;
            SetSplinePoint();
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
                return;
            Initialize();
            if (ArePointsValid())
            {
                RecalculateRope();
                SimulatePhysics();
            }
            else
            {
                lineRenderer.positionCount = 0;
            }
        }

        private void Initialize()
        {
            if (!TryGetComponent(out LineRenderer l))
                return;

            l.startWidth = ropeWidth;
            l.endWidth = ropeWidth;
            lineRenderer = l;
        }

        private void Update()
        {
            if (IsPrefab || !ArePointsValid())
                return;
            SetSplinePoint();
            if (!Application.isPlaying && (IsPointsMoved() || IsRopeSettingsChanged()))
            {
                SimulatePhysics();
                NotifyPointsChanged();
            }

            prevStartPointPosition = startPoint.position;
            prevEndPointPosition = endPoint.position;
            prevMidPointPosition = midPointPosition;
            prevMidPointWeight = midPointWeight;
            prevLineQuality = linePoints;
            prevRopeWidth = ropeWidth;
            prevStiffness = stiffness;
            prevDampness = damping;
            prevRopeLength = ropeLength;
        }

        private bool ArePointsValid()
        {
            return startPoint && endPoint;
        }

        private void SetSplinePoint()
        {
            if (lineRenderer.positionCount != linePoints + 1)
                lineRenderer.positionCount = linePoints + 1;
            targetValue = GetMidPoint();
            var value = currentValue;
            if (midPoint != null)
                midPoint.position = GetRationalBezierPoint(startPoint.position, value,
                    endPoint.position, midPointPosition, 1f, midPointWeight, 1f);
            for (var index = 0; index < linePoints; ++index)
            {
                Vector3 rationalBezierPoint = GetRationalBezierPoint(startPoint.position, value,
                    endPoint.position, index / (float)linePoints, 1f, midPointWeight, 1f);
                lineRenderer.SetPosition(index, rationalBezierPoint);
            }

            lineRenderer.SetPosition(linePoints, endPoint.position);
        }

        private static float CalculateYFactorAdjustment(float weight)
        {
            return (float)(1.0 + Mathf.Lerp(0.493f, 0.323f, Mathf.InverseLerp(1f, 15f, weight)) *
                (double)Mathf.Log(weight));
        }

        private Vector3 GetMidPoint()
        {
            var position1 = startPoint.position;
            var position2 = endPoint.position;
            var point = Vector3.Lerp(position1, position2, midPointPosition);
            var num = (ropeLength - Mathf.Min(Vector3.Distance(position1, position2), ropeLength)) /
                      CalculateYFactorAdjustment(midPointWeight);
            point.y -= num;
            return point;
        }

        private Vector3 GetRationalBezierPoint(
            Vector3 p0,
            Vector3 p1,
            Vector3 p2,
            float t,
            float w0,
            float w1,
            float w2)
        {
            var vector3_1 = w0 * p0;
            var vector3_2 = w1 * p1;
            var vector3_3 = w2 * p2;
            var num1 = (float)(w0 * (double)Mathf.Pow(1f - t, 2f) +
                               2.0 * w1 * (1.0 - t) * t +
                               w2 * (double)Mathf.Pow(t, 2f));
            var num2 = (double)Mathf.Pow(1f - t, 2f);
            return (vector3_1 * (float)num2 + vector3_2 * (2f * (1f - t) * t) + vector3_3 * Mathf.Pow(t, 2f)) / num1;
        }

        public Vector3 GetPointAt(float t)
        {
            if (ArePointsValid())
                return GetRationalBezierPoint(startPoint.position, currentValue, endPoint.position,
                    t, 1f, midPointWeight, 1f);
            Debug.LogError("StartPoint和EndPoint不可以为空", gameObject);
            return Vector3.zero;
        }

        private void FixedUpdate()
        {
            if (IsPrefab || !ArePointsValid())
                return;
            if (!isFirstFrame)
                SimulatePhysics();
            isFirstFrame = false;
        }

        private void SimulatePhysics()
        {
            currentVelocity =
                currentVelocity *
                Mathf.Max(0.0f, (float)(1.0 - damping * (double)Time.fixedDeltaTime)) +
                (targetValue - currentValue) * (stiffness * Time.fixedDeltaTime) +
                LeftItem;
            currentValue += currentVelocity * Time.fixedDeltaTime;
            if (Vector3.Distance(currentValue, targetValue) >= 0.0099999997764825821 ||
                currentVelocity.magnitude >= 0.0099999997764825821)
                return;
            currentValue = targetValue;
            currentVelocity = Vector3.zero;
        }

        private void OnDrawGizmos()
        {
            if (!ArePointsValid())
                return;
            GetMidPoint();
        }

        public void SetStartPoint(Transform newStartPoint, bool instantAssign = false)
        {
            startPoint = newStartPoint;
            prevStartPointPosition = startPoint == null
                ? Vector3.zero
                : startPoint.position;
            if (instantAssign || newStartPoint == null)
                RecalculateRope();
            NotifyPointsChanged();
        }

        public void SetMidPoint(Transform newMidPoint, bool instantAssign = false)
        {
            midPoint = newMidPoint;
            prevMidPointPosition = midPoint == null
                ? 0.5f
                : midPointPosition;
            if (instantAssign || newMidPoint == null)
                RecalculateRope();
            NotifyPointsChanged();
        }

        public void SetEndPoint(Transform newEndPoint, bool instantAssign = false)
        {
            endPoint = newEndPoint;
            prevEndPointPosition = endPoint == null
                ? Vector3.zero
                : endPoint.position;
            if (instantAssign || newEndPoint == null)
                RecalculateRope();
            NotifyPointsChanged();
        }

        public void RecalculateRope()
        {
            if (!ArePointsValid())
            {
                lineRenderer.positionCount = 0;
            }
            else
            {
                currentValue = GetMidPoint();
                targetValue = currentValue;
                currentVelocity = Vector3.zero;
                SetSplinePoint();
            }
        }

        private void NotifyPointsChanged()
        {
            var onPointsChanged = OnPointsChanged;
            onPointsChanged?.Invoke();
        }

        private bool IsPointsMoved()
        {
            return startPoint.position != prevStartPointPosition |
                   endPoint.position != prevEndPointPosition;
        }

        private bool IsRopeSettingsChanged()
        {
            var num1 = !Mathf.Approximately(linePoints, prevLineQuality) ? 1 : 0;
            var flag1 = !Mathf.Approximately(ropeWidth, prevRopeWidth);
            var flag2 = !Mathf.Approximately(stiffness, prevStiffness);
            var flag3 = !Mathf.Approximately(damping, prevDampness);
            var flag4 = !Mathf.Approximately(ropeLength, prevRopeLength);
            var flag5 = !Mathf.Approximately(midPointPosition, prevMidPointPosition);
            var flag6 = !Mathf.Approximately(midPointWeight, prevMidPointWeight);
            var num2 = flag1 ? 1 : 0;
            return (num1 | num2 | (flag2 ? 1 : 0) | (flag3 ? 1 : 0) | (flag4 ? 1 : 0) | (flag5 ? 1 : 0) |
                    (flag6 ? 1 : 0)) != 0;
        }
    }
}