using DAL.Inventory.SpareLight;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Models
{
    public class AppDBContext : DbContext
    {
        public AppDBContext(DbContextOptions<AppDBContext> options) : base(options) { }


        public DbSet<usermaster> usermaster { get; set; }
        public DbSet<userRole> userRole { get; set; }
        public DbSet<tblMan> tblMan { get; set; }

        public DbSet<usermastermenus> usermastermenus { get; set; }

        public DbSet<RolePermissions> RolePermissions { get; set; }
        public DbSet<hardwarereceived> hardwarereceived { get; set; }

        public DbSet<HPCExtract> HPCExtract { get; set; }

        public DbSet<HPCExtractSummary> HPCExtractSummary { get; set; }

        public DbSet<t_hardware> t_hardware { get; set; }
        public DbSet<tblScanList> tblScanList { get; set; }

        public DbSet<TblPackingSlip> TblPackingSlip { get; set; }

        public DbSet<tblSettingsApi> tblSettingsApi { get; set; }
        public DbSet<tblAPILog> tblAPILog { get; set; }
        public DbSet<tblSettings> tblSettings { get; set; }
        public DbSet<tblErrors> tblErrors { get; set; }
        public DbSet<tblIMEILengthExceptions> tblIMEILengthExceptions { get; set; }
        public DbSet<WWAccessories> WWAccessories { get; set; }

        public DbSet<WWInventory> WWInventory { get; set; }

        public DbSet<WWSerialNumber> WWSerialNumber { get; set; }
        public DbSet<tblCounts> tblCounts { get; set; }
        public DbSet<WWSalesDetailTEMP> WWSalesDetailTEMP { get; set; }
        public DbSet<tbIACCBckOrders> tbIACCBckOrders { get; set; }
        public DbSet<tblACCCounts> tblACCCounts { get; set; }
        public DbSet<tblIMEICountDuplicates> tblIMEICountDuplicates { get; set; }
        public DbSet<IMEIStatus> IMEIStatus { get; set; }
        public DbSet<tblACCBackOrders> tblACCBackOrders { get; set; }
        public DbSet<tblOpeningBalanceACC> tblOpeningBalanceACC { get; set; }
        public DbSet<tblInvoiceList> tblInvoiceList { get; set; }
        public DbSet<tblSalesActivations> tblSalesActivations { get; set; }
        public DbSet<SalesActivations> SalesActivations { get; set; }

        public DbSet<SalesActivationsDetail> SalesActivationsDetail { get; set; }

        public DbSet<tblSpireInvoice> tblSpireInvoice { get; set; }

        public DbSet<tblOnhandIMEIs> tblOnhandIMEIs { get; set; }

        public DbSet<tbllastpoitem> tbllastpoitem { get; set; }




        public DbSet<tblTransferList> tblTransferList { get; set; }
        public DbSet<tblTransferListACC> tblTransferListACC { get; set; }
        public DbSet<tblTransferLog> tblTransferLog { get; set; }


        public DbSet<RogersAR> RogersAR { get; set; }
        public DbSet<RogersARData> RogersARData { get; set; }

        public DbSet<AdvantageVoiceImport> AdvantageVoiceImports { get; set; }




        public DbSet<tblSKU> tblSKU { get; set; }
        public DbSet<tblAdvantageSettings> tblAdvantageSettings { get; set; }
        public DbSet<dbo_t_orderimport> dbo_t_orderimport { get; set; }





        //public DbSet<public_sales_history> public_sales_history { get; set; } // test
        //public DbSet<HISTORY_ADDRESS> HISTORY_ADDRESS { get; set; }
        public DbSet<tblBulkChangeList> tblBulkChangeList { get; set; }



        // Sales Tax Report Tables
        public DbSet<TaxCodeHistory> TaxCodeHistory { get; set; }
        public DbSet<TblTaxDataOutput> tblTaxDataOutput { get; set; }
        public DbSet<TblGLTransToTaxAccounts> tblGLTransToTaxAccounts { get; set; }
        public DbSet<tblTaxAccounts> tblTaxAccounts { get; set; }
        public DbSet<WWGLTrans> WWGLTrans { get; set; }
        public DbSet<Tbl21410Summary> tbl21410Summary { get; set; }

        public DbSet<TblCustomerGroups> tblCustomerGroups { get; set; }
        public DbSet<TblCustomerColumns> tblCustomerColumns { get; set; }
        public DbSet<TblCustomerSalesOutput> tblCustomerSalesOutput { get; set; }

        // ARCollections Tables
        public DbSet<TblEventTypes> tblEventTypes { get; set; }
        public DbSet<TblRootCauses> tblRootCauses { get; set; }
        public DbSet<TblTerritoryGroups> tblTerritoryGroups { get; set; }
        public DbSet<TblAllowedAccounts> tblAllowedAccounts { get; set; }
        public DbSet<TblEvents> tblEvents { get; set; }
        public DbSet<TblEventTrans> tblEventTrans { get; set; }
        public DbSet<TblARDetailExtra> tblARDetailExtra { get; set; }
        public DbSet<TblBulkCustomers> tblBulkCustomers { get; set; }
        public DbSet<TblCustomerGroupsRR> tblCustomerGroupsRR { get; set; }
        public DbSet<TblCustomersOpen> tblCustomersOpen { get; set; }
        public DbSet<TblARDetailView> ARDetailView { get; set; }
        public DbSet<TblARDetailViewFull> tblARDetailViewFull { get; set; }
        public DbSet<TblActivationsLookup> tblActivationsLookup { get; set; }
        public DbSet<TblUsers> tblUsers { get; set; }

        // RMA Reporting Spire
        public DbSet<TblRMA> tblRMA { get; set; }
        public DbSet<TblRMAResponses> tblRMAResponses { get; set; }
        public DbSet<TblRogersReportCMRMA> tblRogersReportCMRMA { get; set; }
        public DbSet<TblRogersReportCM> tblRogersReportCM { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TblEvents>()
                .ToTable("tblEvents", tb => tb.HasTrigger("tr_tblEvents_Audit"));

            modelBuilder.Entity<TblEventTrans>()
                .ToTable("tblEventTrans", tb => {
                    tb.HasTrigger("tr_tblEvents_Audit");
                    tb.HasTrigger("T_tblEventTrans_UTrig");
                    tb.HasTrigger("T_tblEventTrans_ITrig");



                }
                );
        }
    }
}