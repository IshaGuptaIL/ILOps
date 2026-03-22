import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { CommonModule } from '@angular/common';
import { InventoryService } from './inventory-service';
import { CookieService } from 'ngx-cookie-service';
import { Observable, of } from 'rxjs';
import { delay, tap } from 'rxjs/operators';
import { SpinnerService } from '../../shared/spinner/spinner-service';
import Swal from 'sweetalert2';

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
  UserRoleId: number | null = null;
  warehouses$: Observable<any[]> = of([]);
  manufacturers: any[] = [];
  groupLabel: string = 'Accessory Group';
  filteredManufacturers: any[] = [];

  constructor(
    private fb: FormBuilder,
    private inventoryService: InventoryService,
    private toastr: ToastrService,
    private cookies: CookieService,
    private spinner:SpinnerService,
    private cdr: ChangeDetectorRef
  ) {
     this.form = this.fb.group({
      Whse: ['', Validators.required],
      PartNo: ['', Validators.required],
      ProductCode: ['',Validators.required],
      Description: ['', [Validators.required, Validators.maxLength(80)]],
      FrDescription: ['', [Validators.required, Validators.maxLength(80)]],
      Type: ['', Validators.required],
    AccessoryGroup: ['', Validators.required],
      SalesDept: [''],
   CostPrice: [0, [Validators.min(0)]], 
  SellingPrice: [0, [Validators.min(0)]]
    });
  
  }

  ngOnInit(): void {
    this.UserRoleId = Number(this.cookies.get('UserRoleId')) || null;

    this.form.get('PartNo')?.valueChanges.subscribe((v: string) => {
      if (v && v !== v.toUpperCase()) {
        this.form.patchValue({ PartNo: v.toUpperCase() }, { emitEvent: false });
      }
    });

    if (this.UserRoleId && this.UserRoleId > 0) {
    this.warehouses$ = this.inventoryService.getWarehouses(this.UserRoleId).pipe(
    tap((data: any[]) => {
      if (data && data.length > 0) {
        this.form.patchValue({
          Whse: data[0].whse
        });
      }
    }),
    delay(0),
    tap(() => setTimeout(() => this.cdr.detectChanges(), 0))
  );
    }

    // Manufacturers
    this.inventoryService.getManufacturers().pipe(delay(0)).subscribe({
      next: (res: any) => {
        this.manufacturers = res || [];
        this.filteredManufacturers = [...this.manufacturers];
        this.cdr.detectChanges();
      },
      error: () => {
        this.toastr.error('Failed to load manufacturers');
        this.cdr.detectChanges();
      }
    });
  }
  

  trackByWhse(index: number, item: any) {
    return item.whse;
  }

  filterManufacturers(code: string) {
    if (code === 'ACC') {
      this.filteredManufacturers = this.manufacturers.filter(m => m.inventoryType === 'ACC');
    } else if (code === 'HCC') {
      this.filteredManufacturers = this.manufacturers.filter(m => m.inventoryType === 'HCC');
    } else {
      this.filteredManufacturers = [...this.manufacturers];
    }
    this.cdr.detectChanges();
  }

