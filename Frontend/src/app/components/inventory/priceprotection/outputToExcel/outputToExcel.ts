import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PriceProtectionService } from '../priceprotection.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-output-to-excel',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './outputToExcel.html',
  styleUrls: ['./outputToExcel.css']
})
export class OutputToExcelComponent implements OnInit {
  claimsToCredits: any[] = [];
  isExporting: boolean = false;
  sortAsc: boolean = true;

  constructor(
    private ppService: PriceProtectionService,
    private toastr: ToastrService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadClaimsToCredits();
  }

  loadClaimsToCredits(): void {
    this.ppService.getClaimsToCreditsData().subscribe({
      next: (res) => {
        if (res && res.success) {
          this.claimsToCredits = res.result || [];
          this.cdr.detectChanges();
        }
      },
      error: (err) => {
        console.error('Error loading claims matching credits:', err);
        this.toastr.error('Failed to load claims matching credits data.', 'Error');
      }
    });
  }

  outputFilteredRecords(): void {
    this.isExporting = true;
    this.cdr.detectChanges();
    this.ppService.exportClaimsToCredits().subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = 'PPClaimsToCredits.xlsx';
        link.click();
        window.URL.revokeObjectURL(url);
        this.toastr.success('Claims to Credits report exported successfully!', 'Export Complete');
        this.isExporting = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.toastr.error('Failed to export Claims to Credits.', 'Error');
        this.isExporting = false;
        this.cdr.detectChanges();
      }
    });
  }

  sortBy(key: string): void {
    this.sortAsc = !this.sortAsc;
    this.claimsToCredits.sort((a, b) => {
      const val1 = a[key];
      const val2 = b[key];
      if (val1 == null && val2 == null) return 0;
      if (val1 == null) return this.sortAsc ? 1 : -1;
      if (val2 == null) return this.sortAsc ? -1 : 1;

      let res = 0;
      if (typeof val1 === 'string' && typeof val2 === 'string') {
        res = val1.localeCompare(val2);
      } else {
        res = val1 < val2 ? -1 : (val1 > val2 ? 1 : 0);
      }
      return this.sortAsc ? res : -res;
    });
  }
}
