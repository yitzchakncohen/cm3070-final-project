using System;
using System.Collections.Generic;
using System.Linq;
using ModularVehicleSimulator.Vehicle;
using UnityEngine;

namespace ModularVehicleSimulator.Physics
{
    public static class VehiclePhysics
    {
        public const float RPM_TO_METERS_PER_SECOND = (2f * Mathf.PI) / 60f;
        public const float METERS_PER_SECOND_TO_KM_PER_HOUR = 3.6f;
        public const int SPHERE_SEGMENTS = 24;
        // Shared buffers to avoid GC allocations during runtime
        private static readonly List<Vector2> boundingPointsBuffer = new List<Vector2>();
        private static readonly List<Vector2> convexHullBuffer = new List<Vector2>();
        private static readonly List<Vector3> meshVerticesBuffer = new List<Vector3>();

        public static float GetVehicleSpeed(Wheel[] wheels, float radius)
        {
            Wheel[] nonMotorizedWheels = wheels.Where(wheel => !wheel.IsMotorized).ToArray();
            float rpm = 0 ;
            if(nonMotorizedWheels.Length > 0)
            {
                rpm = Mathf.Abs(nonMotorizedWheels.Average(wheel => wheel.GetEffectiveRPM()));                
            }
            else
            {
                rpm = Mathf.Abs(wheels.Average(wheel => wheel.GetEffectiveRPM()));
            }
            float forwardSpeed = rpm * radius * RPM_TO_METERS_PER_SECOND;
            return forwardSpeed;
        }
        #region Tires
        public static float GetNominalTireDeflection(float mass, float numberOfWheels, float stiffness)
        {
            float vehicleWeight =  mass * Mathf.Abs(UnityEngine.Physics.gravity.y);
            return  vehicleWeight / numberOfWheels / stiffness;
        }

        public static float GetTireDeflection(float verticalForce, float stiffness)
        {
            // Linear approximation of tire deformation
            return verticalForce / stiffness;
        }

        public static float GetSidewaysFriction(WheelFrictionCurve curve, float slip, ref WheelHit hit)
        {
            float sidewaysFrictionCoefficient = EvaluateFrictionCurve(curve, slip);
            return sidewaysFrictionCoefficient * hit.force * Mathf.Sign(hit.sidewaysSlip);
        }

        public static float GetForwardFriction(WheelFrictionCurve curve, float slip, ref WheelHit hit)
        {
            float forwardFrictionCoefficient = EvaluateFrictionCurve(curve, slip);
            return forwardFrictionCoefficient * hit.force * Mathf.Sign(hit.forwardSlip);
        }

        private static float EvaluateFrictionCurve(WheelFrictionCurve curve, float slip)
        {
            float absSlip = Mathf.Abs(slip);

            // 1. First spline section: from 0 to Extremum
            if (absSlip < curve.extremumSlip)
            {
                float t = absSlip / curve.extremumSlip;
                // Cubic spline interpolation with zero tangent at origin and extremum
                return Mathf.SmoothStep(0f, curve.extremumValue, t);
            }
            // 2. Second spline section: from Extremum to Asymptote
            else if (absSlip < curve.asymptoteSlip)
            {
                float range = curve.asymptoteSlip - curve.extremumSlip;
                float t = (absSlip - curve.extremumSlip) / range;
                // Cubic spline interpolation between Extremum Value and Asymptote Value
                return Mathf.SmoothStep(curve.extremumValue, curve.asymptoteValue, t);
            }
            // 3. Beyond Asymptote: returns the constant Asymptote Value
            else
            {
                return curve.asymptoteValue;
            }
        }
        #endregion

