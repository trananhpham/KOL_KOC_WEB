using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KOL_KOC_TAAA.Migrations
{
    /// <inheritdoc />
    public partial class FixRegistrationConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Bước 1: Xóa ràng buộc UNIQUE cũ trên Phone (nếu tồn tại)
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.objects WHERE name = 'UQ__Users__5C7E359EE8FD8754' AND type = 'UQ')
                BEGIN
                    ALTER TABLE [dbo].[Users] DROP CONSTRAINT [UQ__Users__5C7E359EE8FD8754];
                END
            ");

            // Bước 2: Tạo Filtered Index mới cho Phone
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_Users_Phone_Filtered')
                BEGIN
                    CREATE UNIQUE INDEX [UQ_Users_Phone_Filtered] ON [dbo].[Users]([Phone]) WHERE [Phone] IS NOT NULL;
                END
            ");

            // Bước 3: Xóa ràng buộc CHECK trên InfluencerType (nếu tồn tại)
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK__KolProfil__Influ__49C3F6B7')
                BEGIN
                    ALTER TABLE [dbo].[KolProfiles] DROP CONSTRAINT [CK__KolProfil__Influ__49C3F6B7];
                END
            ");

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KolUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reviews_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reviews_KolProfiles_KolUserId",
                        column: x => x.KolUserId,
                        principalTable: "KolProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reviews_Users_CustomerUserId",
                        column: x => x.CustomerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_BookingId",
                table: "Reviews",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_CustomerUserId",
                table: "Reviews",
                column: "CustomerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_KolUserId",
                table: "Reviews",
                column: "KolUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.RenameIndex(
                name: "UQ_Users_Phone_Filtered",
                table: "Users",
                newName: "UQ__Users__5C7E359EE8FD8754");
        }
    }
}
