using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LilyMarket.Migrations
{
    /// <inheritdoc />
    public partial class AddAuctionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Auctions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Condition",
                table: "Auctions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CoverImageUrl",
                table: "Auctions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Auctions");

            migrationBuilder.DropColumn(
                name: "Condition",
                table: "Auctions");

            migrationBuilder.DropColumn(
                name: "CoverImageUrl",
                table: "Auctions");
        }
    }
}
