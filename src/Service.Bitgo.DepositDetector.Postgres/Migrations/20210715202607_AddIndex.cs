using Microsoft.EntityFrameworkCore.Migrations;

namespace Service.Bitgo.DepositDetector.Postgres.Migrations
{
    public partial class AddIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_deposits_TransactionId",
                schema: "deposits",
                table: "deposits",
                column: "TransactionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_deposits_TransactionId",
                schema: "deposits",
                table: "deposits");
        }
    }
}
