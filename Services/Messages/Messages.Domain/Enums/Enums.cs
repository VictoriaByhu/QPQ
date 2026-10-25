namespace Messages.Domain.Enums;

public enum SwapStatus
{
    Accepted,
    Completed,
    Cancelled
}

public enum ParticipantRole
{
    Initiator,
    Partner
}

public enum SystemEvent
{
    SwapAccepted,
    SwapCompleted,
    SwapCancelled
}
