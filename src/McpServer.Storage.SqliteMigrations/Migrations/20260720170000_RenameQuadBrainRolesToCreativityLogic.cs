using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.SqliteMigrations.Migrations
{
    /// <inheritdoc />
    public partial class RenameQuadBrainRolesToCreativityLogic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "BrainSlotDefinitions"
                SET "Role" = 'Creativity',
                    "PartyId" = 'brain-slot:creativity',
                    "SlotId" = REPLACE("SlotId", 'left-hemisphere', 'creativity')
                WHERE "Role" = 'LeftHemisphere';
                """);
            migrationBuilder.Sql("""
                UPDATE "BrainSlotDefinitions"
                SET "Role" = 'Logic',
                    "PartyId" = 'brain-slot:logic',
                    "SlotId" = REPLACE("SlotId", 'right-hemisphere', 'logic')
                WHERE "Role" = 'RightHemisphere';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "BrainSlotDefinitions"
                SET "Role" = 'LeftHemisphere',
                    "PartyId" = 'brain-slot:left-hemisphere',
                    "SlotId" = REPLACE("SlotId", 'creativity', 'left-hemisphere')
                WHERE "Role" = 'Creativity';
                """);
            migrationBuilder.Sql("""
                UPDATE "BrainSlotDefinitions"
                SET "Role" = 'RightHemisphere',
                    "PartyId" = 'brain-slot:right-hemisphere',
                    "SlotId" = REPLACE("SlotId", 'logic', 'right-hemisphere')
                WHERE "Role" = 'Logic';
                """);
        }
    }
}
