import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { 
  ArCollectionService, 
  TerritoryGroup, 
  ARCustomerRow, 
  ARTransactionRow, 
  ARCommentEvent, 
  UpdateARDetailRequest, 
  AddCommentRequest, 
  CreateNoticeRequest, 
  ExportInvoiceRequest 
} from './ar-collection.service';
import { SpinnerService } from '../../shared/spinner/spinner-service';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-ar-collection',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './ar-collection-component.html',
  styleUrls: ['./ar-collection-component.css']
})
export class ArCollectionComponent implements OnInit {
  // Filter bindings
  territoryGroups: TerritoryGroup[] = [];
  selectedGroup: TerritoryGroup | null = null;
  selectBy: number = 1; // 1 = Single, 2 = Group
  agingDate: string = '';
  
  // Lists
  customers: ARCustomerRow[] = [];
  selectedCustomer: ARCustomerRow | null = null;
  selectedCustomerCode: string = '';
  
  transactions: ARTransactionRow[] = [];
  selectedTransaction: ARTransactionRow | null = null;
  
  events: ARCommentEvent[] = [];
  
  // Selected transaction fields for binding
  banInput: string = '';
  rootCauseIdInput: number = 0;
  opcResolvedInput: boolean = false;
  opcDescriptionInput: string = '';
  ignoreGroupInput: boolean = false;
  billToCustInput: string = '';

  // Comments
  commentTextInput: string = '';
  isAddingComment: boolean = false;
  editingCommentId: number | null = null;
  editingCommentText: string = '';

  // Root cause list matching tblRootCauses
  rootCauses = [
    { code: 1, name: 'Slow Paying Customer' },
    { code: 2, name: 'Customer / Rep. does not respond/ refuses to pay' },
    { code: 3, name: 'Customer Requesting revision of Invoice (add P.O., etc)' },
    { code: 4, name: 'Customer pays Rogers Directly' },
    { code: 5, name: 'Bankruptcy Protection' },
    { code: 6, name: 'Write-off underway' },
    { code: 7, name: 'Settled/Paid' },
    { code: 8, name: 'Customer Dispute- MSF pricing' },
    { code: 9, name: 'Customer Dispute- H/W pricing (Wrong cost)' },
    { code: 10, name: 'Customer Dispute- H/W already changed in V21' },
    { code: 11, name: 'Shipping Dispute' },
    { code: 12, name: 'Bankrupt Customer' },
    { code: 13, name: 'Escalation to Rogers 90 & 120 Days Accounts' }
  ];

  constructor(
    private arService: ArCollectionService,
    private spinner: SpinnerService,
    private toastr: ToastrService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    // Default aging date to today
    const today = new Date();
    this.agingDate = today.toISOString().split('T')[0];
    this.loadTerritoryGroups();
  }

  loadTerritoryGroups(): void {
    this.arService.getTerritoryGroups().subscribe({
      next: (groups) => {
        this.territoryGroups = groups;
        if (groups.length > 0) {
          // Default to first group (ENT Corporate)
          this.selectedGroup = groups[0];
          this.loadCustomers();
        }
      },
      error: () => this.toastr.error('Failed to load territory groups')
    });
  }

  onFiltersChange(): void {
    this.selectedCustomer = null;
    this.selectedCustomerCode = '';
    this.transactions = [];
    this.events = [];
    this.selectedTransaction = null;
    this.loadCustomers();
  }

  loadCustomers(): void {
    if (!this.selectedGroup || !this.agingDate) return;

    this.spinner.show();
    const criteria = this.selectedGroup.groupCriteria || '';
    this.arService.loadOpenCustomers(this.selectBy, criteria, this.agingDate).subscribe({
      next: (data) => {
        this.customers = data;
        this.spinner.hide();
        this.cdr.detectChanges();
      },
      error: () => {
        this.spinner.hide();
        this.toastr.error('Failed to load open customers');
      }
    });
  }

  onCustomerSelect(): void {
    this.selectedTransaction = null;
    this.transactions = [];
    this.events = [];
    
    if (this.selectBy === 1) {
      this.selectedCustomer = this.customers.find(c => c.cust === this.selectedCustomerCode) || null;
    } else {
      this.selectedCustomer = this.customers.find(c => c.custGroup === this.selectedCustomerCode) || null;
    }

    if (this.selectedCustomer) {
      const code = this.selectBy === 1 ? this.selectedCustomer.cust : (this.selectedCustomer.custGroup || '');
      this.arService.checkOpenPayments(code).subscribe({
        next: (hasOpenPayments) => {
          if (hasOpenPayments) {
            Swal.fire('Open Payments Found', 'Please note there are open payments/credits on this customer\'s account.', 'warning');
          }
        }
      });
      this.refreshARGrid();
      this.loadEvents();
    }
  }

