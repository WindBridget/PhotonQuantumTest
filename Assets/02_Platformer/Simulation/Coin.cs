namespace Quantum
{
	/// <summary>
	/// Partial implementation of a collectible coin component.
	/// </summary>
	public partial struct Coin
	{
		public bool IsActive(Frame frame) => RefreshTimer.IsRunning(frame) == false;
	}
}
