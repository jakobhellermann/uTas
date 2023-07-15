namespace TasEditor.Communication;

public enum RequestOpCode : byte {
    EstablishConnection = 0,
    SetInfoString = 1,
    CloseConnection = 2,
}

public enum ResponseOpCode : byte {
}