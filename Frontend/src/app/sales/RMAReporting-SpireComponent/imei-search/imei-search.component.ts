import { Component, OnInit, Inject, PLATFORM_ID, ChangeDetectorRef } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ImeiSearchService, IMEISearchResponse, RMAResult, RogersResponseItem, RogersReportCMRMAItem } from './imei-search.service';
import { SpinnerService } from '../../../components/shared/spinner/spinner-service';
import { Spinner } from '../../../components/shared/spinner/spinner';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-imei-search',
  standalone: true,
  imports: [CommonModule, FormsModule, Spinner],
  templateUrl: './imei-search.component.html',
  styleUrls: ['./imei-search.component.css']
})
export class ImeiSearchComponent implements OnInit {
  criteriaOptions: string[] = ['IMEI', 'receive waybill', 'return waybill'];
  selectedCriteria: string = 'IMEI';
  searchQuery: string = '';

  errorMessage: string = '';

  // Data sets
  rmaResults: RMAResult[] = [];
  rogersResponses: RogersResponseItem[] = [];
  cmRmaResults: RogersReportCMRMAItem[] = [];

  // Selected row indices
  selectedRmaIndex: number = 0;
  selectedResponseIndex: number = 0;
  selectedCmRmaIndex: number = 0;

  // Search filter text within each subform
  rmaFilterText: string = '';
  responseFilterText: string = '';
  cmRmaFilterText: string = '';

  // Double-clicked modal / detail viewer
  viewingResponseDetail: RogersResponseItem | null = null;
  showDetailModal: boolean = false;

