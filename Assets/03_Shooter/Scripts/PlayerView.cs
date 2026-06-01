using Quantum;
using UnityEngine;
using UnityEngine.Rendering;

namespace Starter.Shooter
{
	/// <summary>
	/// Handles the visual representation and effects for a player character, including first/third person views,
	/// animations, sounds and VFX. Provides special handling for local vs remote players.
	/// </summary>
	public class PlayerView : QuantumEntityViewComponent<SceneContext>
	{
		public bool IsLocalPlayer => ViewContext.LocalPlayerEntity == EntityRef;

		[Header("References")]
		public PlayerInput Input;
		public Animator Animator;
		public Transform CameraPivot;
		public Transform CameraHandle;
		public Transform ScalingRoot;
		public UINameplate Nameplate;
		public Renderer[] HeadRenderers;
		public GameObject[] FirstPersonOverlayObjects;

		[Header("Fire Setup")]
		public GameObject ImpactPrefab;
		public ParticleSystem MuzzleParticle;

		[Header("Animation Setup")]
		public Transform ChestTargetPosition;
		public Transform ChestBone;

		[Header("Sounds")]
		public AudioSource FireSound;
		public AudioSource FootstepSound;
		public AudioClip JumpAudioClip;
		public AudioClip LandAudioClip;

		[Header("VFX")]
		public ParticleSystem DustParticles;

		public override void OnActivate(Frame frame)
		{
			QuantumEvent.Subscribe<EventFired>(this, OnFired);
			QuantumEvent.Subscribe<EventJumped>(this, OnJumped);
			QuantumEvent.Subscribe<EventLanded>(this, OnLanded);
			QuantumEvent.Subscribe<EventDamageReceived>(this, OnDamageReceived);

			var playerLink = GetPredictedQuantumComponent<PlayerLink>();
			if (Game.PlayerIsLocal(playerLink.PlayerRef))
			{
				// Store local player references
				ViewContext.LocalPlayer = playerLink.PlayerRef;
				ViewContext.LocalPlayerEntity = EntityRef;

				// Make head invisible in first person view but keep casting shadows
				for (int i = 0; i < HeadRenderers.Length; i++)
				{
					HeadRenderers[i].shadowCastingMode = ShadowCastingMode.ShadowsOnly;
				}

				// Place weapon model on overlay layer to prevent wall clipping
				int overlayLayer = UnityEngine.LayerMask.NameToLayer("FirstPersonOverlay");
				for (int i = 0; i < FirstPersonOverlayObjects.Length; i++)
				{
					FirstPersonOverlayObjects[i].layer = overlayLayer;
				}
			}
			else
			{
				// Show nameplate only for remote players
				var playerData = frame.GetPlayerData(playerLink.PlayerRef);
				Nameplate.SetNickname(playerData != null ? playerData.PlayerNickname : string.Empty);
			}
		}

		public override void OnUpdateView()
		{
			var kcc = GetPredictedQuantumComponent<KCC>();

			// Transform velocity vector to local space.
			var moveSpeed = transform.InverseTransformVector(kcc.Data.RealVelocity.ToUnityVector3());

			// Update animator parameters based on movement and state
			Animator.SetFloat(AnimatorId.SpeedX, moveSpeed.x, 0.1f, Time.deltaTime);
			Animator.SetFloat(AnimatorId.SpeedZ, moveSpeed.z, 0.1f, Time.deltaTime);
			Animator.SetBool(AnimatorId.Grounded, kcc.IsGrounded);
			Animator.SetFloat(AnimatorId.Pitch, kcc.GetLookRotation(true, false).X.AsFloat, 0.02f, Time.deltaTime);

			// Adjust footstep sound based on movement speed
			FootstepSound.enabled = kcc.IsGrounded && kcc.RealSpeed > 1;
			FootstepSound.pitch = kcc.RealSpeed > 6 ? 1.5f : 1f;

			// Smooth out scaling effects
			ScalingRoot.localScale = Vector3.Lerp(ScalingRoot.localScale, Vector3.one, Time.deltaTime * 8f);

			// Control dust particles based on ground movement
			var emission = DustParticles.emission;
			emission.enabled = kcc.IsGrounded && kcc.RealSpeed > 1;
		}

		public override void OnLateUpdateView()
		{
			var health = GetPredictedQuantumComponent<Health>();
			if (health.IsAlive == false)
				return;

			var kcc = GetPredictedQuantumComponent<KCC>();

			// Update camera rotation - use direct input for local player, simulated data for remote players
			if (IsLocalPlayer)
			{
				CameraPivot.rotation = Quaternion.Euler(Input.LookRotation);
			}
			else
			{
				CameraPivot.localRotation = Quaternion.Euler(kcc.GetLookRotation(true, false).ToUnityVector2());
			}

			// Apply chest IK by blending between target position and animated position
			float blendAmount = IsLocalPlayer ? 0.05f : 0.2f;
			ChestBone.position = Vector3.Lerp(ChestTargetPosition.position, ChestBone.position, blendAmount);
			ChestBone.rotation = Quaternion.Lerp(ChestTargetPosition.rotation, ChestBone.rotation, blendAmount);

			// Only the local player needs to update camera
			if (IsLocalPlayer)
			{
				// Transfer properties from camera handle to Main Camera.
				Camera.main.transform.SetPositionAndRotation(CameraHandle.position, CameraHandle.rotation);
			}
		}

		private void OnFired(EventFired callback)
		{
			if (callback.FireEntity != EntityRef)
				return;

			FireSound.PlayOneShot(FireSound.clip);
			MuzzleParticle.Play();
			Animator.SetTrigger(AnimatorId.Shoot);

			// Spawn impact VFX when hitting environment (not entities)
			if (callback.HitNormal != default && callback.HitEntity.IsValid == false)
			{
				// Impact gets destroyed automatically with DestroyAfter script
				var impactRotation = Quaternion.LookRotation(callback.HitNormal.ToUnityVector3());
				Instantiate(ImpactPrefab, callback.EndPosition.ToUnityVector3(), impactRotation);
			}
		}

		private void OnJumped(EventJumped callback)
		{
			if (callback.Entity != EntityRef)
				return;

			AudioSource.PlayClipAtPoint(JumpAudioClip, transform.position, 0.7f);

			if (IsLocalPlayer == false)
			{
				// Apply vertical stretch effect on jump
				ScalingRoot.localScale = new Vector3(0.5f, 1.5f, 0.5f);
			}
		}

		private void OnLanded(EventLanded callback)
		{
			if (callback.Entity != EntityRef)
				return;

			AudioSource.PlayClipAtPoint(LandAudioClip, transform.position, 1f);

			if (IsLocalPlayer == false)
			{
				// Apply squash effect on landing
				ScalingRoot.localScale = new Vector3(1.25f, 0.75f, 1.25f);
			}
		}

		private void OnDamageReceived(EventDamageReceived callback)
		{
			if (callback.Entity != EntityRef)
				return;

			if (IsLocalPlayer == false)
			{
				// Apply hit reaction squash/stretch
				ScalingRoot.localScale = new Vector3(0.85f, 1.15f, 0.85f);
			}
		}
	}
}