  refreshARGrid(): void {
    if (!this.selectedCustomer || !this.selectedGroup) return;

    this.spinner.show();
    const code = this.selectBy === 1 ? this.selectedCustomer.cust : (this.selectedCustomer.custGroup || '');
    const criteria = this.selectedGroup.groupCriteria || '';

    this.arService.refreshARGrid(code, this.selectBy, criteria, this.agingDate).subscribe({
      next: (data) => {
        this.transactions = data;
        this.spinner.hide();
        this.cdr.detectChanges();
      },
      error: () => {
        this.spinner.hide();
        this.toastr.error('Failed to load transactions grid');
      }
    });
  }

  loadEvents(): void {
    if (!this.selectedCustomer) return;
    const code = this.selectBy === 1 ? this.selectedCustomer.cust : (this.selectedCustomer.custGroup || '');

    this.arService.getEvents(code, this.selectBy).subscribe({
      next: (data) => {
        this.events = data;
        this.cdr.detectChanges();
      },
      error: () => this.toastr.error('Failed to load events log')
    });
  }

  selectTransactionRow(row: ARTransactionRow): void {
    this.selectedTransaction = row;
    
    // Bind detail form fields
    this.banInput = row.ban || '';
    this.rootCauseIdInput = row.rootCauseID || 0;
    this.opcResolvedInput = row.opcResolved;
    this.opcDescriptionInput = row.opcDescription || '';
    this.ignoreGroupInput = row.ignoreGroup;
    this.billToCustInput = row.billToCust || '';
  }

  saveARDetail(): void {
    if (!this.selectedTransaction) return;

    // VBA Validation: If OPCResolved is checked, OPCDescription is required
    if (this.opcResolvedInput && !this.opcDescriptionInput.trim()) {
      this.toastr.warning('You must enter an OPC Description if OPC Resolved is checked.');
      return;
    }

    this.spinner.show();
    const request: UpdateARDetailRequest = {
      transNo: this.selectedTransaction.tranS_NO,
      ban: this.banInput,
      rootCauseID: this.rootCauseIdInput > 0 ? this.rootCauseIdInput : undefined,
      opcResolved: this.opcResolvedInput,
      opcDescription: this.opcDescriptionInput,
      ignoreGroup: this.ignoreGroupInput,
      billToCust: this.billToCustInput
    };

    this.arService.updateARDetailRow(request).subscribe({
      next: (success) => {
        if (success) {
          this.toastr.success('Transaction details saved successfully.');
          this.refreshARGrid();
        } else {
          this.toastr.error('Failed to save transaction details.');
          this.spinner.hide();
        }
      },
      error: () => {
        this.spinner.hide();
        this.toastr.error('Error saving transaction details.');
      }
    });
  }

  // --- Grid Sum calculations ---
  get sumOutstanding(): number {
    return this.transactions.reduce((sum, row) => sum + row.balance, 0);
  }
  get sumCurrent(): number {
    return this.transactions.reduce((sum, row) => sum + row.current, 0);
  }
  get sum30Days(): number {
    return this.transactions.reduce((sum, row) => sum + row.thirtyDays, 0);
  }
  get sum60Days(): number {
    return this.transactions.reduce((sum, row) => sum + row.sixtyDays, 0);
  }
  get sum90Days(): number {
    return this.transactions.reduce((sum, row) => sum + row.ninetyDays, 0);
  }
  get sum120Days(): number {
    return this.transactions.reduce((sum, row) => sum + row.oneTwentyPlusDays, 0);
  }

  // --- Comments / Events Operations ---
  get checkedTransNos(): string[] {
    return this.transactions.filter(t => t.checked).map(t => t.tranS_NO);
  }

  toggleAddComment(): void {
    if (!this.selectedCustomer) {
      this.toastr.warning('Please select a customer first');
      return;
    }
    this.isAddingComment = !this.isAddingComment;
    this.commentTextInput = '';
  }

  submitComment(): void {
    if (!this.commentTextInput.trim() || !this.selectedCustomer) return;

    this.spinner.show();
    const custCode = this.selectBy === 1 ? this.selectedCustomer.cust : (this.selectedCustomer.custGroup || '');
    
    const request: AddCommentRequest = {
      custNo: custCode,
      custType: this.selectBy === 1 ? 'Single' : 'Group',
      commentText: this.commentTextInput,
      checkedTransNos: this.checkedTransNos
    };

    this.arService.addComment(request).subscribe({
      next: () => {
        this.toastr.success('Comment added successfully');
        this.isAddingComment = false;
        this.commentTextInput = '';
        this.loadEvents();
        this.refreshARGrid();
      },
      error: () => {
        this.spinner.hide();
        this.toastr.error('Failed to add comment');
      }
    });
  }

  deleteComment(commentId: number): void {
    Swal.fire({
      title: 'Delete Comment?',
      text: 'Are you sure you want to delete this comment?',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Yes',
      cancelButtonText: 'No'
    }).then((result) => {
      if (result.isConfirmed) {
        this.spinner.show();
        this.arService.deleteComment(commentId).subscribe({
          next: () => {
            this.toastr.success('Comment deleted successfully');
            this.loadEvents();
          },
          error: () => {
            this.spinner.hide();
            this.toastr.error('Failed to delete comment');
          }
        });
      }
    });
  }

