import { Component, ElementRef, OnInit, ViewChild, AfterViewInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { InventoryService } from '../add-inventory-component/inventory-service';
import { CommonModule, NgIf, NgForOf } from '@angular/common';
import { ChangeDetectorRef } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { SpinnerService } from '../../shared/spinner/spinner-service';
import { finalize } from 'rxjs/operators'; // Finalize import kiya hai spinner stop karne ke liye

declare var bootstrap: any;

@Component({
  selector: 'app-modify-inventory-component',
  imports: [CommonModule, FormsModule, NgIf, NgForOf],
  standalone: true,
  templateUrl: './modify-inventory-component.html',
  styleUrls: ['./modify-inventory-component.css'],
})
export class ModifyInventoryComponent implements OnInit, AfterViewInit {

  @ViewChild('priceModal') priceModal!: ElementRef;
  private modalInstance: any;

  inventoryItems: any[] = [];
  allWarehouses: any[] = [];
  searchTerm: string = '';

  // modal fields
  modalInvId: number | null = null;
  modalUomId: number = 0;
  modalWhse: string = '';
  modalPartNo: string = '';
  modalCurrentCost: number = 0;
  modalAverageCost: number = 0;
  modalSellPrice: number = 0;
  applyToAll: boolean = false;

  currentPage: number = 1;
  pageSize: number = 10;
  totalItems: number = 0;
  totalPages: number = 0;

  constructor(
    private http: HttpClient, 
    private inventoryService: InventoryService,
    private toaster: ToastrService,
    private spinner: SpinnerService, // Spinner Inject
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadInventory();
  }

  ngAfterViewInit(): void {
    setTimeout(() => {
      this.modalInstance = new bootstrap.Modal(this.priceModal.nativeElement, {
        backdrop: 'static',
        keyboard: false
      });
    }, 300);
  }

  fillModal(invId: number, whse: string, partNo: string,
            current: number, avg: number, sell: number, uomId: number) {
    this.modalInvId = invId;
    this.modalUomId = uomId || 0;
    this.modalWhse = whse || '';
    this.modalPartNo = partNo || '';
    this.modalCurrentCost = Number(current) || 0;
    this.modalAverageCost = Number(avg) || 0;
    this.modalSellPrice = Number(sell) || 0;
    this.applyToAll = false;

    this.getAllWarehouses(this.modalPartNo, this.modalWhse);
    this.cdr.detectChanges();

    requestAnimationFrame(() => {
      if (this.modalInstance) {
        this.modalInstance.show();
      }
    });
  }

  clearModalFields() {
    this.modalInvId = null;
    this.modalUomId = 0;
    this.modalWhse = '';
    this.modalPartNo = '';
    this.modalCurrentCost = 0;
    this.modalAverageCost = 0;
    this.modalSellPrice = 0;
    this.applyToAll = false;
  }

  loadInventory(page: number = 1) {
    if (page < 1) page = 1;
    this.currentPage = page;

    this.spinner.show(); // 🔄 Start Spinner
    this.inventoryService.getModifyInventoryList(this.searchTerm, this.currentPage, this.pageSize)
      .pipe(finalize(() => this.spinner.hide())) // ⏹ Stop Spinner chahe success ho ya error
      .subscribe({
        next: (res) => {
          this.inventoryItems = res.inventoryItems || [];
          this.totalItems = res.totalItems || 0;
          this.totalPages = this.totalItems > 0 ? Math.ceil(this.totalItems / this.pageSize) : 0;
          this.cdr.detectChanges();
        },
        error: () => {
          this.toaster.error("Failed to load inventory");
        }
      });
  }

  getAllWarehouses(partNo: string, skipWhse: string) {
    this.spinner.show(); // 🔄 Start Spinner
    this.inventoryService
      .getAllWarehousesForPart(partNo, skipWhse)
      .pipe(finalize(() => this.spinner.hide())) // ⏹ Stop Spinner
      .subscribe({
        next: (res) => {
          this.allWarehouses = res;
          this.cdr.detectChanges();
        }
      });
  }

  submitForm() {
    if (!this.validateAllFields()) return;

    const payload = {
      partNo: this.modalPartNo,
      whse: this.modalWhse,
      currentCost: this.modalCurrentCost,
      averageCost: this.modalAverageCost,
      sellPrice: this.modalSellPrice,
      uomId: this.modalUomId
    };

    this.spinner.show(); // 🔄 Start Spinner
    this.inventoryService
      .updateInventoryPrice(payload, this.applyToAll)
      .pipe(finalize(() => this.spinner.hide())) // ⏹ Stop Spinner
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.toaster.success(res.message);
            if (this.modalInstance) this.modalInstance.hide(); // ✅ Modal close on success
            this.loadInventory();
          } else {
            this.toaster.error(res.message);
          }
        },
        error: () => {
          this.toaster.error("Something went wrong!");
        }
      });
  }

  // VALIDATION METHODS (Same as your logic)
  validateField(id: string, value: number): boolean {
    const field = document.getElementById(id);
    const error = document.getElementById(id + 'Error');
    field?.classList.remove('is-invalid');
    if (error) error.innerHTML = '';

    if (value < 0 || value === null || isNaN(value)) {
      field?.classList.add('is-invalid');
      if (error) {
        error.innerHTML = `<i class="fas fa-exclamation-circle me-1"></i>Value cannot be negative`;
      }
      return false;
    }
    return true;
  }

  validateAllFields(): boolean {
    let valid = true;
    valid = this.validateField('modalCurrentCost', this.modalCurrentCost) && valid;
    valid = this.validateField('modalAverageCost', this.modalAverageCost) && valid;
    valid = this.validateField('modalSellPrice', this.modalSellPrice) && valid;

    if (this.modalCurrentCost !== this.modalAverageCost) {
      this.showError('modalCurrentCost', 'Current Cost and Average Cost must be the same');
      this.showError('modalAverageCost', 'Current Cost and Average Cost must be the same');
      valid = false;
    }
    return valid;
  }

  showError(id: string, message: string) {
    const field = document.getElementById(id);
    const error = document.getElementById(id + 'Error');
    field?.classList.add('is-invalid');
    if (error) {
      error.innerHTML = `<i class="fas fa-exclamation-circle me-1"></i>${message}`;
    }
  }

  get pages(): number[] {
    const maxVisiblePages = 5;
    const pages: number[] = [];
    if (this.totalPages <= maxVisiblePages) {
      for (let i = 1; i <= this.totalPages; i++) pages.push(i);
    } else {
      const startPage = Math.max(1, this.currentPage - 2);
      const endPage = Math.min(this.totalPages, this.currentPage + 2);
      if (startPage > 1) {
        pages.push(1);
        if (startPage > 2) pages.push(-1);
      }
      for (let i = startPage; i <= endPage; i++) pages.push(i);
      if (endPage < this.totalPages) {
        if (endPage < this.totalPages - 1) pages.push(-1);
        pages.push(this.totalPages);
      }
    }
    return pages;
  }
}