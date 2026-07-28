import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { SpinnerService } from '../../shared/spinner/spinner-service';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';
import { 
  PriceProtectionService, 
  PriceProtectionBatchRow, 
  ReceiptInfoBO, 
  PostedClaimSummaryBO 
} from './priceprotection.service';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-price-protection',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './priceprotection.html',
  styleUrls: ['./priceprotection.css']
})
export class PriceProtectionComponent implements OnInit {
  // Active view tabs: 'create' or 'summary'
  activeTab: string = 'create';
  showCurrentBatch: boolean = false;
  showSummary: boolean = false;

  // Dropdowns
  skus: any[] = [];
  nextBatchId: string = 'n/a';

  // Section 1: Generate PP Claim Based on Onhand Qty at Specific Date
  selectedSku: string = '';
  description: string = '';
  onhandDate: string = '';
  priceBefore: number = 0;
  priceAfter: number = 0;
  statusOnhand: string = '';

  // Section 2: Generate PP Claim Based on single Receipt
  receiptNo: string = '';
  partReceipt: string = '';
  descriptionReceipt: string = '';
  receiptCost: number = 0;
  receiptQty: number = 0;
  poNumber: string = '';
  priceDropDateReceipt: string = '';
  priceBeforeReceipt: number = 0;
  priceAfterReceipt: number = 0;
  statusReceipt: string = '';

  // Manual Add / Remove IMEI
  manualImeiAdd: string = '';
  manualImeiRemove: string = '';

  // Remove Batch
  batchNoToRemove: string = '';

  // Output Raw Claim Data
  priceDropDateStart: string = '';
  priceDropDateEnd: string = '';

  // Current Batch Grid Data
  batchRows: PriceProtectionBatchRow[] = [];

  // Summary Grid Data
  postedSummaries: PostedClaimSummaryBO[] = [];

  // Sorting and filtering states for grids
  batchSortKey: string = '';
  batchSortAsc: boolean = true;
  summarySortKey: string = '';
  summarySortAsc: boolean = true;

  constructor(
    private ppService: PriceProtectionService,
    private http: HttpClient,
    private spinner: SpinnerService,
    private toastr: ToastrService,
    private cdr: ChangeDetectorRef,
    private router: Router
  ) { }

  ngOnInit(): void {
    // Default dates
    const today = new Date();
    const todayStr = today.toISOString().split('T')[0];
    this.onhandDate = todayStr;
    this.priceDropDateReceipt = todayStr;
    this.priceDropDateStart = todayStr;
    this.priceDropDateEnd = todayStr;

    this.loadSkus();
    this.loadNextBatchId();
    this.loadCurrentBatchGrid();
    this.loadPostedSummaryGrid();
    this.cdr.detectChanges();
  }

  loadSkus(): void {
    this.http.get<any[]>(`${environment.apiUrl}/Sku`).subscribe({
      next: (data) => {
        this.skus = data;
        this.cdr.detectChanges();
      },
      error: () => {
        this.toastr.error('Failed to load SKUs list.');
        this.cdr.detectChanges();
      }
    });
  }

  loadNextBatchId(): void {
    this.ppService.getNextBatchID().subscribe({
      next: (res) => {
        if (res.success) {
          this.nextBatchId = res.result.toString();
        }
        this.cdr.detectChanges();
      },
      error: () => {
        this.cdr.detectChanges();
      }
    });
  }

  onSkuChange(): void {
    const matched = this.skus.find(s => s.sku === this.selectedSku);
    this.description = matched ? matched.description || '' : '';
    this.cdr.detectChanges();
  }

  // #region Load Data & Process (Onhand)
  loadOnhandData(): void {
    if (!this.selectedSku) {
      Swal.fire('Warning', 'You must select a part number.', 'warning');
      return;
    }
    if (!this.onhandDate) {
      Swal.fire('Warning', 'You must enter the onhand date.', 'warning');
      return;
    }

    Swal.fire({
      title: 'Load Data',
      text: 'Any previously loaded data will be removed. Are you sure?',
      icon: 'question',
      showCancelButton: true,
      confirmButtonText: 'Yes',
      cancelButtonText: 'Cancel'
    }).then((result) => {
      if (result.isConfirmed) {
        this.spinner.show();
        this.ppService.loadClaimData(this.selectedSku, this.onhandDate).subscribe({
          next: (res) => {
            this.spinner.hide();
            if (res.success) {
              Swal.fire('Success', 'Data Loaded', 'success');
              this.loadCurrentBatchGrid();
            } else {
              Swal.fire('Error', res.message || 'Failed to load data.', 'error');
            }
            this.cdr.detectChanges();
          },
          error: (err) => {
            this.spinner.hide();
            Swal.fire('Error', err.error?.message || 'Error occurred.', 'error');
            this.cdr.detectChanges();
          }
        });
      }
    });
  }

