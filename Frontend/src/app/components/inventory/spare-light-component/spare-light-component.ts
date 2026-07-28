import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SpareLightService } from '../spare-light-service';
import * as XLSX from 'xlsx-js-style';
import { saveAs } from 'file-saver';
import Swal from 'sweetalert2';
import { SpinnerService } from '../../shared/spinner/spinner-service';
import { Spinner } from '../../shared/spinner/spinner';

@Component({
  selector: 'app-spare-light-component',
  imports: [CommonModule, FormsModule,Spinner],
  templateUrl: './spare-light-component.html',
  styleUrl: './spare-light-component.css',
})
export class SpareLightComponent implements OnInit {
  transferDate: string = new Date().toISOString().split('T')[0];
  startDate: string = '';
  endDate: string = '';
  
  // Hardware State
  hardwareItems: any[] = [];
  hardwareErrors: number = 0;
  isHardwareValid: boolean = false;
  
  // Accessory State
  accessoryItems: any[] = [];
  accessoryErrors: number = 0;
  isAccessoryValid: boolean = false;

  // Modal State
  isShowingErrors: boolean = false;
  errorItems: any[] = [];
  errorColumnName: string = '';

  constructor(
    private spareLightService: SpareLightService,
    private cdr: ChangeDetectorRef,
      private spinner: SpinnerService,
  ) {}  

  ngOnInit(): void {}

  // --- Hardware Methods ---
  onHardwareFileSelected(event: any): void {
    const file = event.target.files[0];
    if (file) {
      debugger
      this.spinner.show()
      this.spareLightService.uploadHardware(file).subscribe(res => {
        if (res.success) {
          this.hardwareItems = res.result;
          this.hardwareErrors = 0;
          this.isHardwareValid = false;
          this.spinner.hide()
          Swal.fire({ icon: 'success', title: 'Imported Successfully', text: `${this.hardwareItems.length} records imported.` });
          this.cdr.detectChanges();
        }
      });
    }
          this.spinner.hide()

    event.target.value = '';
  }

  validateHardware(): void {
    this.spinner.show()
    if (this.hardwareItems.length === 0)
    {
this.spinner.hide()
      return;
    }
    this.spareLightService.validateHardware().subscribe(res => {
      if (res.success) {
        this.hardwareItems = res.result;
        this.hardwareErrors = res.count;
        this.isHardwareValid = (this.hardwareErrors === 0);
        this.spinner.hide()
        Swal.fire({ icon: 'info', title: 'Validation Complete', text: `${this.hardwareErrors} errors were found.` });
        this.cdr.detectChanges();
      }
    });
  }

  doHardwareTransfer(): void {
    if (!this.isHardwareValid) 
      {
        this.spinner.hide()
        return;
      }
      
      Swal.fire({
        title: 'Are you sure?',
        text: 'Do you want to proceed with the Hardware Transfer?',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: 'Yes, Proceed'
      }).then((result) => {
        if (result.isConfirmed) {
        this.spinner.show()
        this.spareLightService.doHardwareTransfer(this.transferDate).subscribe(res => {
          if (res.success) {
            Swal.fire({ icon: 'success', title: 'Transfer Complete' });
            this.hardwareItems = [];
            this.isHardwareValid = false;
            this.hardwareErrors = 0;
            this.spinner.hide()
          } else {
            this.spinner.hide()

            Swal.fire({ icon: 'error', title: 'Transfer Failed', text: res.message });
          }
          this.cdr.detectChanges();
        });
      }
    });
  }

