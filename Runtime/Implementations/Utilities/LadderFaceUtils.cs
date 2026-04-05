namespace P3k.PlayerLadderController.Implementations.Utilities
{
   using P3k.PlayerLadderController.Abstractions.Enums;
   using P3k.PlayerLadderController.Abstractions.Interfaces;

   using System.Linq;

   using UnityEngine;

   internal static class LadderFaceUtils
   {
      /// <summary>
      ///    Gets the closest ladder face to a world position, preferring climbable faces.
      /// </summary>
      /// <param name="ladder">The ladder volume to evaluate.</param>
      /// <param name="worldPosition">The world position to test.</param>
      /// <returns>The closest ladder face.</returns>
      internal static LadderFace GetClosestFace(ILadderVolume ladder, Vector3 worldPosition)
      {
         var approach = GetApproachFace(ladder, worldPosition);

         return approach is LadderFace.Front or LadderFace.Back ?
                   approach :
                   GetClosestClimbableFace(ladder, worldPosition);
      }

      /// <summary>
      ///    Gets the opposite face for a given ladder face.
      /// </summary>
      /// <param name="face">The face to invert.</param>
      /// <returns>The opposite face, or <see cref="LadderFace.None" /> if unknown.</returns>
      internal static LadderFace GetOppositeFace(LadderFace face)
      {
         return face switch
            {
               LadderFace.Front => LadderFace.Back,
               LadderFace.Back => LadderFace.Front,
               LadderFace.Left => LadderFace.Right,
               LadderFace.Right => LadderFace.Left,
               LadderFace.Top => LadderFace.Bottom,
               LadderFace.Bottom => LadderFace.Top,
               _ => LadderFace.None
            };
      }

      /// <summary>
      ///    Classifies a local position to the dominant axis face of the ladder volume.
      /// </summary>
      /// <param name="localPos">The point in ladder-local space.</param>
      /// <param name="center">The ladder-local center.</param>
      /// <param name="halfExtents">The half extents of the ladder volume.</param>
      /// <returns>The face that is most aligned with the position.</returns>
      private static LadderFace Classify(Vector3 localPos, Vector3 center, Vector3 halfExtents)
      {
         var delta = localPos - center;

         // Normalize by half extents to compare axis dominance.
         var nx = Mathf.Abs(delta.x) / halfExtents.x;
         var ny = Mathf.Abs(delta.y) / halfExtents.y;
         var nz = Mathf.Abs(delta.z) / halfExtents.z;

         if (nx >= ny && nx >= nz)
         {
            return delta.x > 0 ? LadderFace.Right : LadderFace.Left;
         }

         if (ny >= nx && ny >= nz)
         {
            return delta.y > 0 ? LadderFace.Top : LadderFace.Bottom;
         }

         return delta.z > 0 ? LadderFace.Front : LadderFace.Back;
      }

      /// <summary>
      ///    Gets the face the position approaches based on ladder-local space.
      /// </summary>
      /// <param name="ladder">The ladder volume to evaluate.</param>
      /// <param name="worldPosition">The world position to test.</param>
      /// <returns>The face most aligned with the approach direction.</returns>
      private static LadderFace GetApproachFace(ILadderVolume ladder, Vector3 worldPosition)
      {
         var local = ladder.InverseTransformPoint(worldPosition);

         return Classify(local, ladder.LocalCenter, ladder.LocalHalfExtents);
      }

      /// <summary>
      ///    Gets the closest climbable face, restricted to front or back.
      /// </summary>
      /// <param name="ladder">The ladder volume to evaluate.</param>
      /// <param name="worldPosition">The world position to test.</param>
      /// <returns>The closest climbable face.</returns>
      private static LadderFace GetClosestClimbableFace(ILadderVolume ladder, Vector3 worldPosition)
      {
         var local = ladder.InverseTransformPoint(worldPosition);

         // Climbable faces are evaluated along the local Z axis.
         return local.z - ladder.LocalCenter.z >= 0 ? LadderFace.Front : LadderFace.Back;
      }
   }
}
