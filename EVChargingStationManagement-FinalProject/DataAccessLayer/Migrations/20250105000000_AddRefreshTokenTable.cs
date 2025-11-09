using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if table already exists before creating
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS tbl_refresh_token (
                    ""Id"" uuid NOT NULL,
                    ""Token"" text NOT NULL,
                    ""UserId"" uuid NOT NULL,
                    ""ExpiresAt"" timestamp with time zone NOT NULL,
                    ""IsRevoked"" boolean NOT NULL,
                    ""RevokedAt"" timestamp with time zone,
                    ""CreatedAt"" timestamp with time zone NOT NULL,
                    ""UpdatedAt"" timestamp with time zone NOT NULL,
                    CONSTRAINT ""PK_tbl_refresh_token"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_tbl_refresh_token_tbl_user_UserId"" FOREIGN KEY (""UserId"") REFERENCES tbl_user (""Id"") ON DELETE CASCADE
                );
            ");

            // Create indexes if they don't exist
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_tbl_refresh_token_Token"" ON tbl_refresh_token (""Token"");
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_tbl_refresh_token_Token_Unique"" ON tbl_refresh_token (""Token"") WHERE ""Token"" IS NOT NULL;
                CREATE INDEX IF NOT EXISTS ""IX_tbl_refresh_token_UserId"" ON tbl_refresh_token (""UserId"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_refresh_token");
        }
    }
}





