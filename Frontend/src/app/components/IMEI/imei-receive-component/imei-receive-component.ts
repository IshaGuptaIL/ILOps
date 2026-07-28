import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';
import { SpinnerService } from '../../shared/spinner/spinner-service';
import { PurchaseOrderListItem, CheckErrorsResponse } from '../imei-service';
import { ImeiService } from '../imei-service';

@Component({
  selector: 'app-imei-receive-component',
  standalone: true,
  imports: [FormsModule, CommonModule],
  templateUrl: './imei-receive-component.html',
  styleUrls: ['./imei-receive-component.css'],
})
export class ImeiReceiveComponent implements OnInit {

  // ── PO Selection (maps to Combo3 in frmReceive) ──────────────────────
  purchaseOrders: PurchaseOrderListItem[] = [];
  selectedPoId: number | null = null;
  selectedPo: PurchaseOrderListItem | null = null;

  // Fields mapped from Combo3 columns after selection
  purchaseOrderId: number = 0;
  purchaseOrderLineId: string = '';

  // ── Form Fields ───────────────────────────────────────────────────────
  cmoNumber: string = '';
  isReversal: boolean = false;
  postReceipt: boolean = true;

  // ── IMEI Lists ────────────────────────────────────────────────────────
  packingSlipImeis: string[] = [];
  scanListImeis: string[] = [];

  // ── Result Grids ──────────────────────────────────────────────────────
  matches: string[] = [];
  scanNoPack: string[] = [];
  packNoScan: string[] = [];
  alreadyInInventory: string[] = [];
  invalidScanImeis: string[] = [];
  invalidPackImeis: string[] = [];

  // ── Stats (maps to txtScanListCount, txtPackingSlipCount etc) ─────────
  psCount: number = 0;
  slCount: number = 0;
  psInvalid: number = 0;
  slInvalid: number = 0;
  psDupes: number = 0;
  slDupes: number = 0;
  hpcCost: any;
  unitCost: any;

  // ── Verification State ────────────────────────────────────────────────
  verificationStatus: string = 'Not Checked';
  verificationErrors: string[] = [];
  isVerified: boolean = false;
  isProcessing: boolean = false;
  isLoadingPos: boolean = false;

  selectedScanFile: File | null = null;
  selectedPackingFile: File | null = null;

  poDisplay = {
    poNumber: '',
    vendor: '',
    whse: '',
    partNo: '',
    orderQty: 0,
    receivedQty: 0
  };

  constructor(
    private hardwareService: ImeiService,
    private toastr: ToastrService,
    private spinner: SpinnerService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.loadPurchaseOrders();
  }

  // ── Load POs — already filtered status I/R + part_no!='' by backend ──
  loadPurchaseOrders() {
    this.isLoadingPos = true;
    this.spinner.show();
    this.hardwareService.getPurchaseOrderss().subscribe({
      next: (pos) => {
        this.purchaseOrders = pos || [];
        this.isLoadingPos = false;
        this.spinner.hide();
        console.log('Loaded POs:', this.purchaseOrders);
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isLoadingPos = false;
        this.spinner.hide();
        this.toastr.error('Failed to load Purchase Orders: ' + (err.error?.message || err.message), 'Error');
        this.cdr.detectChanges();
      }
    });
  }

  // ── PO Selection (maps to Combo3_AfterUpdate) ─────────────────────────
  onPoChange(): void {
    this.selectedPo = this.purchaseOrders.find(p => String(p.id) === String(this.selectedPoId)) ?? null;

    if (!this.selectedPo) {
      this.resetVerification();
      this.cdr.detectChanges();
      return;
    }

    this.purchaseOrderId = Number(this.selectedPo.purchaseOrderId);           
    this.purchaseOrderLineId = String(this.selectedPo.id);

    this.poDisplay = {
      poNumber: this.selectedPo.poNumber || '',
      vendor: this.selectedPo.vendor || '',
      whse: this.selectedPo.whse || '',
      partNo: this.selectedPo.partNo || '',
      orderQty: this.selectedPo.orderQty,
      receivedQty: this.selectedPo.receivedQty
    };

    this.unitCost = this.selectedPo.unitCost;
    this.hpcCost = this.unitCost * 0.1;

    this.resetVerification();
    this.cdr.detectChanges();
  }

