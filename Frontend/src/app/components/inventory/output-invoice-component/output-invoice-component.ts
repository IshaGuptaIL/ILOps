import { ChangeDetectorRef, Component } from '@angular/core';
import { OutputInvoiceService } from '../output-invoice-service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { delay, tap } from 'rxjs/operators';
import { SpinnerService } from '../../shared/spinner/spinner-service';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-output-invoice-component',
  imports: [CommonModule,FormsModule],
  templateUrl: './output-invoice-component.html',
  styleUrl: './output-invoice-component.css',
})
export class OutputInvoiceComponent {
invoices: any[] = [];
  outputFolder: string = 'C:\\PDFsInvoices';
  filePrefix: string = '';
  statusMessage: string = '';
  isLoading: boolean = false;
  currentPage: number = 1;
pageSize: number = 10;
totalInvoices: number = 0;
invoiceType: string = 'Normal';

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

 onOutputInvoices() {
    if (this.invoices.length === 0) {
       this.toastr.warning("No invoices found in the list", "Warning");
        return;
    }
this.spinner.show()
    const payload = {
        OutputFolder: this.outputFolder,
        FilePrefix: this.filePrefix,
        InvoiceType: this.invoiceType // <-- Ye add karna zaroori hai
    };

    this.invoiceService.outputInvoices(payload).subscribe({
        next: (res) => {
           this.spinner.hide()
            Swal.fire({
                title: 'Process Completed!',
                text: `${res.processedCount} invoices processed and saved to ${this.outputFolder}`,
                icon: 'success',
                confirmButtonText: 'Great!'
            });
        },
        error: () => {
                    this.spinner.hide()

           this.toastr.error("Error during PDF generation", "Process Failed");
        }
    });
}
}