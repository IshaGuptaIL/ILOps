import { Component, OnInit, ChangeDetectorRef, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import * as XLSX from 'xlsx-js-style'; 
import { saveAs } from 'file-saver';
import { InventoryService } from '../add-inventory-component/inventory-service';
import Swal from 'sweetalert2';

// ✅ Correct imports
import { Observable, of } from 'rxjs';
import { delay, tap } from 'rxjs/operators';
import { SpinnerService } from '../../shared/spinner/spinner-service';

declare var bootstrap: any;

interface ApiResponse {
  success: boolean;
  message: string;
  result?: {
    validRows: any[];
    invalidRows: any[];
    insertedCount: number;
    failedCount: number;
  };
  statusCode?: number;
}

@Component({
  selector: 'app-cost-validation-component',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './cost-validation-component.html',
  styleUrls: ['./cost-validation-component.css'],
})
export class CostValidationComponent implements OnInit {
  // Page state
  title = 'HPC Latest';
  
  selectedFile: File | null = null;

  // Valid records data
  model: any[] = [];
  modelColumns: string[] = [];

 invalidRows: any[] = [];
  invalidColumns: string[] = [];

  insertedCount: number = 0;
  failedCount: number = 0;

  // Pagination
  currentPage = 1;
  pageSize = 5;
  totalPages = 0;
  pagedData: any[] = [];
  pagesArray: number[] = [];
  activeSection: string = 'HPC'; 

  constructor(
    private svc: InventoryService,
    private zone: NgZone,
   private cdr: ChangeDetectorRef,private spinner:SpinnerService
  ) {}

  ngOnInit(): void {
    this.svc.HpcLatest().pipe(
      delay(0),
      tap(() => setTimeout(() => this.cdr.detectChanges(), 0))
    ).subscribe({
      next: (res: any) => {
        if (res.success) {
          this.bindGrid(res.result || []);
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Failed to load HPC Latest', err);
        this.cdr.detectChanges();
      }
    });
  }

  // ========== FILE UPLOAD METHODS ==========

  triggerFileInput() {
    const input = document.getElementById('fileInput') as HTMLInputElement;
    if (input) input.click();
  }

  onFileUpload(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) {
      this.selectedFile = null;
      return;
    }

    this.zone.run(() => {
      this.selectedFile = input.files![0];
      console.log('File selected:', this.selectedFile?.name);
      this.cdr.detectChanges();
    });
  }

  uploadfile() {
    this.spinner.show();
    if (!this.selectedFile) {
      Swal.fire({
        icon: 'warning',
        title: 'No File Selected',
        text: 'Please select a file first.'
      });
      this.spinner.hide();
      return;
    }

  
    this.invalidRows = [];
    this.invalidColumns = [];
    this.insertedCount = 0;
    this.failedCount = 0;

    this.cdr.detectChanges();

    this.svc.uploadHpc(this.selectedFile).pipe(
      delay(0),
      tap(() => {
        setTimeout(() => this.cdr.detectChanges(), 0);
      })
    ).subscribe({
      next: (res: ApiResponse) => {
        this.handleUploadResponse(res);
      this.spinner.hide();

      },
      error: (err) => {
       // this.spinner.hide()
        this.cdr.detectChanges();
        Swal.fire({
          icon: 'error',
          title: 'Upload Error',
          text: err.message || 'Server error occurred'
        });
      this.spinner.hide();

      }
    });
  }

  /**
   * ✅ FIXED: Handle upload API response
   */
  private handleUploadResponse(res: ApiResponse) {
    console.log('=== handleUploadResponse ===');
    console.log('Success:', res.success);
    console.log('Message:', res.message);
    console.log('Result:', res.result);

    if (!res.success && res.message === "Validation failed") {
      console.log('=== VALIDATION FAILED ===');

      this.insertedCount = res.result?.insertedCount ?? 0;
      this.failedCount = res.result?.failedCount ?? 0;
      this.invalidRows = [...(res.result?.invalidRows || [])];
      this.invalidColumns = this.invalidRows.length > 0
        ? Object.keys(this.invalidRows[0])
        : [];

      console.log('Invalid Rows:', this.invalidRows);
      console.log('Failed Count:', this.failedCount);

      this.model = [];
      this.pagedData = [];
      this.modelColumns = [];
      this.totalPages = 0;
      this.pagesArray = [];

      this.cdr.detectChanges();

      setTimeout(() => {
        this.cdr.detectChanges();
        this.showValidationErrorDialog();
      }, 100);

      return;
    }

    if (res.success && res.result) {
      this.insertedCount = res.result.insertedCount ?? 0;
      this.failedCount = res.result.failedCount ?? 0;
      this.invalidRows = [...(res.result.invalidRows || [])];
      this.invalidColumns = this.invalidRows.length > 0
        ? Object.keys(this.invalidRows[0])
        : [];

      const validRows = res.result.validRows || [];

      this.cdr.detectChanges();

      Swal.fire({
        icon: 'success',
        title: 'Upload Successful',
        html: `
          <div style="text-align: left;">
            <p>✅ Inserted: <strong style="color: green;">${this.insertedCount}</strong></p>
            <p>❌ Failed: <strong style="color: red;">${this.failedCount}</strong></p>
          </div>
        `,
        confirmButtonText: 'OK'
      }).then(() => {
        this.bindGrid(validRows);
        this.cdr.detectChanges();

        if (this.failedCount > 0) {
          this.showInvalidRecordsPrompt();
        }
      });

      return;
    }


    Swal.fire({
      icon: 'error',
      title: 'Error',
      text: res.message || 'Upload failed'
    });
  }

  
  private showValidationErrorDialog() {
    console.log('showValidationErrorDialog - invalidRows:', this.invalidRows);

    const errorPreview = this.invalidRows.slice(0, 3)
      .map((row:any) => `<li><strong>Row ${row.RowNumber}:</strong> ${row.Reason}</li>`)
      .join('');

    const moreErrors = this.invalidRows.length > 3
      ? `<li style="color: gray;">...and ${this.invalidRows.length - 3} more errors</li>`
      : '';

    Swal.fire({
      icon: 'warning',
      title: 'Validation Failed',
      html: `
        <div style="text-align: left;">
          <p><strong>${this.failedCount}</strong> invalid row(s) found!</p>
          <ul style="color: red; font-size: 14px; padding-left: 20px;">
            ${errorPreview}
            ${moreErrors}
          </ul>
          <p style="margin-top: 15px; color: #666;">
            Please check the <strong>Invalid Records</strong> tab for full details.
          </p>
        </div>
      `,
      confirmButtonText: 'View Invalid Records',
      showCancelButton: true,
      cancelButtonText: 'Close',
      confirmButtonColor: '#ffc107',
      cancelButtonColor: '#6c757d'
    }).then((result) => {
      console.log('Dialog closed - invalidRows:', this.invalidRows);
      this.cdr.detectChanges();

      if (result.isConfirmed) {
        this.switchToInvalidTab();
      }
    });
  }

  private showInvalidRecordsPrompt() {
    setTimeout(() => {
      Swal.fire({
        icon: 'info',
        title: 'Some Records Failed',
        text: `${this.failedCount} record(s) failed validation. Would you like to view them?`,
        showCancelButton: true,
        confirmButtonText: 'View Invalid Records',
        cancelButtonText: 'Later',
        confirmButtonColor: '#ffc107'
      }).then((result) => {
        this.cdr.detectChanges();
        if (result.isConfirmed) {
          this.switchToInvalidTab();
        }
      });
    }, 500);
  }

  switchToInvalidTab() {
    console.log('=== switchToInvalidTab ===');
    console.log('invalidRows at switch:', this.invalidRows);

    this.cdr.detectChanges();

    setTimeout(() => {
      try {
        const invalidTabBtn = document.getElementById('invalid-tab');
        if (invalidTabBtn) {
          const validTabBtn = document.getElementById('valid-tab');
          const validPane = document.getElementById('tabValid');
          const invalidPane = document.getElementById('tabInvalid');

          if (validTabBtn) {
            validTabBtn.classList.remove('active');
            validTabBtn.setAttribute('aria-selected', 'false');
          }
          if (validPane) {
            validPane.classList.remove('show', 'active');
          }

          invalidTabBtn.classList.add('active');
          invalidTabBtn.setAttribute('aria-selected', 'true');
          if (invalidPane) {
            invalidPane.classList.add('show', 'active');
          }

          if (typeof bootstrap !== 'undefined') {
            const tab = new bootstrap.Tab(invalidTabBtn);
            tab.show();
          }
        }
      } catch (e) {
        console.error('Tab switch error:', e);
      }

      this.cdr.detectChanges();
    }, 150);
  }

formatHeader(key: string): string {
  if (!key) return '';
  return key
    .replace(/([A-Z])/g, ' $1')          // Insert space before capitals (startDate -> start Date)
    .replace(/^./, str => str.toUpperCase()) // Capitalize first letter (start Date -> Start Date)
    .trim();                             // Remove extra spaces
}
  bindGrid(data: any[]) {
    this.model = data || [];
    this.currentPage = 1;
    this.totalPages = Math.ceil(this.model.length / this.pageSize);
    this.pagesArray = Array.from({ length: this.totalPages }, (_, i) => i + 1);
    this.modelColumns = this.model.length > 0 ? Object.keys(this.model[0]) : [];

    this.applyPagination();
    this.cdr.detectChanges();
  }

  applyPagination() {
    const start = (this.currentPage - 1) * this.pageSize;
    this.pagedData = this.model.slice(start, start + this.pageSize);
    this.cdr.detectChanges();
  }

  goToPage(page: number) {
    if (page < 1 || page > this.totalPages) return;
    this.currentPage = page;
    this.applyPagination();
  }

  // getValue(row: any, col: string): any {
  //   return row?.[col] ?? '-';
  // }


  loadHpcLatest() {
   // this.spinner.show()
    this.title = 'HPC Latest';
      this.activeSection = 'HPC';
    this.cdr.detectChanges();

    this.svc.HpcLatest().pipe(
      delay(0),
      tap(() => setTimeout(() => this.cdr.detectChanges(), 0))
    ).subscribe({
      next: (res: any) => {
       // this.spinner.hide()
        if (res.success) {
          this.bindGrid(res.result || []);
        } else {
          Swal.fire('Error', res.message, 'error');
        }
        this.cdr.detectChanges();
      },
      error: (err) => this.handleReportError(err)
    });
  }

  loadHpcDiscrepancies() {
      this.activeSection = 'Discrepancy';
   this.spinner.show()
   debugger
    this.title = 'HPC Discrepancies';
    this.cdr.detectChanges();

    this.svc.HpcDiscrepancies().pipe(
      delay(0),
      tap(() => setTimeout(() => this.cdr.detectChanges(), 0))
    ).subscribe({
      next: (res: any) => {
       this.spinner.hide()
        if (res.success) this.bindGrid(res.result || []);
        else Swal.fire('Error', res.message, 'error');
        this.cdr.detectChanges();
      },
      error: (err) => 
        {
          this.handleReportError(err)
       this.spinner.hide()

        }
    });
  }

  CostVarianceAcrossWarehousesMethod() {
   this.spinner.show()
    this.title = 'Cost Variance Across Warehouses';
     this.activeSection = 'CostVarianceAcrossWarehouses';
    this.cdr.detectChanges();

  this.svc.CostVarianceAcrossWarehouses().pipe(
  delay(0),
  tap(() => setTimeout(() => this.cdr.detectChanges(), 0))
).subscribe({
  next: (res: any) => {
    this.spinner.hide();
    if (res.success) {
      this.bindGrid(res.result || []);
    } else {
      Swal.fire('Error', res.message, 'error');
    }
    this.cdr.detectChanges();
  },
  error: (err: any) => {
    this.spinner.hide();
    this.handleReportError(err);
  }
});
  }

 getValue(row: any, col: string): any {
  const value = row?.[col];

  if (!value) return '-';

  if (value instanceof Date) {
    return this.formatDate(value);
  }

  if (typeof value === 'string' && /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}/.test(value)) {
    const d = new Date(value);
    return this.formatDate(d);
  }

  return value;
}

// Helper to format date as YYYY-MM-DD
private formatDate(d: Date): string {
  return `${d.getFullYear()}-${('0' + (d.getMonth() + 1)).slice(-2)}-${('0' + d.getDate()).slice(-2)}`;
}

  CostVarianceCurrentVsAvgMethod() {
   this.spinner.show()
    this.title = 'Compare Variance Current vs Avg Per Item';
      this.activeSection = 'CostVarianceCurrentVsAvg';
    this.cdr.detectChanges();

    this.svc.CostVarianceCurrentVsAvg().pipe(
      delay(0),
      tap(() => setTimeout(() => this.cdr.detectChanges(), 0))
    ).subscribe({
      next: (res: any) => {
       this.spinner.hide()
        if (res.success) this.bindGrid(res.result || []);
        else Swal.fire('Error', res.message, 'error');
        this.cdr.detectChanges();
       this.spinner.hide()

      },
      error: (err) => this.handleReportError(err)
      
    });
  }

  RDMethod() {
   this.spinner.show()
    this.title = 'Compare RD Hardware Cost To Spire Current';
     this.activeSection = 'RD';
    this.cdr.detectChanges();

    this.svc.RDHardwareVsSpire().pipe(
      delay(0),
      tap(() => setTimeout(() => this.cdr.detectChanges(), 0))
    ).subscribe({
      next: (res: any) => {
       this.spinner.hide()
        if (res.success) 
        {
       this.spinner.hide()

          this.bindGrid(res.result || []);
        }
        else Swal.fire('Error', res.message, 'error');
       this.spinner.hide()

        this.cdr.detectChanges();
      },
      error: (err) => this.handleReportError(err)
    });
  }

  private handleReportError(err: any) {
   this.spinner.hide()
    this.cdr.detectChanges();
    Swal.fire('API Error', err.message || 'Server error', 'error');
  }

  // ========== EXPORT METHODS ==========

  // downloadTemplate() {
  //   this.spinner.show()
  //   const headers = [['Whse', 'Part', 'StartDate', 'RogersCost', 'DelistDate']];
  //   const ws = XLSX.utils.aoa_to_sheet(headers);
  //   ws['!cols'] = [{ wch: 12 }, { wch: 25 }, { wch: 15 }, { wch: 15 }, { wch: 15 }];

  //   const wb = XLSX.utils.book_new();
  //   XLSX.utils.book_append_sheet(wb, ws, 'Template');
  //   const buffer = XLSX.write(wb, { bookType: 'xlsx', type: 'array' });
  //   saveAs(new Blob([buffer]), 'HPC_Template.xlsx');
  //   this.spinner.hide()
  //   Swal.fire({ icon: 'success', title: 'Template Downloaded', timer: 2000, showConfirmButton: false });
  // }

  downloadTemplate() {
  this.spinner.show();
  
  const headers = [['Whse', 'Part', 'StartDate', 'RogersCost', 'DelistDate']];
  const ws = XLSX.utils.aoa_to_sheet(headers);

  // 1. Set Column Widths
  ws['!cols'] = [{ wch: 12 }, { wch: 25 }, { wch: 15 }, { wch: 15 }, { wch: 15 }];

  // 2. Apply Bold Style to the First Row (Header)
  const range = XLSX.utils.decode_range(ws['!ref']!);
  for (let col = range.s.c; col <= range.e.c; col++) {
    const cellAddress = XLSX.utils.encode_cell({ r: 0, c: col }); // r: 0 is the first row
    if (!ws[cellAddress]) continue;

    ws[cellAddress].s = {
      font: {
        bold: true,
        sz: 12 
      },
      alignment: { horizontal: "center" } 
    };
  }

  const wb = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(wb, ws, 'Template');

  // 3. Generate Buffer and Save
  const buffer = XLSX.write(wb, { bookType: 'xlsx', type: 'array' });
  saveAs(new Blob([buffer]), 'HPC_Template.xlsx');

  this.spinner.hide();
  Swal.fire({ 
    icon: 'success', 
    title: 'Template Downloaded', 
    timer: 2000, 
    showConfirmButton: false 
  });
}

exportToExcel(name?: string) {
  if (!this.model || this.model.length === 0) {
    Swal.fire({ icon: 'info', title: 'No Data', text: 'No data available to export' });
    return;
  }

  const headerKeys = Object.keys(this.model[0]);
  const formattedHeaders = headerKeys.map(key => 
    key.replace(/([A-Z])/g, ' $1').replace(/^./, str => str.toUpperCase()).trim()
  );

  const dataRows = this.model.map(row => headerKeys.map(k => row[k]));
  const ws = XLSX.utils.aoa_to_sheet([formattedHeaders, ...dataRows]);

  const range = XLSX.utils.decode_range(ws['!ref']!);
  for (let col = range.s.c; col <= range.e.c; col++) {
    const cell = ws[XLSX.utils.encode_cell({ r: 0, c: col })];
    if (cell) {
      cell.s = { font: { bold: true } };
    }
  }

  ws['!cols'] = headerKeys.map(() => ({ wch: 20 }));

  const wb = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(wb, ws, 'Sheet1');
  
  const filename = (name || 'Export').replace(/\s+/g, '_') + '.xlsx';
  const buffer = XLSX.write(wb, { bookType: 'xlsx', type: 'array' });
  saveAs(new Blob([buffer]), filename);
  Swal.fire({ icon: 'success', title: 'Exported', timer: 2000, showConfirmButton: false });
}

exportInvalidRows() {
  if (!this.invalidRows || this.invalidRows.length === 0) {
    Swal.fire({ icon: 'info', title: 'No Invalid Records', text: 'No invalid records to export' });
    return;
  }

  const wsData = [
    ['Row Number', 'SKU', 'Invalid Column', 'Invalid Value', 'Reason'],
    ...this.invalidRows.map((row: any) => [
      row.RowNumber || '-',
      row.SKU || '-',
      row.Column || '-',
      row.Value || '-',
      row.Reason || '-'
    ])
  ];

  const ws = XLSX.utils.aoa_to_sheet(wsData);

  // Apply Bold Style to Header Row
  const range = XLSX.utils.decode_range(ws['!ref']!);
  for (let col = range.s.c; col <= range.e.c; col++) {
    const cell = ws[XLSX.utils.encode_cell({ r: 0, c: col })];
    if (cell) {
      cell.s = { font: { bold: true } };
    }
  }

  ws['!cols'] = [{ wch: 12 }, { wch: 20 }, { wch: 20 }, { wch: 20 }, { wch: 50 }];

  const wb = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(wb, ws, 'Invalid Records');
  
  const filename = `Invalid_Records_${new Date().toISOString().split('T')[0]}.xlsx`;
  const buffer = XLSX.write(wb, { bookType: 'xlsx', type: 'array' });
  saveAs(new Blob([buffer]), filename);
  Swal.fire({ icon: 'success', title: 'Exported', timer: 2000, showConfirmButton: false });
}
}