namespace TasEditor.Communication;

public enum ClientOpCode : byte {
    EstablishConnection = 0,
    SetInfoString = 1,
    CloseConnection = 2,
}

public enum ServerOpCode : byte {
    KeybindTriggered,
}

public enum TasKeybind : byte {
    StartStop = 0,
    FrameAdvance = 1,
    PauseResume = 2,

    ToggleHitboxes = 3,
}