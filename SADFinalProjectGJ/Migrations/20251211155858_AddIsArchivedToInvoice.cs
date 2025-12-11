using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SADFinalProjectGJ.Migrations
{
    /// <inheritdoc />
    public partial class AddIsArchivedToInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Invoices",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Invoices");
        }
    }
}
