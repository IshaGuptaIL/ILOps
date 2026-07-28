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
} from '../ar-collection.service';
import { SpinnerService } from '../../../shared/spinner/spinner-service';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-ar-collection-review',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './ar-collection-review.html',
  styleUrls: ['./ar-collection-review.css']
})
export class ArCollectionReviewComponent implements OnInit {
  // Filter bindings
  territoryGroups: TerritoryGroup[] = [];
  selectedGroup: TerritoryGroup | null = null;
  selectBy: number = 1; // 1 = Single, 2 = Group
  agingDate: string = '';
  
  // Lists
  customers: ARCustomerRow[] = [];
  selectedCustomer: ARCustomerRow | null = null;
  selectedCustomerCode: string = '';
  
  // Local bindings matching Access form controls
  txtCustNo: string = '';
  txtContact: string = '';
  txtPhone: string = '';
  txtEmail: string = '';
  txtLanguage: string = '';
  chkSendBulk: boolean = false;
  cmbAgeing: string = 'All';
  allTransactions: ARTransactionRow[] = [];
  
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
    this.cdr.detectChanges();
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
        this.cdr.detectChanges();
      },
      error: () => {
        this.toastr.error('Failed to load territory groups');
        this.cdr.detectChanges();
      }
    });
    this.cdr.detectChanges();
  }

  onFiltersChange(): void {
    this.selectedCustomer = null;
    this.selectedCustomerCode = '';
    this.transactions = [];
    this.events = [];
    this.selectedTransaction = null;
    this.loadCustomers();
    this.cdr.detectChanges();
  }

  loadCustomers(): void {
    if (!this.selectedGroup || !this.agingDate) {
      this.cdr.detectChanges();
      return;
    }

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
        this.cdr.detectChanges();
      }
    });
    this.cdr.detectChanges();
  }

  onCustomerSelect(): void {
    this.selectedTransaction = null;
    this.transactions = [];
    this.allTransactions = [];
    this.events = [];
    
    if (this.selectBy === 1) {
      this.selectedCustomer = this.customers.find(c => c.cust === this.selectedCustomerCode) || null;
    } else {
      this.selectedCustomer = this.customers.find(c => c.custGroup === this.selectedCustomerCode) || null;
    }

    if (this.selectedCustomer) {
      this.txtCustNo = this.selectBy === 1 ? this.selectedCustomer.cust : (this.selectedCustomer.custGroup || '');
      
      // Handle both camelCase and System.Text.Json default capitalizations
      const c = this.selectedCustomer as any;
      this.txtContact = this.selectedCustomer.bvcocontact1name || c.bVCOCONTACT1NAME || c.BVCOCONTACT1NAME || '';
      this.txtPhone = this.selectedCustomer.bvaddrtelno1 || c.bVADDRTELNO1 || c.BVADDRTELNO1 || '';
      this.txtEmail = this.selectedCustomer.bvaddremail || c.bVADDREMAIL || c.BVADDREMAIL || this.selectedCustomer.bvcocontact1email || c.bVCOCONTACT1EMAIL || c.BVCOCONTACT1EMAIL || '';
      
      this.txtLanguage = this.selectedCustomer.language || '';
      this.chkSendBulk = this.selectedCustomer.sendBulk || false;

      const code = this.selectBy === 1 ? this.selectedCustomer.cust : (this.selectedCustomer.custGroup || '');
      this.arService.checkOpenPayments(code).subscribe({
        next: (hasOpenPayments) => {
          if (hasOpenPayments) {
            Swal.fire('Open Payments Found', 'Please note there are open payments/credits on this customer\'s account.', 'warning');
          }
          this.cdr.detectChanges();
        },
        error: () => {
          this.cdr.detectChanges();
        }
      });
      this.refreshARGrid();
      this.loadEvents();
    } else {
      this.txtCustNo = '';
      this.txtContact = '';
      this.txtPhone = '';
      this.txtEmail = '';
      this.txtLanguage = '';
      this.chkSendBulk = false;
    }
    this.cdr.detectChanges();
  }

  refreshARGrid(): void {
    if (!this.selectedCustomer || !this.selectedGroup) {
      this.cdr.detectChanges();
      return;
    }

    this.spinner.show();
    const code = this.selectBy === 1 ? this.selectedCustomer.cust : (this.selectedCustomer.custGroup || '');
    const criteria = this.selectedGroup.groupCriteria || '';

    this.arService.refreshARGrid(code, this.selectBy, criteria, this.agingDate).subscribe({
      next: (data) => {
        this.allTransactions = data;
        this.applyAgeingFilter();
        this.spinner.hide();
        this.cdr.detectChanges();
      },
      error: () => {
        this.spinner.hide();
        this.toastr.error('Failed to load transactions grid');
        this.cdr.detectChanges();
      }
    });
    this.cdr.detectChanges();
  }

  applyAgeingFilter(): void {
    if (this.cmbAgeing === 'All') {
      this.transactions = [...this.allTransactions];
    } else {
      const days = parseInt(this.cmbAgeing, 10);
      this.transactions = this.allTransactions.filter(t => t.daysOld !== undefined && t.daysOld > days);
    }
    this.cdr.detectChanges();
  }

  onAgeingFilterChange(): void {
    this.applyAgeingFilter();
  }

  clearFilters(): void {
    this.cmbAgeing = 'All';
    this.applyAgeingFilter();
  }

  loadEvents(): void {
    if (!this.selectedCustomer) {
      this.cdr.detectChanges();
      return;
    }
    const code = this.selectBy === 1 ? this.selectedCustomer.cust : (this.selectedCustomer.custGroup || '');

    this.arService.getEvents(code, this.selectBy).subscribe({
      next: (data) => {
        this.events = data;
        this.cdr.detectChanges();
      },
      error: () => {
        this.toastr.error('Failed to load events log');
        this.cdr.detectChanges();
      }
    });
    this.cdr.detectChanges();
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
    this.cdr.detectChanges();
  }

  saveARDetail(): void {
    if (!this.selectedTransaction) {
      this.cdr.detectChanges();
      return;
    }

    // VBA Validation: If OPCResolved is checked, OPCDescription is required
    if (this.opcResolvedInput && !this.opcDescriptionInput.trim()) {
      this.toastr.warning('You must enter an OPC Description if OPC Resolved is checked.');
      this.cdr.detectChanges();
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
        this.cdr.detectChanges();
      },
      error: () => {
        this.spinner.hide();
        this.toastr.error('Error saving transaction details.');
        this.cdr.detectChanges();
      }
    });
    this.cdr.detectChanges();
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
      this.cdr.detectChanges();
      return;
    }
    this.isAddingComment = !this.isAddingComment;
    this.commentTextInput = '';
    this.cdr.detectChanges();
  }

  submitComment(eventType?: number): void {
    if (!this.commentTextInput.trim() || !this.selectedCustomer) {
      this.cdr.detectChanges();
      return;
    }

    this.spinner.show();
    const custCode = this.selectBy === 1 ? this.selectedCustomer.cust : (this.selectedCustomer.custGroup || '');
    
    // In VBA: Type 1 if any trans checked, Type 9 (BareComment) if none checked
    let resolvedEventType = eventType;
    if (!resolvedEventType) {
      resolvedEventType = this.checkedTransNos.length > 0 ? 1 : 9;
    }

    const request: AddCommentRequest = {
      custNo: custCode,
      custType: this.selectBy === 1 ? 'Single' : 'Group',
      commentText: this.commentTextInput,
      checkedTransNos: this.checkedTransNos,
      eventType: resolvedEventType
    };

    this.arService.addComment(request).subscribe({
      next: () => {
        this.toastr.success('Comment added successfully');
        this.isAddingComment = false;
        this.commentTextInput = '';
        this.loadEvents();
        this.refreshARGrid();
        this.cdr.detectChanges();
      },
      error: () => {
        this.spinner.hide();
        this.toastr.error('Failed to add comment');
        this.cdr.detectChanges();
      }
    });
    this.cdr.detectChanges();
  }

  logCallOut(): void {
    this.promptEventLogger(5, 'Log Call Out');
  }

  logCallIn(): void {
    this.promptEventLogger(6, 'Log Call In');
  }

  logEmail(): void {
    this.promptEventLogger(7, 'Log Email');
  }

  logFax(): void {
    this.promptEventLogger(8, 'Log Fax');
  }

  private promptEventLogger(eventType: number, title: string): void {
    if (!this.selectedCustomer) {
      this.toastr.warning('Please select a customer first.');
      this.cdr.detectChanges();
      return;
    }

    const c = this.selectedCustomer as any;
    const contact = this.selectedCustomer.bvcocontact1name || c.bVCOCONTACT1NAME || c.BVCOCONTACT1NAME || 'N/A';
    const phone = this.selectedCustomer.bvaddrtelno1 || c.bVADDRTELNO1 || c.BVADDRTELNO1 || 'N/A';
    const email = this.selectedCustomer.bvaddremail || c.bVADDREMAIL || c.BVADDREMAIL || 'N/A';
    
    // HTML representing the MS Access frmEventLogger form look
    const formHtml = `
      <div style="text-align: left; font-size: 13px; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;">
        <div style="margin-bottom: 15px; padding: 10px; background: #f0f0f0; border: 1px solid #ccc; border-radius: 4px;">
          <div style="margin-bottom: 5px;"><strong>Contact:</strong> ${contact}</div>
          <div style="margin-bottom: 5px;"><strong>Phone:</strong> ${phone}</div>
          <div><strong>Email:</strong> ${email}</div>
        </div>
        <div style="margin-bottom: 8px;">
          <label style="font-weight: bold; display: block; margin-bottom: 4px;">Event Type:</label>
          <select id="swal-ev-type" class="swal2-select" style="display: block; width: 100%; margin: 0; padding: 5px;">
            <option value="5" ${eventType === 5 ? 'selected' : ''}>Call Out</option>
            <option value="6" ${eventType === 6 ? 'selected' : ''}>Call In</option>
            <option value="7" ${eventType === 7 ? 'selected' : ''}>Email</option>
            <option value="8" ${eventType === 8 ? 'selected' : ''}>Fax</option>
          </select>
        </div>
        <div>
          <label style="font-weight: bold; display: block; margin-bottom: 4px;">Event Details:</label>
          <textarea id="swal-ev-text" class="swal2-textarea" style="display: block; width: 100%; margin: 0; padding: 5px; box-sizing: border-box; min-height: 100px;" placeholder="Type event details here..."></textarea>
        </div>
      </div>
    `;

    Swal.fire({
      title: title,
      html: formHtml,
      showCancelButton: true,
      confirmButtonText: 'Log Event',
      cancelButtonText: 'Cancel',
      confirmButtonColor: '#000080',
      width: '500px',
      preConfirm: () => {
        const selectedType = (document.getElementById('swal-ev-type') as HTMLSelectElement).value;
        const text = (document.getElementById('swal-ev-text') as HTMLTextAreaElement).value;
        if (!text.trim()) {
          Swal.showValidationMessage('Event details cannot be empty!');
          return false;
        }
        return { type: parseInt(selectedType, 10), text: text };
      }
    }).then((result) => {
      if (result.isConfirmed && result.value) {
        const custCode = this.selectBy === 1 ? this.selectedCustomer!.cust : (this.selectedCustomer!.custGroup || '');
        const request: AddCommentRequest = {
          custNo: custCode,
          custType: this.selectBy === 1 ? 'Single' : 'Group',
          commentText: result.value.text,
          checkedTransNos: this.checkedTransNos,
          eventType: result.value.type
        };
        this.spinner.show();
        this.arService.addComment(request).subscribe({
          next: () => {
            this.toastr.success('Event logged successfully');
            this.loadEvents();
            this.refreshARGrid();
            this.cdr.detectChanges();
          },
          error: () => {
            this.spinner.hide();
            this.toastr.error('Failed to log event');
            this.cdr.detectChanges();
          }
        });
      }
      this.cdr.detectChanges();
    });
    this.cdr.detectChanges();
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
             this.cdr.detectChanges();
           },
           error: () => {
             this.spinner.hide();
             this.toastr.error('Failed to delete comment');
             this.cdr.detectChanges();
           }
         });
      }
      this.cdr.detectChanges();
    });
    this.cdr.detectChanges();
  }

  startEditComment(ev: ARCommentEvent): void {
    this.editingCommentId = ev.id;
    this.editingCommentText = ev.eventText || '';
    this.cdr.detectChanges();
  }

  cancelEditComment(): void {
    this.editingCommentId = null;
    this.cdr.detectChanges();
  }

  saveCommentEdit(): void {
    if (!this.editingCommentId || !this.editingCommentText.trim()) {
      this.cdr.detectChanges();
      return;
    }

    this.spinner.show();
    this.arService.editComment(this.editingCommentId, this.editingCommentText).subscribe({
      next: () => {
        this.toastr.success('Comment updated successfully');
        this.editingCommentId = null;
        this.loadEvents();
        this.cdr.detectChanges();
      },
      error: () => {
        this.spinner.hide();
        this.toastr.error('Failed to update comment');
        this.cdr.detectChanges();
      }
    });
    this.cdr.detectChanges();
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
            this.cdr.detectChanges();
          },
          error: () => {
            this.spinner.hide();
            this.toastr.error('Failed to remove transaction link');
            this.cdr.detectChanges();
          }
        });
      }
      this.cdr.detectChanges();
    });
    this.cdr.detectChanges();
  }

  // --- Button Actions ---
  selectAll(): void {
    this.transactions.forEach(t => t.checked = true);
    this.cdr.detectChanges();
  }

  selectNone(): void {
    this.transactions.forEach(t => t.checked = false);
    this.cdr.detectChanges();
  }

  outputNotice(noticeType: number): void {
    if (!this.selectedCustomer) {
      this.toastr.warning('Please select a customer first.');
      this.cdr.detectChanges();
      return;
    }

    const checkedRows = this.transactions.filter(t => t.checked);
    if (checkedRows.length === 0) {
      this.toastr.warning('You must select at least one transaction to print notice.');
      this.cdr.detectChanges();
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
        this.cdr.detectChanges();
      },
      error: () => {
        this.spinner.hide();
        this.toastr.error('Failed to generate notice document.');
        this.cdr.detectChanges();
      }
    });
    this.cdr.detectChanges();
  }

  exportInvoice(): void {
    if (!this.selectedTransaction) {
      this.toastr.warning('Please select a transaction row in the grid first.');
      this.cdr.detectChanges();
      return;
    }

    this.spinner.show();
    
    // Check if bulk output is enabled and this transaction has a BulkID
    const isBulk = this.chkSendBulk && !!this.selectedTransaction.bulkID;
    
    const request: ExportInvoiceRequest = {
      invoiceRef: isBulk ? this.selectedTransaction.bulkID! : (this.selectedTransaction.reF_NO || this.selectedTransaction.tranS_NO),
      invoiceType: isBulk ? 'Bulk' : 'Normal',
      custNo: this.selectedTransaction.cust,
      custName: this.selectedCustomer?.custName || ''
    };

    this.arService.outputInvoicePdf(request).subscribe({
      next: (blob) => {
        this.spinner.hide();
        const ext = isBulk ? 'zip' : 'pdf';
        const prefix = isBulk ? 'BulkInvoice' : 'Invoice';
        this.downloadBlob(blob, `${prefix}-${request.invoiceRef}.${ext}`);
        this.toastr.success(`${isBulk ? 'Bulk invoices ZIP' : 'Invoice PDF'} exported successfully.`);
        this.cdr.detectChanges();
      },
      error: () => {
        this.spinner.hide();
        this.toastr.error(`Failed to export ${isBulk ? 'bulk invoices' : 'invoice PDF'}.`);
        this.cdr.detectChanges();
      }
    });
    this.cdr.detectChanges();
  }

  outputCheckedDocuments(): void {
    if (!this.selectedCustomer) {
      this.toastr.warning('Please select a customer first.');
      this.cdr.detectChanges();
      return;
    }

    const checkedRows = this.transactions.filter(t => t.checked);
    if (checkedRows.length === 0) {
      this.toastr.warning('You must select at least one transaction in the grid first.');
      this.cdr.detectChanges();
      return;
    }

    this.spinner.show();
    const custCode = this.selectBy === 1 ? this.selectedCustomer.cust : (this.selectedCustomer.custGroup || '');
    
    const request = {
      custNo: custCode,
      chkSendBulk: this.chkSendBulk,
      checkedTransNos: this.checkedTransNos
    };

    this.arService.outputCheckedDocuments(request).subscribe({
      next: (blob) => {
        this.spinner.hide();
        const dateStr = new Date().toISOString().split('T')[0];
        this.downloadBlob(blob, `Documents_${custCode}_${dateStr}.zip`);
        this.toastr.success('Checked documents ZIP exported successfully.');
        this.cdr.detectChanges();
      },
      error: () => {
        this.spinner.hide();
        this.toastr.error('Failed to export checked documents ZIP.');
        this.cdr.detectChanges();
      }
    });
    this.cdr.detectChanges();
  }

  outputPaymentAdvice(): void {
    if (!this.selectedTransaction) {
      this.toastr.warning('Please select a transaction row in the grid first.');
      this.cdr.detectChanges();
      return;
    }

    if (this.selectedTransaction.type !== 'P' && this.selectedTransaction.type !== 'C') {
      this.toastr.warning('Selected transaction is not a payment or credit.');
      this.cdr.detectChanges();
      return;
    }

    this.spinner.show();
    this.arService.outputPaymentAdvicePdf(this.selectedTransaction.tranS_NO).subscribe({
      next: (blob) => {
        this.spinner.hide();
        this.downloadBlob(blob, `PaymentAdvice-${this.selectedTransaction!.tranS_NO}.pdf`);
        this.toastr.success('Payment advice PDF exported successfully.');
        this.cdr.detectChanges();
      },
      error: () => {
        this.spinner.hide();
        this.toastr.error('Failed to export payment advice PDF.');
        this.cdr.detectChanges();
      }
    });
    this.cdr.detectChanges();
  }

  exploreCustomerFolder(): void {
    if (!this.selectedCustomer) {
      this.cdr.detectChanges();
      return;
    }

    if (this.transactions.length === 0) {
      this.toastr.warning('No transactions found to download.');
      this.cdr.detectChanges();
      return;
    }

    this.spinner.show();
    const custCode = this.selectBy === 1 ? this.selectedCustomer.cust : (this.selectedCustomer.custGroup || '');
    const allTransNos = this.transactions.map(t => t.tranS_NO);

    const request = {
      custNo: custCode,
      chkSendBulk: this.chkSendBulk,
      checkedTransNos: allTransNos
    };

    this.arService.outputCheckedDocuments(request).subscribe({
      next: (blob) => {
        this.spinner.hide();
        const dateStr = new Date().toISOString().split('T')[0];
        this.downloadBlob(blob, `Folder_${custCode}_${dateStr}.zip`);
        this.toastr.success('Customer folder ZIP exported successfully.');
        this.cdr.detectChanges();
      },
      error: () => {
        this.spinner.hide();
        this.toastr.error('Failed to export customer folder ZIP.');
        this.cdr.detectChanges();
      }
    });
    this.cdr.detectChanges();
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
    this.cdr.detectChanges();
  }
}
