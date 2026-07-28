import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class InventoryEditService {
  private apiUrl = `${environment.apiUrl}/InventoryEdit`;

  constructor(private http: HttpClient) { }

  // Terms Edit
  getInvoiceTerms(invoiceNo: string): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/GetInvoiceTerms?invoiceNo=${invoiceNo}`);
  }

  updateInvoiceTerms(data: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/UpdateInvoiceTerms`, data);
  }

  // Bulk ID Edit
  getBulkIdCount(bulkId: string): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/GetBulkIdCount?bulkId=${bulkId}`);
  }

  updateBulkId(data: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/UpdateBulkId`, data);
  }

  getSingleInvoiceBulkId(invoiceNo: string): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/GetSingleInvoiceBulkId?invoiceNo=${invoiceNo}`);
  }

  updateSingleInvoiceBulkId(data: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/UpdateSingleInvoiceBulkId`, data);
  }

  updateMultipleBulkIds(data: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/UpdateMultipleBulkIds`, data);
  }

  // Address Edit
  getInvoiceAddress(invoiceNo: string): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/GetInvoiceAddress?invoiceNo=${invoiceNo}`);
  }

  updateInvoiceAddress(data: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/UpdateInvoiceAddress`, data);
  }
}
