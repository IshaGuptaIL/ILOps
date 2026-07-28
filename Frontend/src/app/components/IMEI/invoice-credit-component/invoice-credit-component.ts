import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';
import { InvoiceCreditService, ApiResponse } from '../invoice-credit-service';
import { SpinnerService } from '../../shared/spinner/spinner-service';
import { finalize } from 'rxjs/operators';

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
    private toastr: ToastrService,
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
      this.cdr.detectChanges();
    }
  }

  toggleLoading(state: boolean) {
    this.loading = state;
    if (state) this.spinner.show();
    else this.spinner.hide();
    this.cdr.detectChanges();
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
      this.cdr.detectChanges();
    });
  }

  filterReceiptsByType() {
    const target = this.searchType === 'Hardware' ? 'HDW' : 'ACC';
    
    this.filteredReceipts = this.receipts.filter(r => {
      const rType = (r.type || '').trim().toUpperCase();
      return rType === target || rType.includes(target);
    });

    this.currentPage = 1;
    
    if (this.filteredReceipts.length > 0) {
      this.selectReceipt(this.filteredReceipts[0]);
    } else {
      this.selectedReceipt = null;
    }
    this.cdr.detectChanges();
  }

  findReceiptByBVNo() {
    const term = this.bvReceiptNo.trim();
    if (!term) { this.loadAllReceipts(); return; }

    this.toggleLoading(true);
    this.invoiceService.findReceiptByBVNo(term, this.searchType).pipe(
      finalize(() => this.toggleLoading(false))
    ).subscribe(res => {
      this.currentPage = 1;
      if (res.success && res.result) {
        const data = Array.isArray(res.result) ? res.result : [res.result];
        this.filteredReceipts = data.map(r => this.mapReceipt(r));
        
        if (this.filteredReceipts.length > 0) {
          this.selectReceipt(this.filteredReceipts[0]);
        }
      } else {
        this.filteredReceipts = [];
        this.selectedReceipt = null;
        this.toastr.info(res.message || "No Receipts Found", 'Search Result');
      }
      this.cdr.detectChanges();
    });
  } 

  findByPONumber() {
    const term = this.poNumber.trim();
    if (!term) { this.loadAllReceipts(); return; }

    this.toggleLoading(true);
    this.invoiceService.getMissingReceiptsByPO(term).pipe(
      finalize(() => this.toggleLoading(false))
    ).subscribe(res => {
      this.currentPage = 1;
      if (res.success && res.result) {
        console.log(res);
        this.filteredReceipts = res.result.map((r: any) => this.mapReceipt(r));
        if (this.filteredReceipts.length > 0) this.selectReceipt(this.filteredReceipts[0]);
      } else {
        this.filteredReceipts = [];
        this.selectedReceipt = null;
        this.toastr.info(res.message || "No records found", 'Search Result');
      }
      this.cdr.detectChanges();
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
      this.cdr.detectChanges();
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
    this.cdr.detectChanges();
  }

  closeInvoiceModal() { 
    this.invoiceModalVisible = false; 
    this.cdr.detectChanges();
  }

  saveInvoice() {
    if (!this.invRefNo) { 
      this.toastr.warning('Ref No is required', 'Validation Error'); 
      return; 
    }
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
        this.toastr.success('Invoice details saved successfully', 'Success');
        this.selectReceipt(this.selectedReceipt!); 
        this.closeInvoiceModal();
      } else { 
        this.toastr.error(res.message || 'Failed to save invoice', 'Error'); 
      }
      this.cdr.detectChanges();
    });
  }

  loadNewAccReceipts() {
    this.toggleLoading(true);
    this.invoiceService.loadAccReceipts().pipe(
      finalize(() => this.toggleLoading(false))
    ).subscribe(res => {
      if (res.success) { 
        Swal.fire('Success', 'Accessory Receipts Loaded Successfully', 'success');
        this.loadAllReceipts(); 
      } else {
        this.toastr.error(res.message || 'Failed to load receipts', 'Error');
      }
      this.cdr.detectChanges();
    });
  }
}