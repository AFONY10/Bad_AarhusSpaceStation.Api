using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AarhusSpaceProgram.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffApplicationUserLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Scientists",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Managers",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Astronauts",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Scientists_ApplicationUserId",
                table: "Scientists",
                column: "ApplicationUserId",
                unique: true,
                filter: "[ApplicationUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Managers_ApplicationUserId",
                table: "Managers",
                column: "ApplicationUserId",
                unique: true,
                filter: "[ApplicationUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Astronauts_ApplicationUserId",
                table: "Astronauts",
                column: "ApplicationUserId",
                unique: true,
                filter: "[ApplicationUserId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Astronauts_AspNetUsers_ApplicationUserId",
                table: "Astronauts",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Managers_AspNetUsers_ApplicationUserId",
                table: "Managers",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Scientists_AspNetUsers_ApplicationUserId",
                table: "Scientists",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Astronauts_AspNetUsers_ApplicationUserId",
                table: "Astronauts");

            migrationBuilder.DropForeignKey(
                name: "FK_Managers_AspNetUsers_ApplicationUserId",
                table: "Managers");

            migrationBuilder.DropForeignKey(
                name: "FK_Scientists_AspNetUsers_ApplicationUserId",
                table: "Scientists");

            migrationBuilder.DropIndex(
                name: "IX_Scientists_ApplicationUserId",
                table: "Scientists");

            migrationBuilder.DropIndex(
                name: "IX_Managers_ApplicationUserId",
                table: "Managers");

            migrationBuilder.DropIndex(
                name: "IX_Astronauts_ApplicationUserId",
                table: "Astronauts");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Scientists");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Managers");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Astronauts");
        }
    }
}