  constructor(
    private imeiService: ImeiSearchService,
    public spinnerService: SpinnerService,
    private cdr: ChangeDetectorRef,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {}

  ngOnInit(): void {
    // Ready for user search input
  }

  onCriteriaChange(): void {
    this.searchQuery = '';
    this.clearGrids();
    this.cdr.detectChanges();
  }

  onSearchQueryChange(): void {
    if (!this.searchQuery || !this.searchQuery.trim()) {
      this.clearGrids();
      this.cdr.detectChanges();
    }
  }

  clearGrids(): void {
    this.rmaResults = [];
    this.rogersResponses = [];
    this.cmRmaResults = [];
    this.selectedRmaIndex = 0;
    this.selectedResponseIndex = 0;
    this.selectedCmRmaIndex = 0;
    this.errorMessage = '';
    this.rmaFilterText = '';
    this.responseFilterText = '';
    this.cmRmaFilterText = '';
  }

  executeSearch(): void {
    if (!this.searchQuery || !this.searchQuery.trim()) {
      this.clearGrids();
      this.cdr.detectChanges();
      Swal.fire({
        icon: 'warning',
        title: 'Input Required',
        text: `Please enter a valid ${this.selectedCriteria} to search.`
      });
      return;
    }

    this.errorMessage = '';
    this.spinnerService.show();

    this.imeiService.search(this.selectedCriteria, this.searchQuery.trim()).subscribe({
      next: (data: IMEISearchResponse) => {
        this.rmaResults = data.rmaResults || [];
        this.rogersResponses = data.rogersResponses || [];
        this.cmRmaResults = data.cmRmaResults || [];
        this.selectedRmaIndex = 0;
        this.selectedResponseIndex = 0;
        this.selectedCmRmaIndex = 0;
        this.spinnerService.hide();
        this.cdr.detectChanges();

        if (this.rmaResults.length === 0 && this.rogersResponses.length === 0 && this.cmRmaResults.length === 0) {
          Swal.fire({
            icon: 'info',
            title: 'No Records Found',
            text: `No matching records found for ${this.selectedCriteria}: ${this.searchQuery}`
          });
        }
      },
      error: (err) => {
        this.errorMessage = 'Failed to fetch search results from server.';
        this.spinnerService.hide();
        this.cdr.detectChanges();
        console.error(err);
        Swal.fire({
          icon: 'error',
          title: 'Search Error',
          text: 'Failed to fetch search results from server.'
        });
      }
    });
  }

  exportToExcel(): void {
    if (this.rmaResults.length === 0 && this.rogersResponses.length === 0 && this.cmRmaResults.length === 0) {
      Swal.fire({
        icon: 'warning',
        title: 'No Data',
        text: 'No search data to export. Please perform a search first.'
      });
      return;
    }

    import('xlsx').then(XLSX => {
      const wb = XLSX.utils.book_new();

      if (this.rmaResults.length > 0) {
        const wsRma = XLSX.utils.json_to_sheet(this.rmaResults);
        XLSX.utils.book_append_sheet(wb, wsRma, 'RMA Results');
      }

      if (this.rogersResponses.length > 0) {
        const wsResp = XLSX.utils.json_to_sheet(this.rogersResponses);
        XLSX.utils.book_append_sheet(wb, wsResp, 'Rogers Responses');
      }

      if (this.cmRmaResults.length > 0) {
        const wsCmRma = XLSX.utils.json_to_sheet(this.cmRmaResults);
        XLSX.utils.book_append_sheet(wb, wsCmRma, 'CMRMA Results');
      }

      const fileName = `IMEI_Search_${this.selectedCriteria}_${new Date().toISOString().slice(0, 10)}.xlsx`;
      XLSX.writeFile(wb, fileName);
    });
  }

  selectRmaRow(index: number): void {
    this.selectedRmaIndex = index;
  }

  onRmaRowDoubleClick(item: RMAResult): void {
    const matchingResponse = this.rogersResponses.find(r => r.imei === item.imei) || this.rogersResponses[0];
    if (matchingResponse) {
      this.viewingResponseDetail = matchingResponse;
      this.showDetailModal = true;
    }
  }

  selectResponseRow(index: number): void {
    this.selectedResponseIndex = index;
  }

  selectCmRmaRow(index: number): void {
    this.selectedCmRmaIndex = index;
  }

  // Navigation handlers for Grid 1 (RMA)
  navFirstRma(): void {
    if (this.rmaResults.length > 0) this.selectedRmaIndex = 0;
  }
  navPrevRma(): void {
    if (this.selectedRmaIndex > 0) this.selectedRmaIndex--;
  }
  navNextRma(): void {
    if (this.selectedRmaIndex < this.rmaResults.length - 1) this.selectedRmaIndex++;
  }
  navLastRma(): void {
    if (this.rmaResults.length > 0) this.selectedRmaIndex = this.rmaResults.length - 1;
  }

  // Navigation handlers for Grid 2 (Responses)
  navFirstResponse(): void {
    if (this.rogersResponses.length > 0) this.selectedResponseIndex = 0;
  }
  navPrevResponse(): void {
    if (this.selectedResponseIndex > 0) this.selectedResponseIndex--;
  }
  navNextResponse(): void {
    if (this.selectedResponseIndex < this.rogersResponses.length - 1) this.selectedResponseIndex++;
  }
  navLastResponse(): void {
    if (this.rogersResponses.length > 0) this.selectedResponseIndex = this.rogersResponses.length - 1;
  }

  // Navigation handlers for Grid 3 (CMRMA)
  navFirstCmRma(): void {
    if (this.cmRmaResults.length > 0) this.selectedCmRmaIndex = 0;
  }
  navPrevCmRma(): void {
    if (this.selectedCmRmaIndex > 0) this.selectedCmRmaIndex--;
  }
  navNextCmRma(): void {
    if (this.selectedCmRmaIndex < this.cmRmaResults.length - 1) this.selectedCmRmaIndex++;
  }
  navLastCmRma(): void {
    if (this.cmRmaResults.length > 0) this.selectedCmRmaIndex = this.cmRmaResults.length - 1;
  }

  closeDetailModal(): void {
    this.showDetailModal = false;
    this.viewingResponseDetail = null;
  }

  formatDate(dateVal: any): string {
    if (!dateVal) return '';
    try {
      const d = new Date(dateVal);
      if (isNaN(d.getTime())) return String(dateVal);
      return `${d.getMonth() + 1}/${d.getDate()}/${d.getFullYear()}`;
    } catch {
      return String(dateVal);
    }
  }

  formatCurrency(val: any): string {
    if (val === null || val === undefined || isNaN(Number(val))) return '';
    return '$' + Number(val).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }
}
