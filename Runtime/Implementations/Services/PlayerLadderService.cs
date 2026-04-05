namespace P3k.PlayerLadderController.Implementations.Services
{
   using P3k.PlayerLadderController.Abstractions.Configs;
   using P3k.PlayerLadderController.Abstractions.Interfaces;
   using P3k.PlayerLadderController.Abstractions.Models;
   using P3k.PlayerLadderController.Implementations.Modules;

   using System;
   using System.Linq;

   using UnityEngine;

   public sealed class PlayerLadderService : IPlayerLadderService
   {
      private readonly LadderClimbModule _climbModule;

      private readonly LadderMountService _mountService;

      private readonly Transform _characterTransform;

      public bool IsSprinting { get; private set; }

      public bool IsMounted => _mountService is { IsMounted: true };

      public bool IsTransitioning => _mountService is { IsTransitioning: true };

      public ILadderVolume CurrentLadder => _mountService?.CurrentLadder;

      public LadderMountPoint CurrentMountPoint => _mountService?.CurrentMountPoint ?? default;

      /// <summary>
      ///    Normalised 0–1 progress along the mounted ladder's rail.
      ///    Returns 0 if not mounted.
      /// </summary>
      public float NormalizedProgress => _climbModule?.NormalizedProgress ?? 0f;

      /// <summary>
      ///    World-space position of the player on the current ladder.
      ///    Returns <see cref="Vector3.zero" /> if not mounted.
      /// </summary>
      public Vector3 LadderPosition => _climbModule?.WorldPosition ?? Vector3.zero;

      /// <summary>
      ///    Fired when a dismount transition begins.
      ///    Parameters are the current position/rotation of the character,
      ///    followed by the target landing position/rotation.
      /// </summary>
      public event Action<Vector3, Quaternion, Vector3, Quaternion> DismountStarted;

      public event Action FinishUsingLadder;

      public event Action<ILadderVolume, LadderMountPoint> MountStarted;
      private readonly PlayerLadderConfig _config;

      public PlayerLadderService(PlayerLadderConfig config, Transform characterTransform)
      {
         _characterTransform = characterTransform;
         if (config == null)
         {
            Debug.LogWarning("Ladder controller config is not assigned.");
            return;
         }

         _config = ScriptableObject.Instantiate(config);

         _climbModule = new LadderClimbModule(_config, characterTransform);

         _mountService = new LadderMountService(_config, characterTransform);

         _mountService.BeginMount += () =>
            {
               MountStarted?.Invoke(_mountService.CurrentLadder, _mountService.CurrentMountPoint);
            };
         _mountService.FinishMount += () =>
            {
               _climbModule.UpdateLadderState(_mountService.CurrentLadder, _mountService.CurrentMountPoint);
            };
         _mountService.BeginDismount += (targetPos, targetRot) =>
            {
               var trx = characterTransform;
               DismountStarted?.Invoke(trx.position, trx.rotation, targetPos, targetRot);
            };
         _mountService.FinishDismount += () => FinishUsingLadder?.Invoke();
      }

      public void SetLadderDetectionDistance(float distance)
      {
         _config.SetProbeRange(distance);
      }

      public static float AdjustInputForCamera(float verticalInput, float cameraForwardY, float characterForwardY)
      {
         if (cameraForwardY < characterForwardY - 0.8f)
         {
            return -verticalInput;
         }

         return verticalInput;
      }

      public bool CanTryAutoDismount()
      {
         return _mountService?.TryAutoDismount(false) ?? false;
      }

      /// <summary>
      ///    Initiates a dismount with automatic detection of the landing position.
      /// </summary>
      public void Dismount()
      {
         _mountService?.Dismount();
      }

      /// <summary>
      ///    Initiates a dismount transition to the specified target position and
      ///    rotation, bypassing landing-point detection. Intended for
      ///    server-authoritative dismounting where the landing has already been
      ///    validated.
      /// </summary>
      /// <param name="position">Target world-space landing position.</param>
      /// <param name="rotation">Target world-space landing rotation.</param>
      public void Dismount(Vector3 position, Quaternion rotation)
      {
         _mountService?.Dismount(position, rotation);
      }

      public void ForceDismount()
      {
         _mountService?.ForceDismount();
      }

      /// <summary>
      ///    Mounts the player onto the specified ladder at the given mount point
      ///    without performing any proximity or raycast detection. Intended for
      ///    server-authoritative mounting where validation has already occurred.
      /// </summary>
      /// <param name="ladder">The ladder to mount onto.</param>
      /// <param name="mountPoint">The mount point to use.</param>
      /// <returns><c>true</c> if the mount succeeded; <c>false</c> if <paramref name="ladder" /> is null.</returns>
      public bool ForceMount(ILadderVolume ladder, LadderMountPoint mountPoint)
      {
         if (ladder == null || _mountService == null)
         {
            return false;
         }

         var result = _mountService.ForceMount(ladder, mountPoint);

         if (result)
         {
            MountStarted?.Invoke(ladder, mountPoint);
         }

         return result;
      }

      public void Move(float verticalInput, float dt, bool isSprinting = false, float? cameraForwardY = null)
      {
         IsSprinting = isSprinting;
         var forwardY = cameraForwardY ?? _characterTransform.forward.y;
         verticalInput = AdjustInputForCamera(verticalInput, forwardY, _characterTransform.forward.y);
         if (_mountService.IsMounted)
         {
            _climbModule.Move(verticalInput, dt, isSprinting);
         }
      }

      public void TickMountingAnimators(float dt, bool allowAutoDismount = true)
      {
         if (_climbModule == null || _mountService == null)
         {
            return;
         }

         _mountService.TickTransitions(dt);
         _mountService.TryAutoDismount(allowAutoDismount);
      }

      public bool TryMount(Vector3 playerHeadPosition, Quaternion playerHeadRotation)
      {
         return _mountService?.TryMount(playerHeadPosition, playerHeadRotation) ?? false;
      }

      /// <summary>
      ///    Returns a <see cref="LadderSnapshot" /> populated from the current ladder
      ///    state. If the player is not mounted, returns a default snapshot with
      ///    <see cref="LadderSnapshot.IsMounted" /> set to <c>false</c>.
      /// </summary>
      /// <returns>A snapshot of the current ladder state.</returns>
      public LadderSnapshot GetSnapshot()
      {
         if (!IsMounted)
         {
            return default;
         }

         return new LadderSnapshot(
         CurrentLadder?.LadderId ?? 0,
         CurrentMountPoint,
         NormalizedProgress,
         LadderPosition,
         IsMounted,
         IsTransitioning,
         IsSprinting);
      }

      /// <summary>
      ///    Force-sets the internal ladder state to match the supplied snapshot.
      ///    Used for silent network reconciliation — does not fire
      ///    <see cref="MountStarted" />, <see cref="DismountStarted" />, or
      ///    <see cref="FinishUsingLadder" /> events.
      ///    If <paramref name="snapshot" />.<see cref="LadderSnapshot.IsMounted" /> is
      ///    <c>false</c>, state is silently cleared and the character transform is set
      ///    to <paramref name="snapshot" />.<see cref="LadderSnapshot.Position" />.
      /// </summary>
      /// <param name="snapshot">The snapshot to restore from.</param>
      /// <param name="ladder">
      ///    Optional ladder reference. Required when restoring a mounted state and
      ///    <see cref="CurrentLadder" /> is <c>null</c> or does not match the
      ///    snapshot’s <see cref="LadderSnapshot.LadderId" />.
      /// </param>
      public void RestoreSnapshot(LadderSnapshot snapshot, ILadderVolume ladder = null)
      {
         if (_mountService == null || _climbModule == null)
         {
            return;
         }

         if (!snapshot.IsMounted)
         {
            _mountService.SilentClearState();

            if (snapshot.Position != Vector3.zero)
            {
               _characterTransform.position = snapshot.Position;
            }

            return;
         }

         // Resolve the ladder: prefer the explicitly supplied reference,
         // fall back to CurrentLadder if it already matches.
         var resolved = ladder;

         if (resolved == null || resolved.LadderId != snapshot.LadderId)
         {
            resolved = CurrentLadder;
         }

         if (resolved == null || resolved.LadderId != snapshot.LadderId)
         {
            // No matching ladder available — caller must ensure validity.
            return;
         }

         _mountService.ForceSetState(resolved, snapshot.MountPoint, true);
         _climbModule.RestoreState(resolved, snapshot.MountPoint, snapshot.Progress);

         _characterTransform.position = snapshot.Position;
      }
   }
}
