import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
// import {
//   HardwareService,
//   PurchaseOrderListItem,
//   CheckErrorsResponse
// } from '../../IMEI/imei-service.ts'

import { PurchaseOrderListItem,CheckErrorsResponse } from '../imei-service';

import { ImeiService } from '../imei-service';

@Component({
  selector: 'app-imei-receive-component',
  imports: [FormsModule,CommonModule],
  templateUrl: './imei-receive-component.html',
  styleUrl: './imei-receive-component.css',
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
  hpcCost:any;
  unitCost:any;

  // ── Verification State ────────────────────────────────────────────────
  verificationStatus: string = 'Not Checked';
  verificationErrors: string[] = [];
  isVerified: boolean = false;
  isProcessing: boolean = false;
  isLoadingPos: boolean = false;
poDisplay = {
  poNumber: '',
  vendor: '',
  whse: '',
  partNo: '',
  orderQty: 0,
  receivedQty: 0
};


  constructor(private hardwareService: ImeiService) {}

  ngOnInit() {
    this.loadPurchaseOrders();
  }

  // ── Load POs — already filtered status I/R + part_no!='' by backend ──
loadPurchaseOrders() {
  this.isLoadingPos = true;
  this.hardwareService.getPurchaseOrderss().subscribe({
    next: (pos) => {
      debugger
      this.purchaseOrders = pos || [];
      this.isLoadingPos = false;
      console.log('Loaded POs:', this.purchaseOrders);  // confirm data here
    },
    error: (err) => {
      alert('Failed to load Purchase Orders: ' + (err.error?.message || err.message));
      this.isLoadingPos = false;
    }
  });
}

  // ── PO Selection (maps to Combo3_AfterUpdate) ─────────────────────────
onPoChange(): void {
  debugger
  this.selectedPo = this.purchaseOrders.find(p => String(p.id) === String(this.selectedPoId)) ?? null;

  if (!this.selectedPo) {
    this.resetVerification();
    return;
  }
this.purchaseOrderId = Number(this.selectedPo.purchaseOrderId);           
this.purchaseOrderLineId = String(this.selectedPo.id);

  this.poDisplay = {
    poNumber: this.selectedPo.poNumber,
    vendor: this.selectedPo.vendor,
    whse: this.selectedPo.whse,
    partNo: this.selectedPo.partNo,
    orderQty: this.selectedPo.orderQty,
    receivedQty: this.selectedPo.receivedQty
  };

      this.unitCost = this.selectedPo.unitCost;
    this.hpcCost = this.unitCost * 0.1;

  this.resetVerification();
}
 onUnitCostChange() {
    this.hpcCost = this.unitCost * 0.1;
  }
  // ── Excel Upload (maps to cmdImportScanList_Click / cmdImportPackingSlip_Click) ──
  onFileSelected(event: any, type: 'packing' | 'scan') {
    const file: File = event.target.files[0];
    if (!file) return;

    // Guard: PO must be selected first (matches VBA: If IsNull(Me.Combo3) Then...)
    if (!this.selectedPo) {
      alert(`You must select a purchase order before importing a ${type === 'packing' ? 'Packing Slip' : 'Scan List'}`);
      event.target.value = '';
      return;
    }

    // Guard: Part number must exist
    if (!this.selectedPo.partNo) {
      alert('There is no PO Part Number selected');
      event.target.value = '';
      return;
    }

    this.hardwareService.uploadExcel(file).subscribe({
      next: imeis => {
        if (type === 'packing') {
          this.packingSlipImeis = imeis;
          this.psCount = imeis.length;
        } else {
          this.scanListImeis = imeis;
          this.slCount = imeis.length;
        }
        this.resetVerification();
      },
      error: err => alert('Upload failed: ' + (err.error?.message || err.message))
    });
  }

  // ── Check Errors (maps to CheckErrors() Sub) ──────────────────────────
  checkErrors() {
    debugger
    // All guards match the VBA CheckErrors sub
    if (!this.selectedPo) {
      alert('You must select an item from a PO');
      return;
    }
    if (this.packingSlipImeis.length === 0) {
      alert('You must import packing slip data.');
      return;
    }
    if (this.scanListImeis.length === 0) {
      alert('You must import scan list data.');
      return;
    }

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
      },
      error: err => alert('Check Errors failed: ' + (err.error?.message || err.message))
    });
  }

  // ── Post Receipts (maps to cmdPostReceipts_Click → ReceivePOIMEI) ─────
 postReceipts() {
    if (!this.isVerified) {
      alert('You must verify data first (no errors)');
      return;
    }

    if (!this.cmoNumber?.trim()) {
      alert('You must enter a CMO number');
      return;
    }

    if (this.isReversal && !confirm('You are processing a REVERSAL\n\nIs this correct?')) {
      return;
    }

    this.isProcessing = true;
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
        if (res.success) {
          alert(`Processed ${this.scanListImeis.length} IMEIs successfully`);
          this.resetForm();
        } else {
          alert('Error: ' + (res.message || 'Unknown error'));
        }
      },
      error: err => {
        this.isProcessing = false;
        alert('Post Receipts failed: ' + (err.error?.message || err.message));
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
    this.resetVerification();
  }

  // Helper: remaining qty for display
  get remainingQty(): number {
    if (!this.selectedPo) return 0;
    return this.selectedPo.orderQty - this.selectedPo.receivedQty;
  }
}


