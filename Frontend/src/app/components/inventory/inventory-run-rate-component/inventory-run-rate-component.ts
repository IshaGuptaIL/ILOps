import { ChangeDetectorRef, Component } from '@angular/core';
import { RunrateService } from '../runrate-service';
import { SpinnerService } from '../../shared/spinner/spinner-service';
import { ToastrService } from 'ngx-toastr';
import * as XLSX from 'xlsx';
import { saveAs } from 'file-saver';
import Swal from 'sweetalert2';
import { finalize } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { CookieService } from 'ngx-cookie-service';

@Component({
  selector: 'app-inventory-run-rate-component',
  standalone: true,
  imports: [FormsModule, CommonModule, HttpClientModule],
  templateUrl: './inventory-run-rate-component.html',
  styleUrl: './inventory-run-rate-component.css',
})
export class InventoryRunRateComponent {
  wfhInventory: any[] = [];
  loading = false;
  
  hardwareList: any[] = [];
  accessoriesList: any[] = [];
  runRateList: any[] = [];
  userId: number = 0;

  // VBA Fields
  reportDate: string = new Date().toISOString().split('T')[0];
  calendarDays: number = 28;
  workingDaysLoaded: number = 0;
  
  startDate!: string;
  endDate!: string;
  startDay!: string;
  endDay!: string;
  
  minDays: number = 0; 
  maxDays: number = 30;

  showAccessoriesTable = false;
  showHardwareTable = false;
  showRunRateTable = false;

  // Pagination
  currentPageAccessories = 1;
  currentPageHardware = 1;
  currentPageRunRate = 1;
  pageSize = 10;
  totalRecordsAccessories = 0;
  totalRecordsHardware = 0;
    maxDate: string = new Date().toISOString().split('T')[0];

  // Mock Mode
  useMock = false;

  constructor(
    private inventoryService: RunrateService,
    private spinner: SpinnerService,
    private toastr: ToastrService,
    private cookieService: CookieService,
    private cdr: ChangeDetectorRef
  ) {}

  private formatDateForApi(dateStr: string): string {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    return date.toISOString().split('T')[0];
  }

  ngOnInit() {
    this.userId = Number(this.cookieService.get('UserID')) || 0;
    this.figureDates();
  }

  public figureDates(): void {
    if (!this.reportDate) return;
    
    const report = new Date(this.reportDate);
    this.endDate = this.reportDate;
    
    const start = new Date(report);
    start.setDate(report.getDate() - (this.calendarDays - 1));
    this.startDate = start.toISOString().split('T')[0];
    
    this.getWeekday(this.startDate, 'Start');
    this.getWeekday(this.endDate, 'End');
    this.cdr.detectChanges();
  }

  public onReportDateChange(): void {
    this.figureDates();
  }

  public onCalendarDaysChange(): void {
    this.figureDates();
  }

  public getWeekday(dateStr: string, dayType: string): void {
    if (!dateStr) {
      if (dayType === 'Start') this.startDay = '';
      else this.endDay = '';
      return;
    }
    const weekday = new Date(dateStr).toLocaleDateString('en-US', { weekday: 'long' });
    if (dayType === 'Start') this.startDay = weekday;
    else this.endDay = weekday;
  }

  generateRunRateData(): void {
    if (!this.startDate || !this.endDate) {
      alert('Please select start and end dates.');
      return;
    }

    if (this.useMock) {
      this.spinner.show();
      setTimeout(() => {
        this.workingDaysLoaded = this.calendarDays - Math.floor(this.calendarDays / 7) * 2;
        this.spinner.hide();
        this.toastr.success(`[MOCK] Run Rate loaded! Working Days: ${this.workingDaysLoaded}`);
        this.cdr.detectChanges();
      }, 1000);
      return;
    }

    this.spinner.show();
    const start = this.formatDateForApi(this.startDate);
    const end = this.formatDateForApi(this.endDate);

    this.inventoryService.loadRunRate(start, end, this.userId)
      .pipe(finalize(() => this.spinner.hide()))
      .subscribe({
        next: (data) => {
          this.workingDaysLoaded = data.workingDays ?? data.WorkingDays;
          this.toastr.success(`Run Rate loaded successfully! Working Days: ${this.workingDaysLoaded}`);
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error(err);
          this.toastr.error('Error loading Run Rate data.');
        }
      });
  }

