import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-imei-components',
  imports: [CommonModule],
  templateUrl: './imei-components.html',
  styleUrl: './imei-components.css',
})
export class ImeiComponents {
 constructor(private router: Router) {}

  navigate(action: string) {
    switch (action) {
      case 'receive-imei':
        this.router.navigate(['/inventory/imei/receive']);
        break;

      case 'invoice-credit':
        this.router.navigate(['/inventory/invoice-credit']);
        break;

      case 'reports':
        this.router.navigate(['/inventory/imei/reports']);
        break;

      case 'find-imei':
        this.router.navigate(['/inventory/find-imei']);
        break;

      case 'reverse-receipt':
        this.router.navigate(['/inventory/imei/reverse']);
        break;

      case 'imei-exceptions':
        this.router.navigate(['/inventory/imei/exceptions']);
        break;

      case 'exit':
        this.router.navigate(['/dashboard']);
        break;
    }
  }
}
