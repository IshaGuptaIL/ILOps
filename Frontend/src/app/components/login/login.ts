import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { Auth } from './auth';
import { SpinnerService } from '../shared/spinner/spinner-service';
import { CookieService } from 'ngx-cookie-service';

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

  onSubmit() {
    this.spinner.show();
    this.submitted = true;

    if (this.loginForm.invalid) {
      this.spinner.hide();
      return;
    }

    debugger
    this.authService.login(this.loginForm.value).subscribe({
      next: (res: any) => {
        if (res.success) {
          

          this.cookieService.set('token', res.token, 1); // 1 day
          this.cookieService.set('UserID', res.result.userId, 1); // 1 day
          this.cookieService.set('UserRoleId', res.result.userRoleId, 1); // 1 day
          this.cookieService.set('Name', res.result.name, 1); // 1 day
          this.cookieService.set('Email', res.result.email, 1); // 1 day


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
