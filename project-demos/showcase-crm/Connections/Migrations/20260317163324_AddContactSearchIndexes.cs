using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShowcaseCrm.Connections.ShowcaseCrm.Migrations
{
    /// <inheritdoc />
    public partial class AddContactSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_contacts_Email",
                table: "contacts",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_contacts_FirstName",
                table: "contacts",
                column: "FirstName");

            migrationBuilder.CreateIndex(
                name: "IX_contacts_LastName",
                table: "contacts",
                column: "LastName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_contacts_Email",
                table: "contacts");

            migrationBuilder.DropIndex(
                name: "IX_contacts_FirstName",
                table: "contacts");

            migrationBuilder.DropIndex(
                name: "IX_contacts_LastName",
                table: "contacts");
        }
    }
}
