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

  { path: 'inventoryDashbaord', component: InventoryDashboardComponent },
  { path: 'customSearch', component: CustomSearchComponent },
  { path: 'inventoryRunRate', component: InventoryRunRateComponent },
  // { path: 'imei-reports', component: ImeiReportComponent },
  // { path: 'invoice-credit', component: InvoiceCreditComponent },
  { path: 'role-permissions', component: RolePermissionComponent },
  { path: 'inventoryType', component: InventoryTypeComponent },
      { path: 'outputInvoice', component: OutputInvoiceComponent },


{ 
    path: 'imei', 
    component: ImeiComponents, 
    children: [
      { path: 'receive', component: RecieveImeiComponent },
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
