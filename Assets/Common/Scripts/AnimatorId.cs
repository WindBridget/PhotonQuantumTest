using UnityEngine;

namespace Starter
{
	/// <summary>
	/// Provides cached animator parameter hash IDs to improve performance when setting animator parameters.
	/// These IDs are pre-computed at startup instead of converting strings to hashes at runtime.
	/// </summary>
	public static class AnimatorId
	{
		// Movement related parameters
		public static readonly int Speed        = Animator.StringToHash("Speed");
		public static readonly int SpeedX       = Animator.StringToHash("SpeedX");  // Horizontal movement speed
		public static readonly int SpeedZ       = Animator.StringToHash("SpeedZ");  // Forward/backward movement speed

		// Character state parameters
		public static readonly int Grounded     = Animator.StringToHash("Grounded");
		public static readonly int Jump         = Animator.StringToHash("Jump");
		public static readonly int FreeFall     = Animator.StringToHash("FreeFall");
		public static readonly int MotionSpeed  = Animator.StringToHash("MotionSpeed");

		// Combat related parameters
		public static readonly int Pitch        = Animator.StringToHash("Pitch");   // Aim pitch angle
		public static readonly int Shoot        = Animator.StringToHash("Shoot");
	}
}
