import { CommonModule } from '@angular/common';
import { Component, ChangeDetectorRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import Swal from 'sweetalert2';
import { ImeiService } from '../imei-service';

@Component({
  selector: 'app-find-by-imei-component',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './find-by-imei-component.html',
  styleUrl: './find-by-imei-component.css'
})
export class FindByImeiComponent {
  imei: string = '';
  receipt: any;
  rogersList: any[] = [];
  isLoading: boolean = false;

  constructor(
    private imeiService: ImeiService,
    private cdr: ChangeDetectorRef 
  ) {}

  searchIMEI(): void {
    if (!this.imei.trim()) {
      Swal.fire('Error', 'Please enter IMEI', 'warning');
      return;
    }

    console.log('--- Starting Search for IMEI:', this.imei);
    this.isLoading = true;
    this.receipt = [];
    this.rogersList = [];

    this.imeiService.findByImei(this.imei.trim()).subscribe({
      next: (res: any) => {
        console.log('API Raw Response:', res);

        if (res && res.success && res.result) {
          // If result is an object, wrap it in []. If already array, use it.
          this.receipt = Array.isArray(res.result) ? res.result : [res.result];
          console.log('Receipt variable set to:', this.receipt);

          if (res.result.bvReceiptNo) {
            this.loadRogersInvoices(res.result.bvReceiptNo);
          } else {
            this.isLoading = false;
          }
        } else {
          this.isLoading = false;
          Swal.fire('Not Found', 'IMEI not found', 'info');
        }
        this.cdr.detectChanges(); // Force UI to wake up
      },
      error: (err) => {
        console.error('API Error:', err);
        this.isLoading = false;
        Swal.fire('Error', 'API connection failed', 'error');
        this.cdr.detectChanges();
      }
    });
  }

  loadRogersInvoices(bvReceiptNo: string): void {
    console.log('--- Fetching Invoices for:', bvReceiptNo);
    this.imeiService.getRogersInvoices(bvReceiptNo).subscribe({
      next: (res: any) => {
        console.log('Invoices Raw Response:', res);
        this.rogersList = res?.result ?? [];
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Invoice API Error:', err);
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  // CALCULATIONS
  get rogersTotal(): number {
    return this.rogersList.reduce((sum, x) => sum + ((Number(x.perUnitAmount) || 0) * (Number(x.qty) || 0)), 0);
  }

  get variance(): number {
    if (this.receipt.length === 0) return 0;
    const r = this.receipt[0];
    const receiptTotal = (Number(r.unitCost) || 0) * (Number(r.qtyReceived) || 0);
    return receiptTotal - this.rogersTotal;
  }

  get rogersCount(): number {
    return this.rogersList.length;
  }
}