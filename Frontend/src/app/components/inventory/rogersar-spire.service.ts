import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environments.development';

@Injectable({
  providedIn: 'root'
})
export class RogersarSpireService {
  private apiUrl = `${environment.apiUrl}/Roger`;

  constructor(private http: HttpClient) { }

   getARData(searchTerm: string = '', page: number = 1, pageSize: number = 10): Observable<any> {
    const params = {
      searchTerm: searchTerm,
      pageNumber: page.toString(),
      pageSize: pageSize.toString()
    };
    return this.http.get<any>(`${this.apiUrl}/list`, { params, withCredentials: true });
  }

  updateARData(item: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/update`, item, { withCredentials: true });
  }

  loadARData(pageNumber: number, pageSize: number) {
  return this.http.post<any>(
    `${this.apiUrl}/load?pageNumber=${pageNumber}&pageSize=${pageSize}`, 
    {}
  );
}
  exportToExcel(): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/export`, { responseType: 'blob', withCredentials: true });
  }
}

