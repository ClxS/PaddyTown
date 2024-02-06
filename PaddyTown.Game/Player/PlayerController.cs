// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Events;
using Stride.Physics;

namespace PaddyTown.Player;

public class PlayerController : SyncScript
{
    // The character controller does only two things - moves the character and makes it attack close targets
    //  If the character is too far from its target it will run after it until it's close enough then halt movement and attack
    //  If the character is walking towards a specific location instead it will run to it then halt movement when close enough

    private readonly EventReceiver<int> moveDestinationEvent = new EventReceiver<int>(PlayerInput.MoveEventKey);
    private readonly EventReceiver<bool> jumpEvent = new EventReceiver<bool>(PlayerInput.JumpEventKey);

    public static readonly EventKey<bool> IsAttackingEventKey = new EventKey<bool>();
    public static readonly EventKey<int> DirectionEventKey = new EventKey<int>();
    public static readonly EventKey<float> RunSpeedEventKey = new EventKey<float>();

    private CharacterComponent characterComponent;

    private float fadingImpulse = 0.0f;
    private bool wasPressingJumpPreviously = false;
    private bool hasDoubleJumped = false;

    private DateTimeOffset jumpStart;

    [Display("Jump Force")] public float JumpForce = 10f;

    [Display("Max Velocity X")] public float MaxVelocityX = 10;


    [Display("Run Speed")] public float MoveSpeed = 1;

    public override void Start()
    {
        this.characterComponent = this.Entity.Get<CharacterComponent>();
    }

    public override void Update()
    {
        float moveInput = this.GetHorizontalInput();
        Vector3 velocity = new(moveInput * this.MoveSpeed, 0, 0);
        
        RunSpeedEventKey.Broadcast(velocity.X);
        
        velocity.X = MathUtil.Clamp(velocity.X, -this.MaxVelocityX, this.MaxVelocityX);
        this.characterComponent.SetVelocity(velocity);

        // Handle jump input
        if (this.jumpEvent.TryReceive(out bool jump) && jump)
        {
            if (this.characterComponent.IsGrounded || (!this.wasPressingJumpPreviously && !this.hasDoubleJumped))
            {
                Jump();
                jumpStart = DateTimeOffset.UtcNow;
            }
            else if ((jumpStart != default && (DateTimeOffset.UtcNow - jumpStart).TotalMilliseconds < 200))
            {
                Jump(0.2f);
            }

            this.wasPressingJumpPreviously = true;
        }
        else
        {
            this.wasPressingJumpPreviously = false;   
        }

        if (this.fadingImpulse > 0.1f)
        {
            this.characterComponent.Jump(fadingImpulse * Vector3.UnitY);
            fadingImpulse *= 0.65f;
        }
    
        if (this.characterComponent.IsGrounded)
        {
            this.hasDoubleJumped = false;
        }
    }

    private float GetHorizontalInput()
    {
        var moveInput = 0f;

        // Check if left or right movement is signaled
        if (this.moveDestinationEvent.TryReceive(out int direction))
        {
            moveInput = direction;
            DirectionEventKey.Broadcast(direction);
        }

        return moveInput;
    }

    private void Jump(float multiplier = 1.0f)
    {
        fadingImpulse += this.JumpForce * multiplier;
    }
}