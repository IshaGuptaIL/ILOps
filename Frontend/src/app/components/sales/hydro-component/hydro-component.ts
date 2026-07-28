import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HydroService, PostPaymentRequest, GenerateMemoRequest } from './hydro.service';
import { SpinnerService } from '../../shared/spinner/spinner-service';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-hydro-component',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './hydro-component.html',
  styleUrl: './hydro-component.css',
})
export class HydroComponent implements OnInit {
  // Post Payment Section
  postInvoiceNo: string = '';

  // Generate Memo Section
  memoInvoiceNo: string = '';
  originalAmount: number | null = null;
  webOrderID: string = '';
  cardType: string = '';
  generatedMemo: string = '';

  cardTypes: string[] = ['M/C', 'Visa', 'Amex'];

  constructor(
    private hydroService: HydroService,
    private spinner: SpinnerService,
    private toastr: ToastrService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.resetForm();
  }

  resetForm(): void {
    this.postInvoiceNo = '';
    this.memoInvoiceNo = '';
    this.originalAmount = null;
    this.webOrderID = '';
    this.cardType = '';
    this.generatedMemo = '';
    this.cdr.detectChanges();
  }

  postPayment(): void {
    if (!this.postInvoiceNo || !this.postInvoiceNo.trim()) {
      this.toastr.warning('Please enter an Invoice Number.');
      return;
    }

    this.spinner.show();
    const request: PostPaymentRequest = {
      invoiceNo: this.postInvoiceNo.trim()
    };

    this.hydroService.postPayment(request).subscribe({
      next: (response) => {
        this.spinner.hide();
        if (response.success) {
          this.toastr.success('Payment posted successfully.');
          Swal.fire('Success', response.message, 'success');
          this.postInvoiceNo = '';
        } else {
          this.toastr.error('Payment post failed.');
          Swal.fire('Error', response.message, 'error');
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.spinner.hide();
        this.toastr.error('An error occurred while posting payment.');
        Swal.fire('Error', err.error?.message || 'Server error occurred.', 'error');
        this.cdr.detectChanges();
      }
    });
  }

  generateMemo(): void {
    if (!this.memoInvoiceNo || !this.memoInvoiceNo.trim()) {
      this.toastr.warning('Please enter an Invoice Number.');
      return;
    }
    if (this.originalAmount === null || this.originalAmount <= 0) {
      this.toastr.warning('Please enter a valid Original Invoice Amount.');
      return;
    }
    if (!this.webOrderID || !this.webOrderID.trim()) {
      this.toastr.warning('Please enter a Web Order ID.');
      return;
    }
    if (!this.cardType) {
      this.toastr.warning('Please select a Card Type.');
      return;
    }

    this.spinner.show();
    const request: GenerateMemoRequest = {
      invoiceNo: this.memoInvoiceNo.trim(),
      originalAmount: this.originalAmount,
      webOrderID: this.webOrderID.trim(),
      cardType: this.cardType
    };

    this.hydroService.generateMemo(request).subscribe({
      next: (response) => {
        this.spinner.hide();
        if (response.success) {
          this.toastr.success('Information Verified.');
          this.generatedMemo = response.generatedMemo || '';
          Swal.fire('Verified', response.message, 'success');
        } else {
          this.toastr.error('Verification failed.');
          Swal.fire('Error', response.message, 'error');
          this.generatedMemo = '';
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.spinner.hide();
        this.toastr.error('An error occurred during verification.');
        Swal.fire('Error', err.error?.message || 'Server error occurred.', 'error');
        this.generatedMemo = '';
        this.cdr.detectChanges();
      }
    });
  }
}
