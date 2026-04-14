import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-pagination',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './pagination.component.html',
  styleUrls: []
})
export class PaginationComponent implements OnChanges {
  @Input() currentPage: number = 1;
  @Input() totalPages: number = 1;
  @Input() pageSize: number = 10;
  @Input() totalItems: number = 0;
  @Output() pageChanged = new EventEmitter<number>();

  pages: number[] = [];

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['currentPage'] || changes['totalPages']) {
      this.generatePages();
    }
  }

  generatePages() {
    const maxVisiblePages = 5;
    const pages: number[] = [];
    
    if (this.totalPages <= maxVisiblePages) {
      for (let i = 1; i <= this.totalPages; i++) pages.push(i);
    } else {
      const startPage = Math.max(1, this.currentPage - 2);
      const endPage = Math.min(this.totalPages, this.currentPage + 2);
      
      if (startPage > 1) {
        pages.push(1);
        if (startPage > 2) pages.push(-1); // Ellipsis
      }
      
      for (let i = startPage; i <= endPage; i++) pages.push(i);
      
      if (endPage < this.totalPages) {
        if (endPage < this.totalPages - 1) pages.push(-1); // Ellipsis
        pages.push(this.totalPages);
      }
    }
    this.pages = pages;
  }

  changePage(page: number) {
    if (page >= 1 && page <= this.totalPages && page !== this.currentPage) {
      this.pageChanged.emit(page);
    }
  }
}
