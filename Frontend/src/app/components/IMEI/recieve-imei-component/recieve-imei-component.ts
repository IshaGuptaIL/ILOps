import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ImeiService, RecieveIMEIBO } from '../imei-service';
import { ApiResponse } from '../../inventory/add-inventory-component/inventory-service';
import { Observable, of } from 'rxjs';
import { delay, tap } from 'rxjs/operators';

import * as XLSX from 'xlsx';
import { saveAs } from 'file-saver';
import { ToastrService } from 'ngx-toastr';


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
  selectedPO:any;

  scanList: IMEIItem[] = [];
  packingSlip: IMEIItem[] = [];
  matches: IMEIItem[] = [];
  scanNoPack: IMEIItem[] = [];
  packNoScan: IMEIItem[] = [];
  onhand: IMEIItem[] = [];

  errorCount = 0;
  errors: string[] = [];

  modeReversal = false;
selectedPONumber: string = '';
  unitCost = 0;
  hpcCost = 0;
  cmoNumber = '';

  scanFile?: File;
  packingSlipFile?: File;

  checkedOnce = false;      // ✅ NEW
canPost = false;          // ✅ NEW
isCheckingErrors = false; 

  constructor(private http: HttpClient,  private toastr: ToastrService,
    private receiveService:ImeiService,private cdr:ChangeDetectorRef) {}

  ngOnInit(): void {
    this.loadPOs();
    // this.loadGrids();
  }

  // Load Purchase Orders
 loadPOs() {
  debugger;
  this.receiveService.getPurchaseOrders().subscribe(
    resp => {
      if (resp.success && resp.result) {
        this.poList = resp.result as PO[];

        this.cdr.markForCheck(); 
      } else {
        this.toastr.error(resp.message || 'Failed to load POs');
      }
    },
    err => {
      console.error(err);
      this.toastr.error('Something went wrong while loading POs');
      this.cdr.markForCheck();
    }
  );
}

  // When PO selected
onPOChange(poNumber: string) {
  debugger;
  this.selectedPO = this.poList.find(p => p.poNumber === poNumber) || null;
  
  if (this.selectedPO) {
    this.unitCost = this.selectedPO.unitCost;
    this.hpcCost = this.unitCost * 0.1;
    this.cmoNumber = '';
    this.resetErrorState();
    
    this.loadGrids();  
  } else {
  }
}

  onUnitCostChange() {
    this.hpcCost = this.unitCost * 0.1;
  }

  // File selection


  onPackingSlipFileSelected(event: any) {
    this.packingSlipFile = event.target.files[0];
  }

  onFileChange(event: any) {
    this.scanFile = event.target.files[0];
  }

  // Upload Scan List
importScanList() {
    if (!this.scanFile || !this.selectedPO) {
      alert('Select file and PO');
      return;
    }


    const reader = new FileReader();
    reader.onload = (e: any) => {
      const data = new Uint8Array(e.target.result);
      const workbook = XLSX.read(data, { type: 'array' });
      const worksheet = workbook.Sheets[workbook.SheetNames[0]];
      const json: any[] = XLSX.utils.sheet_to_json(worksheet, { header: 1 }); // array of arrays

      const items: RecieveIMEIBO[] = [];

      json.forEach((row, index) => {
        if (row[0]) {
          const imei = row[0].toString().trim().toUpperCase();
          items.push({
            PONumber: this.selectedPO.poNumber,
            RecNo: this.selectedPO.poItemId,
            Whse: this.selectedPO.whse,
            PartNo: this.selectedPO.part,
            GUID: this.selectedPO.guid,
            Vendor: this.selectedPO.vendor || '',
            Location: '',
            IMEI: imei,
            XLSRow: index + 1
          });
        }
      });

      this.receiveService.importScanList(items).subscribe({
        next: (resp: ApiResponse) => {
          alert(resp.message);
        },
        error: (err) => {
          console.error(err);
          alert('Failed to upload scan list');
        }
      });
    };

    reader.readAsArrayBuffer(this.scanFile);

    this.loadGrids(); 
  }


  // Upload Packing Slip
  importPackingSlip() {
    debugger
  if (!this.packingSlipFile || !this.selectedPO) {
    alert('Select file and PO');
    return;
  }

  const reader = new FileReader();
  reader.onload = (e: any) => {
    const data = new Uint8Array(e.target.result);
    const workbook = XLSX.read(data, { type: 'array' });
    const worksheet = workbook.Sheets[workbook.SheetNames[0]];
    const json: any[] = XLSX.utils.sheet_to_json(worksheet, { header: 1 }); // array of arrays

    const items: RecieveIMEIBO[] = [];

    json.forEach((row, index) => {
      if (row[0]) { 
        const imei = row[0].toString().trim().toUpperCase();
        items.push({
          PONumber: this.selectedPO.poNumber,
          RecNo: 0, 
          Whse: this.selectedPO.whse,
          PartNo: this.selectedPO.part,
          GUID: this.selectedPO.guid,
          Vendor: this.selectedPO.vendor || '',
          Location: '',
          IMEI: imei,
          XLSRow: index + 1
        });
      }
    });

    this.receiveService.importPackingSlip(items).subscribe({
      next: (resp: ApiResponse) => {
       this.toastr.success(resp.message);
        this.loadGrids();
      },
      error: (err) => {
        console.error(err);
        this.toastr.error('Failed to upload packing slip');
      }
    });
  };

  reader.readAsArrayBuffer(this.packingSlipFile);

  this.loadGrids(); 
}

  // Load all grids
