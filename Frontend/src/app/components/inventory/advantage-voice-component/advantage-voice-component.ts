import { Component, OnInit } from '@angular/core';
import { AdvantageImportVM, AdvantageVoiceService } from '../advantage-voice-service';
import { ToastrService } from 'ngx-toastr';
import * as XLSX from 'xlsx-js-style';
import { saveAs } from 'file-saver';
import Swal from 'sweetalert2';
import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { ChangeDetectorRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SkuManagementComponent } from '../sku-management/sku-management.component';

@Component({
  selector: 'app-advantage-voice-component',
  standalone: true,
  imports: [CommonModule, HttpClientModule, FormsModule, SkuManagementComponent],
  templateUrl: './advantage-voice-component.html',
  styleUrl: './advantage-voice-component.css',
})
export class AdvantageVoiceComponent implements OnInit {
  pendingOrders: AdvantageImportVM[] = [];
  isLoading = false;
  currentView: 'import' | 'sku' = 'import';

  // Stats
  totalOrders = 0;
  validatedOrders = 0;
  failedOrders = 0;

  constructor(
    private service: AdvantageVoiceService,
    private toastr: ToastrService,
      private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.loadPendingOrders();
  }

  loadPendingOrders(): void {
    this.isLoading = true;
    this.service.getPendingImports().subscribe({
      next: (data) => {
        this.pendingOrders = data;
        this.updateStats();
         this.cdr.detectChanges(); 
      },
      error: (err) => {
        console.error('AdvantageVoice Load Error:', err);
        this.toastr.error('Failed to load pending orders', 'Error');
         this.cdr.detectChanges(); 
      }
    });
  }

  onFileChange(event: any): void {
    const file = event.target.files[0];
    if (!file) return;

    this.isLoading = true;
    this.importData(file);
    
    // Reset input
    event.target.value = '';
  }

  importData(file: File): void {
    this.service.importExcel(file).subscribe({
      next: (success) => {
        if (success) {
          this.toastr.success('File imported and pre-processed successfully', 'Success');
          this.loadPendingOrders();
        } else {
          this.toastr.error('Import failed to save records', 'Error');
           this.cdr.detectChanges(); 
        }
         this.cdr.detectChanges(); 
      },
      error: () => {
        this.toastr.error('Backend import error', 'Error');
         this.cdr.detectChanges(); 
      }
    });
  }

  validateBatch(): void {
    if (this.pendingOrders.length === 0) {
      this.toastr.warning('No orders to validate', 'Warning');
      return;
    }

    this.service.validateData().subscribe({
      next: (data) => {
        this.pendingOrders = data;
        this.updateStats();
        const failedCount = data.filter(x => !x.validated).length;
        if (failedCount > 0) {
          this.toastr.warning(`${failedCount} orders failed validation. Please check the results.`, 'Validation Complete');
        } else {
          this.toastr.success('All orders passed validation logic!', 'Success');
        }
        error: () => {
          this.toastr.error('Validation process failed', 'Error');
          this.cdr.detectChanges(); 
        }
      }, 
    });
  }

  submitOrders(): void {
    const unvalidated = this.pendingOrders.filter(x => !x.validated).length;
    if (unvalidated > 0) {
      Swal.fire('Validation Error', `Cannot submit. There are ${unvalidated} invalid orders in the batch.`, 'error');
      return;
    }

    Swal.fire({
      title: 'Submit Orders?',
      text: `Are you sure you want to submit ${this.pendingOrders.length} orders to the import queue?`,
      icon: 'question',
      showCancelButton: true,
      confirmButtonColor: '#10b981',
      confirmButtonText: 'Yes, Submit Batch',
      cancelButtonText: 'Cancel'
    }).then((result) => {
      if (result.isConfirmed) {
        this.isLoading = true;
        this.service.submitOrders().subscribe({
          next: (success) => {
            if (success) {
              Swal.fire('Submitted', 'Orders have been successfully moved to the import queue.', 'success');
              this.loadPendingOrders();
            }
          },
          error: (err) => {
            this.toastr.error('Submission failed: ' + (err.error || 'Server error'), 'Error');
            this.isLoading = false;
          }
        });
      }
    });
  }

  downloadTemplate(): void {
    const headers = [[
      'Order date',
      'V21 BAN',
      'Order Type-Hardware-Exchange-Accessory',
      'COMPANY NAME',
      'SHIPPING CONTACT',// NEW
      'CONTACT NUMBER',
      'Customer CONTACT EMAIL ADDRESS',
      'Rogers Delivery Specialist Email Address',
      'G Order Number',
      'First Name and Last Name',
      'Temporary Number',
      'Hardware Type',
      'Hardware SKU',
      'Delivery Unit and Street Address',
      'City',
      'Province',
      'Postal Code',
      'MAC ADDRESS',
      'Purolator Number',
      'RETURN PRODUCT Purolator Number',
      'DCI INVOICE',
      'Status',
      'COMPLETED DATE',
      'NOTE'
    ]];

    const ws = XLSX.utils.aoa_to_sheet(headers);

    // Apply column widths
    ws['!cols'] = headers[0].map(() => ({ wch: 25 }));

    // Apply styling to headers (Yellow background, Bold)
    const range = XLSX.utils.decode_range(ws['!ref']!);
    for (let col = range.s.c; col <= range.e.c; col++) {
      const cellAddress = XLSX.utils.encode_cell({ r: 0, c: col });
      if (!ws[cellAddress]) continue;
      ws[cellAddress].s = {
        font: { bold: true, sz: 11 },
        alignment: { horizontal: "center", vertical: "center" },
        fill: { fgColor: { rgb: "FFFF00" } }, // Yellow background
        border: {
          top: { style: "thin" },
          bottom: { style: "thin" },
          left: { style: "thin" },
          right: { style: "thin" }
        }
      };
    }

    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Template');

    const buffer = XLSX.write(wb, { bookType: 'xlsx', type: 'array' });
    saveAs(new Blob([buffer]), 'ADVImport.xlsx');

    this.toastr.success('Template generated and download started', 'Success');
  }

  updateStats(): void {
    this.totalOrders = this.pendingOrders.length;
    this.validatedOrders = this.pendingOrders.filter(x => x.validated).length;
    this.failedOrders = this.pendingOrders.filter(x => x.id && !x.validated).length;
  }

  getStatusClass(item: AdvantageImportVM): string {
    if (item.validated) return 'status-valid';
    if (item.reason) return 'status-invalid';
    return 'status-pending';
  }
}



