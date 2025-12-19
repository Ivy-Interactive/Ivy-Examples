using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AutodealerCrm.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CallDirections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DescriptionText = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CallDirections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FirstName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    LastName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    ViberId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    WhatsappId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    TelegramId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeadIntents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DescriptionText = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadIntents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeadStages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DescriptionText = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadStages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MessageChannels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DescriptionText = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageChannels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MessageDirections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DescriptionText = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageDirections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MessageTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DescriptionText = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SourceChannels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DescriptionText = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceChannels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DescriptionText = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VehicleStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DescriptionText = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    UserRoleId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_users_UserRoles_UserRoleId",
                        column: x => x.UserRoleId,
                        principalTable: "UserRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "leads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CustomerId = table.Column<int>(type: "INTEGER", nullable: false),
                    ManagerId = table.Column<int>(type: "INTEGER", nullable: true),
                    SourceChannelId = table.Column<int>(type: "INTEGER", nullable: false),
                    LeadIntentId = table.Column<int>(type: "INTEGER", nullable: false),
                    LeadStageId = table.Column<int>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_leads_LeadIntents_LeadIntentId",
                        column: x => x.LeadIntentId,
                        principalTable: "LeadIntents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_leads_LeadStages_LeadStageId",
                        column: x => x.LeadStageId,
                        principalTable: "LeadStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_leads_SourceChannels_SourceChannelId",
                        column: x => x.SourceChannelId,
                        principalTable: "SourceChannels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_leads_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_leads_users_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vehicles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Make = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Model = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Vin = table.Column<string>(type: "TEXT", maxLength: 17, nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", nullable: false),
                    VehicleStatusId = table.Column<int>(type: "INTEGER", nullable: false),
                    ManagerId = table.Column<int>(type: "INTEGER", nullable: true),
                    ErpSyncId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vehicles_VehicleStatuses_VehicleStatusId",
                        column: x => x.VehicleStatusId,
                        principalTable: "VehicleStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_vehicles_users_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "call_records",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LeadId = table.Column<int>(type: "INTEGER", nullable: true),
                    CustomerId = table.Column<int>(type: "INTEGER", nullable: false),
                    ManagerId = table.Column<int>(type: "INTEGER", nullable: true),
                    CallDirectionId = table.Column<int>(type: "INTEGER", nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Duration = table.Column<int>(type: "INTEGER", nullable: true),
                    RecordingUrl = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    ScriptScore = table.Column<decimal>(type: "TEXT", nullable: true),
                    Sentiment = table.Column<decimal>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_call_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_call_records_CallDirections_CallDirectionId",
                        column: x => x.CallDirectionId,
                        principalTable: "CallDirections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_call_records_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_call_records_leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_call_records_users_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LeadId = table.Column<int>(type: "INTEGER", nullable: false),
                    ManagerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    DueDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Completed = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tasks_leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tasks_users_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "lead_vehicles",
                columns: table => new
                {
                    LeadId = table.Column<int>(type: "INTEGER", nullable: false),
                    VehicleId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lead_vehicles", x => new { x.LeadId, x.VehicleId });
                    table.ForeignKey(
                        name: "FK_lead_vehicles_leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lead_vehicles_vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "media",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    FileType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    VehicleId = table.Column<int>(type: "INTEGER", nullable: true),
                    LeadId = table.Column<int>(type: "INTEGER", nullable: true),
                    CustomerId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_media", x => x.Id);
                    table.ForeignKey(
                        name: "FK_media_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_media_leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_media_vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "messages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LeadId = table.Column<int>(type: "INTEGER", nullable: true),
                    CustomerId = table.Column<int>(type: "INTEGER", nullable: false),
                    ManagerId = table.Column<int>(type: "INTEGER", nullable: true),
                    MessageChannelId = table.Column<int>(type: "INTEGER", nullable: false),
                    MessageDirectionId = table.Column<int>(type: "INTEGER", nullable: false),
                    MessageTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: true),
                    MediaId = table.Column<int>(type: "INTEGER", nullable: true),
                    SentAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_messages_MessageChannels_MessageChannelId",
                        column: x => x.MessageChannelId,
                        principalTable: "MessageChannels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_messages_MessageDirections_MessageDirectionId",
                        column: x => x.MessageDirectionId,
                        principalTable: "MessageDirections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_messages_MessageTypes_MessageTypeId",
                        column: x => x.MessageTypeId,
                        principalTable: "MessageTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_messages_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_messages_leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_messages_media_MediaId",
                        column: x => x.MediaId,
                        principalTable: "media",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_messages_users_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "CallDirections",
                columns: new[] { "Id", "DescriptionText" },
                values: new object[,]
                {
                    { 1, "Inbound" },
                    { 2, "Outbound" }
                });

            migrationBuilder.InsertData(
                table: "LeadIntents",
                columns: new[] { "Id", "DescriptionText" },
                values: new object[,]
                {
                    { 1, "Buy" },
                    { 2, "Sell" },
                    { 3, "Service Inquiry" },
                    { 4, "Other" }
                });

            migrationBuilder.InsertData(
                table: "LeadStages",
                columns: new[] { "Id", "DescriptionText" },
                values: new object[,]
                {
                    { 1, "New" },
                    { 2, "Contacted" },
                    { 3, "Qualified" },
                    { 4, "In Negotiation" },
                    { 5, "Prepayment" },
                    { 6, "Sold" },
                    { 7, "Lost" }
                });

            migrationBuilder.InsertData(
                table: "MessageChannels",
                columns: new[] { "Id", "DescriptionText" },
                values: new object[,]
                {
                    { 1, "Viber" },
                    { 2, "WhatsApp" },
                    { 3, "Telegram" },
                    { 4, "Email" }
                });

            migrationBuilder.InsertData(
                table: "MessageDirections",
                columns: new[] { "Id", "DescriptionText" },
                values: new object[,]
                {
                    { 1, "Incoming" },
                    { 2, "Outgoing" }
                });

            migrationBuilder.InsertData(
                table: "MessageTypes",
                columns: new[] { "Id", "DescriptionText" },
                values: new object[,]
                {
                    { 1, "Text" },
                    { 2, "Photo" },
                    { 3, "File" },
                    { 4, "Voice Note" }
                });

            migrationBuilder.InsertData(
                table: "SourceChannels",
                columns: new[] { "Id", "DescriptionText" },
                values: new object[,]
                {
                    { 1, "Viber" },
                    { 2, "WhatsApp" },
                    { 3, "Telegram" },
                    { 4, "Call" },
                    { 5, "Email" }
                });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "Id", "DescriptionText" },
                values: new object[,]
                {
                    { 1, "Manager" },
                    { 2, "Supervisor" },
                    { 3, "Admin" },
                    { 4, "Analyst" }
                });

            migrationBuilder.InsertData(
                table: "VehicleStatuses",
                columns: new[] { "Id", "DescriptionText" },
                values: new object[,]
                {
                    { 1, "Available" },
                    { 2, "Reserved" },
                    { 3, "Sold" },
                    { 4, "Archived" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_call_records_CallDirectionId",
                table: "call_records",
                column: "CallDirectionId");

            migrationBuilder.CreateIndex(
                name: "IX_call_records_CustomerId",
                table: "call_records",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_call_records_LeadId",
                table: "call_records",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_call_records_ManagerId",
                table: "call_records",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_lead_vehicles_VehicleId",
                table: "lead_vehicles",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_leads_CustomerId",
                table: "leads",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_leads_LeadIntentId",
                table: "leads",
                column: "LeadIntentId");

            migrationBuilder.CreateIndex(
                name: "IX_leads_LeadStageId",
                table: "leads",
                column: "LeadStageId");

            migrationBuilder.CreateIndex(
                name: "IX_leads_ManagerId",
                table: "leads",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_leads_SourceChannelId",
                table: "leads",
                column: "SourceChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_media_CustomerId",
                table: "media",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_media_LeadId",
                table: "media",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_media_VehicleId",
                table: "media",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_messages_CustomerId",
                table: "messages",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_messages_LeadId",
                table: "messages",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_messages_ManagerId",
                table: "messages",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_messages_MediaId",
                table: "messages",
                column: "MediaId");

            migrationBuilder.CreateIndex(
                name: "IX_messages_MessageChannelId",
                table: "messages",
                column: "MessageChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_messages_MessageDirectionId",
                table: "messages",
                column: "MessageDirectionId");

            migrationBuilder.CreateIndex(
                name: "IX_messages_MessageTypeId",
                table: "messages",
                column: "MessageTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_tasks_LeadId",
                table: "tasks",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_tasks_ManagerId",
                table: "tasks",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_users_UserRoleId",
                table: "users",
                column: "UserRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_ManagerId",
                table: "vehicles",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_VehicleStatusId",
                table: "vehicles",
                column: "VehicleStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_Vin",
                table: "vehicles",
                column: "Vin",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "call_records");

            migrationBuilder.DropTable(
                name: "lead_vehicles");

            migrationBuilder.DropTable(
                name: "messages");

            migrationBuilder.DropTable(
                name: "tasks");

            migrationBuilder.DropTable(
                name: "CallDirections");

            migrationBuilder.DropTable(
                name: "MessageChannels");

            migrationBuilder.DropTable(
                name: "MessageDirections");

            migrationBuilder.DropTable(
                name: "MessageTypes");

            migrationBuilder.DropTable(
                name: "media");

            migrationBuilder.DropTable(
                name: "leads");

            migrationBuilder.DropTable(
                name: "vehicles");

            migrationBuilder.DropTable(
                name: "LeadIntents");

            migrationBuilder.DropTable(
                name: "LeadStages");

            migrationBuilder.DropTable(
                name: "SourceChannels");

            migrationBuilder.DropTable(
                name: "customers");

            migrationBuilder.DropTable(
                name: "VehicleStatuses");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "UserRoles");
        }
    }
}
