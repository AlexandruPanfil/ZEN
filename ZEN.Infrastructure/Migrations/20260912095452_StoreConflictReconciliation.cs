using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZEN.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StoreConflictReconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConflictRecordJson",
                table: "SyncOutboxItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ConflictVersion",
                table: "SyncOutboxItems",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConflictRecordJson",
                table: "SyncOutboxItems");

            migrationBuilder.DropColumn(
                name: "ConflictVersion",
                table: "SyncOutboxItems");
        }
    }
}
