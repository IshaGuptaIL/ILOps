import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environments.development';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class ImeiService {
  private ApiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  findByImei(imei: string): Observable<any> {
    return this.http.get<any>(
      `${this.ApiUrl}/Imei/find`,
      { params: { imei } }
    );
  }

  getRogersInvoices(bvReceiptNo: string): Observable<any> {
    return this.http.get<any>(
      `${this.ApiUrl}/Imei/rogers-invoices`,
      { params: { bvReceiptNo } }
    );
  }
}
