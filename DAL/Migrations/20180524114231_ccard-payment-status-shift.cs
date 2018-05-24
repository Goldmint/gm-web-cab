using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class ccardpaymentstatusshift : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
	        migrationBuilder.Sql("update gm_ccard_payment set status=status+1 where 1=1");
		}

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
