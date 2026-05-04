import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CookieService } from 'ngx-cookie-service';
import { environment } from '../../../environments/environments.development';

export interface AdvantageImportVM {
  id?: number;
  companyName: string;
  shippingContact: string;
  contactNumber: string;
  orderDate: string | Date | null;
  orderType: string;
  spireOrder: string;
  gOrderNumber: string;
  temporaryNumber: string;
  macAddress: string;
  userName: string;
  bvPartNo: string;
  shippingAddress: string;
  address: string;
  city: string;
  province: string;
  postalCode: string;
  v21Ban: string;
  validated?: boolean;
  reason?: string;
  imported?: boolean;
  userId?: number;
  contactEmail?:string;
  rogersSpecialistEmail:string;
  hardwareType?:string;
  purolatorNumber?:string;
  returnPurolatorNumber?:string;
  dciInvoice?:string;
  status?:string;
  completedDate?:string|Date|null;
  note?:string 
}
@Injectable({
  providedIn: 'root',
})
export class AdvantageVoiceService {
private apiUrl = `${environment.apiUrl}/AdvantageVoice`;

  constructor(private http: HttpClient, private cookieService: CookieService) { }

  private getUserId(): number {
    const userIdStr = this.cookieService.get('userid') || this.cookieService.get('userId') || this.cookieService.get('UserId');
    return userIdStr ? parseInt(userIdStr, 10) : 1; // Default to 1 for dev fallback if missing
  }

  getPendingImports(): Observable<AdvantageImportVM[]> {
    return this.http.get<AdvantageImportVM[]>(`${this.apiUrl}/GetPendingImports?userId=${this.getUserId()}`);
  }

  importExcel(file: File): Observable<boolean> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<boolean>(`${this.apiUrl}/ImportExcel?userId=${this.getUserId()}`, formData);
  }

  validateData(): Observable<AdvantageImportVM[]> {
    return this.http.post<AdvantageImportVM[]>(`${this.apiUrl}/ValidateData?userId=${this.getUserId()}`, {});
  }

  submitOrders(): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiUrl}/SubmitOrders?userId=${this.getUserId()}`, {});
  }

}

