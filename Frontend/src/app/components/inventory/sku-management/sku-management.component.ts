import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SkuService, SkuVM } from '../sku.service';
import { ToastrService } from 'ngx-toastr';
import { SpinnerService } from '../../shared/spinner/spinner-service';
import { PaginationComponent } from '../../shared/pagination/pagination.component';
import { Spinner } from '../../shared/spinner/spinner';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-sku-management',
  standalone: true,
  imports: [CommonModule, FormsModule, PaginationComponent, Spinner],
  templateUrl: './sku-management.component.html',
  styleUrl: './sku-management.component.css'
})
export class SkuManagementComponent implements OnInit {
  // ─── All data from server ─────────────────────────────────────────────────
  allSkus: SkuVM[] = [];

  // ─── Paginated slice shown in the table ───────────────────────────────────
  pagedSkus: SkuVM[] = [];

  // ─── Add form ─────────────────────────────────────────────────────────────
  newSku: SkuVM = { sku: '', type: 'Hardware' };
  types = ['Hardware', 'Accessory'];

  // ─── Pagination state ─────────────────────────────────────────────────────
  currentPage = 1;
  pageSize = 10;
  totalPages = 1;
  totalItems = 0;

  constructor(
    private skuService: SkuService,
    private toastr: ToastrService,
    private spinner: SpinnerService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadSkus();
  }

  // ─── Load ─────────────────────────────────────────────────────────────────

  loadSkus(): void {
    this.spinner.show();
    this.cdr.detectChanges();

    this.skuService.getSkus().subscribe({
      next: (data) => {
        this.allSkus = data;
        this.applyPagination();
        this.spinner.hide();
        this.cdr.detectChanges();
      },
      error: () => {
        this.toastr.error('Failed to load SKUs');
        this.spinner.hide();
        this.cdr.detectChanges();
      }
    });
  }

  // ─── Pagination helpers ───────────────────────────────────────────────────

  applyPagination(): void {
    this.totalItems = this.allSkus.length;
    this.totalPages = Math.max(1, Math.ceil(this.totalItems / this.pageSize));

    if (this.currentPage > this.totalPages) {
      this.currentPage = this.totalPages;
    }

    const start = (this.currentPage - 1) * this.pageSize;
    this.pagedSkus = this.allSkus.slice(start, start + this.pageSize);
    this.cdr.detectChanges();
  }

  onPageChanged(page: number): void {
    this.currentPage = page;
    this.applyPagination();
  }

  // ─── CRUD ─────────────────────────────────────────────────────────────────

  addSku(): void {
    if (!this.newSku.sku || !this.newSku.type) {
      this.toastr.warning('Please enter SKU and Type');
      return;
    }

    this.spinner.show();
    this.cdr.detectChanges();

    this.skuService.addSku(this.newSku).subscribe({
      next: (success) => {
        this.spinner.hide();
        if (success) {
          this.toastr.success('SKU added successfully');
          this.newSku = { sku: '', type: 'Hardware' };
          this.loadSkus();
        } else {
          this.toastr.error('Failed to add SKU');
        }
        this.cdr.detectChanges();
      },
      error: () => {
        this.spinner.hide();
        this.toastr.error('Error adding SKU');
        this.cdr.detectChanges();
      }
    });
  }

  updateSku(sku: SkuVM): void {
    this.skuService.updateSku(sku).subscribe({
      next: (success) => {
        if (success) {
          this.toastr.success('SKU updated');
          this.loadSkus();
        }
        this.cdr.detectChanges();
      },
      error: () => {
        this.toastr.error('Error updating SKU');
        this.cdr.detectChanges();
      }
    });
  }

  deleteSku(sku: string): void {
    Swal.fire({
      title: 'Are you sure?',
      text: 'Do you want to delete this SKU?',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      confirmButtonText: 'Yes, delete it!'
    }).then((result) => {
      if (result.isConfirmed) {
        this.spinner.show();
        this.cdr.detectChanges();

        this.skuService.deleteSku(sku).subscribe({
          next: (success) => {
            this.spinner.hide();
            if (success) {
              this.toastr.success('SKU deleted');
              this.loadSkus();
            }
            this.cdr.detectChanges();
          },
          error: () => {
            this.spinner.hide();
            this.toastr.error('Error deleting SKU');
            this.cdr.detectChanges();
          }
        });
      }
    });
  }
}
