namespace P3k.PlayerLadderController.Implementations.Utilities
{
   using P3k.PlayerLadderController.Abstractions.Configs;
   using P3k.PlayerLadderController.Abstractions.Models;

   using System.Linq;

   using UnityEngine;

   internal static class LadderMountObstructionChecker
   {
      private const int MAX_OVERLAP_HITS = 10;

      private static readonly Collider[] OverlapResults = new Collider[MAX_OVERLAP_HITS];

      internal static bool IsObstructed(LadderMountPoint mountPoint, Collider ladderCollider, PlayerLadderConfig config)
      {
         var radius = config.CharacterRadius;
         var height = config.CharacterHeight;
         var mask = config.ObstructionMask;

         var halfExtents = new Vector3(radius, height * 0.5f, radius);

         var targetCenter = mountPoint.Position + (Vector3.up * halfExtents.y)
                            + (mountPoint.FaceNormal * halfExtents.z);

         var overlapCount = Physics.OverlapBoxNonAlloc(
         targetCenter,
         halfExtents,
         OverlapResults,
         Quaternion.LookRotation(mountPoint.FaceNormal),
         mask,
         QueryTriggerInteraction.Ignore);

         for (var i = 0; i < overlapCount; i++)
         {
            if (OverlapResults[i] != ladderCollider)
            {
               return true;
            }
         }

         return false;
      }
   }
}
