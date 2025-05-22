using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class OwnedShuttle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "owned_shuttles",
                columns: table => new
                {
                    shuttle_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    player_user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    shuttle_prototype_id = table.Column<string>(type: "TEXT", nullable: false),
                    shuttle_name = table.Column<string>(type: "TEXT", nullable: false),
                    shuttle_description = table.Column<string>(type: "TEXT", nullable: true),
                    shuttle_save_price = table.Column<int>(type: "INTEGER", nullable: false),
                    shuttle_path = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_owned_shuttles", x => x.shuttle_id);
                    table.ForeignKey(
                        name: "FK_owned_shuttles_player_player_user_id",
                        column: x => x.player_user_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_owned_shuttles_player_user_id",
                table: "owned_shuttles",
                column: "player_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "owned_shuttles");
        }
    }
}
