import { AfterViewInit, Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AddInventoryComponent } from '../add-inventory-component/add-inventory-component';
import { ModifyInventoryComponent } from '../modify-inventory-component/modify-inventory-component';
declare var bootstrap: any;
@Component({
  selector: 'app-inventory-dashboard-component',
 imports: [AddInventoryComponent,ModifyInventoryComponent], 
  templateUrl: './inventory-dashboard-component.html',
  styleUrl: './inventory-dashboard-component.css',
})
export class InventoryDashboardComponent {
activePanel: 'add' | 'modify' | null = 'modify';

 togglePanel(panel: 'add' | 'modify') {
  if (this.activePanel === panel) {
    this.activePanel = panel === 'add' ? 'modify' : 'add';
  } else {
    
    this.activePanel = panel;
  }
}
}