using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using _3K.Infrastructure.Data;

#nullable disable

namespace _3K.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260714095000_AddSandikAmbalajKarari")]
    public partial class AddSandikAmbalajKarari : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AmbalajaDahilMi",
                table: "Sandiklar",
                type: "boolean",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmbalajaDahilMi",
                table: "Sandiklar");
        }
    }
}