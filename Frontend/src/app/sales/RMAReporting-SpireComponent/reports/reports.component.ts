import { Component, OnInit, Inject, PLATFORM_ID } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { SpinnerService } from '../../../components/shared/spinner/spinner-service';
import { environment } from '../../../../environments/environment';

export interface GenericReportRow {
  id: number;
  col1?: string;
  col2?: string;
  col3?: string;
  col4?: string;
  col5?: string;
  col6?: string;
  col7?: string;
  col8?: string;
  col9?: string;
  col10?: string;
  amount1?: number;
  amount2?: number;
  date1?: string;
  date2?: string;
  status?: string;
}

@Component({
  selector: 'app-rma-reports',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './reports.component.html',
  styleUrls: ['./reports.component.css']
})
export class RMAReportsComponent implements OnInit {
  apiUrl = `${environment.apiUrl}/sales/rmareporting/reports`;

  startDate: string = '2015-01-01';
  endDate: string = '2015-12-31';

  activeQuery: string = 'creditMatches';
  queryTitle: string = 'Credit Matches';

  results: GenericReportRow[] = [];
  selectedRowIndex: number = 0;

  statusMessage: string = '';
  errorMessage: string = '';

  constructor(
    private http: HttpClient,
    public spinnerService: SpinnerService,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {}

  ngOnInit(): void {
    if (isPlatformBrowser(this.platformId)) {
      this.runQuery('creditMatches', 'Credit Matches');
    }
  }

  runQuery(type: string, title: string): void {
    this.activeQuery = type;
    this.queryTitle = title;
    this.statusMessage = '';
    this.errorMessage = '';
    this.spinnerService.show();

    this.http.get<GenericReportRow[]>(`${this.apiUrl}/query`, {
      params: { queryType: type, startDate: this.startDate, endDate: this.endDate }
    }).subscribe({
      next: (data) => {
        this.results = data || [];
        this.selectedRowIndex = 0;
        this.spinnerService.hide();
      },
      error: (err) => {
        this.errorMessage = 'Failed to execute query.';
        this.spinnerService.hide();
      }
    });
  }

  exportExcel(): void {
    this.spinnerService.show();
    const url = `${this.apiUrl}/export?queryType=${this.activeQuery}&startDate=${this.startDate}&endDate=${this.endDate}`;
    window.open(url, '_blank');
    this.spinnerService.hide();
  }

  readRogersReturns(): void {
    this.spinnerService.show();
    this.http.post<any>(`${this.apiUrl}/read-returns`, null, {
      params: { startDate: this.startDate, endDate: this.endDate }
    }).subscribe({
      next: (res) => {
        this.statusMessage = res?.message || 'Read In Rogers Returns process completed.';
        this.spinnerService.hide();
      },
      error: (err) => {
        this.errorMessage = 'Error running Read In Rogers Returns.';
        this.spinnerService.hide();
      }
    });
  }
}
