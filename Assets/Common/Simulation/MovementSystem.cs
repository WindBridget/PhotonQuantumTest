using Photon.Deterministic;
using UnityEngine.Scripting;

namespace Quantum
{
	/// <summary>
	/// Handles character movement including walking, running, jumping and rotation.
	/// Processes player input to control character motion and orientation.
	/// </summary>
	[Preserve]
	public unsafe class MovementSystem : SystemMainThreadFilter<MovementSystem.Filter>
	{
		public struct Filter
		{
			public EntityRef    Entity;
			public PlayerLink*  PlayerLink;
			public Transform3D* Transform;
			public Movement*    Movement;
			public KCC*         KCC;
		}

		public override void Update(Frame frame, ref Filter filter)
		{
			if (filter.KCC->IsActive == false)
				return;

			var kcc = filter.KCC;
			var movement = filter.Movement;
			var input = frame.GetPlayerInput(filter.PlayerLink->PlayerRef);

			// Check if player fell out of bounds
			if (filter.Transform->Position.Y < -15)
			{
				frame.Signals.PlayerFell(filter.Entity);
				return;
			}

			// Reset jump state when landing
			if (movement->JumpInProgress && kcc->IsGrounded)
			{
				frame.Events.Landed(filter.Entity);
				movement->JumpInProgress = false;
			}

			// Convert input direction to world space based on camera orientation
			var lookRotation = FPQuaternion.Euler(0, input->LookRotation.Y, 0);
			var moveDirection = lookRotation * new FPVector3(input->MoveDirection.X, 0, input->MoveDirection.Y);

			// Handle character rotation
			if (movement->SetLookRotation)
			{
				kcc->SetLookRotation(input->LookRotation.X, input->LookRotation.Y);
			}
			else if (moveDirection != default)
			{
				// Smoothly rotate character to face movement direction
				var currentRotation = kcc->Data.TransformRotation;
				var targetRotation = FPQuaternion.LookRotation(moveDirection);
				var nextRotation = FPQuaternion.Lerp(currentRotation, targetRotation, movement->RotationSpeed * frame.DeltaTime);

				kcc->SetLookRotation(nextRotation);
			}

			// Apply walk speed multiplier when not sprinting
			if (input->Sprint == false)
			{
				moveDirection *= movement->WalkSpeedMultiplier;
			}

			kcc->SetInputDirection(moveDirection);

			// Process jump input
			if (input->Jump.WasPressed == true && kcc->IsGrounded == true)
			{
				kcc->Jump(FPVector3.Up * filter.Movement->JumpForce);
				movement->JumpInProgress = true;

				frame.Events.Jumped(filter.Entity);
			}
		}
	}
}
