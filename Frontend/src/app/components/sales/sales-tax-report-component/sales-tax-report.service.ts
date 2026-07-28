import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface SalesTaxReportRequest {
  startDate: string;
  endDate: string;
}

export interface SalesTaxReportRow {
  trans: number;
  invdate: string;
  invoice: string;
  webOrderID: string;
  source: string;
  custNo: string;
  custName: string;
  territory: string;
  shipToProvince: string;
  postalDigit: string;
  oneIMEI: string;
  tax1Code: number;
  tax1Name: string;
  tax1GL: string;
  tax2Code: number;
  tax2Name: string;
  tax2GL: string;
  invoiceNet: number;
  tax1Total: number;
  tax2Total: number;
  shippingAmt: number;
  invoiceTotal: number;
  totalOfExtendedSell: number;
  departmentSales: { [key: string]: number };
}

export interface SalesTaxReportResponse {
  data: SalesTaxReportRow[];
  departmentNames: string[];
}

export interface TaxCodeHistory {
  id: number;
  provCode: string;
  provinceName: string;
  tax1Rate: number;
  tax2Rate: number;
  taxType: string;
  startDate: string;
  endDate: string;
  comments: string;
  compoundTax2OnTax1: boolean;
}

export interface VendorBO {
  vendorNo: string;
  name: string;
}

@Injectable({
  providedIn: 'root'
})
export class SalesTaxReportService {
  private apiUrl = `${environment.apiUrl}/SalesTaxReport`;

  constructor(private http: HttpClient) { }
  
  loadSalesHistory(request: SalesTaxReportRequest): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiUrl}/LoadSalesHistory`, request);
  }

  loadGLData(request: SalesTaxReportRequest): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiUrl}/LoadGLData`, request);
  }


  getReport(request: SalesTaxReportRequest): Observable<SalesTaxReportResponse> {
    return this.http.post<SalesTaxReportResponse>(`${this.apiUrl}/GetReport`, request);
  }

  exportExcel(request: SalesTaxReportRequest): Observable<Blob> {
    return this.http.post(`${this.apiUrl}/ExportExcel`, request, { responseType: 'blob' });
  }

  exportVendorActivity(vendor: string, startDate: string, endDate: string): Observable<Blob> {
    return this.http.post(`${this.apiUrl}/ExportVendorActivity`, { vendor, startDate, endDate }, { responseType: 'blob' });
  }

  exportGLITCExcel(request: SalesTaxReportRequest): Observable<Blob> {
    return this.http.post(`${this.apiUrl}/ExportGLITCExcel`, request, { responseType: 'blob' });
  }

  exportGLDataExcel(request: SalesTaxReportRequest): Observable<Blob> {
    return this.http.post(`${this.apiUrl}/ExportGLDataExcel`, request, { responseType: 'blob' });
  }

  // ─── TAX CODE HISTORY ───────────────────────────────────────────

  getTaxCodeHistory(): Observable<TaxCodeHistory[]> {
    return this.http.get<TaxCodeHistory[]>(`${this.apiUrl}/GetTaxCodeHistory`);
  }

  saveTaxCodeHistory(history: TaxCodeHistory): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiUrl}/SaveTaxCodeHistory`, history);
  }

  deleteTaxCodeHistory(id: number): Observable<boolean> {
    return this.http.delete<boolean>(`${this.apiUrl}/DeleteTaxCodeHistory/${id}`);
  }

  getVendors(): Observable<VendorBO[]> {
    return this.http.get<VendorBO[]>(`${this.apiUrl}/GetVendors`);
  }
}
