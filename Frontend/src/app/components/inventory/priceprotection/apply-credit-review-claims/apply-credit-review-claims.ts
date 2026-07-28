import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';
import { SpinnerService } from '../../../shared/spinner/spinner-service';
import { 
  ApplyCreditReviewClaimsService, 
  ClaimsSummaryRow, 
  CreditSummaryRow, 
  UnpaidClaimsDetailRow, 
  CreditDetailRow 
} from './apply-credit-review-claims.service';

@Component({
  selector: 'app-apply-credit-review-claims',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './apply-credit-review-claims.html',
  styleUrls: ['./apply-credit-review-claims.css']
})
export class ApplyCreditReviewClaimsComponent implements OnInit {
  // Grid Data lists
  claimsSummary: ClaimsSummaryRow[] = [];
  creditSummary: CreditSummaryRow[] = [];
  unpaidClaimsDetail: UnpaidClaimsDetailRow[] = [];
  creditDetail: CreditDetailRow[] = [];

  // Selections
  selectedBatchId: number | null = null;
  selectedCreditRow: CreditSummaryRow | null = null;
  selectedClaimDetailId: number | null = null;

  // Modifier inputs
  newCreditNoteNumber: string = '';

  // Modal State & Bindings
  showModal: boolean = false;
  modalCreditNoteNumber: string = '';
  modalCreditNoteDate: string = '';
  modalCreditUnitAmount: number = 0;
  modalTotalCountSelected: number = 0;
  modalTotalDueSelected: number = 0;
  modalCreditTotal: number = 0;
  modalItems: UnpaidClaimsDetailRow[] = [];

  // Validation Limits
  maxDate: string = '';

  constructor(
    private service: ApplyCreditReviewClaimsService,
    private spinner: SpinnerService,
    private toastr: ToastrService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const today = new Date();
    // Format to yyyy-MM-dd
    this.maxDate = today.toISOString().slice(0, 10);
    this.loadClaimsSummary();
  }

  // #region Loaders for grids

