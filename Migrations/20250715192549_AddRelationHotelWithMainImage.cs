using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Final_Project.Migrations
{
    /// <inheritdoc />
    public partial class AddRelationHotelWithMainImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MainImageId",
                table: "Hotels",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Hotels_MainImageId",
                table: "Hotels",
                column: "MainImageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Hotels_HotelImages_MainImageId",
                table: "Hotels",
                column: "MainImageId",
                principalTable: "HotelImages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Hotels_HotelImages_MainImageId",
                table: "Hotels");

            migrationBuilder.DropIndex(
                name: "IX_Hotels_MainImageId",
                table: "Hotels");

            migrationBuilder.DropColumn(
                name: "MainImageId",
                table: "Hotels");
        }
    }
}