  processOnhandClaim(): void {
    if (!this.selectedSku) {
      Swal.fire('Warning', 'You must select a part number.', 'warning');
      return;
    }
    if (this.priceBefore === 0) {
      Swal.fire('Warning', 'You must enter a price before drop.', 'warning');
      return;
    }
    if (this.priceAfter === 0) {
      Swal.fire('Warning', 'You must enter a price after drop.', 'warning');
      return;
    }

    Swal.fire({
      title: 'Process Data',
      text: 'Any previously processed claim data will be removed. Are you sure?',
      icon: 'question',
      showCancelButton: true,
      confirmButtonText: 'Yes',
      cancelButtonText: 'Cancel'
    }).then((result) => {
      if (result.isConfirmed) {
        this.spinner.show();
        this.ppService.processOnhandClaim(this.selectedSku, this.onhandDate, this.priceBefore, this.priceAfter).subscribe({
          next: (res) => {
            this.spinner.hide();
            if (res.success) {
              this.statusOnhand = 'Claim processed. You may now view the claim.';
              Swal.fire('Success', 'Processing of claim complete.', 'success');
              this.loadCurrentBatchGrid();
            } else {
              this.statusOnhand = 'Processing failed.';
              Swal.fire('Error', res.message || 'Processing failed.', 'error');
            }
            this.cdr.detectChanges();
          },
          error: (err) => {
            this.spinner.hide();
            this.statusOnhand = 'Error occurred.';
            Swal.fire('Error', err.error?.message || 'Error occurred.', 'error');
            this.cdr.detectChanges();
          }
        });
      }
    });
  }
  // #endregion

  // #region Receipt Methods
  findReceipt(): void {
    if (!this.receiptNo) {
      Swal.fire('Warning', 'You must enter a receipt number.', 'warning');
      return;
    }

    this.spinner.show();
    this.ppService.findReceipt(this.receiptNo).subscribe({
      next: (res) => {
        this.spinner.hide();
        if (res.success && res.result) {
          const info: ReceiptInfoBO = res.result;
          this.partReceipt = info.partNo || '';
          this.descriptionReceipt = info.description || '';
          this.receiptCost = info.cost;
          this.receiptQty = info.qty;
          this.poNumber = info.poNumber || '';
          this.toastr.success('Receipt loaded.');
        } else {
          this.partReceipt = '';
          this.descriptionReceipt = '';
          this.receiptCost = 0;
          this.receiptQty = 0;
          this.poNumber = '';
          Swal.fire('Not Found', 'Receipt not found', 'info');
        }
        this.cdr.detectChanges();
      },
      error: () => {
        this.spinner.hide();
        Swal.fire('Error', 'Failed to find receipt.', 'error');
        this.cdr.detectChanges();
      }
    });
  }

  processReceiptClaim(): void {
    if (!this.receiptNo) {
      Swal.fire('Warning', 'You must enter a receipt number.', 'warning');
      return;
    }
    if (this.priceBeforeReceipt === 0) {
      Swal.fire('Warning', 'You must enter a price before drop.', 'warning');
      return;
    }
    if (this.priceAfterReceipt === 0) {
      Swal.fire('Warning', 'You must enter a price after drop.', 'warning');
      return;
    }

    Swal.fire({
      title: 'Continue?',
      text: 'This will remove any existing Price Protection batch pending. Are you sure you wish to continue?',
      icon: 'question',
      showCancelButton: true,
      confirmButtonText: 'Yes',
      cancelButtonText: 'Cancel'
    }).then((result) => {
      if (result.isConfirmed) {
        this.spinner.show();
        this.ppService.processReceiptClaim(this.receiptNo, this.priceDropDateReceipt, this.priceBeforeReceipt, this.priceAfterReceipt).subscribe({
          next: (res) => {
            this.spinner.hide();
            if (res.success) {
              this.statusReceipt = `Processed: ${res.count} units.`;
              Swal.fire('Success', 'Generation of claim for single receipt is complete.', 'success');
              this.loadCurrentBatchGrid();
            } else {
              this.statusReceipt = 'Failed to generate claim.';
              Swal.fire('Error', res.message || 'Failed to process.', 'error');
            }
            this.cdr.detectChanges();
          },
          error: (err) => {
            this.spinner.hide();
            Swal.fire('Error', err.error?.message || 'Error occurred.', 'error');
            this.cdr.detectChanges();
          }
        });
      }
    });
  }
  // #endregion

