namespace P3k.PlayerLadderController.Implementations.Services
{
   using P3k.PlayerLadderController.Abstractions.Configs;
   using P3k.PlayerLadderController.Abstractions.Interfaces;
   using P3k.PlayerLadderController.Abstractions.Models;
   using P3k.PlayerLadderController.Implementations.Modules;

   using System;
   using System.Linq;

   using UnityEngine;

   internal sealed class LadderMountService
   {
      private readonly LadderMountModule _mountModule;

      private readonly LadderTransitionModule _dismountTransition;

      private readonly LadderTransitionModule _mountTransition;

      private readonly PlayerLadderConfig _config;

      private readonly Transform _characterTransform;

      private bool _isDismounting;

      private bool _isMounting;

      internal bool IsTransitioning => _isMounting || _isDismounting;

      internal bool IsMounted { get; private set; }

      internal ILadderVolume CurrentLadder { get; private set; }

      internal LadderMountPoint CurrentMountPoint { get; private set; }

      internal event Action<Vector3, Quaternion> BeginDismount;

      internal event Action BeginMount;

      internal event Action FinishDismount;

      internal event Action FinishMount;

      internal LadderMountService(PlayerLadderConfig config, Transform characterTransform)
      {
         _config = config;
         _characterTransform = characterTransform;

         _mountModule = new LadderMountModule(_config, characterTransform);
         _mountTransition = new LadderTransitionModule();
         _dismountTransition = new LadderTransitionModule();
      }

      internal void Dismount(Vector3? position = null, Quaternion? rotation = null)
      {
         if (_isDismounting || _isMounting || !IsMounted || !_characterTransform)
         {
            return;
         }

         _isDismounting = true;

         if (position.HasValue && rotation.HasValue)
         {
            StartDismount(position.Value, rotation.Value, _config.DismountDuration);
            return;
         }

         var ladderCollider = CurrentLadder.Collider;

         if (!_mountModule.TryDetectDismountPoint(ladderCollider, out var dismountPos, out var dismountRot))
         {
            dismountPos = _characterTransform.position;
            dismountRot = _characterTransform.rotation;
         }

         StartDismount(dismountPos, dismountRot, _config.DismountDuration);
      }

      internal void ForceDismount()
      {
         _dismountTransition.Stop();
         _mountTransition.Stop();
         IsMounted = false;
         _isDismounting = false;
         _isMounting = false;
         CurrentLadder = null;
         CurrentMountPoint = default;
         FinishDismount?.Invoke();
      }

      /// <summary>
      ///    Mounts the player onto the specified ladder without detection or transition.
      ///    Fires <see cref="BeginMount" /> and immediately completes the mount.
      /// </summary>
      /// <param name="ladder">The ladder to mount.</param>
      /// <param name="mountPoint">The mount point to use.</param>
      /// <returns><c>true</c> if the mount succeeded; <c>false</c> if <paramref name="ladder" /> is null.</returns>
      internal bool ForceMount(ILadderVolume ladder, LadderMountPoint mountPoint)
      {
         if (ladder == null)
         {
            return false;
         }

         CurrentLadder = ladder;
         CurrentMountPoint = mountPoint;
         IsMounted = true;
         _isMounting = false;
         _isDismounting = false;

         _characterTransform.SetPositionAndRotation(mountPoint.Position, mountPoint.Rotation);

         BeginMount?.Invoke();
         FinishMount?.Invoke();

         return true;
      }

      /// <summary>
      ///    Silently sets the mount state without transitions or events.
      ///    Used for network reconciliation.
      /// </summary>
      /// <param name="ladder">The ladder to restore onto.</param>
      /// <param name="mountPoint">The mount point to restore.</param>
      /// <param name="isMounted">Whether the player should be considered mounted.</param>
      internal void ForceSetState(ILadderVolume ladder, LadderMountPoint mountPoint, bool isMounted)
      {
         _dismountTransition.Stop();
         _mountTransition.Stop();
         _isDismounting = false;
         _isMounting = false;
         CurrentLadder = ladder;
         CurrentMountPoint = mountPoint;
         IsMounted = isMounted;
      }

      /// <summary>
      ///    Silently clears all ladder state without firing any events.
      ///    Used for network reconciliation when restoring to a dismounted state.
      /// </summary>
      internal void SilentClearState()
      {
         _dismountTransition.Stop();
         _mountTransition.Stop();
         IsMounted = false;
         _isDismounting = false;
         _isMounting = false;
         CurrentLadder = null;
         CurrentMountPoint = default;
      }

      internal void TickTransitions(float dt)
      {
         _mountTransition.Tick(dt);
         _dismountTransition.Tick(dt);
      }

      internal bool TryAutoDismount(bool callDismount)
      {
         if (IsTransitioning || !IsMounted || !_characterTransform)
         {
            return false;
         }

         if (CurrentLadder is not {AutoDismountEnabled: true})
         {
            return false;
         }

         var localPos = CurrentLadder.InverseTransformPoint(_characterTransform.position);
         var localHeight = localPos.y - CurrentLadder.LocalCenter.y;

         if (localHeight >= CurrentLadder.LocalAutoDismountHeightMax
             || localHeight <= CurrentLadder.LocalAutoDismountHeightMin)
         {
            if (callDismount)
            {
               Dismount();
            }

            return true;
         }

         return false;
      }

      internal bool TryMount(Vector3 fromPosition, Quaternion fromRotation)
      {
         if (IsTransitioning || IsMounted)
         {
            return false;
         }

         if (_mountModule.TryDetectMountPoint(
             fromPosition,
             fromRotation * Vector3.forward,
             out var detectedLadder,
             out var detectedMount))
         {
            StartMount(detectedLadder, detectedMount, _config.MountDuration);
            return true;
         }

         return false;
      }

      private void EndDismount(Vector3 finalPosition, Quaternion finalRotation)
      {
         _characterTransform.SetPositionAndRotation(finalPosition, finalRotation);

         _isDismounting = false;
         CurrentLadder = null;
         CurrentMountPoint = default;
         FinishDismount?.Invoke();
      }

      private void EndMount(LadderMountPoint mountPoint)
      {
         _characterTransform.SetPositionAndRotation(mountPoint.Position, mountPoint.Rotation);

         _isMounting = false;
         IsMounted = true;
         FinishMount?.Invoke();
      }

      private void StartDismount(Vector3 targetPosition, Quaternion targetRotation, float duration)
      {
         IsMounted = false;
         BeginDismount?.Invoke(targetPosition, targetRotation);

         _dismountTransition.Play(
         _characterTransform,
         targetPosition,
         targetRotation,
         duration,
         () => EndDismount(targetPosition, targetRotation));
      }

      private void StartMount(ILadderVolume ladder, LadderMountPoint mountPoint, float mountDuration = 0)
      {
         if (ladder == null)
         {
            return;
         }

         CurrentLadder = ladder;
         CurrentMountPoint = mountPoint;
         IsMounted = false;
         _isMounting = true;

         BeginMount?.Invoke();

         _mountTransition.Play(
         _characterTransform,
         mountPoint.Position,
         mountPoint.Rotation,
         mountDuration,
         () => EndMount(mountPoint));
      }
   }
}