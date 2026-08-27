import { Component, OnInit, Inject, PLATFORM_ID, ChangeDetectorRef } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { forkJoin } from 'rxjs';
import { SpinnerService } from '../../../shared/spinner/spinner-service';
import { Spinner } from '../../../shared/spinner/spinner';
import { environment } from '../../../../../environments/environment';
import * as XLSX from 'xlsx';
import Swal from 'sweetalert2';

export interface ImportBatchSummary {
  cmFiles: string[];
  rmFiles: string[];
  manualFiles: string[];
}

export interface ReconcileFileSummary {
  importFileName: string;
  startDate?: string | Date;
  endDate?: string | Date;
  count: number;
}

export interface ReconcileFileType {
  importFileName: string;
  class: string;
  type: string;
  source: string;
  startDate?: string | Date;
  endDate?: string | Date;
  count: number;
  totalOther: number;
  cmTotal?: number;
  rmTotal?: number;
}

export interface RogersReportCMDetail {
  id: number;
  class?: string;
  source?: string;
  type?: string;
  operatingUnit?: string;
  legalEntityName?: string;
  number?: string;
  date?: string | Date;
  balanceDue?: number;
  discoverComment?: string;
  importFileName?: string;
}

@Component({
  selector: 'app-rogers-report-import',
  standalone: true,
  imports: [CommonModule, FormsModule, Spinner],
  templateUrl: './rogers-report-import.component.html',
  styleUrls: ['./rogers-report-import.component.css']
})
export class RogersReportImportComponent implements OnInit {
  apiUrl = `${environment.apiUrl}/sales/rmareporting`;

  // Selected files
  cmFile: File | null = null;
  rmFile: File | null = null;
  manualFile: File | null = null;

  cmFileName: string = '';
  rmFileName: string = '';
  manualFileName: string = '';

  // Delete batch selection
  selectedCmDelete: string = '';
  selectedRmDelete: string = '';
  selectedManualDelete: string = '';

  // Batch summary lists
  batchSummary: ImportBatchSummary = { cmFiles: [], rmFiles: [], manualFiles: [] };

  // Reconcile Modal (frmFILESReconcile)
  showCmSummaryModal: boolean = false;
  
  // Grid 1: frmFILESCMByFile
  reconcileFiles: ReconcileFileSummary[] = [];
  selectedFile: ReconcileFileSummary | null = null;
  selectedFileIndex: number = 0;

  // Grid 2: frmFILESCMByFileByType
  reconcileFileTypes: ReconcileFileType[] = [];
  selectedFileType: ReconcileFileType | null = null;
  selectedFileTypeIndex: number = 0;

  // Grid 3: frmRogersReportCM
  reconcileDetails: RogersReportCMDetail[] = [];
  selectedDetailIndex: number = 0;

  statusMessage: string = '';
  errorMessage: string = '';

