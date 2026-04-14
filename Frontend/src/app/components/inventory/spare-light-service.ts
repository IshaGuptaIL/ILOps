import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environments.development';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class SpareLightService {
  private apiUrl = `${environment.apiUrl}/SpareLight`;

  constructor(private http: HttpClient) {}

  uploadHardware(file: File): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    debugger
    return this.http.post(`${this.apiUrl}/UploadHardware`, formData);
  }

  validateHardware(): Observable<any> {
    return this.http.post(`${this.apiUrl}/ValidateHardware`, {});
  }

  doHardwareTransfer(transferDate: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/DoHardwareTransfer?transferDate=${transferDate}`, {});
  }

  uploadAccessory(file: File): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post(`${this.apiUrl}/UploadAccessory`, formData);
  }

  validateAccessory(): Observable<any> {
    return this.http.post(`${this.apiUrl}/ValidateAccessory`, {});
  }

  doAccessoryTransfer(transferDate: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/DoAccessoryTransfer?transferDate=${transferDate}`, {});
  }

 getLog(startDate: string, endDate: string, type: string): Observable<any> {
  return this.http.get(`${this.apiUrl}/Log`, {
    params: {
      startDate,
      endDate,
      type
    }
  });
}
}
