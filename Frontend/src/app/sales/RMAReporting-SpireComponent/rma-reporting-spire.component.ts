import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { RogersReportImportComponent } from './rogers-report-import/rogers-report-import.component';

@Component({
  selector: 'app-rma-reporting-spire',
  standalone: true,
  imports: [CommonModule, RogersReportImportComponent],
  templateUrl: './rma-reporting-spire.component.html',
  styleUrls: ['./rma-reporting-spire.component.css']
})
export class RMAReportingSpireComponent {
  
  menuItems = [
    { text: 'Import Rogers CM And RMA data', id: 'importData' },
    { text: 'Reports', id: 'reports' },
    { text: 'Utilites', id: 'utilities' },
    { text: 'IMEI Search', id: 'imeiSearch' },
    { text: 'Previous Menu', id: 'previousMenu' }
  ];

  activeMenu: string = 'importData';

  constructor(private router: Router) {}

  onMenuClick(menuId: string) {
    if (menuId === 'previousMenu') {
      this.router.navigate(['/']); // or wherever the previous menu is
    } else {
      this.activeMenu = menuId;
    }
  }
}