  onUnitCostChange() {
    this.hpcCost = this.unitCost * 0.1;
    this.cdr.detectChanges();
  }

  onFileChange(event: any, type: 'packing' | 'scan') {
    const file = event.target.files?.[0] || null;
    if (type === 'packing') {
      this.selectedPackingFile = file;
    } else {
      this.selectedScanFile = file;
    }
    this.cdr.detectChanges();
  }

  importFile(type: 'packing' | 'scan') {
    const file = type === 'packing' ? this.selectedPackingFile : this.selectedScanFile;
    if (!file) {
      this.toastr.warning(`Please select a ${type === 'packing' ? 'Packing Slip' : 'Scan List'} file first`, 'File Required');
      return;
    }

    // Guard: PO must be selected first
    if (!this.selectedPo) {
      this.toastr.warning(`You must select a purchase order before importing a ${type === 'packing' ? 'Packing Slip' : 'Scan List'}`, 'Selection Required');
      return;
    }

    // Guard: Part number must exist
    if (!this.selectedPo.partNo) {
      this.toastr.warning('There is no PO Part Number selected', 'Invalid PO');
      return;
    }

    this.spinner.show();
    this.hardwareService.uploadExcel(file).subscribe({
      next: imeis => {
        this.spinner.hide();
        if (type === 'packing') {
          this.packingSlipImeis = imeis;
          this.psCount = imeis.length;
          this.toastr.success(`Imported ${this.psCount} IMEIs into Packing Slip`, 'Success');
        } else {
          this.scanListImeis = imeis;
          this.slCount = imeis.length;
          this.toastr.success(`Imported ${this.slCount} IMEIs into Scan List`, 'Success');
        }
        this.resetVerification();
        this.cdr.detectChanges();
      },
      error: err => {
        this.spinner.hide();
        this.toastr.error('Upload failed: ' + (err.error?.message || err.message), 'Import Error');
        this.cdr.detectChanges();
      }
    });
  }



  // ── Check Errors (maps to CheckErrors() Sub) ──────────────────────────
  checkErrors() {
    // All guards match the VBA CheckErrors sub
    if (!this.selectedPo) {
      this.toastr.warning('You must select an item from a PO', 'Selection Required');
      return;
    }
    if (this.packingSlipImeis.length === 0) {
      this.toastr.warning('You must import packing slip data.', 'Packing Slip Empty');
      return;
    }
    if (this.scanListImeis.length === 0) {
      this.toastr.warning('You must import scan list data.', 'Scan List Empty');
      return;
    }

    this.spinner.show();
    const request = {
      purchaseOrderId:     this.purchaseOrderId,
      purchaseOrderLineId: this.purchaseOrderLineId,
      packingSlipImeis:    this.packingSlipImeis,
      scanListImeis:       this.scanListImeis,
      isReversal:          this.isReversal,
      // Pass Combo3 qty fields so backend can validate (Col 7, 8)
      orderQty:    this.selectedPo.orderQty,
      receivedQty: this.selectedPo.receivedQty,
      whse:        this.selectedPo.whse
    };

    this.hardwareService.checkErrorss(request).subscribe({
      next: res => {
        this.spinner.hide();
        this.verificationErrors = res.errors;
        this.isVerified         = !res.hasErrors;
        this.verificationStatus = res.hasErrors ? 'Errors Found' : 'Verification Successful';

        // Populate all grids
        this.matches            = res.matches;
        this.scanNoPack         = res.scanNoPack;
        this.packNoScan         = res.packNoScan;
        this.alreadyInInventory = res.alreadyInInventory;
        this.invalidScanImeis   = res.invalidScanImeis;
        this.invalidPackImeis   = res.invalidPackImeis;

        // Update stats display
        this.slInvalid = res.invalidScanCount;
        this.psInvalid = res.invalidPackCount;
        this.slDupes   = res.scanDupeCount;
        this.psDupes   = res.packDupeCount;

        if (res.hasErrors) {
          this.toastr.error('Verification failed. Please review the errors list.', 'Errors Found');
        } else {
          this.toastr.success('Verification successful! No errors found.', 'Verified');
        }
        this.cdr.detectChanges();
      },
      error: err => {
        this.spinner.hide();
        this.toastr.error('Check Errors failed: ' + (err.error?.message || err.message), 'Error');
        this.cdr.detectChanges();
      }
    });
  }

