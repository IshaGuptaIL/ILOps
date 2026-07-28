import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { Router, RouterModule } from '@angular/router';

@Component({
  selector: 'app-ar-collection-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './ar-collection-dashboard.html',
  styleUrl: './ar-collection-dashboard.css',
})
export class ArCollectionDashboardComponent {
  constructor(private router: Router) {}

  navigate(action: string) {
    if (action === 'exit') {
      this.router.navigate(['/dashboard']);
    } else {
      this.router.navigate(['/arCollection', action]);
    }
  }

  isActive(route: string): boolean {
    return this.router.url.includes(route);
  }

  alertFeature(featureName: string) {
    alert(`${featureName} is not implemented in this version.`);
  }
}
