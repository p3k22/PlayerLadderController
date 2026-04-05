namespace P3k.PlayerLadderController.Implementations.DataContainers
{
   using System.Linq;

   using UnityEngine;

   /// <summary>
   ///    Captures the evaluation of a single dismount direction, including
   ///    intermediate data useful for gizmo visualisation.
   /// </summary>
   internal readonly struct DismountProbeResult
   {
      /// <summary>
      ///    Groups the horizontal ray-cast probe data.
      /// </summary>
      internal readonly struct HorizontalProbe
      {
         internal Vector3 Direction { get; }

         internal Vector3 HeadPosition { get; }

         internal float Distance { get; }

         internal bool HitObstacle { get; }

         internal Vector3 HitPoint { get; }

         internal Vector3 EndPoint => HeadPosition + (Direction * Distance);

         internal HorizontalProbe(
            Vector3 direction,
            Vector3 headPosition,
            float distance,
            bool hitObstacle,
            Vector3 hitPoint)
         {
            Direction = direction;
            HeadPosition = headPosition;
            Distance = distance;
            HitObstacle = hitObstacle;
            HitPoint = hitPoint;
         }
      }

      /// <summary>
      ///    Groups the downward ground-cast probe and clearance data.
      /// </summary>
      internal readonly struct GroundProbe
      {
         internal bool FoundGround { get; }

         internal Vector3 Point { get; }

         internal bool HasClearance { get; }

         internal float CharacterRadius { get; }

         internal float CharacterHeight { get; }

         internal Vector3 CapsuleBottom => Point + (Vector3.up * CharacterRadius);

         internal Vector3 CapsuleTop => Point + (Vector3.up * (CharacterHeight - CharacterRadius));

         internal GroundProbe(
            bool foundGround,
            Vector3 point,
            bool hasClearance,
            float characterRadius,
            float characterHeight)
         {
            FoundGround = foundGround;
            Point = point;
            HasClearance = hasClearance;
            CharacterRadius = characterRadius;
            CharacterHeight = characterHeight;
         }
      }

      internal HorizontalProbe Horizontal { get; }

      internal GroundProbe Ground { get; }

      internal Vector3 DismountPosition { get; }

      internal Quaternion DismountRotation { get; }

      internal Vector3 DownEndPoint => Horizontal.EndPoint + (Vector3.down * (Ground.CharacterHeight * 1.5f));

      internal bool IsValid => !Horizontal.HitObstacle && Ground.FoundGround && Ground.HasClearance;

      internal DismountProbeResult(
         HorizontalProbe horizontal,
         GroundProbe ground,
         Vector3 dismountPosition,
         Quaternion dismountRotation)
      {
         Horizontal = horizontal;
         Ground = ground;
         DismountPosition = dismountPosition;
         DismountRotation = dismountRotation;
      }
   }
}
