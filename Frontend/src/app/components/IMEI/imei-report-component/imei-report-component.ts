import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HardwareReceipt, IMEIReportService } from '../imeireport-service';
import { SpinnerService } from '../../shared/spinner/spinner-service';
import { hidden } from '@angular/forms/signals';
import { DateFormatPipe } from '../../../shared/pipes/date-format-pipe';

@Component({
  selector: 'app-imei-report-component',
  standalone: true,
  imports: [CommonModule, FormsModule,DateFormatPipe  ],
  templateUrl: './imei-report-component.html',
  styleUrls: ['./imei-report-component.css']
})
export class ImeiReportComponent implements OnInit {

  // Filters
  combo8 = ''; // Hardware / Accessory
  vendor: string | null = null;
  part: string | null = null;
  chkAll = false;
  chkAllParts = false;
  chkExcel = false;
  startDate!: string; // yyyy-MM-dd format
  endDate!: string;
  whse: string = 'CO';

  // Data
  vendors: any=[];
  parts: any[] = [];
  reportData: any[]=[];
  stockData: any[] = [];
  // receipts: any;

  // UI state
  error = '';

  receiptNo: string = '';
  poNumber: string = '';
   receipts: HardwareReceipt[] = [];

  receiptStartDate!: string;
receiptEndDate!: string;

imeiStartDate!: string;
imeiEndDate!: string;


  constructor(
    private reportService: IMEIReportService,
    private cdr: ChangeDetectorRef,
    private spinner:SpinnerService,
  ) {}

  ngOnInit() {
    this.loadVendors();
  }


     formatToMMDDYYYY(dateString: string): string {
    if (!dateString) return '';
    
    const date = new Date(dateString);
    if (isNaN(date.getTime())) return dateString;
    
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const year = date.getFullYear();
    
    return `${month}-${day}-${year}`;
  }
  

  // Item type change
  combo8Changed() {
    if (this.combo8 === 'Hardware') this.loadParts('HDW');
    else if (this.combo8 === 'Accessory') this.loadParts('ACC');
  }

  // Load vendors
  loadVendors() {
  this.reportService.getVendors().subscribe({
    next: (res: any) => {
      this.vendors = res.result || [];
    },
    error: (err) => {
      console.error('Vendor API Error:', err);
    }
  });
}

  // Load parts based on type
  loadParts(itemType: string) {
    this.reportService.getParts(itemType).subscribe({
      next: res => {
        this.parts = res.success ? res.result || [] : [];
        this.cdr.detectChanges();
      },
      error: err => {
        this.parts = [];
        this.cdr.detectChanges();
      }
    });
  }

  // Generate IMEI report
  generateIMEIReport() {
    debugger
    this.spinner.show();
   if (!this.imeiStartDate || !this.imeiEndDate) {
  alert('Please select Start Date and End Date');
  this.spinner.hide();
  return;
}


    this.error = '';

     const formattedStartDate = this.formatToMMDDYYYY(this.imeiStartDate);
    const formattedEndDate = this.formatToMMDDYYYY(this.imeiEndDate);

    const payload = {
      itemType: this.combo8 === 'Hardware' ? 'HDW' : 'ACC',
      vendor: this.chkAll ? null : this.vendor,
      part: this.chkAllParts ? null : this.part,
      startDate: formattedStartDate,
      endDate: formattedEndDate,
      exportExcel: this.chkExcel
    };

    this.reportService.getIMEIReport(payload).subscribe({
      next: res => {
        if (this.chkExcel) this.reportService.exportToExcel(res, 'IMEIReport');
        else this.reportData = res;

        this.cdr.detectChanges();
      },
      error: err => {
        console.error(err);
        this.error = 'Failed to load IMEI report';
        this.cdr.detectChanges();
      }
    });
  }

  // Spire Stock Status
  spireStockStatus(exportExcel: boolean = true) {
    this.error = '';
    this.reportService.getSpireStockStatus().subscribe({
      next: res => {
        this.stockData = res;
        this.cdr.detectChanges();

        if (exportExcel && this.stockData.length) {
          this.reportService.exportToExcel(this.stockData, 'SpireStockStatus');
        }
      },
      error: err => {
        console.error(err);
        this.error = 'Failed to load stock status';
        this.cdr.detectChanges();
      }
    });
  }

  // Spire Receipts
  generateReceiptsReport() {
    debugger
   if (!this.receiptStartDate || !this.receiptEndDate) {
  alert('Please select Start Date and End Date');
  return;
}

    this.error = '';
    this.receipts = [];



     const formattedStartDate = this.formatToMMDDYYYY(this.receiptStartDate);
    const formattedEndDate = this.formatToMMDDYYYY(this.receiptEndDate);

    this.reportService.getReceipts(formattedStartDate,
  formattedEndDate, this.whse)
      .subscribe({
        next: data => {
          console.log(data)
          if ( data.length) {
             const startStr = new Date(formattedStartDate).toISOString().split('T')[0];
          const endStr = new Date(formattedEndDate).toISOString().split('T')[0];
          const fileName = `SpireReceipts_${startStr}_to_${endStr}`;

          // Call exportToExcel method
          this.reportService.exportToExcel(data, fileName);
          } else {
            this.receipts = data;
          }
          this.cdr.detectChanges();
        },
        error: err => {
          console.error(err);
          this.error = 'Failed to load receipts';
          this.cdr.detectChanges();
        }
      });
  }


searchReceipts() {
debugger
  this.error = '';
  this.receipts = [];

  if (!this.receiptNo && !this.poNumber) {
    this.error = 'Please enter either Receipt No or PO Number';
    return;
  }




   const params: any = {};
  if (this.receiptNo?.trim()) params.receiptNo = this.receiptNo.trim();
  if (this.poNumber?.trim()) params.poNumber = this.poNumber.trim();

  this.reportService.getHardwareReceipts(params).subscribe({
  
    next: (data) => {

      this.receipts = data;

      if (data && data.length > 0) {
        const fileName = `HardwareReceipts_${this.receiptNo || this.poNumber}`;
        this.reportService.exportToExcel(data, fileName);
      } else {
        this.error = 'No records found';
      }

    },
    error: (err) => {
      console.error(err);
      this.error = 'Failed to load receipts';
    }
  });
}







spireReceivedReport() {
debugger
  if (!this.combo8) {
    alert('Please select Item Type (Hardware/Accessory)');
    return;
  }

  this.error = '';

  const itemType = this.combo8 === 'Hardware' ? 'HDW' : 'ACC';

  const vendorValue = this.vendor ? this.vendor : undefined;
  const partValue = this.part ? this.part : undefined;

  const startDateStr = this.imeiStartDate
    ? new Date(this.imeiStartDate).toISOString().split('T')[0]
    : undefined;

  const endDateStr = this.imeiEndDate
    ? new Date(this.imeiEndDate).toISOString().split('T')[0]
    : undefined;

  this.reportService.getReceivedReport(
    itemType,
    vendorValue,
    partValue,
    startDateStr,
    endDateStr
  ).subscribe({
    next: (data) => {
      if (data.length) {
        const fileName =
          itemType === 'HDW'
            ? 'HardwareReceipts.xlsx'
            : 'AccessoryReceipts.xlsx';

        this.reportService.exportToExcel(data, fileName);
      } else {
        this.reportData = data;
      }

    },
    error: (err) => {
      console.error(err);
      this.error = 'Failed to load report';
    }
  });
}

}