  loadClaimsSummary(): void {
    this.spinner.show();
    this.service.getClaimsSummary().subscribe({
      next: (res) => {
        this.spinner.hide();
        if (res.success) {
          this.claimsSummary = res.result || [];
        } else {
          this.toastr.error(res.message || 'Failed to load Claims Summary', 'Error');
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.spinner.hide();
        this.toastr.error(err.message || 'Server error loading Claims Summary', 'Error');
        this.cdr.detectChanges();
      }
    });
  }

  onSelectBatch(row: ClaimsSummaryRow): void {
    if (this.selectedBatchId === row.claimBatchID) return;
    this.selectedBatchId = row.claimBatchID;

    // Reset subsequent selections and data
    this.selectedCreditRow = null;
    this.selectedClaimDetailId = null;
    this.creditSummary = [];
    this.unpaidClaimsDetail = [];
    this.creditDetail = [];
    this.newCreditNoteNumber = '';

    this.loadCreditSummary(row.claimBatchID);
  }

  loadCreditSummary(batchId: number): void {
    this.spinner.show();
    this.service.getCreditSummary(batchId).subscribe({
      next: (res) => {
        this.spinner.hide();
        if (res.success) {
          this.creditSummary = res.result || [];
        } else {
          this.toastr.error(res.message || 'Failed to load Credit Summary', 'Error');
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.spinner.hide();
        this.toastr.error(err.message || 'Server error loading Credit Summary', 'Error');
        this.cdr.detectChanges();
      }
    });
  }

  onSelectCreditRow(row: CreditSummaryRow): void {
    this.selectedCreditRow = row;

    // Reset subsequent data
    this.selectedClaimDetailId = null;
    this.unpaidClaimsDetail = [];
    this.creditDetail = [];

    this.loadUnpaidClaimsDetail(row.claimBatchID, row.creditNoteNumber);
  }

  loadUnpaidClaimsDetail(batchId: number, creditNoteNumber?: string): void {
    this.spinner.show();
    this.service.getUnpaidClaimsDetail(batchId, creditNoteNumber).subscribe({
      next: (res) => {
        this.spinner.hide();
        if (res.success) {
          this.unpaidClaimsDetail = res.result || [];
        } else {
          this.toastr.error(res.message || 'Failed to load Details', 'Error');
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.spinner.hide();
        this.toastr.error(err.message || 'Server error loading Details', 'Error');
        this.cdr.detectChanges();
      }
    });
  }

  onSelectClaimDetail(row: UnpaidClaimsDetailRow): void {
    this.selectedClaimDetailId = row.id;
    this.creditDetail = [];
    this.loadCreditDetail(row.id);
  }

  loadCreditDetail(claimId: number): void {
    this.spinner.show();
    this.service.getCreditDetail(claimId).subscribe({
      next: (res) => {
        this.spinner.hide();
        if (res.success) {
          this.creditDetail = res.result || [];
        } else {
          this.toastr.error(res.message || 'Failed to load Credit Details', 'Error');
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.spinner.hide();
        this.toastr.error(err.message || 'Server error loading Credit Details', 'Error');
        this.cdr.detectChanges();
      }
    });
  }

  // #endregion

  // #region Action Handlers

  onModifyCreditNoteNumber(): void {
    if (!this.selectedCreditRow) {
      this.toastr.warning('Please select a row in Credit Summary first.');
      return;
    }

    const currentNumber = this.selectedCreditRow.creditNoteNumber;
    if (!currentNumber) {
      this.toastr.error('Cannot modify unassigned/empty credit note number here. Use Enter Credits to assign.', 'Error');
      return;
    }

    if (!this.newCreditNoteNumber || this.newCreditNoteNumber.trim() === '') {
      this.toastr.warning('Please enter a New Credit Note Number.');
      return;
    }

    Swal.fire({
      title: 'Change Credit Note Number?',
      html: `Are you sure you want to change Credit Note Number<br><br><b>From:</b> ${currentNumber}<br><b>To:</b> ${this.newCreditNoteNumber}`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'OK',
      cancelButtonText: 'Cancel',
      confirmButtonColor: '#3085d6',
      cancelButtonColor: '#d33'
    }).then((result) => {
      if (result.isConfirmed) {
        this.spinner.show();
        this.service.modifyCreditNoteNumber(currentNumber, this.newCreditNoteNumber).subscribe({
          next: (res) => {
            this.spinner.hide();
            if (res.success) {
              this.toastr.success('Change Complete');
              this.newCreditNoteNumber = '';
              // Refresh Grid 2
              if (this.selectedBatchId !== null) {
                this.loadCreditSummary(this.selectedBatchId);
              }
            } else {
              this.toastr.error(res.message || 'Failed to modify Credit Note Number', 'Error');
            }
            this.cdr.detectChanges();
          },
          error: (err) => {
            this.spinner.hide();
            this.toastr.error(err.message || 'Error modifying Credit Note Number', 'Error');
            this.cdr.detectChanges();
          }
        });
      }
    });
  }

  exportClaimsSummary(): void {
    this.spinner.show();
    this.service.exportClaimsSummary().subscribe({
      next: (blob) => {
        this.spinner.hide();
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `ClaimsSummary_${new Date().toISOString().slice(0, 10)}.xlsx`;
        a.click();
        window.URL.revokeObjectURL(url);
        this.toastr.success('Excel downloaded successfully.');
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.spinner.hide();
        this.toastr.error(err.message || 'Failed to export Claims Summary', 'Error');
        this.cdr.detectChanges();
      }
    });
  }

  // #endregion

  // #region Apply Credits Dialog Modal

  openApplyCreditsModal(): void {
    if (!this.selectedCreditRow) {
      this.toastr.warning('Please select a claim credit group in Grid 2.');
      return;
    }

    this.modalCreditNoteNumber = '';
    this.modalCreditNoteDate = this.maxDate;
    this.modalCreditUnitAmount = this.selectedCreditRow.unitAmount || 0;

    // VBA logic: display all items in unpaidClaimsDetail for selection
    this.modalItems = this.unpaidClaimsDetail.map(x => ({ ...x, selected: true }));
    
    this.onModalMathChange();
    this.showModal = true;
    this.cdr.detectChanges();
  }

  closeApplyCreditsModal(): void {
    this.showModal = false;
    this.modalItems = [];
    this.cdr.detectChanges();
  }

  selectAllModalItems(): void {
    this.modalItems.forEach(x => x.selected = true);
    this.onModalMathChange();
  }

  selectNoneModalItems(): void {
    this.modalItems.forEach(x => x.selected = false);
    this.onModalMathChange();
  }

  toggleModalItem(item: UnpaidClaimsDetailRow): void {
    item.selected = !item.selected;
    this.onModalMathChange();
  }

  onModalMathChange(): void {
    const selectedList = (this.modalItems || []).filter(x => x.selected);
    this.modalTotalCountSelected = selectedList.length;

    // Due amount is (ClaimAmount - ClaimAmountPaid)
    const totalDue = selectedList.reduce((sum, item) => {
      const claim = Number(item.claimAmount) || 0;
      const paid = Number(item.claimAmountPaid) || 0;
      return sum + (claim - paid);
    }, 0);
    this.modalTotalDueSelected = isNaN(totalDue) ? 0 : totalDue;

    // Credit Total = UnitCreditAmount * totalCountSelected
    const unitAmount = Number(this.modalCreditUnitAmount) || 0;
    const totalCredit = unitAmount * this.modalTotalCountSelected;
    this.modalCreditTotal = isNaN(totalCredit) ? 0 : totalCredit;
    
    this.cdr.detectChanges();
  }

  submitApplyCredit(): void {
    if (this.modalTotalCountSelected === 0) {
      Swal.fire('Warning', 'You have not selected any units.', 'warning');
      return;
    }

    if (!this.modalCreditNoteNumber || this.modalCreditNoteNumber.trim() === '') {
      Swal.fire('Warning', 'You must enter a credit note number.', 'warning');
      return;
    }

    if (!this.modalCreditNoteDate) {
      Swal.fire('Warning', 'You must enter a credit note date.', 'warning');
      return;
    }

    // Constraint: Credit date cannot exceed current date
    const selectedDate = new Date(this.modalCreditNoteDate);
    const currentDate = new Date(this.maxDate);
    if (selectedDate > currentDate) {
      Swal.fire('Warning', 'Credit Note Date cannot be in the future.', 'warning');
      return;
    }

    // Rounding helper to handle JavaScript floating-point errors
    const roundToTwo = (num: number) => Math.round((num + Number.EPSILON) * 100) / 100;

    const roundedCreditTotal = roundToTwo(this.modalCreditTotal);
    const roundedTotalDueSelected = roundToTwo(this.modalTotalDueSelected);

    // VBA Check: Verify credit amount matches sum of selected items due amounts
    if (roundedCreditTotal !== roundedTotalDueSelected) {
      Swal.fire({
        title: 'Amount Variance',
        text: 'The amount of the invoice does not match the total due for the selected items. Are you sure you wish to continue?',
        icon: 'question',
        showCancelButton: true,
        confirmButtonText: 'Yes',
        cancelButtonText: 'No',
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33'
      }).then((result) => {
        if (result.isConfirmed) {
          this.confirmCreditDetailsAndSubmit();
        }
      });
    } else {
      this.confirmCreditDetailsAndSubmit();
    }
  }

  private confirmCreditDetailsAndSubmit(): void {
    // VBA Confirmation message (replicates formatting and currency formats)
    const formattedAmount = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(this.modalCreditTotal);
    
    Swal.fire({
      title: 'Confirm Credit Details',
      html: `<div style="text-align: left; font-family: monospace;">
             Please confirm credit note details:<br><br>
             <b>Credit Note Number:</b> &nbsp;${this.modalCreditNoteNumber}<br>
             <b>Credit Note Date:</b> &nbsp;&nbsp;${this.modalCreditNoteDate}<br>
             <b>Credit Amount:</b> &nbsp;&nbsp;&nbsp;&nbsp;${formattedAmount}<br>
             <b>Unit Count:</b> &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;${this.modalTotalCountSelected}
             </div>`,
      icon: 'info',
      showCancelButton: true,
      confirmButtonText: 'OK',
      cancelButtonText: 'Cancel',
      confirmButtonColor: '#3085d6',
      cancelButtonColor: '#d33'
    }).then((result) => {
      if (result.isConfirmed) {
        this.executeApplyCredit();
      } else {
        this.toastr.info('Recording of credit cancelled.');
      }
    });
  }

  private executeApplyCredit(): void {
    const selectedIds = this.modalItems.filter(x => x.selected).map(x => x.id);

    this.spinner.show();
    this.service.applyCredit({
      claimBatchID: this.selectedBatchId!,
      creditNoteNumber: this.selectedCreditRow?.creditNoteNumber,
      selectedClaimIds: selectedIds,
      applyCreditNoteNumber: this.modalCreditNoteNumber,
      applyCreditNoteDate: this.modalCreditNoteDate,
      creditUnitAmount: this.modalCreditUnitAmount
    }).subscribe({
      next: (res) => {
        this.spinner.hide();
        if (res.success) {
          Swal.fire('Success', 'Credit Recorded', 'success');
          this.closeApplyCreditsModal();
          
          // Re-query/reload all grids to reflect updated amounts
          this.loadClaimsSummary();
          
          if (this.selectedBatchId !== null) {
            this.loadCreditSummary(this.selectedBatchId);
          }
          // Reset child grids
          this.unpaidClaimsDetail = [];
          this.creditDetail = [];
          this.selectedCreditRow = null;
          this.selectedClaimDetailId = null;
        } else {
          Swal.fire('Error', res.message || 'Failed to record credit', 'error');
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.spinner.hide();
        Swal.fire('Error', err.message || 'Error recording credit', 'error');
        this.cdr.detectChanges();
      }
    });
  }

  // #endregion
}
