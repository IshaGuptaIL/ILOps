import { Component, OnInit } from '@angular/core';
import { Router, RouterOutlet, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-price-protection-dashboard',
  standalone: true,
  imports: [RouterOutlet, CommonModule],
  templateUrl: './price-protection-component.html',
  styleUrl: './price-protection-component.css'
})
export class PriceProtectionDashboardComponent implements OnInit {
  currentRoute: string = 'claims';

  constructor(private router: Router) {}

  ngOnInit(): void {
    this.updateCurrentRoute(this.router.url);
    this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe((event: any) => {
        this.updateCurrentRoute(event.urlAfterRedirects || event.url);
      });
  }

  updateCurrentRoute(url: string): void {
    if (url.includes('apply-credits')) {
      this.currentRoute = 'apply-credits';
    } else if (url.includes('imei-search')) {
      this.currentRoute = 'imei-search';
    } else if (url.includes('output-to-excel')) {
      this.currentRoute = 'output-to-excel';
    } else if (url.includes('roger-overpayments')) {
      this.currentRoute = 'roger-overpayments';
    } else {
      this.currentRoute = 'claims';
    }
  }

  navigate(route: string): void {
    this.router.navigate([`/priceProtection/${route}`]);
  }

  exit(): void {
    this.router.navigate(['/dashboard']);
  }
}
