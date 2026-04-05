namespace P3k.PlayerLadderController.Implementations.Utilities
{
   using P3k.PlayerLadderController.Abstractions.Enums;
   using P3k.PlayerLadderController.Abstractions.Interfaces;
   using P3k.PlayerLadderController.Abstractions.Models;

   using System.Linq;

   using UnityEngine;

   internal static class LadderMountPointFactory
   {
      internal static bool TryCreate(
         ILadderVolume ladder,
         LadderFace face,
         float localMountHeight,
         out LadderMountPoint mountPoint)
      {
         mountPoint = default;

         if (face != LadderFace.Front && face != LadderFace.Back)
         {
            return false;
         }

         var localNormal = face == LadderFace.Front ? Vector3.forward : Vector3.back;

         var localPos = ladder.LocalCenter + new Vector3(
                        0f,
                        localMountHeight,
                        localNormal.z * (ladder.LocalHalfExtents.z + ladder.MountDistance));

         var worldPos = ladder.TransformPoint(localPos);

         var worldNormal = ladder.TransformDirection(localNormal);

         var rotation = Quaternion.LookRotation(-worldNormal, Vector3.up);

         mountPoint = new LadderMountPoint(worldPos, rotation, worldNormal);

         return true;
      }
   }
}
