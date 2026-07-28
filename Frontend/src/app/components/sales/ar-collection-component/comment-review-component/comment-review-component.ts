import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ArCollectionService, CommentReviewSummaryRow, ARCommentEvent, TerritoryGroup } from '../ar-collection.service';
import { SpinnerService } from '../../../shared/spinner/spinner-service';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-comment-review-component',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './comment-review-component.html',
  styleUrl: './comment-review-component.css',
})
export class CommentReviewComponent implements OnInit {
  // Dropdown list
  territoryGroups: TerritoryGroup[] = [];
  
  // Selected filter criteria
  selectedGroup: string = '';
  txtGroupCriteria: string = '';
  txtAgeingDate: string = '';
  txtStartDays: number = 90;

  // Record Navigation State
  summaries: CommentReviewSummaryRow[] = [];
  selectedSummary: CommentReviewSummaryRow | null = null;
  currentIndex: number = -1;

  // Summary Comment (EventType 10)
  txtSummaryComment: string = '';
  txtSummaryCommentDate: string = '';
  txtSummaryCommentUser: string = '';

  // Comment History Log
  commentsHistory: ARCommentEvent[] = [];

  constructor(
    private arService: ArCollectionService,
    private spinner: SpinnerService,
    private toastr: ToastrService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const today = new Date();
    this.txtAgeingDate = this.formatDate(today);

    this.loadTerritoryGroups();
    this.cdr.detectChanges();
  }

  loadTerritoryGroups(): void {
    this.spinner.show();
    this.arService.getTerritoryGroups().subscribe({
      next: (groups) => {
        this.territoryGroups = groups;
        if (groups.length > 0) {
          // Select default group (e.g. Rogers or first available)
          const defaultGrp = groups.find(g => g.groupName.toLowerCase().includes('roger')) || groups[0];
          this.selectedGroup = defaultGrp.groupName;
          this.txtGroupCriteria = defaultGrp.groupCriteria || '';
        }
        this.spinner.hide();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.spinner.hide();
        this.toastr.error('Failed to load group channels.');
        this.cdr.detectChanges();
      }
    });
    this.cdr.detectChanges();
  }

  onGroupChange(): void {
    const groupObj = this.territoryGroups.find(g => g.groupName === this.selectedGroup);
    this.txtGroupCriteria = groupObj ? (groupObj.groupCriteria || '') : '';
    this.cdr.detectChanges();
  }

  generateData(): void {
    if (!this.txtAgeingDate) {
      this.toastr.warning('Please select an Ageing Date.');
      this.cdr.detectChanges();
      return;
    }

    Swal.fire({
      title: 'Generate Data?',
      text: 'Are you sure you want to regenerate cached AR and customer data? This could take a minute.',
      icon: 'question',
      showCancelButton: true,
      confirmButtonText: 'Yes, Generate',
      cancelButtonText: 'Cancel',
      confirmButtonColor: '#000080',
      cancelButtonColor: '#888'
    }).then((result) => {
      if (result.isConfirmed) {
        this.spinner.show();
        this.arService.generateCommentReviewData(this.txtAgeingDate).subscribe({
          next: (success) => {
            this.spinner.hide();
            if (success) {
              this.toastr.success('AR details and customer cached data generated successfully.');
              this.loadSummary();
            } else {
              this.toastr.error('Failed to generate comment review data.');
            }
            this.cdr.detectChanges();
          },
          error: (err) => {
            this.spinner.hide();
            this.toastr.error('Error occurred while generating comment review data.');
            this.cdr.detectChanges();
          }
        });
      }
    });
    this.cdr.detectChanges();
  }

  loadSummary(): void {
    if (this.txtStartDays === null || this.txtStartDays === undefined) {
      this.toastr.warning('Please enter valid Start Days.');
      this.cdr.detectChanges();
      return;
    }

    this.spinner.show();
    this.arService.getCommentReviewSummary(this.txtStartDays, this.txtGroupCriteria).subscribe({
      next: (data) => {
        this.summaries = data;
        if (data.length > 0) {
          this.currentIndex = 0;
          this.onRowSelect(data[0]);
        } else {
          this.currentIndex = -1;
          this.selectedSummary = null;
          this.clearRightPane();
        }
        this.spinner.hide();
        this.toastr.success(`Loaded ${data.length} records.`);
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.spinner.hide();
        this.toastr.error('Failed to load Comment Review summary.');
        this.cdr.detectChanges();
      }
    });
    this.cdr.detectChanges();
  }

