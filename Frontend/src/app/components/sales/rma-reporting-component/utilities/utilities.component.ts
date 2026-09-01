import { Component, OnInit, Inject, PLATFORM_ID, Output, EventEmitter, ChangeDetectorRef } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { SpinnerService } from '../../../shared/spinner/spinner-service';
import { Spinner } from '../../../shared/spinner/spinner';
import { environment } from '../../../../../environments/environment';
import Swal from 'sweetalert2';

export interface RMAUser {
  id: number;
  userName: string;
  userInitials: string;
  userRole: string;
  isActive: boolean;
}

@Component({
  selector: 'app-rma-utilities',
  standalone: true,
  imports: [CommonModule, FormsModule, Spinner],
  templateUrl: './utilities.component.html',
  styleUrls: ['./utilities.component.css']
})
export class RMAUtilitiesComponent implements OnInit {
  apiUrl = `${environment.apiUrl}/sales/rmareporting/utilities`;

  @Output() navigateTab = new EventEmitter<string>();

  // Switchboard navigation state
  activeView: 'switchboard' | 'mainSwitchboard' | 'editUsers' = 'switchboard';

  users: RMAUser[] = [];
  selectedUserIndex: number = 0;

  // Add/Edit User Modal
  showUserModal: boolean = false;
  editingUser: RMAUser = { id: 0, userName: '', userInitials: '', userRole: 'User', isActive: true };

  statusMessage: string = '';
  errorMessage: string = '';

  constructor(
    private http: HttpClient,
    private router: Router,
    public spinnerService: SpinnerService,
    private cdr: ChangeDetectorRef,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {}

  ngOnInit(): void {
    if (isPlatformBrowser(this.platformId)) {
      this.loadUsers();
    }
  }

  // Switchboard Button Handlers
  onOptionClick(option: string): void {
    switch (option) {
      case 'imeiSearch':
        this.navigateTab.emit('imeiSearch');
        break;
      case 'editUsers':
        this.activeView = 'editUsers';
        this.loadUsers();
        break;
      case 'previousMenu':
        this.activeView = this.activeView === 'editUsers' ? 'switchboard' : 'mainSwitchboard';
        this.cdr.detectChanges();
        break;
      case 'exitApplication':
        this.exitApplication();
        break;
      case 'rogersReportImport':
        this.navigateTab.emit('rogersReportImport');
        break;
      case 'reports2':
        this.navigateTab.emit('reports2');
        break;
      case 'utilities':
        this.activeView = 'switchboard';
        this.cdr.detectChanges();
        break;
    }
  }

  exitApplication(): void {
    Swal.fire({
      title: 'Exit Application?',
      text: 'Are you sure you want to exit the RMA Reporting Module?',
      icon: 'question',
      showCancelButton: true,
      confirmButtonColor: '#006666',
      cancelButtonColor: '#d33',
      confirmButtonText: 'Yes, Exit',
      cancelButtonText: 'Cancel'
    }).then((result) => {
      if (result.isConfirmed) {
        this.router.navigate(['/']);
      }
    });
  }

  loadUsers(): void {
    this.spinnerService.show();
    this.statusMessage = '';
    this.errorMessage = '';

    this.http.get<RMAUser[]>(`${this.apiUrl}/users`).subscribe({
      next: (data) => {
        this.users = data || [];
        this.selectedUserIndex = 0;
        this.spinnerService.hide();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.errorMessage = 'Failed to load user list.';
        this.spinnerService.hide();
        this.cdr.detectChanges();
      }
    });
  }

  resetDataProcedure(): void {
    Swal.fire({
      title: 'Reset Data Procedure?',
      text: 'Warning: This action will re-initialize staging import calculation tables.',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      cancelButtonColor: '#3085d6',
      confirmButtonText: 'Yes, Reset',
      cancelButtonText: 'Cancel'
    }).then((result) => {
      if (result.isConfirmed) {
        this.spinnerService.show();
        this.http.post<any>(`${this.apiUrl}/reset-data`, null).subscribe({
          next: (res) => {
            this.statusMessage = res?.message || 'Reset data procedure completed successfully.';
            this.spinnerService.hide();
            this.cdr.detectChanges();
            Swal.fire({
              icon: 'success',
              title: 'Success',
              text: 'Reset data procedure completed successfully.'
            });
          },
          error: (err) => {
            this.errorMessage = 'Error during reset data procedure.';
            this.spinnerService.hide();
            this.cdr.detectChanges();
          }
        });
      }
    });
  }

  openEditUser(user: RMAUser): void {
    this.editingUser = { ...user };
    this.showUserModal = true;
    this.cdr.detectChanges();
  }

  openAddUser(): void {
    this.editingUser = { id: 0, userName: '', userInitials: '', userRole: 'User', isActive: true };
    this.showUserModal = true;
    this.cdr.detectChanges();
  }

  saveUser(): void {
    if (!this.editingUser.userName || !this.editingUser.userName.trim()) {
      Swal.fire({
        icon: 'warning',
        title: 'Validation',
        text: 'Please enter a User Name.'
      });
      return;
    }

    this.spinnerService.show();
    this.http.post<any>(`${this.apiUrl}/save-user`, this.editingUser).subscribe({
      next: (res) => {
        this.statusMessage = res?.message || 'User saved successfully.';
        this.showUserModal = false;
        this.loadUsers();
        this.spinnerService.hide();
        this.cdr.detectChanges();
        Swal.fire({
          icon: 'success',
          title: 'Saved',
          text: 'User saved successfully.'
        });
      },
      error: (err) => {
        this.errorMessage = 'Error saving user.';
        this.spinnerService.hide();
        this.cdr.detectChanges();
        Swal.fire({
          icon: 'error',
          title: 'Error',
          text: 'Error saving user.'
        });
      }
    });
  }

  closeUserModal(): void {
    this.showUserModal = false;
    this.cdr.detectChanges();
  }
}
