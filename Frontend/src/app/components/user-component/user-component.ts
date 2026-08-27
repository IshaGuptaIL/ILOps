import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RoleService } from '../../user-role-component/role-service';
import { CommonModule } from '@angular/common';
import { ToastrService } from 'ngx-toastr';
import { SpinnerService } from '../shared/spinner/spinner-service';
import { Spinner } from '../shared/spinner/spinner';
import Swal from 'sweetalert2';

declare var bootstrap: any;

@Component({
  selector: 'app-user-component',
  templateUrl: './user-component.html',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, Spinner],
  styleUrls: ['./user-component.css']
})
export class UserComponent implements OnInit {

  users: any[] = [];
  roles: any[] = [];

  currentPage = 1;
  pageSize = 10;
  totalUsers = 0;
  totalPages = 0;

  userForm!: FormGroup;
  isEdit = false;

  constructor(
    private fb: FormBuilder,
    private userService: RoleService,
    private toastr: ToastrService,
    public spinner: SpinnerService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.buildForm();
    this.loadRoles();
    this.loadUsers();
  }

  loadRoles() {
    this.spinner.show();
    this.userService.getRoles().subscribe({
      next: (res: any) => {
        if (res.success) {
          this.roles = res.result || [];
        } else {
          this.toastr.error(res.message, 'Roles');
        }
        this.spinner.hide();
        this.cdr.detectChanges();
      },
      error: () => {
        this.toastr.error('Failed to load roles', 'Error');
        this.spinner.hide();
        this.cdr.detectChanges();
      }
    });
  }

  buildForm() {
    this.userForm = this.fb.group({
      id: [0],
      fullName: ['', [Validators.required, Validators.maxLength(100)]],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(100)]],
      contactNumber: ['', Validators.maxLength(20)],
      password: [''],
      address: ['', Validators.maxLength(200)],
      state: ['', Validators.maxLength(100)],
      zipCode: ['', Validators.maxLength(20)],
      country: ['', Validators.maxLength(100)],
      city: ['', Validators.maxLength(100)],
      userRoleId: ['', Validators.required],
      isActive: [true]
    });
  }

  loadUsers() {
    this.spinner.show();
    this.userService.getUsers(this.currentPage, this.pageSize).subscribe({
      next: (res: any) => {
        if (res.success) {
          this.users = res.result || [];
          this.totalUsers = res.count || this.users.length;
          this.totalPages = Math.ceil(this.totalUsers / this.pageSize) || 1;
        } else {
          this.toastr.error(res.message, 'Users');
        }
        this.spinner.hide();
        this.cdr.detectChanges();
      },
      error: () => {
        this.toastr.error('Failed to load users', 'Error');
        this.spinner.hide();
        this.cdr.detectChanges();
      }
    });
  }

  openModal() {
    this.isEdit = false;
    this.userForm.reset({ id: 0, isActive: true, userRoleId: '' });
    try {
      new bootstrap.Modal('#userModal').show();
    } catch (e) {}
    this.cdr.detectChanges();
  }

  editUser(id: number) {
    this.isEdit = true;
    this.spinner.show();
    this.userService.getUserById(id).subscribe({
      next: (res: any) => {
        if (res.success) {
          this.userForm.patchValue(res.result);
          try {
            new bootstrap.Modal('#userModal').show();
          } catch (e) {}
        } else {
          this.toastr.error(res.message, 'Edit User');
        }
        this.spinner.hide();
        this.cdr.detectChanges();
      },
      error: () => {
        this.toastr.error('Failed to fetch user', 'Error');
        this.spinner.hide();
        this.cdr.detectChanges();
      }
    });
  }

  saveUser() {
    if (this.userForm.invalid) {
      this.userForm.markAllAsTouched();
      this.toastr.warning('Please fill in all required fields correctly.', 'Validation');
      this.cdr.detectChanges();
      return;
    }

    this.spinner.show();

    const formVal = this.userForm.value;
    const formData = { 
      ...formVal, 
      userRoleId: Number(formVal.userRoleId),
      id: this.isEdit ? Number(formVal.id) : 0
    };

    const apiCall = this.isEdit
      ? this.userService.updateUser(formData)
      : this.userService.createUser(formData);

    apiCall.subscribe({
      next: (res: any) => {
        this.spinner.hide();
        if (res.success) {
          try {
            const modalEl = document.getElementById('userModal');
            if (modalEl) {
              const modalInst = bootstrap.Modal.getInstance(modalEl);
              modalInst?.hide();
            }
          } catch (e) {}
          this.loadUsers();
          this.toastr.success(this.isEdit ? 'User updated successfully' : 'User created successfully', 'Success');
        } else {
          this.toastr.error(res.message, 'Save User');
        }
        this.cdr.detectChanges();
      },
      error: () => {
        this.spinner.hide();
        this.toastr.error('Failed to save user', 'Error');
        this.cdr.detectChanges();
      }
    });
  }

  deleteUser(id: number) {
    Swal.fire({
      title: 'Deactivate User?',
      text: 'Are you sure you want to deactivate this user?',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      cancelButtonColor: '#3085d6',
      confirmButtonText: 'Yes, deactivate',
      cancelButtonText: 'Cancel'
    }).then((result) => {
      if (result.isConfirmed) {
        this.spinner.show();
        this.userService.deleteUser(id).subscribe({
          next: (res: any) => {
            this.spinner.hide();
            if (res.success) {
              this.loadUsers();
              this.toastr.success('User deactivated successfully', 'Success');
            } else {
              this.toastr.error(res.message, 'Delete User');
            }
            this.cdr.detectChanges();
          },
          error: () => {
            this.spinner.hide();
            this.toastr.error('Failed to delete user', 'Error');
            this.cdr.detectChanges();
          }
        });
      }
    });
  }

  changePage(page: number) {
    if (page < 1 || page > this.totalPages) return;
    this.currentPage = page;
    this.loadUsers();
    this.cdr.detectChanges();
  }

  get pages() {
    return Array.from({ length: this.totalPages }, (_, i) => i + 1);
  }

  get startRecord() {
    return (this.currentPage - 1) * this.pageSize + 1;
  }

  get endRecord() {
    return Math.min(this.currentPage * this.pageSize, this.totalUsers);
  }
}
