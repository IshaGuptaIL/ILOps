import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environments.development';
import { ApiResponse } from './add-inventory-component/inventory-service';

@Injectable({
  providedIn: 'root',
})
export class CountService {
  private readonly apiUrl = `${environment.apiUrl}/Count`;

  constructor(private http: HttpClient) {}

  // Command11: Delete specific ACC counts
  

deleteByFile(fileName: string, isACC: boolean): Observable<any> {
  return this.http.delete(`${this.apiUrl}/delete-by-file?fileName=${fileName}&isACC=${isACC}`);
}
syncInventoryFiles(): Observable<any> {
  // Backend endpoint 'sync-inventory-files' se match hona chahiye
  return this.http.post<any>(`${this.apiUrl}/sync-inventory-files`, {});
}

getFileNames(isACC: boolean): Observable<string[]> {
  return this.http.get<string[]>(`${this.apiUrl}/file-names?isACC=${isACC}`);
}
  deleteAllCounts(isACC: boolean): Observable<any> {
  return this.http.delete(`${this.apiUrl}/delete-all/${isACC}`);
}
  // Command4: Load Snapshot
  loadSnapshot(options: { loadACC: boolean, loadIMEI: boolean }): Observable<ApiResponse> {
    return this.http.post<ApiResponse>(`${this.apiUrl}/load-snapshot`, options);
  }


  // Command18 / GetDates: Refresh Dates
  getFileStatus(): Observable<ApiResponse> {
    return this.http.get<ApiResponse>(`${this.apiUrl}/file-status`);
  }

  // Command31 & 32: Excel Exports
  exportHardwareCounts(): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/export-hardware`, { responseType: 'blob' });
  }

exportHardwareSheets(): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/export-hardware`, { responseType: 'blob' });
  }

  exportAccessorySheets(): Observable<Blob> {
  return this.http.get(`${this.apiUrl}/export-accessories`, { 
    responseType: 'blob' 
  });
}
}