  onRowSelect(row: CommentReviewSummaryRow): void {
    this.selectedSummary = row;
    this.clearRightPane();
    
    // 1. Fetch type 10 summary comment
    this.spinner.show();
    this.arService.getSummaryComment(row.groupID).subscribe({
      next: (comment) => {
        if (comment) {
          this.txtSummaryComment = comment.eventText || '';
          
          let displayDate = comment.modDate;
          if (!displayDate || displayDate.toString().startsWith('0001')) {
            displayDate = comment.addDate;
          }
          
          this.txtSummaryCommentDate = displayDate && !displayDate.toString().startsWith('0001') 
            ? this.formatDateTime(new Date(displayDate)) 
            : '';
            
          this.txtSummaryCommentUser = comment.modUser || comment.addUser || '';
        } else {
          this.txtSummaryComment = '';
          this.txtSummaryCommentDate = '';
          this.txtSummaryCommentUser = '';
        }
        this.cdr.detectChanges();
      },
      error: () => {
        this.toastr.error('Failed to load summary comment.');
        this.cdr.detectChanges();
      }
    });

    // 2. Fetch history log
    const selectBy = row.arType === 'Single' ? 1 : 2;
    this.arService.getEvents(row.groupID, selectBy).subscribe({
      next: (events) => {
        // Filter out eventType 10 from history log (VBA Parity)
        this.commentsHistory = events.filter(e => e.eventType !== 10);
        this.spinner.hide();
        this.cdr.detectChanges();
      },
      error: () => {
        this.spinner.hide();
        this.toastr.error('Failed to load comment history.');
        this.cdr.detectChanges();
      }
    });

    this.cdr.detectChanges();
  }

  moveNext(): void {
    if (this.summaries.length > 0 && this.currentIndex < this.summaries.length - 1) {
      this.currentIndex++;
      this.onRowSelect(this.summaries[this.currentIndex]);
    }
    this.cdr.detectChanges();
  }

  movePrevious(): void {
    if (this.summaries.length > 0 && this.currentIndex > 0) {
      this.currentIndex--;
      this.onRowSelect(this.summaries[this.currentIndex]);
    }
    this.cdr.detectChanges();
  }

  moveFirst(): void {
    if (this.summaries.length > 0) {
      this.currentIndex = 0;
      this.onRowSelect(this.summaries[0]);
    }
    this.cdr.detectChanges();
  }

  moveLast(): void {
    if (this.summaries.length > 0) {
      this.currentIndex = this.summaries.length - 1;
      this.onRowSelect(this.summaries[this.currentIndex]);
    }
    this.cdr.detectChanges();
  }

  updateSummaryComment(): void {
    if (!this.selectedSummary) {
      this.toastr.warning('Please select a customer first.');
      this.cdr.detectChanges();
      return;
    }

    this.spinner.show();
    this.arService.saveSummaryComment(
      this.selectedSummary.groupID,
      this.selectedSummary.arType,
      this.txtSummaryComment
    ).subscribe({
      next: (success) => {
        this.spinner.hide();
        if (success) {
          this.toastr.success('Summary comment updated successfully.');
          // Refresh details
          this.onRowSelect(this.selectedSummary!);
        } else {
          this.toastr.error('Failed to update summary comment.');
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.spinner.hide();
        this.toastr.error('Error updating summary comment.');
        this.cdr.detectChanges();
      }
    });
    this.cdr.detectChanges();
  }

  exportExcel(): void {
    if (this.txtStartDays === null || this.txtStartDays === undefined) {
      this.toastr.warning('Please enter valid Start Days.');
      this.cdr.detectChanges();
      return;
    }

    this.spinner.show();
    this.arService.exportSummaryComments(this.txtStartDays, this.txtGroupCriteria).subscribe({
      next: (blob) => {
        this.spinner.hide();
        const fileName = `SummaryComments_${this.formatDate(new Date())}.xlsx`;
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);
        this.toastr.success('Export completed successfully.');
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.spinner.hide();
        this.toastr.error('Failed to export summary comments to Excel.');
        this.cdr.detectChanges();
      }
    });
    this.cdr.detectChanges();
  }

  clearRightPane(): void {
    this.txtSummaryComment = '';
    this.txtSummaryCommentDate = '';
    this.txtSummaryCommentUser = '';
    this.commentsHistory = [];
    this.cdr.detectChanges();
  }

  formatDate(date: Date): string {
    const yyyy = date.getFullYear();
    const mm = String(date.getMonth() + 1).padStart(2, '0');
    const dd = String(date.getDate()).padStart(2, '0');
    return `${yyyy}-${mm}-${dd}`;
  }

  formatDateTime(date: Date): string {
    const yyyy = date.getFullYear();
    const mm = String(date.getMonth() + 1).padStart(2, '0');
    const dd = String(date.getDate()).padStart(2, '0');
    const hh = String(date.getHours()).padStart(2, '0');
    const min = String(date.getMinutes()).padStart(2, '0');
    return `${yyyy}-${mm}-${dd} ${hh}:${min}`;
  }
}
