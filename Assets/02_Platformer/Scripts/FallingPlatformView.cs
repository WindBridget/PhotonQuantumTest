using UnityEngine;
using Quantum;

namespace Starter.Platformer
{
	/// <summary>
	/// A platform that falls when player steps onto it.
	/// </summary>
	public class FallingPlatformView : QuantumEntityViewComponent
	{
		public AudioClip FallAudioClip;

		public override void OnActivate(Frame frame)
		{
			QuantumEvent.Subscribe<EventPlatformFell>(this, OnPlatformFell);
		}

		private void OnPlatformFell(EventPlatformFell callback)
		{
			if (callback.Entity != EntityRef)
				return;

			AudioSource.PlayClipAtPoint(FallAudioClip, transform.position, 1f);
		}
	}
}