        #region Steering
        public static float GetTargetSteeringAngle(float steeringInput, float currentSpeed, float highSpeedThreshold, float maxSteeringAngleAtRest, float maxSteeringAngleAtHighSpeed)
        {
            float speedFactor = Mathf.InverseLerp(0f, highSpeedThreshold, currentSpeed);
            float allowableMaxSteer = Mathf.Lerp(maxSteeringAngleAtRest, maxSteeringAngleAtHighSpeed, speedFactor);
            float targetSteeringAngle = steeringInput * allowableMaxSteer;
            return targetSteeringAngle;
        }

        // Ackerman's Geometric Model //
        public static void GetAckermannSteeringAngles(float wheelBase, float track, float targetAngle, out float rightSteeringAngle, out float leftSteeringAngle)
        {
            // Handle small values
            if(Mathf.Abs(targetAngle) < 0.1f)
            {
                rightSteeringAngle = leftSteeringAngle = targetAngle;
                return;
            }

            float tanOfTargetAngle = Mathf.Tan(Mathf.Abs(targetAngle) * Mathf.Deg2Rad);
            if(targetAngle > 0) // Turning Right
            {
                rightSteeringAngle = Mathf.Rad2Deg * Mathf.Atan(wheelBase / ((wheelBase / tanOfTargetAngle) + (track/2))) * Mathf.Sign(targetAngle);
                leftSteeringAngle = Mathf.Rad2Deg * Mathf.Atan(wheelBase / ((wheelBase / tanOfTargetAngle) - (track/2))) * Mathf.Sign(targetAngle);
            }
            else // Turning Left
            {
                rightSteeringAngle = Mathf.Rad2Deg * Mathf.Atan(wheelBase / ((wheelBase / tanOfTargetAngle) - (track/2))) * Mathf.Sign(targetAngle);
                leftSteeringAngle = Mathf.Rad2Deg * Mathf.Atan(wheelBase / ((wheelBase / tanOfTargetAngle) + (track/2))) * Mathf.Sign(targetAngle);
            }
        }

        public static float GetTurningRadius(float wheelBase, float steeringAngle)
        {
            return wheelBase / Mathf.Tan(steeringAngle * Mathf.Deg2Rad);
        }

        public static float ABSStepFunction(float brakeTorque, float oscillationSpeed)
        {
            float angularFrequency = oscillationSpeed * 2f * Mathf.PI;
            brakeTorque = Mathf.Sin(Time.fixedTime * angularFrequency) > 0f ? brakeTorque : 0f;
            return brakeTorque;
        }
        #endregion

        #region  Air Resistance
        public static void GetCollidersCrossSectionPolygon(Collider[] colliders, Vector3 forwardDirection, Vector3 upDirection, Vector3 center, List<Vector2> crossSectionBuffer)
        {
            forwardDirection.Normalize();
            (Vector3 u, Vector3 v) = Get2DBasisPlane(forwardDirection, upDirection);
            UpdateBoundingPoints(colliders, u, v, center);
            UpdateConvexHull();
            crossSectionBuffer.Clear();
            crossSectionBuffer.AddRange(convexHullBuffer);
        }

        public static float GetAreaOfConvexHull(List<Vector2> convexHull)
        {
            if(convexHull.Count < 3) return 0f;

            float area = 0f;
            int j = convexHull.Count - 1;
            
            // Gauss's Area Formula
            for (int i = 0; i < convexHull.Count; i++)
            {
                area += (convexHull[j].x + convexHull[i].x) * (convexHull[j].y - convexHull[i].y);
                j = i;
            }
            return Mathf.Abs(area * 0.5f);
        }

        private static void UpdateBoundingPoints(Collider[] colliders, Vector3 u, Vector3 v, Vector3 center)
        {
            boundingPointsBuffer.Clear();

            foreach (Collider collider in colliders)
            {
                if (!collider.enabled || collider.isTrigger) continue;

                switch (collider)
                {
                    case BoxCollider boxCollider:
                        AddBoxVertices(boxCollider, u, v, center);
                        break;
                    case SphereCollider sphereCollider:
                        AddSphereVertices(sphereCollider, u, v, center);
                        break;
                    case CapsuleCollider capsuleCollider:
                        AddCapsuleVertices(capsuleCollider, u, v, center);
                        break;
                    case MeshCollider meshCollider:
                        AddMeshVertices(meshCollider, u, v, center);
                        break;
                }
            }
        }

