using UnityEngine;

namespace ChaosCrew
{
    /// <summary>
    /// A stylised office worker built from primitives, animated procedurally: leg and arm
    /// swing, body bob, lean into the walk, plus carry and dazed poses. No animation clips,
    /// no rigged mesh — it reads clearly at the isometric camera distance.
    /// </summary>
    public sealed class CharacterRig : MonoBehaviour
    {
        private static readonly Color[] SkinTones =
        {
            Palette.Hex("F1C9A5"), Palette.Hex("D9A277"), Palette.Hex("A97248"), Palette.Hex("F6D8BC")
        };

        private static readonly Color[] HairTones =
        {
            Palette.Hex("3A2418"), Palette.Hex("8B4A22"), Palette.Hex("1E1A18"), Palette.Hex("C97F3A")
        };

        public PlayerState Player { get; private set; }

        private Transform _rig;
        private Transform _torso;
        private Transform _head;
        private Transform _armL;
        private Transform _armR;
        private Transform _legL;
        private Transform _legR;
        private Transform _ring;
        private Transform _carrySlot;
        private Transform _carried;
        private Transform _dazed;

        private float _phase;
        private float _lean;

        public Transform Head => _head;

        public static CharacterRig Create(Transform parent, PlayerState player, bool isLocal)
        {
            var go = new GameObject("Char_" + player.DisplayName);
            go.transform.SetParent(parent, false);
            var rig = go.AddComponent<CharacterRig>();
            rig.Build(player, isLocal);
            return rig;
        }

        private void Build(PlayerState player, bool isLocal)
        {
            Player = player;

            Color shirt = player.Color;
            Color pants = Palette.Darken(Palette.Hex("32405C"), player.ColorIndex * 0.05f);
            Color skin = SkinTones[player.ColorIndex % SkinTones.Length];
            Color hair = HairTones[player.ColorIndex % HairTones.Length];

            _rig = PropKit.Group(transform, "Rig", Vector3.zero);

            // Legs are pivoted at the hip so a plain rotation reads as a stride.
            _legL = PropKit.Group(_rig, "LegL", new Vector3(-0.11f, 0.42f, 0f));
            PropKit.Part(_legL, new Vector3(0f, -0.21f, 0f), new Vector3(0.16f, 0.44f, 0.17f), pants, 0.15f);
            PropKit.Part(_legL, new Vector3(0f, -0.45f, -0.04f), new Vector3(0.18f, 0.09f, 0.27f),
                Pal3D.PaperWhite, 0.25f);

            _legR = PropKit.Group(_rig, "LegR", new Vector3(0.11f, 0.42f, 0f));
            PropKit.Part(_legR, new Vector3(0f, -0.21f, 0f), new Vector3(0.16f, 0.44f, 0.17f), pants, 0.15f);
            PropKit.Part(_legR, new Vector3(0f, -0.45f, -0.04f), new Vector3(0.18f, 0.09f, 0.27f),
                Pal3D.PaperWhite, 0.25f);

            _torso = PropKit.Group(_rig, "Torso", new Vector3(0f, 0.42f, 0f));
            PropKit.Part(_torso, new Vector3(0f, 0.26f, 0f), new Vector3(0.42f, 0.52f, 0.26f), shirt, 0.16f);
            // Collar and a lanyard badge, which is what sells "office worker" at this size.
            PropKit.Part(_torso, new Vector3(0f, 0.5f, -0.02f), new Vector3(0.3f, 0.06f, 0.24f),
                Palette.Lighten(shirt, 0.25f), 0.16f);
            PropKit.Part(_torso, new Vector3(0.06f, 0.3f, -0.14f), new Vector3(0.1f, 0.13f, 0.02f),
                Pal3D.PaperWhite, 0.2f);

            _armL = PropKit.Group(_torso, "ArmL", new Vector3(-0.25f, 0.46f, 0f));
            PropKit.Part(_armL, new Vector3(0f, -0.18f, 0f), new Vector3(0.12f, 0.38f, 0.13f), shirt, 0.16f);
            PropKit.Sphere(_armL, new Vector3(0f, -0.39f, 0f), 0.13f, skin, 0.2f);

            _armR = PropKit.Group(_torso, "ArmR", new Vector3(0.25f, 0.46f, 0f));
            PropKit.Part(_armR, new Vector3(0f, -0.18f, 0f), new Vector3(0.12f, 0.38f, 0.13f), shirt, 0.16f);
            PropKit.Sphere(_armR, new Vector3(0f, -0.39f, 0f), 0.13f, skin, 0.2f);

            _head = PropKit.Group(_torso, "Head", new Vector3(0f, 0.63f, 0f));
            PropKit.Sphere(_head, Vector3.zero, 0.34f, skin, 0.18f);
            // Hair: a cap plus a bun or fringe so the four characters read apart.
            PropKit.Sphere(_head, new Vector3(0f, 0.08f, 0f), 0.35f, hair, 0.2f);
            if (player.ColorIndex % 2 == 0)
                PropKit.Sphere(_head, new Vector3(0f, 0.16f, 0.2f), 0.2f, hair, 0.2f);
            else
                PropKit.Part(_head, new Vector3(0f, 0.1f, -0.17f), new Vector3(0.34f, 0.2f, 0.14f), hair, 0.2f);

            // Eyes face -Z, which is the rig's forward.
            PropKit.Sphere(_head, new Vector3(-0.1f, 0.02f, -0.28f), 0.11f, Color.white, 0.3f);
            PropKit.Sphere(_head, new Vector3(0.1f, 0.02f, -0.28f), 0.11f, Color.white, 0.3f);
            PropKit.Sphere(_head, new Vector3(-0.1f, 0.02f, -0.33f), 0.055f, Pal3D.PlasticDark, 0.4f);
            PropKit.Sphere(_head, new Vector3(0.1f, 0.02f, -0.33f), 0.055f, Pal3D.PlasticDark, 0.4f);

            _carrySlot = PropKit.Group(_torso, "CarrySlot", new Vector3(0f, 0.3f, -0.42f));

            _dazed = PropKit.Group(transform, "Dazed", new Vector3(0f, 1.72f, 0f));
            for (int i = 0; i < 3; i++)
            {
                float a = i * Mathf.PI * 2f / 3f;
                PropKit.Sphere(_dazed, new Vector3(Mathf.Cos(a) * 0.24f, 0f, Mathf.Sin(a) * 0.24f), 0.1f,
                    Pal3D.Yellow, 0.3f, 2f);
            }
            _dazed.gameObject.SetActive(false);

            if (isLocal)
            {
                _ring = PropLibrary.SelectionRing(transform, 1.05f, Pal3D.Cyan);
            }

            PropKit.SetShadows(transform, true);
        }