  // #region Manual Add/Remove IMEI
  manualAddImei(): void {
    if (!this.manualImeiAdd) {
      Swal.fire('Warning', 'You must enter an IMEI.', 'warning');
      return;
    }
    if (!this.selectedSku) {
      Swal.fire('Warning', 'Please select a Part Number in the Onhand section.', 'warning');
      return;
    }
    if (this.priceBefore === 0 || this.priceAfter === 0) {
      Swal.fire('Warning', 'Please specify Price Before and Price After in the Onhand section.', 'warning');
      return;
    }

    // Ask confirmation with details like MS Access
    Swal.fire({
      title: 'Add single IMEI?',
      text: `IMEI: ${this.manualImeiAdd}\nPart Number: ${this.selectedSku}`,
      icon: 'question',
      showCancelButton: true,
      confirmButtonText: 'OK',
      cancelButtonText: 'Cancel'
    }).then((result) => {
      if (result.isConfirmed) {
        this.spinner.show();
        this.ppService.manualAddImei(
          this.manualImeiAdd, 
          this.priceBefore, 
          this.priceAfter, 
          this.onhandDate, 
          this.selectedSku, 
          this.description
        ).subscribe({
          next: (res) => {
            this.spinner.hide();
            if (res.success) {
              Swal.fire('Added', 'IMEI added to batch.', 'success');
              this.manualImeiAdd = '';
              this.loadCurrentBatchGrid();
            } else {
              Swal.fire('Error', res.message || 'Failed to add IMEI.', 'error');
            }
            this.cdr.detectChanges();
          },
          error: (err) => {
            this.spinner.hide();
            Swal.fire('Error', err.error?.message || 'Error occurred.', 'error');
            this.cdr.detectChanges();
          }
        });
      }
    });
  }

  manualRemoveImei(): void {
    if (!this.manualImeiRemove) {
      Swal.fire('Warning', 'You must enter an IMEI to remove.', 'warning');
      return;
    }

    this.spinner.show();
    this.ppService.manualRemoveImei(this.manualImeiRemove).subscribe({
      next: (res) => {
        this.spinner.hide();
        if (res.success) {
          Swal.fire('Removed', 'IMEI Removed from batch.', 'success');
          this.manualImeiRemove = '';
          this.loadCurrentBatchGrid();
        } else {
          Swal.fire('Error', 'IMEI is not in the batch.', 'error');
        }
        this.cdr.detectChanges();
      },
      error: () => {
        this.spinner.hide();
        Swal.fire('Error', 'Failed to remove IMEI.', 'error');
        this.cdr.detectChanges();
      }
    });
  }
  // #endregion

  // #region Append Claim
  appendClaimBatch(): void {
    if (this.batchRows.length === 0) {
      Swal.fire('Warning', 'Current batch has no records to append.', 'warning');
      return;
    }

    // Prompt for password
    Swal.fire({
      title: 'Enter Password',
      input: 'password',
      inputPlaceholder: 'Enter security password...',
      inputAttributes: {
        autocapitalize: 'off',
        autocorrect: 'off'
      },
      showCancelButton: true,
      confirmButtonText: 'Append Claim'
    }).then((result) => {
      if (result.isConfirmed && result.value) {
        this.spinner.show();
        this.ppService.appendClaim(result.value).subscribe({
          next: (res) => {
            this.spinner.hide();
            if (res.success) {
              Swal.fire('Appended', 'Claim Appended successfully.', 'success');
              this.loadCurrentBatchGrid();
              this.loadPostedSummaryGrid();
              this.loadNextBatchId();
            } else {
              Swal.fire('Error', res.message || 'Append failed.', 'error');
            }
            this.cdr.detectChanges();
          },
          error: (err) => {
            this.spinner.hide();
            const msg = err.status === 401 ? 'Password incorrect.' : (err.error?.message || 'Error occurred.');
            Swal.fire('Error', msg, 'error');
            this.cdr.detectChanges();
          }
        });
      }
    });
  }
  // #endregion

  // #region Remove Batch
  removeBatch(): void {
    if (!this.batchNoToRemove || isNaN(Number(this.batchNoToRemove))) {
      Swal.fire('Warning', 'You must enter a valid batch number to remove.', 'warning');
      return;
    }

    Swal.fire({
      title: 'Remove Batch?',
      text: `Are you sure you want to remove the batch ${this.batchNoToRemove}?`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Yes',
      cancelButtonText: 'Cancel'
    }).then((result) => {
      if (result.isConfirmed) {
        this.spinner.show();
        this.ppService.removeBatch(Number(this.batchNoToRemove)).subscribe({
          next: (res) => {
            this.spinner.hide();
            if (res.success) {
              Swal.fire('Removed', `Batch ${this.batchNoToRemove} removed.`, 'success');
              this.batchNoToRemove = '';
              this.loadPostedSummaryGrid();
            } else {
              Swal.fire('Error', res.message || 'Failed to remove batch.', 'error');
            }
            this.cdr.detectChanges();
          },
          error: (err) => {
            this.spinner.hide();
            Swal.fire('Error', err.error?.message || 'Error occurred.', 'error');
            this.cdr.detectChanges();
          }
        });
      }
    });
  }
  // #endregion

