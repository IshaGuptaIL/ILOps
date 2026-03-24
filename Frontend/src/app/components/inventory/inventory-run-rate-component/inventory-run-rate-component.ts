import { ChangeDetectorRef, Component } from '@angular/core';
import { RunrateService } from '../runrate-service';
import { SpinnerService } from '../../shared/spinner/spinner-service';
import { ToastrService } from 'ngx-toastr';
import * as XLSX from 'xlsx';
import { saveAs } from 'file-saver';
import Swal from 'sweetalert2';
import { delay, finalize } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';



@Component({
  selector: 'app-inventory-run-rate-component',
  imports: [FormsModule,CommonModule,HttpClientModule],
  templateUrl: './inventory-run-rate-component.html',
  styleUrl: './inventory-run-rate-component.css',
})
export class InventoryRunRateComponent {
  wfhInventory: any[] = [];
  loading = false;
 minDays!: number; 
  maxDays!: number;
    hardwareList: any[] = [];
    accessoriesList: any;
      p: number = 1;         // current page
  pageSizes: number = 10; // items per page

   workingDays: number = 28;
  reportDate!: string;
  startDate!: string;
  endDate!: string;
  startDay!: string;
  endDay!: string;

  constructor(
    private inventoryService: RunrateService,
    private spinner: SpinnerService,
    private toastr: ToastrService,
    private cdr: ChangeDetectorRef
  ) {}

private formatDateForApi(dateStr: string): string {
  if (!dateStr) return '';
  const date = new Date(dateStr);
  return date.toISOString().split('T')[0]; // returns 'YYYY-MM-DD'
}

  private getWeekday(dateStr: string): string {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    return date.toLocaleDateString('en-US', { weekday: 'long' });
  }


  // Method called on button click
  generateRunRateData(): void {
    debugger
  if (!this.startDate || !this.endDate) {
    alert('Please select start and end dates.');
    return;
  }

  const start = this.formatDateForApi(this.startDate);
  const end = this.formatDateForApi(this.endDate);

  // update day names
  this.startDay = this.getWeekday(start);
  this.endDay = this.getWeekday(end);

  this.inventoryService.loadRunRate(start, end).subscribe({
    next: (data) => {
      console.log('Run Rate loaded:', data);
      alert(`Run Rate loaded successfully! Working Days: ${data.WorkingDays}`);
    },
    error: (err) => {
      console.error(err);
      alert('Error loading Run Rate data.');
    }
  });
}