loadGrids() {
  debugger
 if (!this.selectedPO?.poNumber) {
    console.warn('No PO selected');
    return;
  }
  this.receiveService.getIMEIGrids(this.selectedPO.poNumber).subscribe(resp => {
    if (resp.success && resp.result) {
      console.log(resp)
      const data = resp.result as any;
      this.scanList = data.scanList || [];
      this.packingSlip = data.packingSlip || [];
      this.matches = data.matches || [];
      this.scanNoPack = data.scanNoPack || [];
      this.packNoScan = data.packNoScan || [];
      this.onhand = data.onhand || [];
    } else {
      alert(resp.message || 'Failed to load grids');
    }
  }, err => console.error(err));
}

checkErrors() {
  debugger
  if (!this.selectedPO) {
    alert('Please select a Purchase Order first');
    return;
  }

  // Reset state
  this.isCheckingErrors = true;
  this.errors = [];
  this.errorCount = 0;
  this.canPost = false;
  this.checkedOnce = false;

  this.receiveService.checkErrors(
    this.selectedPO.poId,
    this.selectedPO.poItemId,
    this.modeReversal
  ).subscribe({
    next: (resp: any) => {
      const result = resp?.result || {};
      this.errorCount = result.errorCount || 0;
      this.errors = result.errors || [];

      this.canPost = this.errorCount === 0;

      this.checkedOnce = true;
      this.isCheckingErrors = false;

      this.cdr.detectChanges();

      console.log('Check Errors Response:', resp);
      console.log('errorCount:', this.errorCount);
      console.log('canPost:', this.canPost);
      console.log('cmoNumber:', this.cmoNumber);
    },
    error: (err) => {
      console.error('Error checking:', err);
      this.errors = ['Failed to check errors. Please try again.'];
      this.errorCount = 1;
      this.canPost = false;
      this.checkedOnce = true;
      this.isCheckingErrors = false;

      this.cdr.detectChanges();
    }
  });
}
resetErrorState() {
  this.errors = [];
  this.errorCount = 0;
  this.checkedOnce = false;
  this.canPost = false;
}

onModeChange() {
  this.resetErrorState();
}

postReceipts() {
  if (!this.selectedPO) {
    alert('Please select a Purchase Order');
    return;
  }
  
  // ✅ Check if errors checked and passed
  if (!this.canPost) {
    alert('Please run Check Errors first and resolve all errors');
    return;
  }
  
  if (!this.cmoNumber.trim()) {
    alert('Please enter CMO Number');
    return;
  }

  // ✅ Confirm based on mode
  const confirmMsg = this.modeReversal
    ? 'You are processing a REVERSAL. Is this correct?'
    : 'You are posting RECEIPTS. Is this correct?';

  if (!confirm(confirmMsg)) {
    return;
  }

  this.receiveService.postReceipts(
    this.selectedPO.poId,
    this.selectedPO.poItemId,
    this.cmoNumber,
    this.modeReversal
  ).subscribe({
    next: (resp) => {
      alert(resp.message);
      if (resp.success) {
        this.resetErrorState();
        this.loadGrids();
      }
    },
    error: (err) => {
      console.error(err);
      alert('Failed to post receipts');
    }
  });
}
}