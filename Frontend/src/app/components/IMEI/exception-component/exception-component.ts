import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

@Component({
  selector: 'app-exception-component',
  imports: [CommonModule,FormsModule],
  templateUrl: './exception-component.html',
  styleUrl: './exception-component.css',
})
export class ExceptionComponent {




    password: string = '';

  constructor(private router: Router) {}

  checkPassword() {
    if (this.password !== 'subaru') {
      alert('Password incorrect');
      return;
    }

    this.router.navigate(['/frmIMEIExceptions']);
  }

}
