using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETFPay.Migrations
{
    /// <inheritdoc />
    public partial class DodanaSvrhaUplateNaTransakciju : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SvrhaUplate",
                table: "Transakcija",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SvrhaUplate",
                table: "Transakcija");
        }
    }
}
