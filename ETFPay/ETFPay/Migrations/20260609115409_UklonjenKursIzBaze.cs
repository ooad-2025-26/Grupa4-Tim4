using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETFPay.Migrations
{
    /// <inheritdoc />
    public partial class UklonjenKursIzBaze : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Kurs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Kurs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IznosZaJedanUSD = table.Column<double>(type: "float", nullable: false),
                    Valuta = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VrijemeAzuriranja = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kurs", x => x.Id);
                });
        }
    }
}
