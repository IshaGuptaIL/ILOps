import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InventoryEditService } from '../../inventory-edit-service';
import { ToastrService } from 'ngx-toastr';
import { SpinnerService } from '../../../shared/spinner/spinner-service';
import { CookieService } from 'ngx-cookie-service';
import { Router } from '@angular/router';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-address-edit',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './address-edit.html',
  styleUrls: ['./address-edit.css']
})
export class AddressEdit implements OnInit {
  searchInvoice: string = '';
  billTo: any = this.createEmptyAddress();
  shipTo: any = this.createEmptyAddress();
  currentUser: string = 'SystemUser';

  constructor(
    private inventoryEditService: InventoryEditService,
    private toastr: ToastrService,
    private spinner: SpinnerService,
    private cookies: CookieService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.currentUser = this.cookies.get('UserId') || 'SystemUser';
    this.cdr.detectChanges();
  }

  createEmptyAddress() {
    return {
      name: '',
      address1: '',
      address2: '',
      address3: '',
      address4: '',
      city: '',
      provState: '',
      postalZip: '',
      countryCode: ''
    };
  }

  findInvoice() {
    if (!this.searchInvoice) {
      this.toastr.warning('Please enter an invoice number');
      return;
    }

    this.spinner.show();
    this.inventoryEditService.getInvoiceAddress(this.searchInvoice).subscribe({
      next: (res) => {
        this.spinner.hide();
        if (res) {
          this.billTo = res.billTo;
          this.shipTo = res.shipTo;
          this.toastr.success('Invoice data loaded');
        } else {
          this.toastr.error('No address found for this invoice');
          this.clearAddresses();
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.spinner.hide();
        console.error('Load error:', err);
        this.toastr.error(err.error?.message || 'Error loading address');
        this.clearAddresses();
        this.cdr.detectChanges();
      }
    });
  }

  updateInvoice() {
    if (!this.searchInvoice) {
      this.toastr.warning('No invoice selected');
      return;
    }

    const payload = {
      invoiceNo: this.searchInvoice,
      billTo: this.billTo,
      shipTo: this.shipTo,
      modifiedBy: this.currentUser
    };

    this.spinner.show();
    this.inventoryEditService.updateInvoiceAddress(payload).subscribe({
      next: (res) => {
        this.spinner.hide();
        if (res) {
          Swal.fire({
            title: 'Success!',
            text: 'Addresses updated successfully.',
            icon: 'success',
            confirmButtonColor: '#3085d6'
          });
        } else {
          this.toastr.error('Failed to update addresses');
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.spinner.hide();
        console.error(err);
        Swal.fire('Error', 'Failed to update addresses', 'error');
        this.cdr.detectChanges();
      }
    });
  }

  clearAddresses() {
    this.billTo = this.createEmptyAddress();
    this.shipTo = this.createEmptyAddress();
    this.cdr.detectChanges();
  }

  // VBA Button Event Handlers
  onFindClick() {
    this.findInvoice();
  }

  onUpdateClick() {
    this.updateInvoice();
  }

  onInvoiceKeyPress(event: KeyboardEvent) {
    if (event.key === 'Enter') {
      this.findInvoice();
    }
  }

  onInvoiceChange() {
    // Keep data visible until search is triggered
  }
}
