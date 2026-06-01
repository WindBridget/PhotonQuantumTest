using UnityEngine.Scripting;

namespace Quantum.Platformer
{
	/// <summary>
	/// System that handles coin collection mechanics, including temporary disabling of collected coins
	/// and their reactivation after a specified refresh time.
	/// </summary>
	[Preserve]
	public unsafe class CoinSystem : SystemMainThreadFilter<CoinSystem.Filter>, ISignalCoinCollected
	{
		public struct Filter
		{
			public EntityRef          Entity;
			public Coin*              Coin;
			public PhysicsCollider3D* Collider;
		}

		public override void Update(Frame frame, ref Filter filter)
		{
			// Check if coin should be reactivated based on refresh timer
			if (filter.Coin->RefreshTimer.HasStoppedThisFrame(frame))
			{
				filter.Collider->Enabled = true;
			}
		}

		void ISignalCoinCollected.CoinCollected(Frame frame, EntityRef entity)
		{
			var coin = frame.Unsafe.GetPointer<Coin>(entity);
			// Start refresh timer to reactivate coin after specified delay
			coin->RefreshTimer = FrameTimer.FromSeconds(frame, coin->RefreshTime);

			// Disable coin trigger
			var collider = frame.Unsafe.GetPointer<PhysicsCollider3D>(entity);
			collider->Enabled = false;
		}
	}
}
