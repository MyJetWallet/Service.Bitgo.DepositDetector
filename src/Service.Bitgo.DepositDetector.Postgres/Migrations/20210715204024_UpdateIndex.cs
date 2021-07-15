using Microsoft.EntityFrameworkCore.Migrations;

namespace Service.Bitgo.DepositDetector.Postgres.Migrations
{
    public partial class UpdateIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_deposits_TransactionId",
                schema: "deposits",
                table: "deposits");

            migrationBuilder.CreateIndex(
                name: "IX_deposits_TransactionId",
                schema: "deposits",
                table: "deposits",
                column: "TransactionId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_deposits_TransactionId",
                schema: "deposits",
                table: "deposits");

            migrationBuilder.CreateIndex(
                name: "IX_deposits_TransactionId",
                schema: "deposits",
                table: "deposits",
                column: "TransactionId");
        }
    }
}
