// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using PaddyTown.Core;
using Stride.Animations;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Events;
using Stride.Games;

namespace PaddyTown.Player;

public class AnimationController : SyncScript, IBlendTreeBuilder
{
    private readonly EventReceiver<bool> attackEvent = new(PlayerController.IsAttackingEventKey);

    // Internal state
    private readonly EventReceiver<float> runSpeedEvent = new(PlayerController.RunSpeedEventKey);
    private readonly EventReceiver<int> directionEvent = new(PlayerController.DirectionEventKey);
    private AnimationClip animationClipWalkLerp1;
    private AnimationClip animationClipWalkLerp2;

    private AnimationClipEvaluator animEvaluatorIdle;
    private AnimationClipEvaluator animEvaluatorJump;
    private AnimationClipEvaluator animEvaluatorRun;
    private AnimationClipEvaluator animEvaluatorWalk;

    // Idle-Walk-Run lerp
    private AnimationClipEvaluator animEvaluatorWalkLerp1;
    private AnimationClipEvaluator animEvaluatorWalkLerp2;
    private double currentTime;

    private float runVelocity;
    private AnimationState state = AnimationState.Walking;
    private float walkLerpFactor = 0.5f;

    [Display("Animation Component")] public AnimationComponent AnimationComponent { get; set; }

    [Display("Idle")] public AnimationClip AnimationIdle { get; set; }

    [Display("Walk")] public AnimationClip AnimationWalk { get; set; }

    [Display("Run")] public AnimationClip AnimationRun { get; set; }

    [Display("Jump")] public AnimationClip AnimationJump { get; set; }

    [DataMemberRange(0, 1, 0.01, 0.1, 3)]
    [Display("Walk Threshold")]
    public float WalkThreshold { get; set; } = 0.25f;

    [Display("Time Scale")] public double TimeFactor { get; set; } = 1;

    /// <summary>
    ///     BuildBlendTree is called every frame from the animation system when the <see cref="AnimationComponent" /> needs to
    ///     be evaluated
    ///     It overrides the default behavior of the <see cref="AnimationComponent" /> by setting a custom blend tree
    /// </summary>
    /// <param name="blendStack">The stack of animation operations to be blended</param>
    public void BuildBlendTree(FastList<AnimationOperation> blendStack)
    {
        switch (this.state)
        {
            case AnimationState.Walking:
            {
                // Note! The tree is laid out as a stack and has to be flattened before returning it to the animation system!
                blendStack.Add(AnimationOperation.NewPush(this.animEvaluatorWalkLerp1,
                    TimeSpan.FromTicks((long)(this.currentTime * this.animationClipWalkLerp1.Duration.Ticks))));
                blendStack.Add(AnimationOperation.NewPush(this.animEvaluatorWalkLerp2,
                    TimeSpan.FromTicks((long)(this.currentTime * this.animationClipWalkLerp2.Duration.Ticks))));
                blendStack.Add(AnimationOperation.NewBlend(CoreAnimationOperation.Blend, this.walkLerpFactor));
            }
            break;
        }
    }

    public override void Start()
    {
        base.Start();

        if (this.AnimationComponent == null)
        {
            throw new InvalidOperationException("The animation component is not set");
        }

        if (this.AnimationIdle == null)
        {
            throw new InvalidOperationException("Idle animation is not set");
        }

        if (this.AnimationWalk == null)
        {
            throw new InvalidOperationException("Walking animation is not set");
        }

        if (this.AnimationRun == null)
        {
            throw new InvalidOperationException("Running animation is not set");
        }

        if (this.AnimationJump == null)
        {
            throw new InvalidOperationException("Punching animation is not set");
        }

        // By setting a custom blend tree builder we can override the default behavior of the animation system
        //  Instead, BuildBlendTree(FastList<AnimationOperation> blendStack) will be called each frame
        this.AnimationComponent.BlendTreeBuilder = this;

        AnimationClip f;
        this.animEvaluatorIdle = this.AnimationComponent.Blender.CreateEvaluator(this.AnimationIdle);
        this.animEvaluatorWalk = this.AnimationComponent.Blender.CreateEvaluator(this.AnimationWalk);
        this.animEvaluatorRun = this.AnimationComponent.Blender.CreateEvaluator(this.AnimationRun);
        this.animEvaluatorJump = this.AnimationComponent.Blender.CreateEvaluator(this.AnimationJump);

        // Initial walk lerp
        this.walkLerpFactor = 0;
        this.animEvaluatorWalkLerp1 = this.animEvaluatorIdle;
        this.animEvaluatorWalkLerp2 = this.animEvaluatorWalk;
        this.animationClipWalkLerp1 = this.AnimationIdle;
        this.animationClipWalkLerp2 = this.AnimationWalk;
    }

