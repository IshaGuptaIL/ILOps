import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { FieldSelectorComponent } from './field-selector-modal.component';
import { NewCustomerGroupModalComponent } from './new-customer-group-modal.component';
import { EditDataModalComponent } from './edit-data-modal.component';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CustomerSalesService, CustomerSalesRequest, CustomerSalesRow, CustomerGroupBO, BVCustomerBO } from './customer-sales.service';
import { SpinnerService } from '../../shared/spinner/spinner-service';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-customer-sales',
  standalone: true,
  imports: [CommonModule, FormsModule, FieldSelectorComponent, NewCustomerGroupModalComponent, EditDataModalComponent],
  templateUrl: './customer-sales-component.html',
  styleUrls: ['./customer-sales-component.css']
})
export class CustomerSalesComponent implements OnInit {
  // Filters
  startDate: string = '';
  endDate: string = '';
  selectedFields: any[] = [];
  showFieldSelector: boolean = false;
  showNewGroupModal: boolean = false;
  selectedGroup: string = '';
  msdCodeInput: string = '';
  territoryInput: string = '';
  selectedFormat: string = 'Excel';
  isEditing: boolean = false;
  showEditModal: boolean = false;

  // Data
  customerGroups: CustomerGroupBO[] = [];
  customersInGroup: BVCustomerBO[] = [];
  reportData: CustomerSalesRow[] = [];

  // Status
  isDataGenerated: boolean = false;
  lastGeneratedGroup: string = '';

  // Inline Editing
  editingCustNo: string | null = null;
  newCustomer: BVCustomerBO = { bvCustNo: '', bvName: '' };

  constructor(
    private salesService: CustomerSalesService,
    private spinner: SpinnerService,
    private toastr: ToastrService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.startDate = '';
    this.endDate = '';

    this.loadCustomerGroups();
  }


  loadCustomerGroups(): void {
    this.salesService.getCustomerGroups().subscribe({
      next: (groups) => {
        this.customerGroups = groups;
        this.cdr.detectChanges();
      },
      error: () => this.toastr.error('Failed to load customer groups')
    });
  }

  onGroupChange(): void {
    if (this.selectedGroup) {
      this.salesService.getCustomersInGroup(this.selectedGroup).subscribe({
        next: (customers) => {
          this.customersInGroup = customers;
          this.loadGroupFields(); // Fetch fields for the grid
          this.cdr.detectChanges();
        }
      });
    } else {
      this.customersInGroup = [];
      this.selectedFields = [];
    }
  }

  loadGroupFields(): void {
    this.salesService.getCustomerFields(this.selectedGroup).subscribe({
      next: (fields) => {
        this.selectedFields = fields;
        if (this.selectedFields.length === 0) {
          //  this.loadMockFields(); // Fallback for demo
        }
        this.cdr.detectChanges();
      }
    });
  }


  generateData(): void {
    if (!this.startDate) {
      this.toastr.warning('You must enter a valid start date');
      return;
    }
    if (!this.endDate) {
      this.toastr.warning('You must enter a valid end date');
      return;
    }
    if (!this.selectedGroup) {
      this.toastr.warning('You must select a customer group');
      return;
    }

    this.spinner.show();
    const request: CustomerSalesRequest = {
      startDate: this.startDate,
      endDate: this.endDate,
      custGroup: this.selectedGroup
    };

    this.salesService.generateData(request).subscribe({
      next: (success) => {
        if (success) {
          this.toastr.success('Data generation complete');
          this.isDataGenerated = true;
          this.lastGeneratedGroup = this.selectedGroup;
          this.fetchGeneratedData();
        } else {
          this.toastr.error('Data generation failed');
          this.spinner.hide();
        }
      },
      error: () => {
        this.spinner.hide();
        this.toastr.error('Failed to generate data');
      }
    });
  }

  fetchGeneratedData(): void {
    this.salesService.getGeneratedData(this.lastGeneratedGroup).subscribe({
      next: (data) => {
        this.reportData = data;
        this.spinner.hide();
        this.cdr.detectChanges();
      },
      error: () => {
        this.spinner.hide();
        this.toastr.error('Failed to fetch generated data');
      }
    });
  }

  exportExcel(): void {
    if (!this.isDataGenerated || this.reportData.length === 0) {
      this.toastr.warning('Please generate data first');
      return;
    }

    this.spinner.show();
    const request: CustomerSalesRequest = {
      startDate: this.startDate,
      endDate: this.endDate,
      custGroup: this.lastGeneratedGroup
    };

    const exportObs = this.selectedFormat === 'CSV' 
      ? this.salesService.exportCsv(request) 
      : this.salesService.exportExcel(request);

    exportObs.subscribe({
      next: (blob) => {
        const ext = this.selectedFormat === 'CSV' ? 'csv' : 'xlsx';
        this.downloadBlob(blob, `CustomerSales-${this.lastGeneratedGroup}.${ext}`);
        this.spinner.hide();
        this.toastr.success(`${this.selectedFormat} exported successfully`);
      },
      error: () => {
        this.spinner.hide();
        this.toastr.error(`${this.selectedFormat} export failed`);
      }
    });
  }

