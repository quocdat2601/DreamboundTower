using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Map
{
    public class ScrollNonUI : MonoBehaviour
    {
        public float tweenBackDuration = 0.3f;
        public bool freezeX;
        public FloatMinMax xConstraints = new FloatMinMax();
        public bool freezeY;
        public FloatMinMax yConstraints = new FloatMinMax();
        private Vector2 offset;
        // last mouse position in world space while dragging
        private Vector3 lastWorldMousePosition;
        private float zDisplacement;
        private bool dragging;
        private Camera mainCamera;

        private void Awake()
        {
            mainCamera = Camera.main;
            zDisplacement = -mainCamera.transform.position.z + transform.position.z;
        }

        private void Update()
        {
            if (IsPointerDownThisFrame())
            {
                dragging = true;
                lastWorldMousePosition = MouseInWorldCoords();
            }

            if (IsPointerUpThisFrame())
            {
                dragging = false;
                TweenBack();
            }

            if (!dragging) return;

            Vector3 currentWorldMouse = MouseInWorldCoords();
            Vector3 delta = currentWorldMouse - lastWorldMousePosition;
            transform.position = new Vector3(
                freezeX ? transform.position.x : transform.position.x + delta.x,
                freezeY ? transform.position.y : transform.position.y + delta.y,
                transform.position.z);
            lastWorldMousePosition = currentWorldMouse;
        }

        private bool IsPointerDownThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }

        private bool IsPointerUpThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
#else
            return Input.GetMouseButtonUp(0);
#endif
        }

        // returns mouse position in World coordinates for our GameObject to follow. 
        private Vector3 MouseInWorldCoords()
        {
            Vector3 screenMousePos;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
                screenMousePos = Mouse.current.position.ReadValue();
            else
                screenMousePos = Input.mousePosition;
#else
            screenMousePos = Input.mousePosition;
#endif
            //Debug.Log(screenMousePos);
            screenMousePos.z = zDisplacement;
            return mainCamera.ScreenToWorldPoint(screenMousePos);
        }

        private void TweenBack()
        {
            if (freezeY)
            {
                if (transform.localPosition.x >= xConstraints.min && transform.localPosition.x <= xConstraints.max)
                    return;

                float targetX = transform.localPosition.x < xConstraints.min ? xConstraints.min : xConstraints.max;
                // Simple instant move without animation
                Vector3 pos = transform.localPosition;
                pos.x = targetX;
                transform.localPosition = pos;
            }
            else if (freezeX)
            {
                if (transform.localPosition.y >= yConstraints.min && transform.localPosition.y <= yConstraints.max)
                    return;

                float targetY = transform.localPosition.y < yConstraints.min ? yConstraints.min : yConstraints.max;
                // Simple instant move without animation
                Vector3 pos = transform.localPosition;
                pos.y = targetY;
                transform.localPosition = pos;
            }
        }
    }
}
