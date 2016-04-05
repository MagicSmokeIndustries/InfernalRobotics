using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

namespace InfernalRobotics.Gui
{

    public class UIAnimationHelper : MonoBehaviour
    {
        public delegate void SetHeightDelegate(float newHeight);
        public delegate void SetPositionDelegate(Vector2 newPosition);

        public SetHeightDelegate SetHeight;
        public SetPositionDelegate SetPosition;

        private IEnumerator _AnimateHeightCoroutine;
        private IEnumerator _AnimatePositionCoroutine;
        
        public bool isHeightActive {
            get { return _AnimateHeightCoroutine != null; }
        }

        public bool isPositionActive
        {
            get { return _AnimatePositionCoroutine != null; }
        }

        public void AnimateHeight(float from, float to, float duration, Action callback = null)
        {
            if (_AnimateHeightCoroutine != null)
            {
                StopCoroutine(_AnimateHeightCoroutine);
            }

            _AnimateHeightCoroutine = AnimateHeightCoroutine(from, to, duration, callback);
            StartCoroutine(_AnimateHeightCoroutine);
        }

        private IEnumerator AnimateHeightCoroutine(float from, float to, float duration, Action callback)
        {
            // wait for end of frame so that only the last call to fade that frame is honoured.
            yield return new WaitForEndOfFrame();

            float progress = 0.0f;

            while (progress <= 1.0f)
            {
                progress += Time.deltaTime / duration;

                SetHeight(Mathf.Lerp(from, to, progress));

                yield return null;
            }

            if (callback != null)
                callback.Invoke();

            _AnimateHeightCoroutine = null;
        }

        public void AnimatePosition(Vector2 from, Vector2 to, float duration, Action callback = null)
        {
            if (_AnimatePositionCoroutine != null)
            {
                StopCoroutine(_AnimatePositionCoroutine);
            }

            _AnimatePositionCoroutine = AnimatePositionCoroutine(from, to, duration, callback);
            StartCoroutine(_AnimatePositionCoroutine);
        }



        private IEnumerator AnimatePositionCoroutine(Vector2 from, Vector2 to, float duration, Action callback)
        {
            // wait for end of frame so that only the last call to fade that frame is honoured.
            yield return new WaitForEndOfFrame();

            float progress = 0.0f;

            while (progress <= 1.0f)
            {
                progress += Time.deltaTime / duration;

                Vector2 newPosition = new Vector2(Mathf.Lerp(from.x, to.x, progress), Mathf.Lerp(from.y, to.y, progress));
                SetPosition(newPosition);

                yield return null;
            }

            if (callback != null)
                callback.Invoke();

            _AnimatePositionCoroutine = null;
        }

        public void StopHeight()
        {
            StopCoroutine(_AnimateHeightCoroutine);
        }

        public void StopPosition()
        {
            StopCoroutine(_AnimatePositionCoroutine);
        }

    }
}