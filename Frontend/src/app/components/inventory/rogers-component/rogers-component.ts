import { SpinnerService } from '../../shared/spinner/spinner-service';
import { ToastrService } from 'ngx-toastr';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { RogersarSpireService } from '../rogersar-spire.service';
import { PaginationComponent } from '../../shared/pagination/pagination.component';
import { DateFormatPipe } from '../../shared/pipes/date-format-pipe';

@Component({
  selector: 'app-rogers-component',
  imports: [FormsModule, CommonModule, PaginationComponent,DateFormatPipe],
  templateUrl: './rogers-component.html',
  styleUrl: './rogers-component.css',
})
export class RogersComponent implements OnInit {
  arData: any[] = [];
  searchTerm: string = '';
  useMock: boolean = false; // Mock data disabled - fetching from SQL

  // Pagination
  currentPage: number = 1;
  pageSize: number = 10;
  totalItems: number = 0;
  totalPages: number = 0;

  constructor(
    private service: RogersarSpireService,
    private spinner: SpinnerService,
    private toastr: ToastrService
  ) { }

  ngOnInit(): void {
    // this.fetchData();
  }

  fetchData(page: number = 1) {
    this.currentPage = page;
    this.spinner.show();

    this.service.getARData(this.searchTerm, this.currentPage, this.pageSize).subscribe({
      next: (res) => {
        this.arData = res.items || [];
        this.totalItems = res.totalItems || 0;
        this.totalPages = res.totalPages || 0;
        this.spinner.hide();
      },
      error: (err) => {
        this.toastr.error('Error fetching AR data');
        this.spinner.hide();
      }
    });
  }

convertToISO(dateStr: string | null): string | null {
  if (!dateStr) return null;

  const parts = dateStr.split('-');
  if (parts.length !== 3) return null;

  const [month, day, year] = parts;

  return `${year}-${month}-${day}T00:00:00`;
}

  onSearch() {
    this.fetchData(1);
  }

loadAR() {
  this.currentPage = 1;     
  this.pageSize = 10;       

  this.spinner.show();

  this.service.loadARData(this.currentPage, this.pageSize).subscribe({
    next: (res) => {
      this.toastr.success('AR Data loaded successfully');

      this.arData = res.items || [];
      this.totalItems = res.totalItems || 0;
      this.totalPages = res.totalPages || 0;

      this.spinner.hide();
    },
    error: () => {
      this.toastr.error('Error loading AR data from Spire');
      this.spinner.hide();
    }
  });
}

 updateItem(item: any) {

  const payload = {
    ...item,
    sentOn: this.convertToISO(item.sentOn),
    paymentDate: this.convertToISO(item.paymentDate)
  };

  this.service.updateARData(payload).subscribe({
    next: () => {},
    error: (err) => {
      this.toastr.error('Failed to save changes');
    }
  });
}

  exportExcel() {
    this.spinner.show();
    this.service.exportToExcel().subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `RogersAR_${new Date().toISOString().slice(0, 10)}.xlsx`;
        a.click();
        window.URL.revokeObjectURL(url);
        this.spinner.hide();
        this.toastr.success('Export successful');
      },
      error: (err) => {
        this.toastr.error('Error exporting data');
        this.spinner.hide();
      }
    });
  }
}