  // #region Raw Data Export
  outputRawClaimData(): void {
    if (!this.priceDropDateStart || !this.priceDropDateEnd) {
      Swal.fire('Warning', 'Please specify both Start and End drop dates.', 'warning');
      return;
    }

    // Check start date is not greater than end date
    const dStart = new Date(this.priceDropDateStart);
    const dEnd = new Date(this.priceDropDateEnd);
    if (dStart > dEnd) {
      Swal.fire('Warning', 'Start date cannot be greater than End date.', 'warning');
      return;
    }

    // Check end date is not in future
    const today = new Date();
    today.setHours(23, 59, 59, 999);
    if (dEnd > today) {
      Swal.fire('Warning', 'End date cannot be in the future.', 'warning');
      return;
    }

    this.spinner.show();
    this.ppService.exportRawData(this.priceDropDateStart, this.priceDropDateEnd).subscribe({
      next: (blob) => {
        this.spinner.hide();
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `PriceProtectionRawData_${this.priceDropDateStart}_to_${this.priceDropDateEnd}.xlsx`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
        this.toastr.success('Excel downloaded successfully.');
        this.cdr.detectChanges();
      },
      error: () => {
        this.spinner.hide();
        Swal.fire('Error', 'Failed to export Excel file.', 'error');
        this.cdr.detectChanges();
      }
    });
  }
  // #endregion

  // #region Grids loading & sorting
  loadCurrentBatchGrid(): void {
    this.ppService.getBatchData().subscribe({
      next: (res) => {
        if (res.success) {
          this.batchRows = res.result || [];
          this.sortBatchRows();
        }
        this.cdr.detectChanges();
      },
      error: () => {
        this.cdr.detectChanges();
      }
    });
  }

  loadPostedSummaryGrid(): void {
    this.ppService.getPostedSummary().subscribe({
      next: (res) => {
        if (res.success) {
          this.postedSummaries = res.result || [];
          this.sortPostedSummaries();
        }
        this.cdr.detectChanges();
      },
      error: () => {
        this.cdr.detectChanges();
      }
    });
  }

  sortBatch(key: string): void {
    if (this.batchSortKey === key) {
      this.batchSortAsc = !this.batchSortAsc;
    } else {
      this.batchSortKey = key;
      this.batchSortAsc = true;
    }
    this.sortBatchRows();
    this.cdr.detectChanges();
  }

  sortBatchRows(): void {
    if (!this.batchSortKey) return;
    this.batchRows.sort((a: any, b: any) => {
      const valA = a[this.batchSortKey];
      const valB = b[this.batchSortKey];
      if (valA === valB) return 0;
      if (valA == null) return this.batchSortAsc ? -1 : 1;
      if (valB == null) return this.batchSortAsc ? 1 : -1;
      if (typeof valA === 'string') {
        return this.batchSortAsc ? valA.localeCompare(valB) : valB.localeCompare(valA);
      }
      return this.batchSortAsc ? valA - valB : valB - valA;
    });
  }

  sortSummary(key: string): void {
    if (this.summarySortKey === key) {
      this.summarySortAsc = !this.summarySortAsc;
    } else {
      this.summarySortKey = key;
      this.summarySortAsc = true;
    }
    this.sortPostedSummaries();
    this.cdr.detectChanges();
  }

  sortPostedSummaries(): void {
    if (!this.summarySortKey) return;
    this.postedSummaries.sort((a: any, b: any) => {
      const valA = a[this.summarySortKey];
      const valB = b[this.summarySortKey];
      if (valA === valB) return 0;
      if (valA == null) return this.summarySortAsc ? -1 : 1;
      if (valB == null) return this.summarySortAsc ? 1 : -1;
      if (typeof valA === 'string') {
        return this.summarySortAsc ? valA.localeCompare(valB) : valB.localeCompare(valA);
      }
      return this.summarySortAsc ? valA - valB : valB - valA;
    });
  }
  // #endregion

  toggleCurrentBatch(): void {
    this.showCurrentBatch = !this.showCurrentBatch;
    this.showSummary = false;
    this.cdr.detectChanges();
  }

  toggleSummary(): void {
    this.showSummary = !this.showSummary;
    this.showCurrentBatch = false;
    this.cdr.detectChanges();
  }

  exit(): void {
    this.router.navigate(['/dashboard']);
  }
}
