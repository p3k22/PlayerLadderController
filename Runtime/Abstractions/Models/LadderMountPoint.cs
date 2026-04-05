namespace P3k.PlayerLadderController.Abstractions.Models
{
   using System.Linq;

   using UnityEngine;

   public readonly struct LadderMountPoint
   {
      public Vector3 Position { get; }

      public Quaternion Rotation { get; }

      public Vector3 FaceNormal { get; }

      public LadderMountPoint(Vector3 position, Quaternion rotation, Vector3 faceNormal)
      {
         Position = position;
         Rotation = rotation;
         FaceNormal = faceNormal;
      }
   }
}
