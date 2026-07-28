import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PriceProtectionService } from '../priceprotection.service';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-roger-overpayments',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './rogerOverPayments.html',
  styleUrls: ['./rogerOverPayments.css']
})
export class RogerOverPaymentsComponent implements OnInit {
  importedFiles: any[] = [];
  selectedFile: string = '';
  selectedFileRecordCount: number | null = null;
  selectedFileImportDate: string | null = null;

  // File Upload states
  uploadedFile: File | null = null;
  uploadedFileName: string = '';

  isImporting: boolean = false;
  isDeleting: boolean = false;
  isExporting: boolean = false;

  constructor(
    private ppService: PriceProtectionService,
    private toastr: ToastrService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadImportedFiles();
  }

  loadImportedFiles(): void {
    this.ppService.getImportedFilesSummary().subscribe({
      next: (res) => {
        if (res && res.success) {
          this.importedFiles = res.result || [];
          this.cdr.detectChanges();
        }
      },
      error: (err) => {
        console.error('Error loading imported files summary:', err);
      }
    });
  }

  downloadTemplate(): void {
    this.ppService.downloadRogersTemplate().subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = 'RogersOverpayments_Template.xlsx';
        link.click();
        window.URL.revokeObjectURL(url);
        this.toastr.success('Template downloaded successfully!', 'Download Complete');
      },
      error: (err) => {
        console.error(err);
        this.toastr.error('Failed to download template.', 'Error');
      }
    });
  }

  onFileSelected(event: any): void {
    const file = event.target.files?.[0];
    if (file) {
      this.uploadedFile = file;
      this.uploadedFileName = file.name;
    }
  }

  importOverpayments(): void {
    if (!this.uploadedFile) {
      this.toastr.warning('Please select an Excel template to import.', 'File Missing');
      return;
    }

    Swal.fire({
      title: 'Import Rogers Overpayments?',
      text: `Are you sure you want to import data from "${this.uploadedFileName}"?`,
      icon: 'question',
      showCancelButton: true,
      confirmButtonText: 'Yes, Import',
      cancelButtonText: 'Cancel',
      confirmButtonColor: '#4b6b94',
      cancelButtonColor: '#8f8f8f'
    }).then((result) => {
      if (result.isConfirmed) {
        this.isImporting = true;

        this.ppService.importRogersOverpayments(this.uploadedFile!).subscribe({
          next: (res) => {
            if (res && res.success) {
              this.toastr.success(res.message, 'Import Successful');
              this.uploadedFile = null;
              this.uploadedFileName = '';
              this.loadImportedFiles();
            } else {
              this.toastr.error('Failed to import records.', 'Import Error');
            }
            this.isImporting = false;
            this.cdr.detectChanges();
          },
          error: (err) => {
            console.error(err);
            this.toastr.error('An error occurred during import.', 'Error');
            this.isImporting = false;
            this.cdr.detectChanges();
          }
        });
      }
    });
  }

  onSelectedFileChange(): void {
    if (!this.selectedFile) {
      this.selectedFileRecordCount = null;
      this.selectedFileImportDate = null;
      this.cdr.detectChanges();
      return;
    }

    const matchedFile = this.importedFiles.find(f => f.filename === this.selectedFile);
    if (matchedFile) {
      this.selectedFileRecordCount = matchedFile.count;
      this.selectedFileImportDate = matchedFile.importedDate;
    } else {
      this.selectedFileRecordCount = null;
      this.selectedFileImportDate = null;
    }
    this.cdr.detectChanges();
  }

  removeFileRecords(): void {
    if (!this.selectedFile) {
      this.toastr.warning('Please select an imported file first.', 'Selection Required');
      return;
    }

    Swal.fire({
      title: 'Remove Records?',
      html: `Are you sure you want to remove all imported records from:<br/><b>${this.selectedFile}</b>?`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Yes, Delete',
      cancelButtonText: 'Cancel',
      confirmButtonColor: '#d9534f',
      cancelButtonColor: '#8f8f8f'
    }).then((result) => {
      if (result.isConfirmed) {
        this.isDeleting = true;
        this.ppService.removeRecordsByFile(this.selectedFile).subscribe({
          next: (res) => {
            if (res && res.success) {
              this.toastr.success(res.message, 'Records Deleted');
              this.selectedFile = '';
              this.selectedFileRecordCount = null;
              this.selectedFileImportDate = null;
              this.loadImportedFiles();
            } else {
              this.toastr.error('Failed to remove records.', 'Error');
            }
            this.isDeleting = false;
            this.cdr.detectChanges();
          },
          error: (err) => {
            console.error(err);
            this.toastr.error('An error occurred while deleting records.', 'Error');
            this.isDeleting = false;
            this.cdr.detectChanges();
          }
        });
      }
    });
  }

  outputAllOverpayments(): void {
    this.isExporting = true;
    this.ppService.exportRogersOverpayments().subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = 'RogersOverpayments_Export.xlsx';
        link.click();
        window.URL.revokeObjectURL(url);
        this.toastr.success('Export downloaded successfully!', 'Export Complete');
        this.isExporting = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.toastr.error('Failed to export overpayments.', 'Export Error');
        this.isExporting = false;
        this.cdr.detectChanges();
      }
    });
  }
}
