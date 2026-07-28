import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface HydroOnePostPaymentRequest {
  invoiceNo: string;
}

export interface HydroOneGenerateMemoRequest {
  invoiceNo: string;
  amount: number;
  cardType: string;
  webOrderID: string;
}

@Injectable({
  providedIn: 'root'
})
export class HydroSalesService {
  private apiUrl = `${environment.apiUrl}/HydroSales`;

  constructor(private http: HttpClient) { }

  postPayment(request: HydroOnePostPaymentRequest): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/PostPayment`, request);
  }

  generateMemo(request: HydroOneGenerateMemoRequest): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/GenerateMemo`, request);
  }
}
