import { Component, ChangeDetectorRef, OnDestroy, OnInit, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import Swal from 'sweetalert2';
import { ToastrService } from 'ngx-toastr';
import { RogerSalesReportingService } from './roger-sales-reporting.service';
import { SpinnerService } from '../../shared/spinner/spinner-service';

interface SalesActivationRow {
  [key: string]: any;
  Invoice10: string;
  TransactionNo: string;
  InvoiceDate: Date;
  OrderDate: Date;
  CustName: string;
  CustTerritory: string;
  UserName: string;
  CellPhoneNo: string;
  VoicePlan: string;
  DataPlan: string;
  WebOrderID: string;
  Type: string;
  AdjustmentType: string;
  Supress: boolean;
  Fee: number;
  FeeCount: number;
  TopUpOwing: number;
  // Department columns
  CoOpAdvertisingHO: number;
  MiscellaneousGBMNDSIncExp: number;
  OtherRevenueHO: number;
  OtherRevenueCO: number;
  ReceivableUpfrontEdgeRV: number;
  SalesAccessoriesCO: number;
  SalesHardwareCO: number;
  StagingAndDeployment: number;
  UnallocatedSales: number;
  WebHosting: number;
  // Additional columns
  PartNumber: string;
  ProductCode: string;
  IMEIESN: string;
  CostPrice: number;
  SellPrice: number;
  InvoiceNet: number;
  InvoiceTotal: number;
}

@Component({
  selector: 'app-roger-sales-reporting-component',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './roger-sales-reporting-component.html',
  styleUrls: ['./roger-sales-reporting-component.css']
})
export class RogerSalesReportingComponent implements OnInit, OnDestroy {
  startDate: string = '';
  endDate: string = '';
  criteria: string = 'All Territories';
  territory: string = '';

  // Data table properties
  showDataTable: boolean = false;
  currentTableTitle: string = '';
  tableData: SalesActivationRow[] = [];
  filteredData: SalesActivationRow[] = [];
  paginatedData: SalesActivationRow[] = [];
  columnFilters: { [key: string]: string } = {};

  activeFilterColumn: string | null = null;
  sortColumnName: string | null = null;
  sortDirection: 'asc' | 'desc' = 'asc';

  columns = [
    { field: 'Invoice10', label: 'Invoice10', type: 'text' },
    { field: 'TransactionNo', label: 'TransactionNo', type: 'text' },
    { field: 'InvoiceDate', label: 'InvoiceDate', type: 'date' },
    { field: 'OrderDate', label: 'OrderDate', type: 'date' },
    { field: 'CustName', label: 'CustName', type: 'text' },
    { field: 'CustTerritory', label: 'CustTerritory', type: 'edit-text' },
    { field: 'UserName', label: 'UserName', type: 'text' },
    { field: 'CellPhoneNo', label: 'CellPhoneNo', type: 'text' },
    { field: 'VoicePlan', label: 'VoicePlan', type: 'text' },
    { field: 'DataPlan', label: 'DataPlan', type: 'text' },
    { field: 'WebOrderID', label: 'WebOrderID', type: 'text' },
    { field: 'Type', label: 'Type', type: 'text' },
    { field: 'AdjustmentType', label: 'AdjustmentType', type: 'edit-text' },
    { field: 'Supress', label: 'Supress', type: 'edit-checkbox' },
    { field: 'Fee', label: 'Fee', type: 'edit-number' },
    { field: 'FeeCount', label: 'FeeCount', type: 'number' },
    { field: 'TopUpOwing', label: 'TopUpOwing', type: 'currency' },
    { field: 'CoOpAdvertisingHO', label: 'Co-Op Advertising - HO', type: 'currency' },
    { field: 'MiscellaneousGBMNDSIncExp', label: 'Miscellaneous GBM NDS Inc/Exp', type: 'currency' },
    { field: 'OtherRevenueHO', label: 'Other Revenue - HO', type: 'currency' },
    { field: 'OtherRevenueCO', label: 'Other Revenue - CO', type: 'currency' },
    { field: 'ReceivableUpfrontEdgeRV', label: 'Receivable - Upfront Edge - RV', type: 'currency' },
    { field: 'SalesAccessoriesCO', label: 'SALES - Accessories - CO', type: 'currency' },
    { field: 'SalesHardwareCO', label: 'SALES - Hardware - CO', type: 'currency' },
    { field: 'StagingAndDeployment', label: 'Staging and Deployment', type: 'currency' },
    { field: 'UnallocatedSales', label: 'Unallocated Sales', type: 'currency' },
    { field: 'WebHosting', label: 'Web Hosting', type: 'currency' },
    { field: 'PartNumber', label: 'PartNumber', type: 'text' },
    { field: 'ProductCode', label: 'ProductCode', type: 'text' },
    { field: 'IMEIESN', label: 'IMEIESN', type: 'text' },
    { field: 'CostPrice', label: 'CostPrice', type: 'currency' },
    { field: 'SellPrice', label: 'SellPrice', type: 'currency' },
    { field: 'InvoiceNet', label: 'InvoiceNet', type: 'currency' },
    { field: 'InvoiceTotal', label: 'InvoiceTotal', type: 'currency' }
  ];

  // Pagination
  currentPage: number = 1;
  pageSize: number = 50;
  totalPages: number = 1;

  private subscriptions: Subscription = new Subscription();

  constructor(
    private cdr: ChangeDetectorRef,
    private salesService: RogerSalesReportingService,
    private spinner: SpinnerService,
    private toastr: ToastrService
  ) {}

  ngOnInit(): void {
    // Initialize dates to current month
    const now = new Date();
    const firstDay = new Date(now.getFullYear(), now.getMonth(), 1);
    this.startDate = firstDay.toISOString().split('T')[0];
    this.endDate = now.toISOString().split('T')[0];
    this.cdr.detectChanges();
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  onFilterChange(): void {
    this.cdr.detectChanges();
  }

  onCriteriaChange(): void {
    if (this.criteria !== 'Specific Territory') {
      this.territory = '';
    }
    this.cdr.detectChanges();
  }

  private validateInputs(): boolean {
    if (!this.startDate || !this.endDate) {
      this.toastr.warning('Please select both Start and End Dates.', 'Warning');
      return false;
    }
    
    if (this.criteria === 'Specific Territory' && !this.territory.trim()) {
      this.toastr.warning('You must enter a territory code.', 'Warning');
      return false;
    }
    
    return true;
  }

  private handleViewAction(endpoint: string, title: string): void {
    if (!this.validateInputs()) return;

    this.spinner.show();
    this.cdr.detectChanges();

    const sub = this.salesService.executeViewAction(endpoint, this.startDate, this.endDate, this.criteria, this.territory)
      .subscribe({
        next: (data: SalesActivationRow[]) => {
          this.spinner.hide();
          this.tableData = data;
          this.filteredData = [...data];
          this.currentTableTitle = title;
          this.showDataTable = true;
          this.clearFilters();
          this.cdr.detectChanges();
        },
        error: (err: any) => {
          this.spinner.hide();
          this.cdr.detectChanges();
          Swal.fire('Error', `Failed to load ${title}: ` + err.message, 'error');
        }
      });
      
    this.subscriptions.add(sub);
  }

  private handleOutputAction(endpoint: string, title: string): void {
    if (!this.validateInputs()) return;

    this.spinner.show();
    this.cdr.detectChanges();

    const sub = this.salesService.executeOutputAction(endpoint, this.startDate, this.endDate, this.criteria, this.territory)
      .subscribe({
        next: (blob: Blob) => {
          this.spinner.hide();
          this.cdr.detectChanges();
          
          const url = window.URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = `${endpoint}_${this.startDate}.xlsx`;
          document.body.appendChild(a);
          a.click();
          document.body.removeChild(a);
          window.URL.revokeObjectURL(url);
          this.toastr.success(`${title} downloaded successfully.`, 'Success');
        },
        error: (err: any) => {
          this.spinner.hide();
          this.cdr.detectChanges();
          Swal.fire('Error', `Failed to export ${title}: ` + err.message, 'error');
        }
      });
      
    this.subscriptions.add(sub);
  }

  // Data table methods
  closeDataTable(): void {
    this.showDataTable = false;
    this.tableData = [];
    this.filteredData = [];
    this.paginatedData = [];
    this.clearFilters();
    this.cdr.detectChanges();
  }

  get minShowing(): number {
    return Math.min(this.currentPage * this.pageSize, this.filteredData.length);
  }

  applyFilters(): void {
    this.filteredData = this.tableData.filter(row => {
      let isMatch = true;
      for (const key in this.columnFilters) {
        if (this.columnFilters[key]) {
          const rowValue = (row as any)[key];
          const filterValue = this.columnFilters[key].toLowerCase();
          if (rowValue === null || rowValue === undefined || !String(rowValue).toLowerCase().includes(filterValue)) {
            isMatch = false;
            break;
          }
        }
      }
      return isMatch;
    });
    this.currentPage = 1;
    this.updatePagination();
    this.cdr.detectChanges();
  }

  updatePagination(): void {
    this.totalPages = Math.ceil(this.filteredData.length / this.pageSize) || 1;
    const startIndex = (this.currentPage - 1) * this.pageSize;
    const endIndex = startIndex + this.pageSize;
    this.paginatedData = this.filteredData.slice(startIndex, endIndex);
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.updatePagination();
      this.cdr.detectChanges();
    }
  }

  prevPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.updatePagination();
      this.cdr.detectChanges();
    }
  }

  clearFilters(): void {
    this.columnFilters = {};
    this.filteredData = [...this.tableData];
    this.currentPage = 1;
    this.updatePagination();
    this.cdr.detectChanges();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    // Close any open filter dropdowns when clicking outside
    this.activeFilterColumn = null;
    this.cdr.detectChanges();
  }

  toggleFilterDropdown(columnName: string, event: MouseEvent): void {
    event.stopPropagation();
    if (this.activeFilterColumn === columnName) {
      this.activeFilterColumn = null;
    } else {
      this.activeFilterColumn = columnName;
    }
    this.cdr.detectChanges();
  }

  clearColumnFilter(columnName: string): void {
    this.columnFilters[columnName] = '';
    this.applyFilters();
    this.activeFilterColumn = null;
    this.cdr.detectChanges();
  }

  sortColumn(columnName: string, direction: 'asc' | 'desc'): void {
    this.sortColumnName = columnName;
    this.sortDirection = direction;
    
    this.tableData.sort((a, b) => {
      let valA = a[columnName];
      let valB = b[columnName];
      
      if (valA === null || valA === undefined) valA = '';
      if (valB === null || valB === undefined) valB = '';
      
      if (valA instanceof Date && valB instanceof Date) {
        return direction === 'asc' ? valA.getTime() - valB.getTime() : valB.getTime() - valA.getTime();
      }
      
      const isDateA = !isNaN(Date.parse(valA)) && isNaN(Number(valA));
      const isDateB = !isNaN(Date.parse(valB)) && isNaN(Number(valB));
      if (isDateA && isDateB) {
        const dateA = new Date(valA);
        const dateB = new Date(valB);
        return direction === 'asc' ? dateA.getTime() - dateB.getTime() : dateB.getTime() - dateA.getTime();
      }
      
      const numA = Number(valA);
      const numB = Number(valB);
      if (!isNaN(numA) && !isNaN(numB)) {
        return direction === 'asc' ? numA - numB : numB - numA;
      }
      
      const strA = String(valA).toLowerCase();
      const strB = String(valB).toLowerCase();
      
      if (strA < strB) return direction === 'asc' ? -1 : 1;
      if (strA > strB) return direction === 'asc' ? 1 : -1;
      return 0;
    });
    
    this.applyFilters();
    this.activeFilterColumn = null;
    this.cdr.detectChanges();
  }

  trackByInvoice(index: number, item: SalesActivationRow): string {
    return item.Invoice10;
  }
  
  updateRow(originalRow: SalesActivationRow): void {
    // Construct a minimal payload containing only keys and editable fields
    const payload = {
      Invoice10: originalRow.Invoice10,
      TransactionNo: originalRow.TransactionNo,
      BVInvoiceLine: (originalRow as any).BVInvoiceLine !== undefined ? Number((originalRow as any).BVInvoiceLine) : null,
      CustTerritory: originalRow.CustTerritory,
      AdjustmentType: originalRow.AdjustmentType,
      Fee: originalRow.Fee !== null && originalRow.Fee !== undefined && (originalRow.Fee as any) !== '' ? Number(originalRow.Fee) : null,
      Supress: ((originalRow.Supress as any) === true || (originalRow.Supress as any === -1) || (originalRow.Supress as any) === 'true' || (originalRow.Supress as any) === 1)
    };

    this.spinner.show();
    const sub = this.salesService.updateRow(payload).subscribe({  
      next: () => {
        this.spinner.hide();
        this.toastr.success('Row updated successfully.', 'Success');
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        this.spinner.hide();
        
        let errorMsg = err.message;
        if (err.error) {
          if (err.error.errors) {
            // ASP.NET Core validation errors
            errorMsg = JSON.stringify(err.error.errors, null, 2);
          } else if (err.error.title) {
            errorMsg = err.error.title;
          } else if (typeof err.error === 'string') {
            errorMsg = err.error;
          }
        }
        
        Swal.fire('Error', 'Failed to update row: \n' + errorMsg, 'error');
        this.cdr.detectChanges();
      }
    });
    this.subscriptions.add(sub);
  }

  exportToExcel(): void {
    if (this.filteredData.length === 0) {
      this.toastr.warning('No data to export.', 'Warning');
      return;
    }
    
    // Convert filtered data to Excel
    this.spinner.show();
    const sub = this.salesService.exportFilteredData(this.filteredData, this.currentTableTitle)
      .subscribe({
        next: (blob: Blob) => {
          this.spinner.hide();
          const url = window.URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = `${this.currentTableTitle}_Filtered_${new Date().toISOString().split('T')[0]}.xlsx`;
          document.body.appendChild(a);
          a.click();
          document.body.removeChild(a);
          window.URL.revokeObjectURL(url);
          this.toastr.success('Filtered data exported successfully.', 'Success');
          this.cdr.detectChanges();
        },
        error: (err: any) => {
          this.spinner.hide();
          this.cdr.detectChanges();
          Swal.fire('Error', 'Failed to export filtered data: ' + err.message, 'error');
        }
      });
    this.subscriptions.add(sub);
  }

  // Button actions - VIEW actions open data table
  anyTerritoryEdit() { this.handleViewAction('any-territory-edit', 'Any Territory - Edit'); }
  allExceptCorporateEdit() { this.handleViewAction('all-except-corporate-edit', 'All Except Corporate EDIT'); }
  
  invoicesMissingSummary() { this.handleViewAction('invoices-missing-summary', 'Invoices Missing Summary'); }
  invoicesMissingDetails() { this.handleViewAction('invoices-missing-details', 'Invoice Missing Details'); }
  exceptionReport() { this.handleViewAction('exception-report', 'Exception Report'); }
  fffLossOnHardware() { this.handleViewAction('fff-loss-on-hardware', 'FFF Loss on Hardware'); }

  rogersEditAll() { this.handleViewAction('rogers-edit-all', 'Rogers - Edit All'); }
  rogersWithProvince() { this.handleViewAction('rogers-with-province', 'Rogers With Province'); }
  
  editViewAllGbmNdsEdit() { this.handleViewAction('edit-view-all-gbm-nds', 'Edit / View ALL GBM NDS Edit'); }
  editAllGbm() { this.handleViewAction('edit-all-gbm', 'Edit ALL GBM'); }
  editAllNds() { this.handleViewAction('edit-all-nds', 'Edit ALL NDS'); }
  
  editAllRdl() { this.handleViewAction('edit-all-rdl', 'Edit ALL RDL'); }

  // Button actions - OUTPUT actions download Excel
  dumpAllInDateRangeToExcel() { this.handleOutputAction('dump-all', 'Dump All In Date Range to Excel'); }
  outputRogersHupsMisc() { this.handleOutputAction('output-rogers-hups-misc', 'OUTPUT Rogers HUPS & Misc'); }
  outputRogersAcquisitions() { this.handleOutputAction('output-rogers-acquisitions', 'OUTPUT Rogers Acquisitions'); }
  outputRogersReturns() { this.handleOutputAction('output-rogers-returns', 'OUTPUT Rogers Returns'); }
  outputGbmAndNds() { this.handleOutputAction('output-gbm-nds', 'OUTPUT GBM and NDS'); }
  gbmOutput() { this.handleOutputAction('gbm-output', 'GBM Output'); }
  ndsOutput() { this.handleOutputAction('nds-output', 'NDS Output'); }
  anyTerritoryOutput() { this.handleOutputAction('any-territory-output', 'Any Territory - Output'); }
  rdlOutput() { this.handleOutputAction('rdl-output', 'RDL Output'); }
  outputCorporate() { this.handleOutputAction('output-corporate', 'OUTPUT Corporate'); }

  close() {
    Swal.fire('Close', 'Form closed.', 'info');
  }
}
