﻿using System;
using System.Collections;
using System.Collections.Generic;
using Gemso.API;
using UnityEngine;

namespace Crescendo.API {

    /// <summary>
    /// General character class for handling the physics and animations of individual characters
    /// </summary>
    /// Author: James Liu
    /// Authored on 07/01/2015
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Rigidbody))]
	public class Character : GensoBehaviour, IDamageable, IKnocckbackable {

        private enum FacingMode { Rotation, Scale }

        private CapsuleCollider movementCollider;
        private CapsuleCollider triggerCollider;
        private Rigidbody _rigidbody;
        private Collider[] hurtboxes;

        [SerializeField]
        private FacingMode _facingMode = FacingMode.Rotation;

        [SerializeField]
        private float triggerSizeRatio = 1.5f;

        // Private state variables
        private bool _grounded;
        private bool _helpless;
        private bool _facing;
        private bool _dashing;
        private bool _invinicible;
		// Boolean indicating whether the player can move the character or not
		// Could be used for attacking, knockback, stagger, etc
		private bool _canMove = true;

        public Vector3 Velocity
        {
            get { return _rigidbody.velocity; }
            set { _rigidbody.velocity = value; }
        }

        public float Mass
        {
            get { return _rigidbody.mass; }
            set { _rigidbody.mass = value; }
        }

        public void AddForce(Vector3 force) {
            _rigidbody.AddForce(force);
        }

        public void AddForce(float x, float y) {
            _rigidbody.AddForce(x, y, 0f);
        }

        public void AddForce(float x, float y, float z) {
            _rigidbody.AddForce(x, y, z);
        }

        public int PlayerNumber { get; set; }

        public Color PlayerColor {
            get { return Game.GetPlayerColor(PlayerNumber); }
        }

        public ICharacterInput InputSource { get; set; }

        public bool IsGrounded {
            get { return _grounded; }
            set {
                bool changed = _grounded != value;
                _grounded = value;
                if (value)
                    IsHelpless = false;
                if (changed)
                    OnGrounded.SafeInvoke();
            }
        }

        /// <summary>
        /// The direction the character is currently facing.
        /// If set to true, the character faces the right.
        /// If set to false, the character faces the left.
        /// 
        /// The method in which the character is flipped depends on what the Facing Mode parameter is set to.
        /// </summary>
        public bool Facing {
            get {
                if (_facingMode == FacingMode.Scale)
                    return transform.localScale.x > 0;
                else
                    return transform.eulerAngles.y > 179f;
            }
            set {
                if (_facing != value) {
                    if (_facingMode == FacingMode.Rotation)
                        transform.Rotate(0f, 180f, 0f);
                    else {
                        Vector3 temp = transform.localScale;
                        temp.x *= -1;
                        transform.localScale = temp;
                    }
                }
                _facing = value;
            }
        }

        public bool IsInvincible {
            get { return _invinicible; }
            set {
                if (_invinicible == value)
                    return;

                if (value)
                    Debug.Log(name + " is now invincible.");
                else
                    Debug.Log(name + " is no longer invincible.");

                foreach (var hurtbox in hurtboxes)
                    hurtbox.enabled = !value;

                _invinicible = value;
            }
        }

        public bool IsDashing {
            get {
                return IsGrounded && _dashing;
            }
            set {
                _dashing = value;
            }
        }

        public bool IsFastFalling {
            get {
                return !IsGrounded && InputSource != null && InputSource.Crouch;
            }
        }

        public bool IsCrouching {
            get {
                return IsGrounded && InputSource != null && InputSource.Crouch;
            }
        }

        public bool IsHelpless {
            get {
                return !IsGrounded && _helpless;
            }
            set {
                bool changed = _helpless == value;
                _helpless = value;
                if(changed)
                    OnHelpless.SafeInvoke();
            }
        }

		// I only added this to be consistent with the other state variables, I have no idea whether it's actually necessary
		public bool CanMove {
			get {
				return _canMove;
			}
			set {
				_canMove = value;
			}
		}

        public event Action OnJump;
        public event Action OnHelpless;
        public event Action OnGrounded;
        public event Action<Vector2> OnMove;
        public event Action OnBlastZoneExit;
        public event Action<float> OnDamage;
        public event Action OnKnockback;
        public event Action OnAttack;

        public float Height {
            get { return movementCollider.height; }
        }

        #region Unity Callbacks

        protected virtual void Awake() {
            movementCollider = GetComponent<CapsuleCollider>();
            movementCollider.isTrigger = false;

            triggerCollider = gameObject.AddComponent<CapsuleCollider>();
            triggerCollider.isTrigger = true;

			// Moved below the assignment of both colliders, so that it can properly register hurtboxes
			FindHurtboxes ();

            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
        }

        protected virtual void OnEnable() {
            // TODO: Find a better place to put this
            CameraController.AddTarget(this);
        }

        protected virtual void OnDisable() {
            // TODO: Find a better place to put this
            CameraController.RemoveTarget(this);
        }

        protected virtual void Update() {
            // Sync Trigger and Movement Colliders
            triggerCollider.center = movementCollider.center;
            triggerCollider.direction = movementCollider.direction;
            triggerCollider.height = movementCollider.height * triggerSizeRatio;
            triggerCollider.radius = movementCollider.radius * triggerSizeRatio;

            if (InputSource == null)
                return;

            Vector2 movement = InputSource.Movement;

			// Now checks CanMove
            if(movement != Vector2.zero && CanMove)
                Move(movement);

            //Ensure that the character is walking in the right direction
            if ((movement.x > 0 && Facing) ||
               (movement.x < 0 && !Facing))
            {
                Facing = !Facing;
            }

			// Now checks CanMove
            if (InputSource.Jump && CanMove)
                Jump();

            if (InputSource.Attack)
                Attack();
        }

        protected virtual void OnDrawGizmos() {
            FindHurtboxes();
            GizmoUtil.DrawHitboxes(hurtboxes, HitboxType.Damageable, x => x.enabled);
            GizmoUtil.DrawHitboxes(hurtboxes, HitboxType.Intangible, x => !x.enabled);
        }
        #endregion

        void FindHurtboxes() {
            List<Collider> tempHurtboxes = new List<Collider>();
            foreach (Collider collider in GetComponentsInChildren<Collider>()) {
                if (!collider.CheckLayer(Game.HurtboxLayers))
                    continue;
                Hurtbox.Register(this, collider);
                tempHurtboxes.Add(collider);
            }
            hurtboxes = tempHurtboxes.ToArray();
        }

        public virtual void Move(Vector2 direction) {
            OnMove.SafeInvoke(direction);
        }

        public virtual void Jump() {
            OnJump.SafeInvoke();
        }

        public void BlastZoneExit() {
            OnBlastZoneExit.SafeInvoke();
        }

        public void TemporaryInvincibility(float time) {
            StartCoroutine(TempInvincibility(time));
        }

        public void Attack() {
            OnAttack.SafeInvoke();
        }

        IEnumerator TempInvincibility(float duration) {
            IsInvincible = true;
            var t = 0f;
            while (t < duration) {
                yield return null;
                t += Util.dt;
            }
            IsInvincible = false;
        }

        public void Damage(float damage) {
            OnDamage.SafeInvoke(damage);
        }

        public void Knockback(float baseKnockback) {
            OnKnockback.SafeInvoke();
        }

		// Coroutine that prevents input from effecting character movement for a given amount of time (but retains velocity)
		// **Properly locks movement on characters that are not aerial, but for some reason they will not play animations
		// until they are grounded and all movement input is released**
		public IEnumerator LockMovement(float time) {
			CanMove = false;
			yield return new WaitForSeconds (time);
			CanMove = true;
		}
    }

}
