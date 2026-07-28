import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InventoryEditService } from '../../inventory-edit-service';
import { ToastrService } from 'ngx-toastr';
import { SpinnerService } from '../../../shared/spinner/spinner-service';
import { CookieService } from 'ngx-cookie-service';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-bulk-id-edit',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './bulk-id-edit.html',
  styleUrls: ['./bulk-id-edit.css']
})
export class BulkIdEdit implements OnInit {
  // Column 1 Data
  bulkSearchData = {
    oldBulkId: '',
    newBulkId: '',
    count: null as number | null
  };

  // Column 2 Data
  singleInvoiceData = {
    invoiceNo: '',
    currentBulkId: '',
    newBulkId: '',
    found: false
  };

  // Column 3 Data
  multiInvoiceData = {
    invoiceNos: [] as string[],
    newBulkId: ''
  };
  multiInvoiceInput: string = '';
  isPasteModalOpen: boolean = false;
  previewInvoices: string[] = []; // For Excel-style preview

  currentUser: string = 'SystemUser';

  constructor(
    private inventoryEditService: InventoryEditService,
    private toastr: ToastrService,
    private spinner: SpinnerService,
    private cookies: CookieService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.currentUser = this.cookies.get('UserId') || 'SystemUser';
  }

  // --- Column 1 Methods ---
  findBulkIdCount() {
    if (!this.bulkSearchData.oldBulkId) {
      this.toastr.warning('Please enter a Bulk Invoice No');
      return;
    }
    this.spinner.show();
    this.inventoryEditService.getBulkIdCount(this.bulkSearchData.oldBulkId).subscribe({
      next: (res) => {
        this.spinner.hide();
        this.bulkSearchData.count = res.count;
        this.toastr.info(`${res.count} invoices found`);
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.spinner.hide();
        console.error('Find error:', err);
        this.toastr.error(err.error?.message || 'Error finding invoices');
        this.cdr.detectChanges();
      }
    });
  }

  updateBulkId() {
    // Validate New Bulk ID
    if (!this.bulkSearchData.newBulkId) {
      this.toastr.warning('Please enter a New Bulk Invoice ID');
      return;
    }
    
    if (this.bulkSearchData.newBulkId.length > 20) {
      this.toastr.error('New Bulk ID is too long. Maximum 20 characters allowed.');
      return;
    }

    Swal.fire({
      title: 'Are you sure?',
      text: `Update all ${this.bulkSearchData.count} invoices to ID: ${this.bulkSearchData.newBulkId}?`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Yes, update all!',
      html: `
        <p>Update all <strong>${this.bulkSearchData.count}</strong> invoices to ID: <strong>${this.bulkSearchData.newBulkId}</strong>?</p>
        <p style="color: #dc3545; font-size: 14px;">⚠️ This operation may take up to 10 minutes for large datasets.</p>
      `
    }).then((result) => {
      if (result.isConfirmed) {
        const payload = {
          oldBulkId: this.bulkSearchData.oldBulkId,
          newBulkId: this.bulkSearchData.newBulkId,
          modifiedBy: this.currentUser
        };
        this.spinner.show();
        
        // Show progress message for large updates
        if (this.bulkSearchData.count && this.bulkSearchData.count > 100) {
          this.toastr.info('Processing large update... Please wait up to 10 minutes.', 'Processing', {
            timeOut: 0,
            extendedTimeOut: 0
          });
        }
        
        this.inventoryEditService.updateBulkId(payload).subscribe({
          next: () => {
            this.spinner.hide();
            Swal.fire('Success', `${this.bulkSearchData.count} invoices updated successfully`, 'success');
            // Clear all Column 1 fields
            this.clearColumn1();
            this.cdr.detectChanges();
          },
          error: (err) => {
            this.spinner.hide();
            console.error('Update error:', err);
            const errorMsg = err.error?.message || 'Update failed';
            Swal.fire('Error', errorMsg, 'error');
          }
        });
      }
    });
  }

  // --- Column 2 Methods ---
  findSingleInvoiceBulkId() {
    if (!this.singleInvoiceData.invoiceNo) {
      this.toastr.warning('Please enter an invoice number');
      return;
    }
    
    this.spinner.show();
    this.inventoryEditService.getSingleInvoiceBulkId(this.singleInvoiceData.invoiceNo).subscribe({
      next: (res) => {
        this.spinner.hide();
        this.singleInvoiceData.currentBulkId = res.fob || '';
        this.singleInvoiceData.newBulkId = res.fob || '';
        this.singleInvoiceData.found = true;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.spinner.hide();
        console.error('Find single invoice error:', err);
        this.toastr.error(err.error?.message || 'Invoice not found');
        this.singleInvoiceData.found = false;
        this.singleInvoiceData.currentBulkId = '';
        this.singleInvoiceData.newBulkId = '';
        this.cdr.detectChanges();
      }
    });
  }

  updateSingleInvoiceBulkId() {
    // Validate New Bulk ID
    if (!this.singleInvoiceData.newBulkId) {
      this.toastr.warning('Please enter a New Bulk ID');
      return;
    }
    
    if (this.singleInvoiceData.newBulkId.length > 20) {
      this.toastr.error('New Bulk ID is too long. Maximum 20 characters allowed.');
      return;
    }

    const payload = {
      invoiceNo: this.singleInvoiceData.invoiceNo,
      newBulkId: this.singleInvoiceData.newBulkId,
      modifiedBy: this.currentUser
    };
    this.spinner.show();
    this.inventoryEditService.updateSingleInvoiceBulkId(payload).subscribe({
      next: () => {
        this.spinner.hide();
        Swal.fire('Success', 'Invoice Updated Successfully', 'success');
        // Clear all Column 2 fields
        this.clearColumn2();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.spinner.hide();
        console.error('Update single invoice error:', err);
        const errorMsg = err.error?.message || 'Update failed';
        Swal.fire('Error', errorMsg, 'error');
      }
    });
  }