  downloadHardwareTemplate(): void {
    const headers = [['WarehouseCodeTransferFrom', 'WarehouseCodeTransferTo', 'PartNo', 'IMEI', 'SimPartNo', 'Pin']];
    const ws = XLSX.utils.aoa_to_sheet(headers);

    ws['!cols'] = [
      { wch: 30 }, { wch: 30 }, { wch: 20 }, { wch: 20 }, { wch: 20 }, { wch: 15 }
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
    XLSX.utils.book_append_sheet(wb, ws, 'Hardware_Template');
    const buffer = XLSX.write(wb, { bookType: 'xlsx', type: 'array' });
    saveAs(new Blob([buffer]), 'Hardware_Transfer_Template.xlsx');

    Swal.fire({ 
      icon: 'success', 
      title: 'Template Downloaded', 
      text: 'Please fill the necessary columns for Hardware Transfer.',
      timer: 2000, 
      showConfirmButton: false 
    });
  }

  // --- Accessory Methods ---
  onAccessoryFileSelected(event: any): void {
    const file = event.target.files[0];
    if (file) {
      this.spareLightService.uploadAccessory(file).subscribe(res => {
        if (res.success) {
          this.accessoryItems = res.result;
          this.accessoryErrors = 0;
          this.isAccessoryValid = false;
          Swal.fire({ icon: 'success', title: 'Accessory Transfer File Imported.', text: `${this.accessoryItems.length} records imported.` });
          this.cdr.detectChanges();
        }
      });
    }
    event.target.value = '';
  }

  validateAccessory(): void {
    if (this.accessoryItems.length === 0) return;
    this.spareLightService.validateAccessory().subscribe(res => {
      if (res.success) {
        this.accessoryItems = res.result;
        this.accessoryErrors = res.count;
        this.isAccessoryValid = (this.accessoryErrors === 0);
        Swal.fire({ icon: 'info', title: 'Validation Complete', text: `${this.accessoryErrors} errors were found.` });
        this.cdr.detectChanges();
      }
    });
  }

  doAccessoryTransfer(): void {
    if (!this.isAccessoryValid) return;
    Swal.fire({
      title: 'Are you sure?',
      text: 'Do you want to proceed with the Accessory Transfer?',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Yes, Proceed'
    }).then((result) => {
      if (result.isConfirmed) {
        this.spareLightService.doAccessoryTransfer(this.transferDate).subscribe(res => {
          if (res.success) {
            Swal.fire({ icon: 'success', title: 'Transfer Complete' });
            this.accessoryItems = [];
            this.isAccessoryValid = false;
            this.accessoryErrors = 0;
          } else {
            Swal.fire({ icon: 'error', title: 'Transfer Failed', text: res.message });
          }
          this.cdr.detectChanges();
        });
      }
    });
  }

  downloadAccessoryTemplate(): void {
    const headers = [['WarehouseCodeTransferFrom', 'WarehouseCodeTransferTo', 'PartNo', 'Quantity']];
    const ws = XLSX.utils.aoa_to_sheet(headers);

    ws['!cols'] = [
      { wch: 30 }, { wch: 30 }, { wch: 20 }, { wch: 15 }
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
    XLSX.utils.book_append_sheet(wb, ws, 'Accessory_Template');
    const buffer = XLSX.write(wb, { bookType: 'xlsx', type: 'array' });
    saveAs(new Blob([buffer]), 'Accessory_Transfer_Template.xlsx');

    Swal.fire({ 
      icon: 'success', 
      title: 'Template Downloaded', 
      text: 'Please fill the necessary columns for Accessory Transfer.',
      timer: 2000, 
      showConfirmButton: false 
    });
  }

  // --- Common Methods ---
  showErrors(type: 'hardware' | 'accessory'): void {
    this.errorColumnName = type;
    if (type === 'hardware') {
      this.errorItems = this.hardwareItems.filter(i => i.validationResult);
    } else {
      this.errorItems = this.accessoryItems.filter(i => i.validationResult);
    }
    this.isShowingErrors = true;
    this.cdr.detectChanges();
  }

  formatErrors(errors: string): string {
    if (!errors) return '';
    const parts = errors.split('.').map(p => p.trim()).filter(p => p !== '');
    return parts.map(p => `• ${p}`).join('<br/>');
  }

  validateDates(): boolean {
    if (!this.startDate || !this.endDate) {
      Swal.fire('Error', 'Please select both Start Date and End Date.', 'error');
      return false;
    }
    if (new Date(this.endDate) < new Date(this.startDate)) {
      Swal.fire('Error', 'End Date cannot be less than Start Date.', 'error');
      return false;
    }
    return true;
  }

  getHardwareLogs(): void {
  if (!this.validateDates()) return;

  this.spareLightService
    .getLog(this.startDate, this.endDate, 'Hardware')
    .subscribe(res => {
      if (res.success) {
        console.log('Hardware Logs:', res.result);

        this.exportToExcel(res.result, 'Hardware_Logs');

        Swal.fire({
          icon: 'success',
          title: 'Hardware Logs Downloaded',
          text: `${res.result.length} records exported`
        });

        this.cdr.detectChanges();
      }
    });
}

 getAccessoryLogs(): void {
  if (!this.validateDates()) return;

  this.spareLightService
    .getLog(this.startDate, this.endDate, 'Accessory')
    .subscribe(res => {
      if (res.success) {
        console.log('Accessory Logs:', res.result);

        this.exportToExcel(res.result, 'Accessory_Logs');

        Swal.fire({
          icon: 'success',
          title: 'Accessory Logs Downloaded',
          text: `${res.result.length} records exported`
        });

        this.cdr.detectChanges();
      }
    });
}

exportToExcel(data: any[], fileName: string) {
  if (!data || data.length === 0) {
    Swal.fire('No Data', 'No records found', 'warning');
    return;
  }

  const ws: XLSX.WorkSheet = XLSX.utils.json_to_sheet(data);

  // Optional styling
  const wb: XLSX.WorkBook = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(wb, ws, 'Logs');

  const buffer = XLSX.write(wb, {
    bookType: 'xlsx',
    type: 'array'
  });

  saveAs(new Blob([buffer]), `${fileName}_${new Date().getTime()}.xlsx`);
}

  exitApplication(): void {
    Swal.fire({
      title: 'Exit Application?',
      text: 'Are you sure you want to exit?',
      icon: 'question',
      showCancelButton: true,
      confirmButtonText: 'Yes, Exit'
    }).then((result) => {
      if (result.isConfirmed) {
        window.location.href = '/dashboard';
      }
    });
  }
}



