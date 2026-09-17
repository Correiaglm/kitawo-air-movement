using System;
using System.Numerics;
using ImGuiNET;

namespace Kitawo.Core.Screens;

public class DebugOverlayImGui(PlayerTuning playerTuning)
{
	private sealed record FloatEditor(string Label, string Id, Func<float> Get, Action<float> Set, float Speed, float Min, float Max, string Format);

	private readonly FloatEditor[] editors = new FloatEditor[]
	{
		new FloatEditor("Move Speed (px/s)", "MoveSpeed", () => playerTuning.MoveSpeed, delegate(float value)
		{
			playerTuning.MoveSpeed = value;
		}, 1f, 0f, 600f, "%.1f"),
		new FloatEditor("Run Speed (px/s)", "RunSpeed", () => playerTuning.RunSpeed, delegate(float value)
		{
			playerTuning.RunSpeed = value;
		}, 1f, 0f, 1200f, "%.1f"),
		new FloatEditor("Jump Peak Height (pixels)", "JumpPeakHeight", () => playerTuning.JumpPeakHeight, delegate(float value)
		{
			playerTuning.JumpPeakHeight = value;
		}, 1f, 0f, 1200f, "%.1f"),
		new FloatEditor("Time to Peak (seconds)", "TimeToPeak", () => playerTuning.TimeToPeak, delegate(float value)
		{
			playerTuning.TimeToPeak = value;
		}, 0.001f, 0.01f, 2f, "%.3f"),
		new FloatEditor("Falling Gravity (px/s^2)", "FallingGravity", () => playerTuning.FallingGravity, delegate(float value)
		{
			playerTuning.FallingGravity = value;
		}, 10f, 0f, 4000f, "%.1f"),
		new FloatEditor("Max Fall Speed (px/s)", "MaxFallSpeed", () => playerTuning.MaxFallSpeed, delegate(float value)
		{
			playerTuning.MaxFallSpeed = value;
		}, 1f, 0f, 1600f, "%.1f"),
		new FloatEditor("Ground Acceleration (px/s^2)", "GroundAcceleration", () => playerTuning.GroundAcceleration, delegate(float value)
		{
			playerTuning.GroundAcceleration = value;
		}, 0.1f, 0f, 5000f, "%.1f"),
		new FloatEditor("Ground Deceleration (px/s^2)", "GroundDeceleration", () => playerTuning.GroundDeceleration, delegate(float value)
		{
			playerTuning.GroundDeceleration = value;
		}, 0.1f, 0f, 5000f, "%.1f"),

		new FloatEditor(
			"Air Acceleration (px/s^2)",
			"AirAcceleration",
			() => playerTuning.AirAcceleration,
			value => playerTuning.AirAcceleration = value,
			0.1f, 0f, 5000f, "%.1f"),

		new FloatEditor(
			"Air Deceleration (px/s^2)",
			"AirDeceleration",
			() => playerTuning.AirDeceleration,
			value => playerTuning.AirDeceleration = value,
			0.1f, 0f, 5000f, "%.1f"),

		new FloatEditor(
			"Air Resistance (px/s^2)",
			"AirResistance",
			() => playerTuning.AirResistance,
			value => playerTuning.AirResistance = value,
			0.1f, 0f, 5000f, "%.1f"),
		new FloatEditor("Coyote Time (seconds)", "CoyoteTime", () => playerTuning.CoyoteTime, delegate(float value)
		{
			playerTuning.CoyoteTime = value;
		}, 0.001f, 0f, 1f, "%.3f"),
		new FloatEditor("Platform Drop Time (seconds)", "PlatformDropTime", () => playerTuning.PlatformDropTime, delegate(float value)
		{
			playerTuning.PlatformDropTime = value;
		}, 0.005f, 0f, 1f, "%.3f")
	};

	public bool Visible { get; set; }

	public void ToggleVisibility()
	{
		Visible = !Visible;
	}

	public void Draw(float cameraZoomLevel)
	{
		if (!Visible)
		{
			return;
		}
		ImGui.SetNextWindowSize(new Vector2(480f, 780f), ImGuiCond.FirstUseEver);
		ImGui.SetNextWindowSizeConstraints(new Vector2(320f, 480f), new Vector2(float.MaxValue));
		if (!ImGui.Begin("Movement Editor"))
		{
			ImGui.End();
			return;
		}
		ImGui.Text($"Zoom Level: {cameraZoomLevel:0.#}");
		ImGui.PushItemWidth(220f);
		FloatEditor[] array = editors;
		foreach (FloatEditor editor in array)
		{
			ImGui.TextUnformatted(editor.Label);
			float value = editor.Get();
			if (ImGui.DragFloat("##" + editor.Id, ref value, editor.Speed, editor.Min, editor.Max, editor.Format, ImGuiSliderFlags.AlwaysClamp))
			{
				editor.Set(value);
			}
		}
		ImGui.PopItemWidth();
		ImGui.End();
	}
}
