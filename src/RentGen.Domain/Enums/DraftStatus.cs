namespace RentGen.Domain.Enums;

public enum DraftStatus
{
    Draft = 1,
    InProgress = 2,
    ReadyForGeneration = 3,
    Generating = 4,
    Generated = 5,
    Failed = 6,
    Archived = 7
}
