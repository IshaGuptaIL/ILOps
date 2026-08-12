import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { Auth } from './auth';
import { SpinnerService } from '../shared/spinner/spinner-service';
import { CookieService } from 'ngx-cookie-service';
import { delay } from 'rxjs';



@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    FormsModule,
    CommonModule,
    ReactiveFormsModule
  ],
  templateUrl: './login.html',
  styleUrls: ['./login.css'],
})
export class Login {
  loginForm: FormGroup;
  submitted = false;
  
  showPassword = false;

  constructor(
    private fb: FormBuilder,
    private authService: Auth,
    private router: Router,
    private toastr: ToastrService,
    private spinner: SpinnerService,
    private cookieService: CookieService // ✅ COOKIE INJECT
  ) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required],
    });
  }
 togglePasswordVisibility() {
    this.showPassword = !this.showPassword;
  }

 onSubmit() {
  debugger
  this.submitted = true;

  if (this.loginForm.invalid) {
    return;
  }

  this.spinner.show();

  this.authService.login(this.loginForm.value)
    .pipe(delay(0)) // ✅ prevents ExpressionChangedAfterItHasBeenCheckedError
    .subscribe({
      next: (res: any) => {
        if (res.success) {

          this.cookieService.set('token', res.token, 3);
          this.cookieService.set('UserID', res.result.userId, 3);
          this.cookieService.set('UserRoleId', res.result.userRoleId, 3);
          this.cookieService.set('Name', res.result.name, 3);
          this.cookieService.set('Email', res.result.email, 3);

          // Compute user initials from name (e.g. "Super Admin" -> "SA")
          let initials = 'SA';
          if (res.result.name) {
            const parts = res.result.name.trim().split(/\s+/);
            if (parts.length >= 2) {
              initials = (parts[0][0] + parts[1][0]).toUpperCase();
            } else if (parts.length === 1 && parts[0].length >= 2) {
              initials = parts[0].substring(0, 2).toUpperCase();
            } else if (parts.length === 1 && parts[0].length === 1) {
              initials = parts[0].toUpperCase() + 'A';
            }
          }
          this.cookieService.set('userInitials', initials, 3);

          this.toastr.success('Login successful', 'Success');
          this.router.navigate(['/dashboard']);
        } else {
          this.toastr.error('Invalid email or password', 'Login Failed');
        }

        this.spinner.hide();
      },
      error: () => {
        this.toastr.error('Something went wrong. Try again!', 'Error');
        this.spinner.hide();
      },
    });
}



  get f() {
  return this.loginForm.controls as {
    email: any;
    password: any;
  };
}
}
