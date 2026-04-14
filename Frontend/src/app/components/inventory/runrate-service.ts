import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environments.development';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class RunrateService {
  

   
    private readonly RunRateUrl = environment.apiUrl + '/RunRate'; 
 constructor(private http: HttpClient) { }

  
  getWFHInventory(userId: number): Observable<any> {
    return this.http.get(`${this.RunRateUrl}/GetWfhInventory?userId=${userId}`, { withCredentials: true });
  }

  getRunRate(minDays: number, maxDays: number, userId: number): Observable<any> {
    return this.http.get(`${this.RunRateUrl}/GetRunRate?minDays=${minDays}&maxDays=${maxDays}&userId=${userId}`, { withCredentials: true });
  }

   exportHardwareExcel(userId: number): Observable<Blob> {
    return this.http.get(`${this.RunRateUrl}/export-hardware?userId=${userId}`, { responseType: 'blob', withCredentials: true });
  }

  exportAccessoriesExcel(userId: number): Observable<Blob> {
    return this.http.get(`${this.RunRateUrl}/export-accessories?userId=${userId}`, { responseType: 'blob', withCredentials: true });
  }

  exportAccessoriesRogersExcel(userId: number): Observable<Blob> {
    return this.http.get(`${this.RunRateUrl}/export-accessories-rogers?userId=${userId}`, { responseType: 'blob', withCredentials: true });
  }

  getHardwareView(pageNumber: number, pageSize: number, userId: number): Observable<any> {
    return this.http.get(`${this.RunRateUrl}/hardware-view?pageNumber=${pageNumber}&pageSize=${pageSize}&userId=${userId}`, { withCredentials: true });
  }

  getAccessoriesView(pageNumber: number, pageSize: number, userId: number): Observable<any> {
    return this.http.get(`${this.RunRateUrl}/view-accessories?pageNumber=${pageNumber}&pageSize=${pageSize}&userId=${userId}`, { withCredentials: true });
  }


loadRunRate(startDate: string, endDate: string, userId: number): Observable<any> {
  return this.http.post<any>(
    `${this.RunRateUrl}/LoadRunRateData`,
    { startDate, endDate, userId },
    { withCredentials: true }
  );
}


}
