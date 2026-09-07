ALTER TABLE candidates ADD COLUMN IF NOT EXISTS "EvaluatorDecision" character varying(50) NOT NULL DEFAULT 'Pending';
ALTER TABLE candidates ADD COLUMN IF NOT EXISTS "EvaluatorNotes" text;
ALTER TABLE candidates ADD COLUMN IF NOT EXISTS "EvaluatedAtUtc" timestamp with time zone;

