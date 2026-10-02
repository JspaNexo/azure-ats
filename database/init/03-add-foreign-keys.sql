-- 1. candidate_cv_analyses -> candidates
ALTER TABLE candidate_cv_analyses
DROP CONSTRAINT IF EXISTS FK_cv_analyses_candidates,
ADD CONSTRAINT FK_cv_analyses_candidates
FOREIGN KEY (candidate_id) REFERENCES candidates(id) ON DELETE CASCADE;

-- 2. candidate_cv_analyses -> cv_documents
ALTER TABLE candidate_cv_analyses
DROP CONSTRAINT IF EXISTS FK_cv_analyses_documents,
ADD CONSTRAINT FK_cv_analyses_documents
FOREIGN KEY (document_id) REFERENCES cv_documents(id) ON DELETE CASCADE;

-- 3. candidate_disc_results -> candidates
ALTER TABLE candidate_disc_results
DROP CONSTRAINT IF EXISTS FK_disc_results_candidates,
ADD CONSTRAINT FK_disc_results_candidates
FOREIGN KEY (candidate_id) REFERENCES candidates(id) ON DELETE CASCADE;

-- 4. candidate_disc_interpretations -> candidates
ALTER TABLE candidate_disc_interpretations
DROP CONSTRAINT IF EXISTS FK_disc_interpretations_candidates,
ADD CONSTRAINT FK_disc_interpretations_candidates
FOREIGN KEY (candidate_id) REFERENCES candidates(id) ON DELETE CASCADE;

-- 5. candidate_disc_interpretations -> candidate_disc_results
ALTER TABLE candidate_disc_interpretations
DROP CONSTRAINT IF EXISTS FK_disc_interpretations_disc_results,
ADD CONSTRAINT FK_disc_interpretations_disc_results
FOREIGN KEY (disc_result_id) REFERENCES candidate_disc_results(id) ON DELETE CASCADE;

-- 6. candidate_interview_reports -> candidates
ALTER TABLE candidate_interview_reports
DROP CONSTRAINT IF EXISTS FK_interview_reports_candidates,
ADD CONSTRAINT FK_interview_reports_candidates
FOREIGN KEY (candidate_id) REFERENCES candidates(id) ON DELETE CASCADE;

-- 7. candidate_interview_reports -> candidate_cv_analyses
ALTER TABLE candidate_interview_reports
DROP CONSTRAINT IF EXISTS FK_interview_reports_cv_analyses,
ADD CONSTRAINT FK_interview_reports_cv_analyses
FOREIGN KEY (cv_analysis_id) REFERENCES candidate_cv_analyses(id) ON DELETE SET NULL;

-- 8. candidate_interview_reports -> candidate_disc_interpretations
ALTER TABLE candidate_interview_reports
DROP CONSTRAINT IF EXISTS FK_interview_reports_disc_interpretations,
ADD CONSTRAINT FK_interview_reports_disc_interpretations
FOREIGN KEY (disc_interpretation_id) REFERENCES candidate_disc_interpretations(id) ON DELETE SET NULL;

-- 9. processing_jobs -> candidates
ALTER TABLE processing_jobs
DROP CONSTRAINT IF EXISTS FK_processing_jobs_candidates,
ADD CONSTRAINT FK_processing_jobs_candidates
FOREIGN KEY (candidate_id) REFERENCES candidates(id) ON DELETE CASCADE;
