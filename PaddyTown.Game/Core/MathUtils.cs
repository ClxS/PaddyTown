using System;
using BulletSharp;

namespace PaddyTown.Core;

public class MathUtils
{
    public static float DegreesToRadians(float degrees)
    {
        return degrees * (float)Math.PI / 180f;
    }
}