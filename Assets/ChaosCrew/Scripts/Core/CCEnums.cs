namespace ChaosCrew
{
    /// <summary>High level flow of a match. The director owns exactly one of these at a time.</summary>
    public enum GamePhase
    {
        Boot,
        Loading,
        Title,
        Settings,
        Lobby,
        RoleReveal,
        Round,
        Evidence,
        Voting,
        Result
    }

    public enum Role
    {
        Crew,
        Saboteur
    }

    /// <summary>Kind of touch mini game a task uses. Add new values plus a factory entry to extend.</summary>
    public enum MiniGameKind
    {
        Wires,
        HoldGauge,
        TimingBar,
        Mash,
        Keypad,
        TapItems
    }

    /// <summary>Which pictogram the HUD shows for a task. Mirrors the mock-up's task list.</summary>
    public enum TaskIcon
    {
        Document,
        Coffee,
        Box
    }

    public enum SabotageKind
    {
        BananaPeel,
        LightsOut,
        JamDoors,
        MoveObject,
        PrinterFrenzy,
        ElevatorHijack,
        WaterSpill,
        CameraGlitch,
        FalseTrail
    }

    public enum HazardKind
    {
        Banana,
        Puddle,
        Clutter
    }

    /// <summary>Anything worth writing into the security camera log.</summary>
    public enum EvidenceEventKind
    {
        TaskCompleted,
        FakeTask,
        SabotageTriggered,
        HazardPlaced,
        PlayerSlipped,
        DoorJammed
    }

    public enum RoundEndReason
    {
        TimeUp,
        TasksCompleted,
        ChaosMaxed
    }
}
