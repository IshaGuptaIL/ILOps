import { Component, EventEmitter, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CustomerSalesService } from './customer-sales.service';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-new-customer-group-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './new-customer-group-modal.component.html',
  styleUrls: ['./new-customer-group-modal.component.css']
})
export class NewCustomerGroupModalComponent {
  @Output() onSaved = new EventEmitter<void>();
  @Output() onClose = new EventEmitter<void>();

  groupCode: string = '';
  groupName: string = '';
  firstBVCustNo: string = '';

  constructor(private salesService: CustomerSalesService) {}

  async createGroup() {
    if (!this.groupCode || !this.groupName || !this.firstBVCustNo) {
      Swal.fire('Error', 'Please fill all fields', 'error');
      return;
    }

    const result = await Swal.fire({
      title: 'Include French Labels?',
      text: 'Would you like to include the French labels for columns?',
      icon: 'question',
      showCancelButton: true,
      confirmButtonText: 'Yes',
      cancelButtonText: 'No',
      customClass: {
        confirmButton: 'btn btn-success',
        cancelButton: 'btn btn-secondary'
      },
      buttonsStyling: true
    });

    const includeFrench = result.isConfirmed;

    const request = {
      custGroup: this.groupCode,
      groupName: this.groupName,
      bvCustNo: this.firstBVCustNo,
      includeFrench: includeFrench
    };

    this.salesService.createCustomerGroup(request).subscribe({
      next: (res) => {
        if (res) {
          Swal.fire('Success', 'Customer Group Created.', 'success');
          this.onSaved.emit();
          this.close();
        }
      },
      error: (err) => {
        Swal.fire('Error', err.error?.message || 'Failed to create group', 'error');
      }
    });
  }

  close() {
    this.onClose.emit();
  }
}