  generateByTerritory(): void {
    if (!this.selectedGroup) {
      this.toastr.warning('Please select a customer group first');
      return;
    }
    if (!this.territoryInput) {
      this.toastr.warning('You must enter a Territory Code');
      return;
    }
    if (!this.startDate || !this.endDate) {
      this.toastr.warning('You must enter a data range for the search');
      return;
    }

    Swal.fire({
      title: 'Replace existing list?',
      html: 'This process will remove all customers in this group<br><br>and regenerate the list based on the Territory code provided.<br><br>and the date range selected.',
      icon: 'question',
      showCancelButton: true,
      confirmButtonText: 'OK',
      cancelButtonText: 'Cancel'
    }).then((result) => {
      if (result.isConfirmed) {
        this.spinner.show();
        const request: CustomerSalesRequest = {
          startDate: this.startDate,
          endDate: this.endDate,
          custGroup: this.selectedGroup,
          territoryCode: this.territoryInput
        };
        this.salesService.generateByTerritory(request).subscribe({
          next: () => {
            this.spinner.hide();
            Swal.fire('Complete', 'List generation complete.', 'success');
            this.loadCustomerGroups();
            this.onGroupChange();
          },
          error: () => {
            this.spinner.hide();
            this.toastr.error('Failed to generate list');
          }
        });
      }
    });
  }

  generateByMSDInput(): void {
    if (!this.selectedGroup) {
      this.toastr.warning('Please select a customer group first');
      return;
    }
    if (!this.msdCodeInput) {
      this.toastr.warning('You must enter an MSD Code');
      return;
    }

    Swal.fire({
      title: 'Replace existing list?',
      html: 'This process will remove all customers in this group<br><br>and regenerate the list based on the MSD code provided<br><br>and the date range selected.',
      icon: 'question',
      showCancelButton: true,
      confirmButtonText: 'OK',
      cancelButtonText: 'Cancel'
    }).then((result) => {
      if (result.isConfirmed) {
        const request: CustomerSalesRequest = {
          startDate: this.startDate,
          endDate: this.endDate,
          custGroup: this.selectedGroup,
          msdCode: this.msdCodeInput
        };
        this.spinner.show();
        this.salesService.generateByMSD(request).subscribe({
          next: () => {
            this.spinner.hide();
            Swal.fire('Complete', 'List generation complete', 'success');
            this.loadCustomerGroups();
            this.onGroupChange();
          },
          error: () => {
            this.spinner.hide();
            this.toastr.error('Failed to generate list');
          }
        });
      }
    });
  }

  viewFields() {
    if (!this.selectedGroup) {
      Swal.fire('Error', 'Please select a customer group first', 'error');
      return;
    }
    this.salesService.getCustomerFields(this.selectedGroup).subscribe(fields => {
      this.selectedFields = fields;
      this.showFieldSelector = true;
    });
  }

  closeFieldSelector = () => {
    this.showFieldSelector = false;
  }

  newCustomerGroup(): void {
    this.showNewGroupModal = true;
  }

  onGroupCreated() {
    this.showNewGroupModal = false;
    this.loadCustomerGroups();
  }

  closeNewGroupModal = () => {
    this.showNewGroupModal = false;
  }

  deleteCustomerGroup(): void {
    if (!this.selectedGroup) {
      Swal.fire('Error', 'Please select a customer group first', 'error');
      return;
    }

    Swal.fire({
      title: 'Delete Customer Group?',
      text: `Are you sure you wish to delete customer group: ${this.selectedGroup} ?`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'OK',
      cancelButtonText: 'Cancel',
      customClass: {
        confirmButton: 'btn-access primary-btn',
        cancelButton: 'btn-access'
      }
    }).then(async (result) => {
      if (result.isConfirmed) {
        // Password prompt matching frmpasswordentry
        const { value: password } = await Swal.fire({
          title: 'Enter Password',
          input: 'password',
          inputLabel: 'Password',
          inputPlaceholder: 'Enter your password',
          showCancelButton: true,
          confirmButtonText: 'OK',
          cancelButtonText: 'Cancel'
        });

        if (password === 'faisal') {
          this.salesService.deleteCustomerGroup(this.selectedGroup).subscribe({
            next: () => {
              Swal.fire('Deleted!', 'Customer group deleted', 'success');
              this.selectedGroup = '';
              this.loadCustomerGroups();
            },
            error: (err) => {
              Swal.fire('Error', err.error?.message || 'Failed to delete group', 'error');
            }
          });
        } else if (password !== undefined) {
          Swal.fire('Error', 'Incorrect Password', 'error');
        }
      }
    });
  }

  addFDDealerGroup(): void {
    this.spinner.show();
    this.salesService.addFDDealerGroup().subscribe({
      next: () => {
        this.spinner.hide();
        Swal.fire('Success', 'All Customers with territory beginning with FD added to group FDDealer', 'success');
        this.loadCustomerGroups();
      },
      error: () => {
        this.spinner.hide();
        this.toastr.error('Failed to add FDDealer group');
      }
    });
  }

