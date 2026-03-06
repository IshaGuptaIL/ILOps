import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environments.development';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class OutputInvoiceService {

  private readonly apiUrl = environment.apiUrl +"/OutputInvoice"
  constructor(private http: HttpClient) {}

 getInvoices(page: number, pageSize: number): Observable<any> {
  return this.http.get<any>(`${this.apiUrl}/list`, {
    params: {
      page: page.toString(),
      pageSize: pageSize.toString()
    }
  });
}

  clearData(): Observable<any> {
    return this.http.delete(`${this.apiUrl}/clear`);
  }

  outputInvoices(payload: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/output-all`, payload);
  }
}
