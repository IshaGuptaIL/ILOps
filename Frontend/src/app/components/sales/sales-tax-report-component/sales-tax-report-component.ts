import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SalesTaxReportService, SalesTaxReportRequest, SalesTaxReportRow, TaxCodeHistory, VendorBO } from './sales-tax-report.service';
import { SpinnerService } from '../../shared/spinner/spinner-service';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-sales-tax-report',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './sales-tax-report-component.html',
  styleUrls: ['./sales-tax-report-component.css']
})
export class SalesTaxReportComponent implements OnInit {
  // Input fields
  startDate: string = '';
  endDate: string = '';

  // Status fields (matching VBA labels)
  SHStartDate: string = '';
  SHEndDate: string = '';
  SHLoadDate: string = '';

  GLTaxStartDate: string = '';
  GLTaxEndDate: string = '';
  GLTaxLoadDate: string = '';

  GLITCStartDate: string = '';
  GLITCEndDate: string = '';
  GLITLoadDate: string = '';

  // Flags for status
  isSalesHistoryLoaded: boolean = false;
  isGLDataLoaded: boolean = false;
  isGLITCDataLoaded: boolean = false;

  // Tax Code History Screen
  showHistoryScreen: boolean = false;
  taxCodeHistoryList: TaxCodeHistory[] = [];
  currentTaxCode: TaxCodeHistory = { id: 0, provCode: '', provinceName: '', tax1Rate: 0, tax2Rate: 0, taxType: '', startDate: '', endDate: '', comments: '', compoundTax2OnTax1: false };

  vendorList: VendorBO[] = [];

  constructor(
    private salesService: SalesTaxReportService,
    private spinner: SpinnerService,
    private toastr: ToastrService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    const now = new Date();
    const firstDay = new Date(now.getFullYear(), now.getMonth(), 1);
    this.startDate = firstDay.toISOString().split('T')[0];
    this.endDate = now.toISOString().split('T')[0];
    
    this.loadVendors();
    this.cdr.detectChanges();
  }

  loadVendors(): void {
    this.salesService.getVendors().subscribe({
      next: (data) => {
        this.vendorList = data;
        this.cdr.detectChanges();
      },
      error: () => { this.toastr.error('Failed to load vendors'); }
    });
  }

  editHistory(): void {
    Swal.fire({
      title: 'Enter password',
      input: 'password',
      showCancelButton: true,
      confirmButtonText: 'OK',
    }).then((result) => {
      if (result.value === 'HFC') {
        this.toastr.success('Access Granted');
        this.showHistoryScreen = true;
        this.loadTaxCodeHistory();
      } else if (result.dismiss !== Swal.DismissReason.cancel) {
        this.toastr.error('Password Incorrect');
      }
    });
    this.cdr.detectChanges();
  }

  loadTaxCodeHistory(): void {
    this.spinner.show();
    this.salesService.getTaxCodeHistory().subscribe({
      next: (data) => {
        // Format dates for input fields
        this.taxCodeHistoryList = data.map(d => ({ 
          ...d, 
          startDate: d.startDate ? d.startDate.split('T')[0] : '',
          endDate: d.endDate ? d.endDate.split('T')[0] : ''
        }));
        this.spinner.hide();
        this.cdr.detectChanges();
      },
      error: () => { this.spinner.hide(); this.toastr.error('Failed to load history'); }
    });
  }

  saveTaxCode(): void {
    if (!this.currentTaxCode.provCode || !this.currentTaxCode.startDate) {
      this.toastr.warning('Prov Code and Start Date are required.');
      return;
    }
    this.spinner.show();
    this.salesService.saveTaxCodeHistory(this.currentTaxCode).subscribe({
      next: () => {
        this.toastr.success('Saved successfully');
        this.currentTaxCode = { id: 0, provCode: '', provinceName: '', tax1Rate: 0, tax2Rate: 0, taxType: '', startDate: '', endDate: '', comments: '', compoundTax2OnTax1: false };
        this.loadTaxCodeHistory();
      },
      error: () => { this.spinner.hide(); this.toastr.error('Failed to save'); }
    });
  }

  editTaxCodeItem(item: TaxCodeHistory): void {
    this.currentTaxCode = { ...item };
  }

