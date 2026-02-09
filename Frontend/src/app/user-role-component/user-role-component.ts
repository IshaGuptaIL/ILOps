import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { RoleService } from './role-service';
import { CommonModule } from '@angular/common';
import { SpinnerService } from '../components/shared/spinner/spinner-service';

@Component({
  selector: 'app-user-role-component',
  templateUrl: './user-role-component.html',
  styleUrls: ['./user-role-component.css'],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
})
export class UserRoleComponent implements OnInit {

  roles: any[] = [];
  roleForm!: FormGroup;
  isEdit = false;

  // Pagination
  currentPage = 1;
  pageSize = 5;
  totalPages = 0;

  constructor(
    private fb: FormBuilder,
    private roleService: RoleService,
    private toastr: ToastrService,
    private spinner: SpinnerService
  ) {}

  ngOnInit(): void {
    this.buildForm();
    this.loadRoles();
  }

  // ================= FORM =================
  buildForm() {
    this.roleForm = this.fb.group({
      id: [null],
      name: ['', Validators.required]
    });
  }

  // ================= LOAD ROLES =================
  loadRoles() {
    // this.spinner.show();

    this.roleService.getRoles().subscribe({
      next: (res: any) => {
        // this.spinner.hide();
        if (res.success) {
          this.roles = res.result;
            this.currentPage = 1;
          this.calculatePagination();
        } else {
          this.toastr.error(res.message, 'Roles');
        }
      },
      error: () => {
        this.spinner.hide();
        this.toastr.error('Failed to load roles', 'Error');
      }
    });
  }

  // ================= ADD ROLE =================
  addRole() {
    if (this.roleForm.invalid) return;

    this.spinner.show();

    this.roleService.createRole(this.roleForm.value).subscribe({
      next: (res: any) => {
        this.spinner.hide();
        if (res.success) {
          this.toastr.success(res.message, 'Add Role');
          this.resetForm();
          this.loadRoles();
        } else {
          this.toastr.error(res.message, 'Add Role');
        }
      },
      error: () => {
        this.spinner.hide();
        this.toastr.error('Failed to add role', 'Error');
      }
    });
  }

  // ================= EDIT =================
  editRole(role: any) {
    this.isEdit = true;
    this.roleForm.patchValue({
      id: role.id,
      name: role.name
    });
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  // ================= UPDATE =================
  updateRole() {
    if (this.roleForm.invalid) return;

    this.spinner.show();

    this.roleService.updateRoleForm(this.roleForm.value).subscribe({
      next: (res: any) => {
        this.spinner.hide();
        if (res.success) {
          this.toastr.success(res.message, 'Update Role');
          this.resetForm();
          this.loadRoles();
        } else {
          this.toastr.error(res.message, 'Update Role');
        }
      },
      error: () => {
        this.spinner.hide();
        this.toastr.error('Failed to update role', 'Error');
      }
    });
  }

  // ================= TOGGLE ACTIVE =================
  toggleActive(role: any) {
    if (!confirm(`Toggle ${role.name} status?`)) return;

    this.spinner.show();

    this.roleService.toggleActive(role.id).subscribe({
      next: (res: any) => {
        this.spinner.hide();
        if (res.success) {
          this.toastr.success(res.message, 'Toggle Status');
          this.loadRoles();
        } else {
          this.toastr.error(res.message, 'Toggle Status');
        }
      },
      error: () => {
        this.spinner.hide();
        this.toastr.error('Failed to toggle status', 'Error');
      }
    });
  }

  // ================= RESET =================
  resetForm() {
    this.isEdit = false;
    this.roleForm.reset();
  }

  // ================= PAGINATION =================
get pagedRoles() {
    if (!this.roles || this.roles.length === 0) return [];
    const start = (this.currentPage - 1) * this.pageSize;
    return this.roles.slice(start, start + this.pageSize);
  }

  calculatePagination() {
    this.totalPages = Math.ceil(this.roles.length / this.pageSize);
    if (this.currentPage > this.totalPages) this.currentPage = this.totalPages || 1;
    if (this.currentPage < 1) this.currentPage = 1;
  }

  changePage(page: number) {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
    }
  }

  get pages() {
    if (this.totalPages <= 1) return [];
    return Array.from({ length: this.totalPages }, (_, i) => i + 1);
  }
}
