import { ChangeDetectorRef, Component, ElementRef, ViewChild } from '@angular/core';
import { OutputInvoiceService } from '../output-invoice-service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { delay, tap } from 'rxjs/operators';
import { SpinnerService } from '../../shared/spinner/spinner-service';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';
import saveAs from 'file-saver';
import * as XLSX from 'xlsx-js-style'; 

@Component({
  selector: 'app-output-invoice-component',
  imports: [CommonModule,FormsModule],
  templateUrl: './output-invoice-component.html',
  styleUrl: './output-invoice-component.css',
})
export class OutputInvoiceComponent {
   @ViewChild('fileInput') fileInputVariable!: ElementRef;
invoices: any[] = [];
invalidRows: any[] = [];
  outputFolder: string = 'C:\\PDFsInvoices';
  filePrefix: string = '';
  statusMessage: string = '';
  isLoading: boolean = false;
  currentPage: number = 1;
pageSize: number = 10;
totalInvoices: number = 0;
invoiceType: string = 'Normal';
selectedFile: File | null = null;

  constructor(private invoiceService: OutputInvoiceService, private cdr: ChangeDetectorRef,private spinner:SpinnerService,
    private toastr: ToastrService) {}

  ngOnInit() {
    // this.loadInvoices();
  }

  loadInvoices(currentPage: any) {
  debugger

  this.currentPage = currentPage;
  this.spinner.show();

  this.invoiceService.getInvoices(this.currentPage, this.pageSize).subscribe({
    next: (res) => {
      this.invoices = res.data;
      this.totalInvoices = res.totalCount;

      console.log('Invoices loaded successfully:', this.invoices);

      this.spinner.hide();
      this.cdr.detectChanges();
    },
    error: (err) => {
      console.error('Error loading invoices', err);

      this.spinner.hide();
this.toastr.error('Error loading invoices', 'Database Error');
      this.cdr.detectChanges();
    }
  });
}
onPageChange(page: number) {
  if (page < 1) return;
  if ((page - 1) * this.pageSize >= this.totalInvoices) return;

  this.currentPage = page;
  this.loadInvoices(this.currentPage);
}



onClearData() {
    Swal.fire({
      title: 'Are you sure?',
      text: "You won't be able to revert this!",
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#3085d6',
      cancelButtonColor: '#d33',
      confirmButtonText: 'Yes, delete it!'
    }).then((result) => {
      if (result.isConfirmed) {
        this.invoiceService.clearData().subscribe({
          next: () => {
            this.toastr.success('List cleared successfully', 'Completed');
            this.loadInvoices(this.currentPage);
          },
          error: () => this.toastr.error('Could not clear data')
        });
      }
    });
  }

//  onOutputInvoices() {
//     if (this.invoices.length === 0) {
//        this.toastr.warning("No invoices found in the list", "Warning");
//         return;
//     }
// this.spinner.show()
//     const payload = {
//         OutputFolder: this.outputFolder,
//         FilePrefix: this.filePrefix,
//         InvoiceType: this.invoiceType // <-- Ye add karna zaroori hai
//     };

//     this.invoiceService.outputInvoices(payload).subscribe({
//         next: (res) => {
//            this.spinner.hide()
//             Swal.fire({
//                 title: 'Process Completed!',
//                 text: `${res.processedCount} invoices processed and saved to ${this.outputFolder}`,
//                 icon: 'success',
//                 confirmButtonText: 'Great!'
//             });
//         },
//         error: () => {
//                     this.spinner.hide()

//            this.toastr.error("Error during PDF generation", "Process Failed");
//         }
//     });
// }

onOutputInvoices() {

debugger
  this.spinner.show();
  const payload = {
    filePrefix: this.filePrefix || 'INV',
    invoiceType: 'All'
  };

  this.invoiceService.generateInvoicesZip(payload).subscribe({
    next: (blob: Blob) => {
      this.spinner.hide();
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `Invoices_${new Date().getTime()}.zip`;
      link.click();
      window.URL.revokeObjectURL(url);
      Swal.fire('Success', 'ZIP file downloaded successfully', 'success');
    },
    error: (err) => {
      this.spinner.hide();
      Swal.fire('Error', 'Failed to generate ZIP', 'error');
    }
  });
}
onFileUpload(event: Event) {
  const input = event.target as HTMLInputElement;
  if (!input.files || input.files.length === 0) {
    this.selectedFile = null;
    return;
  }
  this.selectedFile = input.files[0];
  this.cdr.detectChanges();
}

uploadfile() {
  if (!this.selectedFile) return;

  this.spinner.show();

  this.invoiceService.uploadInvoiceTemplate(this.selectedFile).subscribe({
    next: (res: any) => {
      this.spinner.hide();
      
      if (res.success) {
        const result = res.result;
        
      if (result.failedCount > 0) {
  let errorList = result.invalidRows
    .map((row: any) => {
      return `<strong>Row ${row.RowNumber}:</strong> ${row.Value}`;
    })
    .join('<br>');

  Swal.fire({
    title: `Sync Results`,
    html: `<div style="text-align:left;">` +
          `<span class="text-success">✔ Matched: ${result.insertedCount}</span><br>` +
          `<span class="text-danger">✖ Failed: ${result.failedCount}</span><br><br>` +
          `<b>Error Details:</b><br>` +
          `<div style="max-height:150px; overflow-y:auto; background:#f8f9fa; padding:10px; border:1px solid #dee2e6;">${errorList}</div>` +
          `</div>`,
    icon: 'warning'
  });
} else {
          Swal.fire('Success', `Matched all ${result.insertedCount} invoices successfully!`, 'success');
        }

        this.loadInvoices(1); 
        this.selectedFile = null;
        if (this.fileInputVariable) {
          this.fileInputVariable.nativeElement.value = ""; 
        }
      } else {
        Swal.fire('Error', res.message || 'Matching failed', 'error');
      }
    },
    error: (err) => {
      this.spinner.hide();
      Swal.fire('Error', 'Something went wrong during sync.', 'error');
    }
  });
}
downloadTemplate() {
  this.spinner.show(); 
  
  const headers = [['IMEI', 'Order Number', 'Invoice Number']];
  const ws = XLSX.utils.aoa_to_sheet(headers);

  ws['!cols'] = [
    { wch: 20 }, // IMEI
    { wch: 20 }, // Order Number
    { wch: 20 }  // Invoice Number
  ];

  
  const range = XLSX.utils.decode_range(ws['!ref']!);
  for (let col = range.s.c; col <= range.e.c; col++) {
    const cellAddress = XLSX.utils.encode_cell({ r: 0, c: col });
    if (!ws[cellAddress]) continue;
    
    ws[cellAddress].s = {
      font: { bold: true, sz: 12 },
      alignment: { horizontal: "center" },
      fill: { fgColor: { rgb: "F0F0F0" } } 
    };
  }

  const wb = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(wb, ws, 'Invoice_Template');

  const buffer = XLSX.write(wb, { bookType: 'xlsx', type: 'array' });
  saveAs(new Blob([buffer]), 'Invoice_Export_Template.xlsx');

  this.spinner.hide();
  
  Swal.fire({ 
    icon: 'success', 
    title: 'Template Downloaded', 
    text: 'Please fill the IMEI, Order No, and Invoice No.',
    timer: 2000, 
    showConfirmButton: false 
  });
}
}