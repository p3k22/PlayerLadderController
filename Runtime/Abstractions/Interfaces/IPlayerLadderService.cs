namespace P3k.PlayerLadderController.Abstractions.Interfaces
{
   using P3k.PlayerLadderController.Abstractions.Models;

   using System;
   using System.Linq;

   using UnityEngine;

   /// <summary>
   ///    Defines the contract for a player ladder service that manages
   ///    mounting, climbing, dismounting, and state synchronisation on ladders.
   /// </summary>
   public interface IPlayerLadderService
   {
      /// <summary>
      ///    Whether the player is currently mounted on a ladder.
      /// </summary>
      bool IsMounted { get; }

      /// <summary>
      ///    Whether the player is currently sprinting on the ladder.
      /// </summary>
      bool IsSprinting { get; }

      /// <summary>
      ///    Whether the player is currently in a mount or dismount transition.
      /// </summary>
      bool IsTransitioning { get; }

      /// <summary>
      ///    Normalised 0–1 progress along the mounted ladder's rail.
      ///    Returns 0 if not mounted.
      /// </summary>
      float NormalizedProgress { get; }

      /// <summary>
      ///    The ladder volume the player is currently mounted on, or <c>null</c>.
      /// </summary>
      ILadderVolume CurrentLadder { get; }

      /// <summary>
      ///    The mount point used on the current ladder.
      /// </summary>
      LadderMountPoint CurrentMountPoint { get; }

      /// <summary>
      ///    World-space position of the player on the current ladder.
      ///    Returns <see cref="Vector3.zero" /> if not mounted.
      /// </summary>
      Vector3 LadderPosition { get; }

      /// <summary>
      ///    Fired when a dismount transition begins.
      ///    Parameters are the current position/rotation of the character,
      ///    followed by the target landing position/rotation.
      /// </summary>
      event Action<Vector3, Quaternion, Vector3, Quaternion> DismountStarted;

      /// <summary>
      ///    Fired when the player has fully finished using the ladder.
      /// </summary>
      event Action FinishUsingLadder;

      /// <summary>
      ///    Fired when a mount transition begins.
      /// </summary>
      event Action<ILadderVolume, LadderMountPoint> MountStarted;

      /// <summary>
      ///    Adjusts the vertical input based on camera orientation relative to
      ///    the character's forward direction.
      /// </summary>
      /// <param name="verticalInput">Raw vertical input.</param>
      /// <param name="cameraForwardY">Camera forward vector's Y component.</param>
      /// <param name="characterForwardY">Character forward vector's Y component.</param>
      /// <returns>The adjusted vertical input value.</returns>
      static float AdjustInputForCamera(float verticalInput, float cameraForwardY, float characterForwardY)
      {
         if (cameraForwardY < characterForwardY - 0.8f)
         {
            return -verticalInput;
         }

         return verticalInput;
      }

      /// <summary>
      ///    Returns whether an automatic dismount would succeed at the current state.
      /// </summary>
      /// <returns><c>true</c> if auto-dismount conditions are met.</returns>
      bool CanTryAutoDismount();

      /// <summary>
      ///    Initiates a dismount with automatic detection of the landing position.
      /// </summary>
      void Dismount();

      /// <summary>
      ///    Initiates a dismount transition to the specified target position and
      ///    rotation, bypassing landing-point detection. Intended for
      ///    server-authoritative dismounting where the landing has already been
      ///    validated.
      /// </summary>
      /// <param name="position">Target world-space landing position.</param>
      /// <param name="rotation">Target world-space landing rotation.</param>
      void Dismount(Vector3 position, Quaternion rotation);

      /// <summary>
      ///    Immediately dismounts the player without a transition.
      /// </summary>
      void ForceDismount();

      /// <summary>
      ///    Mounts the player onto the specified ladder at the given mount point
      ///    without performing any proximity or raycast detection. Intended for
      ///    server-authoritative mounting where validation has already occurred.
      /// </summary>
      /// <param name="ladder">The ladder to mount onto.</param>
      /// <param name="mountPoint">The mount point to use.</param>
      /// <returns><c>true</c> if the mount succeeded; <c>false</c> if <paramref name="ladder" /> is null.</returns>
      bool ForceMount(ILadderVolume ladder, LadderMountPoint mountPoint);

      /// <summary>
      ///    Returns a <see cref="LadderSnapshot" /> populated from the current ladder
      ///    state. If the player is not mounted, returns a default snapshot with
      ///    <see cref="LadderSnapshot.IsMounted" /> set to <c>false</c>.
      /// </summary>
      /// <returns>A snapshot of the current ladder state.</returns>
      LadderSnapshot GetSnapshot();

      /// <summary>
      ///    Moves the player along the ladder based on vertical input.
      /// </summary>
      /// <param name="verticalInput">Vertical input value.</param>
      /// <param name="dt">Delta time for this frame.</param>
      /// <param name="isSprinting">Whether the player is sprinting.</param>
      /// <param name="cameraForwardY">Optional camera forward Y for input adjustment.</param>
      void Move(float verticalInput, float dt, bool isSprinting = false, float? cameraForwardY = null);

      /// <summary>
      ///    Force-sets the internal ladder state to match the supplied snapshot.
      ///    Used for silent network reconciliation — does not fire
      ///    <see cref="MountStarted" />, <see cref="DismountStarted" />, or
      ///    <see cref="FinishUsingLadder" /> events.
      /// </summary>
      /// <param name="snapshot">The snapshot to restore from.</param>
      /// <param name="ladder">
      ///    Optional ladder reference. Required when restoring a mounted state and
      ///    the current ladder is <c>null</c> or does not match the
      ///    snapshot's <see cref="LadderSnapshot.LadderId" />.
      /// </param>
      void RestoreSnapshot(LadderSnapshot snapshot, ILadderVolume ladder = null);

      /// <summary>
      ///    Sets the detection distance used when probing for nearby ladders.
      /// </summary>
      /// <param name="distance">The new probe range.</param>
      void SetLadderDetectionDistance(float distance);

      /// <summary>
      ///    Ticks mount and dismount transition animators.
      /// </summary>
      /// <param name="dt">Delta time for this frame.</param>
      /// <param name="allowAutoDismount">Whether auto-dismount checks are performed.</param>
      void TickMountingAnimators(float dt, bool allowAutoDismount = true);

      /// <summary>
      ///    Attempts to mount the nearest ladder from the given head position and rotation.
      /// </summary>
      /// <param name="playerHeadPosition">World-space head position.</param>
      /// <param name="playerHeadRotation">World-space head rotation.</param>
      /// <returns><c>true</c> if mounting succeeded.</returns>
      bool TryMount(Vector3 playerHeadPosition, Quaternion playerHeadRotation);
   }
}
