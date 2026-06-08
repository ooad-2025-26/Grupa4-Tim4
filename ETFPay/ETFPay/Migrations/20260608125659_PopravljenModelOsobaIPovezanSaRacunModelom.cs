using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETFPay.Migrations
{
    /// <inheritdoc />
    public partial class PopravljenModelOsobaIPovezanSaRacunModelom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateOnly>(
                name: "DatumZaposlenja",
                table: "AspNetUsers",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Plata",
                table: "AspNetUsers",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Racun",
                table: "AspNetUsers",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_Racun",
                table: "AspNetUsers",
                column: "Racun",
                unique: true,
                filter: "[Racun] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Racun_Racun",
                table: "AspNetUsers",
                column: "Racun",
                principalTable: "Racun",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Racun_Racun",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_Racun",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DatumZaposlenja",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Plata",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Racun",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
