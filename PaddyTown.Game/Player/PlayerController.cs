// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using PaddyTown.Core;
using PaddyTown.Physics;
using Stride.Animations;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Events;
using Stride.Particles;
using Stride.Physics;

namespace PaddyTown.Player;

public class PlayerController : SyncScript
{
    public static readonly EventKey<bool> IsAttackingEventKey = new();
    public static readonly EventKey<FacingDirection> DirectionEventKey = new();
    public static readonly EventKey<float> RunSpeedEventKey = new();
    private readonly EventReceiver<bool> jumpEvent = new(PlayerInput.JumpEventKey);
    private readonly EventReceiver<int> moveDestinationEvent = new(PlayerInput.MoveEventKey);

    private CharacterComponent characterComponent;
    private FacingDirection direction = FacingDirection.Right;

    private bool hasDoubleJumped;
    private DateTimeOffset wallJumpStart;
    private DateTimeOffset jumpStart;
    private bool wasPressingJumpPreviously;
    private Simulation simulation;
    private Vector3 baseGravity;
    private float curveLength;
    private float gravityHoldFactor = 0.0f;
    
    private const float JumpForce = 3f;

    [NotNull]
    [DataMember(10)]
    [Display("Gravity Curve")]
    public IComputeCurve<float> Curve { get; set; }


    [Display("Max Velocity X")] public float MaxVelocityX = 10;


    [Display("Run Speed")] public float MoveSpeed = 1;

    public FacingDirection Direction
    {
        get => this.direction;
        set
        {
            this.direction = value;
            DirectionEventKey.Broadcast(this.direction);
        }
    }

    public override void Start()
    {
        this.characterComponent = this.Entity.Get<CharacterComponent>();
        this.simulation = this.GetSimulation();
        this.baseGravity = this.characterComponent.Gravity;
        this.Curve.UpdateChanges();
    }

    private float moveSpeed;

    public override void Update()
    {
        if (this.characterComponent.IsGrounded)
        {
            float moveInput = this.GetHorizontalInput();
            if (moveInput == 0.0f)
            {
                this.moveSpeed *= 0.8f;
            }
            else
            {
                if (MathF.Sign(this.moveSpeed) == MathF.Sign(moveInput))
                {
                    this.moveSpeed += moveInput * this.MoveSpeed * this.simulation.FixedTimeStep;
                }
                else
                {
                    this.moveSpeed *= 0.8f;
                    this.moveSpeed += moveInput * this.MoveSpeed * this.simulation.FixedTimeStep;
                }
            }
        }

        Vector3 velocity = new Vector3(this.moveSpeed, 0, 0);
        velocity.X = MathUtil.Clamp(velocity.X, -this.MaxVelocityX, this.MaxVelocityX);
        
        RunSpeedEventKey.Broadcast(velocity.X);

        this.characterComponent.SetVelocity(velocity);

        // Handle jump input
        if (this.jumpEvent.TryReceive(out bool jump) && jump)
        {
            this.RespondToJumpInput();
        }
        else
        {
            this.wasPressingJumpPreviously = false;
        }

        if (this.characterComponent.IsGrounded)
        {
            this.hasDoubleJumped = false;
            this.characterComponent.Gravity = this.baseGravity;
        }
        else
        {
            float curveGravityProgress = Math.Clamp((float)(DateTimeOffset.UtcNow - this.jumpStart).TotalMilliseconds / 200.0f, 0.0f, 1.0f);
            this.characterComponent.Gravity = this.baseGravity * this.Curve.Evaluate(curveGravityProgress) * (1.0f - gravityHoldFactor);
            gravityHoldFactor *= 0.9f;
        }
    }

    private void RespondToJumpInput()
    {
        if (!this.wasPressingJumpPreviously)
        {
            this.wasPressingJumpPreviously = true;

            if (this.characterComponent.IsGrounded)
            {
                this.characterComponent.Jump(JumpForce * Vector3.UnitY);
                this.jumpStart = DateTimeOffset.UtcNow;
                return;
            }
            
            // if (CollisionTool.Collides(this.simulation, this.characterComponent, this.direction, 1.0f, PhysicsLayer.Jumpable))
            // {
            //     this.jumpStart = DateTimeOffset.UtcNow;
            //     this.wallJumpStart = DateTimeOffset.UtcNow;
            //     return;
            // }
            //
            // if (!this.hasDoubleJumped && !this.characterComponent.IsGrounded)
            // {
            //     this.jumpStart = DateTimeOffset.UtcNow;
            //     this.hasDoubleJumped = true;
            // }
        }
        else
        {
            // If the player is pressing jump again within 200ms of the last jump extend the height of the jump
            if (this.jumpStart != default && (DateTimeOffset.UtcNow - this.wallJumpStart).TotalMilliseconds < 250)
            {
            }
            else if (this.jumpStart != default && (DateTimeOffset.UtcNow - this.jumpStart).TotalMilliseconds < 300)
            {
                this.gravityHoldFactor = 0.80f;
            }
        }
    }

    private float GetHorizontalInput()
    {
        var moveInput = 0f;

        // Check if left or right movement is signaled
        if (this.moveDestinationEvent.TryReceive(out int direction))
        {
            moveInput = direction;
            this.Direction = direction == 1 ? FacingDirection.Right : FacingDirection.Left;
        }

        return moveInput;
    }
}