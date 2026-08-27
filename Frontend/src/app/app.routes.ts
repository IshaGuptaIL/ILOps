import { Routes } from '@angular/router';
import { Login } from './components/login/login';
import { DashboardComponent } from './components/dashboard-component/dashboard-component';
import { ImeiComponents } from './components/IMEI/imei-components/imei-components';
import { AddInventoryComponent } from './components/inventory/add-inventory-component/add-inventory-component';
import { ModifyInventoryComponent } from './components/inventory/modify-inventory-component/modify-inventory-component';
import { UserRoleComponent } from './user-role-component/user-role-component';
import { UserComponent } from './components/user-component/user-component';
import { FindByImeiComponent } from './components/IMEI/find-by-imei-component/find-by-imei-component';
import { CostValidationComponent } from './components/inventory/cost-validation-component/cost-validation-component';
import { RecieveImeiComponent } from './components/IMEI/recieve-imei-component/recieve-imei-component';
import { InvoiceCreditComponent } from './components/IMEI/invoice-credit-component/invoice-credit-component';
import { ImeiReportComponent } from './components/IMEI/imei-report-component/imei-report-component';
import { ExceptionComponent } from './components/IMEI/exception-component/exception-component';
import { InventoryCountComponent } from './components/inventory/inventory-count-component/inventory-count-component';
import { CountSpireComponent } from './components/inventory/count-spire-component/count-spire-component';
import { AnalyseCountComponent } from './components/analyse-count-component/analyse-count-component';
import { InventoryTypeComponent } from './components/inventory/inventory-type-component/inventory-type-component';
import { RolePermissionComponent } from './components/role-permission-component/role-permission-component';
import { OutputInvoiceComponent } from './components/inventory/output-invoice-component/output-invoice-component';
import { CountAnalysisComponent } from './components/inventory/count-analysis-component/count-analysis-component';
import { InventoryDashboardComponent } from './components/inventory/inventory-dashboard-component/inventory-dashboard-component';
import { CustomSearchComponent } from './components/sales/custom-search-component/custom-search-component';
import { InventoryRunRateComponent } from './components/inventory/inventory-run-rate-component/inventory-run-rate-component';
import { ImeiReceiveComponent } from './components/IMEI/imei-receive-component/imei-receive-component';
import { SpareLightComponent } from './components/inventory/spare-light-component/spare-light-component';
import { RogersComponent } from './components/inventory/rogers-component/rogers-component';
import { AdvantageVoiceComponent } from './components/inventory/advantage-voice-component/advantage-voice-component';
import { InventoryEditComponent } from './components/inventory/inventory-edit-component/inventory-edit-component';
import { SalesTaxReportComponent } from './components/sales/sales-tax-report-component/sales-tax-report-component';
import { CustomerSalesComponent } from './components/sales/customer-sales-component/customer-sales-component';
import { HydroComponent } from './components/sales/hydro-component/hydro-component';
import { RogerSalesReportingComponent } from './components/sales/roger-sales-reporting-component/roger-sales-reporting-component';
import { RogersInvoiceSpireComponent } from './components/sales/rogers-invoice-spire/rogers-invoice-spire.component';
import { RMAReportingSpireComponent } from './components/sales/rma-reporting-component/rma-reporting-spire.component';