  fetchWFHInventory() {
    this.spinner.show();
    if (this.useMock) {
      setTimeout(() => {
        this.wfhInventory = [
          { group: 'OFFICE', prod: 'ACC', code: 'W-PEN', description: 'Office Pen', onHand: 45, avgDailySales: 2, weeklyRunRate: 10, weeksAvailable: 4.5 },
          { group: 'REMOTE', prod: 'ACC', code: 'W-MOUS', description: 'Wireless Mouse', onHand: 12, avgDailySales: 1, weeklyRunRate: 5, weeksAvailable: 2.4 }
        ];
        this.spinner.hide();
        this.downloadWFHExcel();
        this.cdr.detectChanges();
      }, 800);
      return;
    }

    this.inventoryService.getWFHInventory(this.userId).subscribe({
      next: (data) => {
        this.wfhInventory = data;
        this.spinner.hide(); 
        this.downloadWFHExcel();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.spinner.hide();
        this.toastr.error('Failed to load WFH inventory');
        console.error(err);
      }
    });
  }

  loadAccessoriesView(page: number = 1) {
    this.spinner.show();
    this.currentPageAccessories = page;
    
    if (this.useMock) {
       setTimeout(() => {
         const mockTotal = 45;
         this.totalRecordsAccessories = mockTotal;
         this.accessoriesList = Array.from({length: Math.min(this.pageSize, mockTotal - (page - 1) * this.pageSize)}, (_, i) => ({
           group: i % 4 === 0 ? 'CHARGERS' : (i % 4 === 1 ? 'CASES' : (i % 4 === 2 ? 'AUDIO' : 'CABLES')),
           prod: 'ACC',
           code: `ITEM-${100 + (page - 1) * this.pageSize + i}`,
           description: `Premium Accessory Product ${i} - Page ${page}`,
           onHand: Math.floor(Math.random() * 150),
           avgDailySales: (Math.random() * 8).toFixed(2),
           weeklyRunRate: (Math.random() * 40).toFixed(2),
           totalSales: Math.floor(Math.random() * 100),
           weeksAvailable: (Math.random() * 12).toFixed(1)
         }));
         this.showAccessoriesTable = true;
         this.showHardwareTable = false;
         this.showRunRateTable = false;
         this.spinner.hide();
         this.cdr.detectChanges();
       }, 500);
       return;
    }

    this.inventoryService.getAccessoriesView(page, this.pageSize, this.userId).pipe(
      finalize(() => {
        this.spinner.hide();
        this.cdr.detectChanges();
      })
    ).subscribe({
      next: (res) => {
        this.accessoriesList = res.items;
        this.totalRecordsAccessories = res.totalCount;
        this.showAccessoriesTable = true;
        this.showHardwareTable = false;
        this.showRunRateTable = false;
      },
      error: (err) => {
        console.error(err);
        this.toastr.error('Failed to load Accessories');
      }
    });
  }

  onPageChangeAccessories(page: number) {
    this.loadAccessoriesView(page);
  }

  loadHardwareView(page: number = 1) {
    this.spinner.show();
    this.currentPageHardware = page;

    if (this.useMock) {
      setTimeout(() => {
        const mockTotal = 22;
        this.totalRecordsHardware = mockTotal;
        this.hardwareList = Array.from({length: Math.min(this.pageSize, mockTotal - (page-1)*this.pageSize)}, (_, i) => ({
          manufacturer: i % 3 === 0 ? 'SAMSUNG' : (i % 3 === 1 ? 'APPLE' : 'GOOGLE'),
          code: `PHONE-${500 + (page - 1) * this.pageSize + i}`,
          description: `NextGen Smartphone Page ${page} Item ${i}`,
          onHand: Math.floor(Math.random() * 40),
          avgDailySales: (Math.random() * 3).toFixed(2),
          weeklyRunRate: (Math.random() * 15).toFixed(2),
          totalSales: Math.floor(Math.random() * 20),
          weeksAvailable: (Math.random() * 6).toFixed(1)
        }));
        this.showHardwareTable = true;
        this.showAccessoriesTable = false;
        this.showRunRateTable = false;
        this.spinner.hide();
        this.cdr.detectChanges();
      }, 500);
      return;
    }

    this.inventoryService.getHardwareView(page, this.pageSize, this.userId).pipe(
      finalize(() => {
        this.spinner.hide();
        this.cdr.detectChanges();
      })
    ).subscribe({
      next: (res) => {
        this.hardwareList = res.items;
        this.totalRecordsHardware = res.totalCount;
        this.showHardwareTable = true;
        this.showAccessoriesTable = false;
        this.showRunRateTable = false;
      },
      error: (err) => {
        console.error(err);
        this.toastr.error('Failed to load Hardware');
      }
    });
  }

