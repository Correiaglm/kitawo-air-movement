namespace Kitawo.Core;

public sealed class PlayerTuning
{
	public float MoveSpeed { get; set; } = 240f;

	public float RunSpeed { get; set; } = 480f;

	public float JumpPeakHeight { get; set; } = 120f;

	public float TimeToPeak { get; set; } = 0.35f;

	public float FallingGravity { get; set; } = 1800f;

	public float MaxFallSpeed { get; set; } = 800f;

	public float GroundAcceleration { get; set; } = 1800f;

	public float GroundDeceleration { get; set; } = 2200f;

	public float AirAcceleration { get; set; } = 1800f;

	public float AirDeceleration { get; set; } = 2200f;

	public float AirResistance { get; set; } = 150f;

	public float CoyoteTime { get; set; } = 0.083f;

	public float PlatformDropTime { get; set; } = 0.2f;

	public PlayerTuningSnapshot Snapshot()
	{
		return new PlayerTuningSnapshot(MoveSpeed, RunSpeed, JumpPeakHeight, TimeToPeak, FallingGravity, MaxFallSpeed, GroundAcceleration, GroundDeceleration, CoyoteTime, PlatformDropTime);
	}
}
