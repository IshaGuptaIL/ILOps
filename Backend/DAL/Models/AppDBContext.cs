using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Models
{
    public class AppDBContext :DbContext
    {
        public AppDBContext(DbContextOptions<AppDBContext>options):base(options) { }
        

        public DbSet<usermaster> usermaster { get; set; }
        public DbSet<userRole> userRole { get; set; }
        public DbSet<tblMan> tblMan { get; set; }

        public DbSet<usermastermenus> usermastermenus { get; set; }

        public DbSet<RolePermissions> RolePermissions { get; set; }
        public DbSet<hardwarereceived> hardwarereceived { get; set; }

        public DbSet<HPCExtract> HPCExtract { get; set; }

        public DbSet<HPCExtractSummary> HPCExtractSummary { get; set; }

        public DbSet<t_hardware> t_hardware { get; set; }






    }
}
