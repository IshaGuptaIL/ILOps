import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { InventoryService } from '../add-inventory-component/inventory-service';
import * as XLSX from 'xlsx';
import { saveAs } from 'file-saver';

@Component({
  selector: 'app-cost-validation-component',
  imports: [],
  templateUrl: './cost-validation-component.html',
  styleUrl: './cost-validation-component.css',
})
export class CostValidationComponent implements OnInit {

  title = 'View HPC';
  exportViewType: string = 'Latest';

  // main data
  model: any[] = [];
  modelColumns: string[] = [];

  // invalid tab
  invalidRows: any[] = [];
  invalidColumns: string[] = [];

  // pagination
  currentPage = 1;
  pageSize = 10;
  totalPages = 0;
  pagedData: any[] = [];

  // upload result
  insertedCount: number | null = null;
  failedCount: number | null = null;

  private uploadedFile: File | null = null;

  constructor(private svc: InventoryService) {}

  ngOnInit(): void {
    this.loadLatest();
  }

downloadTemplate() {
  const headers = [
    ['SKU', 'Dealer Cost', 'Drop Date', 'Delisted Date']
  ];

  const worksheet: XLSX.WorkSheet = XLSX.utils.aoa_to_sheet(headers);
  const workbook: XLSX.WorkBook = {
    Sheets: { 'HPC Template': worksheet },
    SheetNames: ['HPC Template']
  };

  const excelBuffer = XLSX.write(workbook, {
    bookType: 'xlsx',
    type: 'array'
  });

  const blob = new Blob(
    [excelBuffer],
    { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' }
  );

  saveAs(blob, 'HPC_Template.xlsx');
}
  // ================= LOAD DATA =================

  loadLatest() {
    this.title = 'View HPC';
    this.exportViewType = 'Latest';

    this.svc.getLatestHpc().subscribe(res => {
      this.bindGrid(res);
    });
  }

  loadDiscrepancy() {
    this.title = 'View HPC Discrepancy';
    this.exportViewType = 'Discrepancies';

    this.svc.getHpcDiscrepancy().subscribe(res => {
      this.bindGrid(res);

      // invalid rows
      this.invalidRows = res.filter((x: any) =>
        x.existInSpire === 'No' ||
        x.spireProdCode !== 'HCC'
      );

      if (this.invalidRows.length) {
        this.invalidColumns = Object.keys(this.invalidRows[0]);
      }
    });
  }

  // ================= GRID BIND =================

  bindGrid(data: any[]) {
    this.model = data || [];
    this.currentPage = 1;
    this.totalPages = Math.ceil(this.model.length / this.pageSize);

    if (this.model.length > 0) {
      this.modelColumns = Object.keys(this.model[0]);
    } else {
      this.modelColumns = [];
    }

    this.applyPagination();
  }

  applyPagination() {
    const start = (this.currentPage - 1) * this.pageSize;
    this.pagedData = this.model.slice(start, start + this.pageSize);
  }

  goToPage(page: number) {
    this.currentPage = page;
    this.applyPagination();
  }

  // ================= FILE UPLOAD =================

  triggerFileInput() {
  const input = document.getElementById('hpcFileInput') as HTMLInputElement;
  if (input) input.click();
}

// onFileUpload remains same
onFileUpload(event: any) {
  debugger
  if (event.target?.files?.length) {
    this.uploadedFile = event.target.files[0];
  }

  if (!this.uploadedFile) return;

  this.svc.uploadHpc(this.uploadedFile).subscribe(res => {
    this.insertedCount = res.result?.InsertedCount ?? 0;
    this.failedCount = res.result?.FailedCount ?? 0;

    // reload latest after upload
    this.loadLatest();
  });
}

  // ================= EXPORT =================

  exportGrid() {
    this.svc.export(this.exportViewType);
  }
}