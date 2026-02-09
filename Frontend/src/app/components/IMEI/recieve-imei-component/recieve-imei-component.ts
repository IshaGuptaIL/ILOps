import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';


interface PO {
  poNumber: string;
  poId: number;
  poItemId: number;
  vendor: string;
  whse: string;
  part: string;
  ordQty: number;
  unitCost: number;
  guid: string;
}

interface IMEIItem {
  imei: string;
  invalid?: boolean;
  dupe?: boolean;
}

@Component({
  selector: 'app-recieve-imei-component',
  imports: [CommonModule,FormsModule],
  templateUrl: './recieve-imei-component.html',
  styleUrl: './recieve-imei-component.css',
})
export class RecieveImeiComponent implements OnInit {

  poList: PO[] = [];
  selectedPO?: PO;

  scanList: IMEIItem[] = [];
  packingSlip: IMEIItem[] = [];
  matches: IMEIItem[] = [];
  scanNoPack: IMEIItem[] = [];
  packNoScan: IMEIItem[] = [];
  onhand: IMEIItem[] = [];

  errorCount = 0;
  errors: string[] = [];

  modeReversal = false;

  unitCost = 0;
  hpcCost = 0;
  cmoNumber = '';

  scanFile?: File;
  packingSlipFile?: File;

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.loadPOs();
    this.loadGrids();
  }

  // Load Purchase Orders
  loadPOs() {
    this.http.get<PO[]>('/Inventory/ReceiveIMEI/GetPurchaseOrders')
      .subscribe(data => this.poList = data);
  }

  // When PO selected
  onPOChange(poNumber: string) {
    this.selectedPO = this.poList.find(p => p.poNumber === poNumber);
    if (this.selectedPO) {
      this.unitCost = this.selectedPO.unitCost;
      this.hpcCost = this.unitCost * 0.1;
      this.cmoNumber = '';
    }
  }

  // Update HPC cost when Unit cost changes
  onUnitCostChange() {
    this.hpcCost = this.unitCost * 0.1;
  }

  // File selection
  onScanFileSelected(event: any) {
    this.scanFile = event.target.files[0];
  }

  onPackingSlipFileSelected(event: any) {
    this.packingSlipFile = event.target.files[0];
  }

  // Upload Scan List
  importScanList() {
    if (!this.scanFile || !this.selectedPO) { alert('Select file and PO'); return; }

    const fd = new FormData();
    fd.append('poNumber', this.selectedPO.poNumber);
    fd.append('recNo', this.selectedPO.poItemId.toString());
    fd.append('whse', this.selectedPO.whse);
    fd.append('partNo', this.selectedPO.part);
    fd.append('guid', this.selectedPO.guid);
    fd.append('vendor', this.selectedPO.vendor);
    fd.append('location', '');
    fd.append('file', this.scanFile);

    this.http.post<any>('/Inventory/ReceiveIMEI/ImportScanList', fd)
      .subscribe(resp => { alert(resp.message); this.loadGrids(); });
  }

  // Upload Packing Slip
  importPackingSlip() {
    if (!this.packingSlipFile || !this.selectedPO) { alert('Select file and PO'); return; }

    const fd = new FormData();
    fd.append('poNumber', this.selectedPO.poNumber);
    fd.append('recNo', '0');
    fd.append('whse', this.selectedPO.whse);
    fd.append('partNo', this.selectedPO.part);
    fd.append('guid', this.selectedPO.guid);
    fd.append('file', this.packingSlipFile);

    this.http.post<any>('/Inventory/ReceiveIMEI/ImportPackingSlip', fd)
      .subscribe(resp => { alert(resp.message); this.loadGrids(); });
  }

  // Load all grids
  loadGrids() {
    this.http.get<any>('/Inventory/ReceiveIMEI/GetIMEIGrids')
      .subscribe(data => {
        this.scanList = data.scanList || [];
        this.packingSlip = data.packingSlip || [];
        this.matches = data.matches || [];
        this.scanNoPack = data.scanNoPack || [];
        this.packNoScan = data.packNoScan || [];
        this.onhand = data.onhand || [];
      });
  }

  // Check errors
  checkErrors() {
    if (!this.selectedPO) return;

    this.http.get<any>('/Inventory/ReceiveIMEI/CheckErrors', {
      params: {
        poId: this.selectedPO.poId.toString(),
        poItemId: this.selectedPO.poItemId.toString(),
        isReversal: this.modeReversal.toString()
      }
    }).subscribe(resp => {
      this.errorCount = resp.errorCount;
      this.errors = resp.errors || [];
    });
  }

  // Post Receipts
  postReceipts() {
    if (!this.selectedPO) return;
    if (!this.cmoNumber) { alert('Enter CMO Number'); return; }

    this.http.post<any>('/Inventory/ReceiveIMEI/PostReceipts', {
      poId: this.selectedPO.poId,
      poItemId: this.selectedPO.poItemId,
      cmo: this.cmoNumber,
      isReversal: this.modeReversal
    }).subscribe(resp => { alert(resp.message); });
  }
}
