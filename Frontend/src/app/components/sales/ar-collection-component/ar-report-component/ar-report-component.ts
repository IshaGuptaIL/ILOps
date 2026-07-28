import { Component, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ArCollectionService } from '../ar-collection.service';
import { SpinnerService } from '../../../shared/spinner/spinner-service';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-ar-report-component',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './ar-report-component.html',
  styleUrl: './ar-report-component.css',
})
export class ArReportComponent {
  reportForm: FormGroup;
  arMasterForm: FormGroup;
  currentDate = new Date();
  maxDateString: string;

  // Modal Properties
  isModalOpen = false;
  modalTitle = '';
  modalColumns: string[] = [];
  modalData: any[] = [];

  // Pagination Properties
  currentPage = 1;
  pageSize = 100;

  get paginatedModalData() {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.modalData.slice(start, start + this.pageSize);
  }

  get totalPages() {
    return Math.ceil(this.modalData.length / this.pageSize) || 1;
  }

  get startIndex() {
    return this.modalData.length === 0 ? 0 : (this.currentPage - 1) * this.pageSize + 1;
  }

  get endIndex() {
    return Math.min(this.currentPage * this.pageSize, this.modalData.length);
  }

  nextPage() {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.cdr.detectChanges();
    }
  }

  prevPage() {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.cdr.detectChanges();
    }
  }

  constructor(
    private fb: FormBuilder,
    private arService: ArCollectionService,
    private spinner: SpinnerService,
    private cdr: ChangeDetectorRef
  ) {
    this.maxDateString = this.formatDate(this.currentDate);
    const firstDayOfMonth = new Date(this.currentDate.getFullYear(), this.currentDate.getMonth(), 1);

    this.reportForm = this.fb.group({
      lastReportDate: [this.formatDate(firstDayOfMonth), Validators.required],
      startDate: ['', Validators.required],
      endDate: ['', Validators.required],
    });

    this.arMasterForm = this.fb.group({
      agingDate: [this.maxDateString, Validators.required],
    });
  }

  formatDate(date: Date): string {
    const d = new Date(date);
    let month = '' + (d.getMonth() + 1);
    let day = '' + d.getDate();
    const year = d.getFullYear();

    if (month.length < 2) month = '0' + month;
    if (day.length < 2) day = '0' + day;

    return [year, month, day].join('-');
  }

  openModal(title: string, data: any[]) {
    try {
      this.modalTitle = title;
      this.modalData = data || [];
      this.currentPage = 1;
      if (this.modalData.length > 0) {
        this.modalColumns = Object.keys(data[0]);
      } else {
        this.modalColumns = [];
      }
      this.isModalOpen = true;
    } catch (e) {
      console.error(e);
    } finally {
      this.cdr.detectChanges();
    }
  }

  closeModal() {
    try {
      this.isModalOpen = false;
      this.modalData = [];
      this.modalColumns = [];
    } catch (e) {
      console.error(e);
    } finally {
      this.cdr.detectChanges();
    }
  }

  // --- Aging Of Payments Collected Methods ---
  
  generateAgingData() {
    try {
      if (this.reportForm.invalid) {
        this.reportForm.markAllAsTouched();
        this.cdr.detectChanges();
        return;
      }
      const val = this.reportForm.value;
      if (new Date(val.startDate) > new Date(val.endDate)) {
        Swal.fire('Error', 'Start Date cannot be greater than End Date', 'error');
        this.cdr.detectChanges();
        return;
      }

      this.spinner.show();
      this.arService.generateAgingData({
        lastReportDate: val.lastReportDate,
        startDate: val.startDate,
        endDate: val.endDate
      }).subscribe({
        next: (res) => {
          this.spinner.hide();
          if (res) Swal.fire('Success', 'Aging Data generated successfully', 'success');
          else Swal.fire('Error', 'Failed to generate data', 'error');
          this.cdr.detectChanges();
        },
        error: (err) => {
          this.spinner.hide();
          Swal.fire('Error', 'An error occurred', 'error');
          this.cdr.detectChanges();
        }
      });
    } catch (error) {
      this.spinner.hide();
      Swal.fire('Error', 'An unexpected error occurred', 'error');
      this.cdr.detectChanges();
    }
  }

  viewAgedSummary() {
    try {
      this.spinner.show();
      this.arService.getAgedSummaryData().subscribe({
        next: (data) => {
          this.spinner.hide();
          this.openModal('Aged Summary By Channel', data);
        },
        error: (err) => {
          this.spinner.hide();
          Swal.fire('Error', 'Failed to fetch Aged Summary data', 'error');
          this.cdr.detectChanges();
        }
      });
    } catch (error) {
      this.spinner.hide();
      Swal.fire('Error', 'An unexpected error occurred', 'error');
      this.cdr.detectChanges();
    }
  }

  viewPaymentDetails() {
    try {
      this.spinner.show();
      this.arService.getPaymentDetailsData().subscribe({
        next: (data) => {
          this.spinner.hide();
          this.openModal('Payment Details', data);
        },
        error: (err) => {
          this.spinner.hide();
          Swal.fire('Error', 'Failed to fetch Payment Details data', 'error');
          this.cdr.detectChanges();
        }
      });
    } catch (error) {
      this.spinner.hide();
      Swal.fire('Error', 'An unexpected error occurred', 'error');
      this.cdr.detectChanges();
    }
  }

  outputAgedSummaryXlsx() {
    try {
      this.spinner.show();
      this.arService.exportAgedSummary().subscribe({
        next: (blob) => {
          this.spinner.hide();
          const url = window.URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = 'PaymentsReceivedByChannel.xlsx';
          a.click();
          window.URL.revokeObjectURL(url);
          this.cdr.detectChanges();
        },
        error: (err) => {
          this.spinner.hide();
          Swal.fire('Error', 'Failed to export file', 'error');
          this.cdr.detectChanges();
        }
      });
    } catch (error) {
      this.spinner.hide();
      Swal.fire('Error', 'An unexpected error occurred', 'error');
      this.cdr.detectChanges();
    }
  }

  // --- AR Master Methods ---

  generateARMasterData() {
    try {
      if (this.arMasterForm.invalid) {
        this.arMasterForm.markAllAsTouched();
        this.cdr.detectChanges();
        return;
      }
      const val = this.arMasterForm.value;

      this.spinner.show();
      this.arService.generateARMasterData(val.agingDate).subscribe({
        next: (res) => {
          this.spinner.hide();
          if (res) Swal.fire('Success', 'AR Master Data generated successfully', 'success');
          else Swal.fire('Error', 'Failed to generate data', 'error');
          this.cdr.detectChanges();
        },
        error: (err) => {
          this.spinner.hide();
          Swal.fire('Error', 'An error occurred', 'error');
          this.cdr.detectChanges();
        }
      });
    } catch (error) {
      this.spinner.hide();
      Swal.fire('Error', 'An unexpected error occurred', 'error');
      this.cdr.detectChanges();
    }
  }

  viewARMaster() {
    try {
      this.spinner.show();
      this.arService.getARMasterData().subscribe({
        next: (data) => {
          this.spinner.hide();
          this.openModal('AR Master', data);
        },
        error: (err) => {
          this.spinner.hide();
          Swal.fire('Error', 'Failed to fetch AR Master data', 'error');
          this.cdr.detectChanges();
        }
      });
    } catch (error) {
      this.spinner.hide();
      Swal.fire('Error', 'An unexpected error occurred', 'error');
      this.cdr.detectChanges();
    }
  }

  outputARMasterXlsx() {
    try {
      this.spinner.show();
      this.arService.exportARMaster().subscribe({
        next: (blob) => {
          this.spinner.hide();
          const url = window.URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = 'AR-Master.xlsx';
          a.click();
          window.URL.revokeObjectURL(url);
          this.cdr.detectChanges();
        },
        error: (err) => {
          this.spinner.hide();
          Swal.fire('Error', 'Failed to export file', 'error');
          this.cdr.detectChanges();
        }
      });
    } catch (error) {
      this.spinner.hide();
      Swal.fire('Error', 'An unexpected error occurred', 'error');
      this.cdr.detectChanges();
    }
  }

  outputARMasterAllXlsx() {
    try {
      this.spinner.show();
      this.arService.exportARMasterAll().subscribe({
        next: (blob) => {
          this.spinner.hide();
          const url = window.URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = 'AR-Master-ALL.xlsx';
          a.click();
          window.URL.revokeObjectURL(url);
          this.cdr.detectChanges();
        },
        error: (err) => {
          this.spinner.hide();
          Swal.fire('Error', 'Failed to export file', 'error');
          this.cdr.detectChanges();
        }
      });
    } catch (error) {
      this.spinner.hide();
      Swal.fire('Error', 'An unexpected error occurred', 'error');
      this.cdr.detectChanges();
    }
  }

  outputARMasterSummaryXlsx() {
    try {
      this.spinner.show();
      this.arService.exportARMasterSummary().subscribe({
        next: (blob) => {
          this.spinner.hide();
          const url = window.URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = 'AR Summar.xlsx';
          a.click();
          window.URL.revokeObjectURL(url);
          this.cdr.detectChanges();
        },
        error: (err) => {
          this.spinner.hide();
          Swal.fire('Error', 'Failed to export file', 'error');
          this.cdr.detectChanges();
        }
      });
    } catch (error) {
      this.spinner.hide();
      Swal.fire('Error', 'An unexpected error occurred', 'error');
      this.cdr.detectChanges();
    }
  }
}
