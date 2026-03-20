import { ChangeDetectorRef, Component } from '@angular/core';
import { CustomSearchService } from '../custom-search-service';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { delay, finalize, tap } from 'rxjs/operators';
import { SpinnerService } from '../../shared/spinner/spinner-service';
import Swal from 'sweetalert2';
import jsPDF from 'jspdf';
import autoTable from 'jspdf-autotable';

@Component({
  selector: 'app-custom-search-component',
  imports: [CommonModule, DatePipe, FormsModule],
  templateUrl: './custom-search-component.html',
  styleUrl: './custom-search-component.css',
})
export class CustomSearchComponent {


  searchValue: string = '';
  searchResults: any[] = [];
  isLoading: boolean = false;
    invoiceNo: string = '';
  seq: number = 1;
headerList: any[] = [];
detailList: any[] = [];
selectedInvoice: string = '';
headerPage: number = 1;
headerPageSize: number = 10;
headerTotalPages: number = 0;
headerPagedData: any[] = [];

// DETAILS PAGINATION
detailPage: number = 1;
detailPageSize: number = 10;
detailTotalPages: number = 0;
detailPagedData: any[] = [];



  constructor(private searchService: CustomSearchService, private spinner: SpinnerService,
    private cdr: ChangeDetectorRef) { }

onSearch(fieldName: string) {
  if (!this.searchValue) {
    alert('Enter value');
    return;
  }

  this.isLoading = true;
  this.spinner.show();

  this.searchService.getHeaders(fieldName, this.searchValue)
    .pipe(
      delay(200), 
      tap((res: any) => {
        if (res.success) {
          this.headerList = res.result;
          this.detailList = [];
           this.headerPage = 1; // reset page
           console.log("isha",this.headerPagedData)
    this.applyHeaderPagination();
        }
      }),
      finalize(() => {
        this.isLoading = false;
        this.spinner.hide();
        this.cdr.detectChanges(); // 👈 force UI refresh
      })
    )
    .subscribe({
      error: (err) => {
        console.error(err);
      }
    });
}
getDetailPages(): number[] {
  const total = this.detailTotalPages;
  const current = this.detailPage;
  const visiblePages = 5;

  let start = Math.max(1, current - Math.floor(visiblePages / 2));
  let end = start + visiblePages - 1;

  if (end > total) {
    end = total;
    start = Math.max(1, end - visiblePages + 1);
  }

  return Array.from({ length: end - start + 1 }, (_, i) => start + i);
}
getHeaderPages(): number[] {
  const total = this.headerTotalPages;
  const current = this.headerPage;
  const visiblePages = 5;

  let start = Math.max(1, current - Math.floor(visiblePages / 2));
  let end = start + visiblePages - 1;

  if (end > total) {
    end = total;
    start = Math.max(1, end - visiblePages + 1);
  }

  return Array.from({ length: end - start + 1 }, (_, i) => start + i);
}

applyHeaderPagination() {
  const start = (this.headerPage - 1) * this.headerPageSize;
  this.headerPagedData = this.headerList.slice(start, start + this.headerPageSize);
  this.headerTotalPages = Math.ceil(this.headerList.length / this.headerPageSize);
  this.cdr.detectChanges();
}

goToHeaderPage(page: number) {
  if (page < 1 || page > this.headerTotalPages) return;
  this.headerPage = page;
  this.applyHeaderPagination();
}


applyDetailPagination() {
  const start = (this.detailPage - 1) * this.detailPageSize;
  this.detailPagedData = this.detailList.slice(start, start + this.detailPageSize);
  this.detailTotalPages = Math.ceil(this.detailList.length / this.detailPageSize);
  this.cdr.detectChanges();
}

goToDetailPage(page: number) {
  if (page < 1 || page > this.detailTotalPages) return;
  this.detailPage = page;
  this.applyDetailPagination();
}
onRowSelect(row: any) {
  this.selectedInvoice = row.invoice;
 this.invoiceNo = row.invoice;
  this.spinner.show();

  this.searchService.getDetails(this.selectedInvoice)
    .pipe(
      delay(150),
      tap((res: any) => {
        if (res.success) {
          this.detailList = res.result;
          console.log("Details:", this.detailList);
          this.detailPage = 1; // reset page
    this.applyDetailPagination();
        }
      }),
      finalize(() => {
        this.spinner.hide();
        this.cdr.detectChanges();
      })
    )
    .subscribe({
      error: (err) => console.error(err)
    });
}
// generateInvoice() {
//   if (!this.invoiceNo) {
//     Swal.fire({
//       icon: 'warning',
//       title: 'Warning',
//       text: 'Please select an invoice',
//     });
//     return;
//   }

//   this.spinner.show();

//   Swal.fire({
//     title: 'Generating Invoice...',
//     didOpen: () => Swal.showLoading(),
//     allowOutsideClick: false
//   });

//   this.searchService.generateInvoice(this.invoiceNo, this.seq)
//     .pipe(
//       delay(200),
//       finalize(() => {
//         this.spinner.hide();
//         Swal.close();
//         this.cdr.detectChanges();
//       })
//     )
//     .subscribe({
//       next: (res: any) => {
//         debugger
//         console.log(res)
//         if (res.length>0) {

//           const items = res;       // all line items
//         const header = {               // take first item as invoice header
//           invoice_no: items[0].invoice_no,
//           invoice_date: items[0].invoice_date,
//           cust_no: items[0].cust_no,
//           territory_code: items[0].territory_code,
//           terms_description: items[0].terms_description,
//           CUSTOM_AddressesWB_name: items[0].CUSTOM_AddressesWB_name,
//           CUSTOM_AddressesWB_address1: items[0].CUSTOM_AddressesWB_address1,
//           CUSTOM_AddressesWB_address2: items[0].CUSTOM_AddressesWB_address2,
//           CUSTOM_AddressesWB_city: items[0].CUSTOM_AddressesWB_city,
//           CUSTOM_AddressesWB_prov_state: items[0].CUSTOM_AddressesWB_prov_state,
//           CUSTOM_AddressesWB_postal_zip: items[0].CUSTOM_AddressesWB_postal_zip,
//           CUSTOM_AddressesWB_1_name: items[0].CUSTOM_AddressesWB_1_name,
//           CUSTOM_AddressesWB_1_address1: items[0].CUSTOM_AddressesWB_1_address1,
//           CUSTOM_AddressesWB_1_address2: items[0].CUSTOM_AddressesWB_1_address2,
//           CUSTOM_AddressesWB_1_city: items[0].CUSTOM_AddressesWB_1_city,
//           CUSTOM_AddressesWB_1_prov_state: items[0].CUSTOM_AddressesWB_1_prov_state,
//           CUSTOM_AddressesWB_1_postal_zip: items[0].CUSTOM_AddressesWB_1_postal_zip,
//           subtotal: items[0].subtotal,
//           freight: items[0].freight,
//           total_discount: items[0].total_discount,
//           total: items[0].total
//         };

//         this.printInvoice(header, items); 

//         } else {
//           Swal.fire({
//             icon: 'error',
//             title: 'Error',
//             text: res.message || 'Error generating invoice'
//           });
//         }
//       },
//       error: (err) => {
//         Swal.fire({
//           icon: 'error',
//           title: 'Error',
//           text: 'Error generating invoice'
//         });
//         console.error(err);
//       }
//     });
// }
 // ===== SAFE TEXT HELPER =====
  private safeText(value: any): string {
    return value !== undefined && value !== null ? String(value) : '';
  }
  // ===== GENERATE INVOICE =====
  generateInvoice() {
    if (!this.invoiceNo) {
      Swal.fire({
        icon: 'warning',
        title: 'Warning',
        text: 'Please select an invoice',
      });
      return;
    }

    this.spinner.show();
    Swal.fire({
      title: 'Generating Invoice...',
      didOpen: () => Swal.showLoading(),
      allowOutsideClick: false
    });

    this.searchService.generateInvoice(this.invoiceNo, this.seq)
      .pipe(
        delay(200),
        finalize(() => {
          this.spinner.hide();
          Swal.close();
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (res: any) => {
          if (res && res.length > 0) {
            const items = res;
            const header = items[0]; // first item as header
            this.printInvoice(header, items);
          } else {
            Swal.fire({
              icon: 'error',
              title: 'Error',
              text: res?.message || 'Error generating invoice'
            });
          }
        },
        error: (err) => {
          Swal.fire({
            icon: 'error',
            title: 'Error',
            text: 'Error generating invoice'
          });
          console.error(err);
        }
      });
  }

  // ===== PRINT INVOICE =====
  printInvoice(header: any, items: any[]) {
  const pdf = new jsPDF();

  // ===== HEADER =====
  pdf.setFontSize(18);
  pdf.setTextColor(41, 128, 185);
  pdf.text('INVOICE', 105, 15, { align: 'center' });

  pdf.setFontSize(12);
  pdf.setTextColor(0, 0, 0);

  // Left column
  pdf.setFont('helvetica', 'bold');
  pdf.text('Invoice No:', 14, 30);
  pdf.text('Invoice Date:', 14, 38);
  pdf.text('Customer No:', 14, 46);
  pdf.text('Territory:', 14, 54);
  pdf.text('Terms:', 14, 62);

  pdf.setFont('helvetica', 'normal');
  pdf.text(this.safeText(header.invoice_no), 50, 30);
  pdf.text(new Date(header.invoice_date).toLocaleDateString(), 50, 38);
  pdf.text(this.safeText(header.cust_no), 50, 46);
  pdf.text(this.safeText(header.territory_code), 50, 54);
  pdf.text(this.safeText(header.terms_description), 50, 62);

  // Right column – Billing Address
  pdf.setFont('helvetica', 'bold');
  pdf.text('Billing Address:', 120, 30);
  pdf.setFont('helvetica', 'normal');
  pdf.text(this.safeText(header.CUSTOM_AddressesWB_name), 120, 38);
  pdf.text(this.safeText(header.CUSTOM_AddressesWB_address1), 120, 46);
  pdf.text(this.safeText(header.CUSTOM_AddressesWB_address2), 120, 54);
  pdf.text(`${this.safeText(header.CUSTOM_AddressesWB_city)}, ${this.safeText(header.CUSTOM_AddressesWB_prov_state)} - ${this.safeText(header.CUSTOM_AddressesWB_postal_zip)}`, 120, 62);

  // Shipping Address
  pdf.setFont('helvetica', 'bold');
  pdf.text('Shipping Address:', 120, 72);
  pdf.setFont('helvetica', 'normal');
  pdf.text(this.safeText(header.CUSTOM_AddressesWB_1_name), 120, 80);
  pdf.text(this.safeText(header.CUSTOM_AddressesWB_1_address1), 120, 88);
  pdf.text(this.safeText(header.CUSTOM_AddressesWB_1_address2), 120, 96);
  pdf.text(`${this.safeText(header.CUSTOM_AddressesWB_1_city)}, ${this.safeText(header.CUSTOM_AddressesWB_1_prov_state)} - ${this.safeText(header.CUSTOM_AddressesWB_1_postal_zip)}`, 120, 104);

  // ===== ITEMS TABLE =====
  const tableColumn = ['No', 'Part No', 'Description', 'Qty', 'Unit Price', 'Total'];
  const tableRows = items.map((item, index) => [
    `${index + 1}`,
    this.safeText(item.part_no),
    this.safeText(item.description),
    Number(item.committed_qty).toLocaleString(),
    Number(item.unit_price).toLocaleString(),
    Number(item.total).toLocaleString()
  ]);

  autoTable(pdf, {
    startY: 120,
    head: [tableColumn],
    body: tableRows,
    theme: 'striped',
    headStyles: { fillColor: [41, 128, 185], textColor: 255, fontStyle: 'bold' },
    bodyStyles: { fontSize: 11 },
    alternateRowStyles: { fillColor: [245, 245, 245] },
    columnStyles: {
      3: { halign: 'right' }, // Qty
      4: { halign: 'right' }, // Unit Price
      5: { halign: 'right' }, // Total
    },
  });

  // ===== FOOTER / TOTALS =====
  // Get Y position after table
  // @ts-ignore
  const finalY = pdf.lastAutoTable?.finalY || 150;

  pdf.setFontSize(12);
  pdf.setFont('helvetica', 'bold');
  pdf.setTextColor(41, 128, 185);
  pdf.text(`Subtotal: ${Number(header.subtotal).toLocaleString()}`, 140, finalY + 8);
  pdf.text(`Freight: ${Number(header.freight).toLocaleString()}`, 140, finalY + 16);
  pdf.text(`Total Discount: ${Number(header.total_discount).toLocaleString()}`, 140, finalY + 24);
  pdf.setFontSize(14);
  pdf.text(`TOTAL: ${Number(header.total).toLocaleString()}`, 140, finalY + 32);

  // ===== PAGE NUMBERS =====
  const pageCount = pdf.getNumberOfPages();
  for (let i = 1; i <= pageCount; i++) {
    pdf.setPage(i);
    pdf.setFontSize(10);
    pdf.setTextColor(100);
    pdf.text(`Page ${i} of ${pageCount}`, 105, pdf.internal.pageSize.getHeight() - 5, { align: 'center' });
  }

  // ===== SAVE PDF =====
  pdf.save(`Invoice-${this.safeText(header.invoice_no)}.pdf`);
}


generateTransactionPDF() {
  if (!this.invoiceNo) {
    Swal.fire({
      icon: 'warning',
      title: 'Warning',
      text: 'Please select an invoice',
    });
    return;
  }

  this.spinner.show();

  Swal.fire({
    title: 'Generating Transaction Report...',
    didOpen: () => Swal.showLoading(),
    allowOutsideClick: false
  });

  this.searchService.getTransactions(this.invoiceNo)
    .pipe(
      delay(200),
      finalize(() => {
        this.spinner.hide();
        Swal.close();
        this.cdr.detectChanges();
      })
    )
    .subscribe({
      next: (res: any) => {
        if (res.success && res.result.length > 0) {
          this.printTransaction(res.result);
        } else {
          Swal.fire({
            icon: 'error',
            title: 'Error',
            text: 'No transaction data found'
          });
        }
      },
      error: (err) => {
        console.error(err);
        Swal.fire({
          icon: 'error',
          title: 'Error',
          text: 'Error generating transaction report'
        });
      }
    });
}
printTransaction(data: Array<{
  accountNo: string;
  name: string;
  debit: number | string;
  credit: number | string;
  transNo: string;
  date: string | Date;
}>) {
  const pdf = new jsPDF();
  const pageHeight = pdf.internal.pageSize.getHeight();
  let yPos = 35;

  // ===== HEADER =====
  pdf.setFontSize(18);
  pdf.setTextColor(41, 128, 185);
  pdf.text('GL TRANSACTION REPORT', 105, 15, { align: 'center' });

  pdf.setFontSize(12);
  pdf.setTextColor(0, 0, 0);
  pdf.text(`Invoice: ${this.safeText(this.invoiceNo)}`, 14, 25);
  pdf.text(`Generated On: ${new Date().toLocaleDateString()}`, 150, 25);

  // ===== TOTALS INIT =====
  let totalDebit = 0;
  let totalCredit = 0;

  data.forEach((row, index) => {
    // Start a new page if close to bottom
    if (yPos + 40 > pageHeight) {
      pdf.addPage();
      yPos = 20;
    }

    // ===== BACKGROUND BOX =====
    if (index % 2 === 0) {
      pdf.setFillColor(245, 245, 245); // light grey
      pdf.rect(10, yPos - 4, 190, 32, 'F'); // x, y, width, height
    }

    pdf.setFontSize(12);
    pdf.setTextColor(0, 0, 0);

    // Bold labels
    pdf.setFont('helvetica', 'bold');
    pdf.text(`Acc Number:`, 14, yPos);
    pdf.setFont('helvetica', 'normal');
    pdf.text(`${this.safeText(row.accountNo)}`, 50, yPos);

    yPos += 6;
    pdf.setFont('helvetica', 'bold');
    pdf.text(`Name:`, 14, yPos);
    pdf.setFont('helvetica', 'normal');
    pdf.text(`${this.safeText(row.name)}`, 50, yPos);

    yPos += 6;
    pdf.setFont('helvetica', 'bold');
    pdf.text(`Debit:`, 14, yPos);
    pdf.setFont('helvetica', 'normal');
    pdf.text(`${Number(row.debit || 0).toLocaleString()}`, 50, yPos);

    yPos += 6;
    pdf.setFont('helvetica', 'bold');
    pdf.text(`Credit:`, 14, yPos);
    pdf.setFont('helvetica', 'normal');
    pdf.text(`${Number(row.credit || 0).toLocaleString()}`, 50, yPos);

    yPos += 6;
    pdf.setFont('helvetica', 'bold');
    pdf.text(`Trans Number:`, 14, yPos);
    pdf.setFont('helvetica', 'normal');
    pdf.text(`${this.safeText(row.transNo)}`, 50, yPos);

    yPos += 6;
    pdf.setFont('helvetica', 'bold');
    pdf.text(`Date:`, 14, yPos);
    pdf.setFont('helvetica', 'normal');
    pdf.text(`${new Date(row.date).toLocaleDateString()}`, 50, yPos);

    yPos += 10; // spacing after each transaction

    totalDebit += Number(row.debit || 0);
    totalCredit += Number(row.credit || 0);
  });

  // ===== TOTALS BOX =====
  pdf.setFillColor(41, 128, 185); // blue
  pdf.rect(10, yPos - 4, 190, 14, 'F');
  pdf.setFontSize(12);
  pdf.setTextColor(255, 255, 255);
  pdf.setFont('helvetica', 'bold');
  pdf.text(`Total Debit: ${totalDebit.toLocaleString()}`, 14, yPos + 5);
  pdf.text(`Total Credit: ${totalCredit.toLocaleString()}`, 120, yPos + 5);

  // ===== PAGE NUMBERS =====
  const pageCount = pdf.getNumberOfPages();
  for (let i = 1; i <= pageCount; i++) {
    pdf.setPage(i);
    pdf.setFontSize(10);
    pdf.setTextColor(100);
    pdf.text(`Page ${i} of ${pageCount}`, 105, pageHeight - 5, { align: 'center' });
  }

  // ===== SAVE PDF =====
  pdf.save(`Transaction-${this.invoiceNo}.pdf`);
}


// ===== SAFE TEXT HELPER =====


}
