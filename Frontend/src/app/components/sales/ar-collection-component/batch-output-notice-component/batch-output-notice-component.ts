
import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { SpinnerService } from '../../../shared/spinner/spinner-service';
import Swal from 'sweetalert2';
import { ArCollectionService, TerritoryGroup, BatchNoticeSummaryRow, BatchNoticeDetailRow } from '../ar-collection.service';

@Component({
  selector: 'app-batch-output-notice-component',
  standalone: true,
  imports: [CommonModule, FormsModule, HttpClientModule],
  providers: [DatePipe],
  templateUrl: './batch-output-notice-component.html',
  styleUrl: './batch-output-notice-component.css'
})
export class BatchOutputNoticeComponent implements OnInit {
  agingDate: string = '';
  startDays: number = 30;
  endDays: number = 60;
  selectedNoticeType: string = '';
  noticeTypes: string[] = ['First Notice', 'Second Notice', 'Final Notice', 'Comment Review'];
  
  territoryGroups: TerritoryGroup[] = [];
  selectedChannelId: string = '';
  
  summaryData: BatchNoticeSummaryRow[] = [];
  detailData: BatchNoticeDetailRow[] = [];
  
  // Pagination State
  summaryPage: number = 1;
  summaryPageSize: number = 50;
  
  detailPage: number = 1;
  detailPageSize: number = 50;
  
  selectedGroupIds: Set<string> = new Set<string>();

  constructor(
    private arService: ArCollectionService,
    private spinner: SpinnerService,
    private cdr: ChangeDetectorRef,
    private datePipe: DatePipe
  ) {
    this.agingDate = this.datePipe.transform(new Date(), 'yyyy-MM-dd') || '';
  }

  ngOnInit(): void {
    this.loadTerritoryGroups();
  }

  loadTerritoryGroups(): void {
    this.arService.getTerritoryGroups().subscribe({
      next: (groups) => {
        this.territoryGroups = groups;
      },
      error: (err) => {
        console.error('Failed to load territory groups', err);
      }
    });
  }

  getGroupName(): string {
    if (!this.selectedChannelId) return 'All Channels';
    const group = this.territoryGroups.find(g => g.id.toString() === this.selectedChannelId);
    return group ? group.groupName : '';
  }

  getGroupCriteria(): string {
    if (!this.selectedChannelId) {
      return ''; // No filter means all channels
    } else {
      const group = this.territoryGroups.find(g => g.id.toString() === this.selectedChannelId);
      return group && group.groupCriteria ? group.groupCriteria : '';
    }
  }

  searchTerm: string = '';

  // Filtered lists
  get filteredSummaryData(): BatchNoticeSummaryRow[] {
    if (!this.searchTerm.trim()) {
      return this.summaryData;
    }
    const term = this.searchTerm.toLowerCase().trim();
    return this.summaryData.filter(row => 
      row.groupID.toLowerCase().includes(term) || 
      (row.customerName && row.customerName.toLowerCase().includes(term))
    );
  }

  get filteredDetailData(): BatchNoticeDetailRow[] {
    return this.detailData.filter(row => this.selectedGroupIds.has(row.groupID));
  }

  // Pagination Getters
  get pagedSummaryData(): BatchNoticeSummaryRow[] {
    const start = (this.summaryPage - 1) * this.summaryPageSize;
    return this.filteredSummaryData.slice(start, start + this.summaryPageSize);
  }

  get totalSummaryPages(): number {
    return Math.ceil(this.filteredSummaryData.length / this.summaryPageSize);
  }

  get pagedDetailData(): BatchNoticeDetailRow[] {
    const start = (this.detailPage - 1) * this.detailPageSize;
    return this.filteredDetailData.slice(start, start + this.detailPageSize);
  }

  get totalDetailPages(): number {
    return Math.ceil(this.filteredDetailData.length / this.detailPageSize);
  }

  // Selection Actions
  isAllSummarySelected(): boolean {
    const currentList = this.filteredSummaryData;
    return currentList.length > 0 && currentList.every(row => this.selectedGroupIds.has(row.groupID));
  }

