﻿using System;
using UnityEngine;
using System.Collections.Generic;

namespace Crescendo.API {

    [RequireComponent(typeof(Camera))]
    public sealed class CameraController : Singleton<CameraController> {

        [SerializeField]
        private Vector3 targetPositionBias;

        [SerializeField]
        private Vector2 padding;

        [SerializeField]
        private float cameraSpeed = 1f;

        [SerializeField, MinMaxSlider(0, 180)]
        private Vector2 FovRange;

        private Camera _camera;
        private HashSet<Transform> targets;

        protected override void Awake() {
            base.Awake();
            _camera = GetComponent<Camera>();
            targets = new HashSet<Transform>();
        }

        void LateUpdate() {
            var count = 0;
            
            //Find the Bounds in which
            Vector3 sum = Vector3.zero;
            Vector3 min = Vector3.one * float.PositiveInfinity;
            Vector3 max = Vector3.one * float.NegativeInfinity;
            foreach (Transform target in targets) {
                if (target == null)
                    continue;
                count++;
                sum += target.position;
                min = Vector3.Min(min, target.position);
                max = Vector3.Max(max, target.position);
            }

            Vector3 targetPosition = targetPositionBias + ((count <= 0) ? Vector3.zero : sum / count);
            Vector2 size = (Vector2) max - (Vector2) min;

            // Calculate the actual padding to use
            Vector2 actualPadding = new Vector2(1 + 2 * padding.x, 1 + 2 * padding.y);

            // Compute Hadamard product between size and inverse padding to add the padding desired.
            size = new Vector2(size.x * actualPadding.x, size.y * actualPadding.y);
            
            // Calculate the target field of view for the proper level of zoom
            float targetFOV = 2f * Mathf.Atan(size.x * 0.5f / Mathf.Abs(transform.position.z - targetPosition.z)) * Mathf.Rad2Deg;

            // Clamp the FOV so it isn't too small or too big.
            targetFOV = Mathf.Clamp(targetFOV, FovRange.x, FovRange.y);

            // Keep the camera in the same Z plane.
            targetPosition.z = transform.position.z;

            // Lerp both the FOV and the position at the desired speeds
            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, targetFOV, Util.dt * cameraSpeed);
            transform.position = Vector3.Lerp(transform.position, targetPosition, Util.dt * cameraSpeed);
        }

        public static void AddTarget(Component target) {
            if(target == null)
                throw new ArgumentNullException("target");
            if(Instance == null)
                throw new InvalidOperationException("There is no CameraController instance in this scene.");
            Transform targetTransform = target.transform;
            Instance.targets.Add(targetTransform);
        }

        public static void RemoveTarget(Component target) {
            if (target == null || Instance == null)
                return;

            Instance.targets.Remove(target.transform);
        }

        public static void AddTarget(GameObject target) {
            if (target == null)
                throw new ArgumentNullException("target");
            AddTarget(target.transform);
        }

        public static void RemoveTarget(GameObject target) {
            if (target == null)
                return;
            RemoveTarget(target.transform);
        }

    }

}
