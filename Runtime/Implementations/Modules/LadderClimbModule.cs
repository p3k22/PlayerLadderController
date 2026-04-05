namespace P3k.PlayerLadderController.Implementations.Modules
{
   using P3k.PlayerLadderController.Abstractions.Configs;
   using P3k.PlayerLadderController.Abstractions.Interfaces;
   using P3k.PlayerLadderController.Abstractions.Models;

   using System.Linq;

   using UnityEngine;

   internal sealed class LadderClimbModule
   {
      private readonly PlayerLadderConfig _config;

      private LadderMountPoint _currentMountPoint;

      private ILadderVolume _currentLadder;

      private Quaternion _localRotationOnLadder;

      private readonly Transform _characterTransform;

      private Vector3 _localPositionOnLadder;

      internal LadderClimbModule(PlayerLadderConfig configSnapshot, Transform characterTransform)
      {
         _config = configSnapshot;
         _characterTransform = characterTransform;
      }

      /// <summary>
      ///    Normalised 0–1 progress along the mounted ladder's rail.
      ///    Returns 0 if no ladder is active.
      /// </summary>
      internal float NormalizedProgress
      {
         get
         {
            if (_currentLadder == null)
            {
               return 0f;
            }

            var min = _currentLadder.MinClimbHeight;
            var max = _currentLadder.MaxClimbHeight;
            var range = max - min;

            if (range <= 0f)
            {
               return 0f;
            }

            return Mathf.Clamp01((_localPositionOnLadder.y - min) / range);
         }
      }

      /// <summary>
      ///    World-space position of the player on the current ladder.
      ///    Returns <see cref="Vector3.zero" /> if no ladder is active.
      /// </summary>
      internal Vector3 WorldPosition =>
         _currentLadder != null ? _currentLadder.TransformPoint(_localPositionOnLadder) : Vector3.zero;

      internal void UpdateLadderState(ILadderVolume currentLadder, LadderMountPoint currentMountPoint)
      {
         _currentLadder = currentLadder;
         _currentMountPoint = currentMountPoint;

         if (currentLadder != null)
         {
            _localPositionOnLadder = currentLadder.InverseTransformPoint(currentMountPoint.Position);
            _localRotationOnLadder = Quaternion.Inverse(currentLadder.transform.rotation) * currentMountPoint.Rotation;
         }
      }

      /// <summary>
      ///    Silently restores the climb module state from a normalised progress value.
      ///    Used for network reconciliation — does not fire events.
      /// </summary>
      /// <param name="ladder">The ladder to restore onto.</param>
      /// <param name="mountPoint">The mount point to restore.</param>
      /// <param name="normalizedProgress">Normalised 0–1 progress along the rail.</param>
      internal void RestoreState(ILadderVolume ladder, LadderMountPoint mountPoint, float normalizedProgress)
      {
         _currentLadder = ladder;
         _currentMountPoint = mountPoint;

         if (ladder == null)
         {
            return;
         }

         _localRotationOnLadder = Quaternion.Inverse(ladder.transform.rotation) * mountPoint.Rotation;

         // Rebuild local position from normalised progress.
         var baseLocal = ladder.InverseTransformPoint(mountPoint.Position);
         var min = ladder.MinClimbHeight;
         var max = ladder.MaxClimbHeight;
         baseLocal.y = Mathf.Lerp(min, max, Mathf.Clamp01(normalizedProgress));
         _localPositionOnLadder = baseLocal;

         SyncCharacterWithLadder();
      }

      internal void Move(float verticalInput, float deltaTime, bool isSprinting = false)
      {
         if (_characterTransform == null || _currentLadder == null)
         {
            return;
         }

         var ladder = _currentLadder;

         if (!Mathf.Approximately(verticalInput, 0f))
         {
            var speed = _config.MoveSpeed * (isSprinting ? _config.SprintSpeedMultiplier : 1f);
            var moveDelta = verticalInput * speed * deltaTime;

            if (!Mathf.Approximately(moveDelta, 0f))
            {
               var ladderTrx = ladder.transform;
               var characterHeight = _config.CharacterHeight;
               var characterRadius = _config.CharacterRadius;
               var obstructionMask = _config.ObstructionsLayerMask;

               var direction = ladderTrx.up * Mathf.Sign(moveDelta);
               var distance = Mathf.Abs(moveDelta);

               var currentWorld = ladder.TransformPoint(_localPositionOnLadder);
               var bottom = currentWorld;
               var top = bottom + (Vector3.up * characterHeight);
               bottom += Vector3.up * characterRadius;
               top -= Vector3.up * characterRadius;
               var origin = moveDelta > 0 ? top : bottom;

               if (Physics.SphereCast(
                   origin,
                   characterRadius,
                   direction,
                   out var hit,
                   distance,
                   obstructionMask,
                   QueryTriggerInteraction.Ignore))
               {
                  if (hit.collider && !ReferenceEquals(hit.collider, ladder.Collider))
                  {
                     SyncCharacterWithLadder();
                     return;
                  }
               }

               var targetWorld = currentWorld + (ladderTrx.up * moveDelta);
               var localTarget = ladder.InverseTransformPoint(targetWorld);
               localTarget.y = Mathf.Clamp(localTarget.y, ladder.MinClimbHeight, ladder.MaxClimbHeight);

               _localPositionOnLadder = localTarget;
            }
         }

         SyncCharacterWithLadder();
      }

      private void SyncCharacterWithLadder()
      {
         var worldPos = _currentLadder.TransformPoint(_localPositionOnLadder);
         var worldRot = _currentLadder.transform.rotation * _localRotationOnLadder;
         _characterTransform.SetPositionAndRotation(worldPos, worldRot);
      }
   }
}
