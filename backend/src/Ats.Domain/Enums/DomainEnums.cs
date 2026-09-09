namespace Ats.Domain.Enums;

public enum ProcessingStatus
{
    Pending = 1,
    Processing = 2,
    Processed = 3,
    Failed = 4
}

public enum ReportStatus
{
    WaitingForCv = 1,
    WaitingForDisc = 2,
    WaitingForAssessment = 2,
    ReadyToGenerate = 3,
    Generating = 4,
    Generated = 5,
    Failed = 6
}

public enum ProcessType
{
    CvAnalysis = 1,
    DiscInterpretation = 2,
    AssessmentInterpretation = 2,
    InterviewReportGeneration = 3
}

