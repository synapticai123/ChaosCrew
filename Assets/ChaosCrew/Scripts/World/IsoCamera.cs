using UnityEngine;

namespace ChaosCrew
{
    /// <summary>
    /// Orthographic isometric camera that trails the local player. Framing is deliberately
    /// offset so the player sits slightly below centre, leaving room for the HUD up top.
    /// </summary>
    public sealed class IsoCamera : MonoBehaviour
    {
        public const float Pitch = 52f;
        public const float Yaw = 45f;

        private Camera _cam;
        private Vector3 _focus;
        private float _shake;
        private float _shakeTime;
        private float _zoom = CCConfig.CameraSize;

        public Camera Camera => _cam;

        public void Bind(Camera cam)
        {
            _cam = cam;
            _cam.orthographic = true;
            _cam.orthographicSize = CCConfig.CameraSize;
            _cam.nearClipPlane = 0.1f;
            _cam.farClipPlane = 160f;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = Palette.Hex("1A2338");
            _cam.cullingMask = ~0;
            _cam.transform.rotation = Quaternion.Euler(Pitch, Yaw, 0f);
        }

        public void SnapTo(Vector2 worldXZ)
        {
            _focus = new Vector3(worldXZ.x, 0f, worldXZ.y);
            Apply(true);
        }

        public void Shake(float amount, float seconds)
        {
            _shake = Mathf.Max(_shake, amount);
            _shakeTime = Mathf.Max(_shakeTime, seconds);
        }

        /// <summary>Pull back while sprinting so the player can see what they are running into.</summary>
        public void SetZoom(float size)
        {
            _zoom = size;
        }

        public void Follow(Vector2 worldXZ, float dt)
        {
            var target = new Vector3(worldXZ.x, 0f, worldXZ.y);
            _focus = Vector3.Lerp(_focus, target, 1f - Mathf.Exp(-9f * dt));

            if (_shakeTime > 0f)
            {
                _shakeTime -= dt;
                if (_shakeTime <= 0f) _shake = 0f;
            }

            _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, _zoom, 1f - Mathf.Exp(-4f * dt));
            Apply(false);
        }

        private void Apply(bool snap)
        {
            Vector3 dir = Quaternion.Euler(Pitch, Yaw, 0f) * Vector3.forward;
            Vector3 pos = _focus - dir * 40f;

            if (_shake > 0f && !snap)
            {
                float m = _shake * Mathf.Clamp01(_shakeTime * 3f);
                pos += new Vector3(Random.Range(-m, m), Random.Range(-m, m), Random.Range(-m, m));
            }

            _cam.transform.position = pos;
            _cam.transform.rotation = Quaternion.Euler(Pitch, Yaw, 0f);
        }

        /// <summary>Screen-space up/right mapped onto the ground plane, so the stick feels correct.</summary>
        public Vector2 StickToWorld(Vector2 stick)
        {
            float rad = Yaw * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            // Rotate the stick by the camera yaw: screen-up becomes "into" the scene.
            return new Vector2(stick.x * cos + stick.y * sin, -stick.x * sin + stick.y * cos);
        }
    }
}
