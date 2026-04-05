namespace P3k.PlayerLadderController.Implementations.DataContainers
{
   using P3k.PlayerLadderController.Abstractions.Enums;
   using P3k.PlayerLadderController.Abstractions.Models;

   using System.Linq;

   using UnityEngine;

   /// <summary>
   ///    Captures the evaluation of a single mount-face candidate, including
   ///    intermediate data useful for gizmo visualisation.
   /// </summary>
   internal readonly struct MountProbeResult
   {
      /// <summary>
      ///    Groups the line-of-sight check data for a mount candidate.
      /// </summary>
      internal readonly struct LineOfSightProbe
      {
         internal Vector3 Origin { get; }

         internal Vector3 Direction { get; }

         internal float Distance { get; }

         internal bool HitSomething { get; }

         internal Vector3 HitPoint { get; }

         internal bool IsBlocked { get; }

         internal LineOfSightProbe(
            Vector3 origin,
            Vector3 direction,
            float distance,
            bool hitSomething,
            Vector3 hitPoint,
            bool isBlocked)
         {
            Origin = origin;
            Direction = direction;
            Distance = distance;
            HitSomething = hitSomething;
            HitPoint = hitPoint;
            IsBlocked = isBlocked;
         }
      }

      internal LadderFace Face { get; }

      internal bool WasCreated { get; }

      internal LadderMountPoint MountPoint { get; }

      internal bool IsObstructed { get; }

      internal LineOfSightProbe LineOfSight { get; }

      internal bool IsValid => WasCreated && !IsObstructed && !LineOfSight.IsBlocked;

      internal MountProbeResult(
         LadderFace face,
         bool wasCreated,
         LadderMountPoint mountPoint,
         bool isObstructed,
         LineOfSightProbe lineOfSight)
      {
         Face = face;
         WasCreated = wasCreated;
         MountPoint = mountPoint;
         IsObstructed = isObstructed;
         LineOfSight = lineOfSight;
      }

      /// <summary>
      ///    Creates a result for a face that could not produce a mount point.
      /// </summary>
      internal static MountProbeResult NotCreated(LadderFace face)
      {
         return new MountProbeResult(face, false, default, false, default);
      }

      /// <summary>
      ///    Creates a result for a face whose mount point is obstructed.
      /// </summary>
      internal static MountProbeResult Obstructed(LadderFace face, LadderMountPoint mountPoint)
      {
         return new MountProbeResult(face, true, mountPoint, true, default);
      }
   }
}
