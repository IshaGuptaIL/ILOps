import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class RogersReportImportService {
  private apiUrl = '/api/sales/rmareporting/import';

  constructor(private http: HttpClient) {}

  uploadFile(file: File, fileType: string): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    
    return this.http.post(`${this.apiUrl}/upload/${fileType}`, formData);
  }

  downloadTemplate(fileType: string): void {
    // Window open or standard anchor download approach to download file blob
    window.open(`${this.apiUrl}/template/${fileType}`, '_blank');
  }

  generateCmSummary(): Observable<any> {
    return this.http.post(`${this.apiUrl}/cmsummary`, {});
  }

  processManualImport(): Observable<any> {
    return this.http.post(`${this.apiUrl}/manualimport`, {});
  }

  deleteBatchFiles(cmFile: string, rmFile: string, manualFile: string): Observable<any> {
    let params = new HttpParams();
    if (cmFile) params = params.append('cmFile', cmFile);
    if (rmFile) params = params.append('rmFile', rmFile);
    if (manualFile) params = params.append('manualFile', manualFile);

    return this.http.delete(`${this.apiUrl}/deletebatch`, { params });
  }
}
