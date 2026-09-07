-- Migration: Add Recruiter Assignment Columns to Candidates
ALTER TABLE candidates
ADD COLUMN IF NOT EXISTS AssignedRecruiterId VARCHAR(100) NULL,
ADD COLUMN IF NOT EXISTS AssignedRecruiterName VARCHAR(150) NULL,
ADD COLUMN IF NOT EXISTS AssignedRecruiterEmail VARCHAR(150) NULL,
ADD COLUMN IF NOT EXISTS AssignedAtUtc TIMESTAMPTZ NULL;

CREATE INDEX IF NOT EXISTS ix_candidates_assigned_recruiter ON candidates (AssignedRecruiterId);

-- Initial sample assignment for testing:
-- Assign Mateo Morales to Carlos Mendoza (recruiter1)
UPDATE candidates 
SET AssignedRecruiterId = 'carlos.mendoza',
    AssignedRecruiterName = 'Carlos Mendoza',
    AssignedRecruiterEmail = 'carlos.mendoza@empresa.com',
    AssignedAtUtc = NOW()
WHERE Email = 'mateo.morales.tech@example.com';

-- Assign Sofia Vergara / Valentina to Laura Sánchez (recruiter2)
UPDATE candidates 
SET AssignedRecruiterId = 'laura.sanchez',
    AssignedRecruiterName = 'Laura Sánchez',
    AssignedRecruiterEmail = 'laura.sanchez@empresa.com',
    AssignedAtUtc = NOW()
WHERE Email LIKE 'valentina%' OR Email LIKE 'sofia%';
