using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModularVehicleSimulator.Physics
{
    public static class VehiclePhysics
    {
        public const int SPHERE_SEGMENTS = 24;
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
            if(targetAngle > 0)
            {
                rightSteeringAngle = Mathf.Rad2Deg * Mathf.Atan(wheelBase / ((wheelBase / tanOfTargetAngle) + (track/2))) * Mathf.Sign(targetAngle);
                leftSteeringAngle = Mathf.Rad2Deg * Mathf.Atan(wheelBase / ((wheelBase / tanOfTargetAngle) - (track/2))) * Mathf.Sign(targetAngle);
            }
            else
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
            brakeTorque = Mathf.Sin(Time.deltaTime * oscillationSpeed) > 0f ? brakeTorque : 0f;
            return brakeTorque;
        }
        #endregion

        #region  Air Resistance
        public static List<Vector2> GetCollidersCrossSectionPolygon(Collider[] colliders, Vector3 direction)
        {
            direction = direction.normalized;
            (Vector3 u, Vector3 v) = Get2DBasisPlane(direction);
            List<Vector2> boundingPoints = GetBoundingPoints(colliders, u, v);
            List<Vector2> convexHull = GetConvexHull(boundingPoints);
            return convexHull;
        }

        public static float GetAreaOfConvexHull(List<Vector2> convexHull)
        {
            float area = 0f;
            int j = convexHull.Count - 1;
            for (int i = 0; i < convexHull.Count; i++)
            {
                area += (convexHull[j].x + convexHull[i].x) * (convexHull[j].y - convexHull[i].y);
                j = i;
            }
            return Mathf.Abs(area * 0.5f);
        }

        private static List<Vector2> GetBoundingPoints(Collider[] colliders, Vector3 u, Vector3 v)
        {
            List<Vector2> boundingPoints = new List<Vector2>();

            foreach (Collider collider in colliders)
            {
                if (!collider.enabled || collider.isTrigger) continue;

                switch (collider)
                {
                    case BoxCollider boxCollider:
                        Vector3[] boxVertices = GetBoxVertices(boxCollider);
                        boundingPoints.AddRange(ProjectPointsToPlane(boxVertices, u, v));
                        break;
                    case SphereCollider sphereCollider:
                        List<Vector3> sphereVertices = GetSphereVertices(sphereCollider, u, v);
                        boundingPoints.AddRange(ProjectPointsToPlane(sphereVertices, u, v));
                        break;
                    case CapsuleCollider capsuleCollider:
                        List<Vector3> capsuleVertices = GetCapsuleVertices(capsuleCollider, u, v);
                        boundingPoints.AddRange(ProjectPointsToPlane(capsuleVertices, u, v));
                        break;
                    case MeshCollider meshCollider:
                        Vector3[] meshVertices = GetMeshVertices(meshCollider);
                        boundingPoints.AddRange(ProjectPointsToPlane(meshVertices, u, v));
                        break;
                }
            }

            return boundingPoints;
        }

        // Monotone Chain Algorithm
        // https://www.geeksforgeeks.org/dsa/convex-hull-monotone-chain-algorithm/
        private static List<Vector2> GetConvexHull(List<Vector2> boundingPoints)
        {
            if(boundingPoints.Count < 3) return boundingPoints;

            List<Vector2> convexHull = new List<Vector2>();
            // Sort from left to right
            boundingPoints.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));

            // Lower hull
            foreach (Vector2 point in boundingPoints)
            {
                // Check Orientation
                while(convexHull.Count >= 2 && 
                        GetRelativeCrossProduct2D(convexHull[convexHull.Count -2], convexHull[convexHull.Count -1], point) <=0)
                {
                    convexHull.RemoveAt(convexHull.Count - 1);
                }
                convexHull.Add(point);
            }

            // Upper hull
            int lowerHullBounds = convexHull.Count + 1;
            for (int i = boundingPoints.Count - 2; i >= 0; i--)
            {
                while(convexHull.Count >= lowerHullBounds && 
                    GetRelativeCrossProduct2D(convexHull[convexHull.Count -2], convexHull[convexHull.Count -1], boundingPoints[i]) <= 0)
                {
                    convexHull.RemoveAt(convexHull.Count - 1);                    
                }

                convexHull.Add(boundingPoints[i]);                    
            }

            // Remove duplicate point
            convexHull.RemoveAt(convexHull.Count - 1);

            return convexHull;        
        }

        private static (Vector3, Vector3) Get2DBasisPlane(Vector3 direction)
        {
            Vector3 referenceVector = GetPlaneReferenceVector(direction);
            Vector3 u = Vector3.Cross(direction, referenceVector);
            Vector3 v = Vector3.Cross(direction, u);
            return (u, v);
        }

        private static float GetRelativeCrossProduct2D(Vector2 origin, Vector2 a, Vector2 b)
        {
            return (a.x - origin.x) * (b.y - origin.y) - (a.y - origin.y) * (b.x - origin.x);
        }

        private static Vector3 GetPlaneReferenceVector(Vector3 direction)
        {
            if(direction.y > 0.99f)
            {
                return Vector3.forward;
            }
            return Vector3.up;
        }

        private static Vector3[] GetBoxVertices(BoxCollider boxCollider)
        {
            Vector3[] localCorners = new Vector3[8]
            {
                boxCollider.center + new Vector3(-boxCollider.size.x, -boxCollider.size.y, -boxCollider.size.z),
                boxCollider.center + new Vector3(-boxCollider.size.x, -boxCollider.size.y,  boxCollider.size.z),
                boxCollider.center + new Vector3(-boxCollider.size.x,  boxCollider.size.y, -boxCollider.size.z),
                boxCollider.center + new Vector3(-boxCollider.size.x,  boxCollider.size.y,  boxCollider.size.z),
                boxCollider.center + new Vector3( boxCollider.size.x, -boxCollider.size.y, -boxCollider.size.z),
                boxCollider.center + new Vector3( boxCollider.size.x, -boxCollider.size.y,  boxCollider.size.z),
                boxCollider.center + new Vector3( boxCollider.size.x,  boxCollider.size.y, -boxCollider.size.z),
                boxCollider.center + new Vector3( boxCollider.size.x,  boxCollider.size.y,  boxCollider.size.z)
            };
            return Array.ConvertAll(localCorners, corner => boxCollider.transform.TransformPoint(corner));
        }

        private static List<Vector3> GetSphereVertices(SphereCollider sphereCollider, Vector3 u, Vector3 v)
        {
            List<Vector3> vertices = new List<Vector3>();
            Vector3 worldCenter = sphereCollider.transform.TransformPoint(sphereCollider.center);
            Vector3 lossyScale = sphereCollider.transform.lossyScale;
            
            float maxScale = Mathf.Max(Mathf.Abs(lossyScale.x), Mathf.Max(Mathf.Abs(lossyScale.y), Mathf.Abs(lossyScale.z)));
            float worldRadius = sphereCollider.radius * maxScale;
            float step = (Mathf.PI * 2f) / SPHERE_SEGMENTS;

            for (int i = 0; i < SPHERE_SEGMENTS; i++)
            {
                float angle = i * step;
                Vector3 worldPoint = worldCenter + (u * Mathf.Cos(angle) + v * Mathf.Sin(angle)) * worldRadius;
                vertices.Add(worldPoint);
            }

            return vertices;
        }

        private static List<Vector3> GetCapsuleVertices(CapsuleCollider capsuleCollider, Vector3 u, Vector3 v)
        {
            List<Vector3> vertices = new List<Vector3>();
            Vector3 worldCenter = capsuleCollider.transform.TransformPoint(capsuleCollider.center);
            Vector3 lossyScale = capsuleCollider.transform.lossyScale;
            Vector3 capsuleAxis = CapsuleIntToDirection(capsuleCollider.transform, capsuleCollider.direction);
            
            float maxScale = Mathf.Max(Mathf.Abs(lossyScale.x), Mathf.Max(Mathf.Abs(lossyScale.y), Mathf.Abs(lossyScale.z)));
            float worldRadius = capsuleCollider.radius * maxScale;
            float worldHeight = Mathf.Max(capsuleCollider.height * lossyScale[capsuleCollider.direction], worldRadius * 2f);
            float cylinderHalfHeight = (worldHeight * 0.5f) - worldRadius;
            Vector3 topCapsuleCenter = worldCenter + capsuleAxis * cylinderHalfHeight;
            Vector3 bottomCapsuleCenter = worldCenter - capsuleAxis * cylinderHalfHeight;
            
            float step = (Mathf.PI * 2f) / SPHERE_SEGMENTS;

            for (int i = 0; i < SPHERE_SEGMENTS; i++)
            {
                float angle = i * step;
                Vector3 offset = (u * Mathf.Cos(angle) + v * Mathf.Sin(angle)) * worldRadius;
                Vector3 point1 = topCapsuleCenter + offset;
                vertices.Add(point1);
                Vector3 point2 = bottomCapsuleCenter + offset;
                vertices.Add(point2);
            }

            return vertices;
        }

        private static Vector3[] GetMeshVertices(MeshCollider meshCollider)
        {
            Mesh mesh = meshCollider.sharedMesh;
            if(mesh == null) return Array.Empty<Vector3>();

            return Array.ConvertAll(mesh.vertices, point => meshCollider.transform.TransformPoint(point));
        }

        private static Vector2[] ProjectPointsToPlane(Vector3[] points, Vector3 u, Vector3 v)
        {
            return Array.ConvertAll(points, point => ProjectToPlane(point, u, v));
        }

        private static Vector2[] ProjectPointsToPlane(List<Vector3> points, Vector3 u, Vector3 v)
        {
            return Array.ConvertAll(points.ToArray(), point => ProjectToPlane(point, u, v));
        }

        private static Vector2 ProjectToPlane(Vector3 point, Vector3 u, Vector3 v)
        {
            return new Vector2(Vector3.Dot(point, u), Vector3.Dot(point, v));
        }

        private static Vector3 CapsuleIntToDirection(Transform transform, int integer)
        {
            switch (integer)
            {
                case 0:
                    return transform.TransformPoint(Vector3.right).normalized;
                case 1:
                    return transform.TransformPoint(Vector3.up).normalized;
                default:
                    return transform.TransformPoint(Vector3.forward).normalized;
            }
        }
        #endregion
    }
}