  startEditComment(ev: ARCommentEvent): void {
    this.editingCommentId = ev.id;
    this.editingCommentText = ev.eventText || '';
  }

  cancelEditComment(): void {
    this.editingCommentId = null;
  }

  saveCommentEdit(): void {
    if (!this.editingCommentId || !this.editingCommentText.trim()) return;

    this.spinner.show();
    this.arService.editComment(this.editingCommentId, this.editingCommentText).subscribe({
      next: () => {
        this.toastr.success('Comment updated successfully');
        this.editingCommentId = null;
        this.loadEvents();
      },
      error: () => {
        this.spinner.hide();
        this.toastr.error('Failed to update comment');
      }
    });
  }

  removeCommentFromTrans(eventTransId: number): void {
    Swal.fire({
      title: 'Remove Transaction Link?',
      text: 'Are you sure you want to unlink this comment from this transaction?',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Yes',
      cancelButtonText: 'No'
    }).then((result) => {
      if (result.isConfirmed) {
        this.spinner.show();
        this.arService.removeCommentFromTrans(eventTransId).subscribe({
          next: () => {
            this.toastr.success('Link removed successfully');
            this.loadEvents();
          },
          error: () => {
            this.spinner.hide();
            this.toastr.error('Failed to remove transaction link');
          }
        });
      }
    });
  }

  // --- Button Actions ---
  toggleSelectAll(): void {
    const allChecked = this.transactions.every(t => t.checked);
    this.transactions.forEach(t => t.checked = !allChecked);
  }

  outputNotice(noticeType: number): void {
    if (!this.selectedCustomer) {
      this.toastr.warning('Please select a customer first.');
      return;
    }

    const checkedRows = this.transactions.filter(t => t.checked);
    if (checkedRows.length === 0) {
      this.toastr.warning('You must select at least one transaction to print notice.');
      return;
    }

    this.spinner.show();
    const custCode = this.selectBy === 1 ? this.selectedCustomer.cust : (this.selectedCustomer.custGroup || '');
    const custName = this.selectedCustomer.custName || '';
    const lang = this.selectedCustomer.language || 'English';

    // In VBA, notice amount is calculated as sum of selected transaction balances
    const noticeAmount = checkedRows.reduce((sum, r) => sum + r.balance, 0);

    const request: CreateNoticeRequest = {
      noticeType,
      custNo: custCode,
      custName,
      language: lang,
      amount: noticeAmount,
      checkedTransNos: this.checkedTransNos
    };

    this.arService.generateOverdueNotice(request).subscribe({
      next: (blob) => {
        this.spinner.hide();
        const docName = lang === 'French'
          ? (noticeType === 1 ? '1er_Avis.docx' : '2ieme_Avis.docx')
          : (noticeType === 1 ? '1st_Notice.docx' : '2nd_Notice.docx');
        this.downloadBlob(blob, `${custCode}_${docName}`);
        this.toastr.success('Notice generated successfully');
        this.loadEvents();
        this.refreshARGrid();
      },
      error: () => {
        this.spinner.hide();
        this.toastr.error('Failed to generate notice document.');
      }
    });
  }

  exportInvoice(): void {
    if (!this.selectedTransaction) {
      this.toastr.warning('Please select a transaction row in the grid first.');
      return;
    }

    this.spinner.show();
    const request: ExportInvoiceRequest = {
      invoiceRef: this.selectedTransaction.reF_NO || this.selectedTransaction.tranS_NO,
      invoiceType: 'Normal',
      custNo: this.selectedTransaction.cust,
      custName: this.selectedCustomer?.custName || ''
    };

    this.arService.outputInvoicePdf(request).subscribe({
      next: (blob) => {
        this.spinner.hide();
        this.downloadBlob(blob, `Invoice-${request.invoiceRef}.pdf`);
        this.toastr.success('Invoice PDF exported successfully.');
      },
      error: () => {
        this.spinner.hide();
        this.toastr.error('Failed to export invoice PDF.');
      }
    });
  }

  outputPaymentAdvice(): void {
    if (!this.selectedTransaction) {
      this.toastr.warning('Please select a transaction row in the grid first.');
      return;
    }

    if (this.selectedTransaction.type !== 'P' && this.selectedTransaction.type !== 'C') {
      this.toastr.warning('Selected transaction is not a payment or credit.');
      return;
    }

    this.spinner.show();
    this.arService.outputPaymentAdvicePdf(this.selectedTransaction.tranS_NO).subscribe({
      next: (blob) => {
        this.spinner.hide();
        this.downloadBlob(blob, `PaymentAdvice-${this.selectedTransaction!.tranS_NO}.pdf`);
        this.toastr.success('Payment advice PDF exported successfully.');
      },
      error: () => {
        this.spinner.hide();
        this.toastr.error('Failed to export payment advice PDF.');
      }
    });
  }

  exploreCustomerFolder(): void {
    if (!this.selectedCustomer) return;
    // Mock exploring folder - in C# it outputs zip, we can simulate or trigger download
    this.toastr.info('Customer Folder zip generation requested.');
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
