using System.Collections.Generic;
using PaddyTown.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;

namespace PaddyTown.Physics;

public static class CollisionTool
{
    public static bool Collides(
        Simulation simulation, 
        CharacterComponent character, 
        FacingDirection facingDirection,
        float depth, 
        CollisionFilterGroups filterGroup = CollisionFilterGroups.DefaultFilter)
    {
        var physicsComponent = character.Entity.Get<PhysicsComponent>(); // Get the PhysicsComponent of the character's entity

        // Assuming you have a way to determine the character's facing direction
        bool isFacingRight = facingDirection == FacingDirection.Right;

        // Get the character's position
        Vector3 characterPosition = character.Entity.Transform.WorldMatrix.TranslationVector;

        // Define the direction vector based on the facing direction
        Vector3 direction = isFacingRight ? Vector3.UnitX : -Vector3.UnitX;

        var raycastStart = character.Entity.Transform.Position;
        var raycastEnd = character.Entity.Transform.Position + direction * depth;

        var distance = Vector3.Distance(raycastStart, raycastEnd);

        var hitResults = new List<HitResult>();
        simulation.RaycastPenetrating(raycastStart, raycastEnd, hitResults, filterGroup);

        if (hitResults.Count > 0)
        {
            foreach (var hitResult in hitResults)
            {
            }
        }
        
        return hitResults.Count > 0;
    }
}