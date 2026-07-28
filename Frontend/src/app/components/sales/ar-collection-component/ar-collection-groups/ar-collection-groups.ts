import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ArCollectionService, ARGroupSummary, ARGroupCustomerRow, ARBulkCustomerWithName } from '../ar-collection.service';
import { SpinnerService } from '../../../shared/spinner/spinner-service';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-ar-collection-groups',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './ar-collection-groups.html',
  styleUrl: './ar-collection-groups.css'
})
export class ArCollectionGroupsComponent implements OnInit {
  // Selector for group type: "Collections Groups" or "Rogers Reporting Groups"
  groupType: string = 'Collections Groups';

  // Data lists
  groupsSummary: ARGroupSummary[] = [];
  groupCustomers: ARGroupCustomerRow[] = [];
  bulkCustomers: ARBulkCustomerWithName[] = [];

  // Selections
  selectedCustGroup: string = '';
  selectedGroupName: string = '';
  selectedCustomer: ARGroupCustomerRow | null = null;
  selectedBulkCustomer: ARBulkCustomerWithName | null = null;

  // Add Customer Form bindings
  spireCustNo: string = '';
  isNewGroup: boolean = false;
  newGroupName: string = '';
  newGroupNameEnabled: boolean = false;

  // Add Bulk Customer bindings
  addBulkCustNo: string = '';

  // Loading state
  isLoading: boolean = false;

  constructor(
    private arService: ArCollectionService,
    private spinner: SpinnerService,
    private toastr: ToastrService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadAll();
    this.cdr.detectChanges();
  }

  loadAll(): void {
    this.loadGroupsSummary();
    this.loadBulkCustomers();
    this.cdr.detectChanges();
  }

  // --- Group Type Operations ---
  onGroupTypeChange(): void {
    this.selectedCustGroup = '';
    this.selectedGroupName = '';
    this.selectedCustomer = null;
    this.groupCustomers = [];
    this.loadGroupsSummary();
    this.cdr.detectChanges();
  }

  loadGroupsSummary(): void {
    this.isLoading = true;
    this.spinner.show();

    this.arService.getGroupsSummary(this.groupType).subscribe({
      next: (data) => {
        this.groupsSummary = data;
        if (this.selectedCustGroup) {
          const stillExists = this.groupsSummary.find(g => g.custGroup === this.selectedCustGroup);
          if (stillExists) {
            this.selectedGroupName = stillExists.maxOfGroupName;
            this.loadGroupCustomers();
          } else {
            this.selectedCustGroup = '';
            this.selectedGroupName = '';
            this.groupCustomers = [];
          }
        }
        this.isLoading = false;
        this.spinner.hide();
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.spinner.hide();
        this.toastr.error('Failed to load groups summary.');
        this.cdr.detectChanges();
      }
    });
    this.cdr.detectChanges();
  }

  // --- Group Selection ---
  selectGroup(group: ARGroupSummary): void {
    this.selectedCustGroup = group.custGroup;
    this.selectedGroupName = group.maxOfGroupName;
    this.selectedCustomer = null;
    this.loadGroupCustomers();
    this.cdr.detectChanges();
  }

  loadGroupCustomers(): void {
    if (!this.selectedCustGroup) {
      this.cdr.detectChanges();
      return;
    }

    this.isLoading = true;
    this.spinner.show();

    this.arService.getGroupCustomers(this.groupType, this.selectedCustGroup).subscribe({
      next: (data) => {
        this.groupCustomers = data;
        this.isLoading = false;
        this.spinner.hide();
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.spinner.hide();
        this.toastr.error('Failed to load customer list for the selected group.');
        this.cdr.detectChanges();
      }
    });
    this.cdr.detectChanges();
  }

  selectCustomer(cust: ARGroupCustomerRow): void {
    this.selectedCustomer = cust;
    this.cdr.detectChanges();
  }

  // --- Add Customer Form Actions ---
  chkNewGroupChange(): void {
    if (this.isNewGroup) {
      this.newGroupNameEnabled = true;
    } else {
      this.newGroupNameEnabled = false;
      this.newGroupName = '';
    }
    this.cdr.detectChanges();
  }

