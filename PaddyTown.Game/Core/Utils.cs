// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Physics;

namespace PaddyTown.Core;

public static class Utils
{
    public static void SpawnPrefabModel(this ScriptComponent script, Prefab source, Entity attachEntity,
        Matrix localMatrix, Vector3 forceImpulse)
    {
        if (source == null)
        {
            return;
        }

        // Clone
        List<Entity> spawnedEntities = source.Instantiate();

        // Add
        foreach (Entity prefabEntity in spawnedEntities)
        {
            prefabEntity.Transform.UpdateLocalMatrix();
            Matrix entityMatrix = prefabEntity.Transform.LocalMatrix * localMatrix;
            entityMatrix.Decompose(out prefabEntity.Transform.Scale, out prefabEntity.Transform.Rotation,
                out prefabEntity.Transform.Position);

            if (attachEntity != null)
            {
                attachEntity.AddChild(prefabEntity);
            }
            else
            {
                script.SceneSystem.SceneInstance.RootScene.Entities.Add(prefabEntity);
            }

            RigidbodyComponent physComp = prefabEntity.Get<RigidbodyComponent>();
            if (physComp != null)
            {
                physComp.ApplyImpulse(forceImpulse);
            }
        }
    }

    public static void SpawnPrefabInstance(this ScriptComponent script, Prefab source, Entity attachEntity,
        float timeout, Matrix localMatrix)
    {
        if (source == null)
        {
            return;
        }

        Func<Task> spawnTask = async () =>
        {
            // Clone
            List<Entity> spawnedEntities = source.Instantiate();

            // Add
            foreach (Entity prefabEntity in spawnedEntities)
            {
                prefabEntity.Transform.UpdateLocalMatrix();
                Matrix entityMatrix = prefabEntity.Transform.LocalMatrix * localMatrix;
                entityMatrix.Decompose(out prefabEntity.Transform.Scale, out prefabEntity.Transform.Rotation,
                    out prefabEntity.Transform.Position);

                if (attachEntity != null)
                {
                    attachEntity.AddChild(prefabEntity);
                }
                else
                {
                    script.SceneSystem.SceneInstance.RootScene.Entities.Add(prefabEntity);
                }
            }

            // Countdown
            float secondsCountdown = timeout;
            while (secondsCountdown > 0f)
            {
                await script.Script.NextFrame();
                secondsCountdown -= (float)script.Game.UpdateTime.Elapsed.TotalSeconds;
            }

            // Remove
            foreach (Entity clonedEntity in spawnedEntities)
            {
                if (attachEntity != null)
                {
                    attachEntity.RemoveChild(clonedEntity);
                }
                else
                {
                    script.SceneSystem.SceneInstance.RootScene.Entities.Remove(clonedEntity);
                }
            }

            // Cleanup
            spawnedEntities.Clear();
        };

        script.Script.AddTask(spawnTask);
    }

    /// <summary>
    ///     Removes an entity, together with its children, from the Game's scene graph
    /// </summary>
    /// <param name="game">The game instance containing the entity</param>
    /// <param name="entity">Entity to remove</param>
    public static void RemoveEntity(this IGame game, Entity entity)
    {
        Entity parent = entity.GetParent();
        if (parent != null)
        {
            parent.RemoveChild(entity);
            return;
        }

        ((Game)game).SceneSystem.SceneInstance.RootScene.Entities.Remove(entity);
    }

    public static async Task WaitTime(this IGame game, TimeSpan time)
    {
        Game g = (Game)game;
        TimeSpan goal = game.UpdateTime.Total + time;
        while (game.UpdateTime.Total < goal)
        {
            await g.Script.NextFrame();
        }
    }

    public static Vector3 LogicDirectionToWorldDirection(Vector2 logicDirection, CameraComponent camera,
        Vector3 upVector)
    {
        camera.Update();
        Matrix inverseView = Matrix.Invert(camera.ViewMatrix);

        Vector3 forward = Vector3.Cross(upVector, inverseView.Right);
        forward.Normalize();

        Vector3 right = Vector3.Cross(forward, upVector);
        Vector3 worldDirection = forward * logicDirection.Y + right * logicDirection.X;
        worldDirection.Normalize();
        return worldDirection;
    }

    public static bool ScreenPositionToWorldPositionRaycast(Vector2 screenPos, CameraComponent camera,
        Simulation simulation, out ClickResult clickResult)
    {
        Matrix invViewProj = Matrix.Invert(camera.ViewProjectionMatrix);

        Vector3 sPos;
        sPos.X = screenPos.X * 2f - 1f;
        sPos.Y = 1f - screenPos.Y * 2f;

        sPos.Z = 0f;
        Vector4 vectorNear = Vector3.Transform(sPos, invViewProj);
        vectorNear /= vectorNear.W;

        sPos.Z = 1f;
        Vector4 vectorFar = Vector3.Transform(sPos, invViewProj);
        vectorFar /= vectorFar.W;

        clickResult.ClickedEntity = null;
        clickResult.WorldPosition = Vector3.Zero;
        clickResult.Type = ClickType.Empty;
        clickResult.HitResult = new HitResult();

        float minDistance = float.PositiveInfinity;

        var result = new FastList<HitResult>();
        simulation.RaycastPenetrating(vectorNear.XYZ(), vectorFar.XYZ(), result, hitTriggers: true);
        foreach (HitResult hitResult in result)
        {
            ClickType type = ClickType.Empty;

            StaticColliderComponent staticBody = hitResult.Collider as StaticColliderComponent;
            if (staticBody != null)
            {
                if (staticBody.CollisionGroup == CollisionFilterGroups.CustomFilter1)
                {
                    type = ClickType.Ground;
                }

                if (staticBody.CollisionGroup == CollisionFilterGroups.CustomFilter2)
                {
                    type = ClickType.LootCrate;
                }

                if (type != ClickType.Empty)
                {
                    float distance = (vectorNear.XYZ() - hitResult.Point).LengthSquared();
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        clickResult.Type = type;
                        clickResult.HitResult = hitResult;
                        clickResult.WorldPosition = hitResult.Point;
                        clickResult.ClickedEntity = hitResult.Collider.Entity;
                    }
                }
            }
        }

        return clickResult.Type != ClickType.Empty;
    }
}