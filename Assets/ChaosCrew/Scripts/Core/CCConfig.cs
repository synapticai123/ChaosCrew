using UnityEngine;

namespace ChaosCrew
{
    /// <summary>
    /// Central tuning table. Everything a designer would want to tweak lives here so the
    /// systems stay free of magic numbers.
    /// </summary>
    public static class CCConfig
    {
        // --- Match shape -------------------------------------------------
        public const int PlayerCount = 4;
        public const int SaboteurCount = 1;
        public const int TasksPerCrew = 5;

        public const float RoundSeconds = 240f;
        public const float RoleRevealSeconds = 4.5f;
        public const float VotingSeconds = 25f;

        // --- Movement ----------------------------------------------------
        public const float WalkSpeed = 4.2f;
        public const float PlayerRadius = 0.34f;
        public const float SlipStunSeconds = 1.6f;
        public const float ClutterSlowFactor = 0.45f;
        public const float PuddleSlideFactor = 0.86f;

        // --- Interaction -------------------------------------------------
        public const float InteractRange = 1.15f;
        public const float DoorUnjamSeconds = 1.4f;

        // --- Chaos / progress --------------------------------------------
        public const float ChaosMax = 122f;
        public const float ChaosPerSlip = 2.5f;
        /// <summary>Finished work calms the office down, so tasks and sabotage pull against each other.</summary>
        public const float ChaosPerTaskDone = 3f;

        // --- Evidence ----------------------------------------------------
        public const float EvidenceSampleHz = 5f;
        public const float ClipHalfLength = 2.2f;
        public const int MaxClips = 3;
        public const float CameraGlitchSeconds = 10f;

        // --- Sprint / energy ---------------------------------------------
        public const float SprintMultiplier = 1.75f;
        public const float EnergyMax = 100f;
        public const float EnergyDrainPerSecond = 26f;
        public const float EnergyRegenPerSecond = 11f;
        /// <summary>Sprinting is locked out until energy climbs back past this.</summary>
        public const float EnergyResumeThreshold = 18f;
        public const float EnergyPerCoffee = 55f;

        // --- Carrying ----------------------------------------------------
        public const float CarrySpeedFactor = 0.78f;
        public const float CarryPickupRange = 1.5f;
        public const int CrateCount = 3;
        public const float DropZoneRadius = 1.6f;

        // --- Rendering ---------------------------------------------------
        /// <summary>Reference canvas resolution (portrait phone).</summary>
        public static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);
        /// <summary>Half-height of the isometric camera's view, in world units.</summary>
        public const float CameraSize = 9.0f;
        public const float CameraSizeSprint = 9.9f;

        public const float LightsOutViewRadius = 4.2f;

        // --- Bots --------------------------------------------------------
        public const float BotTaskSeconds = 18f;
        public const float BotFakeTaskSeconds = 8f;
        public const float BotRepathInterval = 0.75f;
    }
}
