namespace P3k.PlayerLadderController.Adapters.Components
{
   using P3k.PlayerLadderController.Abstractions.Interfaces;

   using System.Linq;

   using UnityEngine;

   [RequireComponent(typeof(BoxCollider))]
   public sealed class LadderVolume : MonoBehaviour, ILadderVolume
   {
      private const float AUTO_DISMOUNT_MOUNT_POINT_OFFSET = 0.05f;

      /// <summary>
      ///    A stable, unique identifier for this ladder in the scene,
      ///    suitable for referencing across a network.
      /// </summary>
      public int LadderId => _ladderId;

      //Whether the player is automatically dismounted when reaching the climb height limits.
      public bool AutoDismountEnabled => _autoDismountEnabled;

      //Whether to draw ladder gizmos in the editor.
      [field: SerializeField]
      [field: Tooltip("Whether to draw ladder gizmos in the editor.")]
      public bool GizmosEnabled { get; private set; } = true;

      //The <see cref="UnityEngine.BoxCollider" /> that defines this ladder volume.
      public BoxCollider BoxCollider => _boxCollider ?? GetComponent<BoxCollider>();

      private BoxCollider _boxCollider;

      //The underlying <see cref="UnityEngine.Collider" /> for this ladder volume.
      public Collider Collider => BoxCollider;

      //Maximum auto-dismount height in local space, clamped between <see cref="MinClimbHeight" /> and <see cref="MaxClimbHeight" />.
      public float LocalAutoDismountHeightMax => Mathf.Clamp(_autoDismountHeightMax, MinClimbHeight, MaxClimbHeight);

      //Minimum auto-dismount height in local space, clamped between <see cref="MinClimbHeight" /> and <see cref="MaxClimbHeight" />.
      public float LocalAutoDismountHeightMin => Mathf.Clamp(_autoDismountHeightMin, MinClimbHeight, MaxClimbHeight);

      //Maximum mount height in local space, clamped to the upper extent of the volume and the auto-dismount max.
      public float LocalMountHeightMax =>
         Mathf.Min(
         Mathf.Min(_maxMountHeight, LocalHalfExtents.y),
         LocalAutoDismountHeightMax - AUTO_DISMOUNT_MOUNT_POINT_OFFSET);

      //Minimum mount height in local space, clamped to the lower extent of the volume and the auto-dismount min.
      public float LocalMountHeightMin =>
         Mathf.Max(
         Mathf.Max(_minMountHeight, -LocalHalfExtents.y),
         LocalAutoDismountHeightMin + AUTO_DISMOUNT_MOUNT_POINT_OFFSET);

      //Maximum climbable height in local space, inset from the top of the volume.
      public float MaxClimbHeight => (LocalCenter.y + LocalHalfExtents.y) - _climbHeightInsetTop;

      //Minimum climbable height in local space, inset from the bottom of the volume.
      public float MinClimbHeight => (LocalCenter.y - LocalHalfExtents.y) + _climbHeightInsetBottom;

      //Distance from the ladder face at which the player can mount.
      public float MountDistance => _mountDistance;

      //Center of the <see cref="BoxCollider" /> in local space.
      public Vector3 LocalCenter => BoxCollider == null ? Vector3.zero : BoxCollider.center;

      //Half-extents of the <see cref="BoxCollider" /> in local space.
      public Vector3 LocalHalfExtents => BoxCollider == null ? Vector3.zero : BoxCollider.size * 0.5f;

      private void Awake()
      {
         ValidateSetup();
         BoxCollider.isTrigger = true;
      }

      private void OnValidate()
      {
         ValidateSetup();
      }

