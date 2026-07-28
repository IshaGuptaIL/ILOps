import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ArCollectionService, ARCollectionUser, TerritoryGroup } from '../ar-collection.service';
import { SpinnerService } from '../../../shared/spinner/spinner-service';
import { ToastrService } from 'ngx-toastr';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-ar-collection-users',
  standalone: true,
  imports: [CommonModule, FormsModule, PaginationComponent],
  templateUrl: './ar-collection-users.html',
  styleUrl: './ar-collection-users.css'
})
export class ArCollectionUsersComponent implements OnInit {
  // Lists
  users: ARCollectionUser[] = [];
  territoryGroups: TerritoryGroup[] = [];

  // Paging
  currentPage: number = 1;
  pageSize: number = 5; // Access forms typically show a small set
  totalItems: number = 0;
  totalPages: number = 1;

  // Loading indicator state
  isLoading: boolean = false;

  // Bindings for new row in datasheet
  newDomainUser: string = '';
  newInitials: string = '';
  newDefaultChannel: number = 0;

  constructor(
    private arService: ArCollectionService,
    private spinner: SpinnerService,
    private toastr: ToastrService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadTerritoryGroups();
    this.loadUsers();
    this.cdr.detectChanges();
  }

  loadTerritoryGroups(): void {
    this.arService.getTerritoryGroups().subscribe({
      next: (groups) => {
        this.territoryGroups = groups;
        this.cdr.detectChanges();
      },
      error: () => {
        this.toastr.error('Failed to load territory groups');
        this.cdr.detectChanges();
      }
    });
    this.cdr.detectChanges();
  }

  loadUsers(): void {
    this.isLoading = true;
    this.spinner.show();
    this.cdr.detectChanges();

    // setTimeout is added intentionally for simulated async loading state
    setTimeout(() => {
      this.arService.getARUsers(this.currentPage, this.pageSize).subscribe({
        next: (response) => {
          this.users = response.data;
          this.totalItems = response.total;
          this.totalPages = Math.ceil(this.totalItems / this.pageSize) || 1;
          this.isLoading = false;
          this.spinner.hide();
          this.cdr.detectChanges();
        },
        error: () => {
          this.isLoading = false;
          this.spinner.hide();
          this.toastr.error('Failed to load users');
          this.cdr.detectChanges();
        }
      });
    }, 600);
  }

  onPageChange(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.loadUsers();
    }
    this.cdr.detectChanges();
  }

  saveUserInline(user: ARCollectionUser): void {
    if (!user.domainUser.trim()) {
      this.toastr.warning('Domain User is required.');
      this.loadUsers(); // revert changes locally
      this.cdr.detectChanges();
      return;
    }

    this.isLoading = true;
    this.spinner.show();
    this.cdr.detectChanges();

    setTimeout(() => {
      this.arService.updateARUser(user).subscribe({
        next: (success) => {
          this.isLoading = false;
          this.spinner.hide();
          if (success) {
             this.toastr.success('User updated successfully.');
             this.loadUsers();
          } else {
             this.toastr.error('Failed to update user.');
             this.loadUsers();
          }
          this.cdr.detectChanges();
        },
        error: () => {
          this.isLoading = false;
          this.spinner.hide();
          this.toastr.error('Error occurred while updating user.');
          this.loadUsers();
          this.cdr.detectChanges();
        }
      });
    }, 600);
  }

  createUserInline(): void {
    if (!this.newDomainUser.trim()) {
      this.toastr.warning('Domain User Name is required.');
      this.cdr.detectChanges();
      return;
    }

    const payload: ARCollectionUser = {
      domainUser: this.newDomainUser.trim(),
      initials: this.newInitials.trim() || undefined,
      defaultChannel: this.newDefaultChannel > 0 ? this.newDefaultChannel : undefined
    };

    this.isLoading = true;
    this.spinner.show();
    this.cdr.detectChanges();

    setTimeout(() => {
      this.arService.createARUser(payload).subscribe({
        next: (success) => {
          this.isLoading = false;
          this.spinner.hide();
          if (success) {
            this.toastr.success('User added successfully.');
            this.newDomainUser = '';
            this.newInitials = '';
            this.newDefaultChannel = 0;
            this.loadUsers();
          } else {
            this.toastr.error('Failed to add user.');
          }
          this.cdr.detectChanges();
        },
        error: () => {
          this.isLoading = false;
          this.spinner.hide();
          this.toastr.error('Error occurred while adding user.');
          this.cdr.detectChanges();
        }
      });
    }, 600);
  }

  deleteUser(user: ARCollectionUser): void {
    if (!user.id) {
      this.cdr.detectChanges();
      return;
    }

    Swal.fire({
      title: 'Remove User?',
      text: `Are you sure you want to delete user ${user.domainUser}?`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Yes, Delete',
      cancelButtonText: 'Cancel',
      confirmButtonColor: '#d33',
      cancelButtonColor: '#3085d6'
    }).then((result) => {
      if (result.isConfirmed) {
        this.isLoading = true;
        this.spinner.show();
        this.cdr.detectChanges();

        setTimeout(() => {
          this.arService.deleteARUser(user.id!).subscribe({
            next: (success) => {
              this.isLoading = false;
              this.spinner.hide();
              if (success) {
                this.toastr.success('User deleted successfully.');
                this.loadUsers();
              } else {
                this.toastr.error('Failed to delete user.');
              }
              this.cdr.detectChanges();
            },
            error: () => {
              this.isLoading = false;
              this.spinner.hide();
              this.toastr.error('Error occurred while deleting user.');
              this.cdr.detectChanges();
            }
          });
        }, 600);
      }
      this.cdr.detectChanges();
    });
    this.cdr.detectChanges();
  }
}
