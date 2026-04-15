using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _3K.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "StokKayitlari",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "StokKayitlari",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "StokHareketleri",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "StokHareketleri",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Sandiklar",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Sandiklar",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "SandikIcerikleri",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "SandikIcerikleri",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Revizyonlar",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Revizyonlar",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ProjeTransferleri",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "ProjeTransferleri",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Projeler",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Projeler",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Kullanicilar",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Kullanicilar",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "HareketGecmisleri",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "HareketGecmisleri",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "FBTransferleri",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "FBTransferleri",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "CekiSatirlari",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "CekiSatirlari",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Cekiler",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Cekiler",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "StokKayitlari");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "StokKayitlari");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "StokHareketleri");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "StokHareketleri");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Sandiklar");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Sandiklar");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "SandikIcerikleri");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "SandikIcerikleri");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Revizyonlar");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Revizyonlar");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ProjeTransferleri");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "ProjeTransferleri");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Projeler");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Projeler");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "HareketGecmisleri");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "HareketGecmisleri");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "FBTransferleri");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "FBTransferleri");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "CekiSatirlari");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "CekiSatirlari");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Cekiler");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Cekiler");
        }
    }
}