  constructor(
    private http: HttpClient,
    public spinnerService: SpinnerService,
    private cdr: ChangeDetectorRef,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {}

  ngOnInit(): void {
    if (isPlatformBrowser(this.platformId)) {
      this.loadBatches();
    }
  }

  loadBatches(): void {
    this.http.get<ImportBatchSummary>(`${this.apiUrl}/import/batches`).subscribe({
      next: (data) => {
        this.batchSummary = data || { cmFiles: [], rmFiles: [], manualFiles: [] };
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.cdr.detectChanges();
      }
    });
  }

  onCmFileSelected(event: any): void {
    if (event.target.files && event.target.files.length > 0) {
      this.cmFile = event.target.files[0];
      this.cmFileName = this.cmFile ? this.cmFile.name : '';
      this.cdr.detectChanges();
    }
  }

  onRmFileSelected(event: any): void {
    if (event.target.files && event.target.files.length > 0) {
      this.rmFile = event.target.files[0];
      this.rmFileName = this.rmFile ? this.rmFile.name : '';
      this.cdr.detectChanges();
    }
  }

  onManualFileSelected(event: any): void {
    if (event.target.files && event.target.files.length > 0) {
      this.manualFile = event.target.files[0];
      this.manualFileName = this.manualFile ? this.manualFile.name : '';
      this.cdr.detectChanges();
    }
  }

  importFiles(): void {
    if (!this.cmFile && !this.rmFile && !this.manualFile) {
      this.errorMessage = 'Please select at least one file (RM, CM, or Manual RMA) to import.';
      Swal.fire({
        icon: 'warning',
        title: 'File Required',
        text: 'Please select at least one file (RM, CM, or Manual RMA) to import.'
      });
      return;
    }

    this.spinnerService.show();
    this.statusMessage = '';
    this.errorMessage = '';

    const uploadObservables: any[] = [];
    const fileSummaryList: string[] = [];

    if (this.rmFile) {
      const rmFormData = new FormData();
      rmFormData.append('file', this.rmFile);
      uploadObservables.push(this.http.post<any>(`${this.apiUrl}/import/rm`, rmFormData));
      fileSummaryList.push(`RM File: <b>${this.rmFileName}</b>`);
    }

    if (this.cmFile) {
      const cmFormData = new FormData();
      cmFormData.append('file', this.cmFile);
      uploadObservables.push(this.http.post<any>(`${this.apiUrl}/import/cm`, cmFormData));
      fileSummaryList.push(`CM File: <b>${this.cmFileName}</b>`);
    }

    if (this.manualFile) {
      const manualFormData = new FormData();
      manualFormData.append('file', this.manualFile);
      uploadObservables.push(this.http.post<any>(`${this.apiUrl}/import/manual`, manualFormData));
      fileSummaryList.push(`Manual RMA File: <b>${this.manualFileName}</b>`);
    }

    forkJoin(uploadObservables).subscribe({
      next: (responses) => {
        const msgs = responses.map((r) => r?.message || 'Imported successfully').join(' | ');
        this.statusMessage = msgs;
        
        // Reset selected files
        this.cmFile = null;
        this.cmFileName = '';
        this.rmFile = null;
        this.rmFileName = '';
        this.manualFile = null;
        this.manualFileName = '';

        this.loadBatches();
        this.spinnerService.hide();
        this.cdr.detectChanges();

        Swal.fire({
          icon: 'success',
          title: 'Import Successful',
          html: `<p>Successfully imported the following file(s):</p><ul style="text-align: left; margin-left: 20px;">${fileSummaryList.map(item => `<li>${item}</li>`).join('')}</ul>`
        });
      },
      error: (err) => {
        console.error('Import error:', err);
        this.errorMessage = 'An error occurred during file import. Please check file format and columns.';
        this.spinnerService.hide();
        this.cdr.detectChanges();
        Swal.fire({
          icon: 'error',
          title: 'Import Failed',
          text: err?.error?.message || 'An error occurred during file import. Please verify columns match the required template.'
        });
      }
    });
  }

  deleteBatch(): void {
    if (!this.selectedCmDelete && !this.selectedRmDelete && !this.selectedManualDelete) {
      this.errorMessage = 'Please select a batch to delete.';
      Swal.fire({
        icon: 'warning',
        title: 'Selection Required',
        text: 'Please select a batch to delete.'
      });
      return;
    }

    Swal.fire({
      title: 'Delete Import Batch?',
      text: 'Are you sure you want to delete the selected batch of imported files? This will remove related staging records.',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      cancelButtonColor: '#3085d6',
      confirmButtonText: 'Yes, delete',
      cancelButtonText: 'Cancel'
    }).then((result) => {
      if (result.isConfirmed) {
        this.spinnerService.show();
        this.http.post<any>(`${this.apiUrl}/import/delete-batch`, {
          cmFile: this.selectedCmDelete,
          rmFile: this.selectedRmDelete,
          manualFile: this.selectedManualDelete
        }).subscribe({
          next: (res) => {
            this.statusMessage = res?.message || 'Batch deleted successfully.';
            this.selectedCmDelete = '';
            this.selectedRmDelete = '';
            this.selectedManualDelete = '';
            this.loadBatches();
            this.spinnerService.hide();
            this.cdr.detectChanges();
            Swal.fire({
              icon: 'success',
              title: 'Deleted',
              text: 'Batch deleted successfully.'
            });
          },
          error: (err) => {
            this.errorMessage = 'Error deleting batch.';
            this.spinnerService.hide();
            this.cdr.detectChanges();
            Swal.fire({
              icon: 'error',
              title: 'Error',
              text: 'Error deleting batch.'
            });
          }
        });
      }
    });
  }

  // ==========================================
  // RECONCILE MODAL & CASCADING GRIDS (frmFILESReconcile)
  // ==========================================
  openCmSummary(): void {
    this.showCmSummaryModal = true;
    this.loadReconcileFiles();
  }

  closeCmSummary(): void {
    this.showCmSummaryModal = false;
    this.cdr.detectChanges();
  }

  loadReconcileFiles(): void {
    this.spinnerService.show();
    this.http.get<ReconcileFileSummary[]>(`${this.apiUrl}/reconcile/files`).subscribe({
      next: (data) => {
        this.reconcileFiles = data || [];
        this.spinnerService.hide();
        if (this.reconcileFiles.length > 0) {
          this.selectFile(this.reconcileFiles[0], 0);
        } else {
          this.reconcileFileTypes = [];
          this.reconcileDetails = [];
          this.selectedFile = null;
          this.selectedFileType = null;
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.spinnerService.hide();
        this.cdr.detectChanges();
      }
    });
  }

  selectFile(file: ReconcileFileSummary, index: number): void {
    this.selectedFile = file;
    this.selectedFileIndex = index;
    this.loadReconcileFileTypes(file.importFileName);
  }

  loadReconcileFileTypes(fileName: string): void {
    this.spinnerService.show();
    this.http.get<ReconcileFileType[]>(`${this.apiUrl}/reconcile/file-types?fileName=${encodeURIComponent(fileName)}`).subscribe({
      next: (data) => {
        this.reconcileFileTypes = data || [];
        this.spinnerService.hide();
        if (this.reconcileFileTypes.length > 0) {
          this.selectFileType(this.reconcileFileTypes[0], 0);
        } else {
          this.reconcileDetails = [];
          this.selectedFileType = null;
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.spinnerService.hide();
        this.cdr.detectChanges();
      }
    });
  }

  selectFileType(fileType: ReconcileFileType, index: number): void {
    this.selectedFileType = fileType;
    this.selectedFileTypeIndex = index;
    this.loadReconcileDetails(fileType.importFileName, fileType.class, fileType.type, fileType.source);
  }

  loadReconcileDetails(fileName: string, className?: string, typeName?: string, sourceName?: string): void {
    this.spinnerService.show();
    let url = `${this.apiUrl}/reconcile/details?fileName=${encodeURIComponent(fileName)}`;
    if (className) url += `&className=${encodeURIComponent(className)}`;
    if (typeName) url += `&typeName=${encodeURIComponent(typeName)}`;
    if (sourceName) url += `&sourceName=${encodeURIComponent(sourceName)}`;

    this.http.get<RogersReportCMDetail[]>(url).subscribe({
      next: (data) => {
        this.reconcileDetails = data || [];
        this.selectedDetailIndex = 0;
        this.spinnerService.hide();
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.spinnerService.hide();
        this.cdr.detectChanges();
      }
    });
  }

  selectDetail(index: number): void {
    this.selectedDetailIndex = index;
    this.cdr.detectChanges();
  }

  // ==========================================
  // TEMPLATE DOWNLOADS
  // ==========================================
  downloadRmaTemplate(): void {
    const wsData = [
      [
        'IMEI', 'RogersResponse', 'RMANumber', 'RMADate', 'HeaderReturnReason', 
        'FileName', 'ITEM', 'Qty', 'DateReceived', 'DateIssued', 
        'VPFLastMoveDate', 'VPFAssignDate', 'ReturnReason', 'CreditAmount', 
        'RestockFee', 'TotalCredit', 'Status', 'LastStatusMessage', 
        'RejectReason', 'RejectReasonComment'
      ],
      [
        '358901234567890', 'APPROVED', 'RMA-9001', '2015-03-05', 'DEFECTIVE',
        'RM_FEB_2015.xlsx', 'IPHONE6-16GB', 1, '2015-03-05', '2015-03-05',
        '2015-03-06', '2015-03-06', 'DOA', 149.93,
        0.00, 149.93, 'CLOSED', 'CREDITED',
        '', ''
      ]
    ];
    const ws = XLSX.utils.aoa_to_sheet(wsData);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'RMA_Template');
    XLSX.writeFile(wb, 'Template_RMA_File.xlsx');
  }

  downloadCmTemplate(): void {
    const wsData = [
      [
        'Class', 'Source', 'Type', 'Operating Unit', 'Legal Entity Name', 
        'Number', 'Date', 'Balance Due', 'Currency', 'DiscoverComment', 
        'CMNumber', 'CMDate', 'CMAmount', 'RMA', 'SKU', 
        'Qty', 'UnitPrice', 'RMAmount', 'RMAmountTotal', 'IMEIRMA'
      ],
      [
        'Credit Memo', 'NRIS Credit Memo OM', 'Dealer Credit Memo', 'RCI Operating Unit', 'RCI Legal Entity',
        '5067220', '2015-03-06', -1083.67, 'CAD', 'Matched',
        '5067220', '2015-03-06', 1083.67, 'RMA-9001', 'IPHONE6-16GB',
        1, 1083.67, 1083.67, 1083.67, '358901234567890'
      ]
    ];
    const ws = XLSX.utils.aoa_to_sheet(wsData);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'CM_Template');
    XLSX.writeFile(wb, 'Template_CM_File.xlsx');
  }

  downloadManualRmaTemplate(): void {
    const wsData = [
      [
        'SKU', 'IMEI', 'ReturnReasonCode', 'ExtraInfo', 'InvoiceSold', 
        'InvoiceSoldDate', 'WhseSold', 'BVCreditOrder', 'ReturnedRogers', 'ReturnedRogersBVOrder', 
        'Swap', 'SwapCMO', 'FinalDisposition', 'ReturnWaybill', 'LogInDate', 
        'CreditAmtClaimed', 'Status'
      ],
      [
        'IPHONE6-16GB', '358901234567890', 'BRBP', 'Warranty Replacement', 'INV-100234',
        '2015-01-15', 'WH01', 'BV-8891', 'YES', 'BV-9912',
        'NO', '', 'RETURN_TO_VENDOR', 'WB-4458901', '2015-03-01',
        149.93, 'IN_PROGRESS'
      ]
    ];
    const ws = XLSX.utils.aoa_to_sheet(wsData);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Manual_RMA_Template');
    XLSX.writeFile(wb, 'Template_Manual_RMA_File.xlsx');
  }
}
