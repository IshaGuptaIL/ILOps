import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { Router, RouterModule } from '@angular/router';

@Component({
  selector: 'app-imei-components',
  imports: [CommonModule,RouterModule],
  templateUrl: './imei-components.html',
  styleUrl: './imei-components.css',
})
export class ImeiComponents {
 constructor(private router: Router) {}

  navigate(action: string) {
  this.router.navigate(['/imei', action]);
}
}