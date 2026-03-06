import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InvoiceCreditService, ApiResponse } from '../invoice-credit-service';
import { SpinnerService } from '../../shared/spinner/spinner-service';
import { finalize } from 'rxjs/operators';
import { Console } from 'console';

interface RogersInvoice {
  transType: string;
  refNo: string;
  transDate: string;
  perUnitAmount: number;
  remarks?: string;
}

interface Receipt {
  bvReceiptNo: string;
  receiptDate: string;
  poNumber: string;
  partNo: string;
  qtyReceived: number;
  unitCost: number;
  cmo: string;
  type: string;
  vendor: string;
  invoices: RogersInvoice[];
}

@Component({
  selector: 'app-invoice-credit-component',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './invoice-credit-component.html',
  styleUrls: ['./invoice-credit-component.css']
})
export class InvoiceCreditComponent implements OnInit {
  receipts: Receipt[] = [];           
  filteredReceipts: Receipt[] = [];   
  selectedReceipt: Receipt | null = null;
  loading = false;

  currentPage = 1;
  itemsPerPage = 10;

  invoiceModalVisible = false;
  editingInvoice: RogersInvoice | null = null;
  invTransType = 'I';
  invRefNo = '';
  invDate = '';
  invAmount = 0;
  invRemarks = '';

  poNumber = '';
  bvReceiptNo = '';
  searchType: 'Hardware' | 'ACC' = 'Hardware';

  constructor(
    private invoiceService: InvoiceCreditService,
    private spinner: SpinnerService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadAllReceipts();
  }

  get paginatedReceipts() {
    const startIndex = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredReceipts.slice(startIndex, startIndex + this.itemsPerPage);
  }

  get totalPages() {
    return Math.ceil(this.filteredReceipts.length / this.itemsPerPage) || 1;
  }

  setPage(page: number) {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
    }
  }

  toggleLoading(state: boolean) {
    this.loading = state;
    if (state) this.spinner.show();
    else this.spinner.hide();
  }

  loadAllReceipts() {
    this.toggleLoading(true);
    this.invoiceService.getAllReceipts().pipe(
      finalize(() => this.toggleLoading(false))
    ).subscribe(res => {
      if (res.success && res.result) {
        this.receipts = res.result.map((r: any) => this.mapReceipt(r));
        this.filterReceiptsByType();
      }
    });
  }

  filterReceiptsByType() {
  // Target fix karein
  const target = this.searchType === 'Hardware' ? 'HDW' : 'ACC';
  
  this.filteredReceipts = this.receipts.filter(r => {
    // Null check aur clean string comparison
    const rType = (r.type || '').trim().toUpperCase();
    return rType === target || rType.includes(target);
  });

  this.currentPage = 1;
  
  if (this.filteredReceipts.length > 0) {
    this.selectReceipt(this.filteredReceipts[0]);
  } else {
    this.selectedReceipt = null;
    // Debugging ke liye: console.log('No match for:', target, 'in', this.receipts);
  }
}
  findReceiptByBVNo() {
    const term = this.bvReceiptNo.trim();
    if (!term) { this.loadAllReceipts(); return; }

    this.toggleLoading(true);
    this.invoiceService.findReceiptByBVNo(term, this.searchType).pipe(
      finalize(() => this.toggleLoading(false))
    ).subscribe(res => {
      if (res.success && res.result) {
        // Result array ho ya single object, handle karein
        const data = Array.isArray(res.result) ? res.result : [res.result];
        this.filteredReceipts = data.map(r => this.mapReceipt(r));
        
        if (this.filteredReceipts.length > 0) {
          this.selectReceipt(this.filteredReceipts[0]);
        }
      } else {
        this.filteredReceipts = [];
        this.selectedReceipt = null;
        alert(res.message || "No Receipts Found");
      }
    });
} 

  findByPONumber() {
    if (!this.poNumber.trim()) return;
    this.toggleLoading(true);
    this.invoiceService.getMissingReceiptsByPO(this.poNumber.trim()).pipe(
      finalize(() => this.toggleLoading(false))
    ).subscribe(res => {
      if (res.success && res.result) {
        console.log(res)
        this.filteredReceipts = res.result.map((r: any) => this.mapReceipt(r));
        if (this.filteredReceipts.length > 0) this.selectReceipt(this.filteredReceipts[0]);
      } else {
        alert(res.message || "No records found");
      }
    });
  }

  selectReceipt(r: Receipt) {
    this.selectedReceipt = r;
    this.invoiceService.getInvoices(r.bvReceiptNo).subscribe(res => {
      if (res.success && this.selectedReceipt) {
        this.selectedReceipt.invoices = res.result.map((inv: any) => ({
          transType: inv.transType || inv.TransType,
          refNo: inv.refNo || inv.RefNo,
          transDate: (inv.transDate || inv.TransDate || '').substring(0, 10),
          perUnitAmount: inv.amount || inv.Amount || inv.perUnitAmount || 0,
          remarks: inv.remarks || inv.Remarks || ''
        }));
      }
    });
  }

  private mapReceipt(r: any): Receipt {
    return {
      bvReceiptNo: r.bvReceiptNo || r.BVReceiptNo || '',
      receiptDate: (r.receiptDate || r.ReceiptDate || '').substring(0, 10),
      poNumber: r.poNumber || r.PONumber || '',
      partNo: r.partNo || r.PartNo || '',
      qtyReceived: r.qtyReceived || r.QtyReceived || 0,
      unitCost: r.unitCost || r.UnitCost || 0,
      cmo: r.cmo || r.CMO || '',
      type: r.type || r.Type || '',
      vendor: r.vendor || r.Vendor || '',
      invoices: []
    };
  }

  openInvoiceModal(invoice?: RogersInvoice) {
    this.editingInvoice = invoice || null;
    this.invTransType = invoice?.transType || 'I';
    this.invRefNo = invoice?.refNo || '';
    this.invDate = invoice?.transDate || new Date().toISOString().substring(0, 10);
    this.invAmount = invoice?.perUnitAmount || 0;
    this.invRemarks = invoice?.remarks || '';
    this.invoiceModalVisible = true;
  }

  closeInvoiceModal() { this.invoiceModalVisible = false; }

  saveInvoice() {
    if (!this.invRefNo) { alert('Ref No is required'); return; }
    const payload = {
      BVReceiptNo: this.selectedReceipt?.bvReceiptNo,
      TransType: this.invTransType,
      RefNo: this.invRefNo,
      TransDate: this.invDate,
      PerUnitAmount: this.invAmount,
      Remarks: this.invRemarks
    };

    this.toggleLoading(true);
    this.invoiceService.saveInvoice(payload).pipe(
      finalize(() => this.toggleLoading(false))
    ).subscribe(res => {
      if (res.success) {
        this.selectReceipt(this.selectedReceipt!); 
        this.closeInvoiceModal();
      } else { alert(res.message); }
    });
  }

  loadNewAccReceipts() {
    this.toggleLoading(true);
    this.invoiceService.loadAccReceipts().pipe(
      finalize(() => this.toggleLoading(false))
    ).subscribe(res => {
      if (res.success) { alert("Success!"); this.loadAllReceipts(); }
    });
  }
}