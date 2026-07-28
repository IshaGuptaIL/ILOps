import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface PostPaymentRequest {
  invoiceNo: string;
  userId?: number;
}

export interface PostPaymentResponse {
  success: boolean;
  message: string;
}

export interface GenerateMemoRequest {
  invoiceNo: string;
  originalAmount: number;
  webOrderID: string;
  cardType: string;
  userId?: number;
}

export interface GenerateMemoResponse {
  success: boolean;
  message: string;
  generatedMemo?: string;
}

@Injectable({
  providedIn: 'root'
})
export class HydroService {
  private apiUrl = `${environment.apiUrl}/Sales/Hydro`;

  constructor(private http: HttpClient) { }

  postPayment(request: PostPaymentRequest): Observable<PostPaymentResponse> {
    return this.http.post<PostPaymentResponse>(`${this.apiUrl}/PostPayment`, request);
  }

  generateMemo(request: GenerateMemoRequest): Observable<GenerateMemoResponse> {
    return this.http.post<GenerateMemoResponse>(`${this.apiUrl}/GenerateMemo`, request);
  }
}
