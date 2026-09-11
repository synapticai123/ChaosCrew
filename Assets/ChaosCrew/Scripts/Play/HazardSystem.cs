using System.Collections.Generic;
using UnityEngine;

namespace ChaosCrew
{
    public sealed class Hazard
    {
        public HazardKind Kind;
        public Vector2 Position;
        public float Radius;
        public float Lifetime;
        public int OwnerId;
        /// <summary>Set when a one-shot hazard has fired and should be cleaned up.</summary>
        public bool Consumed;
        public float Spin;
    }

    /// <summary>
    /// Owns the harmless-but-annoying props the saboteur leaves behind. Effects are applied
    /// per frame to whoever walks into them; the placer is immune so sabotage stays deniable.
    /// </summary>
    public sealed class HazardSystem
    {
        public readonly List<Hazard> Hazards = new List<Hazard>();

        private MatchState _state;
        private EvidenceRecorder _evidence;
        private SfxSynth _sfx;

        public System.Action<PlayerState, Hazard> OnPlayerSlipped;

        public void Bind(MatchState state, EvidenceRecorder evidence, SfxSynth sfx)
        {
            _state = state;
            _evidence = evidence;
            _sfx = sfx;
            Hazards.Clear();
        }

        public Hazard Spawn(HazardKind kind, Vector2 pos, int ownerId)
        {
            var h = new Hazard
            {
                Kind = kind,
                Position = pos,
                OwnerId = ownerId,
                Spin = (Hazards.Count * 47f) % 360f
            };

            switch (kind)
            {
                case HazardKind.Banana:
                    h.Radius = 0.42f;
                    h.Lifetime = 45f;
                    break;
                case HazardKind.Puddle:
                    h.Radius = 0.75f;
                    h.Lifetime = 30f;
                    break;
                case HazardKind.Clutter:
                    h.Radius = 0.55f;
                    h.Lifetime = 22f;
                    break;
            }

            Hazards.Add(h);
            return h;
        }

        public void Clear() => Hazards.Clear();

        public void Tick(float dt)
        {
            for (int i = Hazards.Count - 1; i >= 0; i--)
            {
                Hazard h = Hazards[i];
                h.Lifetime -= dt;
                if (h.Lifetime <= 0f || h.Consumed)
                {
                    Hazards.RemoveAt(i);
                    continue;
                }

                for (int p = 0; p < _state.Players.Count; p++)
                {
                    PlayerState player = _state.Players[p];
                    if (player.OwnerImmune(h)) continue;
                    float d = Vector2.Distance(player.Position, h.Position);
                    if (d > h.Radius + CCConfig.PlayerRadius * 0.7f) continue;

                    switch (h.Kind)
                    {
                        case HazardKind.Banana:
                            Slip(player, h);
                            h.Consumed = true;
                            break;
                        case HazardKind.Puddle:
                            // Water does not stun, it makes you skid: control is halved and
                            // momentum carries you past where you meant to stop.
                            player.SpeedMultiplier *= 1.12f;
                            player.SlideVelocity = Vector2.Lerp(player.SlideVelocity,
                                player.Facing * CCConfig.WalkSpeed, 1f - CCConfig.PuddleSlideFactor);
                            break;
                        case HazardKind.Clutter:
                            player.SpeedMultiplier *= CCConfig.ClutterSlowFactor;
                            break;
                    }
                }
            }
        }

        private void Slip(PlayerState player, Hazard h)
        {
            player.StunTimer = Mathf.Max(player.StunTimer, CCConfig.SlipStunSeconds);
            player.BusyStationId = null;
            player.SlideVelocity = Vector2.zero;
            _state.AddChaos(CCConfig.ChaosPerSlip);
            _evidence?.LogEvent(EvidenceEventKind.PlayerSlipped, player.Id, player.Position,
                player.DisplayName + " rutscht aus");
            if (player.IsLocal) _sfx?.Play(Sfx.Slip);
            OnPlayerSlipped?.Invoke(player, h);
        }

        public Hazard NearestHazard(Vector2 p, HazardKind kind, float maxDist)
        {
            Hazard best = null;
            float bestD = maxDist;
            for (int i = 0; i < Hazards.Count; i++)
            {
                if (Hazards[i].Kind != kind) continue;
                float d = Vector2.Distance(Hazards[i].Position, p);
                if (d < bestD)
                {
                    bestD = d;
                    best = Hazards[i];
                }
            }
            return best;
        }
    }

    internal static class HazardExtensions
    {
        /// <summary>The player who placed a hazard walks over it unharmed.</summary>
        public static bool OwnerImmune(this PlayerState player, Hazard h) => player.Id == h.OwnerId;
    }
}
