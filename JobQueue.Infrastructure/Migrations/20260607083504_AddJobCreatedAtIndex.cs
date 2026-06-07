using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobQueue.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJobCreatedAtIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name:"IX_Jobs_Status",
                table:"Jobs",
                column:"Status");
            migrationBuilder.CreateIndex( 
                name: "IX_Jobs_CreatedAt", 
                table: "Jobs", 
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Jobs_Status",
                table: "Jobs");
            migrationBuilder.DropIndex(
                name: "IX_Jobs_CreatedAt",
                table: "Jobs");
        }
    }
}
