import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { SpinnerService } from '../../../shared/spinner/spinner-service';
import { CookieService } from 'ngx-cookie-service';
import Swal from 'sweetalert2';
import { InventoryEditService } from '../../inventory-edit-service';

@Component({
  selector: 'app-terms-edit',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './terms-edit.html',
  styleUrls: ['./terms-edit.css']
})
export class TermsEdit implements OnInit {
  termsData = {
    invoiceNo: '',
    existingTerms: '',
    invoiceTotal: 0,
    newTerms: '',
    found: false
  };
  currentUser: string = 'SystemUser';

  constructor(
    private inventoryEditService: InventoryEditService,
    private toastr: ToastrService,
    private spinner: SpinnerService,
    private cookies: CookieService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.currentUser = this.cookies.get('UserId') || 'SystemUser';
  }

  findInvoiceTerms() {
    if (!this.termsData.invoiceNo) {
      this.toastr.warning('Please enter an invoice number');
      return;
    }


    this.spinner.show();
    this.cdr.detectChanges();
    
    this.inventoryEditService.getInvoiceTerms(this.termsData.invoiceNo).subscribe({
      next: (res:any) => {
        this.spinner.hide();
        if (res) {
          this.termsData.existingTerms = res.terms_description;
          this.termsData.invoiceTotal = res.total;
          this.termsData.found = true;
          this.toastr.success('Invoice found');
        } else {
          this.toastr.error('Invoice not found');
          this.termsData.found = false;
        }
        this.cdr.detectChanges();
      },
      error: (err:any) => {
        this.spinner.hide();
        this.toastr.error('Error finding invoice');
        this.cdr.detectChanges();
      }
    });
  }

  updateTerms() {
    debugger
    if (this.termsData.invoiceTotal !== 0) {
      Swal.fire('Error', 'You cannot modify the terms on an invoice with a non-zero total', 'error');
      return;
    }
    if (!this.termsData.newTerms) {
      this.toastr.warning('Please select new terms');
      return;
    }


    const payload = {
      invoiceNo: this.termsData.invoiceNo,
      termsLabel: this.termsData.newTerms,
      modifiedBy: this.currentUser
    };

    this.spinner.show();
    this.cdr.detectChanges();

    this.inventoryEditService.updateInvoiceTerms(payload).subscribe({
      next: () => {
        this.spinner.hide();
        Swal.fire('Success', 'Invoice Terms Updated', 'success');
        this.findInvoiceTerms(); 
        this.cdr.detectChanges();
      },
      error: (err:any) => {
        this.spinner.hide();
        Swal.fire('Error', 'Error updating terms', 'error');
        this.cdr.detectChanges();
      }
    });
  }
}
