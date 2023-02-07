using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DooProject.Migrations
{
    /// <inheritdoc />
    public partial class SQLiteMigration1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductLookUps",
                columns: table => new
                {
                    ProductID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductLookUps", x => x.ProductID);
                });

            migrationBuilder.CreateTable(
                name: "ProductTransections",
                columns: table => new
                {
                    TransectionID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TransectionAmount = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductID = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductTransections", x => x.TransectionID);
                    table.ForeignKey(
                        name: "FK_ProductTransections_ProductLookUps_ProductID",
                        column: x => x.ProductID,
                        principalTable: "ProductLookUps",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductTransections_ProductID",
                table: "ProductTransections",
                column: "ProductID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductTransections");

            migrationBuilder.DropTable(
                name: "ProductLookUps");
        }
    }
}
