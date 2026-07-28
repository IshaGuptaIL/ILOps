import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { 
  RogersInvoiceSpireService, 
  ProcessDataRequest, 
  CostVerificationRow, 
  DailySalesRow, 
  ReturnsVerificationRow 
} from './rogers-invoice-spire.service';
import { SpinnerService } from '../../../components/shared/spinner/spinner-service';
import { PaginationComponent } from '../../../components/shared/pagination/pagination.component';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-rogers-invoice-spire',
  standalone: true,
  imports: [CommonModule, FormsModule, PaginationComponent],
  templateUrl: './rogers-invoice-spire.component.html',
  styleUrl: './rogers-invoice-spire.component.css'
})
export class RogersInvoiceSpireComponent implements OnInit {
  // Input parameters
  preprocessStartDate: string = '';
  preprocessEndDate: string = '';
  costStartDate: string = '';
  costEndDate: string = '';
  salesStartDate: string = '';
  salesEndDate: string = '';
  returnsStart: string = '';
  returnsEnd: string = '';
  searchQuery: string = '';

  // Tab State: 'preprocess' | 'cost' | 'sales'
  activeTab: string = 'preprocess';

  // Datasets
  costReportData: CostVerificationRow[] = [];
  filteredCostData: CostVerificationRow[] = [];
  
  salesReportData: DailySalesRow[] = [];
  filteredSalesData: DailySalesRow[] = [];
  
  returnsReportData: ReturnsVerificationRow[] = [];
  filteredReturnsData: ReturnsVerificationRow[] = [];

  // Pagination state
  currentPage: number = 1;
  pageSize: number = 15;
  totalPages: number = 1;
  paginatedData: any[] = [];

  constructor(
    private service: RogersInvoiceSpireService,
    private spinner: SpinnerService,
    private toastr: ToastrService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    // Default dates to current month range
    const now = new Date();
    const firstDay = new Date(now.getFullYear(), now.getMonth(), 1);
    const startStr = this.formatDate(firstDay);
    const endStr = this.formatDate(now);
    
    this.preprocessStartDate = startStr;
    this.preprocessEndDate = endStr;
    this.costStartDate = startStr;
    this.costEndDate = endStr;
    this.salesStartDate = startStr;
    this.salesEndDate = endStr;
    
    // Default returns range to last 30 days
    const past30 = new Date();
    past30.setDate(now.getDate() - 30);
    this.returnsStart = this.formatDate(past30);
    this.returnsEnd = this.formatDate(now);
  }

  private formatDate(date: Date): string {
    const yyyy = date.getFullYear();
    const mm = String(date.getMonth() + 1).padStart(2, '0');
    const dd = String(date.getDate()).padStart(2, '0');
    return `${yyyy}-${mm}-${dd}`;
  }

  // --- Tab Management ---
  selectTab(tab: string) {
    this.activeTab = tab;
    this.currentPage = 1;
    this.searchQuery = '';
    this.applyFilterAndPagination();
    this.cdr.detectChanges();
  }



  // --- Actions & API Fetching ---
  processData() {
    if (!this.preprocessStartDate || !this.preprocessEndDate) {
      this.toastr.warning('Please select both Start Date and End Date.');
      return;
    }
    if (new Date(this.preprocessStartDate) > new Date(this.preprocessEndDate)) {
      this.toastr.warning('End Date must be greater than or equal to Start Date.');
      return;
    }

    Swal.fire({
      title: 'Run Process Data?',
      text: 'This will clear temporary user session records and run the preprocessing pipeline.',
      icon: 'question',
      showCancelButton: true,
      confirmButtonText: 'Run Pipeline',
      cancelButtonText: 'Cancel'
    }).then((result) => {
      if (result.isConfirmed) {
        this.spinner.show();
        const request: ProcessDataRequest = {
          startDate: this.preprocessStartDate,
          endDate: this.preprocessEndDate
        };

        this.service.processData(request).subscribe({
          next: (res) => {
            this.spinner.hide();
            if (res.success) {
              this.toastr.success('Preprocessing Completed.');
              Swal.fire('Complete', res.message, 'success');
            } else {
              this.toastr.error('Process failed.');
              Swal.fire('Error', res.message, 'error');
            }
            this.cdr.detectChanges();
          },
          error: (err) => {
            this.spinner.hide();
            this.toastr.error('An error occurred during data processing.');
            Swal.fire('Error', err.error?.message || 'Server error occurred.', 'error');
            this.cdr.detectChanges();
          }
        });
      }
    });
  }

