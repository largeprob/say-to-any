using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqlBoTx.Net.DbManager.Migrations
{
    /// <inheritdoc />
    public partial class m1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "organization",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false, comment: "ID")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "NVARCHAR(255)", nullable: false, comment: "组织名称"),
                    parent_id = table.Column<long>(type: "bigint", nullable: true, comment: "父组织ID"),
                    description = table.Column<string>(type: "NVARCHAR(255)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization", x => x.id);
                    table.ForeignKey(
                        name: "FK_organization_organization_parent_id",
                        column: x => x.parent_id,
                        principalTable: "organization",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "组织结构");

            migrationBuilder.CreateTable(
                name: "organization_role",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false, comment: "主键自增ID")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "NVARCHAR(255)", nullable: false, comment: "角色名称"),
                    description = table.Column<string>(type: "NVARCHAR(255)", nullable: true, comment: "角色说明"),
                    sort_order = table.Column<int>(type: "int", nullable: false, comment: "排序号"),
                    is_active = table.Column<bool>(type: "bit", nullable: false, comment: "是否启用"),
                    role_type = table.Column<int>(type: "int", nullable: false, comment: "角色类型"),
                    created_date = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "创建时间")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_role", x => x.id);
                },
                comment: "组织角色");

            migrationBuilder.CreateTable(
                name: "organization_user",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false, comment: "主键自增ID")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_name = table.Column<string>(type: "NVARCHAR(255)", nullable: false, comment: "用户显示名称，如：张三"),
                    login_account = table.Column<string>(type: "NVARCHAR(255)", nullable: false, comment: "登录账号"),
                    password = table.Column<string>(type: "NVARCHAR(255)", nullable: false, comment: "登录密码"),
                    email = table.Column<string>(type: "NVARCHAR(255)", nullable: true, comment: "用户邮箱"),
                    admin = table.Column<bool>(type: "bit", nullable: false, comment: "系统用户"),
                    organization_id = table.Column<long>(type: "bigint", nullable: false, comment: "所属组织ID"),
                    organization_role_id = table.Column<long>(type: "bigint", nullable: false, comment: "所属角色ID"),
                    is_active = table.Column<bool>(type: "bit", nullable: false, comment: "账号是否启用"),
                    last_login_date = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "最后一次登录时间"),
                    created_date = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "账号创建时间")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_user", x => x.id);
                    table.ForeignKey(
                        name: "FK_organization_user_organization_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organization",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_organization_user_organization_role_organization_role_id",
                        column: x => x.organization_role_id,
                        principalTable: "organization_role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "问数系统用户");

            migrationBuilder.CreateTable(
                name: "refresh_token",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false, comment: "主键自增ID")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<long>(type: "bigint", nullable: false, comment: "用户ID"),
                    token_hash = table.Column<string>(type: "NVARCHAR(255)", nullable: false, comment: "令牌哈希值"),
                    jwt_id = table.Column<string>(type: "NVARCHAR(255)", nullable: false, comment: "关联的JWT ID"),
                    is_revoked = table.Column<bool>(type: "bit", nullable: false, comment: "是否已撤销"),
                    expires_at = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "过期时间"),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "创建时间")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_token", x => x.id);
                    table.ForeignKey(
                        name: "FK_refresh_token_organization_user_user_id",
                        column: x => x.user_id,
                        principalTable: "organization_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "刷新令牌");

            migrationBuilder.CreateIndex(
                name: "IX_organization_parent_id",
                table: "organization",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "IX_organization_user_organization_id",
                table: "organization_user",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_organization_user_organization_role_id",
                table: "organization_user",
                column: "organization_role_id");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_token_user_id",
                table: "refresh_token",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "refresh_token");

            migrationBuilder.DropTable(
                name: "organization_user");

            migrationBuilder.DropTable(
                name: "organization");

            migrationBuilder.DropTable(
                name: "organization_role");
        }
    }
}
