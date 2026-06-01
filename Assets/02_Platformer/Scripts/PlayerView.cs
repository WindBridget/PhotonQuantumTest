using Quantum;
using UnityEngine;

namespace Starter.Platformer
{
	/// <summary>
	/// Handles the visual representation, animations, sounds and effects for a player character.
	/// Manages both local and remote player views, including camera control for local player
	/// and nameplates for remote players.
	/// </summary>
	public class PlayerView : QuantumEntityViewComponent<SceneContext>
	{
		[Header("References")]
		public PlayerInput Input;
		public Animator Animator;
		public Transform CameraPivot;
		public Transform CameraHandle;
		[Tooltip("Root transform for squash/stretch effects")]
		public Transform ScalingRoot;
		public UINameplate Nameplate;

		[Header("Sounds")]
		public AudioSource FootstepSound;
		public AudioClip JumpAudioClip;
		public AudioClip LandAudioClip;

		[Header("VFX")]
		public ParticleSystem DustParticles;

		public override void OnActivate(Frame frame)
		{
			QuantumEvent.Subscribe<EventJumped>(this, OnJumped);
			QuantumEvent.Subscribe<EventLanded>(this, OnLanded);

			var playerLink = GetPredictedQuantumComponent<PlayerLink>();
			if (Game.PlayerIsLocal(playerLink.PlayerRef))
			{
				// Store local player references
				ViewContext.LocalPlayer = playerLink.PlayerRef;
				ViewContext.LocalPlayerEntity = EntityRef;
			}
			else
			{
				// Nameplate is shown only for other players, not for a local player
				var playerData = frame.GetPlayerData(playerLink.PlayerRef);
				Nameplate.SetNickname(playerData != null ? playerData.PlayerNickname : string.Empty);
			}
		}

		public override void OnUpdateView()
		{
			var kcc = GetPredictedQuantumComponent<KCC>();

			// Update animator parameters based on movement state
			Animator.SetFloat(AnimatorId.Speed, kcc.RealSpeed.AsFloat);
			Animator.SetBool(AnimatorId.Grounded, kcc.IsGrounded);

			// Adjust footstep sound based on movement speed
			FootstepSound.enabled = kcc.IsGrounded && kcc.RealSpeed > 1;
			FootstepSound.pitch = kcc.RealSpeed > 6 ? 1.5f : 1f;

			// Smoothly interpolate character scale back to default after jump/land squash-stretch
			ScalingRoot.localScale = Vector3.Lerp(ScalingRoot.localScale, Vector3.one, Time.deltaTime * 8f);

			// Control dust particles emission based on ground movement
			var emission = DustParticles.emission;
			emission.enabled = kcc.IsGrounded && kcc.RealSpeed > 1;
		}

		public override void OnLateUpdateView()
		{
			// Camera control is only for local player
			if (EntityRef != ViewContext.LocalPlayerEntity)
				return;

			// Update camera pivot and transfer properties from camera handle to Main Camera
			CameraPivot.rotation = Quaternion.Euler(Input.LookRotation);
			Camera.main.transform.SetPositionAndRotation(CameraHandle.position, CameraHandle.rotation);
		}

		private void OnJumped(EventJumped callback)
		{
			if (callback.Entity != EntityRef)
				return;

			AudioSource.PlayClipAtPoint(JumpAudioClip, transform.position, 1f);
			ScalingRoot.localScale = new Vector3(0.5f, 1.5f, 0.5f);  // Stretch effect on jump
		}

		private void OnLanded(EventLanded callback)
		{
			if (callback.Entity != EntityRef)
				return;

			AudioSource.PlayClipAtPoint(LandAudioClip, transform.position, 1f);
			ScalingRoot.localScale = new Vector3(1.25f, 0.75f, 1.25f);  // Squash effect on land
		}
	}
}
