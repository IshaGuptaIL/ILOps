import { CommonModule } from '@angular/common';
import { Component, ChangeDetectorRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';
import { SpinnerService } from '../../shared/spinner/spinner-service';
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
  receipt: any[] = [];
  rogersList: any[] = [];
  isLoading: boolean = false;

  constructor(
    private imeiService: ImeiService,
    private toastr: ToastrService,
    private spinner: SpinnerService,
    private cdr: ChangeDetectorRef 
  ) {}

  searchIMEI(): void {
    if (!this.imei.trim()) {
      this.toastr.warning('Please enter an IMEI number', 'Input Required');
      return;
    }

    this.isLoading = true;
    this.spinner.show();
    this.receipt = [];
    this.rogersList = [];

    this.imeiService.findByImei(this.imei.trim()).subscribe({
      next: (res: any) => {
        console.log('API Raw Response:', res);

        if (res && res.success && res.result) {
          this.receipt = Array.isArray(res.result) ? res.result : [res.result];

          if (res.result.bvReceiptNo) {
            this.loadRogersInvoices(res.result.bvReceiptNo);
          } else {
            this.isLoading = false;
            this.spinner.hide();
          }
        } else {
          this.isLoading = false;
          this.spinner.hide();
          this.toastr.info('IMEI not found', 'Not Found');
        }
        this.cdr.detectChanges(); 
      },
      error: (err) => {
        this.isLoading = false;
        this.spinner.hide();
        this.toastr.error('API connection failed', 'Error');
        this.cdr.detectChanges();
      }
    });
  }

  loadRogersInvoices(bvReceiptNo: string): void {
    this.imeiService.getRogersInvoices(bvReceiptNo).subscribe({
      next: (res: any) => {
        console.log('Invoices Raw Response:', res);
        this.rogersList = res?.result ?? [];
        this.isLoading = false;
        this.spinner.hide();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isLoading = false;
        this.spinner.hide();
        this.cdr.detectChanges();
      }
    });
  }

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