  fetchCostVerification() {
    if (!this.costStartDate || !this.costEndDate) {
      this.toastr.warning('Start and End dates are required.');
      return;
    }
    if (new Date(this.costStartDate) > new Date(this.costEndDate)) {
      this.toastr.warning('End Date must be greater than or equal to Start Date.');
      return;
    }

    this.spinner.show();
    this.service.getCostVerificationReport(this.costStartDate, this.costEndDate).subscribe({
      next: (data) => {
        this.spinner.hide();
        this.costReportData = data || [];
        this.toastr.success(`Loaded ${this.costReportData.length} records.`);
        this.selectTab('cost');
      },
      error: (err) => {
        this.spinner.hide();
        this.toastr.error('Failed to load Cost Verification Report.');
        Swal.fire('Error', err.error?.message || 'Server error.', 'error');
      }
    });
  }

  fetchDailySalesSummary() {
    if (!this.salesStartDate || !this.salesEndDate) {
      this.toastr.warning('Start and End dates are required.');
      return;
    }
    if (new Date(this.salesStartDate) > new Date(this.salesEndDate)) {
      this.toastr.warning('End Date must be greater than or equal to Start Date.');
      return;
    }

    this.spinner.show();
    this.service.getDailySalesSummary(this.salesStartDate, this.salesEndDate).subscribe({
      next: (data) => {
        this.spinner.hide();
        this.salesReportData = data || [];
        this.toastr.success(`Loaded ${this.salesReportData.length} summary rows.`);
        this.selectTab('sales');
      },
      error: (err) => {
        this.spinner.hide();
        this.toastr.error('Failed to load Daily Sales Summary.');
        Swal.fire('Error', err.error?.message || 'Server error.', 'error');
      }
    });
  }

  fetchReturnsVerification() {
    if (!this.preprocessStartDate || !this.preprocessEndDate || !this.returnsStart || !this.returnsEnd) {
      this.toastr.warning('All date parameters (Start, End, Returns Start, Returns End) are required.');
      return;
    }
    if (new Date(this.preprocessStartDate) > new Date(this.preprocessEndDate)) {
      this.toastr.warning('End Date must be greater than or equal to Start Date.');
      return;
    }
    if (new Date(this.returnsStart) > new Date(this.returnsEnd)) {
      this.toastr.warning('Returns End Date must be greater than or equal to Returns Start Date.');
      return;
    }

    this.spinner.show();
    this.service.getReturnsVerificationReport(this.preprocessStartDate, this.preprocessEndDate, this.returnsStart, this.returnsEnd).subscribe({
      next: (data) => {
        this.spinner.hide();
        this.returnsReportData = data || [];
        this.toastr.success(`Loaded ${this.returnsReportData.length} return validation records.`);
        // Note: Returns validation is displayed within the Data Preparation tab
        this.selectTab('preprocess');
        this.currentPage = 1;
        this.activeTab = 'preprocess';
        this.applyFilterAndPagination();
      },
      error: (err) => {
        this.spinner.hide();
        this.toastr.error('Failed to load Returns Verification Report.');
        Swal.fire('Error', err.error?.message || 'Server error.', 'error');
      }
    });
  }

