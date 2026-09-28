using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace yggdrasil.Modules.Games.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOAuthClients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "client_id",
                schema: "games",
                table: "games",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "server_client_id",
                schema: "games",
                table: "games",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_games_client_id",
                schema: "games",
                table: "games",
                column: "client_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_games_server_client_id",
                schema: "games",
                table: "games",
                column: "server_client_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_games_client_id",
                schema: "games",
                table: "games");

            migrationBuilder.DropIndex(
                name: "ix_games_server_client_id",
                schema: "games",
                table: "games");

            migrationBuilder.DropColumn(
                name: "client_id",
                schema: "games",
                table: "games");

            migrationBuilder.DropColumn(
                name: "server_client_id",
                schema: "games",
                table: "games");
        }
    }
}
