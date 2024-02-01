// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;
using Stride.Engine.Events;
using Stride.Input;

namespace PaddyTown.Player;

public class PlayerInput : SyncScript
{
    public static readonly EventKey<int> MoveEventKey = new();
    public static readonly EventKey<bool> JumpEventKey = new();

    public override void Start()
    {
    }

    public override void Update()
    {
        if (!this.Input.HasKeyboard)
        {
            return;
        }

        if (this.Input.IsKeyDown(Keys.Right) || this.Input.IsKeyDown(Keys.D))
        {
            MoveEventKey.Broadcast(1);
        }
        else if (this.Input.IsKeyDown(Keys.Left) || this.Input.IsKeyDown(Keys.A))
        {
            MoveEventKey.Broadcast(-1);
        }
        
        if (this.Input.IsKeyDown(Keys.Space))
        {
            JumpEventKey.Broadcast(true);
        }
    }
}