  fetchWFHInventory() {
  this.spinner.show();
  this.cdr.detectChanges(); 

  this.inventoryService.getWFHInventory().subscribe({
    next: (data) => {
      setTimeout(() => {
        this.wfhInventory = data;
        this.spinner.hide(); 

        this.cdr.detectChanges();

        this.downloadWFHExcel();

      }, 300); 
    },
    error: (err) => {
      setTimeout(() => {
        this.spinner.hide();
        this.toastr.error('Failed to load WFH inventory', 'Error');
        this.cdr.detectChanges();
        console.error(err);
      }, 300); 
    }
  });
}

loadAccessoriesView() {
    this.loading = true;
    this.cdr.detectChanges(); // show spinner immediately

    this.inventoryService.getAccessoriesView()
      .pipe(
        delay(300),
        finalize(() => {
          this.loading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (res) => this.accessoriesList = res,
        error: (err) => console.error(err)
      });
  }

loadHardwareView() {
    this.spinner.show()
    this.cdr.detectChanges(); // force spinner to show immediately

    this.inventoryService.getHardwareView()
      .pipe(
        delay(300), // optional: show spinner at least 300ms
        finalize(() => {
          this.spinner.hide() // hide spinner when request completes
          this.cdr.detectChanges(); // update view
        })
      )
      .subscribe({
        next: (res) => {
          this.hardwareList = res;
          this.spinner.hide() // hide spinner when request completes

        },
        error: (err) => {
          console.error(err);
          this.spinner.hide() // hide spinner when request completes

        }
      });
  }




downloadAccessoriesExcel() {
  this.spinner.show()
    this.inventoryService.exportAccessoriesExcel().subscribe({
      next: (blob) => {
        this.spinner.hide()
        const fileName = `Stock_Status_Accessories_${new Date().toISOString().slice(0,10)}.xlsx`;
        saveAs(blob, fileName);
        Swal.fire({ icon: 'success', title: 'Exported', text: 'Excel file downloaded', timer: 1500, showConfirmButton: false });
      
      },
      error: (err) => {
        console.error(err);
        this.spinner.hide()

        Swal.fire({ icon: 'error', title: 'Error', text: 'Failed to export Excel' });
      }
    });
  }

  downloadHardwareExcel() {
    debugger
    this.spinner.show()
    this.inventoryService.exportHardwareExcel().subscribe({
      next: (blob) => {
        this.spinner.hide()

        const fileName = `Stock_Status_Hardware_${new Date().toISOString().slice(0,10)}.xlsx`;
        saveAs(blob, fileName);
        Swal.fire({ icon: 'success', title: 'Exported', text: 'Excel file downloaded', timer: 1500, showConfirmButton: false });
      },
      error: (err) => {
        this.spinner.hide()

        console.error(err);
        Swal.fire({ icon: 'error', title: 'Error', text: 'Failed to export Excel' });
      }
    });
  }
exportRunRateExcel() {
  if (!this.wfhInventory || this.wfhInventory.length === 0) {
    Swal.fire({ icon: 'info', title: 'No Data', text: 'No data to export' });
    return;
  }

  const reportDate = new Date().toLocaleDateString('en-US', {
    month: 'short',
    day: '2-digit',
    year: 'numeric'
  });

  // 🟡 HEADER (same as VBA template)
  const header = [
    ['DISCOVER COMMUNICATIONS INC.'],
    ['Inventory Report'],
    [`AS ON ${reportDate}`],
    [],
    ['Group','Product Code','SKU','Description','Qty','Avg Daily Sales','Weekly Run Rate','3 Week Run Rate','8 Week Run Rate','Weeks Available']
  ];

  // 🔵 DATA MAPPING (VERY IMPORTANT)
  const data = this.wfhInventory.map(item => {
    const threeWeek = item.weeklyRunRate * 3;
    const eightWeek = item.weeklyRunRate * 8;

    return [
      item.group,
      item.prod,
      item.code,
      item.description,
      item.onHand,
      item.avgDailySales,
      item.weeklyRunRate,
      threeWeek,
      eightWeek,
      item.totalSales === 0 ? 'NA' : item.weeksAvailable
    ];
  });

  const wsData = [...header, ...data];

  const ws: XLSX.WorkSheet = XLSX.utils.aoa_to_sheet(wsData);

  // 🟢 COLUMN WIDTH
  ws['!cols'] = [
    { wch: 20 }, { wch: 15 }, { wch: 20 }, { wch: 40 },
    { wch: 10 }, { wch: 18 }, { wch: 18 },
    { wch: 18 }, { wch: 18 }, { wch: 18 }
  ];

  const wb: XLSX.WorkBook = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(wb, ws, 'RunRate');

  const fileName = `Stock-Status-Accessories-${new Date().toISOString().slice(0,10)}.xlsx`;

  XLSX.writeFile(wb, fileName);

  Swal.fire({ icon: 'success', title: 'Excel Exported', timer: 1500, showConfirmButton: false });
}

fetchRunRate(minDays: number, maxDays: number) {
  this.spinner.show();

  this.inventoryService.getRunRate(minDays, maxDays)
    .pipe(finalize(() => this.spinner.hide()))
    .subscribe({
      next: (data) => {
        this.wfhInventory = data || [];
        if (!this.wfhInventory.length) {
          this.toastr.warning('No WFH inventory found');
          return;
        }
        this.downloadWFHExcel(); 
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.toastr.error('Failed to load WFH inventory');
        console.error(err);
        this.cdr.markForCheck();
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