        /// <summary>Puts a prop in the character's hands; pass null to clear it.</summary>
        public void SetCarried(Transform prop)
        {
            if (_carried != null && _carried != prop) Destroy(_carried.gameObject);
            _carried = prop;
            if (prop == null) return;
            prop.SetParent(_carrySlot, false);
            prop.localPosition = Vector3.zero;
            prop.localRotation = Quaternion.identity;
        }

        public void Tick(float dt, bool visible)
        {
            if (Player == null) return;

            if (gameObject.activeSelf != visible) gameObject.SetActive(visible);
            if (!visible) return;

            transform.position = new Vector3(Player.Position.x, 0f, Player.Position.y);

            Vector3 forward = new Vector3(Player.Facing.x, 0f, Player.Facing.y);
            if (forward.sqrMagnitude > 0.0001f)
            {
                // The rig models forward as -Z, so look the other way round.
                Quaternion want = Quaternion.LookRotation(-forward.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, want, 1f - Mathf.Exp(-14f * dt));
            }

            bool stunned = Player.IsStunned;
            bool busy = !string.IsNullOrEmpty(Player.BusyStationId);
            float speed01 = Mathf.Clamp01(Player.Speed / CCConfig.WalkSpeed);
            bool moving = speed01 > 0.08f && !stunned;

            _phase += dt * (moving ? 7f + speed01 * 5f : 2.5f);

            float swing = moving ? Mathf.Sin(_phase) * (0.55f + speed01 * 0.5f) : 0f;
            float idle = Mathf.Sin(_phase * 0.6f) * 0.05f;

            _legL.localRotation = Quaternion.Euler(swing * Mathf.Rad2Deg * 0.55f, 0f, 0f);
            _legR.localRotation = Quaternion.Euler(-swing * Mathf.Rad2Deg * 0.55f, 0f, 0f);

            if (_carried != null)
            {
                // Both arms come up to hold the load, so only the legs keep swinging.
                _armL.localRotation = Quaternion.Euler(-95f, 0f, 12f);
                _armR.localRotation = Quaternion.Euler(-95f, 0f, -12f);
            }
            else if (busy)
            {
                float work = Mathf.Sin(_phase * 2.2f) * 12f;
                _armL.localRotation = Quaternion.Euler(-70f + work, 0f, 14f);
                _armR.localRotation = Quaternion.Euler(-70f - work, 0f, -14f);
            }
            else
            {
                _armL.localRotation = Quaternion.Euler(-swing * Mathf.Rad2Deg * 0.45f, 0f, 8f + idle * 20f);
                _armR.localRotation = Quaternion.Euler(swing * Mathf.Rad2Deg * 0.45f, 0f, -8f - idle * 20f);
            }

            float bob = moving ? Mathf.Abs(Mathf.Sin(_phase)) * 0.07f * speed01 : idle * 0.4f;
            _rig.localPosition = new Vector3(0f, bob, 0f);

            float wantLean = stunned ? 0f : (moving ? 7f * speed01 : 0f);
            _lean = Mathf.Lerp(_lean, wantLean, 1f - Mathf.Exp(-8f * dt));
            _torso.localRotation = Quaternion.Euler(_lean, 0f, 0f);

            if (stunned)
            {
                _rig.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 18f) * 14f);
                _head.localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * 9f) * 22f, 0f);
            }
            else
            {
                _rig.localRotation = Quaternion.identity;
                _head.localRotation = Quaternion.Euler(0f, Mathf.Sin(_phase * 0.4f) * 5f, 0f);
            }

            if (_dazed.gameObject.activeSelf != stunned) _dazed.gameObject.SetActive(stunned);
            if (stunned) _dazed.localRotation = Quaternion.Euler(0f, Time.time * 260f, 0f);

            if (_ring != null)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 3.4f) * 0.06f;
                _ring.localScale = new Vector3(pulse, 1f, pulse);
                _ring.localRotation = Quaternion.Euler(0f, Time.time * 26f, 0f);
            }
        }
    }
}
