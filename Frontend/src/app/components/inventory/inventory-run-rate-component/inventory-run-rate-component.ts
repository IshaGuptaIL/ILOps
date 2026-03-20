import { ChangeDetectorRef, Component } from '@angular/core';
import { RunrateService } from '../runrate-service';
import { SpinnerService } from '../../shared/spinner/spinner-service';
import { ToastrService } from 'ngx-toastr';
import * as XLSX from 'xlsx';
import { saveAs } from 'file-saver';
import Swal from 'sweetalert2';



@Component({
  selector: 'app-inventory-run-rate-component',
  imports: [],
  templateUrl: './inventory-run-rate-component.html',
  styleUrl: './inventory-run-rate-component.css',
})
export class InventoryRunRateComponent {
  wfhInventory: any[] = [];
  loading = false;

  constructor(
    private inventoryService: RunrateService,
    private spinner: SpinnerService,
    private toastr: ToastrService,
    private cdr: ChangeDetectorRef
  ) {}

  fetchWFHInventory() {
    this.loading = true;
    this.spinner.show();
    this.cdr.detectChanges(); 

    this.inventoryService.getWFHInventory().subscribe({
      next: (data) => {
        setTimeout(() => {
          this.wfhInventory = data;
          this.loading = false;
          this.spinner.hide(); 
          this.cdr.detectChanges(); 
        }, 300); 
        debugger
        this.downloadWFHExcel()
      },
      error: (err) => {
        setTimeout(() => {
          this.loading = false;
          this.spinner.hide();
          this.toastr.error('Failed to load WFH inventory', 'Error');
          this.cdr.detectChanges();
          console.error(err);
        }, 300); 
      }
    });
  }


downloadWFHExcel() {
  debugger
    if (!this.wfhInventory || this.wfhInventory.length === 0) {
      Swal.fire({ icon: 'info', title: 'No Data', text: 'No WFH Inventory Found To Export' });
      return;
    }

    this.exportToExcel(this.wfhInventory, `WorkFromHome-Onhand-${new Date().toISOString().slice(0,10)}`);
  }

 exportToExcel(data: any[], name?: string) {
  if (!data || data.length === 0) {
    Swal.fire({ icon: 'info', title: 'No Data', text: 'No WFH Inventory Found To Export' });
    return;
  }

  const filename = (name || 'WFH_Inventory').replace(/\s+/g, '_') + '.xlsx';

  const ws = XLSX.utils.json_to_sheet(data);

  ws['!cols'] = Object.keys(data[0]).map(() => ({ wch: 20 }));

  const headerRow = Object.keys(data[0]);
  headerRow.forEach((key, idx) => {
    const cellAddress = XLSX.utils.encode_cell({ c: idx, r: 0 });
    if (ws[cellAddress]) {
      const value = ws[cellAddress].v as string;
      ws[cellAddress].v = value.charAt(0).toUpperCase() + value.slice(1);
      ws[cellAddress].s = { font: { bold: true } };
    }
  });

  const wb = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(wb, ws, 'Data');

  const buffer = XLSX.write(wb, { bookType: 'xlsx', type: 'array', cellStyles: true });
  saveAs(new Blob([buffer], { type: 'application/octet-stream' }), filename);

  Swal.fire({ icon: 'success', title: 'Excel Exported', timer: 1500, showConfirmButton: false });
}

}
