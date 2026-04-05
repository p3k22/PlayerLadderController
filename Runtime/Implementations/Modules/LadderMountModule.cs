
namespace P3k.PlayerLadderController.Implementations.Modules
{
   using P3k.PlayerLadderController.Abstractions.Configs;
   using P3k.PlayerLadderController.Abstractions.Enums;
   using P3k.PlayerLadderController.Abstractions.Interfaces;
   using P3k.PlayerLadderController.Abstractions.Models;
   using P3k.PlayerLadderController.Implementations.DataContainers;
   using P3k.PlayerLadderController.Implementations.Utilities;

   using System.Linq;

   using UnityEngine;

   internal sealed class LadderMountModule
   {
      private static readonly DismountProbeResult[] DirectionBuffer = new DismountProbeResult[4];

      private readonly PlayerLadderConfig _config;

      private readonly Transform _player;

      internal LadderMountModule(PlayerLadderConfig config, Transform player)
      {
         _config = config;
         _player = player;
      }

      internal bool TryDetectDismountPoint(Collider ladderCollider, out Vector3 position, out Quaternion rotation)
      {
         position = _player.position;
         rotation = _player.rotation;

         var count = EvaluateDismountDirections(_player, ladderCollider, _config, DirectionBuffer);

         for (var i = 0; i < count; i++)
         {
            if (DirectionBuffer[i].IsValid)
            {
               position = DirectionBuffer[i].DismountPosition;
               rotation = DirectionBuffer[i].DismountRotation;
               return true;
            }
         }

         return false;
      }

      internal static int EvaluateDismountDirections(
         Transform characterTransform,
         Collider ladderCollider,
         PlayerLadderConfig config,
         DismountProbeResult[] results)
      {
         var trx = characterTransform;
         var mask = config.ObstructionMask;
         var radius = config.CharacterRadius;
         var height = config.CharacterHeight;
         var headPos = trx.position + (Vector3.up * (height - radius));

         var directions = new[] { -trx.forward, trx.forward, -trx.right, trx.right };
         var count = 0;

         foreach (var rawDir in directions)
         {
            if (rawDir.sqrMagnitude <= 0f)
            {
               continue;
            }

            var dir = rawDir.normalized;
            var isBackward = Vector3.Dot(dir, trx.forward) < -0.5f;
            var horizontalDistance = radius * (isBackward ? 2f : 4f);
            var horizontalEnd = headPos + (dir * horizontalDistance);

            // Horizontal ray
            var hitHorizontalObstacle = false;
            var horizontalHitPoint = Vector3.zero;

            if (Physics.Raycast(headPos, dir, out var hHit, horizontalDistance, mask, QueryTriggerInteraction.Ignore))
            {
               if (!ladderCollider || hHit.collider != ladderCollider)
               {
                  hitHorizontalObstacle = true;
                  horizontalHitPoint = hHit.point;
               }
            }

            var horizontal = new DismountProbeResult.HorizontalProbe(
            dir,
            headPos,
            horizontalDistance,
            hitHorizontalObstacle,
            horizontalHitPoint);

            if (hitHorizontalObstacle)
            {
               var ground = new DismountProbeResult.GroundProbe(false, default, false, radius, height);
               results[count++] = new DismountProbeResult(horizontal, ground, trx.position, trx.rotation);
               continue;
            }

            // Ground ray
            var foundGround = Physics.Raycast(
            horizontalEnd,
            Vector3.down,
            out var groundHit,
            height * 1.5f,
            mask,
            QueryTriggerInteraction.Ignore);

            if (!foundGround)
            {
               var ground = new DismountProbeResult.GroundProbe(false, default, false, radius, height);
               results[count++] = new DismountProbeResult(horizontal, ground, trx.position, trx.rotation);
               continue;
            }

            // Clearance check
            var hasClearance = HasClearance(groundHit.point, groundHit.collider, ladderCollider, radius, height, mask);

            var dismountPos = groundHit.point;
            var dismountRot = isBackward ? trx.rotation : Quaternion.LookRotation(dir, Vector3.up);

            var groundProbe = new DismountProbeResult.GroundProbe(true, groundHit.point, hasClearance, radius, height);

            results[count++] = new DismountProbeResult(horizontal, groundProbe, dismountPos, dismountRot);
         }

         return count;
      }