  deleteTaxCode(id: number): void {
    Swal.fire({
      title: 'Are you sure?',
      text: "You won't be able to revert this!",
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      confirmButtonText: 'Yes, delete it!'
    }).then((result) => {
      if (result.isConfirmed) {
        this.spinner.show();
        this.salesService.deleteTaxCodeHistory(id).subscribe({
          next: () => {
            this.toastr.success('Deleted successfully');
            this.loadTaxCodeHistory();
          },
          error: () => { this.spinner.hide(); this.toastr.error('Failed to delete'); }
        });
      }
    });
  }

  closeHistoryScreen(): void {
    this.showHistoryScreen = false;
    this.currentTaxCode = { id: 0, provCode: '', provinceName: '', tax1Rate: 0, tax2Rate: 0, taxType: '', startDate: '', endDate: '', comments: '', compoundTax2OnTax1: false };
  }

  loadSalesTaxHistory(): void {
    if (!this.startDate || !this.endDate) {
      this.toastr.warning('You must enter valid dates for Start Date and End Date.');
      return;
    }
    this.spinner.show();
    const request: SalesTaxReportRequest = { startDate: this.startDate, endDate: this.endDate };
    this.salesService.loadSalesHistory(request).subscribe({
      next: (success) => {
        if (success) {
          this.SHStartDate = this.startDate;
          this.SHEndDate = this.endDate;
          this.SHLoadDate = new Date().toLocaleString();
          this.isSalesHistoryLoaded = true;
          this.toastr.success('Data for Sales Tax History Loaded');
        } else {
          this.toastr.error('Failed to load data');
        }
        this.spinner.hide();
        this.cdr.detectChanges();
      },
      error: () => { this.spinner.hide(); this.toastr.error('API Error: Failed to load data'); this.cdr.detectChanges(); }
    });
  }

  previewData(): void {
    if (!this.isSalesHistoryLoaded) {
      this.toastr.warning('It does not appear that data is properly loaded.');
      return;
    }
    this.spinner.show();
    const request: SalesTaxReportRequest = { startDate: this.SHStartDate, endDate: this.SHEndDate };
    this.salesService.exportExcel(request).subscribe({
      next: (blob) => {
        this.downloadBlob(blob, `TaxData_${this.SHStartDate}.xlsx`);
        this.spinner.hide();
        this.toastr.success('Sales Tax Data Previewed (Excel Downloaded)');
        this.cdr.detectChanges();
      },
      error: () => { this.spinner.hide(); this.toastr.error('Failed to preview data'); this.cdr.detectChanges(); }
    });
  }

  loadGLData(): void {
    if (!this.startDate || !this.endDate) {
      this.toastr.warning('You must enter valid dates for Start Date and End Date.');
      return;
    }
    Swal.fire({
      title: 'Warning',
      text: 'This app may need slight changes once it is processing transactions posted by Spire',
      icon: 'warning',
      confirmButtonText: 'OK'
    }).then(() => {
      this.spinner.show();
      const request: SalesTaxReportRequest = { startDate: this.startDate, endDate: this.endDate };
      this.salesService.loadGLData(request).subscribe({
        next: (success) => {
          if (success) {
            this.GLTaxStartDate = this.startDate;
            this.GLTaxEndDate = this.endDate;
            this.GLTaxLoadDate = new Date().toLocaleString();
            this.isGLDataLoaded = true;
            this.toastr.success('Data for GL Tax Accounts Loaded');
          } else {
            this.toastr.error('Failed to load GL data');
          }
          this.spinner.hide();
          this.cdr.detectChanges();
        },
        error: () => { this.spinner.hide(); this.toastr.error('API Error: Failed to load GL data'); this.cdr.detectChanges(); }
      });
    });
  }

  previewGLData(): void {
    if (!this.isGLDataLoaded) {
      this.toastr.warning('It does not appear that GL Tax Account data is loaded correctly.');
      return;
    }
    this.spinner.show();
    const request: SalesTaxReportRequest = { startDate: this.GLTaxStartDate, endDate: this.GLTaxEndDate };
    this.salesService.exportGLDataExcel(request).subscribe({
      next: (blob) => {
        this.downloadBlob(blob, `GLTaxData_${this.GLTaxStartDate}.xlsx`);
        this.spinner.hide();
        this.toastr.success('GL Tax Data Previewed (Excel Downloaded)');
        this.cdr.detectChanges();
      },
      error: () => { this.spinner.hide(); this.toastr.error('Failed to preview GL data'); this.cdr.detectChanges(); }
    });
  }