  onAddCustomer(): void {
    const custNo = this.spireCustNo.trim();
    if (!custNo) {
      this.toastr.warning('You must enter a Spire Customer Number.');
      this.cdr.detectChanges();
      return;
    }

    if (!this.isNewGroup && !this.selectedCustGroup) {
      this.toastr.warning('You must select a group from the left panel.');
      this.cdr.detectChanges();
      return;
    }

    if (this.isNewGroup && !this.newGroupName.trim()) {
      this.toastr.warning('New Group Name is required.');
      this.cdr.detectChanges();
      return;
    }

    this.isLoading = true;
    this.spinner.show();

    this.arService.lookupCustomerName(custNo).subscribe({
      next: (res) => {
        this.isLoading = false;
        this.spinner.hide();

        if (!res.exists) {
          Swal.fire({
            title: 'Customer Not Found',
            text: `Spire Customer Number ${custNo} not found.`,
            icon: 'error',
            confirmButtonColor: '#3085d6'
          });
          this.cdr.detectChanges();
          return;
        }

        const custName = res.name;
        let confirmText = '';
        let confirmTitle = '';

        if (this.isNewGroup) {
          confirmTitle = 'Create Group?';
          confirmText = `Are you sure you wish to create a new group "${this.newGroupName.trim()}"\n\nwith customer\n\n${custNo} - ${custName}?`;
        } else {
          confirmTitle = 'Add Customer to Group?';
          confirmText = `Are you sure you wish to add customer\n\n${custNo} - ${custName}\n\nto group\n\n${this.selectedCustGroup} - ${this.selectedGroupName}?`;
        }

        Swal.fire({
          title: confirmTitle,
          text: confirmText,
          icon: 'question',
          showCancelButton: true,
          confirmButtonColor: '#3085d6',
          cancelButtonColor: '#d33',
          confirmButtonText: 'Yes',
          cancelButtonText: 'No'
        }).then((result) => {
          if (result.isConfirmed) {
            this.isLoading = true;
            this.spinner.show();
            this.cdr.detectChanges();

            const payload = {
              groupType: this.groupType,
              custNo: custNo,
              isNewGroup: this.isNewGroup,
              newGroupName: this.newGroupName.trim(),
              selectedCustGroup: this.selectedCustGroup
            };

            this.arService.addCustomerToGroup(payload).subscribe({
              next: (response) => {
                this.isLoading = false;
                this.spinner.hide();

                if (response.message === 'SUCCESS') {
                  this.toastr.success(this.isNewGroup ? 'New group added.' : 'Customer added to group.');
                  this.spireCustNo = '';
                  this.newGroupName = '';
                  this.isNewGroup = false;
                  this.newGroupNameEnabled = false;
                  this.loadGroupsSummary();
                } else {
                  Swal.fire('Error', response.message, 'error');
                }
                this.cdr.detectChanges();
              },
              error: (err) => {
                this.isLoading = false;
                this.spinner.hide();
                this.toastr.error('Error occurred while assigning customer.');
                this.cdr.detectChanges();
              }
            });
          }
          this.cdr.detectChanges();
        });
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.spinner.hide();
        this.toastr.error('Error verifying customer number.');
        this.cdr.detectChanges();
      }
    });
    this.cdr.detectChanges();
  }

  // --- Remove Customer Action ---
  onRemoveCustomer(): void {
    if (!this.selectedCustomer) {
      this.toastr.warning('You must select a customer from the middle pane.');
      this.cdr.detectChanges();
      return;
    }

    const customer = this.selectedCustomer;

    Swal.fire({
      title: 'Remove Customer From Group?',
      text: `Are you sure you want to remove customer ${customer.bvCustNo} from the group?\n\nNOTE: Activities associated with this group such as Bare Comments, Calls, Emails etc. are associated with the GROUP and will no longer be associated with this BV Customer.`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      cancelButtonColor: '#3085d6',
      confirmButtonText: 'Yes',
      cancelButtonText: 'No'
    }).then((result) => {
      if (result.isConfirmed) {
        this.isLoading = true;
        this.spinner.show();
        this.cdr.detectChanges();

        const payload = {
          groupType: this.groupType,
          custNo: customer.bvCustNo
        };

        this.arService.removeCustomerFromGroup(payload).subscribe({
          next: (success) => {
            this.isLoading = false;
            this.spinner.hide();

            if (success) {
              this.toastr.success('Customer removed from group.');
              this.selectedCustomer = null;
              this.loadGroupsSummary();
            } else {
              this.toastr.error('Failed to remove customer.');
            }
            this.cdr.detectChanges();
          },
          error: () => {
            this.isLoading = false;
            this.spinner.hide();
            this.toastr.error('Error removing customer from group.');
            this.cdr.detectChanges();
          }
        });
      }
      this.cdr.detectChanges();
    });
    this.cdr.detectChanges();
  }

