using System;
using FlatRedBall.AnimationChain;
using FlatRedBall2.AnimationEditorCommon;
using Kitawo.Core.Combat;
using Kitawo.Ecb;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;

namespace Kitawo.Core;

public sealed class Player : IEcbBody2D, ICollisionActor
{
	private readonly record struct VerticalStep(float MotionVelocity, float FinalVelocity);

	public AnimationPlayer<AnimationFrame>? AnimationPlayer;

	private float leftGroundTimer;

	private float direction = 1f;

	private bool clipLocked;

	public PlayerTuning Tuning { get; set; } = new PlayerTuning();

	public int Id { get; }

	public Transform2 Transform { get; set; } = new Transform2();

	public Transform2 Facing { get; } = new Transform2();

	public Vector2 Position
	{
		get
		{
			return Transform.Position;
		}
		set
		{
			Transform.Position = value;
		}
	}

	public Vector2 DrawOriginPx { get; set; } = new Vector2(24f, 40f);

	public Vector2 Velocity { get; set; }

	public PlayerState State { get; private set; }

	private PlayerGait Gait { get; set; }

	public EcbController2D Controller { get; }

	public CollisionShape2D Shape => new CollisionShape2D(Controller.Bounds);

	public AttackComponent2D AttackComponent { get; set; }

	public AnimationChainList<AnimationFrame>? Animations { get; set; }

	public Texture2D? DebugHitboxTexture { get; set; }

	public Player(int id, Vector2 position, CollisionWorld2D world)
	{
		Id = id;
		Position = position;
		Facing.Parent = Transform;
		Velocity = Vector2.Zero;
		Controller = new EcbController2D(this, world)
		{
			HalfWidth = 10f,
			HalfHeight = 18f,
			BoxOffset = new Vector2(0f, -18f)
		};
		Controller.LeftGround += OnLeftGround;
		Controller.Landed += OnLanded;
		AttackComponent = new AttackComponent2D(this, new AttackHitbox2D(GetCurrentFrameForAttack));
	}

	private float GetGravity(float verticalVelocity)
	{
		if (!(verticalVelocity < 0f))
		{
			return Tuning.FallingGravity;
		}
		return GetRisingGravity();
	}

	private VerticalStep IntegrateVerticalVelocity(float initialVelocity, float dt)
	{
		float gravity = GetGravity(initialVelocity);
		float finalVelocity = MathF.Min(initialVelocity + gravity * dt, Tuning.MaxFallSpeed);
		return new VerticalStep((initialVelocity + finalVelocity) * 0.5f, finalVelocity);
	}

