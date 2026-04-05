namespace P3k.PlayerLadderController.Abstractions.Configs
{
   using System.Linq;

   using UnityEngine;

   [CreateAssetMenu(fileName = "PlayerLadderConfig", menuName = "P3k/Player Ladder Config")]
   public sealed class PlayerLadderConfig : ScriptableObject
   {
      [Header("Detection")]
      [SerializeField]
      private float _probeRange = 3f;

      [SerializeField]
      private float _characterHeight = 2f;

      [SerializeField]
      private float _characterRadius = 0.3f;

      [Header("Mounting")]
      [SerializeField]
      private float _mountDuration = 0.2f;

      [SerializeField]
      private float _dismountDuration = 0.15f;

      [Header("Climbing")]
      [SerializeField]
      private float _moveSpeed = 2f;

      [SerializeField]
      private float _sprintSpeedMultiplier = 1.5f;

      [Header("Layer Masks")]
      [SerializeField]
      private LayerMask _obstructionsLayerMask = ~0;

      [SerializeField]
      private LayerMask _exclusionsLayerMask;

      // Properties
      public float ProbeRange => _probeRange;

      public float CharacterHeight => _characterHeight;

      public float CharacterRadius => _characterRadius;

      public float MountDuration => _mountDuration;

      public float DismountDuration => _dismountDuration;

      public float MoveSpeed => _moveSpeed;

      /// <summary>
      ///    Multiplier applied to <see cref="MoveSpeed" /> while sprinting.
      /// </summary>
      public float SprintSpeedMultiplier => _sprintSpeedMultiplier;

      public LayerMask ObstructionsLayerMask => _obstructionsLayerMask;

      public LayerMask ExclusionsLayerMask => _exclusionsLayerMask;

      public int DetectionMask => Physics.AllLayers & ~ExclusionsLayerMask;

      public void SetProbeRange(float range)
      {
         _probeRange = Mathf.Max(0f, range);
      }

      public int ObstructionMask
      {
         get
         {
            var baseMask = (int)ObstructionsLayerMask == 0 ? Physics.AllLayers : (int)ObstructionsLayerMask;
            return baseMask & ~ExclusionsLayerMask;
         }
      }

      public Vector3 ComputeCharacterCentre(Transform characterTransform)
      {
         var centre = characterTransform.position + (Vector3.up * (CharacterHeight * 0.5f));
         centre += characterTransform.forward.normalized * (CharacterRadius * 1.85f);
         return centre;
      }

#if UNITY_EDITOR
      private void OnValidate()
      {
         _probeRange = Mathf.Max(0f, _probeRange);
         _characterHeight = Mathf.Max(0.1f, _characterHeight);
         _characterRadius = Mathf.Max(0f, _characterRadius);
      }
#endif
   }
}