    public override void Cancel()
    {
        this.AnimationComponent.Blender.ReleaseEvaluator(this.animEvaluatorIdle);
        this.AnimationComponent.Blender.ReleaseEvaluator(this.animEvaluatorWalk);
        this.AnimationComponent.Blender.ReleaseEvaluator(this.animEvaluatorRun);
        this.AnimationComponent.Blender.ReleaseEvaluator(this.animEvaluatorJump);
    }

    private void UpdateWalking()
    {
        float speed = Math.Abs(this.runVelocity);

        if (directionEvent.TryReceive(out int direction))
        {
            this.AnimationComponent.Entity.Transform.RotationEulerXYZ = direction switch
            {
                1 => new Vector3(0.0f, MathUtils.DegreesToRadians(90.0f), 0.0f),
                -1 => new Vector3(0.0f, MathUtils.DegreesToRadians(-90.0f), 0.0f),
                _ => this.AnimationComponent.Entity.Transform.RotationEulerXYZ,
            };
        }

        if (speed < this.WalkThreshold)
        {
            this.walkLerpFactor = speed / this.WalkThreshold;
            this.walkLerpFactor =
                (float)Math.Sqrt(this
                    .walkLerpFactor); // Idle-Walk blend looks really werid, so skew the factor towards walking
            this.animEvaluatorWalkLerp1 = this.animEvaluatorIdle;
            this.animEvaluatorWalkLerp2 = this.animEvaluatorWalk;
            this.animationClipWalkLerp1 = this.AnimationWalk;
            this.animationClipWalkLerp2 = this.AnimationWalk;
        }
        else
        {
            this.walkLerpFactor = (speed - this.WalkThreshold) / (1.0f - this.WalkThreshold);
            this.animEvaluatorWalkLerp1 = this.animEvaluatorRun;
            this.animEvaluatorWalkLerp2 = this.animEvaluatorRun;
            this.animationClipWalkLerp1 = this.AnimationRun;
            this.animationClipWalkLerp2 = this.AnimationRun;
        }

        // Use DrawTime rather than UpdateTime
        GameTime time = this.Game.DrawTime;
        // This update function will account for animation with different durations, keeping a current time relative to the blended maximum duration
        long blendedMaxDuration = 0;
        blendedMaxDuration =
            (long)MathUtil.Lerp(this.animationClipWalkLerp1.Duration.Ticks, this.animationClipWalkLerp2.Duration.Ticks,
                this.walkLerpFactor);

        TimeSpan currentTicks = TimeSpan.FromTicks((long)(this.currentTime * blendedMaxDuration));

        currentTicks = blendedMaxDuration == 0
            ? TimeSpan.Zero
            : TimeSpan.FromTicks((currentTicks.Ticks + (long)(time.Elapsed.Ticks * this.TimeFactor)) %
                                 blendedMaxDuration);

        this.currentTime = currentTicks.Ticks / (double)blendedMaxDuration;
    }

    public override void Update()
    {
        // State control
        this.runSpeedEvent.TryReceive(out this.runVelocity);
        switch (this.state)
        {
            case AnimationState.Walking:
                this.UpdateWalking();
                break;
        }
    }

    private enum AnimationState
    {
        Walking,
        Jumping,
    }
}