  runHdwFeeCheck() {
    this.spinner.show();
    this.service.getHdwFeeReport().subscribe({
      next: (data) => {
        this.spinner.hide();
        if (data && data.length > 0) {
          const headers = [
            'transactionNo', 'invoice', 'invoiceDate', 'custName', 'custTerritory', 
            'whse', 'partNumber', 'freeAccessory', 'qty', 'imeiesn', 'costPrice', 
            'sellPrice', 'topUpOwing', 'bvReceiptCost', 'netIMEIReceiveCost', 
            'netPriceProtection', 'poNumber', 'bvReceipt', 'misC_1'
          ];
          const displayHeaders = [
            'TransactionNo', 'Invoice', 'InvoiceDate', 'CustName', 'CustTerritory',
            'Whse', 'PartNumber', 'FreeAccessory', 'Qty', 'IMEIESN', 'CostPrice',
            'SellPrice', 'TopUpOwing', 'BVReceiptCost', 'NetIMEI ReceiveCost',
            'NetPriceProtection', 'PONumber', 'BVReceipt', 'MISC_1'
          ];
          this.exportToCsv('HDWFeeNot1or5.csv', data, headers, displayHeaders);
          Swal.fire('HDW Check Complete', `Found ${data.length} hardware records where fee is not $1 or $5. The CSV report has been downloaded.`, 'info');
        } else {
          Swal.fire('HDW Check Complete', 'No hardware records found where fee is not $1 or $5.', 'info');
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.spinner.hide();
        this.toastr.error('Failed to run HDW Fee check.');
        Swal.fire('Error', err.error?.message || 'Server error.', 'error');
      }
    });
  }

  applyFilterAndPagination() {
    const q = this.searchQuery ? this.searchQuery.toLowerCase().trim() : '';
    
    if (this.activeTab === 'cost') {
      this.filteredCostData = this.costReportData.filter(r => 
        !q ||
        (r.invoice && r.invoice.toLowerCase().includes(q)) ||
        (r.custName && r.custName.toLowerCase().includes(q)) ||
        (r.partNumber && r.partNumber.toLowerCase().includes(q)) ||
        (r.imeiesn && r.imeiesn.toLowerCase().includes(q))
      );
      this.totalPages = Math.ceil(this.filteredCostData.length / this.pageSize) || 1;
      const start = (this.currentPage - 1) * this.pageSize;
      this.paginatedData = this.filteredCostData.slice(start, start + this.pageSize);
    } 
    else if (this.activeTab === 'sales') {
      this.filteredSalesData = this.salesReportData.filter(r => 
        !q ||
        (r.invoiceNo && r.invoiceNo.toLowerCase().includes(q)) ||
        (r.custName && r.custName.toLowerCase().includes(q)) ||
        (r.paymentMethod && r.paymentMethod.toLowerCase().includes(q))
      );
      this.totalPages = Math.ceil(this.filteredSalesData.length / this.pageSize) || 1;
      const start = (this.currentPage - 1) * this.pageSize;
      this.paginatedData = this.filteredSalesData.slice(start, start + this.pageSize);
    } 
    else if (this.activeTab === 'preprocess') {
      this.filteredReturnsData = this.returnsReportData.filter(r => 
        !q ||
        (r.invoice && r.invoice.toLowerCase().includes(q)) ||
        (r.partNumber && r.partNumber.toLowerCase().includes(q)) ||
        (r.webOrderID && r.webOrderID.toLowerCase().includes(q)) ||
        (r.invoice2 && r.invoice2.toLowerCase().includes(q))
      );
      this.totalPages = Math.ceil(this.filteredReturnsData.length / this.pageSize) || 1;
      const start = (this.currentPage - 1) * this.pageSize;
      this.paginatedData = this.filteredReturnsData.slice(start, start + this.pageSize);
    }
  }

  onPageChanged(page: number) {
    this.currentPage = page;
    this.applyFilterAndPagination();
    this.cdr.detectChanges();
  }

  onSearchChanged() {
    this.currentPage = 1;
    this.applyFilterAndPagination();
    this.cdr.detectChanges();
  }

  getUserId(): number {
    const name = 'userId=';
    const decodedCookie = decodeURIComponent(document.cookie);
    const ca = decodedCookie.split(';');
    for (let i = 0; i < ca.length; i++) {
      let c = ca[i];
      while (c.charAt(0) === ' ') {
        c = c.substring(1);
      }
      if (c.indexOf(name) === 0) {
        const val = c.substring(name.length, c.length);
        return parseInt(val, 10) || 1;
      }
    }
    return 1;
  }

  downloadRogersEstimate() {
    const userId = this.getUserId();
    this.service.downloadRogersEstimate(userId).subscribe({
      next: (blob: Blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        
        const dateObj = new Date();
        const dateStr = dateObj.getFullYear().toString() +
                        (dateObj.getMonth() + 1).toString().padStart(2, '0') +
                        dateObj.getDate().toString().padStart(2, '0') + '_' +
                        dateObj.getHours().toString().padStart(2, '0') +
                        dateObj.getMinutes().toString().padStart(2, '0') +
                        dateObj.getSeconds().toString().padStart(2, '0');

        link.download = `RogersInvoiceEstimate_${dateStr}.csv`;
        link.click();
        window.URL.revokeObjectURL(url);
        this.toastr.success('Rogers Estimate downloaded successfully.');
      },
      error: (err: any) => {
        this.toastr.error('Error downloading Rogers Estimate.');
      }
    });
  }

  exportCostCsv() {
    const headers = [
      'transactionNo', 'invoice', 'invoiceDate', 'custName', 'custTerritory', 
      'whse', 'partNumber', 'freeAccessory', 'qty', 'imeiesn', 'costPrice', 
      'sellPrice', 'topUpOwing', 'bvReceiptCost', 'netIMEIReceiveCost', 
      'netPriceProtection', 'poNumber', 'bvReceipt', 'misC_1'
    ];
    const displayHeaders = [
      'TransactionNo', 'Invoice', 'InvoiceDate', 'CustName', 'CustTerritory',
      'Whse', 'PartNumber', 'FreeAccessory', 'Qty', 'IMEIESN', 'CostPrice',
      'SellPrice', 'TopUpOwing', 'BVReceiptCost', 'NetIMEI ReceiveCost',
      'NetPriceProtection', 'PONumber', 'BVReceipt', 'MISC_1'
    ];
    this.exportToCsv('Cost_Verification_Report.csv', this.costReportData, headers, displayHeaders);
  }

  exportSalesCsv() {
    const headers = [
      'invoiceNo', 'webOrderID', 'date', 'paymentMethod', 'transNo', 
      'custNo', 'custName', 'total', 'invTerr', 'custTerr'
    ];
    this.exportToCsv('Daily_Sales_Summary.csv', this.salesReportData, headers);
  }

  exportReturnsCsv() {
    const headers = [
      'invoice', 'invoiceDate', 'partNumber', 'qty', 'costPrice', 'sellPrice', 
      'topUpOwing', 'accessoryPrice', 'topUpAcc', 'topUpTotal', 'arAmount', 
      'invoice2', 'invoiceDate2', 'partNumber2', 'qty2', 'costPrice2', 
      'sellPrice2', 'topUpOwing2', 'accessoryPrice2', 'topUpAcc2', 'topUpTotal2', 'arAmount2'
    ];
    this.exportToCsv('Returns_Verification_Report.csv', this.returnsReportData, headers);
  }

  private exportToCsv(filename: string, rows: any[], headers: string[], displayHeaders?: string[]) {
    if (!rows || !rows.length) {
      this.toastr.warning('No data available to export.');
      return;
    }
    const finalHeaders = displayHeaders && displayHeaders.length === headers.length ? displayHeaders : headers;
    const separator = ',';
    const csvContent =
      finalHeaders.join(separator) +
      '\n' +
      rows.map(row => {
        return headers.map(fieldName => {
          let val = row[fieldName];
          if (val === null || val === undefined) val = '';
          val = val.toString().replace(/"/g, '""');
          if (val.search(/("|,|\n)/g) >= 0) val = `"${val}"`;
          return val;
        }).join(separator);
      }).join('\n');

    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    if (link.download !== undefined) {
      const url = URL.createObjectURL(blob);
      link.setAttribute('href', url);
      link.setAttribute('download', filename);
      link.style.visibility = 'hidden';
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
    }
  }
}