	public void Update(GameTime gameTime, float move, IPlayerAction? playerAction)
	{
		float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
		move = MathHelper.Clamp(move, -1f, 1f);
		Vector2 localVelocity = Velocity;
		AttackComponent.Tick();
		if (!(playerAction is ActionJump))
		{
			if (!(playerAction is ActionDrop))
			{
				if (!(playerAction is ActionRun))
				{
					if (!(playerAction is ActionLightAttack))
					{
						if (playerAction == null && move == 0f)
						{
							Gait = PlayerGait.Walk;
						}
					}
					else if (Controller.IsGrounded)
					{
						AttackComponent.RequestAttack(AttackIntent.Light);
					}
				}
				else if (Controller.IsGrounded)
				{
					Gait = PlayerGait.Run;
				}
			}
			else if (Controller.IsGrounded)
			{
				ApplyJumpAndDrop(jumpPressed: false, dropPressed: true, ref localVelocity);
			}
		}
		else if (!AttackComponent.IsBusy)
		{
			ApplyJumpAndDrop(jumpPressed: true, dropPressed: false, ref localVelocity);
		}
		State = ResolveState(move, localVelocity.Y);
		switch (State)
		{
		case PlayerState.Idle:
			ApplyHorizontalMovement(0f, dt, ref localVelocity);
			break;
		case PlayerState.Walking:
			ApplyHorizontalMovement(move, dt, ref localVelocity);
			break;
		case PlayerState.Airborne:
			ApplyAirMovement(move, dt, ref localVelocity);
			break;
		case PlayerState.Attacking:
			if (!Controller.IsGrounded || localVelocity.Y < 0f)
			{
				ApplyAirMovement(0f, dt, ref localVelocity);
			}
			else
			{
				ApplyHorizontalMovement(0f, dt, ref localVelocity);
			}
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
		VerticalStep vertical = IntegrateVerticalVelocity(localVelocity.Y, dt);
		localVelocity.Y = vertical.MotionVelocity;
		Velocity = localVelocity;
		Controller.Step(dt);
		UpdateCoyoteTime(dt);
		bool verticalMotionBlocked = ((vertical.MotionVelocity < 0f) ? (Controller.CeilingNormal != Vector2.Zero) : Controller.IsGrounded);
		Velocity = new Vector2(Velocity.X, verticalMotionBlocked ? 0f : vertical.FinalVelocity);
		if (State != PlayerState.Attacking)
		{
			AnimationPlayer?.Update(gameTime.ElapsedGameTime);
		}
		bool flag = move != 0f;
		if (flag)
		{
			PlayerState state = State;
			bool flag2 = (uint)(state - 1) <= 1u;
			flag = flag2;
		}
		if (flag)
		{
			direction = float.Sign(move);
		}
		Facing.Scale = new Vector2(direction * 1f, 1f);
		if (State != PlayerState.Attacking)
		{
			UpdateAnimation();
		}
	}

	private void UpdateAnimation()
	{
		if (!clipLocked)
		{
			var (name, loop) = State switch
			{
				PlayerState.Airborne => ("Airborne", false),
				PlayerState.Walking => (Gait != PlayerGait.Run) ? ("WalkRight", true) : ("RunRight", true),
				PlayerState.Idle => ("IdleRight", true),
				_ => (string.Empty, true),
			};
			if (!(name == "") && AnimationPlayer != null)
			{
				AnimationPlayer.IsLooping = loop;
				AnimationPlayer.Play(name);
			}
		}
	}

	private PlayerState ResolveState(float move, float verticalVelocity)
	{
		if (AttackComponent.IsBusy)
		{
			return PlayerState.Attacking;
		}
		if (!Controller.IsGrounded || verticalVelocity < 0f)
		{
			return PlayerState.Airborne;
		}
		if (move != 0f)
		{
			return PlayerState.Walking;
		}
		return PlayerState.Idle;
	}

	public void Draw(SpriteBatch spriteBatch)
	{
		if (State == PlayerState.Attacking)
		{
			DrawAttackAnimation(spriteBatch);
			DrawLocalBox(spriteBatch);
			return;
		}
		AnimationFrame currentFrame = AnimationPlayer?.CurrentFrame;
		if (currentFrame != null)
		{
			DrawFrameAtFeet(spriteBatch, currentFrame);
		}
	}

	private void DrawLocalBox(SpriteBatch spriteBatch)
	{
		AttackData2D attack = AttackComponent.CurrentAttack;
		if (attack != null && DebugHitboxTexture != null)
		{
			AnimationFrame frame = GetCurrentFrameForAttack(attack);
			if (frame != null && AttackSpace.TryHitCenterLocal(attack, frame, out var center))
			{
				BoundingBox2D bounds = BoundingBox2D.CreateFromCenterAndExtents(center, AttackSpace.HitHalfExtents(attack));
				spriteBatch.Draw(DebugHitboxTexture, bounds.Min, null, Color.Red, 0f, Vector2.Zero, bounds.Size, SpriteEffects.None, 0f);
			}
		}
	}

	private AnimationFrame? GetCurrentFrameForAttack(AttackData2D attack)
	{
		AnimationChain<AnimationFrame> chain = Animations?.Find((AnimationChain<AnimationFrame> anim) => anim.Name == attack.Animation);
		if (chain == null || chain.Count == 0)
		{
			return null;
		}
		int index = AttackSpace.SpriteFrameIndex(chain, AttackComponent.AttackNormalizedTime);
		return chain[index];
	}

	private void DrawAttackAnimation(SpriteBatch spriteBatch)
	{
		AttackData2D attack = AttackComponent.CurrentAttack;
		if (attack != null)
		{
			AnimationFrame frame = GetCurrentFrameForAttack(attack);
			if (frame != null)
			{
				DrawFrameAtFeet(spriteBatch, frame);
			}
		}
	}

	private void DrawFrameAtFeet(SpriteBatch spriteBatch, AnimationFrame frame)
	{
		if (frame.Texture != null)
		{
			PixelRectangle? sourceRectangle = frame.SourceRectangle;
			if (sourceRectangle.HasValue)
			{
				PixelRectangle pr = sourceRectangle.GetValueOrDefault();
				spriteBatch.Draw(frame.Texture, Vector2.Zero, new Rectangle(pr.X, pr.Y, pr.Width, pr.Height), Color.White, 0f, DrawOriginPx, 1f, SpriteEffects.None, 0f);
			}
		}
	}

	private void ApplyHorizontalMovement(float move, float dt, ref Vector2 velocity)
	{
		if (move != 0f)
		{
			CancelOneShot();
		}
		float speed = ((Gait == PlayerGait.Run) ? Tuning.RunSpeed : Tuning.MoveSpeed);
		float targetVelocity = move * speed;
		float acceleration = ((MathF.Abs(move) > float.Epsilon) ? Tuning.GroundAcceleration : Tuning.GroundDeceleration);
		velocity.X = MoveTowards(velocity.X, targetVelocity, acceleration * dt);
	}
	private void ApplyAirMovement(float move, float dt, ref Vector2 velocity)
	{
		if (move != 0f)
		{
			CancelOneShot();

			bool isBraking = velocity.X != 0f
				&& MathF.Sign(move) != MathF.Sign(velocity.X);

			if (isBraking)
			{
				velocity.X = MoveTowards(
					velocity.X,
					0f,
					Tuning.AirDeceleration * dt);
			}
			else
			{
				float speed = Gait == PlayerGait.Run
					? Tuning.RunSpeed
					: Tuning.MoveSpeed;

				float targetVelocity = move * speed;

				if (MathF.Abs(velocity.X) < MathF.Abs(targetVelocity))
				{
					velocity.X = MoveTowards(
						velocity.X,
						targetVelocity,
						Tuning.AirAcceleration * dt);
				}
			}
		}

		velocity.X = MoveTowards(
			velocity.X,
			0f,
			Tuning.AirResistance * dt);
	}

	private static float MoveTowards(float current, float target, float maximumChange)
	{
		float difference = target - current;
		if (MathF.Abs(difference) <= maximumChange)
		{
			return target;
		}
		return current + (float)MathF.Sign(difference) * maximumChange;
	}

	private void ApplyJumpAndDrop(bool jumpPressed, bool dropPressed, ref Vector2 velocity)
	{
		if (dropPressed && Controller.CurrentFloorIsOneWay)
		{
			Controller.StartPlatformDrop(Tuning.PlatformDropTime);
		}
		else if (jumpPressed && (Controller.IsGrounded || leftGroundTimer > 0f) && velocity.Y >= 0f)
		{
			velocity.Y = GetJumpVelocity();
		}
		CancelOneShot();
	}

	private float GetJumpVelocity()
	{
		float time = MathF.Max(Tuning.TimeToPeak, 0.001f);
		float height = MathF.Max(Tuning.JumpPeakHeight, 0f);
		return 0f - 2f * height / time;
	}

	private float GetRisingGravity()
	{
		float time = MathF.Max(Tuning.TimeToPeak, 0.001f);
		float height = MathF.Max(Tuning.JumpPeakHeight, 0f);
		return 2f * height / (time * time);
	}

	private void UpdateCoyoteTime(float dt)
	{
		if (leftGroundTimer >= 0f)
		{
			leftGroundTimer -= dt;
		}
	}

	private void OnLeftGround()
	{
		leftGroundTimer = Tuning.CoyoteTime;
		CancelOneShot();
	}

	private void OnLanded()
	{
		leftGroundTimer = 0f;
		PlayOneShot("Landed");
	}

	private void PlayOneShot(string name)
	{
		if (AnimationPlayer != null)
		{
			clipLocked = true;
			AnimationPlayer.IsLooping = false;
			AnimationPlayer.Play(name);
			AnimationPlayer.AnimationFinished += OnOneShotFinished;
		}
	}

	private void CancelOneShot()
	{
		clipLocked = false;
		AnimationPlayer<AnimationFrame>? animationPlayer = AnimationPlayer;
		if (animationPlayer != null)
		{
			animationPlayer.AnimationFinished -= OnOneShotFinished;
		}
	}

	private void OnOneShotFinished()
	{
		clipLocked = false;
		AnimationPlayer.AnimationFinished -= OnOneShotFinished;
	}
}