  editData(): void {
    if (this.reportData.length === 0) {
      this.toastr.warning('Please generate data first');
      return;
    }
    this.showEditModal = true;
  }

  saveEditedData(): void {
    this.spinner.show();
    this.salesService.updateGeneratedData(this.reportData).subscribe({
      next: (success) => {
        this.spinner.hide();
        if (success) {
          this.toastr.success('Data changes saved to database successfully.');
          this.showEditModal = false;
        } else {
          this.toastr.error('Failed to save changes to database.');
        }
      },
      error: () => {
        this.spinner.hide();
        this.toastr.error('Error saving data changes.');
      }
    });
  }

  closeEditModal = () => {
    this.showEditModal = false;
  }

  // --- Customer Management (Side Grid) ---

  startAddCustomer(): void {
    if (!this.selectedGroup) {
      this.toastr.warning('Please select a customer group first');
      return;
    }
    this.editingCustNo = 'NEW';
    this.newCustomer = { bvCustNo: '', bvName: '' };
  }

  editCustomer(cust: BVCustomerBO): void {
    this.editingCustNo = cust.bvCustNo;
    this.newCustomer = { ...cust };
  }

  cancelCustomerEdit(): void {
    this.editingCustNo = null;
  }

  saveCustomer(): void {
    if (!this.newCustomer.bvCustNo) {
      this.toastr.warning('Customer Number is required');
      return;
    }

    if (this.editingCustNo === 'NEW') {
      this.salesService.addCustomerToGroup(this.selectedGroup, this.newCustomer).subscribe({
        next: () => {
          this.toastr.success('Customer added to group');
          this.editingCustNo = null;
          this.onGroupChange();
        },
        error: () => this.toastr.error('Failed to add customer')
      });
    } else {
      this.salesService.updateCustomerInGroup(this.selectedGroup, this.editingCustNo!, this.newCustomer).subscribe({
        next: () => {
          this.toastr.success('Customer updated');
          this.editingCustNo = null;
          this.onGroupChange();
        },
        error: () => this.toastr.error('Failed to update customer')
      });
    }
  }

  deleteCustomer(cust: BVCustomerBO): void {
    Swal.fire({
      title: 'Remove Customer?',
      text: `Remove ${cust.bvCustNo} from group ${this.selectedGroup}?`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Yes',
      cancelButtonText: 'No'
    }).then((result) => {
      if (result.isConfirmed) {
        this.salesService.removeCustomerFromGroup(this.selectedGroup, cust.bvCustNo).subscribe({
          next: () => {
            this.toastr.success('Customer removed');
            this.onGroupChange();
          },
          error: () => this.toastr.error('Failed to remove customer')
        });
      }
    });
  }

  outputPerCustomer(): void {
    if (!this.isDataGenerated) {
      this.toastr.warning('Please generate data first');
      return;
    }
    this.spinner.show();
    this.salesService.exportPerCustomer({
      startDate: this.startDate,
      endDate: this.endDate,
      custGroup: this.lastGeneratedGroup
    }).subscribe({
      next: (blob) => {
        this.downloadBlob(blob, `PerCustomer-${this.lastGeneratedGroup}.zip`);
        this.spinner.hide();
        this.toastr.success('Individual customer files generated in ZIP');
      },
      error: () => { this.spinner.hide(); this.toastr.error('Export failed'); }
    });
  }

  sunLifeXLS(): void {
    if (!this.isDataGenerated) {
      this.toastr.warning('Please generate data first');
      return;
    }
    this.spinner.show();
    const request: CustomerSalesRequest = {
      startDate: this.startDate,
      endDate: this.endDate,
      custGroup: this.lastGeneratedGroup
    };
    this.salesService.exportSunLife(request).subscribe({
      next: (blob) => {
        this.downloadBlob(blob, `SunLife-${this.lastGeneratedGroup}.xlsx`);
        this.spinner.hide();
        this.toastr.success('SunLife report exported');
      },
      error: () => { this.spinner.hide(); this.toastr.error('SunLife export failed'); }
    });
  }

  splitPayment(): void {
    if (!this.isDataGenerated) {
      this.toastr.warning('Please generate data first');
      return;
    }
    this.spinner.show();
    const request: CustomerSalesRequest = {
      startDate: this.startDate,
      endDate: this.endDate,
      custGroup: this.lastGeneratedGroup
    };
    const format = this.selectedFormat === 'CSV' ? 'CSV' : 'Excel';
    this.salesService.exportSplitPayment(request, format).subscribe({
      next: (blob) => {
        this.downloadBlob(blob, `SplitPayment-${this.lastGeneratedGroup}.${format === 'CSV' ? 'csv' : 'xlsx'}`);
        this.spinner.hide();
        this.toastr.success('Split Payment report exported');
      },
      error: () => { this.spinner.hide(); this.toastr.error('Split Payment export failed'); }
    });
  }

  private downloadBlob(blob: Blob, filename: string): void {
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
    document.body.removeChild(a);
  }
}
