using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JwtCourseApi.Advanced.Migrations;

/// <inheritdoc />
public partial class InitialRefreshTokens : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "RefreshTokens",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                UserId = table.Column<string>(
                    type: "TEXT",
                    maxLength: 128,
                    nullable: false),
                TokenHash = table.Column<string>(
                    type: "TEXT",
                    maxLength: 64,
                    nullable: false),
                FamilyId = table.Column<Guid>(type: "TEXT", nullable: false),
                Transport = table.Column<string>(
                    type: "TEXT",
                    maxLength: 16,
                    nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                ExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                RevokedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                RevokedReason = table.Column<string>(
                    type: "TEXT",
                    maxLength: 32,
                    nullable: true),
                ReplacedByTokenId = table.Column<Guid>(type: "TEXT", nullable: true),
                Version = table.Column<Guid>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RefreshTokens", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_FamilyId",
            table: "RefreshTokens",
            column: "FamilyId");

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_TokenHash",
            table: "RefreshTokens",
            column: "TokenHash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_UserId",
            table: "RefreshTokens",
            column: "UserId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "RefreshTokens");
    }
}
