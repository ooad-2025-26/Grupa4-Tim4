using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETFPay.Migrations
{
    /// <inheritdoc />
    public partial class DodatnaOgranicenjaNaRacunModelu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "brojRacuna",
                table: "Racun",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "IBAN",
                table: "Racun",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Racun_brojRacuna",
                table: "Racun",
                column: "brojRacuna",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Racun_IBAN",
                table: "Racun",
                column: "IBAN",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Racun_brojRacuna",
                table: "Racun");

            migrationBuilder.DropIndex(
                name: "IX_Racun_IBAN",
                table: "Racun");

            migrationBuilder.AlterColumn<string>(
                name: "brojRacuna",
                table: "Racun",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "IBAN",
                table: "Racun",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