        // Monotone Chain Algorithm
        // https://www.geeksforgeeks.org/dsa/convex-hull-monotone-chain-algorithm/
        private static void UpdateConvexHull()
        {
            convexHullBuffer.Clear();
            if(boundingPointsBuffer.Count < 3) return;

            // Sort from left to right
            boundingPointsBuffer.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));

            // Lower hull
            foreach (Vector2 point in boundingPointsBuffer)
            {
                // Check Orientation
                while(convexHullBuffer.Count >= 2 && 
                        GetRelativeCrossProduct2D(convexHullBuffer[convexHullBuffer.Count -2], convexHullBuffer[convexHullBuffer.Count -1], point) <=0)
                {
                    convexHullBuffer.RemoveAt(convexHullBuffer.Count - 1);
                }
                convexHullBuffer.Add(point);
            }

            // Upper hull
            int lowerHullBounds = convexHullBuffer.Count + 1;
            for (int i = boundingPointsBuffer.Count - 2; i >= 0; i--)
            {
                while(convexHullBuffer.Count >= lowerHullBounds && 
                    GetRelativeCrossProduct2D(convexHullBuffer[convexHullBuffer.Count -2], convexHullBuffer[convexHullBuffer.Count -1], boundingPointsBuffer[i]) <= 0)
                {
                    convexHullBuffer.RemoveAt(convexHullBuffer.Count - 1);                    
                }

                convexHullBuffer.Add(boundingPointsBuffer[i]);                    
            }