  // --- Column 3 Methods ---
  addInvoiceToList() {
    if (this.multiInvoiceInput) {
      // Split by newline, comma, or space and filter out empty strings
      const invoices = this.multiInvoiceInput
        .split(/[\n,\s]+/)
        .map(i => i.trim())
        .filter(i => i.length > 0);
      
      // Remove duplicates
      const uniqueInvoices = invoices.filter(inv => !this.multiInvoiceData.invoiceNos.includes(inv));
      
      this.multiInvoiceData.invoiceNos = [...this.multiInvoiceData.invoiceNos, ...uniqueInvoices];
      this.multiInvoiceInput = '';
      this.isPasteModalOpen = false; // Close modal after adding
      this.cdr.detectChanges();
      this.toastr.success(`${uniqueInvoices.length} invoices added to list (Total: ${this.multiInvoiceData.invoiceNos.length})`);
    }
  }

  togglePasteModal(open: boolean) {
    this.isPasteModalOpen = open;
    if (open) {
      // Focus on textarea when modal opens
      setTimeout(() => {
        const textarea = document.querySelector('.excel-textarea') as HTMLTextAreaElement;
        if (textarea) textarea.focus();
      }, 100);
    } else {
      // Clear preview when closing
      this.previewInvoices = [];
    }
    this.cdr.detectChanges();
  }

  onTextareaInput() {
    // Real-time preview of parsed invoices (Excel-style)
    if (this.multiInvoiceInput.trim()) {
      this.previewInvoices = this.multiInvoiceInput
        .split(/[\n,\s]+/)
        .map(i => i.trim())
        .filter(i => i.length > 0);
    } else {
      this.previewInvoices = [];
    }
  }

  clearTextarea() {
    this.multiInvoiceInput = '';
    this.previewInvoices = [];
    this.cdr.detectChanges();
  }

  clearInvoiceList() {
    this.multiInvoiceData.invoiceNos = [];
    this.multiInvoiceInput = '';
    this.cdr.detectChanges();
    this.toastr.info('Invoice list cleared');
  }

  removeInvoiceFromList(index: number) {
    this.multiInvoiceData.invoiceNos.splice(index, 1);
    this.cdr.detectChanges();
  }

  // Clear methods for each column
  clearColumn1() {
    this.bulkSearchData = {
      oldBulkId: '',
      newBulkId: '',
      count: null
    };
  }

  clearColumn2() {
    this.singleInvoiceData = {
      invoiceNo: '',
      currentBulkId: '',
      newBulkId: '',
      found: false
    };
  }

  clearColumn3() {
    this.multiInvoiceData = {
      invoiceNos: [],
      newBulkId: ''
    };
    this.multiInvoiceInput = '';
    this.previewInvoices = [];
    this.isPasteModalOpen = false;
  }

  // Clear all columns at once
  clearAllColumns() {
    this.clearColumn1();
    this.clearColumn2();
    this.clearColumn3();
    this.toastr.info('All fields cleared');
    this.cdr.detectChanges();
  }

  updateMultipleBulkIds() {
    // Validate inputs
    if (this.multiInvoiceData.invoiceNos.length === 0) {
      this.toastr.warning('Please add invoices to the list first');
      return;
    }
    
    if (!this.multiInvoiceData.newBulkId) {
      this.toastr.warning('Please enter a New Bulk ID');
      return;
    }
    
    if (this.multiInvoiceData.newBulkId.length > 20) {
      this.toastr.error('New Bulk ID is too long. Maximum 20 characters allowed.');
      return;
    }

    Swal.fire({
      title: 'Update Multiple Invoices?',
      html: `
        <p>Update <strong>${this.multiInvoiceData.invoiceNos.length}</strong> invoices to Bulk ID: <strong>${this.multiInvoiceData.newBulkId}</strong>?</p>
        <p style="color: #dc3545; font-size: 14px;">⚠️ This operation may take several minutes for large lists.</p>
      `,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Yes, update all!'
    }).then((result) => {
      if (result.isConfirmed) {
        const payload = {
          invoiceNos: this.multiInvoiceData.invoiceNos,
          newBulkId: this.multiInvoiceData.newBulkId,
          modifiedBy: this.currentUser
        };
        this.spinner.show();
        
        // Show progress message for large lists
        if (this.multiInvoiceData.invoiceNos.length > 20) {
          this.toastr.info('Processing large batch... Please wait.', 'Processing', {
            timeOut: 0,
            extendedTimeOut: 0
          });
        }
        
        this.inventoryEditService.updateMultipleBulkIds(payload).subscribe({
          next: () => {
            this.spinner.hide();
            Swal.fire('Success', `${this.multiInvoiceData.invoiceNos.length} invoices updated successfully`, 'success');
            // Clear all Column 3 fields
            this.clearColumn3();
            this.cdr.detectChanges();
          },
          error: (err) => {
            this.spinner.hide();
            console.error('Update multiple invoices error:', err);
            const errorMsg = err.error?.message || 'Update failed';
            Swal.fire('Error', errorMsg, 'error');
          }
        });
      }
    });
  }
}