  onPageChangeHardware(page: number) {
    this.loadHardwareView(page);
  }

  fetchRunRate(min: number, max: number) {
    this.spinner.show();
    if (this.useMock) {
      setTimeout(() => {
        this.runRateList = Array.from({length: 15}, (_, i) => ({
          group: 'ACCESSORY',
          code: `PO-ITEM-${200+i}`,
          onHand: Math.floor(Math.random() * 50),
          poLast: `PO123${i}`,
          qtyLast: 20,
          ageLast: Math.floor(Math.random() * 250),
          poLast2: `PO120${i}`,
          qtyLast2: 15,
          ageLast2: Math.floor(Math.random() * 500)
        }));
        this.currentPageRunRate = 1;
        this.showRunRateTable = true;
        this.showAccessoriesTable = false;
        this.showHardwareTable = false;
        this.spinner.hide();
        this.cdr.detectChanges();
      }, 500);
      return;
    }
    this.inventoryService.getRunRate(min, max, this.userId)
      .pipe(finalize(() => this.spinner.hide()))
      .subscribe({
        next: (data) => {
          this.runRateList = data || [];
          this.currentPageRunRate = 1;
          this.showRunRateTable = true;
          this.showAccessoriesTable = false;
          this.showHardwareTable = false;
          this.cdr.detectChanges();
        },
        error: (err) => {
          this.toastr.error('Failed to load Run Rate data');
          console.error(err);
        }
      });
  }

  downloadAccessoriesExcel() {
    this.spinner.show();
    this.inventoryService.exportAccessoriesExcel(this.userId).subscribe({
      next: (blob) => {
        this.spinner.hide();
        const fileName = `Stock_Status_Accessories_${new Date().toISOString().slice(0,10)}.xlsx`;
        saveAs(blob, fileName);
        Swal.fire({ icon: 'success', title: 'Exported', text: 'Excel file downloaded', timer: 1500, showConfirmButton: false });
      },
      error: () => {
        this.spinner.hide();
        Swal.fire({ icon: 'error', title: 'Error', text: 'Failed to export Excel' });
      }
    });
  }

  downloadHardwareExcel() {
    this.spinner.show();
    this.inventoryService.exportHardwareExcel(this.userId).subscribe({
      next: (blob) => {
        this.spinner.hide();
        const fileName = `Stock_Status_Hardware_${new Date().toISOString().slice(0,10)}.xlsx`;
        saveAs(blob, fileName);
        Swal.fire({ icon: 'success', title: 'Exported', text: 'Excel file downloaded', timer: 1500, showConfirmButton: false });
      },
      error: () => {
        this.spinner.hide();
        Swal.fire({ icon: 'error', title: 'Error', text: 'Failed to export Excel' });
      }
    });
  }

  downloadAccessoriesRogersExcel() {
    this.spinner.show();
    this.inventoryService.exportAccessoriesRogersExcel(this.userId).subscribe({
      next: (blob) => {
        this.spinner.hide();
        const fileName = `Stock_Status_Accessories_Rogers_${new Date().toISOString().slice(0, 10)}.xlsx`;
        saveAs(blob, fileName);
        Swal.fire({ icon: 'success', title: 'Exported', text: 'Rogers Excel file downloaded', timer: 1500, showConfirmButton: false });
      },
      error: () => {
        this.spinner.hide();
        Swal.fire({ icon: 'error', title: 'Error', text: 'Failed to export Rogers Excel' });
      }
    });
  }

  downloadWFHExcel() {
    if (!this.wfhInventory || this.wfhInventory.length === 0) {
      Swal.fire({ icon: 'info', title: 'No Data', text: 'No WFH Inventory Found To Export' });
      return;
    }
    this.exportToExcel(this.wfhInventory, `WorkFromHome-Onhand-${new Date().toISOString().slice(0,10)}`);
  }

  exportToExcel(data: any[], name?: string) {
    const filename = (name || 'Inventory_Report').replace(/\s+/g, '_') + '.xlsx';
    const ws = XLSX.utils.json_to_sheet(data);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Data');
    XLSX.writeFile(wb, filename);
    Swal.fire({ icon: 'success', title: 'Excel Exported', timer: 1500, showConfirmButton: false });
  }
}


