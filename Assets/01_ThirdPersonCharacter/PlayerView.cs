using Quantum;
using UnityEngine;

namespace Starter.ThirdPersonCharacter
{
	/// <summary>
	/// Handles the visual representation and animation of a player character, including camera control and footstep sounds.
	/// Works in conjunction with the quantum simulation to update the view based on predicted states.
	/// </summary>
	public class PlayerView : QuantumEntityViewComponent
	{
		[Header("References")]
		public PlayerInput Input;
		public Animator Animator;
		public Transform CameraPivot;
		public Transform CameraHandle;

		[Header("Sounds")]
        public AudioClip[] FootstepAudioClips;
		public AudioClip LandingAudioClip;
		[Range(0f, 1f)]
		public float FootstepVolumeWalk = 0.15f;
		[Range(0f, 1f)]
		public float FootstepVolumeSprint = 0.3f;

		public override void OnUpdateView()
		{
			var kcc = GetPredictedQuantumComponent<KCC>();
			var movement = GetPredictedQuantumComponent<Movement>();

			// Update animator parameters based on character's movement state
			Animator.SetFloat(AnimatorId.Speed, kcc.RealSpeed.AsFloat, 0.15f, Time.deltaTime);
			Animator.SetFloat(AnimatorId.MotionSpeed, 1f);
			Animator.SetBool(AnimatorId.Jump, movement.JumpInProgress);
			Animator.SetBool(AnimatorId.Grounded, kcc.IsGrounded);
			Animator.SetBool(AnimatorId.FreeFall, kcc.RealVelocity.Y < -10);
		}

		public override void OnLateUpdateView()
		{
			var playerLink = GetPredictedQuantumComponent<PlayerLink>();

			// Only update camera for local player
			if (Game.PlayerIsLocal(playerLink.PlayerRef) == false)
				return;

			// Update camera pivot based on input and sync main camera position/rotation with camera handle
			CameraPivot.rotation = Quaternion.Euler(Input.LookRotation);
			Camera.main.transform.SetPositionAndRotation(CameraHandle.position, CameraHandle.rotation);
		}

		// Animation event triggered during walking/running animations
		private void OnFootstep(AnimationEvent animationEvent)
		{
			if (animationEvent.animatorClipInfo.weight < 0.5f)
				return;

			if (FootstepAudioClips.Length > 0)
			{
				// Adjust volume based on movement speed (walking vs sprinting)
				float volume = GetPredictedQuantumComponent<KCC>().Data.RealSpeed < 4 ? FootstepVolumeWalk : FootstepVolumeSprint;
				var index = Random.Range(0, FootstepAudioClips.Length);
				AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.position, volume);
			}
		}

		// Animation event triggered when character lands after being airborne
		private void OnLand(AnimationEvent animationEvent)
		{
			AudioSource.PlayClipAtPoint(LandingAudioClip, transform.position, FootstepVolumeSprint);
		}
	}
}
