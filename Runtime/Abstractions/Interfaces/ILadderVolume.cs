namespace P3k.PlayerLadderController.Abstractions.Interfaces
{
   using UnityEngine;

   /// <summary>
   ///    Defines the contract for a ladder volume that provides spatial data
   ///    for mounting, climbing, and dismounting a ladder.
   /// </summary>
   public interface ILadderVolume
   {
      /// <summary>
      ///    A stable, unique identifier for this ladder in the scene,
      ///    suitable for referencing across a network.
      /// </summary>
      int LadderId { get; }

      /// <summary>
      ///    Whether the player is automatically dismounted when reaching the climb height limits.
      /// </summary>
      bool AutoDismountEnabled { get; }

      /// <summary>
      ///    Whether to draw ladder gizmos in the editor.
      /// </summary>
      bool GizmosEnabled { get; }

      /// <summary>
      ///    The <see cref="UnityEngine.BoxCollider" /> that defines this ladder volume.
      /// </summary>
      BoxCollider BoxCollider { get; }

      /// <summary>
      ///    The underlying <see cref="UnityEngine.Collider" /> for this ladder volume.
      /// </summary>
      Collider Collider { get; }

      /// <summary>
      ///    Maximum auto-dismount height in local space, clamped between
      ///    <see cref="LocalMountHeightMax" /> and <see cref="MaxClimbHeight" />.
      /// </summary>
      float LocalAutoDismountHeightMax { get; }

      /// <summary>
      ///    Minimum auto-dismount height in local space, clamped between
      ///    <see cref="MinClimbHeight" /> and <see cref="LocalMountHeightMin" />.
      /// </summary>
      float LocalAutoDismountHeightMin { get; }

      /// <summary>
      ///    Maximum mount height in local space, clamped to the upper extent of the volume.
      /// </summary>
      float LocalMountHeightMax { get; }

      /// <summary>
      ///    Minimum mount height in local space, clamped to the lower extent of the volume.
      /// </summary>
      float LocalMountHeightMin { get; }

      /// <summary>
      ///    Maximum climbable height in local space, inset from the top of the volume.
      /// </summary>
      float MaxClimbHeight { get; }

      /// <summary>
      ///    Minimum climbable height in local space, inset from the bottom of the volume.
      /// </summary>
      float MinClimbHeight { get; }

      /// <summary>
      ///    Distance from the ladder face at which the player can mount.
      /// </summary>
      float MountDistance { get; }

      /// <summary>
      ///    Center of the box collider in local space.
      /// </summary>
      Vector3 LocalCenter { get; }

      /// <summary>
      ///    Half-extents of the box collider in local space.
      /// </summary>
      Vector3 LocalHalfExtents { get; }

      /// <summary>
      ///    The <see cref="UnityEngine.Transform" /> of this ladder volume.
      /// </summary>
      Transform transform { get; }

      /// <summary>
      ///    Transforms a world-space position into the ladder's local space.
      /// </summary>
      /// <param name="worldPosition">The world-space position to transform.</param>
      /// <returns>The position in local space.</returns>
      Vector3 InverseTransformPoint(Vector3 worldPosition);

      /// <summary>
      ///    Computes the local mount height by clamping the world position to the
      ///    nearest valid mount point on the ladder.
      /// </summary>
      /// <param name="worldPosition">World-space position used to find the closest mount point.</param>
      /// <returns>The clamped local mount height.</returns>
      float LocalMountHeight(Vector3 worldPosition);

      /// <summary>
      ///    Transforms a local-space direction into world space.
      /// </summary>
      /// <param name="localDirection">The local-space direction to transform.</param>
      /// <returns>The direction in world space.</returns>
      Vector3 TransformDirection(Vector3 localDirection);

      /// <summary>
      ///    Transforms a local-space position into world space.
      /// </summary>
      /// <param name="localPosition">The local-space position to transform.</param>
      /// <returns>The position in world space.</returns>
      Vector3 TransformPoint(Vector3 localPosition);
   }
}
