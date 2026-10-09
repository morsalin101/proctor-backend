using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PROCTOR.Infrastructure.Data;

#nullable disable

namespace PROCTOR.Infrastructure.Migrations;

[DbContext(typeof(ProctorDbContext))]
[Migration("20261009100329_AddType3WorkflowAndInvestigation")]
public partial class AddType3WorkflowAndInvestigation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>("CanSend", "MenuPermissions", "boolean", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<bool>("IsConfidential", "Cases", "boolean", nullable: false, defaultValue: false);
        migrationBuilder.Sql("UPDATE \"Cases\" SET \"IsConfidential\" = TRUE, \"Type\" = 'Type2' WHERE \"Type\" = 'Confidential';");

        migrationBuilder.Sql("""
            CREATE TABLE "DisciplinaryResolutions" (
                "Id" uuid NOT NULL,
                "ResolutionNumber" text NOT NULL,
                "Status" text NOT NULL,
                "CreatedById" uuid NOT NULL,
                "CreatedByName" text NOT NULL,
                "ForwardRemarks" text NULL,
                "ApprovedById" uuid NULL,
                "ApprovedByName" text NULL,
                "ApprovedAt" timestamp with time zone NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_DisciplinaryResolutions" PRIMARY KEY ("Id")
            );
            CREATE UNIQUE INDEX "IX_DisciplinaryResolutions_ResolutionNumber"
                ON "DisciplinaryResolutions" ("ResolutionNumber");

            CREATE TABLE "InvestigationAttachments" (
                "Id" uuid NOT NULL,
                "CaseId" uuid NOT NULL,
                "Name" text NOT NULL,
                "Kind" text NOT NULL,
                "StorageName" text NULL,
                "ExternalUrl" text NULL,
                "ContentType" text NULL,
                "FileSize" bigint NULL,
                "UploadedById" uuid NOT NULL,
                "UploadedByName" text NOT NULL,
                "UploadedByRole" text NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_InvestigationAttachments" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_InvestigationAttachments_Cases_CaseId" FOREIGN KEY ("CaseId") REFERENCES "Cases" ("Id") ON DELETE CASCADE
            );
            CREATE INDEX "IX_InvestigationAttachments_CaseId" ON "InvestigationAttachments" ("CaseId");

            CREATE TABLE "Type3Workflows" (
                "Id" uuid NOT NULL,
                "CaseId" uuid NOT NULL,
                "CurrentStage" text NOT NULL,
                "StartedById" uuid NOT NULL,
                "StartedByName" text NOT NULL,
                "StartedAt" timestamp with time zone NOT NULL,
                "CompletedAt" timestamp with time zone NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_Type3Workflows" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_Type3Workflows_Cases_CaseId" FOREIGN KEY ("CaseId") REFERENCES "Cases" ("Id") ON DELETE CASCADE
            );
            CREATE UNIQUE INDEX "IX_Type3Workflows_CaseId" ON "Type3Workflows" ("CaseId");

            CREATE TABLE "DisciplinaryResolutionCases" (
                "Id" uuid NOT NULL,
                "ResolutionId" uuid NOT NULL,
                "CaseId" uuid NOT NULL,
                "DisplayOrder" integer NOT NULL,
                "ShortDescription" text NOT NULL,
                "SecretaryRemarks" text NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_DisciplinaryResolutionCases" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_DisciplinaryResolutionCases_Cases_CaseId" FOREIGN KEY ("CaseId") REFERENCES "Cases" ("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_DisciplinaryResolutionCases_DisciplinaryResolutions_ResolutionId" FOREIGN KEY ("ResolutionId") REFERENCES "DisciplinaryResolutions" ("Id") ON DELETE CASCADE
            );
            CREATE UNIQUE INDEX "IX_DisciplinaryResolutionCases_CaseId" ON "DisciplinaryResolutionCases" ("CaseId");
            CREATE INDEX "IX_DisciplinaryResolutionCases_ResolutionId" ON "DisciplinaryResolutionCases" ("ResolutionId");

            CREATE TABLE "DcMemberRemarks" (
                "Id" uuid NOT NULL,
                "WorkflowId" uuid NOT NULL,
                "MemberUserId" uuid NOT NULL,
                "MemberName" text NOT NULL,
                "Content" text NULL,
                "SubmittedAt" timestamp with time zone NULL,
                "LockedAt" timestamp with time zone NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_DcMemberRemarks" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_DcMemberRemarks_Type3Workflows_WorkflowId" FOREIGN KEY ("WorkflowId") REFERENCES "Type3Workflows" ("Id") ON DELETE CASCADE
            );
            CREATE UNIQUE INDEX "IX_DcMemberRemarks_WorkflowId_MemberUserId" ON "DcMemberRemarks" ("WorkflowId", "MemberUserId");

            CREATE TABLE "Type3WorkflowTransitions" (
                "Id" uuid NOT NULL,
                "WorkflowId" uuid NOT NULL,
                "FromStage" text NOT NULL,
                "ToStage" text NOT NULL,
                "ActorUserId" uuid NOT NULL,
                "ActorName" text NOT NULL,
                "ActorRole" text NOT NULL,
                "TargetRole" text NOT NULL,
                "Remarks" text NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_Type3WorkflowTransitions" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_Type3WorkflowTransitions_Type3Workflows_WorkflowId" FOREIGN KEY ("WorkflowId") REFERENCES "Type3Workflows" ("Id") ON DELETE CASCADE
            );
            CREATE UNIQUE INDEX "IX_Type3WorkflowTransitions_WorkflowId_FromStage_ToStage"
                ON "Type3WorkflowTransitions" ("WorkflowId", "FromStage", "ToStage");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE IF EXISTS "DcMemberRemarks";
            DROP TABLE IF EXISTS "DisciplinaryResolutionCases";
            DROP TABLE IF EXISTS "InvestigationAttachments";
            DROP TABLE IF EXISTS "Type3WorkflowTransitions";
            DROP TABLE IF EXISTS "DisciplinaryResolutions";
            DROP TABLE IF EXISTS "Type3Workflows";
            """);
        migrationBuilder.DropColumn("CanSend", "MenuPermissions");
        migrationBuilder.DropColumn("IsConfidential", "Cases");
    }
}