      private static bool HasClearance(
         Vector3 foot,
         Collider groundCollider,
         Collider ladderCollider,
         float radius,
         float height,
         int mask)
      {
         var overlaps = Physics.OverlapCapsule(
         foot + (Vector3.up * radius),
         foot + (Vector3.up * (height - radius)),
         radius,
         mask,
         QueryTriggerInteraction.Ignore);

         foreach (var c in overlaps)
         {
            if (c == groundCollider)
            {
               continue;
            }

            if (ladderCollider && c == ladderCollider)
            {
               continue;
            }

            return false;
         }

         return true;
      }

      private static readonly LadderFace[] FacesToTry = new LadderFace[2];

      private static readonly MountProbeResult[] CandidateBuffer = new MountProbeResult[2];

      internal bool TryDetectMountPoint(
         Vector3 fromPosition,
         Vector3 facingDirection,
         out ILadderVolume ladder,
         out LadderMountPoint mountPoint)
      {
         ladder = null;
         mountPoint = default;

         if (!TryDetectLadder(
             fromPosition,
             facingDirection,
             _config.ProbeRange,
             _config.DetectionMask,
             out _,
             out var detectedLadder))
         {
            return false;
         }

         ladder = detectedLadder;

         var count = EvaluateMountCandidates(detectedLadder, fromPosition, _player, _config, CandidateBuffer);

         for (var i = 0; i < count; i++)
         {
            if (CandidateBuffer[i].IsValid)
            {
               mountPoint = CandidateBuffer[i].MountPoint;
               return true;
            }
         }

         return false;
      }

      /// <summary>
      ///    Casts a ray from origin along direction to detect a <see cref="LadderVolume"/>.
      /// </summary>
      internal static bool TryDetectLadder(
         Vector3 origin,
         Vector3 direction,
         float range,
         int detectionMask,
         out RaycastHit hit,
         out ILadderVolume ladder)
      {
         ladder = null;

         return Physics.Raycast(origin, direction, out hit, range, detectionMask, QueryTriggerInteraction.Collide)
                && hit.collider.TryGetComponent(out ladder);
      }

      /// <summary>
      ///    Evaluates mount candidates for the two climbable faces and writes results into
      ///    <paramref name="results"/>. Returns the number of results written (up to 2).
      ///    Shared by runtime detection and editor gizmos.
      /// </summary>
      internal static int EvaluateMountCandidates(
         ILadderVolume ladder,
         Vector3 cameraWorldPosition,
         Transform characterTransform,
         PlayerLadderConfig config,
         MountProbeResult[] results)
      {
         var mountFace = LadderFaceUtils.GetClosestFace(ladder, cameraWorldPosition);

         FacesToTry[0] = mountFace;
         FacesToTry[1] = LadderFaceUtils.GetOppositeFace(mountFace);

         var localMountHeight = ladder.LocalMountHeight(characterTransform.position);
         var detectionMask = config.DetectionMask;
         var count = 0;

         foreach (var face in FacesToTry)
         {
            if (!LadderMountPointFactory.TryCreate(ladder, face, localMountHeight, out var candidate))
            {
               results[count++] = MountProbeResult.NotCreated(face);
               continue;
            }

            if (LadderMountObstructionChecker.IsObstructed(candidate, ladder.Collider, config))
            {
               results[count++] = MountProbeResult.Obstructed(face, candidate);
               continue;
            }

            var characterCentre = config.ComputeCharacterCentre(characterTransform);
            var toMountPoint = candidate.Position - characterCentre;
            var distance = toMountPoint.magnitude;

            if (distance <= 0.001f)
            {
               results[count++] = MountProbeResult.NotCreated(face);
               continue;
            }

            var dir = toMountPoint / distance;
            var hitSomething = false;
            var hitPoint = Vector3.zero;
            var blocked = false;

            if (Physics.Raycast(
                characterCentre,
                dir,
                out var hit,
                distance,
                detectionMask,
                QueryTriggerInteraction.Ignore))
            {
               hitSomething = true;
               hitPoint = hit.point;

               var hitLadder = hit.collider.GetComponent<ILadderVolume>();
               blocked = !ReferenceEquals(hitLadder, ladder);
            }

            var los = new MountProbeResult.LineOfSightProbe(
            characterCentre,
            dir,
            distance,
            hitSomething,
            hitPoint,
            blocked);

            results[count++] = new MountProbeResult(face, true, candidate, false, los);
         }

         return count;
      }
   }
}
