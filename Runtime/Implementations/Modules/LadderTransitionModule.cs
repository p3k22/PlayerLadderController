namespace P3k.PlayerLadderController.Implementations.Modules
{
   using System;

   using UnityEngine;

   internal sealed class LadderTransitionModule
   {
      private Transform _target;
      private Vector3 _startPosition;
      private Vector3 _endPosition;
      private Quaternion _startRotation;
      private Quaternion _endRotation;
      private float _duration;
      private float _elapsed;
      private Action _onComplete;

      internal bool IsPlaying { get; private set; }

      internal void Play(
         Transform target,
         Vector3 endPosition,
         Quaternion endRotation,
         float duration,
         Action onComplete)
      {
         if (IsPlaying)
         {
            return;
         }

         if (!target)
         {
            return;
         }

         if (duration <= 0f)
         {
            target.SetPositionAndRotation(endPosition, endRotation);
            onComplete?.Invoke();
            return;
         }

         _target = target;
         _startPosition = target.position;
         _startRotation = target.rotation;
         _endPosition = endPosition;
         _endRotation = endRotation;
         _duration = duration;
         _elapsed = 0f;
         _onComplete = onComplete;
         IsPlaying = true;
      }

      internal void Stop()
      {
         IsPlaying = false;
         _target = null;
         _onComplete = null;
      }

      internal void Tick(float deltaTime)
      {
         if (!IsPlaying || !_target)
         {
            return;
         }

         _elapsed += deltaTime;
         var t = Mathf.Clamp01(_elapsed / _duration);

         _target.SetPositionAndRotation(
            Vector3.Lerp(_startPosition, _endPosition, t),
            Quaternion.Slerp(_startRotation, _endRotation, t));

         if (_elapsed < _duration)
         {
            return;
         }

         _target.SetPositionAndRotation(_endPosition, _endRotation);
         IsPlaying = false;
         _target = null;
         var callback = _onComplete;
         _onComplete = null;
         callback?.Invoke();
      }
   }
}
