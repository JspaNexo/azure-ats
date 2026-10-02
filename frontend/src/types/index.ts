export interface SkillDto {
  name: string;
  normalizedName: string;
  category: string;
  experienceYears: number;
  evidence: string;
  confidence: number;
}

export interface LanguageDto {
  name: string;
  level: string;
  evidence: string;
}

export interface EducationDto {
  degree: string;
  institution: string;
  graduationYear?: number;
}

export interface CertificationDto {
  name: string;
  issuer: string;
  year?: number;
}

export interface WorkExperienceDto {
  role: string;
  company: string;
  durationYears: number;
  keyAchievements: string[];
}

export interface CvAnalysisDto {
  professionalSummary: string;
  currentRole: string;
  estimatedSeniority: string;
  totalExperienceYears: number;
  skills: SkillDto[];
  languages: LanguageDto[];
  education: EducationDto[];
  certifications: CertificationDto[];
  workExperience: WorkExperienceDto[];
  pointsToValidate: string[];
  warnings: string[];
}

export interface DiscInterpretationDto {
  primaryStyle: string;
  summary: string;
  strengthsToExplore: string[];
  pointsToExplore: string[];
  behavioralQuestionTopics: string[];
  disclaimer: string;
}

export interface CandidateOverviewDto {
  name: string;
  currentRole: string;
  experienceYears: number;
  professionalSummary: string;
}

export interface ProfessionalProfileDto {
  mainSkills: string[];
  relevantExperience: string[];
  education: string[];
  languages: string[];
  certifications: string[];
}

export interface DiscSummaryDto {
  primaryStyle: string;
  summary: string;
  strengthsToExplore: string[];
  pointsToExplore: string[];
}

export interface ValidationPointDto {
  topic: string;
  reason: string;
  source: string;
}

export interface InterviewGuideDto {
  professionalQuestions: string[];
  technicalQuestions: string[];
  behavioralQuestions: string[];
}

export interface InterviewReportDto {
  candidateOverview: CandidateOverviewDto;
  professionalProfile: ProfessionalProfileDto;
  discSummary: DiscSummaryDto;
  validationPoints: ValidationPointDto[];
  interviewGuide: InterviewGuideDto;
  disclaimer: string;
  fileUrl?: string;
}

export interface Candidate {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string;
  createdAtUtc: string;
  targetRole: string;
  seniority: string;
  experienceYears: number | null;
  matchScore: number | null;
  primaryDiscStyle: string;
  status: 'Registered' | 'CvAnalyzed' | 'DiscEvaluated' | 'ReportReady';
  cvAnalysis?: CvAnalysisDto | null;
  discInterpretation?: DiscInterpretationDto | null;
  report?: InterviewReportDto | null;
  evaluatorDecision?: 'Pending' | 'Approved' | 'Waitlisted' | 'Rejected' | string;
  evaluatorNotes?: string | null;
  evaluatedAtUtc?: string | null;
  assignedRecruiterId?: string | null;
  assignedRecruiterName?: string | null;
  assignedRecruiterEmail?: string | null;
  assignedAtUtc?: string | null;
  jobPositionId?: string | null;
  discScores?: {
    dominance: number;
    influence: number;
    steadiness: number;
    conscientiousness: number;
  } | null;
}

export interface JobPosition {
  id: string;
  title: string;
  department: string;
  seniority: string;
  minExperienceYears: number;
  description?: string;
  requirements?: string;
  status: 'Active' | 'Paused' | 'Closed';
  createdAtUtc: string;
  candidateCount?: number;
}

export interface Recruiter {
  id: string;
  fullName: string;
  email: string;
  role: string;
  activeAssignmentsCount: number;
}

export interface UserProfile {
  id: string;
  username: string;
  fullName: string;
  email: string;
  role: 'ats_admin' | 'ats_recruiter';
}
