﻿using System.Collections.Generic;

using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit;
using UnityEngine;

using Interactables;

namespace Player
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(FixedJoint))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(ControllerPoseSynchronizer))]
    public class GrabbableController : MonoBehaviour, IMixedRealityInputHandler
    {
        [Header("Input")]
        [SerializeField] private MixedRealityInputAction action = MixedRealityInputAction.None;
        [SerializeField] private Handedness handedness = Handedness.None;

        private List<Grabbable> contactGrabbables = new List<Grabbable>();
        private ControllerPoseSynchronizer pose = null;
        private Grabbable currentGrabbable = null;
        private FixedJoint joint = null;

        private void Awake()
        {
            joint = GetComponent<FixedJoint>();
            pose = GetComponent<ControllerPoseSynchronizer>();
            CoreServices.InputSystem?.RegisterHandler<IMixedRealityInputHandler>(this);

            if (!joint) Debug.LogWarning("No FixedJoint Component Provided!");
            if (!pose) Debug.LogWarning("No ControllerPoseSynchronizer Component Provided!");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Grabbable"))
            {
                contactGrabbables.Add(other.GetComponent<Grabbable>());
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Grabbable"))
            {
                contactGrabbables.Remove(other.GetComponent<Grabbable>());
            }
        }

        public void OnInputUp(InputEventData eventData)
        {
            if (eventData.MixedRealityInputAction != action || eventData.Handedness != handedness || currentGrabbable == null) return;

            if (currentGrabbable != null && !currentGrabbable.IsEquippable) Drop();
        }

        public void OnInputDown(InputEventData eventData)
        {
            if (eventData.MixedRealityInputAction != action || eventData.Handedness != handedness) return;

            // Check if there is a grabbable equiped
            if (currentGrabbable != null && currentGrabbable.IsEquippable) Drop();
            else Pickup();
        }

        public void Pickup()
        {
            currentGrabbable = GetNearestGrabbable();

            if (!currentGrabbable) return;
            if (currentGrabbable.ActiveController) currentGrabbable.ActiveController.Drop();

            if (currentGrabbable.IsEquippable)
            {
                var poseTransform = pose.transform;
                currentGrabbable.transform.position = poseTransform.position;
                currentGrabbable.transform.rotation = poseTransform.rotation * Quaternion.Euler(currentGrabbable.EquippableOffset);
            }

            currentGrabbable.OnPickup();
            Rigidbody targetBody = currentGrabbable.GetComponent<Rigidbody>();
            joint.connectedBody = targetBody;

            currentGrabbable.ActiveController = this;
        }

        public void Drop()
        {
            if (!currentGrabbable) return;

            if (currentGrabbable.IsThrowable)
            {
                Vector3 velocity = pose.Controller.Velocity;
                Vector3 angularVelocity = pose.Controller.AngularVelocity;
                currentGrabbable.OnDrop(velocity, angularVelocity);
            }

            currentGrabbable.OnDrop();
            joint.connectedBody = null;
            currentGrabbable.ActiveController = null;
            currentGrabbable = null;
        }

        private Grabbable GetNearestGrabbable()
        {
            Grabbable nearestGrabbable = null;
            float minDistance = float.MaxValue;

            foreach (Grabbable grabbable in contactGrabbables)
            {
                float distance = (grabbable.transform.position - transform.position).sqrMagnitude;

                if (!(distance < minDistance))
                {
                    continue;
                }

                minDistance = distance;
                nearestGrabbable = grabbable;
            }

            return nearestGrabbable;
        }
    }
}