  // ── Post Receipts (maps to cmdPostReceipts_Click → ReceivePOIMEI) ─────
  postReceipts() {
    if (!this.isVerified) {
      this.toastr.warning('You must verify data first (no errors)', 'Verification Required');
      return;
    }

    if (!this.cmoNumber?.trim()) {
      this.toastr.warning('You must enter a CMO number', 'CMO Required');
      return;
    }

    if (this.isReversal) {
      Swal.fire({
        title: 'Confirm Reversal',
        text: 'You are processing a REVERSAL. Is this correct?',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: 'Yes, proceed!'
      }).then((result) => {
        if (result.isConfirmed) {
          this.executePostReceipts();
        }
      });
    } else {
      this.executePostReceipts();
    }
  }

  private executePostReceipts() {
    this.isProcessing = true;
    this.spinner.show();
    const request = {
      purchaseOrderId: this.purchaseOrderId,
      purchaseOrderLineId: this.purchaseOrderLineId,
      imeis: this.scanListImeis,
      cmoNumber: this.cmoNumber.trim(),
      isReversal: this.isReversal,
      postReceipt: this.postReceipt
    };

    this.hardwareService.receiveImei(request).subscribe({
      next: res => {
        this.isProcessing = false;
        this.spinner.hide();
        if (res.success) {
          Swal.fire('Success', `Processed ${this.scanListImeis.length} IMEIs successfully`, 'success');
          this.resetForm();
        } else {
          this.toastr.error('Error: ' + (res.message || 'Unknown error'), 'Posting Failed');
        }
        this.cdr.detectChanges();
      },
      error: err => {
        this.isProcessing = false;
        this.spinner.hide();
        this.toastr.error('Post Receipts failed: ' + (err.error?.message || err.message), 'Error');
        this.cdr.detectChanges();
      }
    });
  }

  resetVerification() {
    this.isVerified         = false;
    this.verificationStatus = 'Not Checked';
    this.verificationErrors = [];
    this.matches            = [];
    this.scanNoPack         = [];
    this.packNoScan         = [];
    this.alreadyInInventory = [];
    this.invalidScanImeis   = [];
    this.invalidPackImeis   = [];
    this.slInvalid = 0;
    this.psInvalid = 0;
    this.slDupes   = 0;
    this.psDupes   = 0;
  }

  // Maps to Form_Load / Form_Unload cleanup (delete tblScanList, tblPackingSlip)
  resetForm() {
    this.packingSlipImeis    = [];
    this.scanListImeis       = [];
    this.selectedPoId        = null;
    this.selectedPo          = null;
    this.purchaseOrderId     = 0;
    this.purchaseOrderLineId = '';
    this.cmoNumber           = '';
    this.psCount             = 0;
    this.slCount             = 0;
    this.selectedScanFile    = null;
    this.selectedPackingFile = null;
    this.resetVerification();
    this.cdr.detectChanges();
  }

  // Helper: remaining qty for display
  get remainingQty(): number {
    if (!this.selectedPo) return 0;
    return this.selectedPo.orderQty - this.selectedPo.receivedQty;
  }
}


