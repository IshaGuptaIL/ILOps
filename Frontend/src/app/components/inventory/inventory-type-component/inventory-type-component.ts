import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ToastrService } from 'ngx-toastr';
import { delay } from 'rxjs/operators';
import { InventoryService } from '../add-inventory-component/inventory-service';

@Component({
  selector: 'app-inventory-type-component',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './inventory-type-component.html',
  styleUrl: './inventory-type-component.css',
})
export class InventoryTypeComponent implements OnInit {
  addForm: FormGroup;
  editForm: FormGroup;
  
  dataList: any[] = [];
  currentType: string = 'HCC';
  currentPage: number = 1;
  pageSize: number = 10;
  totalCount: number = 0;
  loading: boolean = false;

  constructor(
    private fb: FormBuilder,
    private inventoryService: InventoryService,
    private toastr: ToastrService,
    private cdr: ChangeDetectorRef // CDR Inject kiya gaya
  ) {
    this.addForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(100)]]
    });

    this.editForm = this.fb.group({
      id: [null],
      name: ['', [Validators.required, Validators.maxLength(100)]]
    });
  }

  ngOnInit(): void {
    // Initial Load
    this.loadGrid();

    // Auto UpperCase logic (Jaise aapne ngOnInit mein manga tha)
    this.addForm.get('name')?.valueChanges.subscribe((v: string) => {
      if (v && v !== v.toUpperCase()) {
        this.addForm.patchValue({ name: v.toUpperCase() }, { emitEvent: false });
      }
    });
  }

  onTypeChange(type: string): void {
    this.currentType = type;
    this.currentPage = 1;
    this.loadGrid();
  }

  loadGrid(): void {
    this.loading = true;
    
    // delay(0) aur cdr.detectChanges() ka use data smooth load karne ke liye
    this.inventoryService.getInventoryTypes(this.currentType, this.currentPage, this.pageSize)
      .pipe(delay(0)) 
      .subscribe({
        next: (res: any) => {
          this.dataList = res.data || [];
          this.totalCount = res.totalCount || 0;
          this.loading = false;
          this.cdr.detectChanges(); // UI Update refresh
        },
        error: () => {
          this.toastr.error('Failed to load data');
          this.loading = false;
          this.cdr.detectChanges();
        }
      });
  }

  onAdd(): void {
    if (this.addForm.invalid) {
      this.addForm.markAllAsTouched();
      return;
    }

    const payload = {
      name: this.addForm.value.name.trim(),
      inventoryType: this.currentType // Backend property match karein (InventoryType/TableType)
    };

    this.inventoryService.addGroup(payload).subscribe({
      next: (res: any) => {
        if (res.success) {
          this.toastr.success('Added successfully');
          this.addForm.reset();
          this.loadGrid();
        }
      },
      error: () => this.toastr.error('Add failed')
    });
  }

  openEdit(item: any): void {
    this.editForm.patchValue({
      id: item.id,
      name: item.name
    });
    
    // Modal open logic
    const modal = document.getElementById('editModal');
    if (modal) {
      (modal as any).classList.add('show', 'd-block');
      this.cdr.detectChanges();
    }
  }

  closeEdit(): void {
    const modal = document.getElementById('editModal');
    if (modal) {
      (modal as any).classList.remove('show', 'd-block');
      this.editForm.reset();
      this.cdr.detectChanges();
    }
  }

  onUpdate(): void {
    if (this.editForm.invalid) return;

    this.inventoryService.updateGroup(this.editForm.value).subscribe({
      next: (res: any) => {
        if (res.success) {
          this.toastr.success('Updated successfully');
          this.closeEdit();
          this.loadGrid();
        }
      },
      error: () => this.toastr.error('Update failed')
    });
  }

  get totalPages(): number {
    return Math.ceil(this.totalCount / this.pageSize);
  }

  changePage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.loadGrid();
    }
  }
}