import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { HardwareReceipt, IMEIReportService } from '../imeireport-service';
import { SpinnerService } from '../../shared/spinner/spinner-service';
import { hidden } from '@angular/forms/signals';
import Swal from 'sweetalert2';
import { DateFormatPipe } from '../../shared/pipes/date-format-pipe';

@Component({
  selector: 'app-imei-report-component',
  standalone: true,
  imports: [CommonModule, FormsModule, DateFormatPipe],
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
  vendors: any = [];
  parts: any[] = [];
  reportData: any[] = [];
  stockData: any[] = [];
  // receipts: any;

  // UI state
  error = '';
  today: any;
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
    private spinner: SpinnerService,
    private toastr: ToastrService
  ) { }

  ngOnInit() {
    this.loadVendors();
    const now = new Date();
    this.today = now.toISOString().split('T')[0];
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


  combo8Changed() {
    if (this.combo8 === 'Hardware') this.loadParts('HDW');
    else if (this.combo8 === 'Accessory') this.loadParts('ACC');
  }

  loadVendors() {
    this.reportService.getVendors().subscribe({
      next: (res: any) => {
        this.vendors = res.result || [];
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Vendor API Error:', err);
        this.cdr.detectChanges();
      }
    });
  }

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

  generateIMEIReport() {
    this.spinner.show();
    if (!this.imeiStartDate || !this.imeiEndDate) {
      this.toastr.warning('Please select Start Date and End Date', 'Selection Required');
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
        this.spinner.hide();
        if (this.chkExcel) this.reportService.exportToExcel(res, 'IMEIReport');
        else this.reportData = res;

        this.cdr.detectChanges();
      },
      error: err => {
        this.spinner.hide();
        console.error(err);
        this.error = 'Failed to load IMEI report';
        this.cdr.detectChanges();
      }
    });
  }

  spireStockStatus(exportExcel: boolean = true) {
    this.spinner.show()
    this.error = '';
    this.reportService.getSpireStockStatus().subscribe({
      next: res => {
        this.stockData = res;
        this.cdr.detectChanges();

        if (exportExcel && this.stockData.length) {
          this.reportService.exportToExcel(this.stockData, 'SpireStockStatus');
          this.spinner.hide()
        }
      },
      error: err => {
        console.error(err);
        this.error = 'Failed to load stock status';
        this.spinner.hide()

        this.cdr.detectChanges();
      }
    });
  }

  generateReceiptsReport() {
    debugger
    this.spinner.show()
    if (!this.receiptStartDate || !this.receiptEndDate) {
      this.spinner.hide()
      Swal.fire('Error', 'Please select Start Date and End Date', 'error');
      return;
    }

    const today = new Date();
    const startDate = new Date(this.receiptStartDate);
    const endDate = new Date(this.receiptEndDate);

    if (startDate > today || endDate > today) {
      Swal.fire('Error', 'Dates cannot be in the future', 'error');
      this.spinner.hide()
      this.receiptStartDate = '';
      this.receiptEndDate = '';

      return;
    }

    if (endDate < startDate) {
      Swal.fire('Error', 'End Date cannot be before Start Date', 'error');
      this.spinner.hide()
      this.receiptStartDate = '';
      this.receiptEndDate = '';

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
          debugger
          if (data.length) {
            const startStr = new Date(formattedStartDate).toISOString().split('T')[0];
            const endStr = new Date(formattedEndDate).toISOString().split('T')[0];
            const fileName = `SpireReceipts_${startStr}_to_${endStr}`;
            this.reportService.exportToExcel(data, fileName);
            this.receiptStartDate = '';
            this.receiptEndDate = '';
            this.spinner.hide()

          } else {
            this.receipts = data;
            this.spinner.hide()
            this.receiptStartDate = '';
            this.receiptEndDate = '';
            Swal.fire('Info', 'No records to download', 'info');
            return;

          }
          this.cdr.detectChanges();
        },
        error: err => {
          console.error(err);
          this.error = 'Failed to load receipts';
          this.spinner.hide()
          this.receiptStartDate = '';
          this.receiptEndDate = '';
          this.cdr.detectChanges();
        }
      });
  }


  searchReceipts() {
    this.spinner.show()
    this.error = '';
    this.receipts = [];
    if (!this.receiptNo && !this.poNumber) {
      this.error = 'Please enter either Receipt No or PO Number';
      this.spinner.hide()
      this.cdr.detectChanges();
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
          this.spinner.hide()
          this.receiptNo='';
          this.poNumber=''
        } else {
          this.error = 'No records found';
          this.spinner.hide()
          Swal.fire('Info', 'No records to download', 'info');
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.spinner.hide()
        this.error = 'Failed to load receipts';
        this.cdr.detectChanges();
      }
    });
  }








  spireReceivedReport() {
    this.spinner.show()
    if (!this.combo8) {
      Swal.fire('Error', 'Please select Item Type (Hardware/Accessory)', 'error');
      this.spinner.hide();
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

    if (!startDateStr || !endDateStr) {
      this.imeiStartDate = '';
      this.imeiEndDate = '';
    }

    this.reportService.getReceivedReport(
      itemType,
      vendorValue,
      partValue,
      startDateStr,
      endDateStr
    ).subscribe({
      next: (data) => {
        this.spinner.hide();
        if (!data || data.length === 0) {
          Swal.fire('Info', 'No records to download', 'info');
          this.imeiStartDate = '';
          this.imeiEndDate = '';
          this.cdr.detectChanges();
          return;
        }

        const fileName =
          itemType === 'HDW'
            ? 'HardwareReceipts.xlsx'
            : 'AccessoryReceipts.xlsx';

        this.reportService.exportToExcel(data, fileName);
        this.clearInputs();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.spinner.hide();
        console.error(err);
        Swal.fire('Error', 'Failed to load report', 'error');
        this.error = 'Failed to load report';
        this.cdr.detectChanges();
      }
    });
  }
  clearInputs() {
    this.imeiStartDate = '';
    this.imeiEndDate = '';
    this.vendor = '';
    this.part = '';
    this.cdr.detectChanges();
  }

}

