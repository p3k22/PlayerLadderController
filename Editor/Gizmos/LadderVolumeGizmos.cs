#if UNITY_EDITOR
namespace P3k.PlayerLadderController.Editor.Gizmos
   {
      using P3k.PlayerLadderController.Adapters.Components;

      using System.Linq;

      using UnityEditor;

      using UnityEngine;

      [CustomEditor(typeof(LadderVolume))]
      public class LadderVolumeGizmos : Editor
      {
         public override void OnInspectorGUI()
         {
            serializedObject.Update();

            var volume = (LadderVolume)target;
            var halfY = volume.LocalHalfExtents.y;
            var minClimb = volume.MinClimbHeight;
            var maxClimb = volume.MaxClimbHeight;

            DrawPropertiesExcluding(
               serializedObject,
               "_maxMountHeight",
               "_minMountHeight",
               "_autoDismountHeightMax",
               "_autoDismountHeightMin");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Mount Heights", EditorStyles.boldLabel);

            var maxMountProp = serializedObject.FindProperty("_maxMountHeight");
            var minMountProp = serializedObject.FindProperty("_minMountHeight");

            EditorGUILayout.Slider(maxMountProp, -halfY, halfY, new GUIContent("Max Mount Height", maxMountProp.tooltip));
            EditorGUILayout.Slider(minMountProp, -halfY, halfY, new GUIContent("Min Mount Height", minMountProp.tooltip));

            var autoDismountProp = serializedObject.FindProperty("_autoDismountEnabled");

            if (autoDismountProp.boolValue)
            {
               EditorGUILayout.Space();
               EditorGUILayout.LabelField("Auto-Dismount Heights", EditorStyles.boldLabel);

               var maxDismountProp = serializedObject.FindProperty("_autoDismountHeightMax");
               var minDismountProp = serializedObject.FindProperty("_autoDismountHeightMin");

               EditorGUILayout.Slider(maxDismountProp, minClimb, maxClimb, new GUIContent("Auto-Dismount Max", maxDismountProp.tooltip));
               EditorGUILayout.Slider(minDismountProp, minClimb, maxClimb, new GUIContent("Auto-Dismount Min", minDismountProp.tooltip));
            }

            serializedObject.ApplyModifiedProperties();
         }

         [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
         private static void DrawGizmos(LadderVolume volume, GizmoType gizmoType)
         {
            if (volume.BoxCollider == null || !volume.GizmosEnabled)
            {
               return;
            }

            var oldHandlesMatrix = Handles.matrix;
            var oldGizmosMatrix = Gizmos.matrix;
            var matrix = volume.transform.localToWorldMatrix;

            Handles.matrix = matrix;
            Gizmos.matrix = matrix;

            DrawVolumeBounds(volume);
            DrawFaces(volume);
            DrawMountPointExtremes(volume);
            DrawClimbHeightExtents(volume);
            DrawAutoDismountPoints(volume);

            Handles.matrix = oldHandlesMatrix;
            Gizmos.matrix = oldGizmosMatrix;
         }

         private static void DrawFaces(LadderVolume volume)
         {
            var center = volume.LocalCenter;
            var extents = volume.LocalHalfExtents;

            DrawFaceQuad(
            center,
            Vector3.forward,
            Vector3.right,
            Vector3.up,
            extents.z,
            extents.x,
            extents.y,
            Color.blue,
            "Front");

            DrawFaceQuad(
            center,
            Vector3.back,
            Vector3.left,
            Vector3.up,
            extents.z,
            extents.x,
            extents.y,
            Color.blue,
            "Back");

            DrawFaceQuad(
            center,
            Vector3.right,
            Vector3.back,
            Vector3.up,
            extents.x,
            extents.z,
            extents.y,
            Color.red,
            "Right");

            DrawFaceQuad(
            center,
            Vector3.left,
            Vector3.forward,
            Vector3.up,
            extents.x,
            extents.z,
            extents.y,
            Color.red,
            "Left");

            DrawFaceQuad(
            center,
            Vector3.up,
            Vector3.right,
            Vector3.back,
            extents.y,
            extents.x,
            extents.z,
            Color.green,
            "Top");

            DrawFaceQuad(
            center,
            Vector3.down,
            Vector3.right,
            Vector3.forward,
            extents.y,
            extents.x,
            extents.z,
            Color.green,
            "Bottom");
         }

         private static void DrawMountPointExtremes(LadderVolume volume)
         {
            var localCenter = volume.LocalCenter;
            var localHalfExtents = volume.LocalHalfExtents;
            var minHeight = volume.LocalMountHeightMin;
            var maxHeight = volume.LocalMountHeightMax;
            var colour = new Color(1f, 0.5f, 0f); // Orange

            DrawMountPointRange(volume, localCenter, localHalfExtents, Vector3.forward, minHeight, maxHeight, colour);
            DrawMountPointRange(volume, localCenter, localHalfExtents, Vector3.back, minHeight, maxHeight, colour);
         }

         private static void DrawClimbHeightExtents(LadderVolume volume)
         {
            var localCenter = volume.LocalCenter;
            var localHalfExtents = volume.LocalHalfExtents;
            var minHeight = volume.MinClimbHeight;
            var maxHeight = volume.MaxClimbHeight;
            var colour = new Color(1f, 0.9f, 0.1f);

            DrawClimbHeightLines(localCenter, localHalfExtents, Vector3.forward, minHeight, maxHeight, colour);
            DrawClimbHeightLines(localCenter, localHalfExtents, Vector3.back, minHeight, maxHeight, colour);
         }

         private static void DrawVolumeBounds(LadderVolume volume)
         {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(volume.BoxCollider.center, volume.BoxCollider.size);
         }

         private static void DrawFaceQuad(
            Vector3 center,
            Vector3 normal,
            Vector3 tangent,
            Vector3 bitangent,
            float normalDist,
            float tangentDist,
            float bitangentDist,
            Color colour,
            string label)
         {
            var faceCenter = center + (normal * normalDist);

            var corners = new Vector3[4]
                             {
                                faceCenter + (tangent * tangentDist) + (bitangent * bitangentDist),
                                (faceCenter - (tangent * tangentDist)) + (bitangent * bitangentDist),
                                faceCenter - (tangent * tangentDist) - (bitangent * bitangentDist),
                                (faceCenter + (tangent * tangentDist)) - (bitangent * bitangentDist)
                             };

            var faceColour = new Color(colour.r, colour.g, colour.b, 0.15f);
            var outlineColour = new Color(colour.r, colour.g, colour.b, 0.8f);

            Handles.DrawSolidRectangleWithOutline(corners, faceColour, outlineColour);

            Gizmos.color = colour;
            Gizmos.DrawLine(center, faceCenter);
            DrawUniformSphere(faceCenter, 0.05f);

            Handles.Label(faceCenter + (normal * 0.2f), label);
         }

         private static void DrawMountPointRange(
            LadderVolume volume,
            Vector3 localCenter,
            Vector3 localHalfExtents,
            Vector3 localNormal,
            float minHeight,
            float maxHeight,
            Color colour)
         {
            var faceZ = localNormal.z * (localHalfExtents.z + volume.MountDistance);

            var localMin = localCenter + new Vector3(0f, minHeight, faceZ);
            var localMax = localCenter + new Vector3(0f, maxHeight, faceZ);
            var halfHeight = Mathf.Max(localHalfExtents.y, 0.0001f);
            var minPercent = ((minHeight - localCenter.y) / halfHeight) * 100f;
            var maxPercent = ((maxHeight - localCenter.y) / halfHeight) * 100f;

            Gizmos.color = colour;
            Gizmos.DrawLine(localMin, localMax);
            DrawUniformSphere(localMin, 0.08f);
            DrawUniformSphere(localMax, 0.08f);

            Handles.Label(localMax + (localNormal * 0.15f), $"Max Mount: {maxPercent:F0}%");
            Handles.Label(localMin + (localNormal * 0.15f), $"Min Mount: {minPercent:F0}%");
         }

         private static void DrawClimbHeightLines(
            Vector3 localCenter,
            Vector3 localHalfExtents,
            Vector3 localNormal,
            float minHeight,
            float maxHeight,
            Color colour)
         {
            var faceZ = localNormal.z * localHalfExtents.z;
            var minStart = localCenter + new Vector3(-localHalfExtents.x, minHeight, faceZ);
            var minEnd = localCenter + new Vector3(localHalfExtents.x, minHeight, faceZ);
            var maxStart = localCenter + new Vector3(-localHalfExtents.x, maxHeight, faceZ);
            var maxEnd = localCenter + new Vector3(localHalfExtents.x, maxHeight, faceZ);
            var prevColor = Handles.color;
            var labelOffset = localNormal * 0.15f;
            var halfHeight = Mathf.Max(localHalfExtents.y, 0.0001f);
            var minPercent = ((minHeight - localCenter.y) / halfHeight) * 100f;
            var maxPercent = ((maxHeight - localCenter.y) / halfHeight) * 100f;

            Handles.color = colour;
            Handles.DrawDottedLine(minStart, minEnd, 4f);
            Handles.DrawDottedLine(maxStart, maxEnd, 4f);
            Handles.Label(minEnd + labelOffset, $"Min Climb: {minPercent:F0}%");
            Handles.Label(maxEnd + labelOffset, $"Max Climb: {maxPercent:F0}%");
            Handles.color = prevColor;
         }

         private static void DrawAutoDismountPoints(LadderVolume volume)
         {
            if (!volume.AutoDismountEnabled)
            {
               return;
            }

            var localCenter = volume.LocalCenter;
            var localHalfExtents = volume.LocalHalfExtents;
            var minHeight = volume.LocalAutoDismountHeightMin;
            var maxHeight = volume.LocalAutoDismountHeightMax;
            var colour = new Color(1f, 0.2f, 0.2f); // Red

            DrawAutoDismountRange(volume, localCenter, localHalfExtents, Vector3.forward, minHeight, maxHeight, colour);
            DrawAutoDismountRange(volume, localCenter, localHalfExtents, Vector3.back, minHeight, maxHeight, colour);
         }

         private static void DrawAutoDismountRange(
            LadderVolume volume,
            Vector3 localCenter,
            Vector3 localHalfExtents,
            Vector3 localNormal,
            float minHeight,
            float maxHeight,
            Color colour)
         {
            var faceZ = localNormal.z * (localHalfExtents.z + volume.MountDistance);
            var localMin = localCenter + new Vector3(0f, minHeight, faceZ);
            var localMax = localCenter + new Vector3(0f, maxHeight, faceZ);
            var halfHeight = Mathf.Max(localHalfExtents.y, 0.0001f);
            var minPercent = ((minHeight - localCenter.y) / halfHeight) * 100f;
            var maxPercent = ((maxHeight - localCenter.y) / halfHeight) * 100f;
            var labelOffset = localNormal * 0.3f;

            Gizmos.color = colour;
            DrawUniformSphere(localMin, 0.06f);
            DrawUniformSphere(localMax, 0.06f);

            Handles.Label(localMax + labelOffset, $"Auto Dismount Max: {maxPercent:F0}%");
            Handles.Label(localMin + labelOffset, $"Auto Dismount Min: {minPercent:F0}%");
         }

         private static void DrawUniformSphere(Vector3 localPosition, float radius)
         {
            var worldPosition = Gizmos.matrix.MultiplyPoint(localPosition);
            var prevMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.DrawSphere(worldPosition, radius);
            Gizmos.matrix = prevMatrix;
         }
      }
   }
#endif
