import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RoleService } from '../../user-role-component/role-service';
import { CommonModule } from '@angular/common';
import { ToastrService } from 'ngx-toastr';
import { SpinnerService } from '../shared/spinner/spinner-service';

declare var bootstrap: any;

@Component({
  selector: 'app-user-component',
  templateUrl: './user-component.html',
  imports: [CommonModule, ReactiveFormsModule],
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
    private spinner:SpinnerService
  ) {}

  ngOnInit() {
    this.buildForm();
    this.loadUsers();
    this.loadRoles();
  }

   loadRoles() {
    this.spinner.show();  // ✅ Show before API
    this.userService.getRoles().subscribe({
      next: (res: any) => {
        if (res.success) {
          this.roles = res.result;
        } else {
          this.toastr.error(res.message, 'Roles');
        }
        this.spinner.hide();  // ✅ Hide after success
      },
      error: (err) => {
        this.toastr.error('Failed to load roles', 'Error');
        this.spinner.hide();  // ✅ Hide on error
      }
    });
  }


  buildForm() {
    this.userForm = this.fb.group({
      id: [0],
      fullName: ['', Validators.required],
      email: ['', Validators.required],
      contactNumber: [''],
      password: [''],
      userRoleId: ['', Validators.required],
      isActive: [true]
    });
  }

  loadUsers() {
    this.spinner.show();  // ✅ Show before API
    this.userService.getUsers(this.currentPage, this.pageSize).subscribe({
      next: (res: any) => {
        if (res.success) {
          this.users = res.result;
          this.totalUsers = res.count;
          this.totalPages = Math.ceil(this.totalUsers / this.pageSize);
        } else {
          this.toastr.error(res.message, 'Users');
        }
        this.spinner.hide();  // ✅ Hide after success
      },
      error: () => {
        this.toastr.error('Failed to load users', 'Error');
        this.spinner.hide();  // ✅ Hide on error
      }
    });
  }

  openModal() {
    this.isEdit = false;
    this.userForm.reset({ isActive: true });
    new bootstrap.Modal('#userModal').show();
  }

 editUser(id: number) {
    this.isEdit = true;
    this.spinner.show();  // ✅ Show before API
    this.userService.getUserById(id).subscribe({
      next: (res: any) => {
        if (res.success) {
          this.userForm.patchValue(res.result);
          new bootstrap.Modal('#userModal').show();
        } else {
          this.toastr.error(res.message, 'Edit User');
        }
        this.spinner.hide();  // ✅ Hide after success
      },
      error: () => {
        this.toastr.error('Failed to fetch user', 'Error');
        this.spinner.hide();  // ✅ Hide on error
      }
    });
  }

  // ✅ saveUser - Spinner fix
  saveUser() {
    if (this.userForm.invalid) return;

    this.spinner.show();  // ✅ Show before API

    const formData = { 
      ...this.userForm.value, 
      userRoleId: Number(this.userForm.value.userRoleId),
      id: this.isEdit ? Number(this.userForm.value.id) : 0
    };

    const apiCall = this.isEdit
      ? this.userService.updateUser(formData)
      : this.userService.createUser(formData);

    apiCall.subscribe({
      next: (res: any) => {
        this.spinner.hide();  // ✅ Hide after success
        if (res.success) {
          bootstrap.Modal.getInstance(document.getElementById('userModal'))?.hide();
          this.loadUsers();
          this.toastr.success(this.isEdit ? 'User updated successfully' : 'User created successfully');
        } else {
          this.toastr.error(res.message, 'Save User');
        }
      },
      error: () => {
        this.spinner.hide();  // ✅ Hide on error
        this.toastr.error('Failed to save user', 'Error');
      }
    });
  }

  // ✅ deleteUser - Spinner fix
  deleteUser(id: number) {
    if (!confirm('Deactivate this user?')) return;

    this.spinner.show();  // ✅ Show before API
    this.userService.deleteUser(id).subscribe({
      next: (res: any) => {
        this.spinner.hide();  // ✅ Hide after success
        if (res.success) {
          this.loadUsers();
          this.toastr.success('User deactivated successfully');
        } else {
          this.toastr.error(res.message, 'Delete User');
        }
      },
      error: () => {
        this.spinner.hide();  // ✅ Hide on error
        this.toastr.error('Failed to delete user', 'Error');
      }
    });
  }

  changePage(page: number) {
    if (page < 1 || page > this.totalPages) return;
    this.currentPage = page;
    this.loadUsers();
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
