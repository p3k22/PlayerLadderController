namespace P3k.PlayerLadderController.Implementations.Services
{
   using P3k.PlayerLadderController.Abstractions.Configs;
   using P3k.PlayerLadderController.Abstractions.Interfaces;
   using P3k.PlayerLadderController.Abstractions.Models;
   using P3k.PlayerLadderController.Implementations.DataContainers;
   using P3k.PlayerLadderController.Implementations.Modules;

   using System.Linq;

   using UnityEngine;
#if UNITY_EDITOR
   using UnityEditor;
#endif

   /// <summary>
   ///    Plain helper that visualises ladder detection, mount and dismount probes
   ///    via Unity gizmos. Create an instance, then call <see cref="DrawGizmos" />
   ///    from the owning MonoBehaviour's <c>OnDrawGizmos</c>.
   /// </summary>
   public sealed class PlayerLadderGizmoService
   {
      private readonly DismountProbeResult[] _dismountResults = new DismountProbeResult[4];

      private readonly MountProbeResult[] _mountResults = new MountProbeResult[2];

      private readonly PlayerLadderConfig _config;

      private readonly IPlayerLadderService _service;

      private readonly Transform _characterTransform;

      private float _lastVerticalInput;

      private ILadderVolume _lastLadderVolume;

      /// <summary>
      ///    Creates a new gizmo helper bound to the given service and transforms.
      /// </summary>
      /// <param name="service">The ladder service to read state from.</param>
      /// <param name="config">The ladder configuration asset.</param>
      /// <param name="characterTransform">The character's transform.</param>
      public PlayerLadderGizmoService(
         IPlayerLadderService service,
         PlayerLadderConfig config,
         Transform characterTransform)
      {
         _service = service;
         _config = config;
         _characterTransform = characterTransform;
      }

      /// <summary>
      ///    Draws all ladder gizmos. Call from <c>OnDrawGizmos</c>.
      /// </summary>
      public void DrawGizmos(Vector3 playerHeadPosition, Quaternion playerHeadRotation, float vInput)
      {
      #if UNITY_EDITOR
         if (_config == null)
         {
            return;
         }

         _verticalInput = vInput;
         var origin = playerHeadPosition;
         var direction = playerHeadRotation * Vector3.forward;

         if (_service is {IsMounted: true})
         {
            DrawMoverCollider(_lastLadderVolume);
            DrawDismountProbes(_service.CurrentLadder);
            return;
         }

         if (!LadderMountModule.TryDetectLadder(
             origin,
             direction,
             _config.ProbeRange,
             _config.DetectionMask,
             out var hit,
             out var ladder))
         {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(origin, origin + (direction * _config.ProbeRange));
            Handles.Label(origin + (direction * _config.ProbeRange), "No hit");
            return;
         }

         Gizmos.color = Color.green;
         Gizmos.DrawLine(origin, hit.point);
         Handles.Label(hit.point, "Hit");

         _lastLadderVolume = ladder;

         var count = LadderMountModule.EvaluateMountCandidates(
         ladder,
         origin,
         _characterTransform,
         _config,
         _mountResults);

         for (var i = 0; i < count; i++)
         {
            var result = _mountResults[i];

            if (!result.WasCreated)
            {
               continue;
            }

            DrawObstructionBox(result.MountPoint);

            if (result.IsObstructed)
            {
               Gizmos.color = Color.red;
               Gizmos.DrawWireSphere(result.MountPoint.Position, 0.15f);
               Handles.Label(result.MountPoint.Position + (result.MountPoint.FaceNormal * 0.15f), "Obstructed");
               continue;
            }

            if (result.LineOfSight.IsBlocked)
            {
               Gizmos.color = Color.red;

               if (result.LineOfSight.HitSomething)
               {
                  Gizmos.DrawLine(result.LineOfSight.Origin, result.LineOfSight.HitPoint);
               }

               continue;
            }

            Gizmos.color = Color.green;
            Gizmos.DrawLine(result.LineOfSight.Origin, result.MountPoint.Position);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(result.MountPoint.Position, 0.2f);

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(result.MountPoint.Position, result.MountPoint.Rotation * Vector3.forward * 0.5f);

            Handles.Label(result.MountPoint.Position + (result.MountPoint.FaceNormal * 0.15f), "Mount");
            Handles.Label(
            result.MountPoint.Position + (result.MountPoint.Rotation * Vector3.forward * 0.55f),
            "Facing");

            break;
         }
      #endif
      }

   #if UNITY_EDITOR
      private void DrawDismountProbes(ILadderVolume currentLadder)
      {
         if (!_characterTransform)
         {
            return;
         }

         var ladderCollider = currentLadder?.Collider;
         var headPos = _characterTransform.position
                       + (Vector3.up * (_config.CharacterHeight - _config.CharacterRadius));

         Gizmos.color = Color.white;
         Gizmos.DrawWireSphere(headPos, 0.06f);

         var count = LadderMountModule.EvaluateDismountDirections(
         _characterTransform,
         ladderCollider,
         _config,
         _dismountResults);

         for (var i = 0; i < count; i++)
         {
            var probe = _dismountResults[i];

            if (probe.Horizontal.HitObstacle)
            {
               Gizmos.color = Color.red;
               Gizmos.DrawLine(probe.Horizontal.HeadPosition, probe.Horizontal.HitPoint);
               continue;
            }

            Gizmos.color = Color.white;
            Gizmos.DrawLine(probe.Horizontal.HeadPosition, probe.Horizontal.EndPoint);

            if (!probe.Ground.FoundGround)
            {
               Gizmos.color = Color.yellow;
               Gizmos.DrawLine(probe.Horizontal.EndPoint, probe.DownEndPoint);
               continue;
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(probe.Horizontal.EndPoint, probe.Ground.Point);

            Gizmos.color = probe.Ground.HasClearance ? Color.green : Color.red;
            Gizmos.DrawWireSphere(probe.Ground.CapsuleBottom, probe.Ground.CharacterRadius);
            Gizmos.DrawWireSphere(probe.Ground.CapsuleTop, probe.Ground.CharacterRadius);
            Gizmos.DrawLine(probe.Ground.CapsuleBottom, probe.Ground.CapsuleTop);
         }
      }

      private void DrawMoverCollider(ILadderVolume ladder)
      {
         if (ladder == null)
         {
            return;
         }

         var characterHeight = _config.CharacterHeight;
         var characterRadius = _config.CharacterRadius;

         var pos = _characterTransform.position;
         var bottom = pos + (Vector3.up * characterRadius);
         var top = pos + (Vector3.up * (characterHeight - characterRadius));

         var verticalInput = _verticalInput;
         if (!Mathf.Approximately(verticalInput, 0f))
         {
            _lastVerticalInput = verticalInput;
         }

         var moveDelta = _lastVerticalInput * _config.MoveSpeed * Time.deltaTime;

         if (Mathf.Approximately(moveDelta, 0f))
         {
            return;
         }

         var origin = moveDelta > 0f ? top : bottom;
         var direction = ladder.transform.up * Mathf.Sign(moveDelta);

         var distance = Mathf.Abs(moveDelta);
         if (distance < 1f)
         {
            distance = 1f;
         }

         Gizmos.color = Color.cyan;
         Gizmos.DrawWireSphere(origin, characterRadius);
         Handles.Label(origin + (direction * 0.1f), "Mover origin");

         if (Physics.SphereCast(
             origin,
             characterRadius,
             direction,
             out var hit,
             distance,
             _config.ObstructionsLayerMask,
             QueryTriggerInteraction.Ignore))
         {
            if (hit.collider != null && !ReferenceEquals(hit.collider, ladder.Collider))
            {
               distance = hit.distance;
               Gizmos.DrawCube(hit.point, new Vector3(characterRadius * 2f, 0.1f, characterRadius * 2f));
               Handles.Label(hit.point, "Mover hit");
            }
         }

         Gizmos.color = Color.red;
         Gizmos.DrawLine(origin, origin + (direction * distance));
         Handles.Label(origin + (direction * distance), "Mover path");
      }

      private float _verticalInput;

      private void DrawObstructionBox(LadderMountPoint mountPoint)
      {
         var halfExtents = new Vector3(
         _config.CharacterRadius,
         _config.CharacterHeight * 0.5f,
         _config.CharacterRadius);

         var center = mountPoint.Position + (Vector3.up * halfExtents.y) + (mountPoint.FaceNormal * halfExtents.z);

         Gizmos.color = Color.yellow;
         Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.LookRotation(mountPoint.FaceNormal), Vector3.one);
         Gizmos.DrawWireCube(Vector3.zero, halfExtents * 2f);
         Gizmos.matrix = Matrix4x4.identity;
         Handles.Label(center, "Obstruction box");
      }
   #endif
   }
}
