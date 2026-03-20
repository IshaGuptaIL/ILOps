import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environments.development';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class CustomSearchService {
  


  private readonly baseUrl = `${environment.apiUrl}/CustomSearch`;

  constructor(private http: HttpClient) {}

  getHeaders(fieldName: string, value: string): Observable<any> {
  const params = new HttpParams()
    .set('fieldName', fieldName)
    .set('value', value);

  return this.http.get(`${this.baseUrl}/headers`, { params });
}

getDetails(invoiceNo: string): Observable<any> {
  const params = new HttpParams().set('invoiceNo', invoiceNo);
  return this.http.get(`${this.baseUrl}/details`, { params });
}


   generateInvoice(invoiceNo: string, seq: number): Observable<any> {
    const params = new HttpParams()
      .set('invoiceNo', invoiceNo)
      .set('seq', seq.toString());
    return this.http.post<any>(`${this.baseUrl}/generate-invoice`, null, { params });
  }


  getTransactions(invoiceNo: string): Observable<any> {
  const params = new HttpParams().set('invoiceNo', invoiceNo);
  return this.http.get(`${this.baseUrl}/transactions`, { params });
}
}