      private void ValidateSetup()
      {
         _boxCollider = GetComponent<BoxCollider>();

         if (_ladderId == 0)
         {
            _ladderId = System.Guid.NewGuid().GetHashCode();
         }

         if (_boxCollider == null)
         {
            return;
         }

         var halfY = LocalHalfExtents.y;
         var minClimb = MinClimbHeight;
         var maxClimb = MaxClimbHeight;

         // Clamp mount heights to the actual volume extents.
         _maxMountHeight = Mathf.Clamp(_maxMountHeight, -halfY, halfY);
         _minMountHeight = Mathf.Clamp(_minMountHeight, -halfY, halfY);

         // Ensure min does not exceed max.
         _minMountHeight = Mathf.Min(_minMountHeight, _maxMountHeight);

         if (_autoDismountEnabled)
         {
            // Clamp auto-dismount heights to the climbable range.
            _autoDismountHeightMax = Mathf.Clamp(_autoDismountHeightMax, minClimb, maxClimb);
            _autoDismountHeightMin = Mathf.Clamp(_autoDismountHeightMin, minClimb, maxClimb);

            // Ensure auto-dismount min does not exceed auto-dismount max.
            _autoDismountHeightMin = Mathf.Min(_autoDismountHeightMin, _autoDismountHeightMax);

            // Ensure auto-dismount bounds stay outside the mount bounds so the
            // player is never auto-dismounted the instant they mount.
            _autoDismountHeightMax = Mathf.Max(_autoDismountHeightMax, _maxMountHeight + AUTO_DISMOUNT_MOUNT_POINT_OFFSET);
            _autoDismountHeightMin = Mathf.Min(_autoDismountHeightMin, _minMountHeight - AUTO_DISMOUNT_MOUNT_POINT_OFFSET);

            // Re-clamp to climbable range after the adjustment.
            _autoDismountHeightMax = Mathf.Clamp(_autoDismountHeightMax, minClimb, maxClimb);
            _autoDismountHeightMin = Mathf.Clamp(_autoDismountHeightMin, minClimb, maxClimb);

            // If clamping collapsed the gap, push mount points inward as a last resort.
            _maxMountHeight = Mathf.Min(_maxMountHeight, _autoDismountHeightMax - AUTO_DISMOUNT_MOUNT_POINT_OFFSET);
            _minMountHeight = Mathf.Max(_minMountHeight, _autoDismountHeightMin + AUTO_DISMOUNT_MOUNT_POINT_OFFSET);

            if (_minMountHeight > _maxMountHeight)
            {
               var mid = (_autoDismountHeightMin + _autoDismountHeightMax) * 0.5f;
               _minMountHeight = mid;
               _maxMountHeight = mid;
            }
         }
      }

      public Vector3 InverseTransformPoint(Vector3 worldPosition)
      {
         return transform.InverseTransformPoint(worldPosition);
      }

      public float LocalMountHeight(Vector3 worldPosition)
      {
         var localPos = InverseTransformPoint(worldPosition);
         var relativeY = localPos.y - LocalCenter.y;

         return Mathf.Clamp(relativeY, LocalMountHeightMin, LocalMountHeightMax);
      }

      public Vector3 TransformDirection(Vector3 localDirection)
      {
         return transform.TransformDirection(localDirection);
      }

      public Vector3 TransformPoint(Vector3 localPosition)
      {
         return transform.TransformPoint(localPosition);
      }

      #region Serialised Fields

      [SerializeField]
      [Tooltip("Stable unique identifier for this ladder, used for network referencing.")]
      private int _ladderId;

      [SerializeField]
      [Tooltip("Maximum mount height relative to ladder center.")]
      private float _maxMountHeight = 0.3f;

      [SerializeField]
      [Tooltip("Minimum mount height relative to ladder center.")]
      private float _minMountHeight = -0.45f;

      [SerializeField]
      [Tooltip("Mounting distance from face.")]
      private float _mountDistance = 0.6f;

      [SerializeField]
      [Tooltip("Inset from the top of the volume to define the maximum climb height.")]
      private float _climbHeightInsetTop = 0.1f;

      [SerializeField]
      [Tooltip("Inset from the bottom of the volume to define the minimum climb height.")]
      private float _climbHeightInsetBottom = 0.1f;

      [SerializeField]
      [Tooltip("Whether auto-dismount is enabled.")]
      private bool _autoDismountEnabled = true;

      [SerializeField]
      [Tooltip("Maximum auto-dismount height relative to ladder center. Clamped to climb height range.")]
      private float _autoDismountHeightMax = 0.3f;

      [SerializeField]
      [Tooltip("Minimum auto-dismount height relative to ladder center. Clamped to climb height range.")]
      private float _autoDismountHeightMin = -0.45f;

      #endregion
   }
}
