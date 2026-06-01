namespace Quantum
{
	/// <summary>
	/// Partial health component implementation to provide common utility methods.
	/// </summary>
	public partial struct Health
	{
		public bool IsAlive => CurrentHealth > 0;

		/// <summary>
		/// Checks if entity's death sequence is complete, including death animation
		/// </summary>
		public bool IsFinished(Frame frame)
		{
			return IsAlive == false && DeathTimer.IsRunning(frame) == false;
		}

		/// <summary>
		/// Applies damage to the entity
		/// </summary>
		/// <returns>The actual amount of damage applied</returns>
		public int ApplyDamage(Frame frame, EntityRef entity, int damage)
		{
			// Skip if already dead or invalid damage
			if (CurrentHealth <= 0 || damage < 0)
				return 0;

			// Clamp damage to remaining health
			if (damage > CurrentHealth)
			{
				damage = CurrentHealth;
			}

			CurrentHealth -= damage;

			if (IsAlive == false && DeathTime > 0)
			{
				frame.Signals.EntityDied(entity);

				// Start death timer
				if (DeathTime > 0)
				{
					DeathTimer = FrameTimer.FromSeconds(frame, DeathTime);
				}
			}

			frame.Events.DamageReceived(entity);

			return damage;
		}

		/// <summary>
		/// Restores entity to full health and resets death timer
		/// </summary>
		public void Revive()
		{
			CurrentHealth = MaxHealth;
			DeathTimer = default;
		}
	}
}
