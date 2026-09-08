using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ats.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "job_positions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    department = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    seniority = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Senior"),
                    min_experience_years = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    description = table.Column<string>(type: "text", nullable: true),
                    requirements = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Active"),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_positions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "candidates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    EvaluatorDecision = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Pending"),
                    EvaluatorNotes = table.Column<string>(type: "text", nullable: true),
                    EvaluatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    assignedrecruiterid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    assignedrecruitername = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    assignedrecruiteremail = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    assignedatutc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    target_role = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    job_position_id = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candidates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_candidates_job_positions_job_position_id",
                        column: x => x.job_position_id,
                        principalTable: "job_positions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "candidate_disc_results",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    dominance = table.Column<int>(type: "integer", nullable: false),
                    influence = table.Column<int>(type: "integer", nullable: false),
                    steadiness = table.Column<int>(type: "integer", nullable: false),
                    conscientiousness = table.Column<int>(type: "integer", nullable: false),
                    primary_style = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candidate_disc_results", x => x.Id);
                    table.ForeignKey(
                        name: "FK_candidate_disc_results_candidates_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "candidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cv_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UploadedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cv_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cv_documents_candidates_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "candidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "processing_jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ErrorCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processing_jobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_processing_jobs_candidates_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "candidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "candidate_disc_interpretations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscResultId = table.Column<Guid>(type: "uuid", nullable: false),
                    InterpretationJson = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ProviderName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ModelName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PromptVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candidate_disc_interpretations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_candidate_disc_interpretations_candidate_disc_results_DiscR~",
                        column: x => x.DiscResultId,
                        principalTable: "candidate_disc_results",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_candidate_disc_interpretations_candidates_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "candidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "candidate_cv_analyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnalysisJson = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ProviderName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ModelName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PromptVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FeedbackJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candidate_cv_analyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_candidate_cv_analyses_candidates_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "candidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_candidate_cv_analyses_cv_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "cv_documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "candidate_interview_reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    CvAnalysisId = table.Column<Guid>(type: "uuid", nullable: true),
                    DiscInterpretationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReportContentJson = table.Column<string>(type: "jsonb", nullable: false),
                    FileUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ProviderName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ModelName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PromptVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    GeneratedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candidate_interview_reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_candidate_interview_reports_candidate_cv_analyses_CvAnalysi~",
                        column: x => x.CvAnalysisId,
                        principalTable: "candidate_cv_analyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_candidate_interview_reports_candidate_disc_interpretations_~",
                        column: x => x.DiscInterpretationId,
                        principalTable: "candidate_disc_interpretations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_candidate_interview_reports_candidates_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "candidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_candidate_cv_analyses_CandidateId",
                table: "candidate_cv_analyses",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_candidate_cv_analyses_DocumentId",
                table: "candidate_cv_analyses",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_candidate_disc_interpretations_CandidateId",
                table: "candidate_disc_interpretations",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_candidate_disc_interpretations_DiscResultId",
                table: "candidate_disc_interpretations",
                column: "DiscResultId");

            migrationBuilder.CreateIndex(
                name: "IX_candidate_disc_results_CandidateId",
                table: "candidate_disc_results",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_candidate_interview_reports_CandidateId",
                table: "candidate_interview_reports",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_candidate_interview_reports_CvAnalysisId",
                table: "candidate_interview_reports",
                column: "CvAnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_candidate_interview_reports_DiscInterpretationId",
                table: "candidate_interview_reports",
                column: "DiscInterpretationId");

            migrationBuilder.CreateIndex(
                name: "IX_candidates_email",
                table: "candidates",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_candidates_job_position_id",
                table: "candidates",
                column: "job_position_id");

            migrationBuilder.CreateIndex(
                name: "IX_cv_documents_CandidateId",
                table: "cv_documents",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_job_positions_status",
                table: "job_positions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_processing_jobs_CandidateId",
                table: "processing_jobs",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_processing_jobs_CorrelationId",
                table: "processing_jobs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_processing_jobs_EventId",
                table: "processing_jobs",
                column: "EventId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "candidate_interview_reports");

            migrationBuilder.DropTable(
                name: "processing_jobs");

            migrationBuilder.DropTable(
                name: "candidate_cv_analyses");

            migrationBuilder.DropTable(
                name: "candidate_disc_interpretations");

            migrationBuilder.DropTable(
                name: "cv_documents");

            migrationBuilder.DropTable(
                name: "candidate_disc_results");

            migrationBuilder.DropTable(
                name: "candidates");

            migrationBuilder.DropTable(
                name: "job_positions");
        }
    }
}
