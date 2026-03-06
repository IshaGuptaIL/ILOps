import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RoleService } from '../../user-role-component/role-service';
import { ToastrService } from 'ngx-toastr'; // ✅ Toastr Import
import { finalize } from 'rxjs/operators';
import { SpinnerService } from '../shared/spinner/spinner-service';

@Component({
  selector: 'app-role-permission',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './role-permission-component.html',
  styleUrl: './role-permission-component.css'
})
export class RolePermissionComponent implements OnInit {
  roles: any[] = [];
  menus: any[] = [];
  selectedRoleId: number | null = null;
  selectedMenuIds: number[] = [];

  constructor(
    private roleService: RoleService,
    private toastr: ToastrService,       // ✅ Inject Toastr
    private spinner: SpinnerService,    // ✅ Inject Spinner
    private cdr: ChangeDetectorRef      // ✅ Inject ChangeDetector
  ) {}

  ngOnInit() {
    this.loadInitialData();
  }

  loadInitialData() {
    this.spinner.show(); // ✅ Show Spinner
    
    // Roles aur Menus dono ko load karein
    this.roleService.getActiveRoles().subscribe({
      next: (res) => {
        if (res.success) this.roles = res.result;
        this.cdr.detectChanges();
      },
      error: () => this.toastr.error("Failed to load roles")
    });

    this.roleService.getMenus().pipe(
      finalize(() => this.spinner.hide()) // ✅ Hide Spinner when done
    ).subscribe({
      next: (res) => {
        if (res.success) this.menus = res.result;
        this.cdr.detectChanges();
      },
      error: () => this.toastr.error("Failed to load menus")
    });
  }

  onRoleChange() {
    if (this.selectedRoleId) {
      this.spinner.show();
      this.roleService.getRolePermissions(this.selectedRoleId).pipe(
        finalize(() => {
          this.spinner.hide();
          this.cdr.detectChanges();
        })
      ).subscribe({
        next: (res) => {
          this.selectedMenuIds = res.success ? res.result : [];
          if (res.success) {
          }
        },
        error: () => this.toastr.error("Failed to fetch role permissions")
      });
    } else {
      this.selectedMenuIds = [];
    }
  }

  savePermissions() {
    if (!this.selectedRoleId) {
      this.toastr.warning("Please select a role first!"); // ✅ Warning Toast
      return;
    }

    this.spinner.show();
    this.roleService.saveRolePermissions(this.selectedRoleId, this.selectedMenuIds).pipe(
      finalize(() => this.spinner.hide())
    ).subscribe({
      next: (res) => {
        if (res.success) {
          this.toastr.success(res.message || "Permissions updated successfully!"); // ✅ Success Toast
        } else {
          this.toastr.error(res.message || "Failed to update permissions");
        }
      },
      error: (err) => {
        this.toastr.error("An error occurred while saving");
        console.error(err);
      }
    });
  }

  // --- UI Helpers ---
  toggleMenu(menuId: number) {
    const index = this.selectedMenuIds.indexOf(menuId);
    if (index > -1) {
      this.selectedMenuIds.splice(index, 1);
    } else {
      this.selectedMenuIds.push(menuId);
    }
  }

  isMenuSelected(menuId: number): boolean {
    return this.selectedMenuIds.includes(menuId);
  }

  getParentMenus() {
    return this.menus.filter(m => !m.parentId || m.parentId === 0);
  }

  getChildMenus(parentId: number) {
    return this.menus.filter(m => m.parentId === parentId);
  }
}