onProductCode(): void {
  const code = (this.form.get('ProductCode')?.value || '').toUpperCase().trim();
  this.form.patchValue({ ProductCode: code }, { emitEvent: false });

  if (code === 'HCC') {
    this.form.patchValue({ Type: 'Hardware', SalesDept: '4' });
    this.groupLabel = 'Manufacturer';   
    this.filterManufacturers('HCC');
  } 
  else if (code === 'ACC') {
    this.form.patchValue({ Type: 'Accessory', SalesDept: '5' });
    this.groupLabel = 'Accessory Group'; 
    this.filterManufacturers('ACC');
  }
}

 onTypeChange(): void {
  const type = this.form.get('Type')?.value;
  const groupControl = this.form.get('AccessoryGroup');

  if (type === 'Hardware') {
    this.form.patchValue({ ProductCode: 'HCC', SalesDept: 4 });
    this.groupLabel = 'Manufacturer';   
    groupControl?.setValidators([Validators.required]);
    this.filterManufacturers('HCC');
  } 
  else if (type === 'Accessory') {
    this.form.patchValue({ ProductCode: 'ACC', SalesDept: 5 });
    this.groupLabel = 'Accessory Group';  
    groupControl?.clearValidators();
    this.filterManufacturers('ACC');
  }

  groupControl?.updateValueAndValidity();
}

  checkPartNo(): void {
    debugger
  const partNo = this.form.get('PartNo')?.value?.trim();
  const whse = this.form.get('Whse')?.value;

  if (!partNo || !whse) return;

  this.inventoryService.checkPartNo(partNo, whse).subscribe({
    next: (res: any) => {
      if (res?.success && res?.result?.exists === true) { 
        this.partNoError = `Already exists in ${whse}`;
        this.form.get('PartNo')?.setErrors({ duplicate: true });
      } else {
        this.inventoryService.checkPartNo(partNo, 'FR').subscribe((frRes: any) => {
          if (frRes?.success && frRes?.result?.exists === true) {
            this.partNoError = 'Already exists in warehouse FR';
            this.form.get('PartNo')?.setErrors({ duplicate: true });
          } else {
            this.partNoError = '';
            const currentErrors = this.form.get('PartNo')?.errors;
            if (currentErrors) {
              delete currentErrors['duplicate'];
              const remainingErrors = Object.keys(currentErrors).length > 0 ? currentErrors : null;
              this.form.get('PartNo')?.setErrors(remainingErrors);
            }
          }
        });
      }
      this.cdr.detectChanges();
    }
  });
}

  isInvalid(controlName: string): boolean {
    const control = this.form.get(controlName);
    return !!(control?.invalid && (control?.dirty || control?.touched));
  }

//  submit(): void {
//   // show spinner immediately
//   this.spinner.show();
//   this.cdr.detectChanges(); // force update so spinner appears

//   if (this.form.invalid) {
//     this.form.markAllAsTouched();
//     this.submitting = false;
//     this.spinner.hide();
//     this.cdr.detectChanges();
//     return;
//   }

//   this.submitting = true;

//   this.inventoryService.addInventoryItem(this.form.value).subscribe({
//     next: (res: any) => {
//       if (res.success) {
//         this.spinner.hide();
//         this.cdr.detectChanges();
//         this.toastr.success('Inventory item added successfully!');
//         this.form.reset();
//       } else {
//         this.spinner.hide();
//         this.cdr.detectChanges();
//         this.toastr.error(res.message || 'Failed to add item');
//       }
//     },
//     error: (err) => {
//       this.spinner.hide();
//       this.cdr.detectChanges();
//       this.toastr.error('Server error while saving item');
//       console.error(err);
//     },
//     complete: () => {
//       this.submitting = false;
//       this.cdr.detectChanges();
//     }
//   });
// }

submit(): void {
  debugger
  // 1. Mandatory Field Validation (Jaise VBA mein If Me.txtWhse = "" tha)
  if (this.form.invalid) {
    this.form.markAllAsTouched();
    this.toastr.error("Please fill all required fields.");
    return;
  }

  const cost = this.form.get('CostPrice')?.value;
  const sell = this.form.get('SellingPrice')?.value;

 if (cost === 0 || sell === 0 || cost === null || sell === null) {
  const zeroType = (cost === 0 || cost === null) ? 'Cost' : 'Selling';
    Swal.fire({
      title: 'Zero Price ',
      text: `You have entered a ${cost === 0 ? 'Cost' : 'Selling'} price of zero. Is this correct?`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Yes',
      cancelButtonText: 'No'
    }).then((result) => {
      if (result.isConfirmed) {
        this.processFormSubmission(); 
      }
    });
  } else {
    this.processFormSubmission(); 
  }
}

private processFormSubmission(): void {
  debugger
  this.spinner.show();
  this.submitting = true;

  this.inventoryService.addInventoryItem(this.form.value).subscribe({
    next: (res: any) => {
      this.spinner.hide();
      if (res.success) {
        Swal.fire(
          'Success!',
          'Inventory item added successfully in both warehouses.',
          'success'
        );
        this.form.reset();
       this.form.patchValue({ 
    Whse: '', 
    Type: '', 
    CostPrice: 0, 
    SellingPrice: 0 
  });
      } else {
        Swal.fire('Error', res.message || 'Failed to add item', 'error');
      }
    },
    error: (err) => {
      this.spinner.hide();
      Swal.fire('Oops!', 'Server error while saving item. Please try again.', 'error');
      console.error(err);
    },
    complete: () => {
      this.submitting = false;
      this.cdr.detectChanges();
    }
  });
}
}