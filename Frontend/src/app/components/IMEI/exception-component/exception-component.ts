import { CommonModule } from '@angular/common';
import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';
import { environment } from '../../../../environments/environments.development';

@Component({
  selector: 'app-exception-component',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './exception-component.html',
  styleUrl: './exception-component.css',
})
export class ExceptionComponent implements OnInit {
  password: string = '';
  showPassword: boolean = false;
  isAuthenticated: boolean = false;
  activeTab: string = 'length'; // 'length' | 'errors'

  // IMEI Length Exceptions data
  lengthExceptions: any[] = [];
  newException: any = { exceptionPart: '', imeiLength: 15, allowAlpha: false };

  // System Errors data
  systemErrors: any[] = [];

  private apiUrl = environment.apiUrl + '/Exception';

  constructor(
    private http: HttpClient,
    private toastr: ToastrService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {}

  checkPassword() {
    if (this.password !== 'subaru') {
      this.toastr.error('Password incorrect', 'Access Denied');
      this.password = '';
      this.cdr.detectChanges();
      return;
    }
    this.isAuthenticated = true;
    this.loadLengthExceptions();
    this.loadSystemErrors();
    this.cdr.detectChanges();
  }

  loadLengthExceptions() {
    this.http.get<any>(`${this.apiUrl}/GetIMEILengthExceptions`).subscribe({
      next: (res) => {
        if (res.success) {
          this.lengthExceptions = res.result ? [...res.result] : [];
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error loading length exceptions', err);
        this.cdr.detectChanges();
      }
    });
  }

  loadSystemErrors() {
    this.http.get<any>(`${this.apiUrl}/GetExceptions`).subscribe({
      next: (res) => {
        if (res.success) {
          this.systemErrors = res.result ? [...res.result] : [];
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error loading system errors', err);
        this.cdr.detectChanges();
      }
    });
  }

  saveLengthException(item: any) {
    if (!item.exceptionPart) {
      this.toastr.warning('Exception Part is required', 'Validation Error');
      return;
    }
    this.http.post<any>(`${this.apiUrl}/SaveIMEILengthException`, item).subscribe({
      next: (res) => {
        if (res.success) {
          this.toastr.success('Exception saved successfully', 'Success');
          this.loadLengthExceptions();
          if (item === this.newException) {
            this.newException = { exceptionPart: '', imeiLength: 15, allowAlpha: false };
          }
        } else {
          this.toastr.error('Failed to save exception: ' + res.message, 'Error');
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.toastr.error('Error saving exception: ' + err.message, 'Error');
        this.cdr.detectChanges();
      }
    });
  }

  deleteLengthException(part: string) {
    Swal.fire({
      title: 'Are you sure?',
      text: `Are you sure you want to delete the exception for ${part}?`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#3085d6',
      cancelButtonColor: '#d33',
      confirmButtonText: 'Yes, delete it!'
    }).then((result) => {
      if (result.isConfirmed) {
        this.http.delete<any>(`${this.apiUrl}/DeleteIMEILengthException/${part}`).subscribe({
          next: (res) => {
            if (res.success) {
              this.toastr.success('Exception deleted successfully', 'Success');
              this.loadLengthExceptions();
            } else {
              this.toastr.error('Failed to delete exception: ' + res.message, 'Error');
            }
            this.cdr.detectChanges();
          },
          error: (err) => {
            this.toastr.error('Error deleting exception: ' + err.message, 'Error');
            this.cdr.detectChanges();
          }
        });
      }
    });
  }

  resolveError(id: number) {
    this.http.post<any>(`${this.apiUrl}/ResolveException`, { id, userId: 'ADMIN' }).subscribe({
      next: (res) => {
        if (res.success) {
          this.toastr.success('Error marked as resolved', 'Resolved');
          this.loadSystemErrors();
        } else {
          this.toastr.error('Failed to resolve error: ' + res.message, 'Error');
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.toastr.error('Error resolving error: ' + err.message, 'Error');
        this.cdr.detectChanges();
      }
    });
  }

  clearAllErrors() {
    Swal.fire({
      title: 'Clear All Errors?',
      text: 'Are you sure you want to clear all system errors?',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      cancelButtonColor: '#3085d6',
      confirmButtonText: 'Yes, clear all!'
    }).then((result) => {
      if (result.isConfirmed) {
        this.http.delete<any>(`${this.apiUrl}/ClearAllExceptions`).subscribe({
          next: (res) => {
            if (res.success) {
              this.toastr.success('All errors cleared', 'Success');
              this.loadSystemErrors();
            } else {
              this.toastr.error('Failed to clear errors: ' + res.message, 'Error');
            }
            this.cdr.detectChanges();
          },
          error: (err) => {
            this.toastr.error('Error clearing errors: ' + err.message, 'Error');
            this.cdr.detectChanges();
          }
        });
      }
    });
  }
}


