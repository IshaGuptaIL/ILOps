import { Component, OnInit } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { CommonModule } from '@angular/common';
import { InventoryService } from './inventory-service';
import { CookieService } from 'ngx-cookie-service';

@Component({
  selector: 'app-add-inventory',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './add-inventory-component.html',
  styleUrls: ['./add-inventory-component.css']
})
export class AddInventoryComponent implements OnInit {

  form: any;
  submitting = false;
  partNoError = '';
  warehouses: any[] = [];
  manufacturers: any[] = [];
  filteredManufacturers: any[] = [];
  UserRoleId:any

  constructor(
    private fb: FormBuilder,
    private inventoryService: InventoryService,
    private toastr: ToastrService,
    private cookies:CookieService
  ) {
    // Initialize the form inside constructor
    this.form = this.fb.group({
      Whse: ['', Validators.required],
      PartNo: ['', Validators.required],
      ProductCode: [''],
      Description: ['', Validators.required],
      FrDescription: ['', Validators.required],
      Type: ['', Validators.required],
      AccessoryGroup: [''],
      SalesDept: [''],
      CostPrice: [null, [Validators.required, Validators.min(0.01)]],
      SellingPrice: [null, [Validators.required, Validators.min(0.01)]]
    });
  }

ngOnInit(): void {
  this.UserRoleId=Number(this.cookies.get('UserRoleId'))

  debugger;
  this.form.get('PartNo')?.valueChanges.subscribe((v: any) => {
    this.form.patchValue({ PartNo: v?.toUpperCase() }, { emitEvent: false });
  });

  // =============================
  // Load Warehouses
  // =============================
  if(this.UserRoleId >0)
  {

    this.inventoryService.getWarehouses(this.UserRoleId).subscribe({
      next: (res: any) => {
        this.warehouses = res;
        console.log("isha",this.warehouses)
      },
      error: () => this.toastr.error('Failed to load warehouses')
    });
  }

  // =============================
  // Load Manufacturers
  // =============================
  this.inventoryService.getManufacturers().subscribe({
    next: (res: any) => {
      this.manufacturers = res;
      this.filteredManufacturers = res;
    },
    error: () => this.toastr.error('Failed to load manufacturers')
  });
}
filterManufacturers(code: string) {
  if (code === 'ACC') {
    this.filteredManufacturers =
      this.manufacturers.filter(m => m.inventoryType === 'ACC');
  }
  else if (code === 'HCC') {
    this.filteredManufacturers =
      this.manufacturers.filter(m => m.inventoryType === 'HCC');
  }
  else {
    this.filteredManufacturers =
      this.manufacturers.filter(m => m.inventoryType === 'OTH');
  }
}


  // =============================
  // Check duplicate part number
  // =============================
  checkPartNo(): void {
    const partNo = this.form.get('PartNo')?.value;
    const whse = this.form.get('Whse')?.value;
    if (!partNo || !whse) return;

    this.inventoryService.checkPartNo(partNo, whse).subscribe((res: any) => {
      if (res.success && res.result) {
        this.partNoError = res.result.Exists ? 'Part Number already exists' : '';
        if (res.result.Exists) {
          this.form.get('PartNo')?.setErrors({ duplicate: true });
        }
      }
    });
  }

  isInvalid(controlName: string): boolean {
  const control = this.form.get(controlName);
  return control?.invalid && (control.dirty || control.touched);
}

onProductCode(): void {
  const code = this.form.get('ProductCode')?.value?.toUpperCase();
  this.form.patchValue({ ProductCode: code }, { emitEvent: false });

  if (code === 'HCC') {
    this.form.patchValue({ Type: 'Hardware', SalesDept: '4' });
    this.filterManufacturers('HCC');
  }
  else if (code === 'ACC') {
    this.form.patchValue({ Type: 'Accessory', SalesDept: '5' });
    this.filterManufacturers('ACC');
  }
}
  // =============================
  // Submit inventory item
  // =============================
  submit(): void {
    if (this.form.invalid) return;

    this.submitting = true;

    this.inventoryService.addInventoryItem(this.form.value).subscribe({
      next: (res: any) => {
        if (res.success) {
          this.toastr.success(res.message || 'Inventory item added successfully!');
          this.form.reset();
        } else {
          this.toastr.error(res.message || 'Failed to add inventory');
        }
      },
      error: (err) => {
        this.toastr.error('Server error, try again later');
        console.error(err);
      },
      complete: () => this.submitting = false
    });
  }

  // =============================
  // Type change handler
  // =============================
onTypeChange(): void {
  const type = this.form.get('Type')?.value;

  if (type === 'Hardware') {
    this.form.patchValue({ ProductCode: 'HCC', SalesDept: '4' });
    this.filterManufacturers('HCC');
  }
  else if (type === 'Accessory') {
    this.form.patchValue({ ProductCode: 'ACC', SalesDept: '5' });
    this.filterManufacturers('ACC');
  }
  else {
    this.filteredManufacturers = this.manufacturers;
  }
}
}
