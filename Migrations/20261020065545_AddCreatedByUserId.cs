using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MalapadCogonEConcern.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedByUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "Concerns",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Concerns_CreatedByUserId",
                table: "Concerns",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Concerns_AspNetUsers_CreatedByUserId",
                table: "Concerns",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Concerns_AspNetUsers_CreatedByUserId",
                table: "Concerns");

            migrationBuilder.DropIndex(
                name: "IX_Concerns_CreatedByUserId",
                table: "Concerns");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Concerns");
        }
    }
}