  // --- Modify Group Name Action ---
  onModifyGroupName(): void {
    if (!this.selectedCustGroup) {
      this.toastr.warning('You must select a group from the left panel.');
      this.cdr.detectChanges();
      return;
    }

    Swal.fire({
      title: 'Change Group Name',
      input: 'text',
      inputLabel: 'Enter the new name:',
      inputValue: this.selectedGroupName,
      showCancelButton: true,
      confirmButtonColor: '#3085d6',
      cancelButtonColor: '#d33',
      confirmButtonText: 'Save',
      cancelButtonText: 'Cancel',
      inputValidator: (value) => {
        if (!value || !value.trim()) {
          return 'Group name cannot be empty!';
        }
        return null;
      }
    }).then((result) => {
      if (result.isConfirmed && result.value) {
        const newName = result.value.trim();
        this.isLoading = true;
        this.spinner.show();
        this.cdr.detectChanges();

        const payload = {
          groupType: this.groupType,
          custGroup: this.selectedCustGroup,
          newGroupName: newName
        };

        this.arService.modifyGroupName(payload).subscribe({
          next: (success) => {
            this.isLoading = false;
            this.spinner.hide();

            if (success) {
              this.toastr.success('Group name changed.');
              this.selectedGroupName = newName;
              this.loadGroupsSummary();
            } else {
              this.toastr.error('Failed to change group name.');
            }
            this.cdr.detectChanges();
          },
          error: () => {
            this.isLoading = false;
            this.spinner.hide();
            this.toastr.error('Error changing group name.');
            this.cdr.detectChanges();
          }
        });
      }
      this.cdr.detectChanges();
    });
    this.cdr.detectChanges();
  }

  // --- Bulk Customers Operations ---
  loadBulkCustomers(): void {
    this.arService.getBulkCustomersWithName().subscribe({
      next: (data) => {
        this.bulkCustomers = data;
        this.cdr.detectChanges();
      },
      error: () => {
        this.toastr.error('Failed to load bulk customers.');
        this.cdr.detectChanges();
      }
    });
    this.cdr.detectChanges();
  }

  selectBulkCustomer(bulk: ARBulkCustomerWithName): void {
    this.selectedBulkCustomer = bulk;
    this.cdr.detectChanges();
  }

  onAddBulkCustomer(): void {
    const custNo = this.addBulkCustNo.trim();
    if (!custNo) {
      this.toastr.warning('Customer No is required.');
      this.cdr.detectChanges();
      return;
    }

    this.isLoading = true;
    this.spinner.show();

    this.arService.lookupCustomerName(custNo).subscribe({
      next: (res) => {
        if (!res.exists) {
          this.isLoading = false;
          this.spinner.hide();
          Swal.fire({
            title: 'Customer Not Found',
            text: `Spire Customer Number ${custNo} not found.`,
            icon: 'error'
          });
          this.cdr.detectChanges();
          return;
        }

        this.arService.addBulkCustomer(custNo).subscribe({
          next: (success) => {
            this.isLoading = false;
            this.spinner.hide();

            if (success) {
              this.toastr.success('Bulk customer added successfully.');
              this.addBulkCustNo = '';
              this.loadBulkCustomers();
            } else {
              this.toastr.error('Bulk customer already exists or failed to add.');
            }
            this.cdr.detectChanges();
          },
          error: () => {
            this.isLoading = false;
            this.spinner.hide();
            this.toastr.error('Error adding bulk customer.');
            this.cdr.detectChanges();
          }
        });
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.spinner.hide();
        this.toastr.error('Error verifying customer number.');
        this.cdr.detectChanges();
      }
    });
    this.cdr.detectChanges();
  }

  onRemoveBulkCustomer(bulk: ARBulkCustomerWithName): void {
    Swal.fire({
      title: 'Remove Bulk Customer?',
      text: `Are you sure you want to delete customer ${bulk.custNo} - ${bulk.name} from the Bulk list?`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      cancelButtonColor: '#3085d6',
      confirmButtonText: 'Yes, Delete',
      cancelButtonText: 'Cancel'
    }).then((result) => {
      if (result.isConfirmed) {
        this.isLoading = true;
        this.spinner.show();
        this.cdr.detectChanges();

        this.arService.removeBulkCustomer(bulk.id).subscribe({
          next: (success) => {
            this.isLoading = false;
            this.spinner.hide();

            if (success) {
              this.toastr.success('Bulk customer deleted successfully.');
              if (this.selectedBulkCustomer?.id === bulk.id) {
                this.selectedBulkCustomer = null;
              }
              this.loadBulkCustomers();
            } else {
              this.toastr.error('Failed to delete bulk customer.');
            }
            this.cdr.detectChanges();
          },
          error: () => {
            this.isLoading = false;
            this.spinner.hide();
            this.toastr.error('Error deleting bulk customer.');
            this.cdr.detectChanges();
          }
        });
      }
      this.cdr.detectChanges();
    });
    this.cdr.detectChanges();
  }
}
