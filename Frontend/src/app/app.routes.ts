import { Routes } from '@angular/router';
import { Login } from './components/login/login';
import { DashboardComponent } from './components/dashboard-component/dashboard-component';
import { ImeiComponents } from './components/IMEI/imei-components/imei-components';
import { AddInventoryComponent } from './components/inventory/add-inventory-component/add-inventory-component';
import { ImeiExceptionComponent } from './components/IMEI/imei-exception-component/imei-exception-component';
import { ModifyInventoryComponent } from './components/inventory/modify-inventory-component/modify-inventory-component';
import { UserRoleComponent } from './user-role-component/user-role-component';
import { UserComponent } from './components/user-component/user-component';
import { FindByImeiComponent } from './components/IMEI/find-by-imei-component/find-by-imei-component';
import { CostValidationComponent } from './components/inventory/cost-validation-component/cost-validation-component';
import { RecieveImeiComponent } from './components/IMEI/recieve-imei-component/recieve-imei-component';

export const routes: Routes = [
  { path: '', component: Login },           // default login page
  { path: 'login', component: Login },      // optional login alias
  { path: 'dashboard', component: DashboardComponent },
  { path: 'inventory', component: DashboardComponent }, // change to real components
  { path: 'sales', component: DashboardComponent },
  { path: 'settings', component: DashboardComponent },
  { path: 'imei', component: ImeiComponents },
  { path: 'add-inventory', component: AddInventoryComponent },
  { path: 'imei-exception', component: ImeiExceptionComponent },
  { path: 'modify-inventory', component: ModifyInventoryComponent },
  { path: 'manage-user', component: UserComponent },
  { path: 'user-role', component: UserRoleComponent },
  { path: 'find-by-imei', component: FindByImeiComponent },
  { path: 'cost-validation', component: CostValidationComponent },
  { path: 'recieve-imei', component: RecieveImeiComponent },









  { path: '**', redirectTo: '' }            // fallback to login
];
