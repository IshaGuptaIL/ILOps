import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PriceProtectionService } from '../priceprotection.service';

@Component({
  selector: 'app-imei-search',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './imeiSearch.html',
  styleUrls: ['./imeiSearch.css']
})
export class ImeiSearchComponent implements OnInit {
  searchImeiText: string = '';
  claims: any[] = [];
  credits: any[] = [];
  overpayments: any[] = [];
  isLoading: boolean = false;
  hasSearched: boolean = false;

  // Sorting helper states
  claimsSortAsc: boolean = true;
  claimsSortKey: string = '';
  creditsSortAsc: boolean = true;
  creditsSortKey: string = '';
  overpaymentsSortAsc: boolean = true;
  overpaymentsSortKey: string = '';

  // Excel Filter properties
  priceBeforeDropFilters: { value: number; checked: boolean }[] = [];
  priceAfterDropFilters: { value: number; checked: boolean }[] = [];
  activeDropdown: string | null = null;

  constructor(private ppService: PriceProtectionService, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.onSearch();
  }

  onSearch(): void {
    this.isLoading = true;
    this.hasSearched = true;

    const searchTerm = this.searchImeiText ? this.searchImeiText.trim() : '';

    this.ppService.searchImei(searchTerm).subscribe({
      next: (res) => {
        if (res && res.success) {
          const data = res.result;
          this.claims = data.claims || [];
          this.credits = data.credits || [];
          this.overpayments = data.overpayments || [];
          this.initializeFilters();
        } else {
          this.claims = [];
          this.credits = [];
          this.overpayments = [];
          this.priceBeforeDropFilters = [];
          this.priceAfterDropFilters = [];
        }
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error fetching IMEI search details:', err);
        this.claims = [];
        this.credits = [];
        this.overpayments = [];
        this.priceBeforeDropFilters = [];
        this.priceAfterDropFilters = [];
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  sortClaims(key: string, direction?: 'asc' | 'desc'): void {
    if (direction) {
      this.claimsSortAsc = direction === 'asc';
      this.claimsSortKey = key;
    } else {
      if (this.claimsSortKey === key) {
        this.claimsSortAsc = !this.claimsSortAsc;
      } else {
        this.claimsSortKey = key;
        this.claimsSortAsc = true;
      }
    }
    this.claims.sort((a, b) => this.compareValues(a[key], b[key], this.claimsSortAsc));
    this.cdr.detectChanges();
  }

  // Excel Filter helpers
  initializeFilters(): void {
    // Price Before Drop
    const uniqueBefore = Array.from(new Set(this.claims.map(c => c.priceBeforeDrop || 0)))
      .sort((a, b) => a - b);
    this.priceBeforeDropFilters = uniqueBefore.map(val => ({ value: val, checked: true }));

    // Price After Drop
    const uniqueAfter = Array.from(new Set(this.claims.map(c => c.priceAfterDrop || 0)))
      .sort((a, b) => a - b);
    this.priceAfterDropFilters = uniqueAfter.map(val => ({ value: val, checked: true }));
  }

  toggleDropdown(col: string): void {
    if (this.activeDropdown === col) {
      this.activeDropdown = null;
    } else {
      this.activeDropdown = col;
    }
    this.cdr.detectChanges();
  }

  isAllSelected(col: string): boolean {
    if (col === 'priceBeforeDrop') {
      return this.priceBeforeDropFilters.every(f => f.checked);
    } else if (col === 'priceAfterDrop') {
      return this.priceAfterDropFilters.every(f => f.checked);
    }
    return false;
  }

  toggleSelectAll(col: string, event: any): void {
    const checked = event.target.checked;
    if (col === 'priceBeforeDrop') {
      this.priceBeforeDropFilters.forEach(f => f.checked = checked);
    } else if (col === 'priceAfterDrop') {
      this.priceAfterDropFilters.forEach(f => f.checked = checked);
    }
    this.cdr.detectChanges();
  }

  onFilterChange(): void {
    this.cdr.detectChanges();
  }

  resetFilter(col: string): void {
    if (col === 'priceBeforeDrop') {
      this.priceBeforeDropFilters.forEach(f => f.checked = true);
    } else if (col === 'priceAfterDrop') {
      this.priceAfterDropFilters.forEach(f => f.checked = true);
    }
    this.activeDropdown = null;
    this.cdr.detectChanges();
  }

  get filteredClaims(): any[] {
    return this.claims.filter(claim => {
      // Check priceBeforeDrop
      const beforeFilter = this.priceBeforeDropFilters.find(f => f.value === (claim.priceBeforeDrop || 0));
      if (beforeFilter && !beforeFilter.checked) {
        return false;
      }
      // Check priceAfterDrop
      const afterFilter = this.priceAfterDropFilters.find(f => f.value === (claim.priceAfterDrop || 0));
      if (afterFilter && !afterFilter.checked) {
        return false;
      }
      return true;
    });
  }

  sortCredits(key: string): void {
    if (this.creditsSortKey === key) {
      this.creditsSortAsc = !this.creditsSortAsc;
    } else {
      this.creditsSortKey = key;
      this.creditsSortAsc = true;
    }
    this.credits.sort((a, b) => this.compareValues(a[key], b[key], this.creditsSortAsc));
    this.cdr.detectChanges();
  }

  sortOverpayments(key: string): void {
    if (this.overpaymentsSortKey === key) {
      this.overpaymentsSortAsc = !this.overpaymentsSortAsc;
    } else {
      this.overpaymentsSortKey = key;
      this.overpaymentsSortAsc = true;
    }
    this.overpayments.sort((a, b) => this.compareValues(a[key], b[key], this.overpaymentsSortAsc));
    this.cdr.detectChanges();
  }

  private compareValues(val1: any, val2: any, asc: boolean): number {
    if (val1 == null && val2 == null) return 0;
    if (val1 == null) return asc ? 1 : -1;
    if (val2 == null) return asc ? -1 : 1;

    let res = 0;
    if (typeof val1 === 'string' && typeof val2 === 'string') {
      res = val1.localeCompare(val2);
    } else {
      res = val1 < val2 ? -1 : (val1 > val2 ? 1 : 0);
    }
    return asc ? res : -res;
  }
}
