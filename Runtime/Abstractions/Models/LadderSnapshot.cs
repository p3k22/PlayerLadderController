namespace P3k.PlayerLadderController.Abstractions.Models
{
   using UnityEngine;

   /// <summary>
   ///    Immutable snapshot of a player's ladder state, suitable for
   ///    network synchronisation and reconciliation.
   /// </summary>
   public readonly struct LadderSnapshot
   {
      /// <summary>
      ///    The <see cref="Abstractions.Interfaces.ILadderVolume.LadderId" /> of the
      ///    ladder the player is on, or 0 if not mounted.
      /// </summary>
      public readonly int LadderId;

      /// <summary>
      ///    The mount point used when the player mounted the ladder.
      /// </summary>
      public readonly LadderMountPoint MountPoint;

      /// <summary>
      ///    Normalised 0–1 progress along the ladder rail.
      /// </summary>
      public readonly float Progress;

      /// <summary>
      ///    World-space position of the player on the ladder.
      /// </summary>
      public readonly Vector3 Position;

      /// <summary>
      ///    Whether the player is currently mounted on a ladder.
      /// </summary>
      public readonly bool IsMounted;

      /// <summary>
      ///    Whether the player is in a mount or dismount transition.
      /// </summary>
      public readonly bool IsTransitioning;

      /// <summary>
      ///    Whether the player is currently sprinting on the ladder.
      /// </summary>
      public readonly bool IsSprinting;

      /// <summary>
      ///    Creates a new <see cref="LadderSnapshot" />.
      /// </summary>
      /// <param name="ladderId">Stable ladder identifier.</param>
      /// <param name="mountPoint">Mount point used when mounting.</param>
      /// <param name="progress">Normalised 0–1 climb progress.</param>
      /// <param name="position">World-space position on the ladder.</param>
      /// <param name="isMounted">Whether the player is mounted.</param>
      /// <param name="isTransitioning">Whether a transition is active.</param>
      /// <param name="isSprinting">Whether the player is sprinting.</param>
      public LadderSnapshot(
         int ladderId,
         LadderMountPoint mountPoint,
         float progress,
         Vector3 position,
         bool isMounted,
         bool isTransitioning,
         bool isSprinting)
      {
         LadderId = ladderId;
         MountPoint = mountPoint;
         Progress = progress;
         Position = position;
         IsMounted = isMounted;
         IsTransitioning = isTransitioning;
         IsSprinting = isSprinting;
      }
   }
}