            // Remove duplicate point
            convexHullBuffer.RemoveAt(convexHullBuffer.Count - 1);
        }

        public static (Vector3, Vector3) Get2DBasisPlane(Vector3 forwardDirection, Vector3 upDirection)
        {
            Vector3 referenceVector = forwardDirection.y > 0.99f  ? Vector3.forward : upDirection;
            Vector3 u = Vector3.Cross(referenceVector, forwardDirection).normalized;
            Vector3 v = Vector3.Cross(forwardDirection, u).normalized;
            return (u, v);
        }

        private static float GetRelativeCrossProduct2D(Vector2 origin, Vector2 a, Vector2 b)
        {
            return (a.x - origin.x) * (b.y - origin.y) - (a.y - origin.y) * (b.x - origin.x);
        }

        private static void AddBoxVertices(BoxCollider boxCollider, Vector3 u, Vector3 v, Vector3 center)
        {
            Vector3 boxColliderHalfSize = boxCollider.size * 0.5f;
            Vector3 boxColliderCenter = boxCollider.center;
            // No GC allocation by using loops to add corner points.
            for (int x = -1; x <= 1; x += 2) 
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 localCorner = boxColliderCenter + new Vector3(x * boxColliderHalfSize.x, y * boxColliderHalfSize.y, z * boxColliderHalfSize.z);
                        Vector3 worldPoint = boxCollider.transform.TransformPoint(localCorner);
                        boundingPointsBuffer.Add(ProjectToPlane(worldPoint, u, v, center));
                    }
                }
            }
        }

        private static void AddSphereVertices(SphereCollider sphereCollider, Vector3 u, Vector3 v, Vector3 center)
        {
            Vector3 worldCenter = sphereCollider.transform.TransformPoint(sphereCollider.center);
            Vector3 lossyScale = sphereCollider.transform.lossyScale;
            
            float maxScale = Mathf.Max(Mathf.Abs(lossyScale.x), Mathf.Max(Mathf.Abs(lossyScale.y), Mathf.Abs(lossyScale.z)));
            float worldRadius = sphereCollider.radius * maxScale;
            float step = (Mathf.PI * 2f) / SPHERE_SEGMENTS;

            for (int i = 0; i < SPHERE_SEGMENTS; i++)
            {
                float angle = i * step;
                Vector3 worldPoint = worldCenter + (u * Mathf.Cos(angle) + v * Mathf.Sin(angle)) * worldRadius;
                boundingPointsBuffer.Add(ProjectToPlane(worldPoint, u, v, center));
            }

        }

        private static void AddCapsuleVertices(CapsuleCollider capsuleCollider, Vector3 u, Vector3 v, Vector3 center)
        {
            Vector3 worldCenter = capsuleCollider.transform.TransformPoint(capsuleCollider.center);
            Vector3 lossyScale = capsuleCollider.transform.lossyScale;
            Vector3 capsuleAxis = CapsuleIntToDirection(capsuleCollider.transform, capsuleCollider.direction);
            
            float maxScale = Mathf.Max(Mathf.Abs(lossyScale.x), Mathf.Max(Mathf.Abs(lossyScale.y), Mathf.Abs(lossyScale.z)));
            float dirScale = capsuleCollider.direction == 0 ? lossyScale.x : (capsuleCollider.direction == 1 ? lossyScale.y : lossyScale.z);
            float worldRadius = capsuleCollider.radius * maxScale;
            float worldHeight = Mathf.Max(capsuleCollider.height * dirScale, worldRadius * 2f);
            float cylinderHalfHeight = (worldHeight * 0.5f) - worldRadius;
            Vector3 topCapsuleCenter = worldCenter + capsuleAxis * cylinderHalfHeight;
            Vector3 bottomCapsuleCenter = worldCenter - capsuleAxis * cylinderHalfHeight;
            
            float step = (Mathf.PI * 2f) / SPHERE_SEGMENTS;

            for (int i = 0; i < SPHERE_SEGMENTS; i++)
            {
                float angle = i * step;
                Vector3 offset = (u * Mathf.Cos(angle) + v * Mathf.Sin(angle)) * worldRadius;
                Vector3 point1 = topCapsuleCenter + offset;
                boundingPointsBuffer.Add(ProjectToPlane(point1, u, v, center));
                Vector3 point2 = bottomCapsuleCenter + offset;
                boundingPointsBuffer.Add(ProjectToPlane(point2, u, v, center));
            }
        }

        private static void AddMeshVertices(MeshCollider meshCollider, Vector3 u, Vector3 v, Vector3 center)
        {
            Mesh mesh = meshCollider.sharedMesh;
            if(mesh == null) return;
            meshVerticesBuffer.Clear();
            mesh.GetVertices(meshVerticesBuffer);
            int count = meshVerticesBuffer.Count;

            for(int i = 0; i < count; i++)
            {
                Vector3 point = meshCollider.transform.TransformPoint(meshVerticesBuffer[i]);
                boundingPointsBuffer.Add(ProjectToPlane(point, u, v, center));
            }
        }

        private static Vector2 ProjectToPlane(Vector3 point, Vector3 u, Vector3 v, Vector3 center)
        {
            Vector3 relativePoint = point - center;
            return new Vector2(Vector3.Dot(relativePoint, u), Vector3.Dot(relativePoint, v));
        }

        private static Vector3 CapsuleIntToDirection(Transform transform, int integer)
        {
            switch (integer)
            {
                case 0:
                    return transform.TransformDirection(Vector3.right).normalized;
                case 1:
                    return transform.TransformDirection(Vector3.up).normalized;
                default:
                    return transform.TransformDirection(Vector3.forward).normalized;
            }
        }
        #endregion

        public static void SetChildrenLayerRecursive(Transform parent, int layer)
        {
            foreach (Transform child in parent)
            {
                child.gameObject.layer = layer;
                if (child.childCount == 0) return;

                SetChildrenLayerRecursive(child, layer);
            }
        }
    }
}
