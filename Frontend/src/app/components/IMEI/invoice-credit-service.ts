import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environments.development';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ApiResponse {
  success: boolean;
  message: string;
  result?: any;
}

@Injectable({ providedIn: 'root' })
export class InvoiceCreditService {
  private readonly apiUrl = `${environment.apiUrl}/InvoiceCredit`;

  constructor(private http: HttpClient) {}

  getAllReceipts(): Observable<ApiResponse> {
    return this.http.get<ApiResponse>(`${this.apiUrl}/GetAllReceipts`);
  }

  getInvoices(receiptNo: string): Observable<ApiResponse> {
    return this.http.get<ApiResponse>(`${this.apiUrl}/GetInvoices/${receiptNo}`);
  }

  saveInvoice(invoice: any): Observable<ApiResponse> {
    return this.http.post<ApiResponse>(`${this.apiUrl}/SaveInvoice`, invoice);
  }

  loadAccReceipts(): Observable<ApiResponse> {
    return this.http.post<ApiResponse>(`${this.apiUrl}/load-acc`, {});
  }

  getMissingReceiptsByPO(poNumber: string): Observable<ApiResponse> {
    return this.http.get<ApiResponse>(`${this.apiUrl}/GetMissingReceiptsByPO/${poNumber}`);
  }

 findReceiptByBVNo(bvReceiptNo: string, type: string): Observable<ApiResponse> {
    let params = new HttpParams();
    
    if (bvReceiptNo) {
        params = params.set('bvReceiptNo', bvReceiptNo.trim());
    }
    if (type) {
        params = params.set('type', type);
    }
    // poNumber agar khali hai to params mein set hi mat karein
    return this.http.get<ApiResponse>(`${this.apiUrl}/SearchReceipts`, { params });
}
}