  toggleAllSummary(event: any): void {
    const currentList = this.filteredSummaryData;
    if (event.target.checked) {
      currentList.forEach(row => this.selectedGroupIds.add(row.groupID));
    } else {
      currentList.forEach(row => this.selectedGroupIds.delete(row.groupID));
    }
    this.detailPage = 1;
  }

  onSearchTermChange(): void {
    this.summaryPage = 1;
    this.detailPage = 1;
  }

  // Pagination Actions
  nextSummaryPage() { if (this.summaryPage < this.totalSummaryPages) this.summaryPage++; }
  prevSummaryPage() { if (this.summaryPage > 1) this.summaryPage--; }
  
  nextDetailPage() { if (this.detailPage < this.totalDetailPages) this.detailPage++; }
  prevDetailPage() { if (this.detailPage > 1) this.detailPage--; }

  onGenerateData(): void {
    if (!this.agingDate) {
      Swal.fire('Warning', 'Please select an Ageing Date.', 'warning');
      return;
    }
    this.spinner.show();
    this.arService.generateBatchNoticeData(this.agingDate).subscribe({
      next: () => {
        this.spinner.hide();
        Swal.fire('Success', 'Data Generated.', 'success');
      },
      error: (err) => {
        this.spinner.hide();
        Swal.fire('Error', err.error || 'Failed to generate data.', 'error');
      }
    });
  }

  onShowData(): void {
    if (!this.startDays || !this.endDays) {
      Swal.fire('Warning', 'Enter a range for number of days.', 'warning');
      return;
    }
    if (!this.selectedNoticeType) {
      Swal.fire('Warning', 'Select a Notice Type.', 'warning');
      return;
    }

    this.spinner.show();
    const criteria = this.getGroupCriteria();
debugger
    this.arService.getBatchNoticeSummary(criteria, this.startDays, this.endDays, this.selectedNoticeType).subscribe({
      next: (summary) => {
        debugger
        this.summaryData = summary;
        this.selectedGroupIds.clear(); // Reset selections
        this.summaryPage = 1;
        this.summaryData.forEach(row => this.selectedGroupIds.add(row.groupID));

        this.arService.getBatchNoticeDetail(criteria, this.startDays, this.endDays, this.selectedNoticeType).subscribe({
          next: (detail) => {
            this.detailData = detail;
            this.detailPage = 1;
            this.spinner.hide();
          },
          error: (err) => {
            this.spinner.hide();
            Swal.fire('Error', err.error || 'Failed to load details.', 'error');
          }
        });
      },
      error: (err) => {
        this.spinner.hide();
        Swal.fire('Error', err.error || 'Failed to load summary.', 'error');
      }
    });
  }

  toggleGroupSelection(groupID: string, event: any): void {
    if (event.target.checked) {
      this.selectedGroupIds.add(groupID);
    } else {
      this.selectedGroupIds.delete(groupID);
    }
    this.detailPage = 1;
  }

  onOutputNotice(): void {
    if (this.selectedNoticeType === 'Comment Review') {
      Swal.fire('Info', 'Output Notice is disabled for Comment Review.', 'info');
      return;
    }
    if (this.selectedGroupIds.size === 0) {
      Swal.fire('Warning', 'You must select at least one customer/group from the summary.', 'warning');
      return;
    }

    this.spinner.show();
    const criteria = this.getGroupCriteria();
    const selectedArr = Array.from(this.selectedGroupIds);

    this.arService.outputBatchNotices(selectedArr, this.selectedNoticeType, this.startDays, this.endDays, criteria).subscribe({
      next: (blob) => {
        this.spinner.hide();
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `BatchNotices_${new Date().getTime()}.zip`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);

        Swal.fire('Success', 'Notice Output Complete.', 'success');
      },
      error: (err) => {
        this.spinner.hide();
        Swal.fire('Error', 'Failed to output notices.', 'error');
      }
    });
  }
}
