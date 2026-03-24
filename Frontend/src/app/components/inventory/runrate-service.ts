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

  
  getWFHInventory(): Observable<any> {
    return this.http.get(`${this.RunRateUrl}/GetWfhInventory`);
  }

  getRunRate(minDays: number, maxDays: number): Observable<any> {
    return this.http.get(`${this.RunRateUrl}/GetRunRate?minDays=${minDays}&maxDays=${maxDays}`);
  }

   exportHardwareExcel(): Observable<Blob> {
    return this.http.get(`${this.RunRateUrl}/export-hardware`, { responseType: 'blob' });
  }

  exportAccessoriesExcel(): Observable<Blob> {
    return this.http.get(`${this.RunRateUrl}/export-accessories`, { responseType: 'blob' });
  }

  getHardwareView(): Observable<any> {
    return this.http.get(`${this.RunRateUrl}/hardware-view`);
  }

  getAccessoriesView() {
  return this.http.get(`${this.RunRateUrl}/view-accessories`);
}


loadRunRate(startDate: string, endDate: string): Observable<any> {
  return this.http.post<any>(
    `${this.RunRateUrl}/LoadRunRateData`,
    { startDate, endDate }  // this matches your C# RunRateRequest DTO
  );
}


}
