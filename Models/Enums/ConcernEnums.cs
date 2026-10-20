namespace MalapadCogonEConcern.Models.Enums;

public enum ConcernStatus
{
    Submitted = 1,
    UnderReview = 2,
    Accepted = 3,
    Assigned = 4,
    InProgress = 5,
    Resolved = 6,
    NeedsMoreInfo = 7,
    ForwardedToAgency = 8,
    Rejected = 9,
    Reopened = 10
}

public enum PriorityLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    Urgent = 4
}

public enum SubmissionChannel
{
    Online = 1,
    WalkIn = 2,
    PhoneCall = 3
}

public enum AttachmentType
{
    SubmissionPhoto = 1,
    ResolutionProof = 2,
    AgencyEndorsement = 3
}