  loadGLITCData(): void {
    if (!this.startDate || !this.endDate) {
      this.toastr.warning('You must enter valid dates for Start Date and End Date.');
      return;
    }
    this.spinner.show();
    const request: SalesTaxReportRequest = { startDate: this.startDate, endDate: this.endDate };
    // GL ITC Data usually uses the same load logic as GL Data (WWGLTrans)
    this.salesService.loadGLData(request).subscribe({
      next: (success) => {
        if (success) {
          this.GLITCStartDate = this.startDate;
          this.GLITCEndDate = this.endDate;
          this.GLITLoadDate = new Date().toLocaleString();
          this.isGLITCDataLoaded = true;
          this.toastr.success('Data for GL ITC Tax Reports Loaded');
        } else {
          this.toastr.error('Failed to load GL ITC data');
        }
        this.spinner.hide();
        this.cdr.detectChanges();
      },
      error: () => { this.spinner.hide(); this.toastr.error('API Error: Failed to load GL ITC data'); this.cdr.detectChanges(); }
    });
  }

  previewGLITCData(): void {
    if (!this.isGLITCDataLoaded) {
      this.toastr.warning('It does not appear that GL ITC Tax Report data is loaded correctly.');
      return;
    }
    this.spinner.show();
    const request: SalesTaxReportRequest = { startDate: this.GLITCStartDate, endDate: this.GLITCEndDate };
    this.salesService.exportGLITCExcel(request).subscribe({
      next: (blob) => {
        this.downloadBlob(blob, `ITCCredits_${this.GLITCStartDate}.xlsx`);
        this.spinner.hide();
        this.toastr.success('GL ITC Data Previewed (Excel Downloaded)');
        this.cdr.detectChanges();
      },
      error: () => { this.spinner.hide(); this.toastr.error('Failed to preview ITC data'); this.cdr.detectChanges(); }
    });
  }

  vendorActivity(): void {
    Swal.fire({
      title: 'Vendor Activity',
      html: `
        <div class="text-start">
          <label class="form-label d-block small mb-1">Select Vendor:</label>
          <select id="swal-vendor" class="form-select form-select-sm mb-3">
            <option value="">Select Vendor...</option>
            ${this.vendorList.map(v => `<option value="${v.vendorNo}">${v.name} (${v.vendorNo})</option>`).join('')}
          </select>
          <label class="form-label d-block small mb-1">Start Date:</label>
          <input type="date" id="swal-start" class="form-control form-control-sm mb-3" value="${this.startDate}">
          <label class="form-label d-block small mb-1">End Date:</label>
          <input type="date" id="swal-end" class="form-control form-control-sm mb-3" value="${this.endDate}">
        </div>
      `,
      showCancelButton: true,
      confirmButtonText: 'Download Excel',
      preConfirm: () => {
        const vendor = (document.getElementById('swal-vendor') as HTMLSelectElement).value;
        const start = (document.getElementById('swal-start') as HTMLInputElement).value;
        const end = (document.getElementById('swal-end') as HTMLInputElement).value;
        if (!vendor) { Swal.showValidationMessage('You must select a vendor'); return; }
        if (!start || !end) { Swal.showValidationMessage('You must enter valid dates'); return; }
        return { vendor, start, end };
      }
    }).then((result) => {
      if (result.isConfirmed && result.value) {
        this.spinner.show();
        this.salesService.exportVendorActivity(result.value.vendor, result.value.start, result.value.end).subscribe({
          next: (blob) => {
            this.downloadBlob(blob, `VendorActivity-${result.value.vendor}.xlsx`);
            this.spinner.hide();
            this.toastr.success('Vendor Activity Exported');
            this.cdr.detectChanges();
          },
          error: () => { this.spinner.hide(); this.toastr.error('Export failed'); this.cdr.detectChanges(); }
        });
      }
    });
  }

  private downloadBlob(blob: Blob, filename: string): void {
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
    document.body.removeChild(a);
  }
}