export const routes: Routes = [
  { path: '', component: Login },           // default login page
  { path: 'login', component: Login },      // optional login alias
  { path: 'dashboard', component: DashboardComponent },
  { path: 'inventory', component: DashboardComponent }, // change to real components
  { path: 'sales', component: DashboardComponent },
  { path: 'settings', component: DashboardComponent },
  { path: 'imei', component: ImeiComponents },
  { path: 'add-inventory', component: AddInventoryComponent },
  { path: 'modify-inventory', component: ModifyInventoryComponent },
  { path: 'manage-user', component: UserComponent },
  { path: 'user-role', component: UserRoleComponent },
  { path: 'cost-validation', component: CostValidationComponent },
  { path: 'costAnalysis', component: CountAnalysisComponent },
{path:'saleTaxReport',component:SalesTaxReportComponent},
  { path: 'inventoryDashbaord', component: InventoryDashboardComponent },
  { 
    path: 'priceProtection', 
    loadComponent: () => import('./components/inventory/price-protection-component/price-protection-component').then(m => m.PriceProtectionDashboardComponent),
    children: [
      {
        path: 'claims',
        loadComponent: () => import('./components/inventory/priceprotection/priceprotection').then(m => m.PriceProtectionComponent)
      },
      {
        path: 'apply-credits',
        loadComponent: () => import('./components/inventory/priceprotection/apply-credit-review-claims/apply-credit-review-claims').then(m => m.ApplyCreditReviewClaimsComponent)
      },
      {
        path: 'imei-search',
        loadComponent: () => import('./components/inventory/priceprotection/imeiSearch/imeiSearch').then(m => m.ImeiSearchComponent)
      },
      {
        path: 'output-to-excel',
        loadComponent: () => import('./components/inventory/priceprotection/outputToExcel/outputToExcel').then(m => m.OutputToExcelComponent)
      },
      {
        path: 'roger-overpayments',
        loadComponent: () => import('./components/inventory/priceprotection/rogerOverPayments/rogerOverPayments').then(m => m.RogerOverPaymentsComponent)
      },
      {
        path: '',
        redirectTo: 'claims',
        pathMatch: 'full'
      }
    ]
  },
  { path: 'customSearch', component: CustomSearchComponent },
  { path: 'inventoryRunRate', component: InventoryRunRateComponent },
  { path: 'rogersAR', component: RogersComponent },
  { path: 'spareLight', component: SpareLightComponent },
  { path: 'role-permissions', component: RolePermissionComponent },
  { path: 'inventoryType', component: InventoryTypeComponent },
  { path: 'customerSales', component: CustomerSalesComponent },
  { path: 'hydro', component: HydroComponent },
{path:'rogerSales',component:RogerSalesReportingComponent},
  { path: 'rogersInvoiceSpire', component: RogersInvoiceSpireComponent },
  { path: 'rmaReporting', component: RMAReportingSpireComponent },
 {
    path: 'arCollection',
    loadComponent: () =>
      import('./components/sales/ar-collection-component/ar-collection-dashboard/ar-collection-dashboard')
        .then(x => x.ArCollectionDashboardComponent),

    children: [
      {
        path: 'users',
        loadComponent: () =>
          import('./components/sales/ar-collection-component/ar-collection-users/ar-collection-users')
            .then(x => x.ArCollectionUsersComponent)
      },

      {
        path: 'groups',
        loadComponent: () =>
          import('./components/sales/ar-collection-component/ar-collection-groups/ar-collection-groups')
            .then(x => x.ArCollectionGroupsComponent)
      },

{
  path: 'review',
  loadComponent: () =>
    import('./components/sales/ar-collection-component/ar-collection-review/ar-collection-review')
      .then(x => x.ArCollectionReviewComponent)
},
{
  path: 'report',
  loadComponent: () =>
    import('./components/sales/ar-collection-component/ar-report-component/ar-report-component')
      .then(x => x.ArReportComponent)
},
{
  path: 'commentReview',
  loadComponent: () =>
    import('./components/sales/ar-collection-component/comment-review-component/comment-review-component')
      .then(x => x.CommentReviewComponent)
},
{
  path: 'glActivity',
  loadComponent: () =>
    import('./components/sales/ar-collection-component/gl-activity-component/gl-activity-component')
      .then(x => x.GlActivityComponent)
},
{
  path: 'batchNotice',
  loadComponent: () =>
    import('./components/sales/ar-collection-component/batch-output-notice-component/batch-output-notice-component')
      .then(x => x.BatchOutputNoticeComponent)
},

      {
        path: '',
        redirectTo: 'users',
        pathMatch: 'full'
      }
    ]
  }
      ,{ path: 'outputInvoice', component: OutputInvoiceComponent },
      { path: 'advantagevoice', component: AdvantageVoiceComponent },
      { 
        path: 'inventory-edit', 
        component: InventoryEditComponent,
        children: [
          { path: 'terms', loadComponent: () => import('./components/inventory/inventory-edit-component/terms-edit/terms-edit').then(m => m.TermsEdit) },
          { path: 'bulk', loadComponent: () => import('./components/inventory/inventory-edit-component/bulk-id-edit/bulk-id-edit').then(m => m.BulkIdEdit) },
          { path: 'address', loadComponent: () => import('./components/inventory/inventory-edit-component/address-edit/address-edit').then(m => m.AddressEdit) },
          { path: '', redirectTo: 'terms', pathMatch: 'full' }
        ]
      },
{ 
    path: 'imei', 
    component: ImeiComponents, 
    children: [
      { path: 'receive', component: ImeiReceiveComponent },
      { path: 'credit', component: InvoiceCreditComponent },
      { path: 'find', component: FindByImeiComponent },
      { path: 'exception', component: ExceptionComponent },
      { path: 'reports', component: ImeiReportComponent },

    ]
  },

  {
    path:'count',
    component:CountSpireComponent,
    children:[
      {path:"maintain-count",component:InventoryCountComponent},
      {path:"analyse",component:AnalyseCountComponent}

    ]
  },











  { path: '**', redirectTo: '' }            // fallback to login
];
