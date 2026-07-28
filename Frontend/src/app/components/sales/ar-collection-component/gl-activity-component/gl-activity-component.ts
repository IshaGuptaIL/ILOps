import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ArCollectionService, GLAllowedAccount, GLActivityRow } from '../ar-collection.service';
import { SpinnerService } from '../../../shared/spinner/spinner-service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-gl-activity',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './gl-activity-component.html',
  styleUrls: ['./gl-activity-component.css']
})
export class GlActivityComponent implements OnInit {
  allowedAccounts: GLAllowedAccount[] = [];
  selectedAccount: string = '';
  startDate: string = '';
  endDate: string = '';
  todayStr: string = '';
  
  // Data list
  activityRows: GLActivityRow[] = [];
  
  // Sum properties
  totalDebit: number = 0;
  totalCredit: number = 0;
  totalBalance: number = 0;

  // Sorting
  sortColumn: string = '';
  sortAscending: boolean = true;

  // Column filtering
  filters = {
    accountNo: '',
    accountName: '',
    date: '',
    transNo: '',
    source: '',
    user: '',
    glMemo: '',
    type: '',
    entity: '',
    document: '',
    debitAmt: '',
    creditAmt: '',
    balance: '',
    webOrderID: '',
    postDate: ''
  };

  constructor(
    private arService: ArCollectionService,
    private spinner: SpinnerService,
    private toastr: ToastrService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const today = new Date();
    this.todayStr = this.formatDate(today);
    this.endDate = this.todayStr;
    // Default to Jan 1st of current year
    this.startDate = this.formatDate(new Date(today.getFullYear(), 0, 1));

    this.loadAllowedAccounts();
    this.cdr.detectChanges();
  }

  loadAllowedAccounts(): void {
    this.spinner.show();
    this.arService.getGLAllowedAccounts().subscribe({
      next: (accounts) => {
        this.allowedAccounts = accounts;
        if (accounts.length > 0) {
          this.selectedAccount = accounts[0].account;
        }
        this.spinner.hide();
        this.cdr.detectChanges();
      },
      error: () => {
        this.spinner.hide();
        this.toastr.error('Failed to load allowed GL accounts');
        this.cdr.detectChanges();
      }
    });
    this.cdr.detectChanges();
  }

  queryGLActivity(): void {
    if (!this.selectedAccount) {
      this.toastr.warning('Please select a GL Account.');
      this.cdr.detectChanges();
      return;
    }

    if (!this.validateDates()) {
      this.cdr.detectChanges();
      return;
    }

    this.spinner.show();
    this.arService.getGLActivity(this.selectedAccount, this.startDate, this.endDate).subscribe({
      next: (data) => {
        this.activityRows = data;
        this.calculateTotals();
        this.spinner.hide();
        this.cdr.detectChanges();
      },
      error: () => {
        this.spinner.hide();
        this.toastr.error('Failed to query GL Activity');
        this.cdr.detectChanges();
      }
    });
    this.cdr.detectChanges();
  }

  exportExcel(): void {
    if (!this.selectedAccount) {
      this.toastr.warning('Please select a GL Account.');
      this.cdr.detectChanges();
      return;
    }

    if (!this.validateDates()) {
      this.cdr.detectChanges();
      return;
    }

    this.spinner.show();
    this.arService.exportGLActivity(this.selectedAccount, this.startDate, this.endDate).subscribe({
      next: (blob) => {
        this.spinner.hide();
        const filename = `GLActivity-${this.selectedAccount} ${this.startDate} to ${this.endDate}.xlsx`;
        this.downloadBlob(blob, filename);
        this.toastr.success('GL Activity Excel file exported successfully.');
        this.cdr.detectChanges();
      },
      error: () => {
        this.spinner.hide();
        this.toastr.error('Failed to export GL Activity Excel');
        this.cdr.detectChanges();
      }
    });
    this.cdr.detectChanges();
  }

  private validateDates(): boolean {
    if (!this.startDate || !this.endDate) {
      this.toastr.warning('Please select both Start Date and End Date.');
      return false;
    }
    const start = new Date(this.startDate);
    const end = new Date(this.endDate);
    const today = new Date();
    start.setHours(0,0,0,0);
    end.setHours(0,0,0,0);
    today.setHours(0,0,0,0);

    if (end > today) {
      this.toastr.warning('End Date cannot be in the future.');
      return false;
    }
    if (start > end) {
      this.toastr.warning('Start Date cannot be after End Date.');
      return false;
    }
    return true;
  }

  calculateTotals(): void {
    const rows = this.filteredRows;
    this.totalDebit = rows.reduce((sum, row) => sum + (row.debitAmt || 0), 0);
    this.totalCredit = rows.reduce((sum, row) => sum + (row.creditAmt || 0), 0);
    this.totalBalance = rows.reduce((sum, row) => sum + (row.balance || 0), 0);
    this.cdr.detectChanges();
  }

  get filteredRows(): GLActivityRow[] {
    let result = [...this.activityRows];

    // Filter by columns
    const keys = Object.keys(this.filters) as (keyof typeof this.filters)[];
    for (const key of keys) {
      const val = this.filters[key]?.toLowerCase().trim();
      if (val) {
        result = result.filter(row => {
          const rowVal = row[key as keyof GLActivityRow];
          if (rowVal === undefined || rowVal === null) return false;
          return String(rowVal).toLowerCase().includes(val);
        });
      }
    }

    // Sort
    if (this.sortColumn) {
      const col = this.sortColumn as keyof GLActivityRow;
      const asc = this.sortAscending;
      result.sort((a, b) => {
        const valA = a[col];
        const valB = b[col];

        if (valA === undefined || valA === null) return asc ? 1 : -1;
        if (valB === undefined || valB === null) return asc ? -1 : 1;

        if (typeof valA === 'number' && typeof valB === 'number') {
          return asc ? valA - valB : valB - valA;
        }

        const strA = String(valA).toLowerCase();
        const strB = String(valB).toLowerCase();
        return asc ? strA.localeCompare(strB) : strB.localeCompare(strA);
      });
    }

    return result;
  }

  onFilterInput(): void {
    this.calculateTotals();
    this.cdr.detectChanges();
  }

  setSort(column: string): void {
    if (this.sortColumn === column) {
      this.sortAscending = !this.sortAscending;
    } else {
      this.sortColumn = column;
      this.sortAscending = true;
    }
    this.calculateTotals();
    this.cdr.detectChanges();
  }

  clearFilters(): void {
    const keys = Object.keys(this.filters) as (keyof typeof this.filters)[];
    for (const key of keys) {
      this.filters[key] = '';
    }
    this.sortColumn = '';
    this.sortAscending = true;
    this.calculateTotals();
    this.cdr.detectChanges();
  }

  formatDate(date: Date): string {
    const yyyy = date.getFullYear();
    const mm = String(date.getMonth() + 1).padStart(2, '0');
    const dd = String(date.getDate()).padStart(2, '0');
    return `${yyyy}-${mm}-${dd}`;
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
