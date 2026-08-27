import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { RogersReportImportComponent } from './rogers-report-import/rogers-report-import.component';
import { ImeiSearchComponent } from './imei-search/imei-search.component';
import { RMAReportsComponent } from './reports/reports.component';
import { RMAUtilitiesComponent } from './utilities/utilities.component';
import { SpinnerService } from '../../shared/spinner/spinner-service';
import { Spinner } from '../../shared/spinner/spinner';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-rma-reporting-spire',
  standalone: true,
  imports: [
    CommonModule, 
    FormsModule, 
    Spinner, 
    RogersReportImportComponent, 
    ImeiSearchComponent,
    RMAReportsComponent,
    RMAUtilitiesComponent
  ],
  templateUrl: './rma-reporting-spire.component.html',
  styleUrls: ['./rma-reporting-spire.component.css']
})
export class RMAReportingSpireComponent implements OnInit {
  
  // Active screen view: 'imeiSearch' | 'rogersReportImport' | 'reports2' | 'utilities'
  activeScreen: string = 'imeiSearch';

  constructor(
    private router: Router,
    public spinnerService: SpinnerService
  ) {}

  ngOnInit(): void {
    // Default to imeiSearch
  }

  navigate(screen: string): void {
    if (screen === 'exit') {
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
      return;
    }
    if (screen === 'previous') {
      this.activeScreen = 'utilities';
      return;
    }
    this.activeScreen = screen;
  }
}
